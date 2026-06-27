using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurInputVisualState : MonoBehaviour
    {
        private TMP_InputField input;
        private Image background;

        private Color normalColor;
        private Color focusedColor;
        private bool lastFocused;

        public void Configure(TMP_InputField sourceInput, Image sourceBackground)
        {
            input = sourceInput;
            background = sourceBackground;

            if (background != null)
            {
                normalColor = background.color;
                focusedColor = new Color(
                    Mathf.Min(1f, normalColor.r + 0.08f),
                    Mathf.Min(1f, normalColor.g + 0.08f),
                    Mathf.Min(1f, normalColor.b + 0.08f),
                    Mathf.Clamp01(normalColor.a + 0.09f));
            }

            ApplyState(true);
        }

        private void LateUpdate()
        {
            ApplyState(false);
        }

        private void ApplyState(bool force)
        {
            if (input == null || background == null)
                return;

            bool focused = input.isFocused;

            if (!force && focused == lastFocused)
                return;

            lastFocused = focused;
            background.color = focused ? focusedColor : normalColor;
        }
    }
}
