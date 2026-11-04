using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>Which control a setting row (<c>AddSetting*Ex</c>) was built with.</summary>
    internal enum SulfurSettingControlKind
    {
        None,
        Toggle,
        Text,
        Number,
        Cycle
    }

    /// <summary>
    /// The pieces of one setting row that its <see cref="SulfurSettingHandle"/> changes,
    /// kept when the row is built instead of being searched for later. Every field may be
    /// null (the row was built by a closed context, or the piece was not requested).
    /// </summary>
    internal sealed class SulfurSettingParts
    {
        public SulfurSettingControlKind Kind;

        /// <summary>Toggle rows: the game's checkbox.</summary>
        public Toggle Toggle;

        /// <summary>Toggle and cycle rows: the binding that owns the value listener and the lock state.</summary>
        public SulfurOptionBinding Binding;

        /// <summary>Text rows: the input field.</summary>
        public TMP_InputField TextInput;

        /// <summary>Number rows: the field's value model (parse, clamp, last accepted value).</summary>
        public SulfurNumberInput Number;

        public GameObject BadgeRow;

        /// <summary>The message line; always built for a handle, inactive while the message is empty.</summary>
        public GameObject MessageRow;

        /// <summary>The Default button row; built whenever the row has an OnDefault action.</summary>
        public GameObject DefaultButtonRow;
    }
}
