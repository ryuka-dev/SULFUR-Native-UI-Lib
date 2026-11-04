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

`PageId` is global across mods and compared case-insensitively. Registering
another page with an id that is already registered replaces that page and logs
a warning naming both.

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
bool IsAlive { get; }
```

### IsAlive

```csharp
if (ctx.IsAlive)
    ctx.SetFooterStatus("Saved.");
```

True while this context's page is the page on screen. Every build of a page
(opening it, `ctx.Rebuild()`, a foldout click) gets a new context, and the old
one stops being alive for good when the page is rebuilt, another category is
shown, or the options screen is hidden.

On a context that is no longer alive:

- `Rebuild`, `SetFooter`, `SetFooterStatus` and `ClearFooter` do nothing.
- `Add*` / `BeginThemedGroup` / foldouts add nothing and log one warning per
  context. Methods that return a row return `null`; `AddTextRow` and `AddList`
  return an inert handle.
- `SulfurListHandle.Update` / `Clear` of a list built by it do nothing.

Keep using the context passed to the current `BuildPage` call. A callback that
can run after its page went away (timer, network event, async result) should
check `IsAlive` first, or go through a handle.

### Rebuild

```csharp
ctx.Rebuild();
```

Rebuilds the whole current custom page. Does nothing when the context is no
longer alive or the options screen is not shown. The scroll position and the
input mode are kept: a keyboard/gamepad user keeps the selected row, a mouse
user gets no row selection. Button rows (`AddButtonRow`) keep the button that
had keyboard/gamepad focus inside the row (see
[Keyboard and gamepad navigation](#keyboard-and-gamepad-navigation)).

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

Adds description text. Returns `null` (the return type is `OptionsScreenOption`
for binary compatibility; the row is not a native option row). The same holds
for `AddReadonlyText`.

A description that wraps (or contains line breaks) makes its row taller by the
extra lines, keeping the single-line padding, so it never runs into the row
below; a single-line description keeps the row's usual height. The same applies
to message lines (`AddMessage` / `AddWarning` / `AddError` / `AddSuccess`, a
setting row's message line, also after `SulfurSettingHandle.SetMessage`) and to
`AddTextRow` / `SulfurTextHandle.SetText`. Message and text rows do not wrap
outside indented setting rows (long text is cut with "…"), so there only explicit
line breaks add lines.

### AddWarning

```csharp
ctx.AddWarning("No config entries match the current filters.");
```

Adds warning text.

### AddBadgeRow

```csharp
ctx.AddBadgeRow("Entries: 3", "Pending: 1");
```

Adds a compact row of badges. To change the badges later without a rebuild, use
`AddBadgeRowEx` (see [AddBadgeRowEx → SulfurBadgeRowHandle](#addbadgerowex--sulfurbadgerowhandle)).

Each badge is at least 100px wide and grows to fit its text, measured with its real
font, so translated and CJK text is not cut off; text beyond 240px is ellipsised.
When the badges don't fit on one line the row wraps onto further lines and grows;
the next row moves down. Width and wrapping follow text changes automatically
(`SetBadge`, `SulfurSettingHandle.SetDirty`, a language change).

### AddSmallButton

```csharp
ctx.AddSmallButton("Refresh", () =>
{
    needsScan = true;
    ctx.Rebuild();
});
```

Adds a small native-style button row. Each call creates its own full-width
row and the button is right-aligned.

The button sizes itself to its label (auto-size range 96–180px). For a wider
button — e.g. a long label that would otherwise be clipped — pass an explicit
width via the third argument:

```csharp
ctx.AddSmallButton("Open-source repo", () => OpenUrl(repo), 260f);
```

(Before 0.10.1 this width was ignored and every small button rendered at a
fixed 120px, ellipsising labels longer than ~10 characters.)

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

### AddBadgeRowEx → SulfurBadgeRowHandle

```csharp
SulfurBadgeRowHandle AddBadgeRowEx(params string[] badges);
SulfurBadgeRowHandle AddBadgeRowEx(IReadOnlyList<string> badges);

