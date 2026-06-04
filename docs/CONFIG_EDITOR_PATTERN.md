# Config Editor Pattern

This document describes the recommended pattern for a config editor built on SULFUR Native UI Lib.

---

## Goals

A config editor should:

- scan loaded BepInEx plugins
- read each plugin's config entries
- display entries in a readable hierarchy
- support localization from each target mod's own `lang/*.json`
- keep edits as pending drafts
- save only when the user clicks Apply
- avoid page rebuilds for ordinary value edits

---

## Hierarchy

Recommended hierarchy:

```text
Plugin
  Section
    Setting
```

Visual hierarchy:

```text
Plugin foldout       largest
Section foldout      medium
Setting row          smallest
```

---

## Config scanning

Use `Chainloader.PluginInfos` to find loaded plugins.

```csharp
foreach (PluginInfo pluginInfo in Chainloader.PluginInfos.Values)
{
    ConfigFile config = pluginInfo.Instance.Config;
    ...
}
```

Each `ConfigEntryBase` has:

- `ConfigFile`
- `Definition`
- `SettingType`
- `Description`
- `DefaultValue`
- `GetSerializedValue()`
- `SetSerializedValue(...)`

---

## Draft values

Do not apply immediately.

Recommended model:

```csharp
public string AppliedValue;
public string DraftValue;

public bool IsDirty
{
    get { return AppliedValue != DraftValue; }
}
```

---

## Draft cache

Store unsaved edits across rebuilds:

```csharp
private readonly Dictionary<string, string> draftCache =
    new Dictionary<string, string>();
```

Key:

```csharp
private static string GetDraftKey(ConfigEntryModel entry)
{
    return entry.PluginGuid + "\u001f" + entry.Section + "\u001f" + entry.Key;
}
```

Save draft:

```csharp
private void SaveDraft(ConfigEntryModel entry)
{
    string key = GetDraftKey(entry);

    if (entry.IsDirty)
        draftCache[key] = entry.DraftValue;
    else
        draftCache.Remove(key);
}
```

Restore drafts after scan:

```csharp
private void RestoreDrafts(List<ConfigPluginGroup> source)
{
    foreach (ConfigPluginGroup plugin in source)
    foreach (ConfigSectionGroup section in plugin.Sections)
    foreach (ConfigEntryModel entry in section.Entries)
    {
        string cached;
        if (draftCache.TryGetValue(GetDraftKey(entry), out cached))
            entry.SetDraft(cached);
    }
}
```

---

## Apply behavior

Only save when the user clicks Apply.

```csharp
entry.Apply();
ClearDraft(entry);
```

Apply implementation:

```csharp
public void Apply()
{
    Entry.SetSerializedValue(DraftValue);
    Entry.ConfigFile.Save();

    AppliedValue = Entry.GetSerializedValue();
    DraftValue = AppliedValue;
}
```

---

## Footer wording

English:

```text
Ready. Changes are not saved until you click Apply.
```

Chinese:

```text
就绪。修改配置后需要点击“应用”才会保存。
```

Pending warning:

```text
This setting has unsaved pending changes. Click Apply to write it to cfg.
```

Chinese:

```text
此项有未保存的待应用更改。点击“应用”后才会写入 cfg。
```

---

## Recommended callback

```csharp
SulfurSettingHandle handle;

ctx.AddSettingToggleEx(
    row,
    entry.GetBoolDraft(),
    (value, h) =>
    {
        entry.SetDraft(value ? "true" : "false");
        SaveDraft(entry);

        status = "Pending change: " + entry.Key;

        h.SetDirty(entry.IsDirty, "Pending", "Unchanged");
        h.SetMessage(
            entry.IsDirty ? pendingWarning : entry.RangeText,
            entry.IsDirty ? SulfurMessageKind.Warning : SulfurMessageKind.Info);

        ctx.SetFooterStatus(status);
    },
    out handle);
```

---

## Recommended Apply button

```csharp
ctx.SetFooter(
    "Entries: " + totalEntries + " | Pending: " + pendingEntries,
    "Ready. Changes are not saved until you click Apply.",
    "Apply",
    () => ApplyDirty(ctx));
```

---

## Filtering

Changing filters is allowed to rebuild the page:

```csharp
activeFilter = filterDraft;
ctx.Rebuild();
```

Filtering changes the visible structure, so a full rebuild is acceptable.

---

## Refresh

Manual refresh should rescan plugin configs:

```csharp
ctx.AddSmallButton("Refresh", () =>
{
    needsScan = true;
    ctx.Rebuild();
});
```
