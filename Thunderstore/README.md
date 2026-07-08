# SULFUR Native UI Lib

A native Options screen UI library for SULFUR BepInEx mods.

This library allows other modders to register custom pages inside the game's native Options screen and build native-style UI for mod configuration, debug panels, help pages, and other in-game tools.

## Features

- Native Options screen page registration
- Custom Options categories
- Foldout sections
- Native-style setting rows
- Toggle, text, number, and cycle controls
- Themed section groups
- Footer/status controls
- Localization helpers
- High-performance row updates for large config pages

## For Players

This is a library mod.

It usually does nothing by itself. Install it only if another mod requires it.

## For Mod Developers

Source code and full documentation are available on GitHub:

https://github.com/ryuka-dev/SULFUR-Native-UI-Lib

The GitHub documentation is the latest source of truth for API usage, examples, performance rules, themed groups, localization, and config editor patterns.

## Installation

Install this mod with BepInEx.

Place the DLL under:

```text
BepInEx/plugins/SULFURNativeUILib/
```

Recommended structure:

```text
BepInEx/plugins/SULFURNativeUILib/
└─ SULFUR Native UI Lib.dll
```

## Requirements

* SULFUR
* BepInEx 5

## Source Code

The source code is available here:

[https://github.com/ryuka-dev/SULFUR-Native-UI-Lib](https://github.com/ryuka-dev/SULFUR-Native-UI-Lib)

The repository contains only original library source code and documentation. It does not include SULFUR game files, Unity assemblies, BepInEx binaries, paid assets, or decompiled game source.

## Notes

This is a developer library. End users only need it when another mod lists it as a dependency.


# Changelog

## 0.6.1

- Added custom Options page registration.
- Added basic native-style row creation helpers.
- Added localization helper support.

## 0.6.2

- Added `SulfurSettingHandle`.
- Added high-performance `AddSettingToggleEx`, `AddSettingTextEx`, `AddSettingNumberEx`, and `AddSettingCycleEx`.
- Cached sample text and native option width per page context.
- Preserved existing `AddSettingToggle`, `AddSettingText`, `AddSettingNumber`, and `AddSettingCycle` APIs.

## 0.6.3

- Added themed section group documentation standard.
- Added guidance for large config editor pages.
- Improved foldout hierarchy styling standard.
- Added row-handle based update pattern for high-performance UI updates.
- Documented that normal value edits should not rebuild the whole page.
- Documented ASCII foldout arrows for TMP font compatibility.
- Documented themed group border behavior and Unity Outline pitfalls.

## 0.7.0

- Reworked localization loading: searches multiple folders for language files, auto-detects language files by name, merges entries from several sources, and follows the current game language.

## 0.7.1
Fixed
- Fixed an issue where custom Native UI pages could disappear every second time the Options menu was opened.
- Custom option categories are now rebuilt more safely when the game Options screen is opened again.
- Improved custom category cleanup to avoid stale category buttons, invalid category references, or mismatched OptionsScreen state.
- Improved custom category label refreshing so labels remain valid after the game UI is rebuilt or re-enabled.

Changed
- Native UI custom categories are now recreated after OptionsScreen setup instead of relying on old injected category objects.
- This should make custom pages more stable when closing and reopening the Options menu with ESC.

## 0.7.2
Added
- New inline input rows for mod developers (`AddInlineTextInput` / `AddInlineNumberInput`): the label and input field sit on the same line, matching the vanilla Options layout, with optional password masking.

Fixed
- Fixed text and option font sizes shrinking a little every time a page refreshed (for example on each edit in a config editor), until the Options screen was reopened.

## 0.7.3
Fixed
- Fixed custom rows (badges, descriptions, section headers, text/number inputs) showing as blank boxes in non-English languages such as Chinese, Japanese, Korean, and Russian. They now use the game's current language font instead of the default Latin font.

## 0.8.0
Added
- New in-game popup banner for mod developers (`SulfurPopupApi.ShowBanner` / `HideBanner`): a single centered, persistent on-screen message that can appear during normal gameplay and combat, styled to match the game's UI. It is display-only — it never pauses the game, captures input, or changes the cursor, so the calling mod keeps full control of any keypress.

## 0.9.0
Added
- New in-game toast notifications for mod developers (`SulfurToastApi.Show`): short messages that slide into the top-right corner during gameplay, hold briefly, then animate away. Supports an optional title and custom duration, and multiple toasts stack with smooth slide/fade animations. Display-only and passive — it never pauses the game, captures input, or changes the cursor.

## 0.10.0
Added
- `AddButtonRow(...)` for mod developers: lays several small buttons out left-to-right in one row instead of one stacked row per button. `AddSmallButton` is unchanged for the single-button case.
- Live-update row handles so a page can change parts of itself while open without a full rebuild: `AddTextRow` returns a `SulfurTextHandle`, `AddButtonRow` returns per-button `SulfurButtonHandle`s, and `AddList` returns a `SulfurListHandle` for refreshable sections.
- Automatic missing-glyph fallback built from installed OS fonts (a broad CJK face plus a symbol face), applied per-glyph on top of the game's active font. Fixes blank rows and missing symbols (e.g. checkmarks) in non-English languages without any bundled font assets.

Changed
- `AddButtonRow` now returns `IReadOnlyList<SulfurButtonHandle>` instead of `void`. It was introduced in this same release and had not shipped before, so no released API is affected.

## 0.10.1
Fixed
- Small-button width now actually applies. Previously the `minWidth` argument and the label-based auto-size had no effect on the rendered width, so every small button stayed at a fixed 120px and labels longer than ~10 characters were cut off with an ellipsis (e.g. `Open-source repo` → `Open-sourc…`). Buttons now size to their label, and an explicit `minWidth` widens them as intended.