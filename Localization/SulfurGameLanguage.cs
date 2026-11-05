using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using I2.Loc;
using PerfectRandom.Sulfur.Core;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// The only place that touches the game's language state (I2 Localization and
    /// <c>AsyncAssetLoading</c>). <see cref="SulfurLanguage"/> owns the library's view of the
    /// language; this adapter only reads the game and reports when the game commits a change.
    ///
    /// Game mechanism (PerfectRandom.Sulfur.Core, verified on 1.0.4):
    ///  - Startup: <c>AsyncAssetLoading.LoadLanguage</c> adds the language sources and sets
    ///    <c>LocalizationManager.CurrentLanguage</c> without raising any event. It runs inside
    ///    <c>AsyncAssetLoading.Start</c>, which sets <c>loadingDone</c> afterwards.
    ///  - Changes: every change, including the font-set path <c>ExecuteChangeLanguage</c>, ends
    ///    in <c>AsyncAssetLoading.ChangeLanguage(string)</c>, which sets
    ///    <c>LocalizationManager.CurrentLanguage</c> and then invokes <c>onLanguageChange</c>.
    ///    A postfix on that method therefore runs after every game handler.
    ///
    /// Typed access is wrapped so a game update that removes or renames any of these fails
    /// once, deterministically, with one log line: the library then stays in English.
    /// </summary>
    internal static class SulfurGameLanguage
    {
        private static bool unsupported;

        /// <summary>
        /// Raised after the game committed a language change (from the
        /// <c>ChangeLanguage(string)</c> postfix). Fired even when nothing changed; the
        /// listener compares.
        /// </summary>
        internal static event Action LanguageCommitted;

        /// <summary>True once reading the game's language state failed.</summary>
        public static bool Unsupported
        {
            get { return unsupported; }
        }

        /// <summary>Hook the game's language commit point. Called once from Plugin.Awake.</summary>
        public static void Install(Harmony harmony)
        {
            if (harmony == null)
                return;

            try
            {
                InstallCore(harmony);
                Plugin.Log?.LogInfo("Language: hooked AsyncAssetLoading.ChangeLanguage(string).");
            }
            catch (Exception e)
            {
                // Reading still works; only in-game changes go unnoticed.
                Plugin.Log?.LogWarning(
                    "Language: hooking AsyncAssetLoading.ChangeLanguage(string) failed (" + e.Message +
                    "). The startup language is still detected, but later language changes will not be followed.");
            }
        }

        /// <summary>
        /// Reads the game's current language once the game has finished its startup load.
        /// Returns false while the game is still loading (or when this game build lacks the
        /// mechanism, see <see cref="Unsupported"/>).
        /// </summary>
        public static bool TryRead(out string code, out bool rightToLeft)
        {
            code = null;
            rightToLeft = false;

            if (unsupported)
                return false;

            try
            {
                return TryReadCore(out code, out rightToLeft);
            }
            catch (Exception e)
            {
                MarkUnsupported("reading the game's language failed: " + e.Message);
                code = null;
                rightToLeft = false;
                return false;
            }
        }

        /// <summary>The game's RTL shaping for <paramref name="text"/>; unchanged on failure.</summary>
        public static string FixRtl(string text)
        {
            if (unsupported || string.IsNullOrEmpty(text))
                return text;

            try
            {
                return FixRtlCore(text);
            }
            catch (Exception e)
            {
                MarkUnsupported("RTL shaping failed: " + e.Message);
                return text;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InstallCore(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                typeof(AsyncAssetLoading),
                nameof(AsyncAssetLoading.ChangeLanguage),
                new[] { typeof(string) });

            if (target == null)
                throw new MissingMethodException("AsyncAssetLoading", "ChangeLanguage(string)");

            MethodInfo postfix = AccessTools.Method(typeof(SulfurGameLanguage), nameof(ChangeLanguagePostfix));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryReadCore(out string code, out bool rightToLeft)
        {
            code = null;
            rightToLeft = false;

            AsyncAssetLoading loading = AsyncAssetLoading.Instance;
            if (loading == null || !loading.loadingDone)
                return false; // the game has not set its startup language yet

            if (LocalizationManager.Sources == null || LocalizationManager.Sources.Count == 0)
                return false;

            string current = LocalizationManager.CurrentLanguageCode;
            if (string.IsNullOrWhiteSpace(current))
                return false;

            // The Traditional Chinese font set is its own I2 source, coded "zh". At startup it
            // comes before the main source and wins the code lookup; after an in-game change it
            // comes after it and the code is "zh-TW". Same language, so report one code.
            code = current.Trim();
            if (string.Equals(code, "zh", StringComparison.OrdinalIgnoreCase))
                code = "zh-TW";

            rightToLeft = LocalizationManager.IsRight2Left;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string FixRtlCore(string text)
        {
            return LocalizationManager.FixRTL_IfNeeded(text);
        }

        private static void ChangeLanguagePostfix()
        {
            Action handler = LanguageCommitted;
            if (handler == null)
                return;

            try
            {
                handler();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Language: handling a language change failed: " + e);
            }
        }

        private static void MarkUnsupported(string reason)
        {
            if (unsupported)
                return;

            unsupported = true;
            Plugin.Log?.LogWarning(
                "Language detection is not supported by this game version (" + reason +
                "). Library text stays in English.");
        }
    }
}
