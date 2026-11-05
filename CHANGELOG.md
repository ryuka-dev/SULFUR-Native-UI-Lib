# Changelog

## Unreleased

### Changed

- **`zh-TW` falls back to `zh-CN`.** `SulfurLocalization.Get` now tries `zh-TW`, `zh-CN`,
  `zh`, `en`, so text without a Traditional Chinese translation shows in Simplified Chinese
  instead of English. This covers every mod that reads its text through the library.
  Other languages are unchanged.
- **The library's own text is translated into Traditional Chinese** (`lang/zh-TW.json`), using
  the game's own terms where it has them.

### Fixed

- **Starting the game in Traditional Chinese is now recognised as `zh-TW`.** The game reports
  `zh` at startup and `zh-TW` after an in-game change, so a player who started in Traditional
  Chinese got neither the `zh-TW` file nor the `zh-CN` fallback. `SulfurLanguage.Code` and
  lookups now use `zh-TW` in both cases.
- **A language change no longer breaks while a custom page is open.** Library rows have no
  game setting, so the game's own text refresh threw on them, which stopped the remaining
  language handlers and left the library on the old language until the next change. This
  happened when a custom page was opened while the new language's fonts were loading.
- **A page opened during a language change no longer goes blank.** It was built with the old
  language's fonts, which the game then releases, so some of its text disappeared until the
  page was reopened. An open custom page is now rebuilt when the change takes effect.
- **The library follows a language change even if another mod's handler throws.**
- **The footer's left text is no longer cut off early.** The status text's huge preferred
  width squeezed it to its minimum, so a summary such as `Dirty: 0 | Files: 2 | Apply Count: 0`
  ended in an ellipsis with free space beside it. It now takes the width it needs, and is
  only shortened when the bar is really full.

## 0.11.0

### Fixed

- **Chinese (and other non-Latin) text no longer disappears from toasts and the
  banner.** Both surfaces took their font from whichever game text
  `FindObjectsByType` happened to return first. Since game 1.0.4 that is often a
  Latin-only HUD font (`Fredoka-Regular SDF`), so in Chinese every CJK character
  was dropped (e.g. `按 [Return] 进入竞技场` showed only `[Return]`) and the font
  could differ from one session to the next. They now use the game's own role fonts
  (below), with the glyph fallback attached to the font itself.
- **Glyph fallback no longer depends on TMP's global fallback list.** The game
  clears that list whenever it switches the input-icon font, so a fallback that
  lived only there could vanish at any time. Every font the library hands out now
  carries the fallback in its own fallback table.
- **No more vertical "1 0 0 %" beside buttons.** `AddButton` rows (and inline text
  inputs, which reuse the same game prefab) showed the prefab's slider-value
  placeholder `100%` wrapped one character per line, because that text sits on a
  zero-width rect and the game only fills it for slider rows. The library now
  clears it on every button-prefab clone.
- **Keyboard/gamepad navigation on custom pages.**
  - Rows refreshed through `SulfurListHandle` stayed in the game's navigation list
    after being destroyed (MissingReferenceException on the next Up/Down) and were
    re-added at the end of the list. Navigation order now always equals screen
    order, and the selection moves off removed rows.
  - Hidden rows (collapsed foldouts, `SetActive(false)`, hidden lists) are skipped.
    Before, they could be selected while invisible, and the game's `SelectIndex`
    could run out of range or loop forever.
  - Up/Down in the category menu goes through the game's own code again, so the
    rebind screen's list navigation is no longer hijacked into switching category.
- **Typing Space or Enter in a library text input no longer triggers the options
  screen's Submit** (which rebuilt the page and destroyed the input, or pressed the
  selected row).
- **Toggles and sliders keep saving after being hidden.** The game's
  `OptionsScreenOption.OnDisable` removes every listener from them; the library now
  re-attaches its own when the row is shown again.
- **`SulfurSettingHandle.SetDirty` no longer overwrites another badge.** On a row built
  without a status badge (`ShowCleanBadge = false`, not dirty) it rewrote the first other
  badge (e.g. "Restart Required"), or did nothing when the row had no badges. It now adds
  the status badge in front, and removes it again when the row becomes clean and
  `ShowCleanBadge` is false (before, such a row showed the clean text).
