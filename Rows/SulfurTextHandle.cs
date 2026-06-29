using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a single text row created by
    /// <see cref="SulfurOptionsContext.AddTextRow"/>. Lets a page update the row's
    /// text, color and visibility in place, without rebuilding the whole options page.
    /// Mirrors <see cref="SulfurSettingHandle"/> for plain text rows.
    /// </summary>
    public sealed class SulfurTextHandle
    {
        private readonly GameObject row;
        private readonly TextMeshProUGUI label;

        internal SulfurTextHandle(GameObject row, TextMeshProUGUI label)
        {
            this.row = row;
            this.label = label;
        }

        public void SetText(string text)
        {
            if (label != null)
                label.text = text ?? "";
        }

        public void SetColor(Color color)
        {
            if (label != null)
                label.color = color;
        }

        public void SetVisible(bool visible)
        {
            if (row != null)
                row.SetActive(visible);
        }
    }
}
