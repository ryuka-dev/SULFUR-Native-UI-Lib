namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// The library's own user-visible text (not text passed in by callers). One source of
    /// truth: every site that renders one of these strings reads it here.
    ///
    /// Values come from the library's <c>lang/&lt;code&gt;.json</c>; the English fallbacks below
    /// must stay identical to <c>lang/en.json</c>. Resolved on each call, which is when a row
    /// is built, so a rebuilt page follows the current language.
    /// </summary>
    internal static class SulfurUiText
    {
        public static string WarningPrefix
        {
            get { return L("ui.message.warning_prefix", "WARNING: "); }
        }

        public static string ErrorPrefix
        {
            get { return L("ui.message.error_prefix", "ERROR: "); }
        }

        public static string SuccessPrefix
        {
            get { return L("ui.message.success_prefix", "OK: "); }
        }

        /// <summary>Placeholder shown in an empty text input.</summary>
        public static string TextInputPlaceholder
        {
            get { return L("ui.input.placeholder", "Enter text..."); }
        }

        /// <summary>Placeholder shown in an empty number input.</summary>
        public static string NumberInputPlaceholder
        {
            get { return L("ui.input.number_placeholder", "Enter number..."); }
        }

        /// <summary>Shown on a custom options page that added no rows.</summary>
        public static string NoOptions
        {
            get { return L("ui.page.no_options", "No options."); }
        }

        /// <summary>Shown instead of a custom options page whose build threw or that is no longer registered.</summary>
        public static string PageLoadFailed
        {
            get { return L("ui.page.load_failed", "This page failed to load. See the BepInEx log."); }
        }

        /// <summary>Label of a small "reset to default" button when the caller supplied none.</summary>
        public static string DefaultButton
        {
            get { return L("ui.button.default", "Default"); }
        }

        /// <summary>Label of the custom-page footer's primary button when the caller supplied none.</summary>
        public static string FooterApply
        {
            get { return L("ui.footer.apply", "Apply"); }
        }

        /// <summary>Tag shown at the right of a plugin-style foldout header.</summary>
        public static string ModTag
        {
            get { return L("ui.foldout.mod_tag", "MOD"); }
        }

        /// <summary>Status badge fallback when a row has no dirty text of its own.</summary>
        public static string DirtyBadge
        {
            get { return L("ui.badge.dirty", "Dirty"); }
        }

        /// <summary>Status badge fallback when a row has no clean text of its own.</summary>
        public static string CleanBadge
        {
            get { return L("ui.badge.clean", "Clean"); }
        }

        /// <summary>Setting-row badge for <see cref="SulfurSettingRow.RequiresRestart"/>.</summary>
        public static string RestartRequiredBadge
        {
            get { return L("ui.badge.restart_required", "Restart Required"); }
        }

        /// <summary>Setting-row badge for <see cref="SulfurSettingRow.LiveApply"/>.</summary>
        public static string LiveApplyBadge
        {
            get { return L("ui.badge.live_apply", "Live Apply"); }
        }

        /// <summary>Setting-row badge for <see cref="SulfurSettingRow.Advanced"/>.</summary>
        public static string AdvancedBadge
        {
            get { return L("ui.badge.advanced", "Advanced"); }
        }

        /// <summary>Setting-row badge for <see cref="SulfurSettingRow.Hidden"/>.</summary>
        public static string HiddenBadge
        {
            get { return L("ui.badge.hidden", "Hidden"); }
        }

        /// <summary>Setting-row badge for <see cref="SulfurSettingRow.Dangerous"/>.</summary>
        public static string DangerousBadge
        {
            get { return L("ui.badge.dangerous", "Dangerous"); }
        }

        /// <summary>
        /// Display text for one of <see cref="SulfurSettingRow"/>'s text fields: when the field
        /// still holds its built-in English default (<paramref name="builtInDefault"/>, compared
        /// exactly) the localized text is shown, otherwise the mod's own value unchanged.
        /// </summary>
        public static string ForSettingRow(string value, string builtInDefault, string localized)
        {
            return string.Equals(value, builtInDefault, System.StringComparison.Ordinal) ? localized : value;
        }

        public static string SettingDirtyText(string value)
        {
            return ForSettingRow(value, SulfurSettingRow.BuiltInDirtyText, DirtyBadge);
        }

        public static string SettingCleanText(string value)
        {
            return ForSettingRow(value, SulfurSettingRow.BuiltInCleanText, CleanBadge);
        }

        /// <summary>Prefix shown before a row message of the given kind ("" for Info).</summary>
        public static string GetMessagePrefix(SulfurMessageKind kind)
        {
            switch (kind)
            {
                case SulfurMessageKind.Warning:
                    return WarningPrefix;
                case SulfurMessageKind.Error:
                    return ErrorPrefix;
                case SulfurMessageKind.Success:
                    return SuccessPrefix;
                default:
                    return "";
            }
        }

        private static string L(string key, string fallback)
        {
            return SulfurLanguage.FixRtl(SulfurLocalization.Get(Plugin.PluginGuid, key, fallback));
        }
    }
}
