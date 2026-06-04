using System.Reflection;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Ryuka.Sulfur.NativeUI
{
    [HarmonyPatch]
    internal static class SulfurOptionsScreenShowPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "Show");
        }

        private static void Prefix(OptionsScreen __instance)
        {
            SulfurOptionsScreenBridge.ResetOptionsScreenStateBeforeShow(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenSetupMenuPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "SetupMenu");
        }

        private static void Postfix(OptionsScreen __instance)
        {
            SulfurOptionsScreenBridge.InjectCustomCategories(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenSetCategoryPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "SetCategory");
        }

        private static bool Prefix(OptionsScreen __instance, OptionsScreenCategory category, bool selectFirst)
        {
            return !SulfurOptionsScreenBridge.TryShowCustomPage(__instance, category, selectFirst);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateVerticalPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "NavigateVertical");
        }

        private static bool Prefix(OptionsScreen __instance, int delta)
        {
            return !SulfurOptionsScreenBridge.TryNavigateVertical(__instance, delta);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateHorizontalPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "NavigateHorizontal");
        }

        private static bool Prefix(OptionsScreen __instance, int delta)
        {
            return !SulfurOptionsScreenBridge.TryNavigateHorizontal(__instance, delta);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateCancelPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OptionsScreen), "NavigateCancel");
        }

        private static bool Prefix(InputAction.CallbackContext context)
        {
            return !SulfurOptionsScreenBridge.TryHandleTextInputCancel(context);
        }
    }

    [HarmonyPatch]
    internal static class SulfurPauseMenuOnEnableClearSelectionPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("PerfectRandom.Sulfur.Core.UI.PauseMenu:OnEnable");
        }

        private static void Postfix()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
