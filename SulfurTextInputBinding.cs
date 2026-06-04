using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

        public static bool CancelActiveInput()
        {
            if (activeBinding == null || activeBinding.InputField == null)
                return false;

            TMP_InputField input = activeBinding.InputField;

            if (!input.isFocused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != input.gameObject)
                return false;

            input.DeactivateInputField(false);

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == input.gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            return true;
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
