using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Resolves the target of one patch class from its <c>Prepare()</c>. A target that a game
    /// update removed or renamed (or made ambiguous) logs one warning naming it and what stops
    /// working, and the class returns false from <c>Prepare</c>: Harmony then skips only that
    /// class instead of PatchAll throwing and leaving every later patch class unapplied.
    /// </summary>
    internal static class SulfurPatchTargets
    {
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);

        public static MethodBase Resolve(Func<MethodBase> lookup, string displayName, string effectWhenMissing)
        {
            MethodBase method = null;
            Exception failure = null;

            try
            {
                method = lookup != null ? lookup() : null;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            // Harmony may call Prepare more than once per class; warn once.
            if (method == null && warned.Add(displayName ?? "") && Plugin.Log != null)
            {
                Plugin.Log.LogWarning(
                    "SULFUR Native UI: " + displayName + " not found" +
                    (failure != null ? " (" + failure.GetType().Name + ": " + failure.Message + ")" : "") +
                    "; that patch is skipped, so " + effectWhenMissing + ". The game may have changed.");
            }

            return method;
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenShowPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "Show"),
                "OptionsScreen.Show",
                "the options screen state is not reset before it opens");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static void Prefix(OptionsScreen __instance)
        {
            SulfurOptionsScreenBridge.ResetOptionsScreenStateBeforeShow(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenHidePatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "Hide"),
                "OptionsScreen.Hide",
                "custom page contexts stay alive after the screen closes");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        // Prefix: Hide has no early return, and a later failure inside it (resolution,
        // modifiers sync) must not keep a hidden page's context alive.
        private static void Prefix(OptionsScreen __instance)
        {
            SulfurOptionsScreenBridge.OnOptionsScreenHiding(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenSetupMenuPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "SetupMenu"),
                "OptionsScreen.SetupMenu",
                "custom options pages do not appear in the menu");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static void Postfix(OptionsScreen __instance)
        {
            SulfurOptionsScreenBridge.InjectCustomCategories(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenSetCategoryPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "SetCategory"),
                "OptionsScreen.SetCategory",
                "custom options pages cannot be shown");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(OptionsScreen __instance, OptionsScreenCategory category, bool selectFirst)
        {
            return !SulfurOptionsScreenBridge.TryShowCustomPage(__instance, category, selectFirst);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateVerticalPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "NavigateVertical"),
                "OptionsScreen.NavigateVertical",
                "arrow keys move the selection while typing in a text input");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(OptionsScreen __instance, int delta)
        {
            return !SulfurOptionsScreenBridge.TryNavigateVertical(__instance, delta);
        }
    }

    /// <summary>
    /// <c>OptionsScreen.SelectIndex</c> is the game's only row-selection write. On a custom
    /// page the requested index is corrected to a visible row first (see
    /// <see cref="SulfurOptionsScreenBridge.PrepareSelectIndex"/>); native pages run unchanged.
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurOptionsScreenSelectIndexPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "SelectIndex"),
                "OptionsScreen.SelectIndex",
                "keyboard navigation may select hidden rows on custom pages");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(OptionsScreen __instance, ref int index)
        {
            return SulfurOptionsScreenBridge.PrepareSelectIndex(__instance, ref index);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateHorizontalPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "NavigateHorizontal"),
                "OptionsScreen.NavigateHorizontal",
                "left/right does nothing on library sliders, cycles and button rows");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(OptionsScreen __instance, int delta)
        {
            return !SulfurOptionsScreenBridge.TryNavigateHorizontal(__instance, delta);
        }
    }

    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateCancelPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "NavigateCancel"),
                "OptionsScreen.NavigateCancel",
                "Escape in a text input closes the page instead of leaving the input");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(InputAction.CallbackContext context)
        {
            if (SulfurOptionsScreenBridge.TryHandleTextInputCancel(context))
                return false;

            SulfurOptionsScreenBridge.BeginGameNavigation();
            return true;
        }

        private static void Finalizer()
        {
            SulfurOptionsScreenBridge.EndGameNavigation();
        }
    }

    /// <summary>
    /// UI Submit (Space / Enter). Swallowed while a library text input has focus, like
    /// Navigate and Cancel; otherwise marks the game's own navigation for the SetCategory
    /// prefix (see <c>SulfurOptionsScreenBridge.TryShowCustomPage</c>).
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurOptionsScreenNavigateUsePatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "NavigateUse"),
                "OptionsScreen.NavigateUse",
                "Enter/Space in a text input also activates the selected row");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix()
        {
            if (SulfurOptionsScreenBridge.ShouldSwallowSubmit())
                return false;

            SulfurOptionsScreenBridge.BeginGameNavigation();
            return true;
        }

        private static void Finalizer()
        {
            SulfurOptionsScreenBridge.EndGameNavigation();
        }
    }

    /// <summary>
    /// <c>OptionsScreen.DoAutoScrolling</c> measures the selected row as a direct child of
    /// the scroll content; on a custom page nested rows are measured correctly instead (see
    /// <see cref="SulfurOptionsScreenBridge.PrepareAutoScroll"/>).
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurOptionsScreenDoAutoScrollingPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreen), "DoAutoScrolling"),
                "OptionsScreen.DoAutoScrolling",
                "keyboard/gamepad navigation on custom pages may not scroll nested rows into view");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static bool Prefix(OptionsScreen __instance, int indexChange, RectTransform currentOption)
        {
            return SulfurOptionsScreenBridge.PrepareAutoScroll(__instance, indexChange, currentOption);
        }
    }

    /// <summary>
    /// <c>OptionsScreenCategory.UpdateLabel</c> is the only game code that writes a category
    /// label (from <c>SetToCategory</c> and the game's language-change handler). Re-apply a
    /// custom page's name right after it, instead of checking the label every frame.
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurOptionsScreenCategoryUpdateLabelPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method(typeof(OptionsScreenCategory), "UpdateLabel"),
                "OptionsScreenCategory.UpdateLabel",
                "custom category labels may show the game's category name after a language change");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static void Postfix(OptionsScreenCategory __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                SulfurCustomCategoryMarker marker = __instance.GetComponent<SulfurCustomCategoryMarker>();
                if (marker != null)
                    marker.ForceApplyLabel();
            }
            catch (Exception ex)
            {
                // Never break the game's label update or the other language-change handlers.
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Failed to restore a custom options category label: " + ex);
            }
        }
    }

    /// <summary>
    /// PauseMenu.OnEnable selects the resume button (the gamepad's starting point). That
    /// selection is kept. Only a selection still left on the options screen (a library text
    /// input, or anything else in the OptionsScreen hierarchy, which OptionsScreen.Hide
    /// deactivates right after enabling the pause menu) is cleared, so a hidden input cannot
    /// keep the focus or the keyboard.
    /// </summary>
    [HarmonyPatch]
    internal static class SulfurPauseMenuOnEnableClearSelectionPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = SulfurPatchTargets.Resolve(
                () => AccessTools.Method("PerfectRandom.Sulfur.Core.UI.PauseMenu:OnEnable"),
                "PauseMenu.OnEnable",
                "a hidden text input may keep the keyboard focus after the options screen closes");
            return target != null;
        }

        private static MethodBase TargetMethod()
        {
            return target;
        }

        private static void Postfix()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected == null)
                return;

            if (selected.GetComponent<SulfurTextInputBinding>() != null || IsUnderOptionsScreen(selected.transform))
                eventSystem.SetSelectedGameObject(null);
        }

        // Walks the parents explicitly: GetComponentInParent skips inactive objects, and the
        // screen may already be inactive when the pause menu is enabled by another path.
        private static bool IsUnderOptionsScreen(Transform transform)
        {
            for (Transform t = transform; t != null; t = t.parent)
            {
                if (t.GetComponent<OptionsScreen>() != null)
                    return true;
            }

            return false;
        }
    }
}
