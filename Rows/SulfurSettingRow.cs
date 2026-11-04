using System;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Describes shared metadata for one native setting row.
    /// The library only uses this for UI presentation.
    /// It does not know about BepInEx ConfigEntry, ConfigFile, dirty saving, or backups.
    /// </summary>
    public sealed class SulfurSettingRow
    {
        public string Label;
        public string Description;

        public bool IsDirty;
        public bool RequiresRestart;
        public bool LiveApply;
        public bool Advanced;
        public bool Hidden;
        public bool Dangerous;

        // Built-in English defaults of the text fields below. A field still holding its
        // default is displayed in the player's language (see SulfurUiText.ForSettingRow); a
        // value the mod set itself is displayed as is.
        internal const string BuiltInDirtyText = "Dirty";
        internal const string BuiltInCleanText = "Clean";
        internal const string BuiltInRestartRequiredText = "Restart Required";
        internal const string BuiltInLiveApplyText = "Live Apply";
        internal const string BuiltInAdvancedText = "Advanced";
        internal const string BuiltInHiddenText = "Hidden";
        internal const string BuiltInDangerousText = "Dangerous";
        internal const string BuiltInDefaultButtonText = "Default";

        public string DirtyText = BuiltInDirtyText;
        public string CleanText = BuiltInCleanText;
        public string RestartRequiredText = BuiltInRestartRequiredText;
        public string LiveApplyText = BuiltInLiveApplyText;
        public string AdvancedText = BuiltInAdvancedText;
        public string HiddenText = BuiltInHiddenText;
        public string DangerousText = BuiltInDangerousText;

        public string[] ExtraBadges;

        public bool ShowCleanBadge = true;
        public bool ShowDefaultButton = true;
        public string DefaultButtonText = BuiltInDefaultButtonText;
        public Action OnDefault;

        public string Message;
        public SulfurMessageKind MessageKind = SulfurMessageKind.Info;

        /// <summary>
        /// Visual indentation level for child setting rows.
        /// Use 1 for entries inside a section, 2 for deeper nesting.
        /// </summary>
        public int IndentLevel = 0;

        /// <summary>
        /// Indent size per level in pixels.
        /// </summary>
        public float IndentPixels = 28f;
    }
}
