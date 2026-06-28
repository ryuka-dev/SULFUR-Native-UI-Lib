using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Shared visual constants for the library's self-built HUD surfaces (banner,
    /// toasts). Kept in one place so every surface reads the same SULFUR-flavored
    /// palette instead of redefining colors locally.
    /// </summary>
    internal static class SulfurUiTheme
    {
        /// SULFUR's signature amber — used for accent rules and bars. Matches the
        /// fallback the OptionsScreen styling uses (section lines, group borders).
        public static readonly Color Accent = new Color(1f, 0.62f, 0.18f, 1f);

        /// Warm near-black panel fill — closer to SULFUR's earthy chrome than pure black.
        public static readonly Color PanelFill = new Color(0.05f, 0.04f, 0.03f, 0.92f);

        /// Warm off-white for body text — easy to read, not clinical white.
        public static readonly Color TextPrimary = new Color(0.94f, 0.90f, 0.84f, 1f);
    }
}