- **A page whose `BuildPage` throws** shows an error row instead of a blank page
  that then threw on Use/Escape; the exception is logged once with the page id. A
  category button whose page was unregistered behaves the same. One mod's failing
  page name no longer removes the buttons of the pages after it.
- **Number inputs** revert invalid input to the last accepted value, not to the
  value the page was built with.
- **Pause menu**: the library no longer clears the game's initial selection (the
  Resume button) every time the pause menu opens; it only clears a selection that
  is a library input or inside the options screen.
- **Glyph fallback** no longer retries every OS font on every row when none can be
  loaded (e.g. under Proton): at most 5 attempts, 30 s apart, one warning.
- **Button rows keep their in-row focus across `ctx.Rebuild()`** (and across
  `SulfurListHandle.Update`). Before, pressing the 2nd button of a row whose
  callback rebuilt the page moved the focus back to the 1st button, so the next
  Enter pressed the wrong one.
- **Badges fit their text and badge rows wrap.** Badges were always 100px wide (the
  intended text-based width never reached the layout), so longer text such as
  "Restart Required" or most translations was cut off with "…". A badge now keeps
  its 100px minimum and grows to its measured text width, up to 240px; a row whose
  badges don't fit wraps onto further lines instead of running past the right edge.
  Badges that fitted before are unchanged.
- **Long descriptions and messages no longer cover the next row.** Description,
  message and text rows had a fixed height while their text wrapped; they now grow
  with the text (single-line rows keep their exact height).
- **Starting to edit with keyboard/gamepad** selects a number input's whole value
  (typing replaces it; before, "0.13" typed into "0.25" gave "0.250.13") and puts a
  text input's caret at the end. Mouse clicks are unchanged. Text selections are now
  drawn.
- **Setting rows always have a message line** (hidden while empty), so
  `SulfurSettingHandle.SetMessage` works on rows built without a message.
- **Forced-open foldouts ignore clicks.** With `forceExpanded`, a click used to
  flip the stored state invisibly, so the foldout came back inverted once the force
  was lifted.
