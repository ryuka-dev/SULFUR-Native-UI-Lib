# Changelog

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
