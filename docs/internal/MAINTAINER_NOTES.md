# Maintainer notes

Facts about the game and its engine that the library depends on, and the design decisions
built on them. Each one cost a debugging round to find. Verified on SULFUR 1.0.4
(Steam build 26433890) unless stated otherwise; re-check against the decompiled game
after an update before relying on them.

Not user documentation — users read `README.md` and `docs/API_REFERENCE.md`. Component map:
`ARCHITECTURE.md`.

## Rules for changing the library

- **Public API only grows.** Mods are compiled against the DLL. Never change an existing
  public signature or return type (e.g. `void AddBadgeRow(...)` → returning a handle breaks
  every compiled caller with `MissingMethodException`); add an overload or a new method
  (`AddBadgeRowEx`, `out` overloads, `*Row` variants). Check before release with
  `tools/Check-DependentCompat.ps1` (see `RELEASE_CHECKLIST.md`).
- **Every Harmony patch resolves its target in `Prepare()`** (`SulfurPatchTargets.Resolve`).
  A `TargetMethod()` that returns null makes `PatchAll` throw and drops every later patch;
  with `Prepare()` a missing method skips only its own patch and logs one warning.
- **Game types stay behind small adapters** (`SulfurGameFonts`, `SulfurGameLanguage`,
  the options bridge). Public API uses project types (`SulfurFontRole`, language codes as
  strings), except where it already exposed game types before (`OptionsScreenOption`).
- **Diagnostics never change behaviour.** Missing-member warnings are logged once.
- **UI only.** No BepInEx config handling in the library: no `ConfigEntryBase` /
  `ConfigFile`, scanning, applying, saving, backups or dirty-state calculation. Those belong
  to the consuming mod (SULFUR Config); the library only renders what it is given
  (`SulfurSettingRow.IsDirty`, ...).

## Fonts

- The game keeps one font per role: `AsyncAssetLoading.Instance.fontCache[6]`, indexed by
  `FontStyle` (RoundRegular … SerifBold), filled only by `FontLocalizer.LoadFontCache()`.
  The library hooks that method (postfix) — it covers both the first fill and language
  changes (`SetAllFonts` calls it).
- In Chinese the cache is often **all null at the main menu**: the menu's
  `FontLocalizer.Awake` runs before the language's font set has loaded; menu text is fixed
  later by I2 `Localize` swapping fonts by name. So `SulfurFonts.Get` calls
  `LoadFontCache()` itself when an entry is empty.
- Roles the game uses: options screen and option rows `SerifRegular`; centred HUD alerts
  `SerifBold`; small HUD text `SerifSemibold`; legends `RoundSemibold`; counters
  `RoundRegular`. English Round fonts (Fredoka) are Latin-only.
- The game's `InputIconsSwitcher.SetActiveFallbackFontAsset` clears
  `TMP_Settings.fallbackFontAssets` whenever the input-icon font changes, so the CJK/symbol
  fallback must sit in each font's own `fallbackFontAssetTable`.
- Build OS-font fallbacks with `TMP_FontAsset.CreateFontAsset(familyName, "Regular", 90)`;
  `CreateFontAsset(Font)` fails for every OS font at run time. Microsoft YaHei has no Hangul,
  so Korean text under a Chinese font shows boxes (seen briefly on the game's own texts
  while switching KO → ZH).

## Language

- Every language change ends in `AsyncAssetLoading.ChangeLanguage(string)`, whose body
  invokes `onLanguageChange`; a finalizer there runs after all of the game's handlers, and also
  when one of them throws (a postfix would not). That is where `SulfurLanguage.Changed` is
  raised, so mod text set from it is not overwritten.
- `ChangeLanguage(float)` loads the language's font set asynchronously and commits when that
  finishes, so the player can reach a custom page before the commit. That page is built with
  the old language's fonts, which the game then releases (blank text, and a
  NullReferenceException from anything measuring it), so `SulfurCustomCategoryMarker` rebuilds
  it. `OptionsScreenOption.UpdateTexts` throws on any row without a `PlayerSetting`, which is
  every library row, and that stops the remaining handlers: `SulfurOptionTextsGuard` skips it.
- A handler that throws during the font-set path also leaves `fontAssetsLoader` set, and the
  game then ignores further language changes until it restarts. Nothing here can undo that.
- In Traditional Chinese `CurrentLanguageCode` is `zh` at startup and `zh-TW` after an in-game
  change: the language's font set is a one-language I2 source coded `zh`, added before the
  main source at startup and after it on a change. `SulfurGameLanguage` reports `zh-TW` for
  both, so a test must cover a start in the language as well as a switch to it.
- The startup language is set in `LoadLanguage` without any event. `SulfurLanguage` checks
  from `Plugin.Update` until `AsyncAssetLoading.loadingDone` and a non-empty
  `LocalizationManager.CurrentLanguageCode`, then stops checking.
- Category labels are written only by `OptionsScreenCategory.UpdateLabel` (from
  `SetToCategory` and `onLanguageChange`). On a custom category it writes the raw enum
  number, so a postfix re-applies the page name.

