using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a native slider row created by
    /// <see cref="SulfurOptionsContext.AddSliderRow(string, float, float, float, float, System.Action{float})"/>.
    /// Same shape as <see cref="SulfurToggleHandle"/>.
    /// </summary>
    public sealed class SulfurSliderHandle
    {
        private readonly SulfurOptionRowParts parts;
        private readonly Slider slider;
        private readonly TextMeshProUGUI valueText;

        internal SulfurSliderHandle(SulfurOptionRowParts parts, Slider slider, TextMeshProUGUI valueText)
        {
            this.parts = parts;
            this.slider = slider;
            this.valueText = valueText;
        }

        /// <summary>True while the row exists and the page that built it is on screen.</summary>
        public bool IsAlive
        {
            get { return parts != null && parts.IsAlive; }
        }

        /// <summary>The slider value shown now (0 when the row is gone).</summary>
        public float Value
        {
            get { return slider != null ? slider.value : 0f; }
        }

        /// <summary>True while <see cref="SetLocked"/>(true) is in effect.</summary>
        public bool IsLocked
        {
            get { return parts != null && parts.IsLocked; }
        }

        /// <summary>
        /// Shows <paramref name="value"/> (clamped to the row's range, not snapped to its step)
        /// without calling the row's onChanged.
        /// </summary>
        public void SetValueWithoutNotify(float value)
        {
            if (slider == null)
                return;

            SulfurOptionBinding binding = parts != null ? parts.Binding : null;
            if (binding != null)
                binding.SetSliderValueWithoutNotify(value);
            else
                slider.SetValueWithoutNotify(value);

            SulfurOptionsContext.SetSliderText(valueText, slider.value);
        }

        /// <summary>
        /// Locks the row: dimmed as a whole, no mouse drag, and keyboard/gamepad left/right
        /// does nothing. <see cref="SetValueWithoutNotify"/> still works.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (parts != null)
                parts.SetLocked(locked);
        }

        /// <summary>Shows or hides the row and its description.</summary>
        public void SetVisible(bool visible)
        {
            if (parts != null)
                parts.SetVisible(visible);
        }

        public void SetLabel(string label)
        {
            if (parts != null)
                parts.SetLabel(label);
        }
    }
}
