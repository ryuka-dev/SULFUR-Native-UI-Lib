using System;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Public, dependency-free entry point for transient message toasts shown in
    /// the top-right corner during normal gameplay. Consumers (e.g. the SULFUR
    /// Together co-op mod) take a hard/soft dependency on this plugin and call
    /// these; this library knows nothing about the caller.
    ///
    /// Toasts are fire-and-forget: each appears with a smooth slide/fade animation,
    /// holds for its duration, then animates out. Multiple toasts stack (newest on
    /// top). They are passive — no game pause, no input capture, no cursor change.
    /// Call from Unity's main thread.
    /// </summary>
    public static class SulfurToastApi
    {
        /// <summary>Show a message toast with the default duration.</summary>
        public static void Show(string message)
        {
            Show(null, message, 0f);
        }

        /// <summary>Show a message toast that holds for <paramref name="durationSeconds"/>.</summary>
        public static void Show(string message, float durationSeconds)
        {
            Show(null, message, durationSeconds);
        }

        /// <summary>Show a titled message toast with the default duration.</summary>
        public static void Show(string title, string message)
        {
            Show(title, message, 0f);
        }

        /// <summary>
        /// Show a titled message toast that holds for <paramref name="durationSeconds"/>
        /// (a non-positive value uses the default duration).
        /// </summary>
        public static void Show(string title, string message, float durationSeconds)
        {
            try
            {
                SulfurToastController.Instance.Show(title, message, durationSeconds);
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("SulfurToastApi.Show failed: " + e);
            }
        }
    }
}
