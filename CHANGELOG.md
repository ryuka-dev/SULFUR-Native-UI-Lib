# Changelog

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
  `docs/TOAST_NOTIFICATIONS.md`.

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
  `docs/POPUP_BANNER.md`.

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
