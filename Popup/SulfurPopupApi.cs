using System;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Public, dependency-free entry point for a single in-game message banner.
    /// Consumers (e.g. the SULFUR Together co-op mod) take a hard/soft dependency
    /// on this plugin and call these from their own code; this library knows
    /// nothing about the caller.
    ///
    /// The banner is a display-only HUD overlay: it does not pause the game, steal
    /// input, or change the cursor. Input (such as a confirm keypress) stays owned
    /// by the caller.
    /// </summary>
    public static class SulfurPopupApi
    {
        /// <summary>
        /// Show, or update the text of, the single persistent message banner.
        /// Idempotent — calling again replaces the text rather than stacking.
        /// The banner stays visible until <see cref="HideBanner"/> is called.
        /// Must be called from Unity's main thread.
        /// </summary>
        public static void ShowBanner(string text)
        {
            try
            {
                SulfurPopupController.Instance.ShowBanner(text);
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SulfurPopupApi.ShowBanner failed: " + e);
            }
        }

        /// <summary>
        /// Hide the banner if it is showing. Safe to call when nothing is shown,
        /// and never creates the overlay if it was never used.
        /// </summary>
        public static void HideBanner()
        {
            try
            {
                SulfurPopupController.HideIfExists();
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SulfurPopupApi.HideBanner failed: " + e);
            }
        }
    }
}
