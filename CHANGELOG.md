# Changelog

## 0.6.1

UI polish pass.

- Improved plugin foldout visual hierarchy.
- Improved section foldout visual hierarchy.
- Added setting row indentation support.
- Improved small button width handling.
- Improved footer text layout to reduce truncation.
- Improved config-editor-like visual structure:
  - Plugin
  - Section
  - Setting

## 0.6.0

Added page state and foldout support.

- Added `SulfurPageStateStore`.
- Added `ctx.GetState<T>()`.
- Added `ctx.SetState<T>()`.
- Added `ctx.HasState()`.
- Added `ctx.RemoveState()`.
- Added `ctx.ClearPageState()`.
- Added `ctx.ToggleBoolState()`.
- Added `ctx.AddFoldout()`.
- Added `ctx.AddFoldoutWithBadges()`.
- Added plugin and section foldout patterns.
- Updated test plugin to simulate multiple mods and sections.

## 0.5.0

Added setting row abstraction.

- Added `SulfurSettingRow`.
- Added `ctx.AddSettingToggle()`.
- Added `ctx.AddSettingText()`.
- Added `ctx.AddSettingNumber()`.
- Added `ctx.AddSettingCycle()`.
- Added `ctx.AddSettingEnum()`.
- Added setting row metadata rendering:
  - Dirty / Clean
  - Restart Required
  - Live Apply
  - Advanced
  - Hidden
  - Dangerous
  - ExtraBadges
  - Default button
  - Optional message

## 0.4.0

Changed direction toward configuration editor support.

- Added fixed footer bar for custom pages.
- Added footer status text.
- Added global primary footer button.
- Added `AddWarning`.
- Added `AddError`.
- Added `AddSuccess`.
- Added `AddMessage`.
- Added `AddBadgeRow`.
- Added `AddSmallButton`.
- Added `AddDefaultButton`.
- Improved input field behavior.
- Added focus highlight support.
- Updated test plugin to simulate global Apply + per-entry Default.

## 0.3.0

Added input controls.

- Added semi-native `TextInput`.
- Added semi-native `NumberInput`.
- Added `TMP_InputField` support.
- Added visible caret overlay.
- Added Backspace/cancel handling.
- Added page `Rebuild()`.
- Added section/description/spacer/read-only helpers.
- Fixed input conflict with OptionsScreen cancel behavior.

## 0.2.0

Localization and native category stabilization.

- Added plugin-localization loading from `lang/*.json`.
- Added language fallback handling.
- Added custom category label repair to avoid enum numeric labels such as `9007`.
- Reduced log spam from language polling.
- Improved custom category injection.

## 0.1.0

Initial proof of concept.

- Injected a custom page into SULFUR OptionsScreen.
- Added native button support.
- Added native toggle support.
- Added native cycle support.
- Added native slider support.
- Added basic test plugin.
