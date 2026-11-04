using System;
using System.Collections.Generic;
using System.Globalization;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    using UIButton = UnityEngine.UI.Button;
    using UIImage = UnityEngine.UI.Image;
    using UISlider = UnityEngine.UI.Slider;
    using UIToggle = UnityEngine.UI.Toggle;
    using UIText = TMPro.TextMeshProUGUI;

    public sealed class SulfurOptionsContext
    {
        private readonly OptionsScreen optionsScreen;
        private readonly RectTransform rootOptionsContainer;
        private RectTransform currentOptionsContainer;
        private readonly Stack<RectTransform> containerStack = new Stack<RectTransform>();
        private readonly List<OptionsScreenOption> nativeOptions;
        private readonly string pageId;

        // The role the game's own options screen and option rows use.
        internal const SulfurFontRole OptionsFontRole = SulfurFontRole.SerifRegular;

        // Size/colour used when no option prefab is available to sample (matches the
        // defaults the row builders use for a missing sample).
        private const float FallbackSampleFontSize = 22f;

        private TextMeshProUGUI sampleTextCache;
        private bool sampleTextCacheInitialized;
        private float nativeOptionWidthCache = -1f;

        // Set by the options bridge when this context's page stops being the page on screen.
        private bool invalidated;
        private bool deadUseLogged;

        internal SulfurOptionsContext(
            OptionsScreen optionsScreen,
            RectTransform optionsContainer,
            List<OptionsScreenOption> nativeOptions,
            string pageId)
        {
            this.optionsScreen = optionsScreen;
            this.rootOptionsContainer = optionsContainer;
            this.currentOptionsContainer = optionsContainer;
            this.nativeOptions = nativeOptions;
            this.pageId = pageId;
        }

        public OptionsScreen OptionsScreen
        {
            get { return optionsScreen; }
        }

        public RectTransform OptionsContainer
        {
            get { return currentOptionsContainer ?? rootOptionsContainer; }
        }

        public string PageId
        {
            get { return pageId; }
        }

        /// <summary>
        /// True while this context's page is the page on screen. It turns false for good when
        /// the page is rebuilt (<see cref="Rebuild"/>, a foldout), another category is shown,
        /// or the options screen is hidden; every build gets a new context. On a context that
        /// is no longer alive, <see cref="Rebuild"/>, <see cref="SetFooter"/>,
        /// <see cref="SetFooterStatus"/> and <see cref="ClearFooter"/> do nothing, and the
        /// <c>Add*</c> methods add nothing (they return null, or an inert handle where they
        /// return a handle) and log one warning per context.
        /// </summary>
        public bool IsAlive
        {
            get { return !invalidated && optionsScreen != null && rootOptionsContainer != null; }
        }

        internal void Invalidate()
        {
            invalidated = true;
        }

        // Guard for the methods that write rows. The root container is the screen's single
        // options container, shared by every page, so a stale context would otherwise write
        // into whatever page is showing now.
        internal bool EnsureAlive(string operation)
        {
            if (IsAlive)
                return true;

            if (!deadUseLogged)
            {
                deadUseLogged = true;

                if (Plugin.Log != null)
                {
                    Plugin.Log.LogWarning(
                        "SULFUR Native UI: " + operation + " was called on the options context of page '" + pageId +
                        "' after that page was closed or rebuilt; ignored. Use the context passed to the current " +
                        "BuildPage call (see SulfurOptionsContext.IsAlive). Further calls on this context are ignored silently.");
                }
            }

            return false;
        }

        public void Rebuild()
        {
            if (!IsAlive || !optionsScreen.IsShown)
                return;

            // The rebuild keeps the selected row by its index in the navigation list; button
            // rows keep their in-row focus by the same index (see SulfurButtonRowNavigator).
            SulfurButtonRowNavigator.FocusCarry previous = BeginButtonFocusCarry();
            try
            {
                SulfurOptionsScreenBridge.RebuildCurrentCustomPage(optionsScreen);
            }
            finally
            {
                SulfurButtonRowNavigator.EndFocusCarry(previous);
            }
        }

        public void SetFooter(
            string leftText,
            string statusText,
            string primaryButtonText,
            Action onPrimaryPressed)
        {
            if (!IsAlive)
                return;

            SulfurOptionsScreenBridge.SetCustomPageFooter(
                optionsScreen,
                leftText,
                statusText,
                primaryButtonText,
                onPrimaryPressed);
        }

        public void SetFooterStatus(string statusText)
        {
            if (!IsAlive)
                return;

            SulfurOptionsScreenBridge.SetCustomPageFooterStatus(optionsScreen, statusText);
        }

        public IDisposable BeginThemedGroup(string name)
        {
            TextMeshProUGUI sample = FindSampleText();
            Color color = sample != null
                ? sample.color
                : new Color(1f, 0.65f, 0.15f, 1f);

            return BeginThemedGroup(name, color, 32f);
        }

        public IDisposable BeginThemedGroup(string name, Color themeColor, float indentPixels)
        {
            if (!EnsureAlive("BeginThemedGroup"))
                return new SulfurContainerScope(this, null, false);

            RectTransform parent = OptionsContainer;
            if (parent == null)
                return new SulfurContainerScope(this, null, false);

            float width = Mathf.Max(360f, GetNativeOptionWidth() - indentPixels + 24f);

            GameObject group = new GameObject(
                string.IsNullOrWhiteSpace(name) ? "SULFUR_ThemedGroup" : name,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            group.transform.SetParent(parent, false);

            RectTransform rt = group.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(indentPixels, 0f);
            rt.sizeDelta = new Vector2(width, 0f);

            LayoutElement groupLayout = group.GetComponent<LayoutElement>();
            groupLayout.minWidth = width;
            groupLayout.preferredWidth = width;
            groupLayout.flexibleWidth = 0f;

            // 背景只给极淡透明，不要让整个区域变黄
            Image image = group.GetComponent<Image>();
            image.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.004f);
            image.raycastTarget = false;

            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 14, 10, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = group.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Color borderColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);

            GameObject overlay = CreateBorderOverlay(group.transform, borderColor);
            overlay.name = "BorderOverlay";

            containerStack.Push(currentOptionsContainer);
            currentOptionsContainer = rt;

            return new SulfurContainerScope(this, rt, true);
        }

        private void EndThemedGroup()
        {
            PopContainer();
        }

        // Redirect subsequent Add* calls into a child container (e.g. a list's own
        // RectTransform), then restore. Wraps the same stack logic used by
        // BeginThemedGroup/EndThemedGroup so SulfurListHandle can rebuild its rows
        // through the normal Add* API.
        internal void PushContainer(RectTransform container)
        {
            containerStack.Push(currentOptionsContainer);
            currentOptionsContainer = container;
        }

        internal void PopContainer()
        {
            if (containerStack.Count > 0)
                currentOptionsContainer = containerStack.Pop();
            else
                currentOptionsContainer = rootOptionsContainer;
        }

        private static void CreateBorderLine(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject line = new GameObject(
                name,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(Image));

            line.transform.SetParent(parent, false);

            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            LayoutElement layout = line.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            Image image = line.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static GameObject CreateBorderOverlay(Transform parent, Color borderColor)
        {
            GameObject overlay = new GameObject(
                "BorderOverlay",
                typeof(RectTransform),
                typeof(LayoutElement));

            overlay.transform.SetParent(parent, false);

            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            LayoutElement overlayLayout = overlay.GetComponent<LayoutElement>();
            overlayLayout.ignoreLayout = true;

            CreateBorderLine(overlay.transform, "BorderLeft", borderColor,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(2f, 0f), new Vector2(6f, 0f));

            CreateBorderLine(overlay.transform, "BorderRight", borderColor,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-6f, 0f), new Vector2(-2f, 0f));

            CreateBorderLine(overlay.transform, "BorderTop", borderColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(2f, -3f), new Vector2(-2f, -1f));

            CreateBorderLine(overlay.transform, "BorderBottom", borderColor,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(2f, 1f), new Vector2(-2f, 3f));

            return overlay;
        }

        private sealed class SulfurContainerScope : IDisposable
        {
            private readonly SulfurOptionsContext context;
            private readonly RectTransform groupRoot;
            private readonly bool active;
            private bool disposed;

            public SulfurContainerScope(SulfurOptionsContext context, RectTransform groupRoot, bool active)
            {
                this.context = context;
                this.groupRoot = groupRoot;
                this.active = active;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                BringBordersToFront(groupRoot);

                if (active && context != null)
                    context.EndThemedGroup();
            }

            private static void BringBordersToFront(RectTransform root)
            {
                if (root == null)
                    return;

                MoveLast(root, "BorderOverlay");
            }

            private static void MoveLast(Transform root, string childName)
            {
                Transform child = root.Find(childName);
                if (child != null)
                    child.SetAsLastSibling();
            }
        }

        public void ClearFooter()
        {
            if (!IsAlive)
                return;

            SulfurOptionsScreenBridge.ClearCustomPageFooter(optionsScreen);
        }

        public OptionsScreenOption AddButton(string label, Action onPressed)
        {
            if (!EnsureAlive("AddButton"))
                return null;

            OptionsScreenOption option = InstantiateButtonOption();

            option.SetLabel(label ?? "");

            SulfurOptionBinding binding = option.gameObject.AddComponent<SulfurOptionBinding>();
            binding.OnUse = delegate
            {
                if (onPressed != null)
                    onPressed();
            };

            option.onButtonPressed = binding.InvokeUse;

            RegisterOption(option);
            return option;
        }

        public OptionsScreenOption AddButton(string label, string description, Action onPressed)
        {
            OptionsScreenOption option = AddButton(label, onPressed);
            AddDescription(description);
            return option;
        }

        public OptionsScreenOption AddInfo(string label)
        {
            if (!EnsureAlive("AddInfo"))
                return null;

            return AddInfoRow(label, true);
        }

        // register: false for a label that belongs to a widget below it (a stacked input),
        // whose own navigation proxy is the row keyboard/gamepad focus lands on.
        private OptionsScreenOption AddInfoRow(string label, bool register)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionInfoPrefab(optionsScreen);
            OptionsScreenOption option = prefab != null
                ? InstantiateOption(prefab)
                : InstantiateButtonOption();
            option.SetLabel(label ?? "");

            if (register)
                RegisterOption(option);

            return option;
        }

        public OptionsScreenOption AddToggle(string label, bool value, Action<bool> onChanged)
        {
            if (!EnsureAlive("AddToggle"))
                return null;

            UIToggle toggle;
            SulfurOptionBinding binding;
            return BuildToggleOption(label, value, onChanged, out toggle, out binding);
        }

        public OptionsScreenOption AddToggle(string label, string description, bool value, Action<bool> onChanged)
        {
            OptionsScreenOption option = AddToggle(label, value, onChanged);
            AddDescription(description);
            return option;
        }

        // AddToggle with a description that also hands out the checkbox and its binding
        // (setting-row handles set the value later).
        internal OptionsScreenOption AddToggleCore(
            string label,
            string description,
            bool value,
            Action<bool> onChanged,
            out UIToggle toggle,
            out SulfurOptionBinding binding)
        {
            toggle = null;
            binding = null;

            if (!EnsureAlive("AddToggle"))
                return null;

            OptionsScreenOption option = BuildToggleOption(label, value, onChanged, out toggle, out binding);
            AddDescription(description);
            return option;
        }

        /// <summary>
        /// Adds a native checkbox row, like <see cref="AddToggle(string, bool, Action{bool})"/>,
        /// and returns a live handle: read the value, set it without raising
        /// <paramref name="onChanged"/>, lock, hide or relabel the row in place.
        /// </summary>
        public SulfurToggleHandle AddToggleRow(string label, bool value, Action<bool> onChanged)
        {
            return AddToggleRow(label, null, value, onChanged);
        }

        /// <summary>As <see cref="AddToggleRow(string, bool, Action{bool})"/>, with a description row below.</summary>
        public SulfurToggleHandle AddToggleRow(string label, string description, bool value, Action<bool> onChanged)
        {
            if (!EnsureAlive("AddToggleRow"))
                return new SulfurToggleHandle(null, null);

            UIToggle toggle;
            SulfurOptionBinding binding;
            OptionsScreenOption option = BuildToggleOption(label, value, onChanged, out toggle, out binding);
            GameObject descriptionRow = AddDescriptionRow(description);

            return new SulfurToggleHandle(new SulfurOptionRowParts(this, option, binding, descriptionRow), toggle);
        }

        private OptionsScreenOption BuildToggleOption(
            string label,
            bool value,
            Action<bool> onChanged,
            out UIToggle toggle,
            out SulfurOptionBinding binding)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionBoolPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

            option.SetLabel(label ?? "");

            // The binding owns the listener so it survives the row being hidden and shown.
            binding = option.gameObject.AddComponent<SulfurOptionBinding>();

            toggle = option.GetComponentInChildren<UIToggle>(true);
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
                toggle.onValueChanged.RemoveAllListeners();

                binding.BindToggle(toggle, new UnityAction<bool>(delegate (bool state)
                {
                    if (onChanged != null)
                        onChanged(state);
                }));
            }

            option.onButtonPressed = null;

            RegisterOption(option);
            return option;
        }

        public OptionsScreenOption AddCycle(
            string label,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            if (!EnsureAlive("AddCycle"))
                return null;

            SulfurOptionBinding binding;
            return BuildCycleOption(label, values, currentIndex, onChanged, out binding);
        }

        /// <summary>
        /// Adds a native alternative ("&lt; value &gt;") row, like
        /// <see cref="AddCycle(string, IReadOnlyList{string}, int, Action{int, string})"/>, and
        /// returns a live handle.
        /// </summary>
        public SulfurCycleHandle AddCycleRow(
            string label,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            return AddCycleRow(label, null, values, currentIndex, onChanged);
        }

        /// <summary>As <see cref="AddCycleRow(string, IReadOnlyList{string}, int, Action{int, string})"/>, with a description row below.</summary>
        public SulfurCycleHandle AddCycleRow(
            string label,
            string description,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            if (!EnsureAlive("AddCycleRow"))
                return new SulfurCycleHandle(null);

            SulfurOptionBinding binding;
            OptionsScreenOption option = BuildCycleOption(label, values, currentIndex, onChanged, out binding);
            GameObject descriptionRow = AddDescriptionRow(description);

            return new SulfurCycleHandle(new SulfurOptionRowParts(this, option, binding, descriptionRow));
        }

        private OptionsScreenOption BuildCycleOption(
            string label,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged,
            out SulfurOptionBinding binding)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionAlternativePrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

            option.SetLabel(label ?? "");

            List<string> safeValues = new List<string>();
            if (values != null)
                safeValues.AddRange(values);

            if (safeValues.Count == 0)
                safeValues.Add("");

            int index = Mathf.Clamp(currentIndex, 0, safeValues.Count - 1);

            UIText altText = SulfurOptionsScreenBridge.GetAlternativeLabelText(option);
            if (altText != null)
                altText.text = safeValues[index];

            binding = option.gameObject.AddComponent<SulfurOptionBinding>();
            binding.CycleValues = safeValues;
            binding.CycleIndex = index;
            binding.CycleLabel = altText;
            binding.OnCycleChanged = onChanged;

            BindCycleButtons(option, binding);

            binding.OnUse = null;
            binding.OnHorizontal = binding.MoveCycle;

            option.onButtonPressed = null;

            RegisterOption(option);
            return option;
        }

        public OptionsScreenOption AddCycle(
            string label,
            string description,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            OptionsScreenOption option = AddCycle(label, values, currentIndex, onChanged);
            AddDescription(description);
            return option;
        }

        // AddCycle with a description that also hands out the row's binding (setting-row
        // handles set the index later).
        internal OptionsScreenOption AddCycleCore(
            string label,
            string description,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged,
            out SulfurOptionBinding binding)
        {
            binding = null;

            if (!EnsureAlive("AddCycle"))
                return null;

            OptionsScreenOption option = BuildCycleOption(label, values, currentIndex, onChanged, out binding);
            AddDescription(description);
            return option;
        }

        public OptionsScreenOption AddSlider(
            string label,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged)
        {
            if (!EnsureAlive("AddSlider"))
                return null;

            UISlider slider;
            UIText valueText;
            SulfurOptionBinding binding;
            return BuildSliderOption(label, value, min, max, step, onChanged, false, out slider, out valueText, out binding);
        }

        public OptionsScreenOption AddSlider(
            string label,
            string description,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged)
        {
            OptionsScreenOption option = AddSlider(label, value, min, max, step, onChanged);
            AddDescription(description);
            return option;
        }

        /// <summary>
        /// Adds a native slider row, like
        /// <see cref="AddSlider(string, float, float, float, float, Action{float})"/>, and
        /// returns a live handle. Unlike <c>AddSlider</c>, mouse drags snap to
        /// <paramref name="step"/> (when it is &gt; 0) the way left/right does, and
        /// <paramref name="onChanged"/> fires only when the snapped value changes.
        /// </summary>
        public SulfurSliderHandle AddSliderRow(
            string label,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged)
        {
            return AddSliderRow(label, null, value, min, max, step, onChanged);
        }

        /// <summary>As <see cref="AddSliderRow(string, float, float, float, float, Action{float})"/>, with a description row below.</summary>
        public SulfurSliderHandle AddSliderRow(
            string label,
            string description,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged)
        {
            if (!EnsureAlive("AddSliderRow"))
                return new SulfurSliderHandle(null, null, null);

            UISlider slider;
            UIText valueText;
            SulfurOptionBinding binding;
            OptionsScreenOption option = BuildSliderOption(label, value, min, max, step, onChanged, true, out slider, out valueText, out binding);
            GameObject descriptionRow = AddDescriptionRow(description);

            return new SulfurSliderHandle(new SulfurOptionRowParts(this, option, binding, descriptionRow), slider, valueText);
        }

        private OptionsScreenOption BuildSliderOption(
            string label,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged,
            bool snapDrags,
            out UISlider slider,
            out UIText valueText,
            out SulfurOptionBinding binding)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionSliderPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

            option.SetLabel(label ?? "");

            UISlider rowSlider = option.GetComponentInChildren<UISlider>(true);
            UIText rowValueText = SulfurOptionsScreenBridge.GetSliderValueText(option);
            slider = rowSlider;
            valueText = rowValueText;

            binding = option.gameObject.AddComponent<SulfurOptionBinding>();

            if (rowSlider != null)
            {
                rowSlider.minValue = min;
                rowSlider.maxValue = max;
                rowSlider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));

                SetSliderText(rowValueText, rowSlider.value);

                rowSlider.onValueChanged.RemoveAllListeners();

                // The binding owns the listener so it survives the row being hidden and shown.
                binding.BindSlider(rowSlider, new UnityAction<float>(delegate (float v)
                {
                    SetSliderText(rowValueText, v);

                    if (onChanged != null)
                        onChanged(v);
                }), snapDrags ? step : 0f);
            }

            binding.OnHorizontal = delegate (int delta)
            {
                if (rowSlider == null)
                    return;

                float actualStep = step > 0f ? step : (max - min) / 20f;
                rowSlider.value = Mathf.Clamp(rowSlider.value + actualStep * delta, min, max);
            };

            RegisterOption(option);
            return option;
        }

        private TMP_InputField CreateStandaloneInputField(
    string name,
    string value,
    string placeholder,
    TMP_InputField.ContentType contentType)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 48f);

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 48f;
            layoutElement.minWidth = GetNativeOptionWidth();
            layoutElement.preferredWidth = GetNativeOptionWidth();

            Image background = row.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.055f);
            background.raycastTarget = true;

            TMP_InputField input = row.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            input.shouldActivateOnSelect = true;
            input.onFocusSelectAll = false;
            input.resetOnDeActivation = false;
            input.restoreOriginalTextOnEscape = false;

            TextMeshProUGUI sampleText = FindSampleText();
            TMP_FontAsset font = sampleText != null ? sampleText.font : null;
            Color textColor = sampleText != null ? sampleText.color : Color.white;
            float fontSize = sampleText != null ? sampleText.fontSize : 22f;

            input.customCaretColor = true;
            input.caretColor = textColor;
            input.caretWidth = 2;
            input.caretBlinkRate = 0.85f;
            input.selectionColor = new Color(textColor.r, textColor.g, textColor.b, 0.25f);

            GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(row.transform, false);

            RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(24f, 0f);
            textAreaRt.offsetMax = new Vector2(-24f, 0f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(textArea.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.text = value ?? "";
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.raycastTarget = false;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.overflowMode = TextOverflowModes.Overflow;

            if (font != null)
                textComponent.font = font;

            GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(textArea.transform, false);

            RectTransform placeholderRt = placeholderObject.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderComponent = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholder ?? "";
            placeholderComponent.fontSize = fontSize;
            placeholderComponent.color = new Color(textColor.r, textColor.g, textColor.b, 0.38f);
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderComponent.raycastTarget = false;
            placeholderComponent.textWrappingMode = TextWrappingModes.NoWrap;
            placeholderComponent.overflowMode = TextOverflowModes.Overflow;

            if (font != null)
                placeholderComponent.font = font;

            input.textViewport = textAreaRt;
            input.textComponent = textComponent;
            input.placeholder = placeholderComponent;
            input.SetTextWithoutNotify(value ?? "");

            // TMP_InputField internal caret may be invisible in this custom native-styled row.
            // Add a small visual overlay caret so focused input fields always show a cursor.
            SulfurInputCaretOverlay.Attach(input, textComponent, textAreaRt, textColor);

            SulfurTextInputBinding inputBinding = input.gameObject.AddComponent<SulfurTextInputBinding>();
            inputBinding.Configure(input);

            SulfurInputVisualState visualState = input.gameObject.AddComponent<SulfurInputVisualState>();
            visualState.Configure(input, background);

            return input;
        }

        private TMP_InputField BuildInlineInputRow(
            string name,
            string label,
            string value,
            string placeholder,
            TMP_InputField.ContentType contentType,
            float labelWidth)
        {
            labelWidth = Mathf.Clamp(labelWidth, 60f, 400f);

            GameObject row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 48f);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 48f;
            rowLayout.preferredHeight = 48f;
            rowLayout.minWidth = GetNativeOptionWidth();
            rowLayout.preferredWidth = GetNativeOptionWidth();

            TextMeshProUGUI sample = FindSampleText();

            // Fixed-width label column on the left so inputs in different rows
            // all start at the same horizontal position.
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(row.transform, false);

            RectTransform labelRt = labelObject.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.sizeDelta = new Vector2(labelWidth, 0f);
            labelRt.anchoredPosition = new Vector2(8f, 0f);

            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.text = label ?? "";
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                labelText.font = sample.font;
                labelText.fontSharedMaterial = sample.fontSharedMaterial;
                labelText.fontSize = Mathf.Max(14f, sample.fontSize * 0.78f);
                labelText.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.85f);
            }
            else
            {
                labelText.fontSize = 16f;
                labelText.color = new Color(1f, 1f, 1f, 0.85f);
            }

            // Input box fills the remaining width, starting just past the label column.
            GameObject box = new GameObject("InputBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            box.transform.SetParent(row.transform, false);

            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0f, 0f);
            boxRt.anchorMax = new Vector2(1f, 1f);
            boxRt.offsetMin = new Vector2(labelWidth + 16f, 6f);
            boxRt.offsetMax = new Vector2(0f, -6f);

            Image background = box.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.055f);
            background.raycastTarget = true;

            TMP_InputField input = box.GetComponent<TMP_InputField>();
            PopulateInputFieldBox(input, background, boxRt, value, placeholder, contentType);

            return input;
        }

        private void PopulateInputFieldBox(
            TMP_InputField input,
            Image background,
            RectTransform boxRt,
            string value,
            string placeholder,
            TMP_InputField.ContentType contentType)
        {
            input.targetGraphic = background;
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            input.shouldActivateOnSelect = true;
            input.onFocusSelectAll = false;
            input.resetOnDeActivation = false;
            input.restoreOriginalTextOnEscape = false;

            TextMeshProUGUI sampleText = FindSampleText();
            TMP_FontAsset font = sampleText != null ? sampleText.font : null;
            Color textColor = sampleText != null ? sampleText.color : Color.white;
            float fontSize = sampleText != null ? sampleText.fontSize : 22f;

            input.customCaretColor = true;
            input.caretColor = textColor;
            input.caretWidth = 2;
            input.caretBlinkRate = 0.85f;
            input.selectionColor = new Color(textColor.r, textColor.g, textColor.b, 0.25f);

            GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(boxRt, false);
            RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(12f, 0f);
            textAreaRt.offsetMax = new Vector2(-12f, 0f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(textArea.transform, false);
            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.text = value ?? "";
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.raycastTarget = false;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                textComponent.font = font;

            GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRt = placeholderObject.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderComponent = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholder ?? "";
            placeholderComponent.fontSize = fontSize;
            placeholderComponent.color = new Color(textColor.r, textColor.g, textColor.b, 0.38f);
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderComponent.raycastTarget = false;
            placeholderComponent.textWrappingMode = TextWrappingModes.NoWrap;
            placeholderComponent.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                placeholderComponent.font = font;

            input.textViewport = textAreaRt;
            input.textComponent = textComponent;
            input.placeholder = placeholderComponent;
            input.SetTextWithoutNotify(value ?? "");

            SulfurInputCaretOverlay.Attach(input, textComponent, textAreaRt, textColor);

            SulfurTextInputBinding inputBinding = input.gameObject.AddComponent<SulfurTextInputBinding>();
            inputBinding.Configure(input);

            SulfurInputVisualState visualState = input.gameObject.AddComponent<SulfurInputVisualState>();
            visualState.Configure(input, background);
        }

        internal TextMeshProUGUI FindSampleText()
        {
            // Make sure non-Latin glyph fallback is in place before any custom row is
            // styled. Idempotent; covers the case where TMP wasn't ready at plugin Awake.
            SulfurFontFallback.EnsureRegistered();

            if (!sampleTextCacheInitialized)
            {
                sampleTextCacheInitialized = true;
                sampleTextCache = ResolveSampleText();
            }

            // Fonts from SulfurFonts already carry the fallback; this also covers the prefab
            // font when no role font is available. The game's Chinese subset font lacks some
            // glyphs (拟, ✓, ○, ...) and TMP's global fallback list is cleared by the game, so
            // the fallback must sit on the font asset itself. Idempotent.
            if (sampleTextCache != null)
                SulfurFontFallback.EnsureFontHasFallback(sampleTextCache.font);

            return sampleTextCache;
        }

        private TextMeshProUGUI ResolveSampleText()
        {
            // Stable base for size/color: sample from an option prefab. The prefab is
            // never restyled, so its font size is always the true native base. Scanning
            // the container for size is unsafe: rows already built have been shrunk by the
            // compact styling, so using them as the base makes fonts shrink on every
            // rebuild. See SulfurOptionsScreenBridge.BuildCustomPage.
            TextMeshProUGUI prefabSample = SulfurOptionsScreenBridge.FindOptionPrefabSampleText(optionsScreen);

            // Font: the option prefab keeps the game's default (Latin) font asset — the
            // game only swaps it per language on instantiated rows, through FontLocalizer.
            // Take the same role font the game gives its option rows, so custom rows match
            // native ones in every language.
            TMP_FontAsset roleFont = SulfurFonts.Get(OptionsFontRole);

            if (roleFont == null || (prefabSample != null && prefabSample.font == roleFont))
                return prefabSample;

            // Combine the prefab's stable size/color with the role font into one detached
            // sample, so every call site keeps reading one component and gets both.
            return BuildStyleSample(prefabSample, roleFont);
        }

        private TextMeshProUGUI BuildStyleSample(TextMeshProUGUI sizeSource, TMP_FontAsset font)
        {
            // Detached, inactive holder: never laid out or rendered, destroyed with the
            // page on the next rebuild. It only carries style values for the call sites.
            // Parented to the page root, not the current container: the sample is cached
            // for the whole page, and a list container would destroy it on its next refresh.
            GameObject go = new GameObject("SulfurStyleSample", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(rootOptionsContainer, false);
            go.SetActive(false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
            tmp.fontSize = sizeSource != null ? sizeSource.fontSize : FallbackSampleFontSize;
            tmp.color = sizeSource != null ? sizeSource.color : Color.white;

            return tmp;
        }

        private void ApplyNativeRowRect(RectTransform rectTransform, float height)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(GetNativeOptionWidth(), height);
        }

        internal float GetNativeOptionWidth()
        {
            if (nativeOptionWidthCache > 100f)
                return nativeOptionWidthCache;

            GameObject prefab = SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen);

            if (prefab != null)
            {
                RectTransform rt = prefab.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.x > 100f)
                {
                    nativeOptionWidthCache = rt.sizeDelta.x;
                    return nativeOptionWidthCache;
                }
            }

            if (OptionsContainer != null)
            {
                RectTransform containerRt = OptionsContainer.GetComponent<RectTransform>();
                if (containerRt != null && containerRt.rect.width > 100f)
                {
                    nativeOptionWidthCache = containerRt.rect.width;
                    return nativeOptionWidthCache;
                }
            }

            nativeOptionWidthCache = 900f;
            return nativeOptionWidthCache;
        }

        public void AddSection(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || !EnsureAlive("AddSection"))
                return;

            GameObject row = new GameObject(
                "SULFUR_Section",
                typeof(RectTransform),
                typeof(LayoutElement));

            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 54f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 54f;
            layout.preferredHeight = 54f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();
            layout.flexibleWidth = 0f;

            GameObject textObject = new GameObject(
                "SectionText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(row.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 0f);
            textRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI sample = FindSampleText();
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();

            text.text = label;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontStyle = FontStyles.Bold;

            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
                text.fontSize = Mathf.Max(18f, sample.fontSize * 0.95f);
                text.color = sample.color;
            }
            else
            {
                text.fontSize = 20f;
                text.color = new Color(1f, 0.65f, 0.15f, 1f);
            }

            GameObject lineObject = new GameObject(
                "SectionLine",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            lineObject.transform.SetParent(row.transform, false);

            RectTransform lineRt = lineObject.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f);
            lineRt.anchorMax = new Vector2(1f, 0f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.offsetMin = new Vector2(0f, 4f);
            lineRt.offsetMax = new Vector2(0f, 8f);

            Image line = lineObject.GetComponent<Image>();

            if (sample != null)
                line.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.85f);
            else
                line.color = new Color(1f, 0.65f, 0.15f, 0.85f);

            line.raycastTarget = false;
        }

        /// <summary>Adds a dimmed, wrapping description row ("— " prefixed).</summary>
        /// <returns>
        /// Always null for now: the row is not a native option row. The return type is kept
        /// for binary compatibility with mods compiled against earlier versions.
        /// </returns>
        public OptionsScreenOption AddDescription(string text)
        {
            AddDescriptionRow(text);
            return null;
        }

        // The description row itself, for handles that show/hide it with their row; null
        // when nothing was added.
        private GameObject AddDescriptionRow(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !EnsureAlive("AddDescription"))
                return null;

            GameObject row = new GameObject("Description", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 42f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            GameObject textObject = new GameObject("DescriptionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(row.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(28f, 0f);
            textRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI sample = FindSampleText();

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = "— " + text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontSize = Mathf.Max(14f, sample.fontSize * 0.72f);
                label.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.58f);
            }
            else
            {
                label.fontSize = 16f;
                label.color = new Color(1f, 1f, 1f, 0.58f);
            }

            // A description that wraps grows the row instead of running into the row below.
            SulfurRowHeightFitter.Attach(row, label, 42f);

            return row;
        }

        // A plain, full-opacity text row that returns a live handle. Unlike
        // AddDescription (dimmed, "— " prefixed, wrapping), this is meant for
        // status lines / values that change while the page is open: keep the
        // handle and call SetText/SetColor/SetVisible without a full Rebuild.
        public SulfurTextHandle AddTextRow(string text)
        {
            if (!EnsureAlive("AddTextRow"))
                return new SulfurTextHandle(null, null);

            GameObject row = new GameObject("SULFUR_TextRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 42f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            GameObject textObject = new GameObject("TextRowText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(row.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(28f, 0f);
            textRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI sample = FindSampleText();
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text ?? "";
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontSize = Mathf.Max(14f, sample.fontSize * 0.72f);
                label.color = sample.color;
            }
            else
            {
                label.fontSize = 16f;
                label.color = Color.white;
            }

            // The row does not wrap; explicit line breaks (also from SetText) grow it.
            SulfurRowHeightFitter.Attach(row, label, 42f);

            return new SulfurTextHandle(row, label);
        }


        public void AddWarning(string text)
        {
            AddMessage(text, SulfurMessageKind.Warning);
        }

        public void AddError(string text)
        {
            AddMessage(text, SulfurMessageKind.Error);
        }

        public void AddSuccess(string text)
        {
            AddMessage(text, SulfurMessageKind.Success);
        }

        public void AddMessage(string text, SulfurMessageKind kind)
        {
            AddMessageRow(text, kind, false);
        }

        // The message row itself. With createWhenEmpty an empty text still builds the row,
        // inactive (layout groups skip it, it has no navigation stop), so a setting-row
        // handle can show a message later; otherwise an empty text adds nothing (null).
        internal GameObject AddMessageRow(string text, SulfurMessageKind kind, bool createWhenEmpty)
        {
            bool empty = string.IsNullOrWhiteSpace(text);
            if ((empty && !createWhenEmpty) || !EnsureAlive("AddMessage"))
                return null;

            GameObject row = new GameObject("SULFUR_Message_" + kind, typeof(RectTransform), typeof(LayoutElement), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 40f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            Image image = row.GetComponent<Image>();
            image.color = GetMessageBackground(kind);
            image.raycastTarget = false;

            GameObject textObject = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(row.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 0f);
            textRt.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI sample = FindSampleText();
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = empty ? "" : GetMessagePrefix(kind) + text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontSize = Mathf.Max(13f, sample.fontSize * 0.66f);
                label.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.9f);
            }
            else
            {
                label.fontSize = 17f;
                label.color = new Color(1f, 1f, 1f, 0.9f);
            }

            // Explicit line breaks grow the row; so does wrapping where a setting row turns it
            // on (compact style) and a later SetMessage. Attached while the row is active.
            SulfurRowHeightFitter.Attach(row, label, 40f);

            if (empty)
                row.SetActive(false);

            return row;
        }

        public void AddBadgeRow(params string[] badges)
        {
            if (badges == null || badges.Length == 0)
                return;

            List<string> clean = new List<string>();
            foreach (string badge in badges)
            {
                if (!string.IsNullOrWhiteSpace(badge))
                    clean.Add(badge.Trim());
            }

            AddBadgeRow((IReadOnlyList<string>)clean);
        }

        public void AddBadgeRow(IReadOnlyList<string> badges)
        {
            AddBadgeRowObject(badges);
        }

        /// <summary>
        /// Adds a badge row like <see cref="AddBadgeRow(string[])"/> and returns a live handle
        /// that replaces the badges in place (<see cref="SulfurBadgeRowHandle.SetBadges(string[])"/>),
        /// without rebuilding the page. Empty and whitespace badges are skipped. With no badge
        /// left the row is still built, hidden (it takes no space), so the handle can show
        /// badges later.
        /// </summary>
        public SulfurBadgeRowHandle AddBadgeRowEx(params string[] badges)
        {
            return AddBadgeRowEx((IReadOnlyList<string>)badges);
        }

        /// <summary>As <see cref="AddBadgeRowEx(string[])"/>.</summary>
        public SulfurBadgeRowHandle AddBadgeRowEx(IReadOnlyList<string> badges)
        {
            if (!EnsureAlive("AddBadgeRowEx"))
                return new SulfurBadgeRowHandle(null, null);

            GameObject row = AddBadgeRowObject(SulfurBadgeRowHandle.CleanBadges(badges), true);
            return new SulfurBadgeRowHandle(this, row);
        }

        // The badge row itself (null when nothing was added), for setting-row handles.
        internal GameObject AddBadgeRowObject(IReadOnlyList<string> badges)
        {
            return AddBadgeRowObject(badges, false);
        }

        // With createWhenEmpty an empty list still builds the row, inactive (layout groups
        // skip it), for a badge-row handle that shows badges later.
        internal GameObject AddBadgeRowObject(IReadOnlyList<string> badges, bool createWhenEmpty)
        {
            bool empty = badges == null || badges.Count == 0;
            if ((empty && !createWhenEmpty) || !EnsureAlive("AddBadgeRow"))
                return null;

            GameObject row = new GameObject("SULFUR_BadgeRow", typeof(RectTransform), typeof(LayoutElement), typeof(SulfurBadgeFlowLayout));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 30f);

            // Width only: the height comes from the flow layout (one line = 30, more when the
            // badges wrap), so the LayoutElement must not pin it.
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            row.GetComponent<SulfurBadgeFlowLayout>().Configure(30f);

            if (!empty)
            {
                foreach (string badge in badges)
                {
                    if (!string.IsNullOrWhiteSpace(badge))
                        CreateBadge(row.transform, badge.Trim());
                }
            }

            if (empty)
                row.SetActive(false);

            return row;
        }

        public void AddDefaultButton(Action onPressed)
        {
            AddSmallButton(SulfurUiText.DefaultButton, onPressed);
        }

        public void AddSmallButton(string label, Action onPressed)
        {
            AddSmallButton(label, onPressed, 0f);
        }

        public void AddSmallButton(string label, Action onPressed, float minWidth)
        {
            AddSmallButtonRow(label, onPressed, minWidth);
        }

        // The small-button row itself (null when nothing was added), for setting-row
        // handles that show/hide their Default button.
        internal GameObject AddSmallButtonRow(string label, Action onPressed, float minWidth)
        {
            if (!EnsureAlive("AddSmallButton"))
                return null;

            string safeLabel = string.IsNullOrWhiteSpace(label) ? SulfurUiText.DefaultButton : label;
            float buttonWidth = minWidth > 0f ? minWidth : CalculateSmallButtonWidth(safeLabel);

            GameObject row = new GameObject("SULFUR_SmallButtonRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 42f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(12, 16, 4, 4);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleRight;
            group.childControlWidth = false;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;

            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(row.transform, false);
            LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1f;

            UIButton button = CreateSmallButton(row.transform, safeLabel, onPressed);
            ApplySmallButtonWidth(button, buttonWidth);

            RegisterNavigationProxy(rowRt, delegate
            {
                SulfurNavigationProxy.Press(button);
            });

            return row;
        }

        // Applies an explicit pixel width to a small button. The button rows use
        // HorizontalLayoutGroup.childControlWidth = false, so the group sizes each child
        // from its RectTransform.sizeDelta and IGNORES LayoutElement.min/preferredWidth.
        // The sizeDelta write below is therefore what actually widens the button (without
        // it the button stays at the 120px sizeDelta baked in CreateSmallButton and long
        // labels ellipsise); the LayoutElement is kept in sync so the width is still
        // honoured if a row is ever switched to control-width mode.
        private static void ApplySmallButtonWidth(UIButton button, float width)
        {
            if (button == null || width <= 0f)
                return;

            LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
            if (buttonLayout != null)
            {
                buttonLayout.minWidth = width;
                buttonLayout.preferredWidth = width;
            }

            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }

        private static float CalculateSmallButtonWidth(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return 96f;

            int length = label.Trim().Length;

            // CJK labels need more width per visible character.
            return Mathf.Clamp(72f + length * 14f, 96f, 180f);
        }

        // Lays several small buttons out left-to-right inside a single row.
        // AddSmallButton keeps its original one-button-per-row, right-aligned
        // behaviour so existing mods are unaffected; use this when you want a
        // compact button group on one line.
        public IReadOnlyList<SulfurButtonHandle> AddButtonRow(params SulfurButton[] buttons)
        {
            return AddButtonRow((IReadOnlyList<SulfurButton>)buttons);
        }

        public IReadOnlyList<SulfurButtonHandle> AddButtonRow(IReadOnlyList<SulfurButton> buttons)
        {
            if (buttons == null || buttons.Count == 0 || !EnsureAlive("AddButtonRow"))
                return Array.Empty<SulfurButtonHandle>();

            List<SulfurButton> valid = new List<SulfurButton>();
            foreach (SulfurButton spec in buttons)
            {
                if (!string.IsNullOrWhiteSpace(spec.Label))
                    valid.Add(spec);
            }

            if (valid.Count == 0)
                return Array.Empty<SulfurButtonHandle>();

            GameObject row = new GameObject("SULFUR_ButtonRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 42f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(12, 16, 4, 4);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = false;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;

            List<SulfurButtonHandle> handles = new List<SulfurButtonHandle>(valid.Count);
            List<UIButton> rowButtons = new List<UIButton>(valid.Count);
            foreach (SulfurButton spec in valid)
            {
                UIButton button = CreateSmallButton(row.transform, spec.Label, spec.OnPressed);
                if (button == null)
                    continue;

                float buttonWidth = spec.MinWidth > 0f ? spec.MinWidth : CalculateSmallButtonWidth(spec.Label);
                ApplySmallButtonWidth(button, buttonWidth);

                TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
                handles.Add(new SulfurButtonHandle(button.gameObject, button, buttonLabel));
                rowButtons.Add(button);
            }

            // One navigation stop for the whole row: left/right picks the button, Use presses it.
            OptionsScreenOption proxy = RegisterNavigationProxy(rowRt, null);
            if (proxy != null)
            {
                SulfurOptionBinding proxyBinding = proxy.GetComponent<SulfurOptionBinding>();
                SulfurButtonRowNavigator navigator = proxy.gameObject.AddComponent<SulfurButtonRowNavigator>();
                navigator.Configure(proxyBinding, rowButtons);
                navigator.RestoreCarriedFocus(pageId, nativeOptions.IndexOf(proxy));

                if (proxyBinding != null)
                {
                    proxyBinding.OnUse = navigator.UseFocused;
                    proxyBinding.OnHorizontal = navigator.Move;
                }
            }

            return handles;
        }

        // Creates an empty, refreshable list section and returns a handle. Call
        // handle.Update(ctx => { ctx.AddTextRow(...); ... }) to (re)populate it in
        // place without rebuilding the whole page. Used for live tables (player +
        // ping + kick button) that change while the page is open.
        public SulfurListHandle AddList()
        {
            if (!EnsureAlive("AddList"))
                return new SulfurListHandle(this, null);

            float width = GetNativeOptionWidth();

            GameObject group = new GameObject(
                "SULFUR_List",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            group.transform.SetParent(OptionsContainer, false);

            RectTransform rt = group.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, 0f);

            LayoutElement groupLayout = group.GetComponent<LayoutElement>();
            groupLayout.minWidth = width;
            groupLayout.preferredWidth = width;
            groupLayout.flexibleWidth = 0f;

            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = group.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return new SulfurListHandle(this, rt);
        }


        public void AddSpacer(float height = 20f)
        {
            if (!EnsureAlive("AddSpacer"))
                return;

            GameObject spacer = new GameObject("SULFUR_Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(OptionsContainer, false);

            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(0f, height);
            layout.preferredHeight = Mathf.Max(0f, height);
            layout.flexibleHeight = 0f;
        }

        /// <summary>Adds a "label: value" description row.</summary>
        /// <returns>
        /// Always null for now (see <see cref="AddDescription"/>). The return type is kept for
        /// binary compatibility with mods compiled against earlier versions.
        /// </returns>
        public OptionsScreenOption AddReadonlyText(string label, string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? (label ?? "") : ((label ?? "") + ": " + value);
            return AddDescription(text);
        }

        public OptionsScreenOption AddTextInput(
            string label,
            string value,
            Action<string> onChanged)
        {
            return AddTextInput(label, null, value, onChanged);
        }

        public OptionsScreenOption AddTextInput(
            string label,
            string description,
            string value,
            Action<string> onChanged)
        {
            TMP_InputField input;
            return AddTextInputCore(label, description, value, onChanged, out input);
        }

        // AddTextInput that also hands out the field (setting-row handles set it later).
        internal OptionsScreenOption AddTextInputCore(
            string label,
            string description,
            string value,
            Action<string> onChanged,
            out TMP_InputField input)
        {
            input = null;

            if (!EnsureAlive("AddTextInput"))
                return null;

            // The label row is not a navigation stop of its own: focus lands on the input below.
            OptionsScreenOption labelOption = AddInfoRow(label, false);
            AddDescription(description);

            TMP_InputField field = CreateStandaloneInputField(
                "TextInput_" + label,
                value ?? "",
                SulfurUiText.TextInputPlaceholder,
                TMP_InputField.ContentType.Standard);

            field.onValueChanged.RemoveAllListeners();
            field.onValueChanged.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onChanged != null)
                    onChanged(text);
            }));

            RegisterInputNavigation(field, field.GetComponent<RectTransform>(), false);

            input = field;
            return labelOption;
        }

        /// <summary>
        /// Adds a stacked number input (label row above an input box) for a float value.
        /// Float holds about 7 significant digits: larger or more precise values are shown
        /// and reported rounded. Use the double overload for those.
        /// </summary>
        public OptionsScreenOption AddNumberInput(
            string label,
            float value,
            float min,
            float max,
            int decimals,
            Action<float> onChanged)
        {
            return AddNumberInput(label, null, value, min, max, decimals, onChanged);
        }

        /// <summary>As <see cref="AddNumberInput(string, float, float, float, int, Action{float})"/>, with a description row below the label.</summary>
        public OptionsScreenOption AddNumberInput(
    string label,
    string description,
    float value,
    float min,
    float max,
    int decimals,
    Action<float> onChanged)
        {
            SulfurNumberInput model;
            return AddNumberInputCore(label, description, value, min, max, decimals, onChanged, null, false, out model);
        }

        /// <summary>
        /// Adds a stacked number input for a double value: parsed, clamped, rounded and
        /// formatted in double with the invariant culture, so values up to 15 significant
        /// digits are shown and reported exactly. Otherwise the same as the float overload.
        /// </summary>
        public OptionsScreenOption AddNumberInput(
            string label,
            double value,
            double min,
            double max,
            int decimals,
            Action<double> onChanged)
        {
            return AddNumberInput(label, null, value, min, max, decimals, onChanged);
        }

        /// <summary>As <see cref="AddNumberInput(string, double, double, double, int, Action{double})"/>, with a description row below the label.</summary>
        public OptionsScreenOption AddNumberInput(
            string label,
            string description,
            double value,
            double min,
            double max,
            int decimals,
            Action<double> onChanged)
        {
            SulfurNumberInput model;
            return AddNumberInputCore(label, description, value, min, max, decimals, null, onChanged, true, out model);
        }

        // Stacked number input of either precision; hands out the value model (setting-row
        // handles set the value later). Exactly one of the callbacks is used.
        internal OptionsScreenOption AddNumberInputCore(
            string label,
            string description,
            double value,
            double min,
            double max,
            int decimals,
            Action<float> onFloatChanged,
            Action<double> onDoubleChanged,
            bool useDouble,
            out SulfurNumberInput model)
        {
            model = null;

            if (!EnsureAlive("AddNumberInput"))
                return null;

            // The label row is not a navigation stop of its own: focus lands on the input below.
            OptionsScreenOption labelOption = AddInfoRow(label, false);
            AddDescription(description);

            TMP_InputField input = CreateStandaloneInputField(
                "NumberInput_" + label,
                useDouble ? SulfurNumberInput.FormatDouble(value, decimals) : SulfurNumberInput.FormatFloat((float)value, decimals),
                SulfurUiText.NumberInputPlaceholder,
                TMP_InputField.ContentType.DecimalNumber);

            // Unparseable input reverts to the last value this field accepted, not to the
            // value the page was built with.
            model = useDouble
                ? SulfurNumberInput.AttachDouble(input, value, min, max, decimals, onDoubleChanged)
                : SulfurNumberInput.AttachFloat(input, (float)value, (float)min, (float)max, decimals, onFloatChanged);

            RegisterInputNavigation(input, input.GetComponent<RectTransform>(), true);

            return labelOption;
        }

        // --- Inline input rows (label and input on the same line) -----------
        // Place the label and the input field on a single row, matching the
        // vanilla Options layout. Pass the same labelWidth to several rows so
        // every input starts at the same horizontal position:
        //
        //     ctx.AddInlineTextInput("Hostname", host, v => host = v);
        //     ctx.AddInlineTextInput("Port",     port, v => port = v);
        //     ctx.AddInlineTextInput("Password", pass, v => pass = v, password: true);
        //
        // These are additive; the stacked AddTextInput / AddNumberInput helpers
        // are unchanged.

        public TMP_InputField AddInlineTextInput(
            string label,
            string value,
            Action<string> onChanged,
            float labelWidth = 160f,
            bool password = false)
        {
            if (!EnsureAlive("AddInlineTextInput"))
                return null;

            TMP_InputField input = BuildInlineInputRow(
                "SULFUR_InlineText_" + (label ?? ""),
                label,
                value ?? "",
                SulfurUiText.TextInputPlaceholder,
                password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard,
                labelWidth);

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onChanged != null)
                    onChanged(text);
            }));

            RegisterInputNavigation(input, input.transform.parent as RectTransform, false);

            return input;
        }

        /// <summary>
        /// Adds an inline number input (label and box on one line) for a float value. Float
        /// holds about 7 significant digits; use the double overload for larger or more
        /// precise values.
        /// </summary>
        /// <remarks>
        /// The returned field's text can be set by the caller, but the value unparseable input
        /// reverts to is the last value the field accepted itself.
        /// </remarks>
        public TMP_InputField AddInlineNumberInput(
            string label,
            float value,
            float min,
            float max,
            int decimals,
            Action<float> onChanged,
            float labelWidth = 160f)
        {
            return AddInlineNumberInputCore(label, value, min, max, decimals, onChanged, null, false, labelWidth);
        }

        /// <summary>
        /// Adds an inline number input for a double value (parsed, rounded and formatted in
        /// double, invariant culture; exact up to 15 significant digits).
        /// </summary>
        public TMP_InputField AddInlineNumberInput(
            string label,
            double value,
            double min,
            double max,
            int decimals,
            Action<double> onChanged,
            float labelWidth = 160f)
        {
            return AddInlineNumberInputCore(label, value, min, max, decimals, null, onChanged, true, labelWidth);
        }

        private TMP_InputField AddInlineNumberInputCore(
            string label,
            double value,
            double min,
            double max,
            int decimals,
            Action<float> onFloatChanged,
            Action<double> onDoubleChanged,
            bool useDouble,
            float labelWidth)
        {
            if (!EnsureAlive("AddInlineNumberInput"))
                return null;

            TMP_InputField input = BuildInlineInputRow(
                "SULFUR_InlineNumber_" + (label ?? ""),
                label,
                useDouble ? SulfurNumberInput.FormatDouble(value, decimals) : SulfurNumberInput.FormatFloat((float)value, decimals),
                SulfurUiText.NumberInputPlaceholder,
                TMP_InputField.ContentType.DecimalNumber,
                labelWidth);

            // Unparseable input reverts to the last value this field accepted, not to the
            // value the page was built with.
            if (useDouble)
                SulfurNumberInput.AttachDouble(input, value, min, max, decimals, onDoubleChanged);
            else
                SulfurNumberInput.AttachFloat(input, (float)value, (float)min, (float)max, decimals, onFloatChanged);

            RegisterInputNavigation(input, input.transform.parent as RectTransform, true);

            return input;
        }

        internal IEnumerable<Transform> GetChildrenFrom(int startIndex)
        {
            if (OptionsContainer == null)
                yield break;

            int start = Mathf.Clamp(startIndex, 0, OptionsContainer.childCount);

            for (int i = start; i < OptionsContainer.childCount; i++)
            {
                Transform child = OptionsContainer.GetChild(i);
                if (child != null)
                    yield return child;
            }
        }

        internal int GetChildCount()
        {
            return OptionsContainer != null ? OptionsContainer.childCount : 0;
        }

        private OptionsScreenOption InstantiateOption(GameObject prefab)
        {
            if (prefab == null)
                throw new InvalidOperationException("Option prefab is null.");

            GameObject go = UnityEngine.Object.Instantiate(prefab, OptionsContainer);
            go.SetActive(true);

            OptionsScreenOption option = go.GetComponent<OptionsScreenOption>();
            if (option == null)
                throw new InvalidOperationException("Instantiated prefab does not contain OptionsScreenOption.");

            return option;
        }

        // The game's button prefab carries the slider value text (placeholder "100%") on a
        // zero-width rect; the game only ever writes it for slider rows. Left alone on a
        // button clone it wraps one character per line into a vertical "1 0 0 %" column,
        // so clear it. Clearing (not deactivating) keeps the row's layout unchanged.
        private OptionsScreenOption InstantiateButtonOption()
        {
            OptionsScreenOption option = InstantiateOption(SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen));

            TextMeshProUGUI valueText = SulfurOptionsScreenBridge.GetSliderValueText(option);
            if (valueText != null)
                valueText.text = "";

            return option;
        }

        // nativeOptions is the game's OptionsScreen.options for this page: the list its
        // keyboard/gamepad navigation walks by index. Rows are inserted at their visual
        // position in the page, not appended, so navigation order matches screen order
        // even when a list section (SulfurListHandle) is refreshed after rows below it
        // were built. A plain top-to-bottom page build still appends (fast path).
        private void RegisterOption(OptionsScreenOption option)
        {
            if (option == null)
                return;

            nativeOptions.Insert(FindNavigationInsertIndex(option.transform), option);
        }

        // Makes a library widget that is not a native option row reachable by keyboard and
        // gamepad: a navigation proxy (see SulfurNavigationProxy) inside host, registered at
        // host's place in the page. Call after host's contents are built. Returns the proxy,
        // or null when it could not be created (the widget then stays mouse-only).
        internal OptionsScreenOption RegisterNavigationProxy(RectTransform host, Action onUse)
        {
            OptionsScreenOption proxy = SulfurNavigationProxy.Create(optionsScreen, host, onUse);
            if (proxy != null)
                RegisterOption(proxy);

            return proxy;
        }

        // Use on the row starts typing in input. Leaving the field (Enter, Escape, a click
        // elsewhere) returns to row navigation with this row still selected: the selection
        // never moved, only the EventSystem focus did. selectAllOnUse: a number field starts
        // with its text selected, so typing replaces the value; a text field starts with the
        // caret at the end. Mouse clicks are not affected (see SulfurTextInputBinding).
        private void RegisterInputNavigation(TMP_InputField input, RectTransform host, bool selectAllOnUse)
        {
            if (input == null || host == null)
                return;

            SulfurTextInputBinding inputBinding = input.GetComponent<SulfurTextInputBinding>();

            RegisterNavigationProxy(host, delegate
            {
                if (input == null || !input.IsActive() || !input.IsInteractable() || input.isFocused)
                    return;

                // The Submit that just ended editing must not start it again.
                if (inputBinding != null && inputBinding.LeftFocusRecently)
                    return;

                if (inputBinding != null)
                    inputBinding.ActivateFromNavigation(selectAllOnUse);
                else
                    input.ActivateInputField();
            });
        }

        // Removes every registered row under container (a list section about to be
        // cleared, including rows of lists nested in it) from the navigation list, plus
        // entries whose row is already destroyed. The game indexes this list directly,
        // so a destroyed entry left behind throws during navigation.
        internal void UnregisterOptionsUnder(Transform container)
        {
            if (container == null)
                return;

            nativeOptions.RemoveAll(delegate (OptionsScreenOption option)
            {
                return option == null || option.transform.IsChildOf(container);
            });
        }

        // Selection bookkeeping around a list refresh; see
        // SulfurOptionsScreenBridge.ReconcileOptionSelection.
        internal int CaptureSelectedOptionIndex()
        {
            return SulfurOptionsScreenBridge.GetSelectedOptionIndex(optionsScreen, nativeOptions);
        }

        internal void ReconcileSelectedOption(int previousSelectedIndex)
        {
            SulfurOptionsScreenBridge.ReconcileOptionSelection(optionsScreen, nativeOptions, previousSelectedIndex);
        }

        // Button-row focus around a rebuild of this page or of one of its lists; pass the
        // result to SulfurButtonRowNavigator.EndFocusCarry when the rebuild returns.
        internal SulfurButtonRowNavigator.FocusCarry BeginButtonFocusCarry()
        {
            return SulfurButtonRowNavigator.BeginFocusCarry(pageId, nativeOptions);
        }

        private readonly List<int> insertPathBuffer = new List<int>();
        private readonly List<int> comparePathBuffer = new List<int>();

        private int FindNavigationInsertIndex(Transform row)
        {
            int count = nativeOptions.Count;
            if (count == 0 || row == null || !TryGetSiblingPath(row, insertPathBuffer))
                return count;

            // Walk back from the end: the first registered row that sits above the new
            // one marks the insert position. Normally the very last entry does.
            for (int i = count - 1; i >= 0; i--)
            {
                OptionsScreenOption existing = nativeOptions[i];
                if (existing == null || !TryGetSiblingPath(existing.transform, comparePathBuffer))
                    continue;

                if (CompareSiblingPaths(comparePathBuffer, insertPathBuffer) <= 0)
                    return i + 1;
            }

            return 0;
        }

        // Sibling indices from the page root down to t. False when t is not under the
        // page root (then the caller falls back to appending).
        private bool TryGetSiblingPath(Transform t, List<int> path)
        {
            path.Clear();

            if (rootOptionsContainer == null)
                return false;

            Transform current = t;
            while (current != null && current != rootOptionsContainer)
            {
                path.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            if (current == null)
                return false;

            path.Reverse();
            return true;
        }

        // Hierarchy (= visual, for these vertical layouts) order; an ancestor sorts first.
        private static int CompareSiblingPaths(List<int> a, List<int> b)
        {
            int n = Mathf.Min(a.Count, b.Count);
            for (int i = 0; i < n; i++)
            {
                if (a[i] != b[i])
                    return a[i] < b[i] ? -1 : 1;
            }

            return a.Count.CompareTo(b.Count);
        }

        private static void BindCycleButtons(OptionsScreenOption option, SulfurOptionBinding binding)
        {
            if (option == null || binding == null)
                return;

            UIButton[] buttons = option.GetComponentsInChildren<UIButton>(true);
            if (buttons == null || buttons.Length == 0)
                return;

            List<UIButton> validButtons = new List<UIButton>();

            foreach (UIButton button in buttons)
            {
                if (button == null)
                    continue;

                validButtons.Add(button);
            }

            if (validButtons.Count == 0)
                return;

            validButtons.Sort(delegate (UIButton a, UIButton b)
            {
                RectTransform ar = a.GetComponent<RectTransform>();
                RectTransform br = b.GetComponent<RectTransform>();

                float ax = ar != null ? ar.anchoredPosition.x : 0f;
                float bx = br != null ? br.anchoredPosition.x : 0f;

                return ax.CompareTo(bx);
            });

            for (int i = 0; i < validButtons.Count; i++)
            {
                UIButton button = validButtons[i];
                int direction = DetectCycleButtonDirection(button, i, validButtons.Count);

                ReplaceButtonClick(button, delegate
                {
                    binding.MoveCycle(direction);
                });
            }
        }

        private static int DetectCycleButtonDirection(UIButton button, int index, int count)
        {
            if (button != null)
            {
                string name = button.gameObject.name.ToLowerInvariant();

                if (name.Contains("left") || name.Contains("prev") || name.Contains("previous") || name.Contains("minus"))
                    return -1;

                if (name.Contains("right") || name.Contains("next") || name.Contains("plus"))
                    return 1;
            }

            if (count <= 1)
                return 1;

            return index == 0 ? -1 : 1;
        }

        private static void ReplaceButtonClick(UIButton button, UnityAction action)
        {
            if (button == null)
                return;

            button.onClick = new UIButton.ButtonClickedEvent();

            if (action != null)
                button.onClick.AddListener(action);
        }


        // One badge of a badge row (also used by SulfurBadgeRowHandle to rebuild a row).
        internal void CreateBadge(Transform parent, string text)
        {
            TextMeshProUGUI sample = FindSampleText();

            GameObject badge = new GameObject(BadgeObjectPrefix + text, typeof(RectTransform), typeof(LayoutElement), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(parent, false);

            Image bg = badge.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);
            bg.raycastTarget = false;

            // Size and position come from the row's SulfurBadgeFlowLayout (width measured from
            // the label's text); the LayoutElement only states the badge's natural size.
            LayoutElement layout = badge.GetComponent<LayoutElement>();
            layout.minWidth = SulfurBadgeFlowLayout.MinBadgeWidth;
            layout.preferredWidth = SulfurBadgeFlowLayout.MinBadgeWidth;
            layout.minHeight = 26f;
            layout.preferredHeight = 26f;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(badge.transform, false);

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f, 0f);
            labelRt.offsetMax = new Vector2(-8f, 0f);

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontSize = Mathf.Max(11f, sample.fontSize * 0.52f);
                label.color = new Color(sample.color.r, sample.color.g, sample.color.b, 0.82f);
            }
            else
            {
                label.fontSize = 14f;
                label.color = new Color(1f, 1f, 1f, 0.82f);
            }
        }

        internal const string BadgeObjectPrefix = "Badge_";

        private UIButton CreateSmallButton(Transform parent, string label, Action onPressed)
        {
            GameObject go = new GameObject(
                "SULFUR_SmallButton",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(UIImage),
                typeof(UIButton));

            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 34f);

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 34f;
            layout.preferredHeight = 34f;
            layout.minWidth = 120f;
            layout.preferredWidth = 120f;

            UIImage image = go.GetComponent<UIImage>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = true;

            UIButton button = go.GetComponent<UIButton>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            // Explicit disabled tint so SetInteractable(false) visibly greys the
            // button background (handle also dims the label).
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;

            button.onClick.RemoveAllListeners();

            if (onPressed != null)
                button.onClick.AddListener(new UnityAction(onPressed));

            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIText));

            textObject.transform.SetParent(go.transform, false);

            RectTransform textRt = textObject.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 0f);
            textRt.offsetMax = new Vector2(-8f, 0f);

            UIText sample = FindSampleText();
            UIText text = textObject.GetComponent<UIText>();

            text.text = label ?? "";
            text.alignment = TextAlignmentOptions.Midline;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
                text.fontSize = Mathf.Max(13f, sample.fontSize * 0.62f);
                text.color = sample.color;
            }
            else
            {
                text.fontSize = 16f;
                text.color = Color.white;
            }

            return button;
        }

        private static Color GetMessageBackground(SulfurMessageKind kind)
        {
            switch (kind)
            {
                case SulfurMessageKind.Warning:
                    return new Color(1f, 0.72f, 0.18f, 0.12f);
                case SulfurMessageKind.Error:
                    return new Color(1f, 0.18f, 0.18f, 0.12f);
                case SulfurMessageKind.Success:
                    return new Color(0.28f, 1f, 0.46f, 0.10f);
                default:
                    return new Color(1f, 1f, 1f, 0.06f);
            }
        }

        private static string GetMessagePrefix(SulfurMessageKind kind)
        {
            return SulfurUiText.GetMessagePrefix(kind);
        }

        internal static void SetSliderText(UIText text, float value)
        {
            if (text != null)
                text.text = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

    }

    // Describes a single button inside an AddButtonRow group.
    public struct SulfurButton
    {
        public string Label;
        public Action OnPressed;
        // Optional fixed width; 0 lets the row size the button from its label.
        public float MinWidth;

        public SulfurButton(string label, Action onPressed)
        {
            Label = label;
            OnPressed = onPressed;
            MinWidth = 0f;
        }

        public SulfurButton(string label, Action onPressed, float minWidth)
        {
            Label = label;
            OnPressed = onPressed;
            MinWidth = minWidth;
        }
    }
}
