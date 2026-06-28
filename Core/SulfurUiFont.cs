using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Resolves the game's current, language-correct TMP font for the library's
    /// self-built HUD surfaces. The game swaps its font asset per language (e.g. a
    /// CJK-capable asset for Chinese); sampling a live game text keeps custom UI
    /// from rendering as blank boxes in non-Latin languages.
    /// </summary>
    internal static class SulfurUiFont
    {
        /// <summary>
        /// Find an active, enabled <see cref="TextMeshProUGUI"/> in the loaded scene
        /// to sample the localized font/material/size/color from. Anything under
        /// <paramref name="exclude"/> (our own overlay) is skipped so we never sample
        /// our own text. Returns null if no live game text is available yet — callers
        /// should fall back without locking in and retry later.
        /// </summary>
        public static TextMeshProUGUI FindLiveGameText(Transform exclude)
        {
            // FindObjectsByType returns only active scene components (not prefab
            // assets), so this naturally reflects the live, current-language chrome.
            TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            if (texts == null)
                return null;

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null || text.font == null)
                    continue;

                if (exclude != null && text.transform.IsChildOf(exclude))
                    continue;

                return text;
            }

            return null;
        }
    }
}
