using System;
using System.Collections.Generic;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

namespace Ryuka.Sulfur.NativeUI
{
    using UIScrollRect = UnityEngine.UI.ScrollRect;
    using UIText = TMPro.TextMeshProUGUI;

    internal static class SulfurOptionsScreenBridge
    {
        private const int CustomCategoryBase = 9000;

        private static readonly Dictionary<int, string> currentCustomPageByScreen = new Dictionary<int, string>();
        private static readonly Dictionary<int, FooterState> footerByScreen = new Dictionary<int, FooterState>();
        private const float FooterHeight = 66f;
        private const float FooterMargin = 8f;

        public static void ResetOptionsScreenStateBeforeShow(OptionsScreen screen)
        {
            if (screen == null)
                return;

            ClearCustomPageFooter(screen);

            try
            {
                // 原版 Show() 会在 SetupMenu() 后直接用 categoryObjects[menuSelectedIndex]。
                // 自定义分类页会让 menuSelectedIndex 停在超出原生分类范围的位置。
                // 每次打开 OptionsScreen 前都重置到第一个分类，最稳。
                SetPrivate(screen, "menuSelectedIndex", 0);
                SetPrivate(screen, "selectedIndex", 0);
                SetPrivate(screen, "menuSelected", true);
                SetPrivate(screen, "currentOption", null);

                TMP_InputField input = GetCurrentTextInput();
                if (input != null && input.isFocused)
                    input.DeactivateInputField();
            }
            catch
            {
            }
        }

        public static bool TryHandleTextInputCancel(InputAction.CallbackContext context)
        {
            TMP_InputField input = GetCurrentTextInput();

            if (input == null || !input.isFocused)
                return false;

            string controlName = "";

            try
            {
                if (context.control != null)
                    controlName = context.control.name ?? "";
            }
            catch
            {
                controlName = "";
            }

            controlName = controlName.ToLowerInvariant();

            // Backspace should delete text inside TMP_InputField.
            // Do not let the game's OptionsScreen.NavigateCancel() treat it as page-back.
            if (controlName.Contains("backspace"))
                return true;

            // Escape / controller cancel: first leave the input field instead of closing the options page.
            input.DeactivateInputField();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(input.gameObject);

            return true;
        }

        private static TMP_InputField GetCurrentTextInput()
        {
            if (EventSystem.current == null)
                return null;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return null;

            TMP_InputField input = selected.GetComponent<TMP_InputField>();
            if (input != null)
                return input;

            input = selected.GetComponentInParent<TMP_InputField>();
            if (input != null)
                return input;

            return selected.GetComponentInChildren<TMP_InputField>(true);
        }

