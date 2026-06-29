using System;
using System.Collections.Generic;
using System.Globalization;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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

        private TextMeshProUGUI sampleTextCache;
        private bool sampleTextCacheInitialized;
        private float nativeOptionWidthCache = -1f;

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

        public void Rebuild()
        {
            SulfurOptionsScreenBridge.RebuildCurrentCustomPage(optionsScreen);
        }

        public void SetFooter(
            string leftText,
            string statusText,
            string primaryButtonText,
            Action onPrimaryPressed)
        {
            SulfurOptionsScreenBridge.SetCustomPageFooter(
                optionsScreen,
                leftText,
                statusText,
                primaryButtonText,
                onPrimaryPressed);
        }

        public void SetFooterStatus(string statusText)
        {
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

        //public IDisposable BeginThemedGroup(string name, Color themeColor, float indentPixels)
        //{
        //    RectTransform parent = OptionsContainer;
        //    if (parent == null)
        //        return new SulfurContainerScope(this, null, false);

        //    float leftInset = indentPixels;
        //    float rightInset = 6f;

        //    GameObject group = new GameObject(
        //        string.IsNullOrWhiteSpace(name) ? "SULFUR_ThemedGroup" : name,
        //        typeof(RectTransform),
        //        typeof(CanvasRenderer),
        //        typeof(Image),
        //        typeof(VerticalLayoutGroup),
        //        typeof(ContentSizeFitter));

        //    group.transform.SetParent(parent, false);

        //    RectTransform rt = group.GetComponent<RectTransform>();
        //    rt.anchorMin = new Vector2(0f, 1f);
        //    rt.anchorMax = new Vector2(1f, 1f);
        //    rt.pivot = new Vector2(0.5f, 1f);
        //    rt.anchoredPosition = Vector2.zero;
        //    rt.offsetMin = new Vector2(leftInset, 0f);
        //    rt.offsetMax = new Vector2(-rightInset, 0f);

        //    Image image = group.GetComponent<Image>();
        //    image.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.018f);
        //    image.raycastTarget = false;

        //    VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
        //    layout.padding = new RectOffset(18, 14, 10, 12);
        //    layout.spacing = 6f;
        //    layout.childAlignment = TextAnchor.UpperLeft;
        //    layout.childControlWidth = true;
        //    layout.childControlHeight = false;
        //    layout.childForceExpandWidth = true;
        //    layout.childForceExpandHeight = false;

        //    ContentSizeFitter fitter = group.GetComponent<ContentSizeFitter>();
        //    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        //    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        //    Color borderColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.68f);

        //    //CreateBorderLine(group.transform, "BorderLeft", borderColor,
        //    //    new Vector2(0f, 0f), new Vector2(0f, 1f),
        //    //    new Vector2(0f, 0f), new Vector2(2f, 0f));

        //    //CreateBorderLine(group.transform, "BorderRight", borderColor,
        //    //    new Vector2(1f, 0f), new Vector2(1f, 1f),
        //    //    new Vector2(-2f, 0f), new Vector2(0f, 0f));

        //    //CreateBorderLine(group.transform, "BorderTop", borderColor,
        //    //    new Vector2(0f, 1f), new Vector2(1f, 1f),
        //    //    new Vector2(0f, -2f), new Vector2(0f, 0f));

        //    //CreateBorderLine(group.transform, "BorderBottom", borderColor,
        //    //    new Vector2(0f, 0f), new Vector2(1f, 0f),
        //    //    new Vector2(0f, 0f), new Vector2(0f, 2f));

        //    containerStack.Push(currentOptionsContainer);
        //    currentOptionsContainer = rt;

        //    return new SulfurContainerScope(this, rt, true);
        //}

        public IDisposable BeginThemedGroup(string name, Color themeColor, float indentPixels)
        {
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
            SulfurOptionsScreenBridge.ClearCustomPageFooter(optionsScreen);
        }

        public OptionsScreenOption AddButton(string label, Action onPressed)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

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
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionInfoPrefab(optionsScreen);
            if (prefab == null)
                prefab = SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen);

            OptionsScreenOption option = InstantiateOption(prefab);
            option.SetLabel(label ?? "");
            RegisterOption(option);
            return option;
        }

        public OptionsScreenOption AddToggle(string label, bool value, Action<bool> onChanged)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionBoolPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

            option.SetLabel(label ?? "");

            UIToggle toggle = option.GetComponentInChildren<UIToggle>(true);
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(new UnityAction<bool>(delegate (bool state)
                {
                    if (onChanged != null)
                        onChanged(state);
                }));
            }

            option.onButtonPressed = null;

            RegisterOption(option);
            return option;
        }

        public OptionsScreenOption AddToggle(string label, string description, bool value, Action<bool> onChanged)
        {
            OptionsScreenOption option = AddToggle(label, value, onChanged);
            AddDescription(description);
            return option;
        }

        public OptionsScreenOption AddCycle(
            string label,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
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

            SulfurOptionBinding binding = option.gameObject.AddComponent<SulfurOptionBinding>();
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

        public OptionsScreenOption AddSlider(
            string label,
            float value,
            float min,
            float max,
            float step,
            Action<float> onChanged)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionSliderPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);

            option.SetLabel(label ?? "");

            UISlider slider = option.GetComponentInChildren<UISlider>(true);
            UIText valueText = SulfurOptionsScreenBridge.GetSliderValueText(option);

            if (slider != null)
            {
                slider.minValue = min;
                slider.maxValue = max;
                slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));

                SetSliderText(valueText, slider.value);

                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(new UnityAction<float>(delegate (float v)
                {
                    SetSliderText(valueText, v);

                    if (onChanged != null)
                        onChanged(v);
                }));
            }

            SulfurOptionBinding binding = option.gameObject.AddComponent<SulfurOptionBinding>();
            binding.OnHorizontal = delegate (int delta)
            {
                if (slider == null)
                    return;

                float actualStep = step > 0f ? step : (max - min) / 20f;
                slider.value = Mathf.Clamp(slider.value + actualStep * delta, min, max);
            };

            RegisterOption(option);
            return option;
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

            // Attach the CJK fallback to the exact font our custom rows render with. The
            // game's per-language font (e.g. the Chinese Noto subset) is missing some
            // glyphs (拟, ✓, ○, ...) and the global fallback list isn't reliably consulted
            // for it, so wire the fallback onto the font asset itself. Idempotent.
            if (sampleTextCache != null)
                SulfurFontFallback.EnsureFontHasFallback(sampleTextCache.font);

            return sampleTextCache;
        }

        private TextMeshProUGUI ResolveSampleText()
        {
            // Stable base for size/color: sample from an option prefab. The prefab is
            // never restyled, so its font size is always the true native base. Scanning
            // the container for size is unsafe: on Rebuild(), the previous build's custom
            // rows are still present this frame (Object.Destroy is deferred) and have
            // already been shrunk by the compact styling, so using them as the base makes
            // fonts shrink on every rebuild. See SulfurOptionsScreenBridge.BuildCustomPage.
            TextMeshProUGUI prefabSample = FindPrefabSampleText();

            // Language font: the option prefab keeps the game's default (Latin) font
            // asset, so non-English glyphs (CJK, Cyrillic, ...) are missing and custom
            // rows render as blank boxes. The game applies a per-language font asset +
            // material to its own live, localized chrome (category tabs, headers). Sample
            // that so custom rows follow the active language instead of the prefab font.
            TextMeshProUGUI liveLanguageSample = FindLiveLanguageText();

            if (prefabSample == null)
                return liveLanguageSample;

            if (liveLanguageSample == null || liveLanguageSample.font == prefabSample.font)
                return prefabSample;

            // Prefab and live text use different font assets (the game swapped fonts for
            // the current language). Combine the prefab's stable size/color with the live
            // language font/material into a single detached sample, so every call site can
            // keep reading one component and gets both correct size and correct glyphs.
            return BuildLanguageSample(prefabSample, liveLanguageSample);
        }

        private TextMeshProUGUI FindLiveLanguageText()
        {
            if (optionsScreen == null)
                return null;

            TextMeshProUGUI[] texts = optionsScreen.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts == null)
                return null;

            RectTransform container = OptionsContainer;

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null || text.font == null)
                    continue;

                // Skip our own custom rows (inside the options container). Only the game's
                // native chrome is reliably localized to the current language font.
                if (container != null && text.transform.IsChildOf(container))
                    continue;

                return text;
            }

            return null;
        }

        private TextMeshProUGUI BuildLanguageSample(TextMeshProUGUI sizeSource, TextMeshProUGUI fontSource)
        {
            // Detached, inactive holder: never laid out or rendered, destroyed with the
            // page on the next rebuild. It only carries style values for the call sites.
            GameObject go = new GameObject("SulfurStyleSample", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(OptionsContainer, false);
            go.SetActive(false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = fontSource.font;
            tmp.fontSharedMaterial = fontSource.fontSharedMaterial;
            tmp.fontSize = sizeSource.fontSize;
            tmp.color = sizeSource.color;

            return tmp;
        }

        private TextMeshProUGUI FindPrefabSampleText()
        {
            if (optionsScreen == null)
                return null;

            GameObject[] prefabs =
            {
                SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen),
                SulfurOptionsScreenBridge.GetOptionInfoPrefab(optionsScreen),
                SulfurOptionsScreenBridge.GetOptionBoolPrefab(optionsScreen),
                SulfurOptionsScreenBridge.GetOptionAlternativePrefab(optionsScreen)
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

        private static bool TryParseNumber(string text, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Trim().Replace(',', '.');

            return float.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private static float RoundNumber(float value, int decimals)
        {
            if (decimals <= 0)
                return Mathf.Round(value);

            float scale = Mathf.Pow(10f, decimals);
            return Mathf.Round(value * scale) / scale;
        }

        public void AddSection(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
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

        public OptionsScreenOption AddDescription(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
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

            return null;
        }

        // A plain, full-opacity text row that returns a live handle. Unlike
        // AddDescription (dimmed, "— " prefixed, wrapping), this is meant for
        // status lines / values that change while the page is open: keep the
        // handle and call SetText/SetColor/SetVisible without a full Rebuild.
        public SulfurTextHandle AddTextRow(string text)
        {
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
            if (string.IsNullOrWhiteSpace(text))
                return;

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
            label.text = GetMessagePrefix(kind) + text;
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
            if (badges == null || badges.Count == 0)
                return;

            GameObject row = new GameObject("SULFUR_BadgeRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(OptionsContainer, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            ApplyNativeRowRect(rowRt, 30f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;
            layout.minWidth = GetNativeOptionWidth();
            layout.preferredWidth = GetNativeOptionWidth();

            HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(24, 12, 5, 5);
            group.spacing = 8f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = false;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;

            foreach (string badge in badges)
            {
                if (!string.IsNullOrWhiteSpace(badge))
                    CreateBadge(row.transform, badge.Trim());
            }
        }

        public void AddDefaultButton(Action onPressed)
        {
            AddSmallButton("Default", onPressed);
        }

        public void AddSmallButton(string label, Action onPressed)
        {
            AddSmallButton(label, onPressed, 0f);
        }

        public void AddSmallButton(string label, Action onPressed, float minWidth)
        {
            string safeLabel = string.IsNullOrWhiteSpace(label) ? "Default" : label;
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
            if (button != null)
            {
                LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
                if (buttonLayout != null)
                {
                    buttonLayout.minWidth = buttonWidth;
                    buttonLayout.preferredWidth = buttonWidth;
                }
            }
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
            if (buttons == null || buttons.Count == 0)
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
            foreach (SulfurButton spec in valid)
            {
                UIButton button = CreateSmallButton(row.transform, spec.Label, spec.OnPressed);
                if (button == null)
                    continue;

                LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
                if (buttonLayout != null)
                {
                    float buttonWidth = spec.MinWidth > 0f ? spec.MinWidth : CalculateSmallButtonWidth(spec.Label);
                    buttonLayout.minWidth = buttonWidth;
                    buttonLayout.preferredWidth = buttonWidth;
                }

                TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
                handles.Add(new SulfurButtonHandle(button.gameObject, button, buttonLabel));
            }

            return handles;
        }

        // Creates an empty, refreshable list section and returns a handle. Call
        // handle.Update(ctx => { ctx.AddTextRow(...); ... }) to (re)populate it in
        // place without rebuilding the whole page. Used for live tables (player +
        // ping + kick button) that change while the page is open.
        public SulfurListHandle AddList()
        {
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
            GameObject spacer = new GameObject("SULFUR_Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(OptionsContainer, false);

            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(0f, height);
            layout.preferredHeight = Mathf.Max(0f, height);
            layout.flexibleHeight = 0f;
        }

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
            OptionsScreenOption labelOption = AddInfo(label);
            AddDescription(description);

            TMP_InputField input = CreateStandaloneInputField(
                "TextInput_" + label,
                value ?? "",
                "Enter text...",
                TMP_InputField.ContentType.Standard);

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onChanged != null)
                    onChanged(text);
            }));

            return labelOption;
        }

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

        public OptionsScreenOption AddNumberInput(
    string label,
    string description,
    float value,
    float min,
    float max,
    int decimals,
    Action<float> onChanged)
        {
            OptionsScreenOption labelOption = AddInfo(label);
            AddDescription(description);

            TMP_InputField input = CreateStandaloneInputField(
                "NumberInput_" + label,
                FormatNumber(value, decimals),
                "Enter number...",
                TMP_InputField.ContentType.DecimalNumber);

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(new UnityAction<string>(delegate (string text)
            {
                float parsed;
                if (!TryParseNumber(text, out parsed))
                {
                    input.SetTextWithoutNotify(FormatNumber(value, decimals));
                    return;
                }

                parsed = Mathf.Clamp(parsed, min, max);
                parsed = RoundNumber(parsed, decimals);

                input.SetTextWithoutNotify(FormatNumber(parsed, decimals));

                if (onChanged != null)
                    onChanged(parsed);
            }));

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
            TMP_InputField input = BuildInlineInputRow(
                "SULFUR_InlineText_" + (label ?? ""),
                label,
                value ?? "",
                "Enter text...",
                password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard,
                labelWidth);

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onChanged != null)
                    onChanged(text);
            }));

            return input;
        }

        public TMP_InputField AddInlineNumberInput(
            string label,
            float value,
            float min,
            float max,
            int decimals,
            Action<float> onChanged,
            float labelWidth = 160f)
        {
            TMP_InputField input = BuildInlineInputRow(
                "SULFUR_InlineNumber_" + (label ?? ""),
                label,
                FormatNumber(value, decimals),
                "Enter number...",
                TMP_InputField.ContentType.DecimalNumber,
                labelWidth);

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(new UnityAction<string>(delegate (string text)
            {
                float parsed;
                if (!TryParseNumber(text, out parsed))
                {
                    input.SetTextWithoutNotify(FormatNumber(value, decimals));
                    return;
                }

                parsed = Mathf.Clamp(parsed, min, max);
                parsed = RoundNumber(parsed, decimals);

                input.SetTextWithoutNotify(FormatNumber(parsed, decimals));

                if (onChanged != null)
                    onChanged(parsed);
            }));

            return input;
        }

        private OptionsScreenOption AddTextInputInternal(
            string label,
            string value,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onChanged,
            Action<string> onEndEdit)
        {
            return AddTextInputInternal(label, value, placeholder, contentType, onChanged, onEndEdit, null);
        }

        private OptionsScreenOption AddTextInputInternal(
            string label,
            string value,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onChanged,
            Action<string> onEndEdit,
            Action<TMP_InputField> afterCreate)
        {
            GameObject prefab = SulfurOptionsScreenBridge.GetOptionButtonPrefab(optionsScreen);
            OptionsScreenOption option = InstantiateOption(prefab);
            option.SetLabel(label ?? "");

            TMP_InputField input = CreateInputFieldOnOption(option, value ?? "", placeholder ?? "", contentType);

            SulfurTextInputBinding inputBinding = input.gameObject.AddComponent<SulfurTextInputBinding>();
            inputBinding.Configure(input);

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onChanged != null)
                    onChanged(text);
            }));

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(new UnityAction<string>(delegate (string text)
            {
                if (onEndEdit != null)
                    onEndEdit(text);
            }));

            option.onButtonPressed = delegate
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(input.gameObject);

                input.ActivateInputField();
            };

            if (afterCreate != null)
                afterCreate(input);

            RegisterOption(option);
            return option;
        }

        private TMP_InputField CreateInputFieldOnOption(
            OptionsScreenOption option,
            string value,
            string placeholder,
            TMP_InputField.ContentType contentType)
        {
            RectTransform optionRt = option.GetComponent<RectTransform>();
            UIText labelText = SulfurOptionsScreenBridge.GetOptionLabelText(option);

            GameObject root = new GameObject("SULFUR_TMP_InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIImage), typeof(TMP_InputField));
            root.transform.SetParent(option.transform, false);

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.52f, 0.18f);
            rootRt.anchorMax = new Vector2(0.98f, 0.82f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            UIImage image = root.GetComponent<UIImage>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = true;

            GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 2f);
            viewportRt.offsetMax = new Vector2(-8f, -2f);

            UIText placeholderText = CreateInputText(viewport.transform, "Placeholder", placeholder, labelText);
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            placeholderText.fontStyle = FontStyles.Italic;

            UIText valueText = CreateInputText(viewport.transform, "Text", value, labelText);
            valueText.color = labelText != null ? labelText.color : Color.white;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewportRt;
            input.textComponent = valueText;
            input.placeholder = placeholderText;
            input.targetGraphic = image;
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.shouldActivateOnSelect = true;
            input.onFocusSelectAll = false;
            input.resetOnDeActivation = false;
            input.restoreOriginalTextOnEscape = false;
            input.SetTextWithoutNotify(value ?? "");

            return input;
        }

        private UIText CreateInputText(Transform parent, string name, string text, UIText template)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UIText));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            UIText tmp = go.GetComponent<UIText>();
            tmp.text = text ?? "";
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            if (template != null)
            {
                tmp.font = template.font;
                tmp.fontSharedMaterial = template.fontSharedMaterial;
                tmp.fontSize = template.fontSize;
                tmp.color = template.color;
            }
            else
            {
                tmp.fontSize = 20f;
                tmp.color = Color.white;
            }

            return tmp;
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

        private void RegisterOption(OptionsScreenOption option)
        {
            if (option == null)
                return;

            nativeOptions.Add(option);
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


        private void CreateBadge(Transform parent, string text)
        {
            TextMeshProUGUI sample = FindSampleText();

            GameObject badge = new GameObject("Badge_" + text, typeof(RectTransform), typeof(LayoutElement), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(parent, false);

            Image bg = badge.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);
            bg.raycastTarget = false;

            LayoutElement layout = badge.GetComponent<LayoutElement>();
            layout.minWidth = Mathf.Clamp(text.Length * 9f + 26f, 70f, 220f);
            layout.preferredWidth = layout.minWidth;
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
            switch (kind)
            {
                case SulfurMessageKind.Warning:
                    return "WARNING: ";
                case SulfurMessageKind.Error:
                    return "ERROR: ";
                case SulfurMessageKind.Success:
                    return "OK: ";
                default:
                    return "";
            }
        }

        private static void SetFirstText(GameObject root, string text)
        {
            if (root == null)
                return;

            UIText first = root.GetComponentInChildren<UIText>(true);
            if (first != null)
                first.text = text ?? "";
        }

        private static void SetSliderText(UIText text, float value)
        {
            if (text != null)
                text.text = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string FormatNumber(float value, int decimals)
        {
            if (decimals <= 0)
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

            string format = "0." + new string('#', decimals);
            return value.ToString(format, CultureInfo.InvariantCulture);
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
