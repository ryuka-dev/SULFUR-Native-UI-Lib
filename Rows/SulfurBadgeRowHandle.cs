using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a badge row created by
    /// <see cref="SulfurOptionsContext.AddBadgeRowEx(string[])"/> or a <c>*FoldoutWithBadges</c>
    /// overload with an <c>out SulfurBadgeRowHandle</c>. Lets a page replace the badges in
    /// place (e.g. a "Pending: N" count on a plugin header after an edit) without rebuilding
    /// the page.
    /// </summary>
    /// <remarks>
    /// Badges look exactly like those of <see cref="SulfurOptionsContext.AddBadgeRow(string[])"/>:
    /// the text is shown as given (trimmed; empty and whitespace entries are skipped), every
    /// badge has the same colours. Badges are not navigation stops, and the row keeps the
    /// size and position it was built with. A row without badges is hidden (it takes no
    /// space) until badges are set again. Once the page is closed or rebuilt
    /// (<see cref="IsAlive"/> false) every member is safe to call and does nothing; take the
    /// new handle from the next build.
    /// </remarks>
    public sealed class SulfurBadgeRowHandle
    {
        private readonly SulfurOptionsContext context;
        private readonly GameObject row;
        private bool visible = true;

        internal SulfurBadgeRowHandle(SulfurOptionsContext context, GameObject row)
        {
            this.context = context;
            this.row = row;
        }

        /// <summary>True while the row exists and the page that built it is on screen.</summary>
        public bool IsAlive
        {
            get { return row != null && context != null && context.IsAlive; }
        }

        /// <summary>The number of badges shown now (0 when the row is gone).</summary>
        public int BadgeCount
        {
            get { return row != null ? row.transform.childCount : 0; }
        }

        /// <summary>
        /// Replaces all badges of the row with <paramref name="badges"/>. An empty list (or
        /// only empty/whitespace texts) hides the row. False when the row is gone.
        /// </summary>
        public bool SetBadges(params string[] badges)
        {
            return SetBadges((IReadOnlyList<string>)badges);
        }

        /// <summary>As <see cref="SetBadges(string[])"/>.</summary>
        public bool SetBadges(IReadOnlyList<string> badges)
        {
            if (!IsAlive)
                return false;

            List<string> clean = CleanBadges(badges);
            Transform rowTransform = row.transform;

            // Out of the row (and its layout) now; the objects go at the end of the frame.
            for (int i = rowTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = rowTransform.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }

            foreach (string badge in clean)
                context.CreateBadge(rowTransform, badge);

            ApplyVisibility();
            return true;
        }

        /// <summary>
        /// Changes the text of the badge at <paramref name="index"/> (0 = leftmost), keeping
        /// the others. False when the row is gone, the index is out of range or
        /// <paramref name="text"/> is empty (use <see cref="SetBadges(string[])"/> to remove
        /// badges).
        /// </summary>
        public bool SetBadge(int index, string text)
        {
            if (!IsAlive || index < 0 || index >= row.transform.childCount || string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.Trim();
            Transform badge = row.transform.GetChild(index);

            TextMeshProUGUI label = badge.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
                return false;

            // The row's flow layout re-measures the badge (and re-wraps the row) in the layout
            // rebuild TMP requests for the new text.
            label.text = trimmed;
            badge.name = SulfurOptionsContext.BadgeObjectPrefix + trimmed;

            return true;
        }

        /// <summary>
        /// Shows or hides the row. A shown row without badges stays hidden until
        /// <see cref="SetBadges(string[])"/> gives it some.
        /// </summary>
        public void SetVisible(bool visible)
        {
            this.visible = visible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (row != null)
                row.SetActive(visible && row.transform.childCount > 0);
        }

        // The badges as AddBadgeRow(params string[]) shows them: trimmed, empty ones skipped.
        internal static List<string> CleanBadges(IReadOnlyList<string> badges)
        {
            List<string> clean = new List<string>();
            if (badges == null)
                return clean;

            foreach (string badge in badges)
            {
                if (!string.IsNullOrWhiteSpace(badge))
                    clean.Add(badge.Trim());
            }

            return clean;
        }
    }
}
