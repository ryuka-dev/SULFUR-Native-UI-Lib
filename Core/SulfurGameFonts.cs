using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using TMPro;

namespace Ryuka.Sulfur.NativeUI
{
    using GameFontStyle = PerfectRandom.Sulfur.Core.FontStyle;

    /// <summary>
    /// The only place that touches the game's font-localization types. It reads the
    /// game's per-role font cache and reports when the game reloads it.
    ///
    /// Game mechanism (PerfectRandom.Sulfur.Core, verified on 1.0.4):
    ///  - <c>AsyncAssetLoading.Instance.fontCache</c> holds one TMP font per
    ///    <c>FontStyle</c> (6 entries) for the current language.
    ///  - <c>FontLocalizer.LoadFontCache()</c> is the only writer of that cache. It runs
    ///    from <c>FontLocalizer.Awake</c> whenever an entry is empty, and from
    ///    <c>FontLocalizer.SetAllFonts</c>, which the game invokes on language change.
    ///
    /// Typed access is wrapped so a game update that removes or renames any of these
    /// fails once, deterministically, with one log line — never a random font.
    /// </summary>
    internal static class SulfurGameFonts
    {
        private static bool unsupported;

        /// <summary>
        /// Raised after the game (re)loaded its font cache. Fired for every load, even if
        /// nothing changed; the listener compares.
        /// </summary>
        internal static event Action CacheLoaded;

        /// <summary>True once a required game member turned out to be missing.</summary>
        public static bool Unsupported
        {
            get { return unsupported; }
        }

        /// <summary>Hook the game's cache loader. Called once from Plugin.Awake.</summary>
        public static void Install(Harmony harmony)
        {
            if (unsupported || harmony == null)
                return;

            try
            {
                InstallCore(harmony);
                Plugin.Log?.LogInfo("Role fonts: hooked FontLocalizer.LoadFontCache.");
            }
            catch (Exception e)
            {
                MarkUnsupported("hooking FontLocalizer.LoadFontCache failed: " + e.Message);
            }
        }

        /// <summary>
        /// The game's current font for <paramref name="role"/>, or null when the game has
        /// none yet (its language assets are still loading) or this game build lacks the
        /// mechanism. Loads the game's cache first if the entry is empty — exactly what the
        /// game's own <c>FontLocalizer.Awake</c> does.
        /// </summary>
        public static TMP_FontAsset TryGet(SulfurFontRole role)
        {
            if (unsupported)
                return null;

            // Outside the try: a bad role is the caller's error, not a missing game member.
            // The FontStyle values are compile-time constants, so no game type loads here.
            int index = ToGameIndex(role);

            try
            {
                return TryGetCore(index, true);
            }
            catch (Exception e)
            {
                MarkUnsupported("reading the game's font cache failed: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// The game's cached font for <paramref name="role"/> without triggering a load.
        /// Used to compare cache contents after a load.
        /// </summary>
        public static TMP_FontAsset PeekCached(SulfurFontRole role)
        {
            if (unsupported)
                return null;

            // Outside the try: a bad role is the caller's error, not a missing game member.
            // The FontStyle values are compile-time constants, so no game type loads here.
            int index = ToGameIndex(role);

            try
            {
                return TryGetCore(index, false);
            }
            catch (Exception e)
            {
                MarkUnsupported("reading the game's font cache failed: " + e.Message);
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InstallCore(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(typeof(FontLocalizer), nameof(FontLocalizer.LoadFontCache));
            if (target == null)
                throw new MissingMethodException("FontLocalizer", "LoadFontCache");

            MethodInfo postfix = AccessTools.Method(typeof(SulfurGameFonts), nameof(LoadFontCachePostfix));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TMP_FontAsset TryGetCore(int index, bool loadIfEmpty)
        {
            AsyncAssetLoading loading = AsyncAssetLoading.Instance;
            if (loading == null)
                return null; // game bootstrap not reached yet

            TMP_FontAsset[] cache = loading.fontCache;
            if (cache == null || index >= cache.Length)
                throw new InvalidOperationException("fontCache has an unexpected shape");

            if (cache[index] == null && loadIfEmpty)
            {
                // Same call the game's FontLocalizer.Awake makes for an empty entry. Before
                // the language's font set has loaded it simply yields null again.
                FontLocalizer.LoadFontCache();
                cache = loading.fontCache;
            }

            TMP_FontAsset font = cache[index];
            return font != null ? font : null; // normalise Unity "destroyed" to real null
        }

        private static int ToGameIndex(SulfurFontRole role)
        {
            // Explicit mapping (not a cast) so a reordered game enum cannot silently
            // shift every role by one.
            switch (role)
            {
                case SulfurFontRole.RoundRegular: return (int)GameFontStyle.RoundRegular;
                case SulfurFontRole.RoundSemibold: return (int)GameFontStyle.RoundSemibold;
                case SulfurFontRole.RoundBold: return (int)GameFontStyle.RoundBold;
                case SulfurFontRole.SerifRegular: return (int)GameFontStyle.SerifRegular;
                case SulfurFontRole.SerifSemibold: return (int)GameFontStyle.SerifSemibold;
                case SulfurFontRole.SerifBold: return (int)GameFontStyle.SerifBold;
                default: throw new ArgumentOutOfRangeException("role", role, "Unknown SulfurFontRole.");
            }
        }

        private static void LoadFontCachePostfix()
        {
            Action handler = CacheLoaded;
            if (handler == null)
                return;

            try
            {
                handler();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Role fonts: refresh after LoadFontCache failed: " + e);
            }
        }

        private static void MarkUnsupported(string reason)
        {
            if (unsupported)
                return;

            unsupported = true;
            Plugin.Log?.LogWarning(
                "Role fonts are not supported by this game version (" + reason +
                "). Using TMP's default font with glyph fallback instead.");
        }
    }
}
