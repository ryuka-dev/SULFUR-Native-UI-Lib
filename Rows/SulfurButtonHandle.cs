using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a single button created by
    /// <see cref="SulfurOptionsContext.AddButtonRow(SulfurButton[])"/>. Lets a page
    /// change the button's label, interactable state and visibility in place,
    /// without rebuilding the whole options page (e.g. Create/Join mutual exclusion).
    /// </summary>
    public sealed class SulfurButtonHandle
    {
        private readonly GameObject root;
        private readonly Button button;
        private readonly TextMeshProUGUI label;
        private readonly Color enabledTextColor;
        private readonly bool hasEnabledTextColor;

        internal SulfurButtonHandle(GameObject root, Button button, TextMeshProUGUI label)
        {
            this.root = root;
            this.button = button;
            this.label = label;

            if (label != null)
            {
                enabledTextColor = label.color;
                hasEnabledTextColor = true;
            }
        }

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text ?? "";
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;

            // ColorTint only fades the background graphic; dim the label too so a
            // disabled button reads as clearly inactive.
            if (label != null && hasEnabledTextColor)
            {
                Color c = enabledTextColor;
                label.color = interactable ? c : new Color(c.r, c.g, c.b, c.a * 0.4f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }
    }
}
