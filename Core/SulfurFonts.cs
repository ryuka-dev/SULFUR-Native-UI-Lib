using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Fonts for mod UI, chosen the way the game chooses its own: by role, following
    /// the current language.
    ///
    /// <para><see cref="Get"/> returns the game's font for a role. <see cref="Bind"/> also
    /// keeps a text on that role, so it switches font when the player changes language.
    /// <see cref="WithFallback"/> gives a mod's own font the same missing-glyph coverage.</para>
    ///
    /// <para>Every font handed out has the library's CJK and symbol fallback attached to its
    /// own fallback table, so missing glyphs resolve even when the game clears TMP's global
    /// fallback list (it does that whenever the input-icon font changes).</para>
    ///
    /// <para>If a game update removes the game's font mechanism, every call returns TMP's
    /// default font (with the same fallback) and one warning is logged. The result never
    /// depends on which texts happen to be loaded.</para>
    ///
    /// <para>Main thread only, like all Unity UI.</para>
    /// </summary>
    public static class SulfurFonts
    {
        private const int MinPruneThreshold = 64;

        private struct Binding
        {
            public WeakReference Text;
            public SulfurFontRole Role;
        }

        // Keyed by the text's instance id: re-binding a text replaces its role.
        private static readonly Dictionary<int, Binding> bindings = new Dictionary<int, Binding>();
        private static readonly List<int> deadKeys = new List<int>();
        private static int pruneThreshold = MinPruneThreshold;

        // Last cache contents seen, per role, to tell a real change from a no-op reload.
        private static readonly TMP_FontAsset[] lastSeen = new TMP_FontAsset[RoleCount];
        private static bool reapplying;

        private const int RoleCount = 6;

        static SulfurFonts()
        {
            SulfurGameFonts.CacheLoaded += OnGameCacheLoaded;
        }

        /// <summary>
        /// Raised on the main thread after the game's role fonts changed (language switch,
        /// or the game finishing its language load). Texts registered with
        /// <see cref="Bind"/> are already updated when this fires; subscribe only if you
        /// hold font assets yourself.
        /// </summary>
        public static event Action FontsChanged;

        /// <summary>
        /// The game's current font for <paramref name="role"/>, with glyph fallback
        /// attached. Falls back to TMP's default font while the game's language assets are
        /// still loading or when this game version lacks the mechanism. Null only if TMP
        /// itself has no default font.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Unknown role value.</exception>
        public static TMP_FontAsset Get(SulfurFontRole role)
        {
            ValidateRole(role);

            TMP_FontAsset font = SulfurGameFonts.TryGet(role);
            if (font != null)
                Remember(role, font);
            else
                font = DefaultFont();

            return WithFallback(font);
        }

        /// <summary>
        /// Give <paramref name="text"/> the font for <paramref name="role"/> now and keep it
        /// on that role: when the game swaps fonts (language change), the text follows.
        /// Binding the same text again replaces its role. The library holds texts weakly,
        /// so destroyed texts simply drop out. Sets the font's own material.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Unknown role value.</exception>
        public static void Bind(TMP_Text text, SulfurFontRole role)
        {
            ValidateRole(role);

            if (text == null)
                return;

            Apply(text, Get(role));

            bindings[text.GetInstanceID()] = new Binding { Text = new WeakReference(text), Role = role };

            if (bindings.Count >= pruneThreshold)
            {
                Prune();
                pruneThreshold = Math.Max(MinPruneThreshold, bindings.Count * 2);
            }
        }

        /// <summary>
        /// Attach the library's CJK and symbol fallback to a font the caller owns, so it
        /// renders missing glyphs like the fonts from <see cref="Get"/>. Idempotent;
        /// returns the same font. Null in, null out.
        /// </summary>
        public static TMP_FontAsset WithFallback(TMP_FontAsset font)
        {
            if (font != null)
                SulfurFontFallback.EnsureFontHasFallback(font);

            return font;
        }

        private static void OnGameCacheLoaded()
        {
            if (reapplying)
                return;

            bool changed = false;
            for (int i = 0; i < RoleCount; i++)
            {
                TMP_FontAsset current = SulfurGameFonts.PeekCached((SulfurFontRole)i);
                if (current == null)
                    continue; // a load that came up empty never replaces a good font

                if (!ReferenceEquals(current, lastSeen[i]))
                {
                    lastSeen[i] = current;
                    changed = true;
                }
            }

            if (!changed)
                return;

            reapplying = true;
            try
            {
                Reapply();
            }
            finally
            {
                reapplying = false;
            }

            Plugin.Log?.LogInfo("Role fonts updated: " + DescribeCache());
            RaiseFontsChanged();
        }

        private static void Reapply()
        {
            deadKeys.Clear();

            foreach (KeyValuePair<int, Binding> pair in bindings)
            {
                TMP_Text text = pair.Value.Text.Target as TMP_Text;
                if (text == null)
                {
                    deadKeys.Add(pair.Key);
                    continue;
                }

                Apply(text, Get(pair.Value.Role));
            }

            RemoveDead();
        }

        private static void Prune()
        {
            deadKeys.Clear();

            foreach (KeyValuePair<int, Binding> pair in bindings)
            {
                TMP_Text text = pair.Value.Text.Target as TMP_Text;
                if (text == null)
                    deadKeys.Add(pair.Key);
            }

            RemoveDead();
        }

        private static void RemoveDead()
        {
            foreach (int key in deadKeys)
                bindings.Remove(key);

            deadKeys.Clear();
        }

        private static void Apply(TMP_Text text, TMP_FontAsset font)
        {
            if (font == null || text.font == font)
                return;

            text.font = font;
            if (font.material != null)
                text.fontSharedMaterial = font.material;
        }

        private static void Remember(SulfurFontRole role, TMP_FontAsset font)
        {
            // A font we hand out counts as seen, so the next cache load does not report it
            // as a change. Bound texts that got TMP's default meanwhile are upgraded by the
            // load that first fills the role (it differs from what was seen: nothing).
            lastSeen[(int)role] = font;
        }

        private static TMP_FontAsset DefaultFont()
        {
            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch (Exception)
            {
                return null; // TMP settings not loaded yet
            }
        }

        private static void RaiseFontsChanged()
        {
            Action handler = FontsChanged;
            if (handler == null)
                return;

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action)subscriber)();
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogWarning("SulfurFonts.FontsChanged subscriber threw: " + e);
                }
            }
        }

        private static string DescribeCache()
        {
            string[] names = new string[RoleCount];
            for (int i = 0; i < RoleCount; i++)
            {
                TMP_FontAsset font = lastSeen[i];
                names[i] = (SulfurFontRole)i + "=" + (font != null ? font.name : "-");
            }

            return string.Join(", ", names);
        }

        private static void ValidateRole(SulfurFontRole role)
        {
            if (role < SulfurFontRole.RoundRegular || role > SulfurFontRole.SerifBold)
                throw new ArgumentOutOfRangeException("role", role, "Unknown SulfurFontRole.");
        }
    }
}
