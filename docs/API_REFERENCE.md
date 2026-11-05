# API reference

Everything is in namespace `Ryuka.Sulfur.NativeUI` and must be called on Unity's main
thread. Signatures below are exact; `ctx` is the `SulfurOptionsContext` passed to
`BuildPage`.

- [Which call to use](#which-call-to-use)
- [Page lifecycle](#page-lifecycle)
- [Soft dependency](#soft-dependency)
- [Pages](#pages)
- [Context, rebuild and footer](#context-rebuild-and-footer)
- [Static rows](#static-rows)
- [Native rows and their handles](#native-rows-and-their-handles)
- [Buttons](#buttons)
- [Text rows and lists](#text-rows-and-lists)
- [Badge rows](#badge-rows)
- [Text and number inputs](#text-and-number-inputs)
- [Foldouts](#foldouts)
- [Themed groups](#themed-groups)
- [Page state](#page-state)
- [Setting rows](#setting-rows)
- [Calls on a dead context](#calls-on-a-dead-context)
- [Keyboard and gamepad navigation](#keyboard-and-gamepad-navigation)
- [Toasts](#toasts)
- [Popup banner](#popup-banner)
- [Fonts](#fonts)
- [Language](#language)
- [Localization](#localization)
- [Other public members](#other-public-members)

## Which call to use

"Older" calls stay supported (compiled mods use them) but give you no way to change the
row after building it.

| Task | New code | Older |
|---|---|---|
| Checkbox | `AddToggleRow` | `AddToggle` |
| Slider | `AddSliderRow` | `AddSlider` |
| Pick one of several values | `AddCycleRow` | `AddCycle` |
| Config-style setting (badges, message line, Default button) | `AddSettingToggleEx` / `AddSettingTextEx` / `AddSettingNumberEx` / `AddSettingCycleEx` | `AddSettingToggle` / `AddSettingText` / `AddSettingNumber` / `AddSettingCycle` / `AddSettingEnum<T>` |
| Badges you update later | `AddBadgeRowEx`; `*FoldoutWithBadges(..., IReadOnlyList<string>, out SulfurBadgeRowHandle)` | `AddBadgeRow`; `*FoldoutWithBadges(..., params string[])` |
| Text that changes while the page is open | `AddTextRow` | `AddDescription` (static, dimmed) |
| Several buttons on one line | `AddButtonRow` | one `AddSmallButton` per row |
| A part of the page that refreshes | `AddList` | `ctx.Rebuild()` |
| Text input | `AddInlineTextInput` (returns the field), `AddSettingTextEx` | `AddTextInput` |
| Number input | `AddInlineNumberInput`, `AddSettingNumberEx`; `double` overloads beyond ~7 digits | `AddNumberInput` |
| Collapsible block | `AddFoldout` / `AddPluginFoldout` / `AddSectionFoldout` | |
| Frame around a block | `BeginThemedGroup` | |
| UI state across rebuilds | `GetState` / `SetState` | |
| Footer summary, status, primary button | `SetFooter`, `SetFooterStatus` | |
| Short message during play | `SulfurToastApi.Show` | |
| Persistent message during play | `SulfurPopupApi.ShowBanner` / `HideBanner` | |
| Font for your own text | `SulfurFonts.Bind` / `Get` | |
| React to the game's language | `SulfurLanguage.Follow` | `SulfurLanguage.Changed`, `SulfurLocalization.LanguageVersion` |

## Page lifecycle

- `BuildPage` runs every time the page is built: when the player opens it, on
  `ctx.Rebuild()`, and on every foldout click. Each run gets a **new** `ctx`, and the footer
  is cleared before it, so set the footer in `BuildPage`.
- The previous `ctx` dies (`IsAlive` false, for good) when the page is rebuilt, another
  category is shown or the options screen closes. Calls on it are ignored and handles built
  from it become inert; see [Calls on a dead context](#calls-on-a-dead-context). Take new
  handles from each run; clear any dictionary of handles at the start of `BuildPage`.
- Nothing you built survives a run. Keep values in your own fields, or in
  [page state](#page-state) (runtime only; foldout open/closed lives there too).
- Page text needs no language handling: the language is changed on the game's own General
  page, which ends your page's context, and the next open builds your page again in the new
  language. If your page is open when the change takes effect (the game loads the language's
  fonts first, so a player can get there), it is rebuilt. The category name is re-read from
  `GetDisplayName`.
- A `BuildPage` that throws shows one error row ("This page failed to load. See the BepInEx
  log.") and the exception is logged (in full the first time).
- Main thread only. A callback from another thread must hand its work to the main thread
  before touching the page, a handle, a toast or the banner.

## Soft dependency

To run without the library installed, keep every library type out of classes that load
when it is missing, and check for it first:

```csharp
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using Ryuka.Sulfur.NativeUI;

[BepInPlugin("yourname.sulfur.example", "Example", "1.0.0")]
[BepInDependency("ryuka.sulfur.nativeui", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class ExamplePlugin : BaseUnityPlugin
{
    private void Awake()
    {
        if (Chainloader.PluginInfos.ContainsKey("ryuka.sulfur.nativeui"))
            NativeUiPage.Register();
    }
}

// The only class that names library types.
internal static class NativeUiPage
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Register()
    {
        SulfurOptionsApi.RegisterPage(new SulfurOptionsPage
        {
            PageId = "yourname.sulfur.example",
            DisplayName = "Example",
            BuildPage = ctx => ctx.AddSection("Example")
        });
    }
}
```

Do not list the library in your Thunderstore dependencies in that case.

## Pages

```csharp
public sealed class SulfurOptionsPage
{
    public string PageId;
    public string DisplayName;
    public int SortOrder = 1000;
    public Func<string> GetDisplayName;
    public Action<SulfurOptionsContext> BuildPage;
}

static void SulfurOptionsApi.RegisterPage(SulfurOptionsPage page);
static void SulfurOptionsApi.UnregisterPage(string pageId);
static IReadOnlyList<SulfurOptionsPage> SulfurOptionsApi.Pages { get; }
```

| Member | |
|---|---|
| `PageId` | Required, global across mods, case-insensitive. Use your plugin GUID. Registering an id again replaces that page and logs a warning naming both. |
| `DisplayName` | Category name when `GetDisplayName` is null or returns empty; `PageId` when this is empty too. |
| `GetDisplayName` | Called whenever the category label is drawn (also after a language change). Return localized text here. |
| `SortOrder` | Pages are sorted ascending, then by `DisplayName`. |
| `BuildPage` | Required. See [Page lifecycle](#page-lifecycle). |

`RegisterPage` throws `ArgumentNullException` for a null page and `ArgumentException` when
`PageId` or `BuildPage` is missing. Call `UnregisterPage` in `OnDestroy`.

## Context, rebuild and footer

```csharp
OptionsScreen OptionsScreen { get; }
RectTransform OptionsContainer { get; }   // where rows go now (inside a themed group or list: that container)
string PageId { get; }
bool IsAlive { get; }

void Rebuild();
void SetFooter(string leftText, string statusText, string primaryButtonText, Action onPrimaryPressed);
void SetFooterStatus(string statusText);
void ClearFooter();
```

- `Rebuild()` runs `BuildPage` again with a new context; it keeps the scroll position, the
  selected row and each button row's focused button. It is expensive on large pages: use it
  for structure changes (filter applied, refresh, Apply), not for value changes.
- `SetFooter` shows a fixed bar under the page: left summary, status text, primary button.
  An empty `primaryButtonText` shows "Apply" in the player's language. `SetFooterStatus`
  changes only the status text; use it for light feedback. `ClearFooter` hides the bar. The
  footer never shows on the game's own categories.
- `OptionsScreen` and `OptionsContainer` expose game/Unity objects for special cases; rows
  you add to them yourself are not part of the library's navigation.

## Static rows

```csharp
void AddSection(string label);                                   // bold header with a rule; empty label adds nothing
OptionsScreenOption AddDescription(string text);                 // dimmed, "— " prefixed, wraps; returns null
OptionsScreenOption AddReadonlyText(string label, string value); // description "label: value"; returns null
void AddMessage(string text, SulfurMessageKind kind);
void AddWarning(string text);                                    // AddMessage(text, Warning)
void AddError(string text);                                      // AddMessage(text, Error)
void AddSuccess(string text);                                    // AddMessage(text, Success)
OptionsScreenOption AddInfo(string label);                       // the game's info row; a navigation stop
void AddSpacer(float height = 20f);

public enum SulfurMessageKind { Info, Warning, Error, Success }
```

Message rows have a tinted background and a localized prefix ("WARNING: ", "ERROR: ", "OK: ";
none for `Info`); they do not wrap, only explicit line breaks add lines. Empty text adds
nothing for descriptions and messages. Descriptions grow with their wrapped text.

## Native rows and their handles

```csharp
SulfurToggleHandle AddToggleRow(string label, bool value, Action<bool> onChanged);
SulfurToggleHandle AddToggleRow(string label, string description, bool value, Action<bool> onChanged);

SulfurSliderHandle AddSliderRow(string label, float value, float min, float max, float step, Action<float> onChanged);
SulfurSliderHandle AddSliderRow(string label, string description, float value, float min, float max, float step, Action<float> onChanged);

SulfurCycleHandle AddCycleRow(string label, IReadOnlyList<string> values, int currentIndex, Action<int, string> onChanged);
SulfurCycleHandle AddCycleRow(string label, string description, IReadOnlyList<string> values, int currentIndex, Action<int, string> onChanged);
```

Older `AddToggle`, `AddSlider`, `AddCycle` take the same parameters, build the same rows
and return `OptionsScreenOption` instead of a handle.

| Member | Toggle | Slider | Cycle |
|---|---|---|---|
| `IsAlive` | row exists and its page is on screen | same | same |
| value | `bool Value` | `float Value` | `int Index`, `string Value` |
| set without `onChanged` | `SetValueWithoutNotify(bool)` | `SetValueWithoutNotify(float)` (clamped, not snapped) | `SetIndexWithoutNotify(int)` (clamped) |
| lock | `SetLocked(bool)`, `IsLocked` | same | same |
| `SetVisible(bool)` (row and its description) | yes | yes | yes |
| `SetLabel(string)` | yes | yes | yes |

- `SetLocked(true)` dims the row, blocks mouse input and makes Use / left-right do nothing;
  `SetValueWithoutNotify` still works. Do not call the game's `OptionsScreenOption.SetLocked`
  on library rows: it makes their label invisible.
- `AddSliderRow` snaps mouse drags to `step` (when `step > 0`) and calls `onChanged` only
  when the snapped value changes. `AddSlider` reports every drag value.
- A cycle with no values shows one empty value; `currentIndex` is clamped.
- Handles survive hiding and showing the row. After a rebuild they are inert (`IsAlive`
  false, getters return defaults).

Example: a host-owned session setting, locked on clients, updated from a session event:

```csharp
private SulfurToggleHandle friendlyFire;

private void BuildPage(SulfurOptionsContext ctx)
{
    friendlyFire = ctx.AddToggleRow("Friendly fire", "Set by the host for the whole session.",
        settings.FriendlyFire, v => settings.FriendlyFire = v);
    ApplySessionRole();
}

private void ApplySessionRole()   // also called from the session's events
{
    if (friendlyFire == null || !friendlyFire.IsAlive)
        return;

    friendlyFire.SetLocked(session.IsClient);
    friendlyFire.SetValueWithoutNotify(session.IsClient ? session.FriendlyFire : settings.FriendlyFire);
}
```

## Buttons

```csharp
IReadOnlyList<SulfurButtonHandle> AddButtonRow(params SulfurButton[] buttons);
IReadOnlyList<SulfurButtonHandle> AddButtonRow(IReadOnlyList<SulfurButton> buttons);
void AddSmallButton(string label, Action onPressed);
void AddSmallButton(string label, Action onPressed, float minWidth);
void AddDefaultButton(Action onPressed);                          // small button labelled "Default" (localized)
OptionsScreenOption AddButton(string label, Action onPressed);    // the game's full-width button row
OptionsScreenOption AddButton(string label, string description, Action onPressed);

public struct SulfurButton
{
    public string Label;
    public Action OnPressed;
    public float MinWidth;                                         // 0 = size from the label
    public SulfurButton(string label, Action onPressed);
    public SulfurButton(string label, Action onPressed, float minWidth);
}
```

- `AddButtonRow` puts the buttons left to right in one row and returns one handle per
  button **that was built**: buttons with an empty label are skipped, and a dead context or
  a row with no labels returns an empty list. Check `Count` before indexing.
- `AddSmallButton` makes its own right-aligned row. An empty label shows "Default".
- Button width follows the label (96–180px) unless `minWidth` / `MinWidth` is set.
- `SulfurButtonHandle`: `SetLabel(string)`, `SetInteractable(bool)` (a disabled button is
  greyed, label included), `SetVisible(bool)`. No `IsAlive`; calls after a rebuild do
  nothing.

```csharp
IReadOnlyList<SulfurButtonHandle> buttons = ctx.AddButtonRow(
    new SulfurButton("Create game", OnCreate),
    new SulfurButton("Join game", OnJoin));

if (buttons.Count == 2)
    buttons[1].SetInteractable(!isHosting);
```

## Text rows and lists

```csharp
SulfurTextHandle AddTextRow(string text);
SulfurListHandle AddList();
```

- `AddTextRow`: full-opacity text, no prefix, cut with "…" when too long; explicit line
  breaks grow the row. Handle: `SetText(string)`, `SetColor(Color)`, `SetVisible(bool)`; no
  `IsAlive`, calls after a rebuild do nothing.
- `AddList`: an empty block that refreshes in place. `Update(Action<SulfurOptionsContext>
  build)` removes its rows and runs `build` with the same context writing into the list, so
  you use the normal `Add*` calls. Also `Clear()` and `SetVisible(bool)`. `Update` and
  `Clear` do nothing once the page is gone, so a timer can keep calling them. Selection and
  button-row focus are kept by position.

```csharp
private SulfurListHandle players;

private void BuildPage(SulfurOptionsContext ctx)
{
    ctx.AddSection("Players");
    players = ctx.AddList();
    RefreshPlayers();
}

private void RefreshPlayers()   // also from a timer or a network event, on the main thread
{
    if (players == null)
        return;

    players.Update(list =>
    {
        foreach (PlayerInfo p in session.Players)
        {
            PlayerInfo player = p;
            list.AddTextRow(player.Name + " - " + player.Ping + " ms");
            list.AddButtonRow(new SulfurButton("Kick " + player.Name, () => session.Kick(player)));
        }
    });
}
```

## Badge rows

```csharp
void AddBadgeRow(params string[] badges);
void AddBadgeRow(IReadOnlyList<string> badges);
SulfurBadgeRowHandle AddBadgeRowEx(params string[] badges);
SulfurBadgeRowHandle AddBadgeRowEx(IReadOnlyList<string> badges);
```

Badges show their text as given (trimmed; empty entries skipped; not translated). Each is at
least 100px wide and grows to its text up to 240px (longer text gets "…"); a row that does
not fit wraps onto more lines. Badges are not navigation stops. `AddBadgeRow` with no
non-empty badge adds nothing. `AddBadgeRowEx` always builds the row and shows it exactly
while it has at least one badge and `SetVisible(false)` is not in effect; otherwise it is
hidden and takes no space.

| `SulfurBadgeRowHandle` | |
|---|---|
| `IsAlive` | row exists and its page is on screen |
| `BadgeCount` | badges shown now |
| `bool SetBadges(params string[])`, `bool SetBadges(IReadOnlyList<string>)` | replaces all; none hides the row; `false` when gone |
| `bool SetBadge(int index, string text)` | one badge; `false` when gone, out of range or `text` empty |
| `SetVisible(bool)` | a row without badges stays hidden |

## Text and number inputs

```csharp
// Label and box on one line; returns the field. Same labelWidth on several rows aligns the boxes.
TMP_InputField AddInlineTextInput(string label, string value, Action<string> onChanged, float labelWidth = 160f, bool password = false);
TMP_InputField AddInlineNumberInput(string label, float value, float min, float max, int decimals, Action<float> onChanged, float labelWidth = 160f);
TMP_InputField AddInlineNumberInput(string label, double value, double min, double max, int decimals, Action<double> onChanged, float labelWidth = 160f);
```

Older `AddTextInput(label, [description,] value, onChanged)` and `AddNumberInput(label,
[description,] value, min, max, decimals, onChanged)` (float and double) put the label on
its own row above a full-width box and return that label row, not the field. With a handle,
the same layout is `AddSettingTextEx` / `AddSettingNumberEx`.

- Text inputs call `onChanged` on **every keystroke**. Number inputs call it when editing
  ends, with the value parsed, clamped to `[min, max]` and rounded to `decimals`;
  unparseable input reverts to the last accepted value.
- `float` overloads hold about 7 significant digits. For larger or more precise values pass
  `double` arguments (e.g. `0d, 1e15`): parsed with the invariant culture (`,` read as
  `.`), up to 15 decimals and 15 significant digits. Calls with `float` or integer
  arguments keep binding to the `float` overloads.
- `password: true` masks the field.

```csharp
ctx.AddInlineTextInput("Hostname", host, v => host = v);
ctx.AddInlineTextInput("Port", port, v => port = v);
ctx.AddInlineTextInput("Password", password, v => password = v, password: true);
ctx.AddInlineNumberInput("Timeout", timeout, 0f, 60f, 1, v => timeout = v);
ctx.AddInlineNumberInput("Seed", seed, 0d, 1e15, 0, v => seed = v);   // double
```

## Foldouts

Extension methods in `SulfurOptionsFoldoutExtensions`. Each adds a clickable header and
returns whether it is open; add the content only when it returns `true`. A click stores the
new state and rebuilds the page. Mind the names: `AddFoldout` is Section style, but
`AddFoldoutWithBadges` is Plugin style (with the "MOD" tag); use
`AddSectionFoldoutWithBadges` for a section header with badges.

```csharp
bool AddFoldout(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded);
bool AddFoldout(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded);
bool AddFoldout(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, string description);
bool AddPluginFoldout(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded);
bool AddSectionFoldout(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded);

// Header followed by a badge row
bool AddFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, params string[] badges);
bool AddPluginFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, params string[] badges);
bool AddSectionFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, params string[] badges);
bool AddFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
bool AddPluginFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
bool AddSectionFoldoutWithBadges(this SulfurOptionsContext ctx, string key, string label, bool defaultExpanded, bool forceExpanded, IReadOnlyList<string> badges, out SulfurBadgeRowHandle badgeRow);
```

| Style | Methods | Look |
|---|---|---|
| Plugin | `AddPluginFoldout*`, `AddFoldoutWithBadges` | 66px, large bold label, accent bar, "MOD" tag on the right |
| Section | `AddFoldout`, `AddSectionFoldout*` | 48px, indented 32px |

- The `description` overload adds a description row under the header.
- `key` identifies the foldout within the page (stored as page state `"foldout." + key`;
  `label` is used when `key` is null). The state lasts until the game closes.
- `forceExpanded: true` shows it open whatever the stored state (e.g. while a search filter
  is active); clicking it does nothing and the stored state is kept for later.
- The badge row sits directly under the header and stays visible while the foldout is
  collapsed. The `params string[]` overloads add it only when there are badges; the `out`
  overloads always build it, as `AddBadgeRowEx` does.

## Themed groups

```csharp
IDisposable BeginThemedGroup(string name);
IDisposable BeginThemedGroup(string name, Color themeColor, float indentPixels);
```

Rows added inside the `using` block go into a framed block: a thin border in the theme
colour around an almost transparent background. `name` names the GameObject. The 1-argument
overload uses the options text colour and 32px indent; the 3-argument one sets both. Always
dispose the scope (use `using`); on a dead context it is an empty scope.

```csharp
if (ctx.AddSectionFoldout("section.General", "General", true, false))
{
    using (ctx.BeginThemedGroup("section.General.content"))
    {
        ctx.AddBadgeRow("Entries: 2");
        ctx.AddToggleRow("Enabled", enabled, v => enabled = v);
        ctx.AddSliderRow("Volume", volume, 0f, 1f, 0.05f, v => volume = v);
    }
}
```

## Page state

Extension methods in `SulfurOptionsStateExtensions`: a small store per page id, kept in
memory until the game closes, shared by every context of the page (it works on a dead
context too). For UI state such as filters and foldouts, not for settings.

```csharp
T GetState<T>(this SulfurOptionsContext ctx, string key, T defaultValue);
void SetState<T>(this SulfurOptionsContext ctx, string key, T value);
bool HasState(this SulfurOptionsContext ctx, string key);
void RemoveState(this SulfurOptionsContext ctx, string key);
void ClearPageState(this SulfurOptionsContext ctx);              // also resets every foldout of the page
bool ToggleBoolState(this SulfurOptionsContext ctx, string key, bool defaultValue);   // returns the new value
```

Keys are case-insensitive. `GetState` returns `defaultValue` when the key is missing or the
stored value cannot be converted to `T`.

## Setting rows

Extension methods in `SulfurOptionsSettingRows`. One setting row is a control, its
description, a badge row, a message line and an optional Default button, described by a
`SulfurSettingRow`.

```csharp
public sealed class SulfurSettingRow
{
    public string Label;
    public string Description;

    public bool IsDirty;
    public bool RequiresRestart;
    public bool LiveApply;
    public bool Advanced;
    public bool Hidden;
    public bool Dangerous;

    public string DirtyText = "Dirty";
    public string CleanText = "Clean";
    public string RestartRequiredText = "Restart Required";
    public string LiveApplyText = "Live Apply";
    public string AdvancedText = "Advanced";
    public string HiddenText = "Hidden";
    public string DangerousText = "Dangerous";

    public string[] ExtraBadges;

    public bool ShowCleanBadge = true;
    public bool ShowDefaultButton = true;
    public string DefaultButtonText = "Default";
    public Action OnDefault;

    public string Message;
    public SulfurMessageKind MessageKind = SulfurMessageKind.Info;

    public int IndentLevel = 0;
    public float IndentPixels = 28f;
}
```

- Badges, in order: `DirtyText` when `IsDirty`, else `CleanText` when `ShowCleanBadge`; then
  one per true flag (`RequiresRestart` … `Dangerous`); then `ExtraBadges`.
- A text field that still holds its English default is shown in the player's language;
  any other value is shown as is.
- Setting `OnDefault` adds a Default button (shown because `ShowDefaultButton` defaults to
  `true`).
- `IndentLevel > 0` indents every part by `IndentLevel * IndentPixels` and switches the row
  to a compact style (smaller text; description and message wrap). `IndentPixels <= 0` counts
  as 28.

```csharp
OptionsScreenOption AddSettingToggleEx(this SulfurOptionsContext ctx, SulfurSettingRow row, bool value, Action<bool, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);
OptionsScreenOption AddSettingTextEx(this SulfurOptionsContext ctx, SulfurSettingRow row, string value, Action<string, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);
OptionsScreenOption AddSettingNumberEx(this SulfurOptionsContext ctx, SulfurSettingRow row, float value, float min, float max, int decimals, Action<float, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);
OptionsScreenOption AddSettingNumberEx(this SulfurOptionsContext ctx, SulfurSettingRow row, double value, double min, double max, int decimals, Action<double, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);
OptionsScreenOption AddSettingCycleEx(this SulfurOptionsContext ctx, SulfurSettingRow row, IReadOnlyList<string> values, int currentIndex, Action<int, string, SulfurSettingHandle> onChanged, out SulfurSettingHandle handle);

// No Ex equivalent
OptionsScreenOption AddSettingEnum<TEnum>(this SulfurOptionsContext ctx, SulfurSettingRow row, TEnum value, Action<TEnum> onChanged);   // TEnum must be an enum; shows the member names
void AddSettingMeta(this SulfurOptionsContext ctx, SulfurSettingRow row);   // badge row, message (if any), Default button (if shown)
static List<string> SulfurOptionsSettingRows.BuildBadges(SulfurSettingRow row);   // badge texts as displayed
```

Older `AddSettingToggle`, `AddSettingText`, `AddSettingNumber` (float and double) and
`AddSettingCycle` build the same rows without the `out` handle; their `onChanged` gets only
the value.

The text, number and cycle controls behave as in [inputs](#text-and-number-inputs) and
[native rows](#native-rows-and-their-handles). For an `int` setting use the float
`AddSettingNumberEx` with `decimals: 0` and cast the value (`(int)v`; it is already
rounded). The `onChanged` of the `Ex` methods also
receives the row's handle. Rows built by these methods always contain a message line and,
with `OnDefault`, a Default button, hidden when empty or `ShowDefaultButton = false`, so the
handle can show them later; a hidden part takes no space and is no navigation stop.

| `SulfurSettingHandle` | |
|---|---|
| `SetDirty(bool dirty)`, `SetDirty(bool dirty, string dirtyText, string cleanText)` | shows the status badge (leftmost) with the dirty or clean text, adding or removing it as needed; while clean with `ShowCleanBadge = false` the row has none. Flag badges and `ExtraBadges` stay as built. |
| `SetMessage(string text, SulfurMessageKind kind)` | shows the message line; empty text hides it |
| `SetActive(bool)` | hides / shows the whole block; an empty message line and a hidden Default button stay hidden |
| `bool SetDefaultButtonVisible(bool)` | `false` when the row has no `OnDefault` or is gone |
| `bool SetValueWithoutNotify(bool)` | toggle rows |
| `bool SetValueWithoutNotify(float)`, `bool SetValueWithoutNotify(double)` | number rows: formatted with the row's decimals, not clamped; becomes the value bad input reverts to |
| `bool SetValueWithoutNotify(string)` | text rows (`null` as `""`) |
| `bool SetIndexWithoutNotify(int)` | cycle rows (clamped) |

The value setters return `false` on another kind of row or once the row is gone; nothing
throws. The handle has no `IsAlive`: check `IsAlive` of the `ctx` that built it, or the
setters' return value. Example: a Default button that updates its row without a rebuild:

```csharp
SulfurSettingHandle handle = null;

SulfurSettingRow row = new SulfurSettingRow
{
    Label = "Enabled",
    IsDirty = enabled != savedEnabled,
    ShowDefaultButton = enabled != DefaultEnabled,
    OnDefault = () =>
    {
        enabled = DefaultEnabled;
        handle.SetValueWithoutNotify(DefaultEnabled);
        handle.SetDirty(enabled != savedEnabled);
        handle.SetDefaultButtonVisible(false);
    }
};

ctx.AddSettingToggleEx(row, enabled, (v, h) =>
{
    enabled = v;
    h.SetDirty(enabled != savedEnabled);
    h.SetDefaultButtonVisible(enabled != DefaultEnabled);
}, out handle);
```

## Calls on a dead context

A context is dead once its page was rebuilt, left or closed (`IsAlive` false). The first
ignored call logs one warning per context.

| Call | Result |
|---|---|
| `Rebuild`, `SetFooter`, `SetFooterStatus`, `ClearFooter` | nothing |
| `AddSection`, `AddMessage` and friends, `AddBadgeRow`, `AddSmallButton`, `AddDefaultButton`, `AddSpacer` | nothing added |
| `AddToggle`, `AddSlider`, `AddCycle`, `AddButton`, `AddInfo`, `AddTextInput`, `AddNumberInput`, `AddDescription`, `AddReadonlyText`, `AddSetting*` (non-`Ex`) | `null` |
| `AddInlineTextInput`, `AddInlineNumberInput` | `null` |
| `AddToggleRow`, `AddSliderRow`, `AddCycleRow`, `AddBadgeRowEx`, `AddTextRow`, `AddList` | an inert handle (`IsAlive` false where it exists; members do nothing) |
| `AddButtonRow` | an empty list |
| `AddSetting*Ex` | `null`, and an inert `SulfurSettingHandle` (setters return `false`) |
| foldouts | no header; still return the stored (or forced) state |
| `BeginThemedGroup` | an empty scope |
| `GetState` / `SetState` / ... | work as usual |

## Keyboard and gamepad navigation

Nothing to set up: every interactive library row is reachable in screen order, and Use
(Enter / A) presses, toggles or starts typing. A button row is one stop where left/right
picks the button. Hidden rows and hidden or disabled buttons are skipped; the footer button
is the last stop. A rebuild keeps the selected row and each button row's focused button, so
a button whose action rebuilds the page stays selected.

## Toasts

```csharp
static void SulfurToastApi.Show(string message);
static void SulfurToastApi.Show(string message, float durationSeconds);
static void SulfurToastApi.Show(string title, string message);
static void SulfurToastApi.Show(string title, string message, float durationSeconds);
```

A card slides into the top-right corner during play, holds for `durationSeconds` (default
4 s; also for a value `<= 0`), then slides out. New toasts stack on top; at most 5 are
shown, older ones leave early. Title in `SerifBold`, message in `SerifSemibold`, both
following the language. Fire-and-forget: no handle, no input capture, no pause, no cursor
change, independent of time scale. Exceptions are logged, not thrown.

```csharp
SulfurToastApi.Show("Saved.");
SulfurToastApi.Show("Player joined", "Cousin connected to the lobby.");
```

## Popup banner

```csharp
static void SulfurPopupApi.ShowBanner(string text);
static void SulfurPopupApi.HideBanner();
```

One banner above the screen centre, shown during play until `HideBanner`. Calling
`ShowBanner` again replaces the text. Display only: it captures no input, does not pause
and does not change the cursor, so key handling (e.g. "Press [Return] to enter") stays in
your mod. Text in `SerifBold` at 34pt, following the language. `HideBanner` is safe when
nothing is shown. Survives scene loads.

## Fonts

For text your mod builds itself (HUD, overlays). Library rows already use these fonts.

```csharp
public enum SulfurFontRole { RoundRegular, RoundSemibold, RoundBold, SerifRegular, SerifSemibold, SerifBold }

static TMP_FontAsset SulfurFonts.Get(SulfurFontRole role);
static void SulfurFonts.Bind(TMP_Text text, SulfurFontRole role);
static TMP_FontAsset SulfurFonts.WithFallback(TMP_FontAsset font);
static event Action SulfurFonts.FontsChanged;
```

| Role | The game uses it for |
|---|---|
| `SerifRegular` | menus, the options screen, option rows |
| `SerifSemibold` | small HUD text, dialogue body |
| `SerifBold` | centred HUD alerts, character titles |
| `RoundRegular` | resource counters, "press to continue" |
| `RoundSemibold` | button legends, rank text |

- `Get`: the game's current font for the role. Until the game's fonts are loaded (or on a
  game version without font roles) it returns TMP's default font.
- `Bind`: sets the role's font and its material now and switches the text when the language
  changes. Binding again replaces the role; destroyed texts drop out. Size, colour and style
  stay yours.
- Every font from `Get` / `Bind` carries a CJK and symbol fallback built from installed
  system fonts, so missing glyphs render in any language. `WithFallback` attaches the same to
  a font your mod ships (idempotent; returns the same font).
- `FontsChanged`: after the role fonts changed; bound texts are already updated. Only
  needed if you keep font assets yourself.
- An unknown role value throws `ArgumentOutOfRangeException`.

## Language

```csharp
static IDisposable SulfurLanguage.Follow(Action<string> onLanguage);
static string SulfurLanguage.Code { get; }          // "en", "zh-CN", "ja", ... = lang/<code>.json
static bool SulfurLanguage.IsReady { get; }
static bool SulfurLanguage.IsRightToLeft { get; }   // Arabic
static string SulfurLanguage.FixRtl(string text);
static event Action<string> SulfurLanguage.Changed;
```

- `Follow` is for text outside the options page: HUD, overlays, strings you cache. Options
  page text needs nothing (see [Page lifecycle](#page-lifecycle)).
- `Follow` calls `onLanguage` with the current code at once if the language is known,
  otherwise when it becomes known (the game sets its startup language without any event),
  then after every change. Dispose the result to stop. Subscribe once (`Awake`) and dispose
  in `OnDestroy`; never in `BuildPage`, which runs on every open and would stack callbacks.
- Notifications run on the main thread after all of the game's own language handlers, so
  text you set is not overwritten. Each callback runs in its own try/catch; exceptions are
  logged as warnings.
- `Code` is `"en"` until `IsReady`. `Changed` fires once when the language becomes known,
  then on every actual change; `Follow` covers both.
- `FixRtl` shapes right-to-left text the way the game shapes its own; other text is returned
  unchanged. Call it once when you resolve a text, not every frame.

```csharp
private IDisposable languageFollow;
private string hudTitle;

private void Awake()
{
    languageFollow = SulfurLanguage.Follow(code =>
        hudTitle = SulfurLanguage.FixRtl(SulfurLocalization.Get(PluginGuid, "hud.title", "Overlay")));
}

private void OnDestroy()
{
    languageFollow.Dispose();
}
```

## Localization

```csharp
static void SulfurLocalization.LoadPluginLocalization(string pluginGuid, string pluginAssemblyLocation);
static string SulfurLocalization.Get(string pluginGuid, string key, string fallback);
static string SulfurLocalization.CurrentLanguageCode { get; }   // lookup language; "en" until known
static int SulfurLocalization.LanguageVersion { get; }          // +1 on each SulfurLanguage.Changed
static void SulfurLocalization.RefreshCurrentLanguage(bool force);   // kept for compatibility; not needed
```

File layout, key format and lookup order: [LOCALIZATION.md](LOCALIZATION.md).

## Other public members

| Member | |
|---|---|
| `Plugin.PluginGuid` / `PluginName` / `PluginVersion` | `"ryuka.sulfur.nativeui"`, `"SULFUR Native UI Lib"`, `"0.11.0"` (constants, compiled into your mod) |
| `SulfurFoldoutStyle` (`Plugin`, `Section`, `Group`) | public enum; no public method takes it |
