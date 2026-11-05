# SULFUR Native UI Lib

A BepInEx library that lets SULFUR mods add their own pages to the game's native Options
screen, built from rows that look and behave like the game's own (mouse, keyboard and
gamepad), plus two HUD surfaces for in-game messages.

Current target version: `0.11.1`

## Features

- Custom pages in the Options screen's category list (`SulfurOptionsApi`).
- Native toggle, slider, cycle, button, text and number rows; sections, descriptions,
  messages, badges, foldouts, themed groups, a fixed footer with a primary button.
- Row handles: change a row while the page is open, without rebuilding it
  (`AddToggleRow`, `AddSliderRow`, `AddCycleRow`, `AddBadgeRowEx`, `AddSetting*Ex`, ...).
- Config-editor setting rows with badges, message line and Default button.
- Keyboard and gamepad navigation for every library widget.
- Toasts (`SulfurToastApi`) and a centred banner (`SulfurPopupApi`) during gameplay.
- The game's fonts by role (`SulfurFonts`), with automatic CJK/symbol glyph fallback.
- The game's language with one call (`SulfurLanguage.Follow`) and per-mod
  `lang/*.json` lookup (`SulfurLocalization`).

## Getting started

### Requirements

- SULFUR 1.0.5
- BepInEx 5 (`BepInEx-BepInExPack-5.4.2100`)
- A mod project targeting .NET Framework 4.7.2

### Project file

Reference the library and the game assemblies it exposes. None of them are copied to
your output: never ship `SULFUR Native UI Lib.dll` in your package, the player installs
the library itself.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>YourName.Sulfur.Example</AssemblyName>
    <!-- Adjust both paths. The library in ProfileDir must be 0.11.0 or later: check
         manifest.json in its folder (an older one fails with CS0246 on the new types). -->
    <SulfurManagedDir>C:\Program Files (x86)\Steam\steamapps\common\SULFUR\Sulfur_Data\Managed</SulfurManagedDir>
    <ProfileDir>$(APPDATA)\com.kesomannen.gale\sulfur\profiles\Default</ProfileDir>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="SULFUR Native UI Lib">
      <HintPath>$(ProfileDir)\BepInEx\plugins\ryuka_labs-SULFUR_Native_UI_Lib\SULFUR Native UI Lib.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="BepInEx">
      <HintPath>$(ProfileDir)\BepInEx\core\BepInEx.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="PerfectRandom.Sulfur.Core">
      <HintPath>$(SulfurManagedDir)\PerfectRandom.Sulfur.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.TextMeshPro">
      <HintPath>$(SulfurManagedDir)\Unity.TextMeshPro.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>$(SulfurManagedDir)\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(SulfurManagedDir)\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.UI">
      <HintPath>$(SulfurManagedDir)\UnityEngine.UI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <None Update="lang\*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

Your `lang/` folder sits next to the csproj; the last item copies it to the output.

### Dependency

Put `[BepInDependency("ryuka.sulfur.nativeui")]` on the plugin class, and this in your
Thunderstore `manifest.json`:

```json
"dependencies": [
  "BepInEx-BepInExPack-5.4.2100",
  "ryuka_labs-SULFUR_Native_UI_Lib-0.11.0"
]
```

