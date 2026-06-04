# Development Notes

This document records the development intent and implementation decisions for SULFUR Native UI Lib.

It is written for future maintainers and AI-assisted development sessions.

## Project purpose

The library exists to support native-style in-game mod settings pages for SULFUR.

It began as groundwork for a future **SULFUR Config** mod, whose goal is to reduce the need to close the game, edit `.cfg` files manually, and restart.

The library was separated from SULFUR Config because the UI layer should be reusable by other mods.

## Current baseline

Current stable baseline: **v0.6.1**

At this point the library can be treated as a usable internal foundation.

Validated features:

- custom Options screen pages
- native category injection
- custom page rebuild
- scroll preservation
- localization
- native-like button/toggle/cycle/slider
- semi-native text input
- semi-native number input
- visible input caret
- Backspace behavior fix
- fixed footer
- footer Apply
- footer status text
- warning/error/success messages
- badge rows
- setting row helper API
- per-setting Default button
- foldouts
- page state store
- plugin/section hierarchy styling
- setting row indentation

## Design rules

### 1. Keep the library UI-only

Do not add BepInEx configuration management to the library.

Do not add:

- `ConfigEntryBase`
- `ConfigFile`
- config scanning
- applying changes
- saving `.cfg`
- backup creation
- dirty-state calculation

These belong to SULFUR Config.

### 2. Prefer page-building callbacks

The library should expose UI callbacks and let the consuming mod decide behavior.

Example:

```csharp
ctx.AddSettingToggle(row, value, newValue =>
{
    value = newValue;
    ctx.Rebuild();
});
```

### 3. Rebuild should be safe

Rebuild should:

- not reset foldout state
- preserve scroll position where possible
- avoid leaving old UI elements behind
- not leak footer state into native pages

### 4. Do not rebuild on every typed character

For text input, especially search/filter input, use:

```text
draft value
Apply Filter button
active value
```

This avoids losing input focus and prevents excessive page rebuilds.

### 5. Use setting row helpers for config-like pages

Avoid manually repeating:

```text
control
description
badges
default button
indent
```

Use `SulfurSettingRow` helpers.

### 6. Foldout hierarchy should match future SULFUR Config

Recommended hierarchy:

```text
Plugin
  Section
    Setting
```

Do not create foldouts for every setting.

## Version history summary

### v0.1

Initial proof of concept.

- Custom page injection
- Basic native option controls
- Early menu integration

### v0.2

Localization and native UI stabilization.

- Per-plugin `lang/*.json`
- Better category label handling
- Avoided language log spam
- Fixed `9007` category display problem

### v0.3

Input support.

- TextInput
- NumberInput
- Backspace/cancel handling
- Visible caret overlay
- Rebuild support

### v0.4

Configuration editor direction.

- Fixed footer bar
- Global Apply button
- Status text
- Badge rows
- Warning/error/success messages
- Per-setting small buttons

### v0.5

Setting row abstraction.

- `SulfurSettingRow`
- `AddSettingToggle`
- `AddSettingText`
- `AddSettingNumber`
- `AddSettingCycle`
- `AddSettingEnum`

### v0.6

Foldouts and page state.

- `SulfurPageStateStore`
- `GetState` / `SetState`
- `AddFoldout`
- plugin/section grouping

### v0.6.1

Visual hierarchy and quality pass.

- Better plugin foldout styling
- Better section foldout styling
- setting row indentation
- wider small buttons
- footer truncation fix

## Important implementation notes

### TextInput is semi-native

The game Options screen did not expose a text input option prefab.

The library creates its own `TMP_InputField` styled to match native rows.

Known support work:

- `TMP_InputField` row creation
- text area
- placeholder
- caret overlay
- Backspace interception
- Escape deactivation
- focus highlight

### Foldouts use buttons

Foldout rows are custom UI rows, not native `OptionsScreenOption` rows.

This is intentional because plugin/section headers are structural UI, not actual setting options.

### Footer should only show on custom pages

The footer must not appear on native game categories.

If the user switches to General / Controls / Display / Audio, hide or clear custom footer.

### Page state is runtime-only

`SulfurPageStateStore` is not persistence.

It is okay if state resets when the game restarts.

If future mods want persistent UI state, they should store it themselves.

## Future roadmap

### v0.7 candidate

- Start SULFUR Config native page integration
- Render real `ConfigEntryModel` values using setting row API
- Keep F10 IMGUI panel as fallback/debug
- Implement plugin and section grouping using foldouts
- Implement filter controls using draft/apply pattern
- Use footer Apply to save all dirty values

### v0.8 candidate

- More advanced validation messages
- Better number input invalid-state display
- Optional confirmation dialog support
- Better keyboard/gamepad navigation polish

### v1.0 criteria

The library can be considered 1.0 when:

- SULFUR Config can use it for real config editing
- all major value types are covered
- input behavior is stable
- custom footer is stable
- foldout state is stable
- game update compatibility has been checked
- README/API docs/examples are complete
