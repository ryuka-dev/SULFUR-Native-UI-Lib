# Architecture

The library extends the game's own `OptionsScreen` instead of drawing a separate menu:
custom categories are injected into its category list and their pages are built from the
game's option prefabs plus self-built rows. Two HUD overlays (toasts, banner) are
independent of it. Game facts behind the design: `MAINTAINER_NOTES.md`.

## Components

| Folder | Types | Role |
|---|---|---|
| `Core/` | `Plugin` | Entry point. Loads the library's `lang/`, `PatchAll`, installs the font and language hooks by hand, registers the glyph fallback; its `Update` only converges the startup language. |
| | `SulfurFonts` (public), `SulfurGameFonts`, `SulfurFontFallback` | Fonts by role. `SulfurGameFonts` is the adapter to the game's font cache; `SulfurFontFallback` builds the OS-font CJK/symbol fallback. |
| | `SulfurReflection`, `SulfurUiTheme` | Cached member lookups (missing members logged once); shared HUD palette. |
| `Options/` | `SulfurOptionsApi`, `SulfurOptionsPage` | Page registry, sorted by `SortOrder`, then name. |
| | `SulfurOptionsContext` | The public page builder. One instance per build. |
| `Integration/` | `SulfurOptionsScreenBridge` | Owns custom-page state per screen: category injection, page build and rebuild, footer, scroll and selection, the navigation list. |
| | `SulfurOptionsScreenPatch` | Harmony patches on `OptionsScreen` (and the category label, pause menu); targets resolved in `Prepare()` through `SulfurPatchTargets`. |
| | `SulfurCustomCategoryMarker`, `SulfurNavigationProxy`, `SulfurMainMenuHoverGuard` | Marks injected categories (and writes their label); keyboard/gamepad proxies for library widgets; the main-menu hover workaround. |
| `Rows/` | handles, foldouts, setting rows | Public row API. `SulfurOptionBinding` owns each native row's listeners; `SulfurOptionRowParts` / `SulfurSettingParts` hold what a handle needs; `SulfurButtonRowNavigator`, `SulfurBadgeFlowLayout`, `SulfurRowHeightFitter` are layout/navigation helpers. |
| `Input/` | `SulfurTextInputBinding`, `SulfurNumberInput`, `SulfurInputCaretOverlay`, `SulfurInputVisualState` | Self-built `TMP_InputField` rows (the game has no text-input prefab): value parsing, Escape/Backspace handling, a visible caret, focus look. |
| `State/` | `SulfurPageStateStore` (internal), `SulfurOptionsStateExtensions` | Runtime-only key/value store per page id; mods reach it only through the extensions. |
| `Localization/` | `SulfurLocalization`, `SulfurLanguage`, `SulfurGameLanguage`, `SulfurUiText` | Per-plugin `lang/*.json`; the game's language (adapter `SulfurGameLanguage`); the library's own strings. |
| `Toast/`, `Popup/` | `SulfurToastApi` / `SulfurPopupApi` and their controllers | Self-built `ScreenSpaceOverlay` canvases, created on first use, `DontDestroyOnLoad`, no raycasts. |

## Page build

```text
OptionsScreen.SetupMenu (postfix)   -> bridge injects one category per registered page
category selected (SetCategory)     -> bridge.TryShowCustomPage
                                         same page already shown and alive -> keep it (Enter / Escape)
                                         otherwise BuildCustomPage
BuildCustomPage
  end previous page: its context is invalidated
  destroy old rows (detached first), clear the footer
  new SulfurOptionsContext -> page.BuildPage(ctx)
    throws / page unregistered -> discard partial rows, invalidate, one error row
  no row of the page registered -> "No options." info row
  select first row (keyboard/gamepad) or leave focus on the category menu
```

`ctx.Rebuild()` → `RebuildCurrentCustomPage`: saves scroll position, selected index and
input mode, then runs `BuildCustomPage` again, which invalidates the old context and builds
with a new one, then restores scroll and selection; button rows restore their in-row focus
by navigation index. A `Rebuild()` from inside `BuildPage` replaces the outer build, which
then stops touching its state.

`OptionsScreen.Hide` / `Show`, switching category and a destroyed screen also end the page
and invalidate its context. Every row-writing call on the context goes through
`EnsureAlive`, because the options container is shared by all pages.

## Navigation

The game reaches only entries of its private `options` list. Native rows built by the
library are registered there in hierarchy order; every self-built widget gets a navigation
proxy (a stretched clone of the game's button prefab, only its border visible) that turns
Use / left-right into the widget's action. `SulfurListHandle` unregisters its rows before
destroying them and re-registers rebuilt ones at its position.

## Fragile points

Private field names of `OptionsScreen` (containers, prefabs, category objects, selection
indices, scroll parent). All reflection goes through `SulfurReflection`, which caches
lookups and logs a missing member once; re-check after a game update. Rules for changing
the library: `MAINTAINER_NOTES.md`.