        public static void InjectCustomCategories(OptionsScreen screen)
        {
            if (screen == null)
                return;

            IReadOnlyList<SulfurOptionsPage> pages = SulfurOptionsApi.Pages;
            if (pages == null || pages.Count == 0)
                return;

            try
            {
                GameObject categoryPrefab = GetPrivate<GameObject>(screen, "optionsCategoryButtonPrefab");
                GameObject categoryMenu = GetPrivate<GameObject>(screen, "optionsCategoryMenu");
                List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");

                if (categoryPrefab == null || categoryMenu == null || categoryObjects == null)
                    return;

                RemoveExistingCustomCategories(categoryMenu.transform, categoryObjects);

                foreach (SulfurOptionsPage page in pages)
                {
                    if (page == null || string.IsNullOrWhiteSpace(page.PageId))
                        continue;

                    GameObject go = UnityEngine.Object.Instantiate(categoryPrefab, categoryMenu.transform);
                    go.name = "SULFUR_CustomCategory_" + page.PageId;
                    go.SetActive(true);

                    OptionsScreenCategory category = go.GetComponent<OptionsScreenCategory>();
                    if (category == null)
                    {
                        UnityEngine.Object.Destroy(go);
                        continue;
                    }

                    category.SetToCategory((PlayerSetting.SettingCategory)(CustomCategoryBase + categoryObjects.Count));

                    SulfurCustomCategoryMarker marker = go.AddComponent<SulfurCustomCategoryMarker>();
                    marker.Initialize(page.PageId);

                    categoryObjects.Add(category);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI failed to inject custom options categories: " + ex);
            }
        }

        public static bool TryShowCustomPage(OptionsScreen screen, OptionsScreenCategory category, bool selectFirst)
        {
            if (screen == null || category == null)
                return false;

            SulfurCustomCategoryMarker marker = category.GetComponent<SulfurCustomCategoryMarker>();
            if (marker == null)
            {
                ClearCustomPageFooter(screen);
                currentCustomPageByScreen.Remove(screen.GetInstanceID());
                return false;
            }

            SulfurOptionsPage page = SulfurOptionsApi.GetPage(marker.PageId);
            if (page == null)
            {
                ClearCustomPageFooter(screen);
                currentCustomPageByScreen.Remove(screen.GetInstanceID());
                return false;
            }

            return BuildCustomPage(screen, category, page, selectFirst, 0);
        }

        public static void RebuildCurrentCustomPage(OptionsScreen screen)
        {
            if (screen == null)
                return;

            string pageId;
            if (!currentCustomPageByScreen.TryGetValue(screen.GetInstanceID(), out pageId))
                return;

            SulfurOptionsPage page = SulfurOptionsApi.GetPage(pageId);
            if (page == null)
                return;

            List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");
            if (categoryObjects == null)
                return;

            OptionsScreenCategory category = null;
            foreach (OptionsScreenCategory c in categoryObjects)
            {
                if (c == null)
                    continue;

                SulfurCustomCategoryMarker marker = c.GetComponent<SulfurCustomCategoryMarker>();
                if (marker != null && string.Equals(marker.PageId, pageId, StringComparison.OrdinalIgnoreCase))
                {
                    category = c;
                    break;
                }
            }

            if (category == null)
                return;

            int selectedIndex = GetPrivate<int>(screen, "selectedIndex");

            RectTransform optionsContainer = GetPrivate<RectTransform>(screen, "optionsContainer");
            UIScrollRect scrollParent = GetPrivate<UIScrollRect>(screen, "scrollParent");

            Vector2 savedAnchoredPosition = Vector2.zero;
            Vector2 savedNormalizedPosition = new Vector2(0f, 1f);
            bool preserveScroll = false;

            if (scrollParent != null)
            {
                preserveScroll = true;
                savedNormalizedPosition = scrollParent.normalizedPosition;

                RectTransform content = scrollParent.content != null ? scrollParent.content : optionsContainer;
                if (content != null)
                    savedAnchoredPosition = content.anchoredPosition;
            }
            else if (optionsContainer != null)
            {
                preserveScroll = true;
                savedAnchoredPosition = optionsContainer.anchoredPosition;
            }

            BuildCustomPage(
                screen,
                category,
                page,
                true,
                selectedIndex,
                preserveScroll,
                savedAnchoredPosition,
                savedNormalizedPosition);
        }

        private static bool BuildCustomPage(
            OptionsScreen screen,
            OptionsScreenCategory category,
            SulfurOptionsPage page,
            bool selectFirst,
            int preferredSelectedIndex,
            bool preserveScroll = false,
            Vector2 savedScrollAnchoredPosition = default(Vector2),
            Vector2 savedScrollNormalizedPosition = default(Vector2))
        {
            try
            {
                RectTransform optionsContainer = GetPrivate<RectTransform>(screen, "optionsContainer");
                CanvasGroup optionsBodyCanvasGroup = GetPrivate<CanvasGroup>(screen, "optionsBodyCanvasGroup");
                UIScrollRect scrollParent = GetPrivate<UIScrollRect>(screen, "scrollParent");
                List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");

                if (optionsContainer == null || categoryObjects == null)
                    return false;

                InvokePrivate(screen, "ShowRebindContent", false, false);

                DestroyChildren(optionsContainer);

                foreach (OptionsScreenCategory c in categoryObjects)
                    c.SetSelected(c == category);

                int categoryIndex = categoryObjects.IndexOf(category);
                if (categoryIndex >= 0)
                    SetPrivate(screen, "menuSelectedIndex", categoryIndex);

                List<OptionsScreenOption> options = new List<OptionsScreenOption>();
                SetPrivate(screen, "options", options);
                SetPrivate(screen, "currentOption", null);
                SetPrivate(screen, "selectedIndex", 0);

                if (scrollParent != null)
                {
                    scrollParent.content = optionsContainer;

                    if (!preserveScroll)
                        optionsContainer.anchoredPosition = Vector2.zero;
                }
                else if (!preserveScroll)
                {
                    optionsContainer.anchoredPosition = Vector2.zero;
                }

                currentCustomPageByScreen[screen.GetInstanceID()] = page.PageId;
                ClearCustomPageFooter(screen);

                SulfurOptionsContext context = new SulfurOptionsContext(screen, optionsContainer, options, page.PageId);
                page.BuildPage(context);

                if (options.Count == 0)
                    context.AddInfo("No options.");

                if (selectFirst && options.Count > 0)
                {
                    int index = Mathf.Clamp(preferredSelectedIndex, 0, options.Count - 1);

                    SetPrivate(screen, "menuSelected", false);
                    SetPrivate(screen, "selectedIndex", index);
                    SetPrivate(screen, "currentOption", options[index]);
                    options[index].SetSelected(true);
                }
                else
                {
                    SetPrivate(screen, "menuSelected", true);
                }

                if (optionsBodyCanvasGroup != null)
                    optionsBodyCanvasGroup.alpha = 1f;

                if (preserveScroll)
                {
                    if (scrollParent != null)
                        screen.StartCoroutine(RestoreScrollAfterLayout(scrollParent, optionsContainer, savedScrollAnchoredPosition, savedScrollNormalizedPosition));
                    else
                        optionsContainer.anchoredPosition = savedScrollAnchoredPosition;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SULFUR Native UI failed to show custom options page: " + ex);

                return false;
            }
        }

        public static bool TryNavigateVertical(OptionsScreen screen, int delta)
        {
            if (screen == null || SulfurOptionsApi.Pages.Count == 0)
                return false;

            if (SulfurTextInputBinding.IsAnyInputFocused)
                return true;

            bool menuSelected = GetPrivate<bool>(screen, "menuSelected");
            if (!menuSelected)
                return false;

            List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");
            if (categoryObjects == null || categoryObjects.Count == 0)
                return false;

            int menuSelectedIndex = GetPrivate<int>(screen, "menuSelectedIndex");

            int next = menuSelectedIndex + delta;
            if (next < 0 || next >= categoryObjects.Count)
                return true;

            SetPrivate(screen, "menuSelectedIndex", next);
            screen.SetCategory(categoryObjects[next], false);
            return true;
        }

        public static bool TryNavigateHorizontal(OptionsScreen screen, int delta)
        {
            if (screen == null)
                return false;

            if (SulfurTextInputBinding.IsAnyInputFocused)
                return true;

            OptionsScreenOption currentOption = GetPrivate<OptionsScreenOption>(screen, "currentOption");
            if (currentOption == null)
                return false;

            SulfurOptionBinding binding = currentOption.GetComponent<SulfurOptionBinding>();
            if (binding == null || binding.OnHorizontal == null)
                return false;

            binding.InvokeHorizontal(delta);
            return true;
        }

        public static bool TryNavigateCancel(OptionsScreen screen)
        {
            if (screen == null)
                return false;

            return SulfurTextInputBinding.CancelActiveInput();
        }

        public static void ResetNativeSelectionStateBeforeShow(OptionsScreen screen)
        {
            if (screen == null)
                return;

            ClearCustomPageFooter(screen);

            try
            {
                List<PlayerSetting.SettingCategory> menuOrder =
                    GetPrivate<List<PlayerSetting.SettingCategory>>(screen, "menuOrder");

                int menuSelectedIndex = GetPrivate<int>(screen, "menuSelectedIndex");

                if (menuOrder == null || menuOrder.Count == 0)
                {
                    SetPrivate(screen, "menuSelectedIndex", 0);
                }
                else if (menuSelectedIndex < 0 || menuSelectedIndex >= menuOrder.Count)
                {
                    SetPrivate(screen, "menuSelectedIndex", 0);
                }

                int selectedIndex = GetPrivate<int>(screen, "selectedIndex");
                if (selectedIndex < 0)
                    SetPrivate(screen, "selectedIndex", 0);

                SetPrivate(screen, "menuSelected", true);
                SetPrivate(screen, "currentOption", null);
                currentCustomPageByScreen.Remove(screen.GetInstanceID());
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI failed to reset OptionsScreen state before Show: " + ex.Message);
            }
        }


        public static void SetCustomPageFooter(
            OptionsScreen screen,
            string leftText,
            string statusText,
            string primaryButtonText,
            Action onPrimaryPressed)
        {
            if (screen == null)
                return;

            try
            {
                int id = screen.GetInstanceID();

                FooterState state;
                if (!footerByScreen.TryGetValue(id, out state) || state == null || state.Root == null)
                {
                    state = CreateFooterState(screen);
                    if (state == null)
                        return;

                    footerByScreen[id] = state;
                }

                state.SetText(leftText, statusText, primaryButtonText);
                state.SetAction(onPrimaryPressed);
                state.SetVisible(true);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI failed to set custom footer: " + ex.Message);
            }
        }

        public static void SetCustomPageFooterStatus(OptionsScreen screen, string statusText)
        {
            if (screen == null)
                return;

            FooterState state;
            if (!footerByScreen.TryGetValue(screen.GetInstanceID(), out state) || state == null)
                return;

            state.SetStatus(statusText);
        }

        public static void ClearCustomPageFooter(OptionsScreen screen)
        {
            if (screen == null)
                return;

            int id = screen.GetInstanceID();

            FooterState state;
            if (!footerByScreen.TryGetValue(id, out state) || state == null)
                return;

            state.Destroy();
            footerByScreen.Remove(id);
        }

        private static FooterState CreateFooterState(OptionsScreen screen)
        {
            UIScrollRect scrollParent = GetPrivate<UIScrollRect>(screen, "scrollParent");
            if (scrollParent == null)
                return null;

            RectTransform scrollRect = scrollParent.GetComponent<RectTransform>();
            RectTransform parent = scrollParent.transform.parent as RectTransform;

            if (parent == null)
                parent = screen.transform as RectTransform;

            if (parent == null)
                return null;

            Vector2 originalOffsetMin = scrollRect != null ? scrollRect.offsetMin : Vector2.zero;

            if (scrollRect != null)
                scrollRect.offsetMin = new Vector2(scrollRect.offsetMin.x, originalOffsetMin.y + FooterHeight + FooterMargin);

            GameObject root = new GameObject(
                "SULFUR_CustomPageFooter",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            root.transform.SetParent(parent, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, FooterHeight);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.38f);
            bg.raycastTarget = true;

            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI sample = root.GetComponentInParent<OptionsScreen>() != null
                ? root.GetComponentInParent<OptionsScreen>().GetComponentInChildren<TextMeshProUGUI>(true)
                : null;

            TextMeshProUGUI left = CreateFooterText(root.transform, "LeftText", sample, TextAlignmentOptions.MidlineLeft, 360f, 460f, 0f);
            TextMeshProUGUI status = CreateFooterText(root.transform, "StatusText", sample, TextAlignmentOptions.MidlineLeft, 260f, 9999f, 1f);

            LayoutElement statusLayout = status.gameObject.GetComponent<LayoutElement>();
            statusLayout.flexibleWidth = 1f;

            Button primaryButton = CreateFooterButton(root.transform, "Apply", sample);

            FooterState state = new FooterState();
            state.Root = root;
            state.ScrollRect = scrollRect;
            state.OriginalScrollOffsetMin = originalOffsetMin;
            state.LeftText = left;
            state.StatusText = status;
            state.PrimaryButton = primaryButton;

            return state;
        }

        private static TextMeshProUGUI CreateFooterText(
            Transform parent,
            string name,
            TextMeshProUGUI sample,
            TextAlignmentOptions alignment,
            float minWidth,
            float preferredWidth,
            float flexibleWidth)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                text.font = sample.font;
                text.fontSize = Mathf.Max(16f, sample.fontSize * 0.78f);
                text.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.88f);
            }
            else
            {
                text.fontSize = 18f;
                text.color = new Color(1f, 1f, 1f, 0.88f);
            }

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minWidth = minWidth;
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 1f;

