using System.Reflection;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Library rows are clones of the game's option prefabs and never get a
    /// <see cref="PlayerSetting"/>, but <c>OptionsScreenOption.OnEnable</c> still subscribes
    /// <c>UpdateTexts</c> to <c>AsyncAssetLoading.onLanguageChange</c>, and that method
    /// dereferences the setting. Left alone it throws, which aborts the rest of the game's
    /// handlers and the library's language hook. Library rows set their own text, so the
    /// refresh is skipped for them; rows that have a setting run it unchanged.
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurOptionTextsGuardPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreenOption), "UpdateTexts"),
                "OptionsScreenOption.UpdateTexts",
                "a language change while a custom page is open can leave the library on the old language");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(PlayerSetting ___setting)
        {
            return ___setting != null;
        }
    }
}
