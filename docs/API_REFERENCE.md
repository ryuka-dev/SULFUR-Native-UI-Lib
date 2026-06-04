# API Reference

This document describes the public-facing API expected from SULFUR Native UI Lib v0.6.1.

## SulfurOptionsApi

### RegisterPage

Registers a custom native Options page.

```csharp
SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
{
    PageId = "my_page",
    DisplayName = "My Page",
    GetDisplayName = () => "My Page",
    SortOrder = 100,
    BuildPage = ctx =>
    {
        ctx.AddSection("General");
    }
});
```

### Page ID rules

Use stable, lowercase IDs:

```text
mod_guid_or_short_name.section_name
```

Examples:

```text
ryuka.sulfur.config
deadeye_instinct
weapon_xp_multiplier
```

The `PageId` is used by the bridge and page-state store. Do not change it between versions unless you intentionally want to reset saved foldout state.

## SulfurOptionsPage

Common fields:

```csharp
public string PageId;
public string DisplayName;
public Func<string> GetDisplayName;
public int SortOrder;
public Action<SulfurOptionsContext> BuildPage;
```

### DisplayName vs GetDisplayName

Use `DisplayName` as a fallback.

Use `GetDisplayName` when localization is needed:

```csharp
GetDisplayName = () => L("page.name", "My Mod")
```

## SulfurOptionsContext

`SulfurOptionsContext` is passed to `BuildPage`.

It contains page-building methods and helpers.

### Basic information

```csharp
ctx.PageId
ctx.OptionsScreen
ctx.OptionsContainer
```

### Rebuild

```csharp
ctx.Rebuild();
```

Rebuilds the current custom page.

The library preserves the current scroll position where possible.

Avoid calling `Rebuild()` on every text input character. Prefer draft values and a separate `Apply Filter` button.

## Basic controls

### Button

```csharp
ctx.AddButton("Refresh", () =>
{
    Refresh();
});
```

With description:

```csharp
ctx.AddButton(
    "Refresh",
    "Reload current data.",
    () => Refresh());
```

### Info

```csharp
ctx.AddInfo("Read-only text");
```

### Toggle

```csharp
ctx.AddToggle(
    "Enable Feature",
    "Enable or disable this feature.",
    enabled,
    value => enabled = value);
```

### Cycle

```csharp
ctx.AddCycle(
    "Mode",
    "Select operation mode.",
    new[] { "Low", "Normal", "High" },
    currentIndex,
    (index, value) =>
    {
        currentIndex = index;
    });
```

### Slider

```csharp
ctx.AddSlider(
    "Multiplier",
    "Adjust multiplier.",
    value,
    0f,
    10f,
    0.5f,
    newValue => value = newValue);
```

## Text and number input

### TextInput

```csharp
ctx.AddTextInput(
    "Search",
    "Type filter text.",
    searchDraft,
    value =>
    {
        searchDraft = value;
        ctx.SetFooterStatus("Filter draft changed.");
    });
```

Do not rebuild the page on every `onChanged` callback unless the field is not actively used for continuous typing.

### NumberInput

```csharp
ctx.AddNumberInput(
    "Spawn Multiplier",
    "Range: 0.1 - 10.",
    spawnMultiplier,
    0.1f,
    10f,
    2,
    value =>
    {
        spawnMultiplier = value;
        ctx.SetFooterStatus("Value changed.");
    });
```

Number input clamps the result on end edit.

## Layout helpers

### Section

```csharp
ctx.AddSection("General");
```

### Description

```csharp
ctx.AddDescription("This setting affects enemy spawning.");
```

### Spacer

```csharp
ctx.AddSpacer(18f);
```

### Read-only text

```csharp
ctx.AddReadonlyText("Status", "Ready");
```

## Footer

The footer is only intended for custom pages. It should be hidden automatically when the user switches to native game categories.

### SetFooter

```csharp
ctx.SetFooter(
    leftText: "Dirty: 3 | Files: 2",
    statusText: "Ready.",
    primaryButtonText: "Apply",
    onPrimaryPressed: ApplyAllDirty);
```