// SulfurOptionsFoldoutExtensions: the *WithBadges foldouts, handing out their badge row
bool AddFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
bool AddPluginFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
bool AddSectionFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
```

The same badge row as `AddBadgeRow` / the existing `*FoldoutWithBadges` calls,
returned as a handle:

| Member | |
|---|---|
| `IsAlive` | row exists and its page is on screen |
| `BadgeCount` | badges shown now |
| `SetBadges(params string[])`, `SetBadges(IReadOnlyList<string>)` | replaces all badges; `false` when the row is gone |
| `SetBadge(int index, string text)` | changes one badge's text; `false` when gone, out of range or `text` empty |
| `SetVisible(bool)` | shows / hides the row |

Badges look as `AddBadgeRow` draws them: the text as given (trimmed, empty
entries skipped; nothing is translated), all badges in the same colours. They
are not navigation stops, and the row keeps its built size and place. A row
without badges is hidden (it takes no space): with no badges at build time the
row is still created, hidden, so `SetBadges` can show it later; `SetBadges()`
with nothing hides it again.

The foldout overloads take the badges as a list or array before the `out`
parameter (`params` must be last); existing calls keep binding to the original
`params string[]` overloads, which still return only the expanded state.

Example: a config editor shows "Pending: N" on each plugin header and updates it
after an edit, without rebuilding the page:

```csharp
readonly Dictionary<string, SulfurBadgeRowHandle> pluginBadges = new Dictionary<string, SulfurBadgeRowHandle>();

void DrawPlugin(SulfurOptionsContext ctx, PluginView plugin)
{
    SulfurBadgeRowHandle badges;
    bool expanded = ctx.AddPluginFoldoutWithBadges(
        "plugin." + plugin.Guid,
        plugin.DisplayName,
        false,
        false,
        new[] { "Entries: " + plugin.EntryCount, PendingBadge(plugin.PendingCount) },
        out badges);

    pluginBadges[plugin.Guid] = badges;

    if (expanded)
        DrawEntries(ctx, plugin);
}

// Called from an entry row's onChanged:
void OnDraftChanged(PluginView plugin)
{
    SulfurBadgeRowHandle badges;
    if (pluginBadges.TryGetValue(plugin.Guid, out badges) && badges.IsAlive)
        badges.SetBadge(1, PendingBadge(plugin.PendingCount));   // or SetBadges(...) for all
}

static string PendingBadge(int count)
{
    return count > 0 ? "Pending: " + count : "Unchanged";
}
```

Clear the dictionary at the start of each `BuildPage`: handles from an earlier
build are not alive.

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

### AddToggleRow / AddSliderRow / AddCycleRow → native row handles

```csharp
SulfurToggleHandle AddToggleRow(string label, bool value, Action<bool> onChanged);
SulfurToggleHandle AddToggleRow(string label, string description, bool value, Action<bool> onChanged);

SulfurSliderHandle AddSliderRow(string label, float value, float min, float max, float step, Action<float> onChanged);
SulfurSliderHandle AddSliderRow(string label, string description, float value, float min, float max, float step, Action<float> onChanged);

SulfurCycleHandle AddCycleRow(string label, IReadOnlyList<string> values, int currentIndex, Action<int, string> onChanged);
SulfurCycleHandle AddCycleRow(string label, string description, IReadOnlyList<string> values, int currentIndex, Action<int, string> onChanged);
```

The same native rows as `AddToggle` / `AddSlider` / `AddCycle` (those keep
returning `OptionsScreenOption`), returned as a handle so the page never has to
reach into the game's private fields:

| Member | Toggle | Slider | Cycle |
|---|---|---|---|
| `IsAlive` | row exists and its page is on screen | same | same |
| value | `bool Value` | `float Value` | `int Index`, `string Value` |
| set without `onChanged` | `SetValueWithoutNotify(bool)` | `SetValueWithoutNotify(float)` (clamped, not snapped) | `SetIndexWithoutNotify(int)` (clamped) |
| `SetLocked(bool)`, `IsLocked` | yes | yes | yes |
| `SetVisible(bool)` (row + its description) | yes | yes | yes |
| `SetLabel(string)` | yes | yes | yes |

`SetLocked(true)` dims the whole row uniformly (CanvasGroup alpha × 0.45),
blocks mouse input, and turns keyboard/gamepad Use (toggle) and left/right
(slider, cycle) into no-ops; a value change that still reaches a locked row is
undone without calling `onChanged`. It never touches the label colour (the
game's own `OptionsScreenOption.SetLocked` recolours the label with colours
that are transparent on library-built rows, so the label disappears).
`SetValueWithoutNotify` keeps working while locked.

`AddSliderRow` also snaps **mouse drags** to `step` (when `step > 0`), the way
left/right already moves by `step`, and calls `onChanged` only when the snapped
value changes. `AddSlider` is unchanged (drags report every value).

Handles stay valid while the row is hidden and shown again. After the page is
closed or rebuilt, `IsAlive` is false and the members do nothing useful; take
the new handle from the next `BuildPage`.

Example: a host-authoritative session setting. The host edits it; on a client
the row is locked and mirrors the host's value without saving it as the
client's own preference:

```csharp
SulfurToggleHandle friendlyFire;

