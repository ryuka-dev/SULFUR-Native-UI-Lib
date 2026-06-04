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

        private float blinkTimer;
        private bool blinkVisible = true;

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
        }

        private void LateUpdate()
        {
            if (input == null || textComponent == null || viewport == null || caretImage == null)
                return;

            if (!input.isFocused)
            {
                caretImage.enabled = false;
                blinkTimer = 0f;
                blinkVisible = true;
                return;
            }

            blinkTimer += Time.unscaledDeltaTime;

            if (blinkTimer >= 0.5f)
            {
                blinkTimer = 0f;
                blinkVisible = !blinkVisible;
            }

            UpdateCaretPosition();

            caretImage.enabled = blinkVisible;
        }

        private void UpdateCaretPosition()
        {
            string text = input.text ?? "";

            int caretIndex = 0;

            try
            {
                caretIndex = Mathf.Clamp(input.caretPosition, 0, text.Length);
            }
            catch
            {
                caretIndex = text.Length;
            }

            string prefix = caretIndex > 0 ? text.Substring(0, caretIndex) : "";

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
            x = Mathf.Clamp(x, 0f, maxX);

            caretRect.sizeDelta = new Vector2(
                caretRect.sizeDelta.x,
                Mathf.Max(20f, textComponent.fontSize * 1.15f));

            caretRect.anchoredPosition = new Vector2(x, 0f);
        }
    }
}