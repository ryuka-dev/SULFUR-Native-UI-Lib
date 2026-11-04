using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurInputCaretOverlay : MonoBehaviour
    {
        private TMP_InputField input;
        private TextMeshProUGUI textComponent;
        private RectTransform viewport;
        private RectTransform caretRect;
        private Image caretImage;

        // Selection highlight. TMP draws its own caret and highlight through a "Caret" child
        // it creates in OnEnable only when its text component is already assigned, which is
        // not the case for the library's fields (the component is added first), so without
        // this a selected text (keyboard start of editing a number, Ctrl+A, a double click)
        // would not be visible. Like TMP, a selection shows the highlight and no caret.
        private RectTransform selectionRect;
        private Image selectionImage;
        private bool selectionShown;

        private float blinkTimer;
        private bool blinkVisible = true;

        // Inputs of the last caret layout. The position is only recomputed when one of
        // them changes; GetPreferredValues is a full TMP layout pass, too costly to run
        // every frame while the field is focused.
        private bool layoutValid;
        private string lastText;
        private int lastCaretPosition;
        private int lastAnchorPosition;
        private float lastFontSize;
        private TMP_FontAsset lastFont;
        private float lastViewportWidth;
        private float lastViewportHeight;

        public static SulfurInputCaretOverlay Attach(
            TMP_InputField input,
            TextMeshProUGUI textComponent,
            RectTransform viewport,
            Color color)
        {
            if (input == null || textComponent == null || viewport == null)
                return null;

            GameObject go = new GameObject(
                "VisibleCaret",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(SulfurInputCaretOverlay));

            go.transform.SetParent(viewport, false);

            SulfurInputCaretOverlay overlay = go.GetComponent<SulfurInputCaretOverlay>();
            overlay.Initialize(input, textComponent, viewport, color);

            return overlay;
        }

        private void Initialize(
            TMP_InputField sourceInput,
            TextMeshProUGUI sourceText,
            RectTransform sourceViewport,
            Color color)
        {
            input = sourceInput;
            textComponent = sourceText;
            viewport = sourceViewport;

            caretRect = GetComponent<RectTransform>();
            caretRect.anchorMin = new Vector2(0f, 0.5f);
            caretRect.anchorMax = new Vector2(0f, 0.5f);
            caretRect.pivot = new Vector2(0f, 0.5f);
            caretRect.sizeDelta = new Vector2(3f, Mathf.Max(20f, sourceText.fontSize * 1.15f));
            caretRect.anchoredPosition = Vector2.zero;

            caretImage = GetComponent<Image>();
            caretImage.color = new Color(color.r, color.g, color.b, 1f);
            caretImage.raycastTarget = false;
            caretImage.enabled = false;

            // First child of the viewport: drawn behind the text.
            GameObject selection = new GameObject(
                "VisibleSelection",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            selection.transform.SetParent(sourceViewport, false);
            selection.transform.SetAsFirstSibling();

            selectionRect = selection.GetComponent<RectTransform>();
            selectionRect.anchorMin = new Vector2(0f, 0.5f);
            selectionRect.anchorMax = new Vector2(0f, 0.5f);
            selectionRect.pivot = new Vector2(0f, 0.5f);
            selectionRect.sizeDelta = Vector2.zero;
            selectionRect.anchoredPosition = Vector2.zero;

            selectionImage = selection.GetComponent<Image>();
            selectionImage.color = sourceInput.selectionColor;
            selectionImage.raycastTarget = false;
            selectionImage.enabled = false;
        }

        private void LateUpdate()
        {
            if (input == null || textComponent == null || viewport == null || caretImage == null)
                return;

            if (!input.isFocused)
            {
                caretImage.enabled = false;
                SetSelectionShown(false);
                blinkTimer = 0f;
                blinkVisible = true;
                layoutValid = false;
                return;
            }

            blinkTimer += Time.unscaledDeltaTime;

            if (blinkTimer >= 0.5f)
            {
                blinkTimer = 0f;
                blinkVisible = !blinkVisible;
            }

            UpdateCaretPositionIfChanged();

            caretImage.enabled = blinkVisible && !selectionShown;
        }

        private void UpdateCaretPositionIfChanged()
        {
            string text = input.text ?? "";

            // String indices, not caret (character) indices: TMP keeps the string positions
            // current on every path (typing, keys, mouse, select all), while the caret indices
            // are re-synced from them only by its own caret renderer, which these fields do
            // not have (see selectionRect). The focus end is where the caret is drawn.
            int caretIndex;
            int anchorIndex;

            try
            {
                caretIndex = Mathf.Clamp(input.stringPosition, 0, text.Length);
                anchorIndex = Mathf.Clamp(input.selectionStringAnchorPosition, 0, text.Length);
            }
            catch
            {
                caretIndex = text.Length;
                anchorIndex = caretIndex;
            }

            float fontSize = textComponent.fontSize;
            TMP_FontAsset font = textComponent.font;
            Rect viewportRect = viewport.rect;

            if (layoutValid
                && caretIndex == lastCaretPosition
                && anchorIndex == lastAnchorPosition
                && fontSize == lastFontSize
                && font == lastFont
                && viewportRect.width == lastViewportWidth
                && viewportRect.height == lastViewportHeight
                && string.Equals(text, lastText, System.StringComparison.Ordinal))
            {
                return;
            }

            layoutValid = true;
            lastText = text;
            lastCaretPosition = caretIndex;
            lastAnchorPosition = anchorIndex;
            lastFontSize = fontSize;
            lastFont = font;
            lastViewportWidth = viewportRect.width;
            lastViewportHeight = viewportRect.height;

            UpdateCaretPosition(text, caretIndex);
            UpdateSelection(text, caretIndex, anchorIndex);
        }

        private void UpdateCaretPosition(string text, int caretIndex)
        {
            float x = GetCaretX(text, caretIndex);

            caretRect.sizeDelta = new Vector2(
                caretRect.sizeDelta.x,
                GetCaretHeight());

            caretRect.anchoredPosition = new Vector2(x, 0f);
        }

        private void UpdateSelection(string text, int caretIndex, int anchorIndex)
        {
            if (selectionRect == null || caretIndex == anchorIndex)
            {
                SetSelectionShown(false);
                return;
            }

            // A row shown again after its text component was assigned has TMP's own caret
            // renderer, which draws the highlight itself.
            // The caret stays hidden while there is a selection either way.
            if (viewport.GetComponentInChildren<TMP_SelectionCaret>(true) != null)
            {
                selectionShown = true;
                selectionImage.enabled = false;
                return;
            }

            float start = GetCaretX(text, Mathf.Min(caretIndex, anchorIndex));
            float end = GetCaretX(text, Mathf.Max(caretIndex, anchorIndex));

            selectionImage.color = input.selectionColor;
            selectionRect.sizeDelta = new Vector2(Mathf.Max(0f, end - start), GetCaretHeight());
            selectionRect.anchoredPosition = new Vector2(start, 0f);
            SetSelectionShown(true);
        }

        private void SetSelectionShown(bool shown)
        {
            selectionShown = shown;

            if (selectionImage != null)
                selectionImage.enabled = shown;
        }

        // X of the caret before text[index], clamped to the viewport.
        private float GetCaretX(string text, int index)
        {
            string prefix = index > 0 ? text.Substring(0, index) : "";

            float x = 0f;

            if (!string.IsNullOrEmpty(prefix))
            {
                Vector2 preferred = textComponent.GetPreferredValues(
                    prefix,
                    10000f,
                    viewport.rect.height);

                x = preferred.x;
            }

            float maxX = Mathf.Max(0f, viewport.rect.width - caretRect.sizeDelta.x);
            return Mathf.Clamp(x, 0f, maxX);
        }

        private float GetCaretHeight()
        {
            return Mathf.Max(20f, textComponent.fontSize * 1.15f);
        }
    }
}