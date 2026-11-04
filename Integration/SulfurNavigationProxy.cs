using System;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Keyboard/gamepad stand-in for a library widget that is not an
    /// <see cref="OptionsScreenOption"/> (foldout header, small button, button row, text
    /// input, footer button).
    /// </summary>
    /// <remarks>
    /// OptionsScreen.Show turns EventSystem navigation off, so the game's own options list is
    /// the only thing arrow keys and Submit can reach. The proxy is a clone of the game's
    /// button-option prefab placed as the first child of the widget's row, stretched over it
    /// and ignored by layout: once registered in that list it is selected, highlighted
    /// (OptionsScreenOption.Update turns its border white) and used by the game's own code,
    /// and its Use is forwarded to the widget.
    /// <para>Everything of the clone except that border is switched off, so an unselected row
    /// looks exactly as before. None of its graphics is a raycast target and a CanvasGroup
    /// stops raycasts for the whole subtree: the proxy never receives pointer events. The
    /// widget keeps all mouse input, a click is never delivered twice (the game's
    /// OnPointerClick would invoke onButtonPressed a second time), and the game's
    /// OnPointerEnter, which throws at the main menu where GameManager.Instance is null, is
    /// never called for it. The trade-off is that hovering a proxied row with the mouse
    /// does not draw the game's border; the widget shows its own hover state instead.</para>
    /// </remarks>
    internal static class SulfurNavigationProxy
    {
        internal const string ObjectName = "SULFUR_NavProxy";

        private static bool missingBorderLogged;

        /// <summary>
        /// Creates the proxy under <paramref name="host"/> and wires its Use to
        /// <paramref name="onUse"/>. The caller registers it in the options list. Returns null
        /// when the game's button prefab is unavailable (the widget then stays mouse-only).
        /// </summary>
        public static OptionsScreenOption Create(OptionsScreen screen, RectTransform host, Action onUse)
        {
            if (screen == null || host == null)
                return null;

            GameObject prefab = SulfurOptionsScreenBridge.GetOptionButtonPrefab(screen);
            if (prefab == null)
                return null;

            GameObject go = null;

            // Navigation is an addition to the widget: a failure here must not fail the
            // page build, the widget then just stays mouse-only.
            try
            {
                go = UnityEngine.Object.Instantiate(prefab, host, false);
                go.name = ObjectName;

                OptionsScreenOption option = go.GetComponent<OptionsScreenOption>();
                if (option == null)
                {
                    Discard(go);
                    return null;
                }

                Configure(go, option, onUse);
                return option;
            }
            catch (Exception ex)
            {
                if (!createFailureLogged && Plugin.Log != null)
                {
                    createFailureLogged = true;
                    Plugin.Log.LogWarning(
                        "SULFUR Native UI: could not make a library widget reachable by keyboard/gamepad; it stays " +
                        "mouse-only (logged once): " + ex);
                }

                Discard(go);
                return null;
            }
        }

        private static bool createFailureLogged;

        private static void Discard(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);
            go.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(go);
        }

        private static void Configure(GameObject go, OptionsScreenOption option, Action onUse)
        {
            go.transform.SetAsFirstSibling();
            go.SetActive(true);

            StretchOverHost(go);
            ExcludeFromLayout(go);

            // Same clean-up as a library button row: the prefab's slider value text would
            // otherwise render as a vertical "100%" column, and the label is unused.
            TextMeshProUGUI valueText = SulfurOptionsScreenBridge.GetSliderValueText(option);
            if (valueText != null)
                valueText.text = "";

            option.SetLabel("");

            KeepOnlyHighlight(option);
            BlockPointerInput(go);

            SulfurOptionBinding binding = go.AddComponent<SulfurOptionBinding>();
            binding.OnUse = onUse;
            option.onButtonPressed = binding.InvokeUse;
        }

        /// <summary>
        /// Keyboard/gamepad Use of a library button: Unity's own submit path (onClick when the
        /// button is active and interactable, plus the pressed-colour flash).
        /// </summary>
        public static void Press(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable())
                return;

            button.OnSubmit(null);
        }

        private static void StretchOverHost(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
                return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        // The host row's height comes from its own LayoutElement (or its layout group's
        // children); an ignored child changes neither.
        private static void ExcludeFromLayout(GameObject go)
        {
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout == null)
                layout = go.AddComponent<LayoutElement>();

            layout.ignoreLayout = true;

            // A size fitter on the prefab root would resize the stretched rect.
            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }

        // The game's highlight is OptionsScreenOption.border going white (Update); the
        // border is transparent while the row is not selected. Every other graphic of the
        // clone (background, label, value text) is switched off so the row looks unchanged.
        private static void KeepOnlyHighlight(OptionsScreenOption option)
        {
            Image border = SulfurReflection.GetField<Image>(option, "border");

            if (border == null && !missingBorderLogged)
            {
                missingBorderLogged = true;

                if (Plugin.Log != null)
                {
                    Plugin.Log.LogWarning(
                        "SULFUR Native UI: the game's option border was not found; library widgets stay " +
                        "reachable by keyboard/gamepad but show no selection highlight.");
                }
            }

            Graphic[] graphics = option.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic == null)
                    continue;

                graphic.raycastTarget = false;

                if (graphic != border)
                    graphic.enabled = false;
            }
        }

        private static void BlockPointerInput(GameObject go)
        {
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = go.AddComponent<CanvasGroup>();

            group.blocksRaycasts = false;
        }
    }
}