public void BuildPage(SulfurOptionsContext ctx)
{
    friendlyFire = ctx.AddToggleRow(
        "Friendly fire",
        "Set by the host for the whole session.",
        config.FriendlyFire,
        v => config.FriendlyFire = v);   // only a host can change it

    ApplySessionRole();
}

// Called from the page's timer / a session event:
void ApplySessionRole()
{
    if (friendlyFire == null || !friendlyFire.IsAlive)
        return;

    bool isClient = session.IsClient;
    friendlyFire.SetLocked(isClient);
    friendlyFire.SetValueWithoutNotify(isClient ? session.FriendlyFire : config.FriendlyFire);
}
```

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

### Number precision: float and double

The original number inputs (`AddNumberInput`, `AddInlineNumberInput`,
`AddSettingNumber`, `AddSettingNumberEx` with `float` arguments) parse, clamp,
round and format in `float`. A float holds about 7 significant digits, so
larger or more precise values (`123456789`, `0.123456789`, a `double` config
value) are shown and reported rounded, and a double setting cannot be edited
exactly. Their behaviour is unchanged.

For those values use the `double` overloads (since 0.11.0). They parse with
`double` and the invariant culture (a `,` typed as decimal separator is read as
`.`), clamp to `[min, max]`, round to `decimals` (ties to even, like the float
version; at most 15 decimals) and format without a thousands separator. The
text shows up to 15 significant digits, which a double holds exactly:

```csharp
OptionsScreenOption AddNumberInput(string label, double value, double min, double max, int decimals, Action<double> onChanged);
OptionsScreenOption AddNumberInput(string label, string description, double value, double min, double max, int decimals, Action<double> onChanged);
TMP_InputField AddInlineNumberInput(string label, double value, double min, double max, int decimals, Action<double> onChanged, float labelWidth = 160f);

// SulfurOptionsSettingRows
OptionsScreenOption AddSettingNumber(this SulfurOptionsContext ctx, SulfurSettingRow row, double value, double min, double max, int decimals, Action<double> onChanged);
OptionsScreenOption AddSettingNumberEx(this SulfurOptionsContext ctx, SulfurSettingRow row, double value, double min, double max, int decimals, Action<double, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);
```

They are overloads, not new names: a call that compiled before still picks the
float version (with `float`, `int` or other integer arguments the float
parameters are the better match, and a lambda `v => ...` fits both delegates
equally), so existing mods are unaffected. Pass a `double` value (or `double`
bounds) to get the double version:

```csharp
ctx.AddNumberInput("Seed", seed, 0d, 1e15, 0, v => seed = v);           // double
ctx.AddNumberInput("Port", port, 0f, 65535f, 0, v => port = (int)v);     // float, as before
```

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

`AddInlineNumberInput` also has a `double` overload; see
[Number precision](#number-precision-float-and-double).

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

`AddSettingNumber` / `AddSettingNumberEx` take `float` or `double` values; see
[Number precision](#number-precision-float-and-double).

A row built with an `Ex` method always has its message line and, when
`OnDefault` is set, its Default button, also when they start hidden (empty
`Message`, `ShowDefaultButton = false`), so the handle can show them later. A
hidden part is an inactive row: it takes no space and is no navigation stop.
(The non-`Ex` methods are built the same way; they just do not hand out the
handle.) `AddSettingMeta` on its own builds only what is visible, as before.

---

## Keyboard and gamepad navigation

The options screen turns Unity's EventSystem navigation off, so arrow keys,
Enter/Space and Escape (and the gamepad equivalents) reach only the game's own
option rows. Every interactive library widget is part of that navigation, in
screen order, with the game's own selection highlight (the white row border):

| Widget | Use (Enter / A) | Left / right |
|---|---|---|
| foldout header (`AddFoldout*`) | toggles it; the same header stays selected (nothing while `forceExpanded`) | — |
| `AddSmallButton`, `AddDefaultButton`, setting-row Default button | presses it (if interactable) | — |
| `AddButtonRow` | presses the focused button | moves the focus between the buttons |
| `AddTextInput` / `AddNumberInput` (and the setting text/number rows) | starts typing | — |
| `AddInlineTextInput` / `AddInlineNumberInput` | starts typing | — |
| footer primary button (`SetFooter`) | presses it (if it has an action) | — |

- A **button row** is one navigation stop. The focused button carries a thin
  white frame while the row is selected; hidden and non-interactable buttons are
  skipped, and there is no wrap-around. A rebuild keeps the focus of every
  button row: `ctx.Rebuild()` (also from a foldout click) and
  `SulfurListHandle.Update` give each rebuilt button row the focus of the row
  that sat at the same position in the page's navigation order before — the
  same key that keeps the selected row — clamped to the new row's buttons (a
  hidden or disabled button moves it to the nearest usable one). When rows
  above it appear or disappear in that rebuild, the position belongs to another
  row, and so does the remembered focus, just as for the selected row. Opening
  the page afresh starts every row on its first button.
- A **forced-open foldout** (`forceExpanded: true`) stays a navigation stop and
  looks the same, but clicking it or Use does nothing (no hover/press tint
  either); its stored open/closed state is left as the player set it.
- In a **text input**, typing, the caret keys and Enter/Escape belong to the
  field. Enter or Escape ends editing and returns to row navigation with the same
  row selected. Starting to type with Use selects the whole value of a number
  input (the first key typed replaces it) and puts the caret at the end of a text
  input; a mouse click still puts the caret where the field was clicked. A stacked input is one stop: the row with the box (its label row
  above is not a separate stop any more).
- The **footer** button is always the last stop of the page.
- Rows hidden with `SetVisible(false)`, `SetActive(false)`, a collapsed foldout
  or a hidden list are skipped.
- Mouse behaviour and the look of unselected rows are unchanged. Hovering one of
  these widgets with the mouse shows the widget's own hover tint, not the game's
  border.

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

The text fields default to English (`"Dirty"`, `"Clean"`, `"Restart Required"`,
`"Live Apply"`, `"Advanced"`, `"Hidden"`, `"Dangerous"`, `"Default"`). A field
that still holds exactly its default is displayed in the player's language (the
library's own `lang` files); any other value is displayed unchanged (assigning
the default string itself cannot be told apart from leaving the default). `SulfurOptionsSettingRows.BuildBadges` returns the
texts as displayed.

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

handle.SetMessage("", SulfurMessageKind.Info);   // hides the message line
```

