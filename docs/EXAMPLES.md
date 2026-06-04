# Examples

## 1. Minimal page

```csharp
SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
{
    PageId = "example_minimal",
    DisplayName = "Example",
    SortOrder = 100,
    BuildPage = ctx =>
    {
        ctx.AddSection("General");

        ctx.AddButton("Click Me", () =>
        {
            ctx.SetFooterStatus("Clicked.");
        });

        ctx.SetFooter("Example", "Ready.", "Apply", () =>
        {
            ctx.SetFooterStatus("Applied.");
        });
    }
});
```

## 2. Localized page

```csharp
private const string PluginGuid = "example.localized";

private void Awake()
{
    SulfurLocalization.LoadPluginLocalization(
        PluginGuid,
        typeof(Plugin).Assembly.Location);

    SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
    {
        PageId = "example_localized",
        DisplayName = "Example",
        GetDisplayName = () => L("page.name", "Example"),
        BuildPage = BuildPage
    });
}

private void BuildPage(SulfurOptionsContext ctx)
{
    ctx.AddSection(L("section.general", "General"));

    ctx.AddToggle(
        L("setting.enabled", "Enabled"),
        L("setting.enabled.desc", "Enable feature."),
        enabled,
        value => enabled = value);
}

private static string L(string key, string fallback)
{
    return SulfurLocalization.Get(PluginGuid, key, fallback);
}
```

## 3. Config editor style page

This pattern is intended for future SULFUR Config integration.

```csharp
private bool enableAutoSpawn = true;
private bool appliedEnableAutoSpawn = true;
private bool defaultEnableAutoSpawn = true;

private float spawnMultiplier = 1.5f;
private float appliedSpawnMultiplier = 1.5f;
private float defaultSpawnMultiplier = 1.5f;

private string status = "Ready.";

private void BuildPage(SulfurOptionsContext ctx)
{
    int dirty = CountDirty();

    ctx.SetFooter(
        "Dirty: " + dirty + " | Files: 1",
        status,
        "Apply",
        () =>
        {
            appliedEnableAutoSpawn = enableAutoSpawn;
            appliedSpawnMultiplier = spawnMultiplier;

            status = "Applied " + dirty + " setting(s).";
            ctx.Rebuild();
        });

    ctx.AddSection("Config Editor Preview");

    bool pluginExpanded = ctx.AddPluginFoldout(
        "plugin.dynamic_pressure",
        "Dynamic Pressure",
        true,
        false);

    if (!pluginExpanded)
        return;

    bool generalExpanded = ctx.AddSectionFoldout(
        "plugin.dynamic_pressure.general",
        "General",
        true,
        false);

    if (!generalExpanded)
        return;

    ctx.AddSettingToggle(
        new SulfurSettingRow
        {
            Label = "EnableAutoSpawn",
            Description = "Enable automatic dynamic pressure spawning.",
            IndentLevel = 1,
            IsDirty = enableAutoSpawn != appliedEnableAutoSpawn,
            LiveApply = true,
            OnDefault = () =>
            {
                enableAutoSpawn = defaultEnableAutoSpawn;
                ctx.Rebuild();
            }
        },
        enableAutoSpawn,
        value =>
        {
            enableAutoSpawn = value;
            status = "EnableAutoSpawn changed.";
            ctx.Rebuild();
        });

    ctx.AddSettingNumber(
        new SulfurSettingRow
        {
            Label = "SpawnMultiplier",
            Description = "Enemy spawn multiplier.",
            IndentLevel = 1,
            IsDirty = Math.Abs(spawnMultiplier - appliedSpawnMultiplier) > 0.0001f,
            RequiresRestart = true,
            ExtraBadges = new[] { "Range: 0.1 - 10" },
            OnDefault = () =>
            {
                spawnMultiplier = defaultSpawnMultiplier;
                ctx.Rebuild();
            }
        },
        spawnMultiplier,
        0.1f,
        10f,
        2,
        value =>
        {
            spawnMultiplier = value;
            status = "SpawnMultiplier changed.";
            ctx.Rebuild();
        });
}

private int CountDirty()
{
    int count = 0;

    if (enableAutoSpawn != appliedEnableAutoSpawn)
        count++;

    if (Math.Abs(spawnMultiplier - appliedSpawnMultiplier) > 0.0001f)
        count++;

    return count;
}
```

## 4. Search/filter pattern

Avoid rebuilding on every text input character.

Use a draft value and an explicit `Apply Filter` button.

```csharp
private string filterDraft = "";
private string activeFilter = "";

private void BuildPage(SulfurOptionsContext ctx)
{
    bool hasFilter = !string.IsNullOrWhiteSpace(activeFilter);

    ctx.AddSettingText(
        new SulfurSettingRow
        {
            Label = "Filter Draft",
            Description = "Type filter text, then click Apply Filter.",
            IsDirty = filterDraft != activeFilter,
            ShowDefaultButton = false
        },
        filterDraft,
        value =>
        {
            filterDraft = value;
            ctx.SetFooterStatus("Filter draft changed.");
        });

    ctx.AddSmallButton("Apply Filter", () =>
    {
        activeFilter = filterDraft;
        ctx.Rebuild();
    });

    ctx.AddSmallButton("Clear Filter", () =>
    {
        filterDraft = "";
        activeFilter = "";
        ctx.Rebuild();
    });

    if (ShouldShow("EnableAutoSpawn", activeFilter))
    {
        // render setting
    }
}
```

## 5. Multi-mod layout pattern

```csharp
foreach (PluginGroup plugin in plugins)
{
    bool pluginExpanded = ctx.AddPluginFoldoutWithBadges(
        "plugin." + plugin.Guid,
        plugin.Name,
        true,
        hasFilter,
        "Plugin",
        "Entries: " + plugin.EntryCount,
        plugin.DirtyCount > 0 ? "Dirty: " + plugin.DirtyCount : "Clean");

    if (!pluginExpanded)
        continue;

    foreach (SectionGroup section in plugin.Sections)
    {
        bool sectionExpanded = ctx.AddSectionFoldout(
            "plugin." + plugin.Guid + ".section." + section.Name,
            section.Name,
            true,
            hasFilter);

        if (!sectionExpanded)
            continue;

        foreach (ConfigEntryModel entry in section.Entries)
        {
            RenderEntry(ctx, entry);
        }
    }
}
```

## 6. Localized `lang/en.json`

```json
{
  "entries": [
    {
      "key": "page.name",
      "value": "Example Settings"
    },
    {
      "key": "section.general",
      "value": "General"
    },
    {
      "key": "setting.enabled.name",
      "value": "Enable Feature"
    },
    {
      "key": "setting.enabled.description",
      "value": "Enable this feature."
    }
  ]
}
```
