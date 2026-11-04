# Release checklist

## Version

Bump together (they drift otherwise):

- `Core/Plugin.cs` `PluginVersion`
- `Properties/AssemblyInfo.cs` `AssemblyVersion` / `AssemblyFileVersion`
- `Thunderstore/manifest.json` `version_number`
- `README.md` "Current target version"
- `CHANGELOG.md` and `Thunderstore/README.md` (player-facing changelog)
- the mod registry entry (`version`) after publishing

## Package

- The zip must contain `lang/*.json` (14 files) next to the DLL. Without them the
  library's own text silently falls back to English. The build copies the root `lang/`
  into `Thunderstore/lang/` (gitignored) next to the DLL; the packaging script
  (`sulfur-thunderstore` pack) zips that folder.
- The packaging script also expects `Thunderstore/CHANGELOG.md` (first line `# Changelog`,
  top entry `## <manifest version>`), which this repository does not have yet: the
  changelog still lives inside `Thunderstore/README.md`. Split it out before packing.
- `lang/*.json` must stay LF without BOM (`.gitattributes` pins them).

## Compatibility with published dependents

1. Download the latest published version of every package that depends on the library
   (Thunderstore API, see the notes in `tools/Check-DependentCompat.ps1`). As of 0.11.0:
   SULFUR Together, SULFUR Config, Nemophila-SimpleChat, h3nw-Sulfur_NoGore.
2. Run `tools/Check-DependentCompat.ps1` against the previous release (control: all must
   resolve) and against the new build (must also all resolve).
3. Check dependents that bind by name with reflection (SULFUR Together binds
   `SulfurPopupApi.ShowBanner(string)`, `HideBanner()`, `SulfurToastApi.Show(string, string)`,
   `SulfurOptionsApi`, `SulfurLocalization.LoadPluginLocalization/Get/LanguageVersion`).
4. Smoke-test the dependents in game where possible.

## Order with dependents

A dependent that uses new API (SULFUR Config 0.3.0 uses 0.11.0 API) is released only after
the library, with its manifest depending on the new library version.
