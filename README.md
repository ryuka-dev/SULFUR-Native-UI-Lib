# SULFUR Native UI Lib

Native OptionsScreen integration library for SULFUR BepInEx mods.

This library lets external BepInEx mods register custom pages inside the game's native Options screen and build UI using the game's existing OptionsScreen style.

Current target version: `0.9.0+`

The current development standard focuses on large custom Options pages, especially in-game config editor pages. The main design rule is:

> Do not rebuild the whole page for ordinary value changes.

Use row handles for local updates and themed groups for section-level visual grouping.

---

## What this library provides

- Native OptionsScreen page registration.
- Custom left-side Options category page.
- Native-style setting rows.
- Toggle, text, number, and cycle controls.
- Foldout groups.
- Themed section content groups.
- Footer and status text support.
- Localization helpers.
- High-performance row updates through `SulfurSettingHandle`.

---

## Typical use case

A config editor such as SULFUR Config can:

1. Scan loaded BepInEx plugins.
2. Read each plugin's `ConfigEntryBase`.
3. Convert config entries into readable UI rows.
4. Let users edit values in-game.
5. Keep edits as pending drafts.
6. Save only when the user presses Apply.

Recommended UX:

```text
Unchanged   = current draft equals saved cfg value
Pending     = user changed the UI draft, but it has not been saved
Applied     = value was written to cfg through ConfigFile.Save()
```

---

## Minimal page registration

```csharp
using BepInEx;
using Ryuka.Sulfur.NativeUI;

[BepInDependency("ryuka.sulfur.nativeui", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin("ryuka.example.mod", "Example Mod", "1.0.0")]
public sealed class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
        {
            PageId = "ryuka.example.mod",
            DisplayName = "Example Mod",
            SortOrder = 1000,
            GetDisplayName = () => "Example Mod",
            BuildPage = BuildPage
        });
    }

    private void OnDestroy()
    {
        SulfurOptionsApi.UnregisterPage("ryuka.example.mod");
    }

    private void BuildPage(SulfurOptionsContext ctx)
    {
        ctx.AddSection("Example");
        ctx.AddDescription("This is a custom native Options page.");
    }
}
```

---

## Recommended dependency

```csharp
[BepInDependency("ryuka.sulfur.nativeui", BepInDependency.DependencyFlags.HardDependency)]
```

---

## Recommended file layout

```text
BepInEx/plugins/SULFURNativeUILib/
└─ SULFUR Native UI Lib.dll

BepInEx/plugins/YourMod/
├─ YourMod.dll
└─ lang/
   ├─ en.json
   ├─ ja.json
   └─ zh-CN.json
```

---

## Documentation index

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/API_REFERENCE.md`](docs/API_REFERENCE.md)
- [`docs/PERFORMANCE_GUIDE.md`](docs/PERFORMANCE_GUIDE.md)
- [`docs/THEMED_GROUPS.md`](docs/THEMED_GROUPS.md)
- [`docs/FOLDOUT_STYLE.md`](docs/FOLDOUT_STYLE.md)
- [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md)
- [`docs/CONFIG_EDITOR_PATTERN.md`](docs/CONFIG_EDITOR_PATTERN.md)
- [`docs/EXAMPLES.md`](docs/EXAMPLES.md)
- [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md)
- [`docs/DEVELOPMENT_NOTES.md`](docs/DEVELOPMENT_NOTES.md)
- [`CHANGELOG.md`](CHANGELOG.md)
