# Architecture

## Overview

SULFUR Native UI Lib extends SULFUR's existing Options screen rather than creating a separate menu framework.

The main idea is:

```text
SULFUR OptionsScreen
├─ Native categories
└─ Custom categories injected by this library
   └─ Custom page content built by mod code
```

The library's core job is to bridge between:

- SULFUR's native `OptionsScreen`
- BepInEx mod code
- dynamically built Unity UI rows

## Major components

### Plugin

The BepInEx entry point for the library.

Responsibilities:

- initialize Harmony patches
- expose logging
- register the library as loaded
- keep version metadata

### SulfurOptionsApi

Global registry for custom pages.

Responsibilities:

- store registered `SulfurOptionsPage` objects
- sort pages by `SortOrder`
- find pages by `PageId`

### SulfurOptionsPage

A custom page definition.

Responsibilities:

- identify the page
- provide display name
- provide localized display name
- build content through `BuildPage`

### SulfurOptionsScreenBridge

The integration layer with the game's `OptionsScreen`.

Responsibilities:

- inject custom category buttons
- map category buttons to custom pages
- build custom page contents
- clear old custom content on rebuild
- preserve scroll position when rebuilding
- reset native selection state when needed
- hide footer when returning to native pages
- handle text input cancel / Backspace interaction

This class uses reflection because many game UI fields are private.

### SulfurOptionsContext

The public page-building surface passed to each page's `BuildPage`.

Responsibilities:

- add controls
- add sections and descriptions
- add inputs
- add messages
- add badges
- add footer
- trigger rebuild
- expose page id and container references

### Input field support

The game Options screen does not expose a native text input option prefab.

The library therefore creates semi-native `TMP_InputField` rows.

Input support includes:

- text field
- number field
- focus handling
- Backspace handling
- Escape/cancel handling
- visible caret overlay
- focus visual state

### SulfurInputCaretOverlay

A small visual overlay that shows a blinking caret.

Reason:

- The internal TMP caret can be invisible when creating the input field dynamically inside the native Options layout.
- The overlay is only visual. Actual input logic remains handled by `TMP_InputField`.

### Footer support

The footer is a fixed custom-page footer.

Responsibilities:

- show left summary text
- show status text
- show a primary button
- stay fixed while the main list scrolls
- hide when the user switches to native game categories

Typical usage:

```text
Dirty: 5 | Files: 3        Ready.        [Apply]
```

### Setting row helpers

Added to reduce repeated UI code for configuration-like pages.

A setting row includes:

- control
- description
- badges
- default button
- indentation
- optional message

The library does not decide whether a setting is dirty. It only renders `IsDirty`.

### Foldouts

Foldouts provide hierarchy:

```text
Plugin
  Section
    Setting
```

The foldout state is stored in `SulfurPageStateStore`.

### Page state store

A runtime-only store scoped by page id.

Used for:

- foldout state
- temporary page UI state
- UI-only filters

Not intended for:

- real mod configuration
- user settings that should persist to disk

## Data flow

### Registering a page

```text
Mod Awake()
  → SulfurOptionsApi.RegisterPage(...)
  → Bridge injects category when OptionsScreen is set up
  → User opens custom page
  → BuildPage(ctx) is called
```

### Rebuilding a page

```text
ctx.Rebuild()
  → Bridge finds current custom page
  → Save scroll position
  → Destroy old custom rows
  → BuildPage(ctx)
  → Restore selection and scroll position
```

### Handling text input

```text
User clicks TMP_InputField
  → TMP_InputField receives focus
  → Backspace is intercepted from OptionsScreen cancel handling
  → Escape first deactivates input field
  → visible caret overlay displays cursor
```

## Design boundaries

The library must remain UI-only.

It should not depend on BepInEx `ConfigEntryBase`.

Correct separation:

```text
SULFUR Native UI Lib
- render rows
- handle input
- handle native page integration
- provide callbacks

SULFUR Config
- scan ConfigFile
- decide dirty state
- parse config values
- apply changes
- save cfg
- create backups
- determine tags
```

## Why use native OptionsScreen

Benefits:

- pause behavior is handled by the game
- input routing is closer to native behavior
- visual style matches SULFUR
- less need for custom IMGUI windows
- easier for players to find settings

## Known fragile points

The bridge relies on private field names such as:

- options containers
- option prefabs
- category objects
- selected indices
- scroll parent

If the game updates these internals, the bridge may need changes.

## Recommended compatibility strategy

- Keep reflection helpers centralized.
- Avoid changing public API names casually.
- Add new helpers as extension methods when possible.
- Keep test plugin updated with each new API.
- Test against current game version after major SULFUR updates.