- **Workaround for a game bug: no more NullReferenceException spam when moving the
  mouse over Options rows at the main menu** (the game's
  `OptionsScreenOption.OnPointerEnter/Exit` read `GameManager.Instance`, which does
  not exist there; affects the game's own rows too). Hover highlighting now works
  there as well. Implemented as Harmony finalizers that act only when the game's
  method actually throws that exception, so a game version that fixes the bug runs
  its own code untouched (`Integration/SulfurMainMenuHoverGuard.cs`; delete it once
  the fix has shipped).

### Added

- **`SulfurFonts` — fonts by role, following the language.**
  `SulfurFonts.Get(SulfurFontRole)` returns the game's current font for one of its
  six roles (`RoundRegular`…`SerifBold`), the same fonts the game's UI uses.
  `SulfurFonts.Bind(text, role)` also keeps a text on its role, so it switches font
  when the player changes language. `SulfurFonts.WithFallback(font)` gives a mod's
  own font the same CJK/symbol coverage; `SulfurFonts.FontsChanged` reports a
  change. Hooks the game's `FontLocalizer.LoadFontCache`; on a game build without
  that mechanism, every call returns TMP's default font (with fallback) and one
  warning is logged.
- **`SulfurLanguage`: follow the game's language with one call.**
  `SulfurLanguage.Follow(code => ...)` calls back once the language is known
  (including when the player simply starts the game in, say, Chinese; the game
  raises no event for that) and after every change, *after* all of the game's own
  language-change handlers have run, so text a mod sets is not overwritten. Also
  `Code`, `IsReady`, `IsRightToLeft`, `Changed`, `FixRtl(text)`. Replaces
  subscribing to `AsyncAssetLoading.onLanguageChange` yourself.
- **Row handles for native rows:** `AddToggleRow`, `AddSliderRow` and `AddCycleRow`
  return `SulfurToggleHandle` / `SulfurSliderHandle` / `SulfurCycleHandle` with
  `SetValueWithoutNotify`, `SetLocked` (dims the row and blocks pointer and
  keyboard input without touching label colours; the game's own `SetLocked` makes
  labels of library-built rows invisible), `SetVisible`, `SetLabel` and `IsAlive`.
  `AddSliderRow` also snaps mouse drags to `step`. The existing `AddToggle` /
  `AddSlider` / `AddCycle` are unchanged.
- **Keyboard and gamepad reach every library widget**: foldout headers, small
  buttons, button rows (Left/Right moves a focus frame between the buttons), Default
  buttons, text and number inputs (Enter starts typing, Escape stops), and the
  footer button. They get the game's own selection border; mouse behaviour is
  unchanged.
- **`SulfurSettingHandle` can set the row's value in place:** `SetValueWithoutNotify`
  (bool / float / double / string), `SetIndexWithoutNotify` (cycle) and
  `SetDefaultButtonVisible`, so e.g. a Default button no longer needs a page
  rebuild. Calls on a row of another kind return false.
- **Badge rows that update in place:** `AddBadgeRowEx(...)` and `out
  SulfurBadgeRowHandle` overloads of the three `*FoldoutWithBadges` methods;
  the handle has `SetBadges`, `SetBadge(index, text)`, `SetVisible`, `IsAlive`.
- **Double-precision number inputs:** `AddNumberInput`, `AddInlineNumberInput`,
  `AddSettingNumber` and `AddSettingNumberEx` overloads taking `double`. The float
  versions keep their ~7 significant digits (documented); existing calls still bind
  to them.
- **`SulfurOptionsContext.IsAlive`.** A context stops being alive when its page is
  rebuilt, another page is shown or the screen closes; calls on a dead context are
  ignored instead of writing into whatever page is on screen now.
- **The library's own text is localized** in all 14 game languages (`lang/*.json`
  beside the DLL): message prefixes, input placeholders, Default/Apply/MOD,
  built-in setting-row badges, and the page-failed message. Machine translations,
  to be reviewed by native speakers.

### Changed

- Fonts are now fixed per surface, matching the game's own usage: option rows and
  the options footer `SerifRegular`; banner `SerifBold` at a fixed 34pt (it was the
  sampled text's size × 1.1, clamped to 28–48); toast title `SerifBold`, toast body
  `SerifSemibold`. Toast texts no longer add faux bold — the weight comes from the
  role font, as in the game's HUD. Font material is the font's own, no longer
  borrowed from a game text. Option-row sizes still come from the option prefab, as
  before.
- Existing callers of `SulfurToastApi` / `SulfurPopupApi` / the options API need no
  code change.
- **Behaviour mods may notice:**
  - After `ctx.Rebuild()`, the old `ctx` is dead: further calls on it do nothing.
    The rebuild runs `BuildPage` again with a new context.
  - Escape (and Enter on the category) on a custom page no longer rebuilds it, and
    Escape keeps the scroll position. A mouse click on a foldout no longer switches
    the page into keyboard-selection mode.
  - A stacked text input's label row is no longer a separate navigation stop (the
    input box is). Pages made only of library widgets no longer get a "No options."
    row.
  - `SulfurSettingRow`'s badge/button fields keep their English defaults; when a
    field still holds that default, the localized text is shown instead.
  - A duplicate page id is now a warning naming both registrations.
- **Performance:** custom category buttons no longer poll and allocate every frame;
  the language is no longer detected by scanning every loaded assembly every 0.5 s;
  `SulfurLocalization.Get` no longer allocates; reflection lookups of game members
  are cached and a missing member is logged once by name; page rebuilds detach the
  old rows before destroying them, so layout doesn't run over both; the text caret
  is only recomputed when it moves.
- **Robustness:** each Harmony patch resolves its target up front; if a game update
  removes one method, only that patch is skipped (with a warning) instead of every
  patch after it.

## 0.10.1

### Fixed

