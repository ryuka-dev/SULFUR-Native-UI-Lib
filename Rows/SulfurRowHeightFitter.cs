using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Grows a fixed-height text row (description, message line, text row) to fit its text
    /// when it runs over more than one line: wrapped, or with explicit line breaks. While the
    /// text fits on one line the row keeps its base height, so single-line rows look exactly
    /// as before; every further line adds its own height, and the top/bottom padding of the
    /// single-line row is kept. Lives on the row's text object.
    /// </summary>
    /// <remarks>
    /// The height is computed when the row is built and again whenever the library changes
    /// the row's text, width or base height (<see cref="Refit(TextMeshProUGUI)"/>,
    /// <see cref="SetBaseHeight"/>). It measures against the row's current width: the built
    /// width before the first layout pass, the laid-out width afterwards. It never runs from
    /// a layout callback, so it cannot re-enter a layout rebuild. The row's RectTransform
    /// height and its LayoutElement min/preferred height are both set, so the row is sized
    /// right whether or not its parent layout group controls child heights.
    /// </remarks>
    internal sealed class SulfurRowHeightFitter : MonoBehaviour
    {
        // Width/height that never constrains a measurement (TMP's own "large" extent).
        private const float Unconstrained = 32767f;

        // A wrapped height this close to the single-line height is the same line count.
        private const float SameHeightTolerance = 0.5f;

        // A text whose unwrapped width is this far inside the available width does not wrap,
        // so it needs no second measurement.
        private const float FitsWidthMargin = 1f;

        private RectTransform rowRect;
        private LayoutElement rowLayout;
        private TextMeshProUGUI text;
        private float baseHeight;

        // TMP sets its component up in Awake, which runs only once its object is active. A
        // row whose text has not been active yet measures on its first OnEnable instead.
        private bool textReady;
        private bool refitPending;

        /// <summary>
        /// Makes <paramref name="row"/> follow <paramref name="label"/>'s line count, with
        /// <paramref name="minHeight"/> as its single-line height, and sizes it now. Call after
        /// the label's font, size, wrapping and text are set, and before the row is hidden.
        /// </summary>
        internal static void Attach(GameObject row, TextMeshProUGUI label, float minHeight)
        {
            if (row == null || label == null)
                return;

            SulfurRowHeightFitter fitter = label.GetComponent<SulfurRowHeightFitter>();
            if (fitter == null)
                fitter = label.gameObject.AddComponent<SulfurRowHeightFitter>();

            fitter.rowRect = row.GetComponent<RectTransform>();
            fitter.rowLayout = row.GetComponent<LayoutElement>();
            fitter.text = label;
            fitter.baseHeight = minHeight;
            fitter.Refit();
        }

        /// <summary>Re-sizes the row of <paramref name="label"/> after its text changed (no-op without a fitter).</summary>
        internal static void Refit(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            SulfurRowHeightFitter fitter = label.GetComponent<SulfurRowHeightFitter>();
            if (fitter != null)
                fitter.Refit();
        }

        /// <summary>
        /// Changes the single-line height of <paramref name="row"/>'s fitter (a restyle that
        /// also changed the font size or width) and re-sizes the row. False when the row has
        /// no fitter.
        /// </summary>
        internal static bool SetBaseHeight(GameObject row, float minHeight)
        {
            if (row == null)
                return false;

            SulfurRowHeightFitter fitter = row.GetComponentInChildren<SulfurRowHeightFitter>(true);
            if (fitter == null)
                return false;

            fitter.baseHeight = minHeight;
            fitter.Refit();
            return true;
        }

        private void OnEnable()
        {
            if (refitPending)
                Refit();
        }

        private void Refit()
        {
            if (rowRect == null || text == null)
                return;

            if (!NeedsMeasuring())
            {
                refitPending = false;
                Apply(baseHeight);
                return;
            }

            if (text.gameObject.activeInHierarchy)
                textReady = true;

            if (!textReady)
            {
                // Measured once the text becomes active (OnEnable).
                refitPending = true;
                Apply(baseHeight);
                return;
            }

            refitPending = false;
            Apply(Measure());
        }

        // Only text that can run over more than one line can need more than the base height.
        private bool NeedsMeasuring()
        {
            string value = text.text;
            if (string.IsNullOrEmpty(value))
                return false;

            TextWrappingModes mode = text.textWrappingMode;
            bool wraps = mode == TextWrappingModes.Normal || mode == TextWrappingModes.PreserveWhitespace;

            return wraps || value.IndexOf('\n') >= 0;
        }

        private float Measure()
        {
            float width = GetTextWidth();
            if (width <= 1f)
                return baseHeight;

            string value = text.text;
            bool hasLineBreak = value.IndexOf('\n') >= 0;

            // x: the unwrapped width, y: the height wrapped at the row's width.
            Vector2 wrapped = text.GetPreferredValues(width, Unconstrained);
            if (!hasLineBreak && wrapped.x <= width - FitsWidthMargin)
                return baseHeight;

            // The same text on one line: its height is what the base height was made for.
            string singleLine = hasLineBreak
                ? value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ')
                : value;
            float singleLineHeight = text.GetPreferredValues(singleLine, Unconstrained, Unconstrained).y;

            if (wrapped.y - singleLineHeight < SameHeightTolerance)
                return baseHeight;

            float padding = Mathf.Max(0f, baseHeight - singleLineHeight);
            return Mathf.Max(baseHeight, Mathf.Ceil(wrapped.y + padding));
        }

        // The text's own width: the row's width minus the text rect's horizontal insets and
        // the text margins.
        private float GetTextWidth()
        {
            RectTransform textRect = text.rectTransform;

            float width;
            if (textRect.parent == rowRect && textRect.anchorMin.x == 0f && textRect.anchorMax.x == 1f)
                width = rowRect.rect.width + textRect.sizeDelta.x;
            else
                width = textRect.rect.width;

            Vector4 margin = text.margin;
            return width - margin.x - margin.z;
        }

        private void Apply(float height)
        {
            Vector2 size = rowRect.sizeDelta;
            if (Mathf.Abs(size.y - height) > 0.01f)
                rowRect.sizeDelta = new Vector2(size.x, height);

            if (rowLayout != null)
            {
                // The setters mark the layout dirty only when the value changes.
                rowLayout.minHeight = height;
                rowLayout.preferredHeight = height;
            }
        }
    }
}
