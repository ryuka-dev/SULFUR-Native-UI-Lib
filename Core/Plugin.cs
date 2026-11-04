using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Ryuka.Sulfur.NativeUI
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ryuka.sulfur.nativeui";
        public const string PluginName = "SULFUR Native UI Lib";
        public const string PluginVersion = "0.11.0";

        internal static ManualLogSource Log { get; private set; }

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            // The library's own UI text (lang/<code>.json next to the dll).
            SulfurLocalization.LoadPluginLocalization(PluginGuid, Info.Location);

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            // Follow the game's per-role font cache (language changes). Patched by hand,
            // outside PatchAll, so a game build without the mechanism degrades to TMP's
            // default font instead of aborting the library's other patches.
            SulfurGameFonts.Install(harmony);

            // Follow the game's language commits (same reasoning: patched by hand). The
            // startup language is picked up from Update, see below.
            SulfurGameLanguage.Install(harmony);
            SulfurLanguage.Initialize();

            // Register the CJK/non-Latin glyph fallback early so all TMP text (ours and
            // the game's) can render missing characters. Best-effort + idempotent: if TMP
            // isn't ready yet, FindSampleText retries when a page is built.
            SulfurFontFallback.EnsureRegistered();

            Logger.LogInfo("SULFUR Native UI Lib loaded.");
        }

        private void Update()
        {
            // Startup convergence only: the game sets its startup language without raising
            // any event, so check each frame until it is known, then never again (later
            // changes arrive through the ChangeLanguage hook). After that this is one bool read.
            if (!SulfurLanguage.IsReady)
                SulfurLanguage.ConvergeInitial();
        }

        private void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }
    }
}