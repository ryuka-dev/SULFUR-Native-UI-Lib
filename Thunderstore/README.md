# SULFUR Native UI Lib

A library that lets SULFUR mods add their own pages to the game's native Options screen and
show messages during play.

## For players

This mod does nothing by itself. Install it when another mod lists it as a dependency.

## Features

- Mod pages in the Options screen, built from the game's own rows: toggles, sliders,
  choices, buttons, text and number inputs, foldouts, badges, a footer with a primary button
- Rows that update in place while the page is open
- Full keyboard and gamepad navigation
- Toast notifications in the top-right corner
- A centred message banner during play
- The game's fonts for the current language, with automatic glyph fallback for Chinese,
  Japanese, Korean and symbols
- Follows the game's language; the library's own text is translated into all 15 game
  languages

## Installation

Install with a mod manager, or by hand:

```text
BepInEx/plugins/ryuka_labs-SULFUR_Native_UI_Lib/
├─ SULFUR Native UI Lib.dll
└─ lang/
```

Keep the `lang/` folder next to the DLL; without it the library's own text shows in English.

## Requirements

- SULFUR 1.0.5
- BepInEx 5 (BepInExPack 5.4.2100)

## For mod developers

Source and documentation: https://github.com/ryuka-dev/SULFUR-Native-UI-Lib

The repository contains only the library's own source and documentation, no game files or
third-party binaries.
