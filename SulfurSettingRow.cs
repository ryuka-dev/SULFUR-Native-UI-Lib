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

        public string DirtyText = "Dirty";
        public string CleanText = "Clean";
        public string RestartRequiredText = "Restart Required";
        public string LiveApplyText = "Live Apply";
        public string AdvancedText = "Advanced";
        public string HiddenText = "Hidden";
        public string DangerousText = "Dangerous";

        public string[] ExtraBadges;

        public bool ShowCleanBadge = true;
        public bool ShowDefaultButton = true;
        public string DefaultButtonText = "Default";
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