- **Small-button width now actually applies.** `AddSmallButton(label, onPressed,
  minWidth)` and per-button `SulfurButton.MinWidth` (and the auto-size from the
  label) previously had no effect on the rendered width: the button rows use
  `HorizontalLayoutGroup.childControlWidth = false`, which sizes each child from
  its `RectTransform.sizeDelta` and ignores `LayoutElement.min/preferredWidth`, so
  every small button stayed at the 120px baseline baked into the button and labels
  longer than ~10 characters were ellipsised (e.g. `Open-source repo` →
  `Open-sourc…`). The computed width is now written to the button's `sizeDelta`, so
  explicit `minWidth` and the label-based auto-size both take effect. **Behaviour
  change:** small buttons that relied on the old fixed 120px width now size to
  their label (auto-size range 96–180px); pass an explicit `minWidth` for a wider
  button.

## 0.10.0

### Added

- **Missing-glyph fallback chain.** Builds a runtime, dynamically-rasterized
  fallback chain from installed OS fonts so missing characters render even when
  the game's active font lacks them — a broad **CJK** face (for Chinese/Japanese/
  Korean missing from a Latin-only font like `Merriweather-Light SDF`) plus a
  **symbol** face (for ✓ ✗ ○ ● ▶ etc. missing even from the game's CJK subset
  `Chinese_Serif_NotoSansSC-Light SDF`). Fallback is **per-glyph**: the primary
  font and its style are kept for every character it already has; only the missing
  ones come from the chain, searched in order. Loaded via TMP's system-font path
  (reads the real font file + face index, so `.ttc` collections work); each
  candidate is verified to actually render its probe before use, and the chain is
  attached both globally and onto the active font's own fallback table (the game's
  per-language font isn't always reached via the global list). No bundled assets.
  Best-effort and idempotent; fixes blank "缺字" rows and also covers the game's
  own text.
- **`ctx.AddButtonRow(...)`** — lays several small buttons out left-to-right in a
  single row (left-aligned), instead of one stacked, right-aligned row per button.
  Each `SulfurButton` is `(label, onPressed)` with an optional fixed `minWidth`.
  Use it for compact button groups on one line; `AddSmallButton` is unchanged for
  the single-button-per-row case.
- **Live-update handles** for options-page rows, so a page can change parts of
  itself while it is open without a full `ctx.Rebuild()` (mirrors the existing
  `SulfurSettingHandle`):
  - `ctx.AddTextRow(text)` → `SulfurTextHandle` with `SetText` / `SetColor` /
    `SetVisible`. A plain, full-opacity text row meant for status lines and
    values that change at runtime.
  - `ctx.AddButtonRow(...)` now returns `IReadOnlyList<SulfurButtonHandle>`, one
    per button, with `SetLabel` / `SetInteractable` / `SetVisible`. Disabling a
    button greys its background and label. (Layout behavior is unchanged.)
  - `ctx.AddList()` → `SulfurListHandle` with `Update(ctx => { ... })` / `Clear()`
    / `SetVisible`. A refreshable section that rebuilds its rows in place using
    the normal `Add*` API — for live tables (e.g. player + ping + kick button).

### Changed

- `AddButtonRow` return type changed from `void` to
  `IReadOnlyList<SulfurButtonHandle>`. It was introduced in this same release and
  had not shipped, so no released API is affected. All other row/button APIs
  (`AddSmallButton`, `AddButton`, `AddDescription`, …) are unchanged.

## 0.9.0

### Added