### SetFooterStatus

```csharp
ctx.SetFooterStatus("Applied.");
```

### ClearFooter

```csharp
ctx.ClearFooter();
```

Use the footer for global actions such as:

- Apply all dirty changes
- Save
- Refresh
- Run selected operation

For configuration pages, prefer a single footer `Apply` button instead of per-setting Apply buttons.

## Messages

### AddMessage

```csharp
ctx.AddMessage("Information.", SulfurMessageKind.Info);
```

### AddWarning

```csharp
ctx.AddWarning("This setting requires restart.");
```

### AddError

```csharp
ctx.AddError("Invalid number.");
```

### AddSuccess

```csharp
ctx.AddSuccess("Saved successfully.");
```

## Badges

### AddBadgeRow

```csharp
ctx.AddBadgeRow("Dirty", "Restart Required", "Advanced");
```

Badge rows are purely visual.

The library does not know what `Dirty`, `Restart Required`, or `Advanced` mean.

## Small buttons

### AddSmallButton

```csharp
ctx.AddSmallButton("Default", () =>
{
    ResetToDefault();
    ctx.Rebuild();
});
```

Optional width overload:

```csharp
ctx.AddSmallButton("Apply Filter", ApplyFilter, 120f);
```

### AddDefaultButton

```csharp
ctx.AddDefaultButton(() =>
{
    ResetToDefault();
});
```

For configuration pages, use a per-setting `Default` button and a global footer `Apply`.

## Setting row helpers

Added in v0.5.

Setting rows combine a control with common metadata:

- label
- description
- dirty/clean badge
- live apply / restart required badge
- advanced / hidden / dangerous badge
- default button
- optional message
- indentation

### SulfurSettingRow

```csharp
new SulfurSettingRow
{
    Label = "SpawnMultiplier",
    Description = "Enemy spawn multiplier.",
    IndentLevel = 1,
    IsDirty = true,
    RequiresRestart = true,
    Advanced = true,
    DefaultButtonText = "Default",
    OnDefault = () => ResetToDefault()
}
```

Important fields:

```csharp
public string Label;
public string Description;

public bool IsDirty;
public bool RequiresRestart;
public bool LiveApply;
public bool Advanced;
public bool Hidden;
public bool Dangerous;

public string[] ExtraBadges;

public bool ShowCleanBadge = true;
public bool ShowDefaultButton = true;
public string DefaultButtonText = "Default";
public Action OnDefault;

public int IndentLevel = 0;
public float IndentPixels = 28f;

public string Message;
public SulfurMessageKind MessageKind = SulfurMessageKind.Info;
```

### AddSettingToggle

```csharp
ctx.AddSettingToggle(
    new SulfurSettingRow
    {
        Label = "EnableAutoSpawn",
        Description = "Enable automatic dynamic pressure spawning.",
        IndentLevel = 1,
        IsDirty = isDirty,
        LiveApply = true,
        OnDefault = () => ResetAutoSpawn()
    },
    enableAutoSpawn,
    value =>
    {
        enableAutoSpawn = value;
        ctx.Rebuild();
    });
```

### AddSettingText

```csharp
ctx.AddSettingText(
    new SulfurSettingRow
    {
        Label = "Search",
        Description = "Filter entries.",
        ShowDefaultButton = false
    },
    searchDraft,
    value =>
    {
        searchDraft = value;
    });
```

### AddSettingNumber

```csharp
ctx.AddSettingNumber(
    new SulfurSettingRow
    {
        Label = "Multiplier",
        Description = "Range: 0 - 100.",
        IndentLevel = 1,
        IsDirty = isDirty,
        ExtraBadges = new[] { "Range: 0 - 100" },
        OnDefault = () => multiplier = defaultMultiplier
    },
    multiplier,
    0f,
    100f,
    2,
    value =>
    {
        multiplier = value;
        ctx.Rebuild();
    });
```

### AddSettingCycle

