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
        public const string PluginVersion = "0.7.1";

        internal static ManualLogSource Log { get; private set; }

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

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