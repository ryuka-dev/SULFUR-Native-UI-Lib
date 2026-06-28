using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// A single toast card and its animation state. Built once, then driven each
    /// frame by <see cref="SulfurToastController"/> via <see cref="Tick"/>. Not a
    /// MonoBehaviour — the controller owns the update loop so all cards animate in
    /// lockstep on unscaled time.
    ///
    /// Motion model (Apple-ish):
    ///   - Enter: slide in from the right with a slight overshoot (easeOutBack),
    ///     fade in, and scale up from 0.94.
    ///   - Hold:  fully visible for its duration.
    ///   - Exit:  accelerate out to the right (easeInCubic), fade and shrink a touch.
    ///   - Restack: vertical position eases toward its target with SmoothDamp, so
    ///     when cards above appear/disappear this card glides to its new slot.
    /// </summary>
    internal sealed class SulfurToastView
    {
        public enum Phase { Entering, Holding, Exiting, Dead }

        private const float EnterDuration = 0.42f;
        private const float ExitDuration = 0.30f;
        private const float RestackSmoothTime = 0.16f;

        // How far off the right edge the card travels for enter/exit slides.
        private const float SlideDistance = SulfurToastController.CardWidth + 80f;
        // Right edge sits this far in from the screen's right edge when visible.
        private const float VisibleX = -SulfurToastController.RightMargin;

        private readonly RectTransform rect;
        private readonly CanvasGroup canvasGroup;

        private Phase phase = Phase.Entering;
        private float phaseTime;
        private readonly float holdDuration;

        private float targetY;
        private float currentY;
        private float yVelocity;
        private bool yInitialized;

        public Phase CurrentPhase { get { return phase; } }
        public bool IsLeaving { get { return phase == Phase.Exiting || phase == Phase.Dead; } }
        public bool IsDead { get { return phase == Phase.Dead; } }
        public float Height { get; private set; }

        private SulfurToastView(RectTransform rect, CanvasGroup canvasGroup, float holdDuration)
        {
            this.rect = rect;
            this.canvasGroup = canvasGroup;
            this.holdDuration = Mathf.Max(0.5f, holdDuration);
        }

        public static SulfurToastView Create(
            Transform parent,
            TMP_FontAsset font,
            Material fontMaterial,
            string title,
            string message,
            float holdDuration)
        {
            GameObject card = new GameObject(
                "Toast",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            // Anchored to the top-right corner; pivot at top-right so anchoredPosition
            // addresses the card's top-right corner directly.
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(SulfurToastController.CardWidth, 0f);

            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false; // passive — never eats input

            Image background = card.GetComponent<Image>();
            background.color = SulfurUiTheme.PanelFill;
            background.raycastTarget = false;

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 18, 14, 14);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = card.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left amber accent bar — Apple-style colored edge, out of the layout flow.
            CreateAccentBar(rect);

            if (!string.IsNullOrEmpty(title))
                CreateText(card.transform, font, fontMaterial, title, 22f, FontStyles.Bold, SulfurUiTheme.Accent);

            CreateText(card.transform, font, fontMaterial, message ?? "", 19f, FontStyles.Normal, SulfurUiTheme.TextPrimary);

            // Force a layout pass now so we can read the final card height for stacking.
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            SulfurToastView view = new SulfurToastView(rect, canvasGroup, holdDuration);
            view.Height = rect.rect.height;
            return view;
        }

        private static void CreateAccentBar(RectTransform cardRect)
        {
            GameObject bar = new GameObject(
                "AccentBar",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(Image));

            bar.transform.SetParent(cardRect, false);

            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(4f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(0f, 6f);
            rt.offsetMax = new Vector2(4f, -6f);

            bar.GetComponent<LayoutElement>().ignoreLayout = true;

            Image image = bar.GetComponent<Image>();
            image.color = SulfurUiTheme.Accent;
            image.raycastTarget = false;
        }

        private static void CreateText(
            Transform parent,
            TMP_FontAsset font,
            Material fontMaterial,
            string content,
            float fontSize,
            FontStyles style,
            Color color)
        {
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            // Leave room for the accent bar on the left.
            text.margin = new Vector4(6f, 0f, 0f, 0f);

            if (font != null)
                text.font = font;
            if (fontMaterial != null)
                text.fontSharedMaterial = fontMaterial;
        }

        /// <summary>Set the vertical slot this card should occupy (top edge Y).</summary>
        public void SetTargetY(float y)
        {
            targetY = y;

            // First placement snaps in vertically (no drift); the slide-in is horizontal.
            if (!yInitialized)
            {
                currentY = y;
                yInitialized = true;
            }
        }

        /// <summary>Begin the exit animation if still visible.</summary>
        public void BeginExit()
        {
            if (phase == Phase.Entering || phase == Phase.Holding)
            {
                phase = Phase.Exiting;
                phaseTime = 0f;
            }
        }

        /// <summary>Advance the animation by one unscaled frame and apply it.</summary>
        public void Tick(float dt)
        {
            phaseTime += dt;

            switch (phase)
            {
                case Phase.Entering:
                    if (phaseTime >= EnterDuration)
                    {
                        phase = Phase.Holding;
                        phaseTime = 0f;
                    }
                    break;

                case Phase.Holding:
                    if (phaseTime >= holdDuration)
                    {
                        phase = Phase.Exiting;
                        phaseTime = 0f;
                    }
                    break;

                case Phase.Exiting:
                    if (phaseTime >= ExitDuration)
                        phase = Phase.Dead;
                    break;
            }

            float slide;
            float alpha;
            float scale;

            if (phase == Phase.Entering)
            {
                float p = Mathf.Clamp01(phaseTime / EnterDuration);
                float e = EaseOutBack(p);
                slide = Mathf.Lerp(SlideDistance, 0f, e);
                alpha = EaseOutCubic(p);
                scale = Mathf.Lerp(0.94f, 1f, e);
            }
            else if (phase == Phase.Holding)
            {
                slide = 0f;
                alpha = 1f;
                scale = 1f;
            }
            else // Exiting / Dead
            {
                float p = phase == Phase.Dead ? 1f : Mathf.Clamp01(phaseTime / ExitDuration);
                float e = EaseInCubic(p);
                slide = Mathf.Lerp(0f, SlideDistance, e);
                alpha = 1f - e;
                scale = Mathf.Lerp(1f, 0.95f, e);
            }

            // Leaving cards freeze their vertical slot and just slide out; cards still
            // in the stack ease toward their (possibly changed) target slot.
            if (!IsLeaving)
                currentY = Mathf.SmoothDamp(currentY, targetY, ref yVelocity, RestackSmoothTime, Mathf.Infinity, dt);

            rect.anchoredPosition = new Vector2(VisibleX + slide, currentY);
            rect.localScale = new Vector3(scale, scale, 1f);
            canvasGroup.alpha = alpha;
        }

        public void Destroy()
        {
            if (rect != null)
                Object.Destroy(rect.gameObject);
        }

        // --- Easing -----------------------------------------------------------

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
