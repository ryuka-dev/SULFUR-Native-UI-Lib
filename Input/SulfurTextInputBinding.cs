using System;
using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurTextInputBinding : MonoBehaviour
    {
        private static SulfurTextInputBinding activeBinding;

        public TMP_InputField InputField { get; private set; }

        public static bool IsAnyInputFocused
        {
            get
            {
                return activeBinding != null &&
                       activeBinding.InputField != null &&
                       activeBinding.InputField.isFocused;
            }
        }

        // Frame on which the field last stopped being focused (Enter, Escape, a click
        // elsewhere); -1 before that.
        private int lastFocusLostFrame = -1;
        private bool wasFocused;

        /// <summary>
        /// True on the frame the field stopped being focused and the next one. A keyboard Use
        /// in that window is the key press that ended editing (the game's Submit callback and
        /// TMP's own key handling run at different points of the frame), not a new request.
        /// </summary>
        public bool LeftFocusRecently
        {
            get { return lastFocusLostFrame >= 0 && Time.frameCount - lastFocusLostFrame <= 1; }
        }

        private void LateUpdate()
        {
            bool focused = InputField != null && InputField.isFocused;

            if (wasFocused && !focused)
                lastFocusLostFrame = Time.frameCount;

            wasFocused = focused;
        }

        public void Configure(TMP_InputField input)
        {
            InputField = input;

            if (InputField == null)
                return;

            InputField.onSelect.AddListener(OnSelected);
            InputField.onDeselect.AddListener(OnDeselected);
        }

        private void OnDestroy()
        {
            if (activeBinding == this)
                activeBinding = null;

            if (InputField != null)
            {
                InputField.onSelect.RemoveListener(OnSelected);
                InputField.onDeselect.RemoveListener(OnDeselected);
            }
        }

        private void OnSelected(string value)
        {
            activeBinding = this;
        }

        private void OnDeselected(string value)
        {
            if (activeBinding == this)
                activeBinding = null;
        }
    }
}