            return text;
        }

        private static Button CreateFooterButton(Transform parent, string text, TextMeshProUGUI sample)
        {
            GameObject go = new GameObject("PrimaryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.11f);
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.11f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.26f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.05f);
            button.colors = colors;

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 150f;
            layout.preferredWidth = 170f;
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = text ?? "";
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontSize = Mathf.Max(16f, sample.fontSize * 0.82f);
                label.color = sample.color;
            }
            else
            {
                label.fontSize = 18f;
                label.color = Color.white;
            }

            return button;
        }

        private sealed class FooterState
        {
            public GameObject Root;
            public RectTransform ScrollRect;
            public Vector2 OriginalScrollOffsetMin;
            public TextMeshProUGUI LeftText;
            public TextMeshProUGUI StatusText;
            public Button PrimaryButton;

            public void SetText(string leftText, string statusText, string primaryButtonText)
            {
                if (LeftText != null)
                    LeftText.text = leftText ?? "";

                if (StatusText != null)
                    StatusText.text = statusText ?? "";

                if (PrimaryButton != null)
                {
                    TextMeshProUGUI label = PrimaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null)
                        label.text = string.IsNullOrWhiteSpace(primaryButtonText) ? "Apply" : primaryButtonText;
                }
            }

            public void SetStatus(string statusText)
            {
                if (StatusText != null)
                    StatusText.text = statusText ?? "";
            }

            public void SetAction(Action action)
            {
                if (PrimaryButton == null)
                    return;

                PrimaryButton.onClick.RemoveAllListeners();

                if (action != null)
                {
                    PrimaryButton.interactable = true;
                    PrimaryButton.onClick.AddListener(new UnityAction(delegate
                    {
                        action();
                    }));
                }
                else
                {
                    PrimaryButton.interactable = false;
                }
            }

            public void SetVisible(bool visible)
            {
                if (Root != null)
                    Root.SetActive(visible);
            }

            public void Destroy()
            {
                if (ScrollRect != null)
                    ScrollRect.offsetMin = OriginalScrollOffsetMin;

                if (Root != null)
                    UnityEngine.Object.Destroy(Root);
            }
        }

        public static GameObject GetOptionsCategoryHeaderPrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionsCategoryHeaderPrefab");
        }

        public static GameObject GetOptionButtonPrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionButtonPrefab");
        }

        public static GameObject GetOptionBoolPrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionBoolPrefab");
        }

        public static GameObject GetOptionInfoPrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionInfoPrefab");
        }

        public static GameObject GetOptionAlternativePrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionAlternativePrefab");
        }

        public static GameObject GetOptionSliderPrefab(OptionsScreen screen)
        {
            return GetPrivate<GameObject>(screen, "optionSliderPrefab");
        }

        public static UIText GetOptionLabelText(OptionsScreenOption option)
        {
            UIText direct = GetPrivate<UIText>(option, "optionLabelText");
            if (direct != null)
                return direct;

            UIText[] texts = option.GetComponentsInChildren<UIText>(true);
            return texts != null && texts.Length > 0 ? texts[0] : null;
        }

        public static UIText GetAlternativeLabelText(OptionsScreenOption option)
        {
            UIText direct = GetPrivate<UIText>(option, "alternativeLabelText");
            if (direct != null)
                return direct;

            Transform found = FindChildByName(option.transform, "LabelTMPro");
            return found != null ? found.GetComponent<UIText>() : null;
        }

        public static UIText GetSliderValueText(OptionsScreenOption option)
        {
            UIText direct = GetPrivate<UIText>(option, "sliderValueText");
            if (direct != null)
                return direct;

            Transform found = FindChildByName(option.transform, "Value");
            if (found == null)
                return null;

            return found.GetComponentInChildren<UIText>(true);
        }

        private static bool HasCategoryMarker(Transform parent, string pageId)
        {
            if (parent == null)
                return false;

            SulfurCustomCategoryMarker[] markers = parent.GetComponentsInChildren<SulfurCustomCategoryMarker>(true);
            foreach (SulfurCustomCategoryMarker marker in markers)
            {
                if (marker != null && string.Equals(marker.PageId, pageId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void RemoveExistingCustomCategories(
            Transform categoryMenu,
            List<OptionsScreenCategory> categoryObjects)
        {
            if (categoryMenu == null)
                return;

            SulfurCustomCategoryMarker[] markers =
                categoryMenu.GetComponentsInChildren<SulfurCustomCategoryMarker>(true);

            foreach (SulfurCustomCategoryMarker marker in markers)
            {
                if (marker == null)
                    continue;

                OptionsScreenCategory category =
                    marker.GetComponent<OptionsScreenCategory>();

                if (category != null && categoryObjects != null)
                    categoryObjects.Remove(category);

                UnityEngine.Object.Destroy(marker.gameObject);
            }
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        private static IEnumerator RestoreScrollAfterLayout(
    UIScrollRect scrollParent,
    RectTransform content,
    Vector2 savedAnchoredPosition,
    Vector2 savedNormalizedPosition)
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            RestoreScrollPosition(scrollParent, content, savedAnchoredPosition, savedNormalizedPosition);

            yield return null;

            Canvas.ForceUpdateCanvases();
            RestoreScrollPosition(scrollParent, content, savedAnchoredPosition, savedNormalizedPosition);
        }

        private static void RestoreScrollPosition(
            UIScrollRect scrollParent,
            RectTransform content,
            Vector2 savedAnchoredPosition,
            Vector2 savedNormalizedPosition)
        {
            if (scrollParent != null)
                scrollParent.StopMovement();

            if (content == null && scrollParent != null)
                content = scrollParent.content;

            if (content != null)
            {
                Vector2 position = savedAnchoredPosition;
                position.y = ClampContentY(scrollParent, content, position.y);
                content.anchoredPosition = position;
                return;
            }

            if (scrollParent != null)
            {
                scrollParent.normalizedPosition = new Vector2(
                    Mathf.Clamp01(savedNormalizedPosition.x),
                    Mathf.Clamp01(savedNormalizedPosition.y));
            }
        }

        private static float ClampContentY(UIScrollRect scrollParent, RectTransform content, float y)
        {
            if (scrollParent == null || content == null)
                return Mathf.Max(0f, y);

            RectTransform viewport = scrollParent.viewport;
            if (viewport == null)
                viewport = scrollParent.GetComponent<RectTransform>();

            if (viewport == null)
                return Mathf.Max(0f, y);

            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);

            return Mathf.Clamp(y, 0f, maxY);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static T GetPrivate<T>(object obj, string fieldName)
        {
            return SulfurReflection.GetField<T>(obj, fieldName);
        }

        private static void SetPrivate(object obj, string fieldName, object value)
        {
            SulfurReflection.SetField(obj, fieldName, value);
        }

        private static object InvokePrivate(object obj, string methodName, params object[] args)
        {
            return SulfurReflection.Invoke(obj, methodName, args);
        }
    }
}
