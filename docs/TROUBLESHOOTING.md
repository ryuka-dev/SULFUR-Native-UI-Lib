# Troubleshooting

## The page does not appear in the Options screen

- Look for `Registered SULFUR native options page: <PageId>` in the BepInEx log.
- `RegisterPage` throws when `PageId` or `BuildPage` is missing.
- Another mod registered the same `PageId` (a warning names both); use your plugin GUID.
- Without `[BepInDependency("ryuka.sulfur.nativeui")]` your plugin may load before the
  library.

## The page shows "This page failed to load"

`BuildPage` threw. The full exception is in the BepInEx log the first time, with the page
id.

## A value edit takes a second or more

The page is rebuilt on every change. Use a row handle instead of `ctx.Rebuild()`; see the
[config editor guide](CONFIG_EDITOR_GUIDE.md#rules).

## Edits are lost after a rebuild or rescan

`BuildPage` re-reads the saved values. Keep unsaved drafts in your own model, outside the
page, and restore them after a rescan.

## Calls do nothing, log says "called on the options context ... after that page was closed or rebuilt"

The `ctx` is dead: a rebuild, foldout click, category switch or closing the screen ended
it. Use the `ctx` of the current `BuildPage` run, check `ctx.IsAlive` in late callbacks
(timers, network events), and take new handles on every run. See
[Calls on a dead context](API_REFERENCE.md#calls-on-a-dead-context).

## `NullReferenceException` on a value returned by an `Add*` call

On a dead context, `AddToggle`, `AddInlineTextInput`, `AddButton` and the other calls that
return a row or field return `null`. Check `ctx.IsAlive`, or use the handle-returning calls.

## `ArgumentOutOfRangeException` when indexing `AddButtonRow`'s result

The list holds only the buttons that were built: empty labels are skipped, and a dead
context returns an empty list. Check `Count`.

## The status badge does not change

- Rows built with `AddSettingToggle` (no `Ex`) give no handle; use `AddSettingToggleEx`
  and friends.
- While clean, a row with `ShowCleanBadge = false` shows no status badge by design.

## A row's label disappears after locking it

The game's `OptionsScreenOption.SetLocked` hides labels of library rows. Use
`SetLocked` on the row's handle (`AddToggleRow`, `AddSliderRow`, `AddCycleRow`).

## Text shows boxes or blank characters

- Library rows use the game's option font (`SulfurFonts.Get(SulfurFontRole.SerifRegular)`)
  and fill missing glyphs from installed system fonts automatically.
- Text your mod creates itself should take its font from `SulfurFonts.Bind(text, role)` (or
  `Get`), or run its own font through `SulfurFonts.WithFallback`. Do not copy the font of a
  random game text: it may be a Latin-only font.
- If boxes remain and the log says `No loadable OS font found for CJK glyph fallback`, the
  system has none of the fonts the library looks for (common under Linux/Proton). Installing
  one, e.g. Microsoft YaHei or Noto Sans CJK SC, fixes it.

## Translations show English

- The log lists every file loaded: `Loaded localization: <guid> / <code> / <n> entries`.
  No line means the files were not found next to your DLL (see
  [Localization](LOCALIZATION.md#files)).
- File names must be language codes with a hyphen (`zh-CN.json`).
- The key in `Get` must match the file exactly.

## The library's own words ("Default", "Apply", message prefixes) stay English

Its `lang/` folder is missing next to `SULFUR Native UI Lib.dll`. Reinstall the package.

## `MissingMethodException` mentioning `Ryuka.Sulfur.NativeUI`

The installed library is older than the one your mod was compiled against. Depend on that
version in your manifest (`ryuka_labs-SULFUR_Native_UI_Lib-0.11.0`).
