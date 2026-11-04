using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Lays out a badge row: each badge as wide as its text (measured with its own font and
    /// size, at least <see cref="MinBadgeWidth"/>, at most <see cref="MaxBadgeWidth"/>),
    /// left to right, wrapping onto further lines when the row is too narrow.
    /// </summary>
    /// <remarks>
    /// <para>One line looks exactly like the old fixed row: badges 100px wide unless their
    /// text needs more, 26px high, 8px apart, placed where the row's former
    /// HorizontalLayoutGroup put them. Every further line adds the row's base height.</para>
    /// <para>Widths are measured during layout, so a badge whose text, font size or language
    /// changes (SulfurSettingHandle.SetDirty, SulfurBadgeRowHandle.SetBadge, the compact
    /// setting-row style) is re-measured by the layout rebuild TMP requests itself; nothing
    /// has to notify this component.</para>
    /// <para>The row's height is reported as its preferred height, for parents that control
    /// child heights, and is also applied to the row's own rect after layout, for parents
    /// that don't. The row's LayoutElement must not pin a height.</para>
    /// </remarks>
    internal sealed class SulfurBadgeFlowLayout : LayoutGroup
    {
        internal const float MinBadgeWidth = 100f;
        internal const float MaxBadgeWidth = 240f;

        private const float BadgeHeight = 26f;
        private const float Spacing = 8f;

        // CreateBadge insets the label 8px on each side; a pixel more avoids an ellipsis
        // from rounding.
        private const float LabelInset = 17f;

        private const float Unconstrained = 32767f;

        private float baseHeight = 30f;
        private readonly List<float> widths = new List<float>();

        /// <summary>Sets the row up as a badge row with <paramref name="lineHeight"/> per line.</summary>
        internal void Configure(float lineHeight)
        {
            baseHeight = lineHeight;
            padding = new RectOffset(24, 12, 5, 5);
            childAlignment = TextAnchor.MiddleLeft;
            SetDirty();
        }

        /// <summary>
        /// Changes the per-line height of <paramref name="row"/>'s badge layout (the compact
        /// setting-row style). False when the row has none.
        /// </summary>
        internal static bool SetBaseHeight(GameObject row, float lineHeight)
        {
            SulfurBadgeFlowLayout flow = row != null ? row.GetComponent<SulfurBadgeFlowLayout>() : null;
            if (flow == null)
                return false;

            flow.baseHeight = lineHeight;
            flow.SetDirty();
            return true;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            widths.Clear();
            float widest = 0f;
            float total = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                float width = MeasureBadge(rectChildren[i]);
                widths.Add(width);
                widest = Mathf.Max(widest, width);
                total += width + (i > 0 ? Spacing : 0f);
            }

            SetLayoutInputForAxis(padding.horizontal + widest, padding.horizontal + total, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float height = HeightForWidth(rectTransform.rect.width);
            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            Place(0);
        }

        public override void SetLayoutVertical()
        {
            Place(1);

            // A parent that does not control child heights leaves the rect alone: size it
            // here. When the parent did size it, the heights already match and this is a no-op.
            float height = HeightForWidth(rectTransform.rect.width);
            if (Mathf.Abs(rectTransform.rect.height - height) > 0.5f)
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void Place(int axis)
        {
            float available = Available(rectTransform.rect.width);

            // Where the old HorizontalLayoutGroup (MiddleLeft, child taller than the inner
            // height) put a 26px badge inside one base-height line.
            float lineOffset = padding.top + ((baseHeight - padding.vertical) - BadgeHeight) * 0.5f;

            float x = 0f;
            int line = 0;

            for (int i = 0; i < rectChildren.Count && i < widths.Count; i++)
            {
                float width = Mathf.Min(widths[i], available);

                if (x > 0f && x + width > available)
                {
                    line++;
                    x = 0f;
                }

                if (axis == 0)
                    SetChildAlongAxis(rectChildren[i], 0, padding.left + x, width);
                else
                    SetChildAlongAxis(rectChildren[i], 1, lineOffset + line * baseHeight, BadgeHeight);

                x += width + Spacing;
            }
        }

        private float HeightForWidth(float rowWidth)
        {
            float available = Available(rowWidth);
            int lines = 1;
            float x = 0f;

            for (int i = 0; i < widths.Count; i++)
            {
                float width = Mathf.Min(widths[i], available);

                if (x > 0f && x + width > available)
                {
                    lines++;
                    x = 0f;
                }

                x += width + Spacing;
            }

            return lines * baseHeight;
        }

        private float Available(float rowWidth)
        {
            return Mathf.Max(MinBadgeWidth, rowWidth - padding.horizontal);
        }

        private static float MeasureBadge(RectTransform badge)
        {
            TextMeshProUGUI label = badge != null ? badge.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label == null || string.IsNullOrEmpty(label.text))
                return MinBadgeWidth;

            float textWidth = label.GetPreferredValues(label.text, Unconstrained, Unconstrained).x;
            return Mathf.Clamp(Mathf.Ceil(textWidth + LabelInset), MinBadgeWidth, MaxBadgeWidth);
        }
    }
}
