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

        // The custom page each options screen currently shows (keyed by screen instance id).
        // Owned here: set by BuildCustomPage, ended (and its context invalidated) when another
        // page or a native category is shown, or when the screen is hidden or shown again.
        private static readonly Dictionary<int, CustomPageState> currentCustomPageByScreen = new Dictionary<int, CustomPageState>();
        private static readonly Dictionary<int, FooterState> footerByScreen = new Dictionary<int, FooterState>();
        private static readonly List<int> pruneBuffer = new List<int>();
        private static readonly HashSet<string> loggedBuildFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const float FooterHeight = 66f;
        private const float FooterMargin = 8f;

        // True while the game's own NavigateUse / NavigateCancel runs (set by their patches).
        // Only then may a SetCategory for the page already shown keep the page instead of
        // rebuilding it: those are the keyboard paths "Enter on the category" and "Escape back
        // to the category menu", which re-show the same page and only move the selection.
        private static bool inGameNavigation;

        private sealed class CustomPageState
        {
            public OptionsScreen Screen;
            public string PageId;
            public OptionsScreenCategory Category;
            public List<OptionsScreenOption> Options;
            public SulfurOptionsContext Context;
        }

        public static void ResetOptionsScreenStateBeforeShow(OptionsScreen screen)
        {
            if (screen == null)
                return;

            ClearCustomPageFooter(screen);
            EndCustomPage(screen);

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
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI failed to reset OptionsScreen state before Show: " + ex);
            }
        }

        // OptionsScreen.Hide: the shown custom page goes away with the screen, so a mod still
        // holding its context must not write into it (or into the next page) any more.
        public static void OnOptionsScreenHiding(OptionsScreen screen)
        {
            if (screen == null)
                return;

            EndCustomPage(screen);
        }

        internal static void BeginGameNavigation()
        {
            inGameNavigation = true;
        }

        internal static void EndGameNavigation()
        {
            inGameNavigation = false;
        }

        // UI Submit (Space / Enter) while a library text input has focus is typing, not a
        // command: OptionsScreen.NavigateUse would otherwise run category.Use() (full page
        // rebuild, the input is destroyed) or currentOption.Use().
        public static bool ShouldSwallowSubmit()
        {
            return SulfurTextInputBinding.IsAnyInputFocused;
        }

        private static bool TryGetCustomPage(OptionsScreen screen, out CustomPageState state)
        {
            state = null;

            if (screen == null)
                return false;

            if (!currentCustomPageByScreen.TryGetValue(screen.GetInstanceID(), out state) || state == null)
                return false;

            return true;
        }

        private static void EndCustomPage(OptionsScreen screen)
        {
            if (screen == null)
                return;

            int id = screen.GetInstanceID();

            CustomPageState state;
            if (!currentCustomPageByScreen.TryGetValue(id, out state))
                return;

            currentCustomPageByScreen.Remove(id);

            if (state != null && state.Context != null)
                state.Context.Invalidate();
        }

        // Entries are keyed by instance id; a destroyed screen (scene change) never ends its
        // page through Hide, so drop those entries whenever a screen sets up its menu.
        private static void PruneDestroyedScreens()
        {
            pruneBuffer.Clear();

            foreach (KeyValuePair<int, CustomPageState> pair in currentCustomPageByScreen)
            {
                if (pair.Value == null || pair.Value.Screen == null)
                    pruneBuffer.Add(pair.Key);
            }

            foreach (int id in pruneBuffer)
            {
                CustomPageState state = currentCustomPageByScreen[id];
                currentCustomPageByScreen.Remove(id);

                if (state != null && state.Context != null)
                    state.Context.Invalidate();
            }

            pruneBuffer.Clear();

            foreach (KeyValuePair<int, FooterState> pair in footerByScreen)
            {
                // The footer root lives under the screen, so it is destroyed with it.
                if (pair.Value == null || pair.Value.Root == null)
                    pruneBuffer.Add(pair.Key);
            }

            foreach (int id in pruneBuffer)
                footerByScreen.Remove(id);

            pruneBuffer.Clear();
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
            // clearSelection ends the edit the way TMP's own Escape handling does (onEndEdit fires,
            // so a number input commits or reverts what was typed), which this early cancel
            // pre-empts; without it the typed text stayed uncommitted until the field lost the
            // EventSystem selection. The row stays selected for keyboard/gamepad navigation.
            Transform rootBefore = input.transform.root;
            input.DeactivateInputField(true);

            // An onEndEdit handler may have rebuilt the page (the field is then detached and
            // about to be destroyed): do not select it again.
            if (EventSystem.current != null && input != null && input.transform.root == rootBefore)
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

            PruneDestroyedScreens();

            IReadOnlyList<SulfurOptionsPage> pages = SulfurOptionsApi.Pages;
            if (pages == null || pages.Count == 0)
                return;

            GameObject categoryPrefab;
            GameObject categoryMenu;
            List<OptionsScreenCategory> categoryObjects;

            try
            {
                categoryPrefab = GetPrivate<GameObject>(screen, "optionsCategoryButtonPrefab");
                categoryMenu = GetPrivate<GameObject>(screen, "optionsCategoryMenu");
                categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");

                if (categoryPrefab == null || categoryMenu == null || categoryObjects == null)
                    return;

                RemoveExistingCustomCategories(categoryMenu.transform, categoryObjects);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI failed to inject custom options categories: " + ex);

                return;
            }

            // Per page: one page whose display-name delegate throws must not hide the pages
            // after it or leave a half-initialized button behind.
            foreach (SulfurOptionsPage page in pages)
            {
                if (page == null || string.IsNullOrWhiteSpace(page.PageId))
                    continue;

                GameObject go = null;

                try
                {
                    go = UnityEngine.Object.Instantiate(categoryPrefab, categoryMenu.transform);
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
                catch (Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogWarning("SULFUR Native UI failed to add the options category for page '" + page.PageId + "'; the page is skipped: " + ex);

                    if (go != null)
                    {
                        // Out of the menu now, so the menu layout and the marker scan on the
                        // next SetupMenu do not see it while Destroy is pending.
                        go.SetActive(false);
                        go.transform.SetParent(null, false);
                        UnityEngine.Object.Destroy(go);
                    }
                }
            }
        }

        // SetCategory prefix. A category carrying the library's marker is always handled here,
        // also when its page is no longer registered or its build fails: the game's own
        // SetCategory cannot render the fake 9000+n category and would leave a blank page with
        // an empty navigation list.
        public static bool TryShowCustomPage(OptionsScreen screen, OptionsScreenCategory category, bool selectFirst)
        {
            if (screen == null || category == null)
                return false;

            SulfurCustomCategoryMarker marker = category.GetComponent<SulfurCustomCategoryMarker>();
            if (marker == null)
            {
                ClearCustomPageFooter(screen);
                EndCustomPage(screen);
                return false;
            }

            try
            {
                SulfurOptionsPage page = SulfurOptionsApi.GetPage(marker.PageId);

                if (page != null && inGameNavigation && TryKeepShownPage(screen, category, page, selectFirst))
                    return true;

                BuildCustomPage(screen, category, marker.PageId, page, selectFirst, 0, true);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SULFUR Native UI failed to show custom options page '" + marker.PageId + "': " + ex);
            }

            return true;
        }

        // Enter on the category (OptionsScreenCategory.Use -> SetCategory(selectFirst: true))
        // and Escape back to the menu (NavigateCancel -> MenuSelectIndex -> SetCategory) re-show
        // the page that is already on screen; the game rebuilds its own pages there, but for a
        // custom page that costs a full BuildPage and resets the scroll. Keep the page and do
        // only what those calls change: category highlight (with the game's select sound, as
        // on its own pages), menuSelectedIndex and the row selection.
        private static bool TryKeepShownPage(
            OptionsScreen screen,
            OptionsScreenCategory category,
            SulfurOptionsPage page,
            bool selectFirst)
        {
            CustomPageState state;
            if (!TryGetCustomPage(screen, out state))
                return false;

            if (state.Category != category ||
                !string.Equals(state.PageId, page.PageId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!screen.IsShown || state.Context == null || !state.Context.IsAlive)
                return false;

            List<OptionsScreenOption> options = GetPrivate<List<OptionsScreenOption>>(screen, "options");
            if (!ReferenceEquals(options, state.Options) || GetPrivate<bool>(screen, "showingRebind"))
                return false;

            List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");
            if (categoryObjects == null)
                return false;

            foreach (OptionsScreenCategory c in categoryObjects)
            {
                if (c != null)
                    c.SetSelected(c == category);
            }

            int categoryIndex = categoryObjects.IndexOf(category);
            if (categoryIndex >= 0)
                SetPrivate(screen, "menuSelectedIndex", categoryIndex);

            if (selectFirst)
            {
                // As a fresh build would: stop the game's auto-scroll and show the top, so
                // the first row (now selected) is in view. Escape back keeps the scroll.
                InvokePrivate(screen, "ShowRebindContent", false, false);

                RectTransform optionsContainer = GetPrivate<RectTransform>(screen, "optionsContainer");
                if (optionsContainer != null)
                    optionsContainer.anchoredPosition = Vector2.zero;

                SelectRowNear(screen, options, 0);
            }
            else
            {
                ReturnSelectionToMenu(screen, options);
            }

            return true;
        }

        public static void RebuildCurrentCustomPage(OptionsScreen screen)
        {
            // Building into a hidden screen is wasted work and the scroll restore coroutine
            // cannot start on an inactive object.
            if (screen == null || !screen.IsShown)
                return;

            CustomPageState state;
            if (!TryGetCustomPage(screen, out state))
                return;

            string pageId = state.PageId;

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

            // Keep the input mode: a keyboard/gamepad user keeps the selected row, a mouse user
            // (category menu focus) gets no row selection, so Escape still closes the screen.
            bool menuSelected = GetPrivate<bool>(screen, "menuSelected");

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
                pageId,
                SulfurOptionsApi.GetPage(pageId),
                !menuSelected,
                selectedIndex,
                false,
                preserveScroll,
                savedAnchoredPosition,
                savedNormalizedPosition);
        }

        private static void BuildCustomPage(
            OptionsScreen screen,
            OptionsScreenCategory category,
            string pageId,
            SulfurOptionsPage page,
            bool selectFirst,
            int preferredSelectedIndex,
            bool replayCategorySelect,
            bool preserveScroll = false,
            Vector2 savedScrollAnchoredPosition = default(Vector2),
            Vector2 savedScrollNormalizedPosition = default(Vector2))
        {
            RectTransform optionsContainer = GetPrivate<RectTransform>(screen, "optionsContainer");
            CanvasGroup optionsBodyCanvasGroup = GetPrivate<CanvasGroup>(screen, "optionsBodyCanvasGroup");
            UIScrollRect scrollParent = GetPrivate<UIScrollRect>(screen, "scrollParent");
            List<OptionsScreenCategory> categoryObjects = GetPrivate<List<OptionsScreenCategory>>(screen, "categoryObjects");

            if (optionsContainer == null || categoryObjects == null)
            {
                EndCustomPage(screen);

                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("SULFUR Native UI cannot show custom options page '" + pageId + "': the options screen layout was not found.");

                return;
            }

            List<OptionsScreenOption> options = new List<OptionsScreenOption>();

            try
            {
                InvokePrivate(screen, "ShowRebindContent", false, false);

                // The previous page's context (this page on a rebuild, or another page) stops
                // being usable before its rows go away.
                EndCustomPage(screen);
                DestroyChildrenNow(optionsContainer);

                foreach (OptionsScreenCategory c in categoryObjects)
                {
                    if (c == null)
                        continue;

                    if (c != category)
                    {
                        c.SetSelected(false);
                    }
                    else if (replayCategorySelect || !GetPrivate<bool>(c, "isSelected"))
                    {
                        // SetSelected(true) plays the game's options-select sound; a library
                        // rebuild of the page already shown must not replay it.
                        c.SetSelected(true);
                    }
                }

                int categoryIndex = categoryObjects.IndexOf(category);
                if (categoryIndex >= 0)
                    SetPrivate(screen, "menuSelectedIndex", categoryIndex);

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

                ClearCustomPageFooter(screen);

                CustomPageState state = new CustomPageState
                {
                    Screen = screen,
                    PageId = pageId,
                    Category = category,
                    Options = options
                };

                currentCustomPageByScreen[screen.GetInstanceID()] = state;

                RunPageBuild(screen, state, optionsContainer, page);

                // The page called Rebuild() from inside its own BuildPage: the inner build
                // replaced this one and already finished; do not touch its state.
                CustomPageState current;
                if (!TryGetCustomPage(screen, out current) || !ReferenceEquals(current, state))
                    return;

                // Rows of the page, not the footer's navigation entry.
                if (!HasRowUnder(options, optionsContainer))
                    state.Context.AddInfo(SulfurUiText.NoOptions);

                if (selectFirst)
                    SelectRowNear(screen, options, preferredSelectedIndex);
                else
                    SetPrivate(screen, "menuSelected", true);

                if (optionsBodyCanvasGroup != null)
                    optionsBodyCanvasGroup.alpha = 1f;

                if (preserveScroll)
                {
                    if (scrollParent != null)
                        screen.StartCoroutine(RestoreScrollAfterLayout(scrollParent, optionsContainer, savedScrollAnchoredPosition, savedScrollNormalizedPosition));
                    else
                        optionsContainer.anchoredPosition = savedScrollAnchoredPosition;
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SULFUR Native UI failed to show custom options page '" + pageId + "': " + ex);

                // Leave the screen navigable: no row selection, focus on the category menu.
                SetPrivate(screen, "options", options);
                SetPrivate(screen, "currentOption", null);
                SetPrivate(screen, "selectedIndex", 0);
                SetPrivate(screen, "menuSelected", true);

                if (optionsBodyCanvasGroup != null)
                    optionsBodyCanvasGroup.alpha = 1f;
            }
        }

        // Runs the page's BuildPage with a new context, registered in state before the build
        // so that a Rebuild() from inside BuildPage invalidates it. When the build throws, or
        // the page is no longer registered, the partial output is discarded and the page shows
        // one error row instead; state.Context is then a fresh context owning only that row,
        // so a mod still holding the failed context cannot add to the error page.
        private static void RunPageBuild(
            OptionsScreen screen,
            CustomPageState state,
            RectTransform optionsContainer,
            SulfurOptionsPage page)
        {
            string pageId = state.PageId;
            List<OptionsScreenOption> options = state.Options;

            SulfurOptionsContext context = new SulfurOptionsContext(screen, optionsContainer, options, pageId);
            state.Context = context;

            if (page != null)
            {
                try
                {
                    page.BuildPage(context);
                    return;
                }
                catch (Exception ex)
                {
                    LogBuildFailure(pageId, ex);
                }
            }
            else if (Plugin.Log != null)
            {
                Plugin.Log.LogWarning("SULFUR Native UI: options page '" + pageId + "' is no longer registered; showing an error row.");
            }

            // A Rebuild() inside the failed build already replaced this page; its rows are not
            // ours to discard.
            if (!context.IsAlive)
                return;

            context.Invalidate();
            options.Clear();
            DestroyChildrenNow(optionsContainer);
            ClearCustomPageFooter(screen);

            SulfurOptionsContext errorContext = new SulfurOptionsContext(screen, optionsContainer, options, pageId);
            state.Context = errorContext;
            errorContext.AddInfo(SulfurUiText.PageLoadFailed);
        }

        // Full exception the first time a page fails, one line on repeats (every open of the
        // page retries the build).
        private static void LogBuildFailure(string pageId, Exception ex)
        {
            if (Plugin.Log == null)
                return;

            if (loggedBuildFailures.Add(pageId ?? ""))
                Plugin.Log.LogError("SULFUR Native UI: BuildPage of options page '" + pageId + "' threw; showing an error row instead: " + ex);
            else
                Plugin.Log.LogError("SULFUR Native UI: BuildPage of options page '" + pageId + "' threw again: " + ex.GetType().Name + ": " + ex.Message);
        }

        // Selects the selectable row nearest to start (keyboard/gamepad focus on the page), or
        // puts the focus back on the category menu when the page has none. Mirrors the field
        // writes OptionsScreen.SelectIndex makes.
        private static void SelectRowNear(OptionsScreen screen, List<OptionsScreenOption> options, int start)
        {
            int index = FindSelectableIndex(options, start, 0);
            if (index < 0)
            {
                ReturnSelectionToMenu(screen, options);
                return;
            }

            foreach (OptionsScreenOption option in options)
            {
                if (option != null)
                    option.SetSelected(false);
            }

            SetPrivate(screen, "menuSelected", false);
            SetPrivate(screen, "selectedIndex", index);
            SetPrivate(screen, "currentOption", options[index]);
            options[index].SetSelected(true);
        }

        private static void ReturnSelectionToMenu(OptionsScreen screen, List<OptionsScreenOption> options)
        {
            if (options != null)
            {
                foreach (OptionsScreenOption option in options)
                {
                    if (option != null)
                        option.SetSelected(false);
                }
            }

            SetPrivate(screen, "menuSelected", true);
            SetPrivate(screen, "currentOption", null);
            SetPrivate(screen, "selectedIndex", 0);
        }

        // SelectIndex prefix (returns whether the game's SelectIndex should run). The game's
        // loop `while (!options[index].gameObject.activeSelf) index += step;` runs past the end
        // of the list, spins forever for step 0 (Enter on a category whose first row is
        // hidden), and still selects rows hidden through an ancestor (a list section, a
        // setting handle, a group). On a custom page the index is moved to the nearest row
        // that is active in the hierarchy first, so the game's loop stops at once and its own
        // field writes and auto-scroll run unchanged. Native pages are not touched.
        public static bool PrepareSelectIndex(OptionsScreen screen, ref int index)
        {
            CustomPageState state;
            if (!TryGetCustomPage(screen, out state))
                return true;

            List<OptionsScreenOption> options = GetPrivate<List<OptionsScreenOption>>(screen, "options");
            if (options == null || !ReferenceEquals(options, state.Options))
                return true;

            int selectedIndex = GetPrivate<int>(screen, "selectedIndex");
            int direction = Math.Sign(index - selectedIndex);

            OptionsScreenOption current = GetPrivate<OptionsScreenOption>(screen, "currentOption");
            bool currentValid = IsSelectableRow(current) && options.Contains(current);

            int target = FindSelectableIndex(options, index, direction);

            // Nothing further in that direction and the current row is gone/hidden too: take
            // the nearest selectable row anywhere instead of leaving a hidden row selected.
            if (target < 0 && direction != 0 && !currentValid)
                target = FindSelectableIndex(options, index, 0);

            if (target >= 0)
            {
                index = target;
                return true;
            }

            // End of the list: stay on the current row.
            if (currentValid)
                return false;

            // No selectable row at all: back to the category menu, so NavigateCancel (which
            // calls currentOption.SetSelected) and NavigateUse see a consistent state.
            ReturnSelectionToMenu(screen, options);
            return false;
        }

        // Only swallows vertical navigation while a library text input is focused (arrow keys
        // move the caret). Everything else, including category-menu navigation, is the game's
        // NavigateVertical: it honours the rebind overlay/rebind list and reaches custom
        // categories through MenuSelectIndex -> SetCategory -> TryShowCustomPage.
        public static bool TryNavigateVertical(OptionsScreen screen, int delta)
        {
            if (screen == null)
                return false;

            return SulfurTextInputBinding.IsAnyInputFocused;
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

                RegisterFooterNavigation(screen, state);
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

            UnregisterFooterNavigation(screen, state);
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

            // Size/colour base: the screen header (see FindFooterStyleSample). Font is bound
            // by role.
            TextMeshProUGUI sample = FindFooterStyleSample(screen);

            // The layout group shares a shortage in proportion to (preferred - min), so the
            // status text must not state a huge preferred width: it takes what is left through
            // flexibleWidth. A negative preferred width lets the left text report its own.
            TextMeshProUGUI left = CreateFooterText(root.transform, "LeftText", sample, TextAlignmentOptions.MidlineLeft, 360f, -1f, 0f);
            TextMeshProUGUI status = CreateFooterText(root.transform, "StatusText", sample, TextAlignmentOptions.MidlineLeft, 260f, 260f, 1f);

            LayoutElement statusLayout = status.gameObject.GetComponent<LayoutElement>();
            statusLayout.flexibleWidth = 1f;

            Button primaryButton = CreateFooterButton(root.transform, SulfurUiText.FooterApply, sample);

            FooterState state = new FooterState();
            state.Root = root;
            state.ScrollRect = scrollRect;
            state.OriginalScrollOffsetMin = originalOffsetMin;
            state.LeftText = left;
            state.StatusText = status;
            state.PrimaryButton = primaryButton;

            // Taken before the navigation proxy (also text-bearing) is added under the button.
            state.PrimaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);

            return state;
        }

        // The footer's size/colour base: the screen header (the large "Options" title,
        // OptionsScreen.optionsHeader). Up to 0.10.1 the footer used the first text under the
        // screen (GetComponentInChildren), in practice that header; the option-prefab sample
        // is much smaller and shrank the footer. Reading the game's serialized field does not
        // depend on hierarchy order; the old lookup remains the fallback.
        private static TextMeshProUGUI FindFooterStyleSample(OptionsScreen screen)
        {
            TextMeshProUGUI header = GetPrivate<TextMeshProUGUI>(screen, "optionsHeader");
            if (header != null)
                return header;

            return screen != null ? screen.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        }

        // Keyboard/gamepad reach for the footer's primary button: a navigation proxy on the
        // button, kept as the last entry of the shown custom page's options list (rows added
        // later are inserted before it, see SulfurOptionsContext.FindNavigationInsertIndex:
        // the proxy is not under the page root). Removed again in ClearCustomPageFooter.
        private static void RegisterFooterNavigation(OptionsScreen screen, FooterState footer)
        {
            if (footer == null || footer.PrimaryButton == null)
                return;

            CustomPageState page;
            if (!TryGetCustomPage(screen, out page) || page.Options == null)
                return;

            if (footer.NavigationProxy == null)
            {
                Button button = footer.PrimaryButton;
                footer.NavigationProxy = SulfurNavigationProxy.Create(
                    screen,
                    button.GetComponent<RectTransform>(),
                    delegate { SulfurNavigationProxy.Press(button); });

                if (footer.NavigationProxy == null)
                    return;
            }

            if (ReferenceEquals(footer.NavigationList, page.Options))
                return;

            UnregisterFooterNavigation(screen, footer);

            page.Options.Add(footer.NavigationProxy);
            footer.NavigationList = page.Options;
        }

        private static void UnregisterFooterNavigation(OptionsScreen screen, FooterState footer)
        {
            if (footer == null || footer.NavigationList == null)
                return;

            List<OptionsScreenOption> list = footer.NavigationList;
            footer.NavigationList = null;

            // The game indexes the list directly and the proxy is destroyed with the footer:
            // take it out, and move the selection off it when it was selected.
            int previousSelectedIndex = GetSelectedOptionIndex(screen, list);
            list.RemoveAll(delegate (OptionsScreenOption option)
            {
                return option == null || ReferenceEquals(option, footer.NavigationProxy);
            });

            if (previousSelectedIndex >= 0)
                ReconcileOptionSelection(screen, list, previousSelectedIndex);
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

            // Font by role (the footer can outlive a language change); the sample only
            // supplies size and colour.
            SulfurFonts.Bind(text, SulfurOptionsContext.OptionsFontRole);

            if (sample != null)
            {
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

            SulfurFonts.Bind(label, SulfurOptionsContext.OptionsFontRole);

            if (sample != null)
            {
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
            public TextMeshProUGUI PrimaryLabel;

            // Keyboard/gamepad stand-in for PrimaryButton and the options list it is in.
            public OptionsScreenOption NavigationProxy;
            public List<OptionsScreenOption> NavigationList;

            public void SetText(string leftText, string statusText, string primaryButtonText)
            {
                if (LeftText != null)
                    LeftText.text = leftText ?? "";

                if (StatusText != null)
                    StatusText.text = statusText ?? "";

                if (PrimaryLabel != null)
                    PrimaryLabel.text = string.IsNullOrWhiteSpace(primaryButtonText) ? SulfurUiText.FooterApply : primaryButtonText;
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

        // The first text (with a font) on one of the option prefabs. The prefab is never
        // restyled, so its size and colour are the true native option-text base; scanning
        // live objects instead returns whatever happens to be found first. Font is not
        // taken from here: the prefab keeps the default font asset (see SulfurFonts).
        internal static TextMeshProUGUI FindOptionPrefabSampleText(OptionsScreen screen)
        {
            if (screen == null)
                return null;

            GameObject[] prefabs =
            {
                GetOptionButtonPrefab(screen),
                GetOptionInfoPrefab(screen),
                GetOptionBoolPrefab(screen),
                GetOptionAlternativePrefab(screen)
            };

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI text in texts)
                {
                    if (text != null && text.font != null)
                        return text;
                }
            }

            return null;
        }

        // selectedIndex of the screen, or -1 when the screen is not currently navigating
        // this options list (another page or a native category replaced it).
        internal static int GetSelectedOptionIndex(OptionsScreen screen, List<OptionsScreenOption> options)
        {
            if (!IsActiveOptionsList(screen, options))
                return -1;

            return GetPrivate<int>(screen, "selectedIndex");
        }

        // Re-points the game's selection after rows were removed from / inserted into the
        // active navigation list (a SulfurListHandle refresh). OptionsScreen.SelectIndex
        // reads options[index].gameObject and NavigateCancel calls
        // currentOption.SetSelected, so neither selectedIndex nor currentOption may be left
        // on a removed row. A surviving selection keeps its row (only its index moves); a
        // removed one moves to the row now at the previous index, so a refreshed list keeps
        // the cursor in place. Mirrors the field writes BuildCustomPage makes.
        internal static void ReconcileOptionSelection(
            OptionsScreen screen,
            List<OptionsScreenOption> options,
            int previousSelectedIndex)
        {
            if (!IsActiveOptionsList(screen, options))
                return;

            OptionsScreenOption current = GetPrivate<OptionsScreenOption>(screen, "currentOption");
            bool hadSelection = !ReferenceEquals(current, null);

            if (current != null)
            {
                int currentIndex = options.IndexOf(current);
                if (currentIndex >= 0)
                {
                    SetPrivate(screen, "selectedIndex", currentIndex);
                    return;
                }
            }

            if (!hadSelection)
            {
                // Nothing was selected (category menu focus, or the page is still being
                // built): only keep the index inside the list for NavigateVertical.
                int clamped = Mathf.Clamp(previousSelectedIndex, 0, Mathf.Max(0, options.Count - 1));
                SetPrivate(screen, "selectedIndex", clamped);
                return;
            }

            // The selected row was removed (or destroyed).
            if (current != null)
                current.SetSelected(false);

            bool menuSelected = GetPrivate<bool>(screen, "menuSelected");

            if (options.Count == 0)
            {
                SetPrivate(screen, "selectedIndex", 0);
                SetPrivate(screen, "currentOption", null);
                SetPrivate(screen, "menuSelected", true);
                return;
            }

            int start = Mathf.Clamp(previousSelectedIndex, 0, options.Count - 1);
            int index = FindSelectableIndex(options, start, 0);
            if (index < 0)
                index = start;

            if (menuSelected)
            {
                // Focus is on the category menu; the game selects a row itself on Use.
                SetPrivate(screen, "selectedIndex", index);
                SetPrivate(screen, "currentOption", null);
                return;
            }

            SetPrivate(screen, "selectedIndex", index);
            SetPrivate(screen, "currentOption", options[index]);
            options[index].SetSelected(true);
        }

        private static bool IsActiveOptionsList(OptionsScreen screen, List<OptionsScreenOption> options)
        {
            if (screen == null || options == null)
                return false;

            return ReferenceEquals(GetPrivate<List<OptionsScreenOption>>(screen, "options"), options);
        }

        private static bool HasRowUnder(List<OptionsScreenOption> options, Transform container)
        {
            if (options == null || container == null)
                return false;

            foreach (OptionsScreenOption option in options)
            {
                if (option != null && option.transform.IsChildOf(container))
                    return true;
            }

            return false;
        }

        // OptionsScreen.DoAutoScrolling prefix (returns whether the game's own should run).
        // The game measures a row by |offsetMax.y|, which is its distance from the top of the
        // scroll content only for a direct child of the content. On a custom page many
        // selectable rows are nested: navigation proxies inside library rows, native rows in
        // themed groups and lists. For those the same scroll rule is applied here with the
        // row's real distance from the content top; direct children, native pages and the
        // rebind lists run the game's code unchanged. The footer's proxy is outside the
        // scroll content and never scrolls it.
        public static bool PrepareAutoScroll(OptionsScreen screen, int indexChange, RectTransform row)
        {
            if (screen == null || row == null)
                return true;

            CustomPageState state;
            if (!TryGetCustomPage(screen, out state))
                return true;

            if (!ReferenceEquals(GetPrivate<List<OptionsScreenOption>>(screen, "options"), state.Options) ||
                GetPrivate<bool>(screen, "showingRebind"))
                return true;

            UIScrollRect scrollParent = GetPrivate<UIScrollRect>(screen, "scrollParent");
            RectTransform optionsContainer = GetPrivate<RectTransform>(screen, "optionsContainer");
            if (scrollParent == null || optionsContainer == null || scrollParent.content != optionsContainer)
                return true;

            RectTransform content = optionsContainer;

            if (row.parent == content)
                return true;

            if (!row.IsChildOf(content))
                return false;

            RectTransform scrollRect = GetPrivate<RectTransform>(screen, "scrollRect");
            if (scrollRect == null || !AutoScrollMembersAvailable())
                return true;

            try
            {
                Vector3[] corners = autoScrollCorners;
                row.GetWorldCorners(corners);
                float rowTop = content.InverseTransformPoint(corners[1]).y;
                float distanceFromTop = content.rect.yMax - rowTop;

                // From here on the game's DoAutoScrolling, with distanceFromTop in place of
                // Mathf.Abs(currentOption.offsetMax.y).
                float margin = GetPrivate<float>(screen, "scrollMargin");
                float viewport = Mathf.Abs(scrollRect.rect.y);
                float onScreen = distanceFromTop - content.anchoredPosition.y;

                float target;
                if (indexChange > 0 && onScreen > viewport - margin)
                    target = distanceFromTop - (viewport - margin);
                else if (indexChange < 0 && onScreen < margin)
                    target = distanceFromTop - margin;
                else
                    return false;

                if (GetPrivate<float>(screen, "currentScrollTarget") == target)
                    return false;

                Coroutine running = GetPrivate<Coroutine>(screen, "scrollRoutine");
                if (running != null)
                    screen.StopCoroutine(running);

                SetPrivate(screen, "currentScrollTarget", target);

                IEnumerator routine = InvokePrivate(screen, "ScrollContent", target, content) as IEnumerator;
                SetPrivate(screen, "scrollRoutine", routine != null ? screen.StartCoroutine(routine) : null);
            }
            catch (Exception ex)
            {
                if (!autoScrollFailureLogged && Plugin.Log != null)
                {
                    autoScrollFailureLogged = true;
                    Plugin.Log.LogWarning("SULFUR Native UI: auto-scroll for a nested options row failed; the game's own is used: " + ex);
                }

                return true;
            }

            return false;
        }

        private static readonly Vector3[] autoScrollCorners = new Vector3[4];
        private static bool autoScrollFailureLogged;
        private static int autoScrollMembersState; // 0 unknown, 1 available, -1 missing

        // The replacement writes the game's private scroll state; if any of it is gone after a
        // game update, keep the game's own (mis-measured but working) auto-scroll instead.
        private static bool AutoScrollMembersAvailable()
        {
            if (autoScrollMembersState == 0)
            {
                Type type = typeof(OptionsScreen);
                bool available =
                    AccessTools.Field(type, "scrollMargin") != null &&
                    AccessTools.Field(type, "currentScrollTarget") != null &&
                    AccessTools.Field(type, "scrollRoutine") != null &&
                    AccessTools.Method(type, "ScrollContent") != null;

                autoScrollMembersState = available ? 1 : -1;

                if (!available && Plugin.Log != null)
                {
                    Plugin.Log.LogWarning(
                        "SULFUR Native UI: OptionsScreen's auto-scroll members changed; nested rows on custom pages " +
                        "use the game's own auto-scroll.");
                }
            }

            return autoScrollMembersState > 0;
        }

        // A row keyboard/gamepad navigation may land on: it exists and is visible, i.e. active
        // in the hierarchy (hidden by itself or by any ancestor counts as hidden).
        private static bool IsSelectableRow(OptionsScreenOption option)
        {
            return option != null && option.gameObject.activeInHierarchy;
        }

        // direction +1/-1: first selectable row from start (inclusive) that way, -1 if none.
        // direction 0: nearest selectable row at or after start, then before it, -1 if none.
        private static int FindSelectableIndex(List<OptionsScreenOption> options, int start, int direction)
        {
            if (options == null || options.Count == 0)
                return -1;

            if (direction != 0)
            {
                for (int i = start; i >= 0 && i < options.Count; i += direction)
                {
                    if (IsSelectableRow(options[i]))
                        return i;
                }

                return -1;
            }

            start = Mathf.Clamp(start, 0, options.Count - 1);

            for (int i = start; i < options.Count; i++)
            {
                if (IsSelectableRow(options[i]))
                    return i;
            }

            for (int i = start - 1; i >= 0; i--)
            {
                if (IsSelectableRow(options[i]))
                    return i;
            }

            return -1;
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

        // Detach before destroying (as SulfurListHandle.ClearRows does): Destroy is deferred to
        // the end of the frame, and rows still parented would be laid out together with the
        // freshly built page for that frame (about twice the layout work on a large page).
        private static void DestroyChildrenNow(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                    continue;

                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
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