```csharp
ctx.AddSettingCycle(
    new SulfurSettingRow
    {
        Label = "Mode",
        Description = "Select mode.",
        IndentLevel = 1,
        IsDirty = isDirty
    },
    new[] { "Low", "Normal", "High" },
    index,
    (newIndex, value) =>
    {
        index = newIndex;
        ctx.Rebuild();
    });
```

### AddSettingEnum

```csharp
ctx.AddSettingEnum<MyEnum>(
    row,
    currentValue,
    value =>
    {
        currentValue = value;
    });
```

## Foldouts

Added in v0.6.

Foldout state is stored in the page-state store and survives page rebuilds.

### AddFoldout

```csharp
bool expanded = ctx.AddFoldout(
    key: "section.general",
    label: "General",
    defaultExpanded: true);

if (expanded)
{
    // render section contents
}
```

### Force expanded

Useful when search results match inside a collapsed group:

```csharp
bool expanded = ctx.AddFoldout(
    "plugin.weapon_xp",
    "Weapon XP Multiplier",
    false,
    forceExpanded: hasActiveSearch);
```

### Plugin foldout

```csharp
bool expanded = ctx.AddPluginFoldout(
    "plugin.deadeye",
    "Deadeye Instinct",
    true,
    forceExpanded: hasActiveSearch);
```

### Section foldout

```csharp
bool expanded = ctx.AddSectionFoldout(
    "plugin.deadeye.section.general",
    "General",
    true,
    forceExpanded: hasActiveSearch);
```

### Foldout with badges

```csharp
bool expanded = ctx.AddFoldoutWithBadges(
    "plugin.weapon_xp",
    "Weapon XP Multiplier",
    false,
    hasActiveSearch,
    "Plugin",
    "Entries: 2",
    "Clean");
```

## Page state

Added in v0.6.

Use page state for UI-only state:

- foldout expanded/collapsed state
- active filters
- temporary mode toggles
- selected subtab

Do not use it for actual mod configuration values.

### GetState / SetState

```csharp
string search = ctx.GetState("search.text", "");
ctx.SetState("search.text", search);
```

### HasState

```csharp
if (ctx.HasState("foldout.general"))
{
    // state exists
}
```

### RemoveState

```csharp
ctx.RemoveState("search.text");
```

### ClearPageState

```csharp
ctx.ClearPageState();
```

### ToggleBoolState

```csharp
bool expanded = ctx.ToggleBoolState("foldout.general", true);
```

## Localization

### LoadPluginLocalization

```csharp
SulfurLocalization.LoadPluginLocalization(
    PluginGuid,
    typeof(Plugin).Assembly.Location);
```

### Get

```csharp
string text = SulfurLocalization.Get(
    PluginGuid,
    "setting.enable.name",
    "Enable Feature");
```

Recommended wrapper:

```csharp
private static string L(string key, string fallback)
{
    return SulfurLocalization.Get(PluginGuid, key, fallback);
}
```

## Recommended config editor pattern

```csharp
ctx.SetFooter(
    "Dirty: " + dirtyCount + " | Files: " + fileCount,
    statusText,
    "Apply",
    () =>
    {
        ApplyAllDirty();
        ctx.Rebuild();
    });

ctx.AddSection("SULFUR Config");

ctx.AddTextInput(
    "Filter",
    "Type text, then click Apply Filter.",
    filterDraft,
    value => filterDraft = value);

ctx.AddSmallButton("Apply Filter", () =>
{
    activeFilter = filterDraft;
    ctx.Rebuild();
});

bool pluginExpanded = ctx.AddPluginFoldout(
    "plugin.example",
    "Example Mod",
    true,
    hasFilter);

if (pluginExpanded)
{
    bool sectionExpanded = ctx.AddSectionFoldout(
        "plugin.example.general",
        "General",
        true,
        hasFilter);

    if (sectionExpanded)
    {
        ctx.AddSettingToggle(...);
        ctx.AddSettingNumber(...);
    }
}
```
