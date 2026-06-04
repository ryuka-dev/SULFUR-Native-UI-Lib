# Performance Guide

Large Options pages can become slow if the page is rebuilt too often.

SULFUR Config-type pages can easily contain 100+ settings. The native Options screen uses Unity UI layout systems such as `VerticalLayoutGroup`, `ContentSizeFitter`, and `ScrollRect`. Destroying and recreating hundreds of rows is expensive.

---

## Main rule

Do not call `ctx.Rebuild()` for normal value changes.

Bad:

```csharp
ctx.AddSettingNumber(row, value, min, max, decimals, v =>
{
    entry.SetDraft(v.ToString());
    ctx.Rebuild();
});
```

Good:

```csharp
SulfurSettingHandle handle;

ctx.AddSettingNumberEx(
    row,
    value,
    min,
    max,
    decimals,
    (v, h) =>
    {
        entry.SetDraft(v.ToString(CultureInfo.InvariantCulture));
        SaveDraft(entry);

        h.SetDirty(entry.IsDirty, "Pending", "Unchanged");
        h.SetMessage(
            entry.IsDirty ? pendingMessage : entry.RangeText,
            entry.IsDirty ? SulfurMessageKind.Warning : SulfurMessageKind.Info);

        ctx.SetFooterStatus("Pending change: " + entry.Key);
    },
    out handle);
```

---

## Allowed Rebuild cases

Use `ctx.Rebuild()` only when the whole page structure truly changes.

Allowed:

- applying/saving changes
- manual refresh
- applying filter
- clearing filter
- changing filter flags
- changing foldout state when the library requires it
- switching page mode

Avoid:

- toggle changed
- number changed
- cycle changed
- text changed
- default draft changed

---

## Use row handles

For config editors, always use:

```csharp
AddSettingToggleEx
AddSettingTextEx
AddSettingNumberEx
AddSettingCycleEx
```

These return `SulfurSettingHandle`.

Handles can update a single row:

```csharp
handle.SetDirty(entry.IsDirty);
handle.SetMessage(message, kind);
```

This avoids full page rebuilds.

---

## Collapse by default

Large config pages should not expand everything by default.

Recommended:

```csharp
bool forceExpanded = HasFilterOrSpecialFilter();

bool pluginExpanded = ctx.AddPluginFoldoutWithBadges(
    "plugin." + plugin.Guid,
    plugin.DisplayName,
    false,
    forceExpanded,
    "Entries: " + count,
    pending > 0 ? "Pending: " + pending : "Unchanged");
```

When filtering, `forceExpanded` can be `true` so users immediately see search results.

---

## Cache scanned data

A config editor should scan BepInEx configs once and cache the result.

Recommended fields:

```csharp
private List<ConfigPluginGroup> groups = new List<ConfigPluginGroup>();
private readonly Dictionary<string, string> draftCache = new Dictionary<string, string>();
private bool needsScan = true;
```

Recommended scan flow:

```csharp
private void EnsureScanned()
{
    if (!needsScan && groups != null && groups.Count > 0)
        return;

    groups = ConfigScanner.Scan(logger);
    RestoreDrafts(groups);
    needsScan = false;
}
```

Refresh button:

```csharp
ctx.AddSmallButton("Refresh", () =>
{
    needsScan = true;
    ctx.Rebuild();
});
```

---

## Cache expensive UI lookups

`SulfurOptionsContext` should cache:

```csharp
private TextMeshProUGUI sampleTextCache;
private bool sampleTextCacheInitialized;
private float nativeOptionWidthCache = -1f;
```

Avoid repeated:

```csharp
GetComponentsInChildren<TextMeshProUGUI>(true)
```

inside every row creation.

---

## Avoid too many child rows

A setting may internally create:

- main option row
- description row
- badge row
- message row
- default button row

For many settings this becomes hundreds of Unity UI objects. Keep descriptions and messages concise, and avoid unnecessary message rows unless useful.

---

## Footer updates

Use:

```csharp
ctx.SetFooterStatus("Pending change: " + key);
```

for lightweight feedback.

Use full:

```csharp
ctx.SetFooter(...)
```

only when the footer count/button needs to change.
