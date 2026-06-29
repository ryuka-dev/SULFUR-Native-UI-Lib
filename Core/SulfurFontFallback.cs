using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Supplies missing glyphs for all TMP text by building a small <b>fallback chain</b>
    /// from installed OS fonts and attaching it where TMP will find it.
    ///
    /// Two problems are covered:
    ///  - Non-Latin scripts (CJK, ...) missing from a Latin-only game font (e.g.
    ///    "Merriweather-Light SDF" in English) → blank "缺字".
    ///  - Symbols (✓ ○ ● ...) missing from even the game's CJK subset font (e.g.
    ///    "Chinese_Serif_NotoSansSC-Light SDF") → box/space.
    ///
    /// No single OS font covers both well, so the chain is ordered: a broad CJK face
    /// first, then a symbol-capable face. TMP substitutes <b>only the missing characters</b>
    /// per-glyph, keeping the primary font/style for every glyph it already has. Each
    /// candidate is verified (must actually load and render its probe) before use.
    ///
    /// Loaded via TMP's system-font path (TryGetSystemFontReference → load from the real
    /// font file + face index), which handles .ttc collections — unlike
    /// CreateFontAsset(Font), which needs imported font data and fails for OS fonts.
    ///
    /// Idempotent and best-effort: if TMP or a suitable OS font is not ready yet, it
    /// quietly does nothing and a later call retries.
    /// </summary>
    internal static class SulfurFontFallback
    {
        private const string FontStyle = "Regular";
        private const int SamplingPointSize = 90;

        // The built fallback chain, in search order (CJK first, then symbols).
        private static readonly List<TMP_FontAsset> fallbackAssets = new List<TMP_FontAsset>();

        // Set once the CJK fallback is built (proves TMP is ready). Symbols are best-effort
        // in the same pass; a missing symbol font does not force endless retries.
        private static bool built;

        // A fallback source: candidate OS font families (priority order) + a probe string
        // that a candidate must fully render to be accepted.
        private sealed class FallbackSource
        {
            public readonly string Label;
            public readonly string Probe;
            public readonly string[] Families;

            public FallbackSource(string label, string probe, string[] families)
            {
                Label = label;
                Probe = probe;
                Families = families;
            }
        }

        private static readonly FallbackSource[] Sources =
        {
            // CJK hanzi/kana/hangul. Probe includes 组/拟 (chars that exposed earlier gaps).
            new FallbackSource("CJK", "中文测试组拟", new[]
            {
                "Microsoft YaHei",        // 微软雅黑 — sans, on every Windows, broad coverage
                "Microsoft YaHei UI",
                "SimHei",                 // 黑体 — sans
                "SimSun",                 // 宋体 — serif
                "NSimSun",                // 新宋体 — serif
                "Noto Sans CJK SC",
                "Source Han Sans SC",
                "Malgun Gothic",          // KR coverage
                "Yu Gothic",              // JP coverage
                "Meiryo"
            }),

            // Misc symbols that CJK faces (and the game's subset) often lack: ✓ ✗ ○ ● ▶.
            new FallbackSource("Symbols", "✓✗○●▶", new[]
            {
                "Segoe UI Symbol",        // Win7+, has ✓✗○● arrows
                "Arial Unicode MS",       // very broad (Office)
                "Segoe UI",
                "Arial",
                "Microsoft Sans Serif",
                "Segoe UI Emoji"
            })
        };

        /// <summary>
        /// Ensure the fallback chain is built and present in TMP's global fallback list.
        /// Safe and cheap to call repeatedly: the fonts are built once, but global-list
        /// membership is re-checked every call (the game can rebuild that list when it
        /// applies a per-language font, which would otherwise drop our entries).
        /// </summary>
        public static void EnsureRegistered()
        {
            EnsureBuilt();
            EnsureInGlobalFallbacks();
        }

        /// <summary>
        /// Attach the whole fallback chain to a specific font asset's own fallback table,
        /// in order. More reliable than the global list for a font the game manages itself:
        /// call this with the font our custom rows actually render with, so missing glyphs
        /// resolve even if the global list isn't consulted for that font. Idempotent.
        /// </summary>
        public static void EnsureFontHasFallback(TMP_FontAsset primary)
        {
            EnsureBuilt();

            if (primary == null || fallbackAssets.Count == 0)
                return;

            try
            {
                List<TMP_FontAsset> table = primary.fallbackFontAssetTable;
                if (table == null)
                {
                    table = new List<TMP_FontAsset>();
                    primary.fallbackFontAssetTable = table;
                }

                foreach (TMP_FontAsset fb in fallbackAssets)
                {
                    if (fb != null && fb != primary && !table.Contains(fb))
                        table.Add(fb);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Attaching glyph fallback to '" + primary.name + "' failed: " + e.Message);
            }
        }

        private static void EnsureBuilt()
        {
            if (built)
                return;

            try
            {
                // CJK is the gate: if it can't build, TMP/the OS font isn't ready — retry
                // later. Once it builds, TMP is proven ready, so the symbol source is a
                // one-shot best-effort attempt (no endless retry if no symbol font exists).
                TMP_FontAsset cjk = BuildFromSource(Sources[0]);
                if (cjk == null)
                    return;

                fallbackAssets.Add(cjk);

                for (int i = 1; i < Sources.Length; i++)
                {
                    TMP_FontAsset extra = BuildFromSource(Sources[i]);
                    if (extra != null)
                        fallbackAssets.Add(extra);
                }

                built = true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Glyph fallback setup failed (will retry): " + e.Message);
            }
        }

        private static TMP_FontAsset BuildFromSource(FallbackSource source)
        {
            foreach (string family in source.Families)
            {
                TMP_FontAsset asset = TryCreateSystemFontAsset(family);
                if (asset == null)
                    continue;

                // Verify the face truly loaded by adding the probe glyphs. If any are
                // missing, this font didn't really load (or lacks the script) — discard it.
                string missing;
                bool ok = asset.TryAddCharacters(source.Probe, out missing);
                if (!ok || !string.IsNullOrEmpty(missing))
                {
                    UnityEngine.Object.Destroy(asset);
                    continue;
                }

                asset.name = "SULFUR_" + source.Label + "_Fallback (" + family + ")";
                Persist(asset);

                Plugin.Log?.LogInfo("Built " + source.Label + " glyph fallback: " + asset.name);
                return asset;
            }

            Plugin.Log?.LogWarning(
                "No loadable OS font found for " + source.Label + " glyph fallback; those glyphs may be unavailable.");
            return null;
        }

        private static TMP_FontAsset TryCreateSystemFontAsset(string familyName)
        {
            try
            {
                return TMP_FontAsset.CreateFontAsset(familyName, FontStyle, SamplingPointSize);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("CreateFontAsset failed for '" + familyName + "': " + e.Message);
                return null;
            }
        }

        // Survive scene loads so the atlas/material persist for the whole session.
        private static void Persist(TMP_FontAsset asset)
        {
            UnityEngine.Object.DontDestroyOnLoad(asset);
            if (asset.material != null)
                UnityEngine.Object.DontDestroyOnLoad(asset.material);
            if (asset.atlasTexture != null)
                UnityEngine.Object.DontDestroyOnLoad(asset.atlasTexture);
        }

        private static void EnsureInGlobalFallbacks()
        {
            if (fallbackAssets.Count == 0)
                return;

            try
            {
                List<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;
                if (globalFallbacks == null)
                    return;

                foreach (TMP_FontAsset fb in fallbackAssets)
                {
                    if (fb != null && !globalFallbacks.Contains(fb))
                        globalFallbacks.Add(fb);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Registering global glyph fallback failed: " + e.Message);
            }
        }
    }
}
