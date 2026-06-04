# SULFUR Native UI Lib

SULFUR Native UI Lib is a BepInEx library for adding native-style custom pages to SULFUR's in-game Options screen.

The library is designed for mod authors who want their settings, tools, or configuration editors to appear inside the game's existing UI instead of using IMGUI popups or external config editing.

Current baseline: **v0.6.1**

## What this library provides

- Custom pages inside SULFUR's native Options screen
- Native-style controls:
  - Button
  - Toggle
  - Cycle / enum-style selector
  - Slider
  - Text input
  - Number input
- Semi-native `TMP_InputField` support with:
  - visible caret overlay
  - Backspace handling
  - Escape / cancel handling
  - focus highlight support
- Fixed footer bar for custom pages
- Global footer button, suitable for "Apply"
- Status text in the footer
- Warning / error / success / info message rows
- Badge rows for metadata such as `Dirty`, `Live Apply`, `Restart Required`, `Advanced`, or `Dangerous`
- Setting row helper API
- Per-setting `Default` button support
- Foldout groups for plugin / section hierarchy
- Page state store for preserving foldout state across rebuilds
- Rebuild support with scroll-position preservation
- Plugin localization using per-mod `lang/*.json`

## What this library does not do

This library does **not** manage BepInEx configuration files directly.

It does not:

- scan `ConfigFile`
- inspect `ConfigEntryBase`
- decide what is dirty
- apply changes
- save `.cfg` files
- create backups
- decide whether a setting is dangerous
- decide whether a setting requires restart

Those responsibilities belong to the consuming mod, such as **SULFUR Config**.

This library only renders native-style UI and exposes callbacks.

## Installation

Place the compiled library DLL in a BepInEx plugin folder, for example:

```text
BepInEx/plugins/SULFURNativeUILib/SULFUR Native UI Lib.dll
```

Consuming mods should depend on the library:

```csharp
[BepInDependency("ryuka.sulfur.nativeui", BepInDependency.DependencyFlags.HardDependency)]
```

## Minimal usage

```csharp
using BepInEx;
using Ryuka.Sulfur.NativeUI;

namespace ExampleMod
{
    [BepInDependency("ryuka.sulfur.nativeui", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("example.mod", "Example Mod", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private bool enabled = true;

        private void Awake()
        {
            SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
            {
                PageId = "example_mod",
                DisplayName = "Example Mod",
                SortOrder = 100,
                BuildPage = ctx =>
                {
                    ctx.AddSection("General");

                    ctx.AddSettingToggle(
                        new SulfurSettingRow
                        {
                            Label = "Enable Feature",
                            Description = "Enable this feature.",
                            LiveApply = true
                        },
                        enabled,
                        value =>
                        {
                            enabled = value;
                            ctx.SetFooterStatus("Changed.");
                        });

                    ctx.SetFooter("Dirty: 0", "Ready.", "Apply", () =>
                    {
                        ctx.SetFooterStatus("Applied.");
                    });
                }
            });
        }
    }
}
```

## Localization

Each consuming mod can include a `lang` folder next to its DLL:

```text
YourMod/
├─ YourMod.dll
└─ lang/
   ├─ en.json
   ├─ ja.json
   └─ zh-CN.json
```

Example JSON format:

```json
{
  "entries": [
    {
      "key": "page.name",
      "value": "My Mod Settings"
    },
    {
      "key": "setting.enable.name",
      "value": "Enable Feature"
    }
  ]
}
```

Load localization before registering your page:

```csharp
SulfurLocalization.LoadPluginLocalization(
    PluginGuid,
    typeof(Plugin).Assembly.Location);
```

Then resolve strings:

```csharp
private static string L(string key, string fallback)
{
    return SulfurLocalization.Get(PluginGuid, key, fallback);
}
```

## Recommended repository layout

```text
SULFUR-Native-UI-Lib/
├─ README.md
├─ CHANGELOG.md
├─ docs/
│  ├─ API_REFERENCE.md
│  ├─ EXAMPLES.md
│  ├─ ARCHITECTURE.md
│  └─ DEVELOPMENT_NOTES.md
└─ src/
   ├─ SULFUR Native UI Lib/
   └─ SULFUR Native UI Test/
```

## Current stability

The v0.6.1 baseline has been tested with:

- custom Options screen page registration
- localization loading
- buttons
- toggles
- cycle controls
- sliders
- text input
- number input
- Backspace behavior
- visible input caret
- fixed footer
- global Apply
- per-setting Default
- badges
- foldouts
- page state
- rebuild with scroll preservation

## Known limitations

- This is not a general-purpose Unity UI framework.
- It is designed specifically around SULFUR's `OptionsScreen`.
- Text input is semi-native. It creates `TMP_InputField` elements styled to match the native UI because the game's Options screen does not expose a native text input option prefab.
- Heavy rebuild usage should be avoided while the user is actively typing. Use a draft text field plus an "Apply Filter" button for search/filter pages.
- The library relies on reflected access to SULFUR private UI fields, so future game updates may require compatibility fixes.

## License

Add your chosen license here before publishing.