For an optional dependency see
[Soft dependency](docs/API_REFERENCE.md#soft-dependency).

### Installed layout

```text
BepInEx/plugins/
├─ ryuka_labs-SULFUR_Native_UI_Lib/
│  ├─ SULFUR Native UI Lib.dll
│  └─ lang/                      the library's own text (15 languages)
└─ YourName-Example/
   ├─ YourName.Sulfur.Example.dll
   └─ lang/
      ├─ en.json
      └─ zh-CN.json ...
```

Your Thunderstore zip holds, at its top level: `manifest.json`, `icon.png` (256x256),
`README.md`, your DLL and `lang/`. No `.pdb` is needed.

### Starter plugin

A page with three settings bound to the mod's BepInEx config, a foldout with a button
row, a footer button that restores the defaults, and a toast. Every value change goes
through a row handle; the page is never rebuilt for it.

```csharp
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Ryuka.Sulfur.NativeUI;

namespace YourName.Sulfur.Example
{
    [BepInPlugin(PluginGuid, "Example", "1.0.0")]
    [BepInDependency("ryuka.sulfur.nativeui")]
    public sealed class ExamplePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "yourname.sulfur.example";

        private ConfigEntry<bool> modEnabled;
        private ConfigEntry<float> volume;
        private ConfigEntry<int> difficulty;

        private void Awake()
        {
            modEnabled = Config.Bind("General", "Enabled", true, "Turns the mod on or off.");
            volume = Config.Bind("General", "Volume", 0.8f, "Effect volume, 0 to 1.");
            difficulty = Config.Bind("General", "Difficulty", 1, "0 = easy, 1 = normal, 2 = hard.");

            // Loads lang/*.json next to this DLL.
            SulfurLocalization.LoadPluginLocalization(PluginGuid, Info.Location);

            SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
            {
                PageId = PluginGuid,
                DisplayName = "Example",
                GetDisplayName = () => T("plugin.name", "Example"),
                BuildPage = BuildPage
            });
        }

        private void OnDestroy()
        {
            SulfurOptionsApi.UnregisterPage(PluginGuid);
        }

        private static string T(string key, string fallback)
        {
            return SulfurLocalization.Get(PluginGuid, key, fallback);
        }

        // Runs on every open, ctx.Rebuild() and foldout click, with a new ctx each time.
        private void BuildPage(SulfurOptionsContext ctx)
        {
            ctx.AddSection(T("section.General", "General"));

            SulfurToggleHandle enabledRow = ctx.AddToggleRow(
                T("entry.General.Enabled.name", "Enabled"),
                T("entry.General.Enabled.description", "Turns the mod on or off."),
                modEnabled.Value,
                v => modEnabled.Value = v);

            SulfurSliderHandle volumeRow = ctx.AddSliderRow(
                T("entry.General.Volume.name", "Volume"),
                volume.Value, 0f, 1f, 0.05f,
                v => volume.Value = v);

            List<string> levels = new List<string>
            {
                T("page.difficulty.easy", "Easy"),
                T("page.difficulty.normal", "Normal"),
                T("page.difficulty.hard", "Hard")
            };

            SulfurCycleHandle difficultyRow = ctx.AddCycleRow(
                T("entry.General.Difficulty.name", "Difficulty"),
                levels,
                difficulty.Value,
                (index, text) => difficulty.Value = index);

            if (ctx.AddFoldout("presets", T("page.presets", "Presets"), false))
            {
                ctx.AddDescription(T("page.presets.description", "Set the volume in one click."));
                ctx.AddButtonRow(
                    new SulfurButton(T("page.preset.quiet", "Quiet"), () =>
                    {
                        volume.Value = 0.2f;
                        volumeRow.SetValueWithoutNotify(0.2f);
                    }),
                    new SulfurButton(T("page.preset.loud", "Loud"), () =>
                    {
                        volume.Value = 1f;
                        volumeRow.SetValueWithoutNotify(1f);
                    }));
            }

            ctx.SetFooter(
                "Example 1.0.0",
                T("page.footer.status", "Changes are saved immediately."),
                T("page.footer.defaults", "Defaults"),
                () =>
                {
                    modEnabled.Value = (bool)modEnabled.DefaultValue;
                    volume.Value = (float)volume.DefaultValue;
                    difficulty.Value = (int)difficulty.DefaultValue;

                    enabledRow.SetValueWithoutNotify(modEnabled.Value);
                    volumeRow.SetValueWithoutNotify(volume.Value);
                    difficultyRow.SetIndexWithoutNotify(difficulty.Value);

                    SulfurToastApi.Show(T("plugin.name", "Example"), T("page.toast.defaults", "Defaults restored."));
                });
        }
    }
}
```

`lang/en.json` (UTF-8 without BOM). Every fallback string in the code must equal its
`en.json` value. Other languages use the same keys, e.g. `lang/zh-CN.json`; a missing key
falls back to English (`zh-TW` tries `zh-CN` first).

```json
{
  "entries": [
    { "key": "plugin.name", "value": "Example" },
    { "key": "section.General", "value": "General" },
    { "key": "entry.General.Enabled.name", "value": "Enabled" },
    { "key": "entry.General.Enabled.description", "value": "Turns the mod on or off." },
    { "key": "entry.General.Volume.name", "value": "Volume" },
    { "key": "entry.General.Difficulty.name", "value": "Difficulty" },
    { "key": "page.difficulty.easy", "value": "Easy" },
    { "key": "page.difficulty.normal", "value": "Normal" },
    { "key": "page.difficulty.hard", "value": "Hard" },
    { "key": "page.presets", "value": "Presets" },
    { "key": "page.presets.description", "value": "Set the volume in one click." },
    { "key": "page.preset.quiet", "value": "Quiet" },
    { "key": "page.preset.loud", "value": "Loud" },
    { "key": "page.footer.status", "value": "Changes are saved immediately." },
    { "key": "page.footer.defaults", "value": "Defaults" },
    { "key": "page.toast.defaults", "value": "Defaults restored." }
  ]
}
```

The `plugin.*`, `section.*` and `entry.*` keys follow the format SULFUR Config reads for
config entries, so the same file also translates your settings there; see
[Localization](docs/LOCALIZATION.md).

## Upgrading from 0.10.x

Mods compiled against 0.10.x keep working: no public signature was changed or removed.
New code should use the calls marked "new code" in the
[task table](docs/API_REFERENCE.md#which-call-to-use). Some behaviour changed in ways a mod
may notice (most importantly: after `ctx.Rebuild()` the old `ctx` is dead); see
"Behaviour mods may notice" under 0.11.0 in the [changelog](CHANGELOG.md#0110).

## Documentation

- [API reference](docs/API_REFERENCE.md): which call to use, page lifecycle, every public member.
- [Localization](docs/LOCALIZATION.md): `lang/*.json`, key format, following the language.
- [Config editor guide](docs/CONFIG_EDITOR_GUIDE.md): large pages, drafts, performance.
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Changelog](CHANGELOG.md)
- Maintainers: [docs/internal/](docs/internal/MAINTAINER_NOTES.md)

The library ships `SULFUR Native UI Lib.xml` next to the DLL, so IDEs show its
documentation comments. Licensed under the [MIT License](LICENSE).
