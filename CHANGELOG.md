# Changelog

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