## Options screen

- `OptionsScreen.Show` sets `EventSystem.sendNavigationEvents = false`: only entries of the
  screen's private `options` list (`OptionsScreenOption`) are reachable by keyboard/gamepad.
  Library widgets get a **navigation proxy** (`SulfurNavigationProxy`): a clone of the
  game's button prefab, stretched behind the widget, only its `border` visible, raycasts
  blocked (so mouse input stays with the widget and nothing fires twice).
- Registration order = navigation order: `RegisterOption` inserts by hierarchy position;
  list refreshes unregister everything under their container first.
- The game's `SelectIndex` loops `while (!options[index].gameObject.activeSelf)` — out of
  range or infinite with hidden rows. A prefix (custom pages only) moves the index to the
  nearest `activeInHierarchy` row.
- `OptionsScreenOption.OnDisable` calls `RemoveAllListeners()` on its slider and toggle.
  `SulfurOptionBinding` owns the library's listener and re-adds it in `OnEnable`.
- **Do not rely on a Harmony patch of a tiny game method.** `OptionsScreenOption.SetSelected`
  is two lines and the JIT inlines it into `SelectIndex`, so a postfix on it never runs on
  that path. Read the game's state instead (`isSelected` via `AccessTools.FieldRefAccess`).
- The game's button prefab carries the slider value text (`sliderValueText`, placeholder
  "100%") on a zero-width rect; on a button clone it wraps into a vertical "1 0 0 %". The
  library clears it on every button-prefab clone.
- `OptionsScreen.Hide` / `Show` / `SetCategory` / a rebuild invalidate the page's context
  (`IsAlive`); a rebuild runs `BuildPage` with a new context.
- Clicking a category (mouse) leaves focus on the category menu (`menuSelected = true`);
  arrows then move between categories. Same as native pages — press Enter to enter a page.
- `SetLocked` on a library-built row makes its label invisible (the serialized locked /
  normal text colours are `(0,0,0,0)` on rows the library builds). Handles lock with a
  `CanvasGroup` instead.

## Layout

- A `HorizontalLayoutGroup` with `childControlWidth = false` sizes children from
  `RectTransform.sizeDelta` and ignores `LayoutElement` widths. This caused both the
  0.10.1 small-button bug and the always-100px badges; badges now use
  `SulfurBadgeFlowLayout` (measured text width 100–240px, wraps onto further lines).
- Description, message and text rows grow with wrapped text through
  `SulfurRowHeightFitter` (measured at build time and on text changes, never from a layout
  callback); single-line rows keep their exact height.
- Row style sample (`FindSampleText`): size and colour from an option **prefab**, font from
  `SulfurFonts.Get(SerifRegular)`. Sampling size from rows in the container shrank fonts on
  every rebuild (those rows are already restyled smaller); the prefab's own font is the
  Latin default, which the game swaps only on instantiated rows.
- Themed groups: no Unity `Outline` (it redraws the whole `Image`, so the group looks
  filled). The frame is four `Image` lines (sides 4px, top/bottom 2px) under a
  `BorderOverlay` child with `LayoutElement.ignoreLayout`, inset 2px (a line at x=0 gets
  clipped), moved to the last
  sibling when the scope is disposed so row backgrounds do not cover it. Background alpha
  0.004. Fixed width `max(360, nativeWidth - indent + 24)` with top-left anchors and
  min/preferred width set: stretch anchors made the group vanish under the screen's
  `VerticalLayoutGroup` + `ContentSizeFitter`.
- Foldout headers: arrow and label are separate TMP texts, so an ellipsised label never
  hides the arrow. Open arrow "▼", closed ">"; "▼" may come from the glyph fallback.

## Game bug worked around

- `OptionsScreenOption.OnPointerEnter/Exit` read `GameManager.Instance.InputReader`
  without a null check; `GameManager` does not exist at the main menu, so every mouse move
  over an option row there throws. `Integration/SulfurMainMenuHoverGuard.cs` uses Harmony
  **finalizers** that act only while the game's method throws exactly that, then set the
  hover state like the game's own `PauseMenuButton` does. A fixed game version runs
  untouched. After a game update: if the log line "Suppressing the game's main-menu
  NullReferenceException" never appears any more, the fix has shipped — delete the file.

## Live testing

- Test profile `NativeUI` (folder profile `NativeUI开发`): host in English, a sandboxed
  client in Chinese. The test plugin `SULFURNativeUITest` builds a page with sections for
  fonts (toast/popup buttons), row handles, the Config-reported issues and wrapping /
  badges / caret; each logs a prefix (`HandleCheck:`, `ConfigIssues:`, `WrapBadges:`).
  Its reference to the library must stay `<Private>False</Private>`.
- Injected key presses go through the Input System only and never reach TMP input fields'
  own key handling: "Enter ends typing" cannot be tested that way; Escape (handled by the
  library) and typed text can.
- At the main menu there is no `GameManager`; mouse sweeps there used to log the hover
  exception above.
