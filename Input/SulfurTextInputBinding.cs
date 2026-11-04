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

        // Keyboard/gamepad start of editing with the whole text selected: onFocusSelectAll is
        // switched on until TMP has applied it, then back to its own value (see
        // ActivateFromNavigation).
        private bool selectAllRestorePending;
        private bool savedOnFocusSelectAll;
        private int selectAllRequestFrame;

        // TMP activates a field in its LateUpdate after ActivateInputField, and the order of
        // its LateUpdate and this one is not defined: allow a few frames before giving up on
        // an activation TMP refused (then the flag is restored all the same).
        private const int SelectAllRestoreFrames = 3;

        private void LateUpdate()
        {
            bool focused = InputField != null && InputField.isFocused;

            if (wasFocused && !focused)
                lastFocusLostFrame = Time.frameCount;

            wasFocused = focused;

            if (selectAllRestorePending && (focused || Time.frameCount - selectAllRequestFrame > SelectAllRestoreFrames))
                RestoreOnFocusSelectAll();
        }

        private void OnDisable()
        {
            RestoreOnFocusSelectAll();
        }

        /// <summary>
        /// Starts editing from the row's navigation proxy (keyboard Enter / gamepad A). With
        /// <paramref name="selectAll"/> the whole text is selected, so typing replaces the
        /// value (number fields); otherwise the caret goes to the end of the text (text
        /// fields). Mouse clicks do not come here and keep TMP's own handling: the caret
        /// lands where the field was clicked, because the library builds its fields with
        /// onFocusSelectAll off.
        /// </summary>
        /// <remarks>
        /// TMP applies the activation in its next LateUpdate (ActivateInputField only sets a
        /// flag; ActivateInputFieldInternal then calls OnFocus, which selects all when
        /// onFocusSelectAll is on, and otherwise leaves the caret where it was: at 0 for a
        /// field never edited). Selecting all goes through that same OnFocus, so TMP also
        /// sets its select-all state (the first key typed replaces the whole text). The
        /// caret position is set before the activation, which keeps it.
        /// </remarks>
        public void ActivateFromNavigation(bool selectAll)
        {
            TMP_InputField field = InputField;
            if (field == null)
                return;

            if (selectAll)
            {
                if (!selectAllRestorePending)
                    savedOnFocusSelectAll = field.onFocusSelectAll;

                field.onFocusSelectAll = true;
                selectAllRestorePending = true;
                selectAllRequestFrame = Time.frameCount;
            }
            else
            {
                field.stringPosition = (field.text ?? "").Length;
            }

            field.ActivateInputField();
        }

        private void RestoreOnFocusSelectAll()
        {
            if (!selectAllRestorePending)
                return;

            selectAllRestorePending = false;

            if (InputField != null)
                InputField.onFocusSelectAll = savedOnFocusSelectAll;
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
