# API Reference

## SulfurOptionsApi

### RegisterPage

Registers a custom page in the native Options screen.

```csharp
SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
{
    PageId = "ryuka.example.mod",
    DisplayName = "Example Mod",
    SortOrder = 1000,
    GetDisplayName = () => "Example Mod",
    BuildPage = BuildPage
});
```

### UnregisterPage

Unregisters a page.

```csharp
SulfurOptionsApi.UnregisterPage("ryuka.example.mod");
```

Call this in `OnDestroy()`.

---

## SulfurOptionsPage

```csharp
public sealed class SulfurOptionsPage
{
    public string PageId;
    public string DisplayName;
    public int SortOrder;
    public Func<string> GetDisplayName;
    public Action<SulfurOptionsContext> BuildPage;
}
```

### PageId

Stable unique id.

Recommended format:

```text
author.game.mod
```

Example:

```text
ryuka.sulfur.config
```

### DisplayName

Fallback display name.

### SortOrder

Controls custom page ordering.

### GetDisplayName

Optional dynamic display name provider. Use this for localization.

### BuildPage

Called when the page is built or rebuilt.

---

## SulfurOptionsContext

A page builder context.

### Important properties

```csharp
OptionsScreen OptionsScreen { get; }
RectTransform OptionsContainer { get; }
string PageId { get; }
```

### Rebuild

```csharp
ctx.Rebuild();
```

Rebuilds the whole current custom page.

Do not use this for ordinary value changes. It is expensive.

Use only for:

- filter changed
- filter cleared
- manual refresh
- Apply / Save
- major structure changes

### SetFooter

```csharp
ctx.SetFooter(
    leftText: "Entries: 164 | Pending: 0",
    statusText: "Ready. Changes are not saved until you click Apply.",
    primaryButtonText: "Apply",
    onPrimaryPressed: Apply);
```

Sets footer text and primary button.

### SetFooterStatus

```csharp
ctx.SetFooterStatus("Pending change: EnableMod");
```

Updates only footer status text.

Use this for light updates.

---

## Themed Groups

### BeginThemedGroup

```csharp
using (ctx.BeginThemedGroup("section.content.General"))
{
    ctx.AddBadgeRow("Entries: 3", "Unchanged");
    DrawEntry(ctx, entry1);
    DrawEntry(ctx, entry2);
}
```

Creates a scoped section content group.

All rows created inside the scope are added to the themed group.

Use this to visually show where a section's contents begin and end.

---

## Basic UI methods

### AddSection

```csharp
ctx.AddSection("General");
```

Adds a simple section header.

### AddDescription

```csharp
ctx.AddDescription("Description text.");
```

Adds description text.

### AddWarning

```csharp
ctx.AddWarning("No config entries match the current filters.");
```

Adds warning text.

### AddBadgeRow

```csharp
ctx.AddBadgeRow("Entries: 3", "Pending: 1");
```

Adds a compact row of badges.

### AddSmallButton

```csharp
ctx.AddSmallButton("Refresh", () =>
{
    needsScan = true;
    ctx.Rebuild();
});
```

Adds a small native-style button row. Each call creates its own full-width
row and the button is right-aligned. This is the original behaviour and is
unchanged, so existing mods are unaffected.

### AddButtonRow

```csharp
ctx.AddButtonRow(
    new SulfurButton("Save", () => Save()),
    new SulfurButton("Reset", () => Reset()),
    new SulfurButton("Cancel", () => Close()));
```

Lays several small buttons out left-to-right inside a single row (left
aligned), instead of one stacked row per button. Use this when you want a
compact button group on one line. Each `SulfurButton` is `(label, onPressed)`,
with an optional third `minWidth` argument to pin a fixed button width:

```csharp
new SulfurButton("OK", () => Confirm(), 120f);
```

Empty/whitespace labels are skipped. For a single button, prefer
`AddSmallButton`.