Every row built with an `Ex` method has a message line, also when it was built
without a `Message` (since 0.11.0; before, `SetMessage` silently did nothing on
such a row). An empty or whitespace text hides the line; a non-empty text shows
it with the kind's prefix and background. A hidden line takes no space (the
row's height is the same as a row built without a message) and is no
navigation stop.

### SetActive

```csharp
handle.SetActive(false);
```

Hides or shows every row of the setting block. Showing it again keeps a hidden
message line (empty message) and a Default button hidden with
`SetDefaultButtonVisible(false)` hidden.

### Setting the value without a rebuild

```csharp
public bool SetValueWithoutNotify(bool value);    // AddSettingToggleEx rows
public bool SetValueWithoutNotify(float value);   // AddSettingNumberEx rows
public bool SetValueWithoutNotify(double value);  // AddSettingNumberEx rows
public bool SetValueWithoutNotify(string value);  // AddSettingTextEx rows
public bool SetIndexWithoutNotify(int index);     // AddSettingCycleEx rows
```

Shows a new value in the row's control without calling the row's `onChanged`
(for example after a "Default" button set the draft back to its default). Each
works only on the kind of row the handle was built for; on another kind, or
once the row is gone (its page closed or rebuilt), it does nothing and returns
`false`. None of them throws.

- **Toggle**: sets the checkbox through the row's binding; works while the row
  is locked.
- **Number**: shows the value formatted with the row's `decimals`, not clamped
  (the same as the value the row was built with), and makes it the value
  unparseable input reverts to. Both overloads work on float and double rows;
  a float row stores the value as `float`.
- **Text**: sets the field's text (`null` as `""`); typing still reports
  every change as before.
- **Cycle**: shows the value at `index` (clamped). It has its own name so that
  an integer literal (`handle.SetValueWithoutNotify(5)`) always means a number.

### SetDefaultButtonVisible

```csharp
public bool SetDefaultButtonVisible(bool visible);
```

Shows or hides the row's Default button. The button is built whenever the row
has an `OnDefault` action, also with `ShowDefaultButton = false` (it then starts
hidden), so it can be shown later; returns `false` when the row has no
`OnDefault` or is gone. A hidden button takes no space and is no navigation
stop. If the button is hidden while it is the selected row (Use on Default),
the next up/down moves on from its position.

Example: a Default button that updates the row in place instead of rebuilding
the page:

