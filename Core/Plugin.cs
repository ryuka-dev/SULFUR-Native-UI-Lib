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
        public const string PluginVersion = "0.10.1";

        internal static ManualLogSource Log { get; private set; }

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            // Register the CJK/non-Latin glyph fallback early so all TMP text (ours and
            // the game's) can render missing characters. Best-effort + idempotent: if TMP
            // isn't ready yet, FindSampleText retries when a page is built.
            SulfurFontFallback.EnsureRegistered();

            Logger.LogInfo("SULFUR Native UI Lib loaded.");
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