`AddButtonRow` returns `IReadOnlyList<SulfurButtonHandle>` (one per button) so you
can change a button after building — see [Live-update handles](#live-update-handles).

### AddSpacer

```csharp
ctx.AddSpacer(12f);
```

Adds vertical spacing.

---

## Live-update handles

Most rows are built once and are static; to change a page **while it is open**
(without `ctx.Rebuild()`), use the handle-returning builders. They mirror the
existing `SulfurSettingHandle` for setting rows.

### AddTextRow → SulfurTextHandle

```csharp
SulfurTextHandle status = ctx.AddTextRow("● Not connected");
// later, e.g. from an event or timer:
status.SetText("● Connected to 1.2.3.4");
status.SetColor(Color.green);
status.SetVisible(true);
```

A plain, full-opacity text row (unlike `AddDescription`, which is dimmed and
prefixed). Handle: `SetText` / `SetColor` / `SetVisible`. Good for status lines,
error/reason messages, and read-only values that change at runtime.

### AddButtonRow → SulfurButtonHandle

```csharp
var buttons = ctx.AddButtonRow(
    new SulfurButton("Create game", OnCreate),
    new SulfurButton("Join game", OnJoin));

buttons[1].SetInteractable(false);   // disable Join while hosting
buttons[0].SetLabel("Hosting…");
```

Each handle: `SetLabel` / `SetInteractable` / `SetVisible`. A disabled button is
greyed (background + label).

### AddList → SulfurListHandle

```csharp
SulfurListHandle players = ctx.AddList();

void RefreshPlayers()
{
    players.Update(list =>
    {
        list.AddTextRow("Alice — 24 ms");
        list.AddTextRow("Bob — 31 ms");
        list.AddButtonRow(new SulfurButton("Kick Bob", () => Kick("Bob")));
    });
}
```

A refreshable section. `Update(build)` clears the list and re-runs `build` with
the context redirected into the list, so you compose rows with the normal `Add*`
API. Also `Clear()` and `SetVisible(bool)`. Good for live tables (player + ping +
per-row actions) refreshed on a timer or network event.

---

```csharp
ctx.AddSpacer(12f);
```

Adds vertical spacing.

---

## Text and number inputs

### Stacked inputs (label above input)

```csharp
ctx.AddTextInput("Hostname", host, value => host = value);
ctx.AddNumberInput("Port", port, 0f, 65535f, 0, value => port = value);
```

The label is placed on its own row above a full-width input box. This is the
original layout and is unchanged.

### Inline inputs (label and input on the same line)

```csharp
TMP_InputField AddInlineTextInput(
    string label,
    string value,
    Action<string> onChanged,
    float labelWidth = 160f,
    bool password = false);

TMP_InputField AddInlineNumberInput(
    string label,
    float value,
    float min,
    float max,
    int decimals,
    Action<float> onChanged,
    float labelWidth = 160f);
```

Places the label and the input field on a single row, matching the vanilla
Options layout. Pass the same `labelWidth` to several rows so every input box
starts at the same horizontal position. Increase `labelWidth` to give longer
or CJK labels more room.

`password: true` masks the field (TMP `ContentType.Password`).

Both methods return the created `TMP_InputField`, so the caller can read or set
its value later.

```csharp
ctx.AddInlineTextInput("Hostname", host, v => host = v);
ctx.AddInlineTextInput("Port",     port, v => port = v);
ctx.AddInlineTextInput("Slot",     slot, v => slot = v);
ctx.AddInlineTextInput("Password", pass, v => pass = v, password: true);

ctx.AddInlineNumberInput("Timeout", timeout, 0f, 60f, 1, v => timeout = v);
```

---

## Setting row APIs

Old API:

```csharp
ctx.AddSettingToggle(row, value, onChanged);
ctx.AddSettingText(row, value, onChanged);
ctx.AddSettingNumber(row, value, min, max, decimals, onChanged);
ctx.AddSettingCycle(row, values, currentIndex, onChanged);
```

These preserve compatibility but do not return a row handle.

Recommended high-performance API:

```csharp
ctx.AddSettingToggleEx(row, value, onChanged, out handle);
ctx.AddSettingTextEx(row, value, onChanged, out handle);
ctx.AddSettingNumberEx(row, value, min, max, decimals, onChanged, out handle);
ctx.AddSettingCycleEx(row, values, currentIndex, onChanged, out handle);
```

Use the `Ex` methods for large pages and config editors.

---

## SulfurSettingRow

```csharp
public sealed class SulfurSettingRow
{
    public string Label;
    public string Description;

    public int IndentLevel;
    public float IndentPixels;

    public bool IsDirty;
    public bool ShowCleanBadge;

    public bool RequiresRestart;
    public bool LiveApply;
    public bool Advanced;
    public bool Hidden;
    public bool Dangerous;

    public string DirtyText;
    public string CleanText;
    public string RestartRequiredText;
    public string LiveApplyText;
    public string AdvancedText;
    public string HiddenText;
    public string DangerousText;

    public string[] ExtraBadges;

    public string Message;
    public SulfurMessageKind MessageKind;

    public bool ShowDefaultButton;
    public string DefaultButtonText;
    public Action OnDefault;
}
```

Recommended defaults for config editors:

```csharp
SulfurSettingRow row = new SulfurSettingRow
{
    Label = entry.DisplayName,
    Description = entry.Description,
    IndentLevel = 1,
    IsDirty = entry.IsDirty,

    RequiresRestart = entry.RequiresRestart,
    LiveApply = entry.LiveApply,
    Advanced = entry.Advanced,
    Hidden = entry.Hidden,
    Dangerous = entry.Dangerous,

    DirtyText = "Pending",
    CleanText = "Unchanged",
    RestartRequiredText = "Restart Required",
    LiveApplyText = "Live Apply",
    AdvancedText = "Advanced",
    HiddenText = "Hidden",
    DangerousText = "Dangerous",

    Message = entry.IsDirty ? pendingMessage : entry.RangeText,
    MessageKind = entry.IsDirty ? SulfurMessageKind.Warning : SulfurMessageKind.Info,

    DefaultButtonText = "Default",
    OnDefault = () => { ... }
};
```

---

## SulfurSettingHandle

Runtime handle for updating one setting row without rebuilding the whole page.

### SetDirty

```csharp
handle.SetDirty(true, "Pending", "Unchanged");
```

or:

```csharp
handle.SetDirty(entry.IsDirty);
```

### SetMessage

```csharp
handle.SetMessage(
    "This setting has unsaved pending changes. Click Apply to write it to cfg.",
    SulfurMessageKind.Warning);
```

### SetActive

```csharp
handle.SetActive(false);
```

---

## SulfurMessageKind

```csharp
public enum SulfurMessageKind
{
    Info,
    Warning,
    Error,
    Success
}
```

Use:

```csharp
SulfurMessageKind.Info
SulfurMessageKind.Warning
SulfurMessageKind.Error
SulfurMessageKind.Success
```

---

## Localization API

### LoadPluginLocalization

```csharp
SulfurLocalization.LoadPluginLocalization(pluginGuid, pluginInfo.Location);
```

Loads `lang/*.json` beside the target plugin dll.

### Get

```csharp
string label = SulfurLocalization.Get(
    pluginGuid,
    "entry.General.EnableMod.name",
    "EnableMod");
```

Fallback is used when no translation exists.