```csharp
SulfurSettingHandle handle = null;

row.ShowDefaultButton = current != entry.DefaultValue;
row.OnDefault = () =>
{
    drafts.Set(entry, entry.DefaultValue);
    handle.SetValueWithoutNotify(entry.DefaultBool);
    handle.SetDirty(drafts.IsPending(entry));
    handle.SetMessage(GetMessage(entry), SulfurMessageKind.Info);
    handle.SetDefaultButtonVisible(false);
};

ctx.AddSettingToggleEx(row, value, (v, h) => Commit(entry, v, h), out handle);
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

---

## Fonts API

Fonts for text a mod builds itself, chosen the way the game chooses its own: by
**role**, following the current language. The library's own surfaces (options rows,
toasts, banner) use the same API.

### SulfurFontRole

```csharp
SulfurFontRole.RoundRegular   // resource counters, "press to continue"
SulfurFontRole.RoundSemibold  // button legends, rank text
SulfurFontRole.RoundBold
SulfurFontRole.SerifRegular   // menus, options screen, option rows
SulfurFontRole.SerifSemibold  // small HUD text (floor info), dialogue body
SulfurFontRole.SerifBold      // centred HUD alerts, character titles
```

The six values mirror the game's own font roles. In English the Round fonts are
Latin-only; every font from this API carries a CJK/symbol fallback, so any role can
show any language.

### Get

```csharp
TMP_FontAsset font = SulfurFonts.Get(SulfurFontRole.SerifRegular);
```

The game's current font for the role, with glyph fallback attached. While the
game's language assets are still loading — or on a game version without the role
mechanism — it returns TMP's default font (also with fallback). There is no lookup
by font name: names change between game versions and do not follow the language.

### Bind

```csharp
SulfurFonts.Bind(myText, SulfurFontRole.SerifBold);
```

Sets the role's font and the font's own material now, and keeps the text on that
role: when the player changes language, the text switches font automatically.
Binding the same text again replaces its role. Texts are held weakly; destroyed
texts drop out on their own. Size, colour and style stay yours.

### WithFallback

```csharp
TMP_FontAsset own = SulfurFonts.WithFallback(myBundledFont);
```

Attaches the library's CJK and symbol fallback to a font your mod ships, so it
renders missing glyphs like the fonts from `Get`. Idempotent; returns the same font.

### FontsChanged

```csharp
SulfurFonts.FontsChanged += () => RebuildMyCachedStyles();
```

Raised on the main thread after the game's role fonts changed. Bound texts are
already updated when it fires; subscribe only if you keep font assets yourself.

---

## Language API

`SulfurLanguage` is the game's current UI language and one notification when it
becomes known or changes. Use it instead of reading I2 Localization or subscribing
to the game's `onLanguageChange` yourself. No game or I2 types appear in it.

### Follow

```csharp
IDisposable languageFollow = SulfurLanguage.Follow(code => RefreshMyTexts());
// later, when the UI goes away:
languageFollow.Dispose();
```

`IDisposable Follow(Action<string> onLanguage)` calls `onLanguage` with the current
code right away if the language is already known, otherwise once it becomes known,
and then after every change. Dispose the result to stop. This is the one call most
mods need. Exceptions thrown by the callback are logged as warnings and swallowed.

### Code

```csharp
string code = SulfurLanguage.Code; // "en", "zh-CN", "ja", ...
```

`string Code { get; }`: the I2 language code, which is also the
`lang/<code>.json` file name. `"en"` until `IsReady`.

### IsReady

`bool IsReady { get; }`: true once the game's language is known. The game sets its
startup language without raising an event, so the library checks once per frame
until the game has finished loading and reports a code, then stops checking.

### IsRightToLeft

`bool IsRightToLeft { get; }`: true for a right-to-left language (Arabic).

### FixRtl

```csharp
label.text = SulfurLanguage.FixRtl(SulfurLocalization.Get(PluginGuid, "hud.title", "Title"));
```

`string FixRtl(string text)`: shapes the text for display when the language is
right to left, the same way the game shapes its own translations; otherwise returns
it unchanged. Call it once when you resolve the text, not every frame.

### Changed

```csharp
SulfurLanguage.Changed += code => RefreshMyTexts();
```

`event Action<string> Changed`: raised on the main thread once when the language
first becomes known, then after every change (only when the code actually changed).
On a change it runs after all of the game's own language-change handlers, so text
you set is not overwritten by the game. Each subscriber runs in its own try/catch.
`SulfurLocalization.CurrentLanguageCode` and `LanguageVersion` are already updated
when it fires. Prefer `Follow`, which also covers the initial language.