- Toast notifications — a new HUD surface for transient messages in the
  top-right corner during normal gameplay. `SulfurToastApi.Show(...)` (message,
  optional title, optional duration) shows a fire-and-forget card that animates
  in, holds, and animates out on its own. Multiple toasts stack (newest on top,
  up to ~5); older ones glide aside to make room. Apple-style motion — slide +
  fade + scale on enter/exit with a slight overshoot, `SmoothDamp` restacking —
  all on unscaled time. Self-built uGUI overlay, passive (no `Time.timeScale`,
  no input capture, no cursor-lock change), language-correct font. See
  [Toasts](docs/API_REFERENCE.md#toasts).

### Changed

- Factored the banner's palette and font sampling into shared `SulfurUiTheme`
  and `SulfurUiFont` helpers, now used by both the banner and toasts. No
  behavior change to the banner.

## 0.8.0

### Added

- In-game popup banner — a new HUD surface independent of the Options screen.
  `SulfurPopupApi.ShowBanner(string)` / `HideBanner()` show a single centered,
  persistent message banner during normal gameplay/combat. It is a self-built
  uGUI overlay (`Canvas` + `TextMeshProUGUI`) styled to match SULFUR's chrome
  (warm panel fill, amber accent rules) with a language-correct font sampled
  from live game text. Display-only and passive — no `Time.timeScale` change,
  no input capture, no cursor-lock change; the caller keeps ownership of any
  keypress. Created lazily on first show and free while hidden. See
  [Popup banner](docs/API_REFERENCE.md#popup-banner).

## 0.7.3

### Fixed

- Custom rows (badges, descriptions, section headers, text/number inputs)
  rendered as blank boxes in non-English languages (CJK, Cyrillic, ...). The
  0.7.2 font-shrink fix sampled the font from a stable option prefab, but the
  prefab keeps the game's default Latin font asset, which lacks those glyphs.
  `FindSampleText` now takes the font *size* from the prefab (still stable, no
  compounding) but the font *asset and material* from the game's live,
  language-localized chrome, so custom rows follow the active language.

## 0.7.2

### Added

- `AddInlineTextInput` and `AddInlineNumberInput` on `SulfurOptionsContext`:
  inline rows where the label and input share one line (vanilla Options
  layout). Pass the same `labelWidth` to align several inputs at the same
  horizontal position. `AddInlineTextInput` supports `password: true` for a
  masked field. Additive — the stacked `AddTextInput` / `AddNumberInput`
  helpers are unchanged.

### Fixed

- Font size shrank one step on every `ctx.Rebuild()` (e.g. each value edit in
  a config editor) until it hit a floor, resetting only when the Options screen
  was reopened. `FindSampleText` based the native style on the first text in
  the container, but on rebuild that was the previous build's already-shrunk
  rows (`Object.Destroy` is deferred). It now samples a stable option prefab.

## 0.7.1

### Fixed

- Custom Options categories were re-injected without removing the previous
  ones, which could leave duplicate category buttons after the Options menu
  was set up again. The bridge now removes existing custom categories (tracked
  by `SulfurCustomCategoryMarker`) before re-injecting them.

### Changed

- Category cleanup is driven by the `SulfurCustomCategoryMarker` component
  instead of skipping injection when a marker already exists.

---

## 0.7.0

### Added

- Localization overhaul in `SulfurLocalization`:
  - multi-directory localization file search per plugin
  - automatic detection of language files by name (`en`, `ja`, `zh-CN`, ...)
  - merging of language maps loaded from multiple sources
  - current game language detection through reflection
  - language code normalization

---

## 0.6.3 Development Standard

Focus:

- large custom Options pages
- config editor performance
- row handle updates
- themed section groups
- foldout hierarchy styling

### Added

- `SulfurSettingHandle`
- `AddSettingToggleEx`
- `AddSettingTextEx`
- `AddSettingNumberEx`
- `AddSettingCycleEx`
- `BeginThemedGroup(...)`
- current/root container switching for scoped groups
- sample text cache
- native option width cache
- themed group documentation standard

### Changed

- Config editors should not rebuild on ordinary edits.
- Foldouts should use ASCII arrows `>` and `v`.
- Themed group borders should use `BorderOverlay`, not Unity `Outline`.
- Plugin and section foldouts should be visually larger than setting rows.
- Section contents should be visually grouped with a border.

### Compatibility

Old APIs remain valid:

```csharp
AddSettingToggle(...)
AddSettingText(...)
AddSettingNumber(...)
AddSettingCycle(...)
```

For performance-sensitive pages, use the `Ex` APIs.

---

## Known implementation notes

- Avoid Unicode arrow glyphs because SULFUR fonts may not include them.
- Avoid `TextAlignmentOptions.MidlineCenter`; use `Center`.
- Avoid `TextAlignmentOptions.MidlineLeft`; use `Left`.
- Avoid stretch anchors for themed groups if they disappear in native layout.
- Avoid Unity `Outline` for group borders.
