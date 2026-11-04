using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Works around a game bug (1.0.4): <c>OptionsScreenOption.OnPointerEnter</c> and
    /// <c>OnPointerExit</c> read <c>GameManager.Instance.InputReader.GamepadUsed</c> without a
    /// null check, and <c>GameManager</c> does not exist at the main menu, so every mouse move
    /// over an option row in the main menu's Options screen logs a NullReferenceException and
    /// the row's hover highlight is never set. This affects the game's own rows as well as the
    /// library's.
    ///
    /// <para><b>Built to stay harmless once the game fixes it.</b> These are Harmony
    /// <i>finalizers</i>: they run after the game's method and act only when that method
    /// actually threw, and only for exactly this failure (a NullReferenceException while
    /// <c>GameManager.Instance</c> is null). They then do what the method does for a mouse
    /// pointer, which is also how the game's own <c>PauseMenuButton.OnPointerEnter</c> treats a
    /// missing GameManager: set the hover state. Any other exception is passed on unchanged.
    /// When a fixed game version no longer throws, the finalizers have nothing to do and the
    /// game's new code runs untouched. If the methods disappear, the patches are skipped.</para>
    ///
    /// <para>Delete this file when the fix has shipped in the game.</para>
    /// </summary>
    internal static class SulfurMainMenuHoverGuard
    {
        private static bool reported;

        internal static Exception Handle(Exception exception, OptionsScreenOption option, bool hovered)
        {
            if (exception == null)
                return null;

            if (!(exception is NullReferenceException) || !IsGameManagerMissing())
                return exception;

            try
            {
                if (option != null)
                    option.SetHovered(hovered);
            }
            catch (Exception)
            {
                // Hover is cosmetic; never turn the suppressed exception into another one.
            }

            if (!reported)
            {
                reported = true;
                Plugin.Log?.LogInfo(
                    "Suppressing the game's main-menu NullReferenceException in OptionsScreenOption." +
                    (hovered ? "OnPointerEnter" : "OnPointerExit") +
                    " (GameManager not created yet). Logged once; stops by itself once the game fixes it.");
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsGameManagerMissing()
        {
            try
            {
                return GameManager.Instance == null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionPointerEnterGuardPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreenOption), "OnPointerEnter"),
                "OptionsScreenOption.OnPointerEnter",
                "the game's main-menu hover NullReferenceException is not suppressed");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static Exception Finalizer(Exception __exception, OptionsScreenOption __instance)
        {
            return SulfurMainMenuHoverGuard.Handle(__exception, __instance, true);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionPointerExitGuardPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreenOption), "OnPointerExit"),
                "OptionsScreenOption.OnPointerExit",
                "the game's main-menu hover NullReferenceException is not suppressed");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static Exception Finalizer(Exception __exception, OptionsScreenOption __instance)
        {
            return SulfurMainMenuHoverGuard.Handle(__exception, __instance, false);
        }
    }
}
