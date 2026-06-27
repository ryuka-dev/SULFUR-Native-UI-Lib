# Examples

## Example 1: Simple custom page

```csharp
private void BuildPage(SulfurOptionsContext ctx)
{
    ctx.SetFooter(
        "Example",
        "Ready.",
        "Apply",
        () => { });

    ctx.AddSection("Example Mod");
    ctx.AddDescription("This is a native SULFUR Options page.");
}
```

---

## Example 2: Toggle with row handle

```csharp
private bool enabled = true;

private void BuildPage(SulfurOptionsContext ctx)
{
    SulfurSettingHandle handle;

    SulfurSettingRow row = new SulfurSettingRow
    {
        Label = "Enable Mod",
        Description = "Enable or disable this mod.",
        IsDirty = false,
        DirtyText = "Pending",
        CleanText = "Unchanged"
    };

    ctx.AddSettingToggleEx(
        row,
        enabled,
        (value, h) =>
        {
            enabled = value;
            h.SetDirty(true, "Pending", "Unchanged");
            h.SetMessage("Click Apply to save this change.", SulfurMessageKind.Warning);
            ctx.SetFooterStatus("Pending change: Enable Mod");
        },
        out handle);
}
```

---

## Example 3: Number setting

```csharp
private float cooldown = 5f;

private void BuildNumber(SulfurOptionsContext ctx)
{
    SulfurSettingHandle handle;

    SulfurSettingRow row = new SulfurSettingRow
    {
        Label = "Cooldown",
        Description = "Cooldown in seconds.",
        IsDirty = false,
        ExtraBadges = new[] { "Single", "Range" },
        Message = "Allowed range: 0.1 - 60",
        MessageKind = SulfurMessageKind.Info
    };

    ctx.AddSettingNumberEx(
        row,
        cooldown,
        0.1f,
        60f,
        2,
        (value, h) =>
        {
            cooldown = value;
            h.SetDirty(true, "Pending", "Unchanged");
            h.SetMessage("Click Apply to save this change.", SulfurMessageKind.Warning);
            ctx.SetFooterStatus("Pending change: Cooldown");
        },
        out handle);
}
```

---

## Example 4: Foldout section with themed group

```csharp
private void BuildPage(SulfurOptionsContext ctx)
{
    bool expanded = ctx.AddSectionFoldout(
        "section.General",
        "General",
        false,
        false);

    if (!expanded)
        return;

    using (ctx.BeginThemedGroup("section.content.General"))
    {
        ctx.AddBadgeRow("Entries: 2", "Unchanged");
        DrawEnableMod(ctx);
        DrawCooldown(ctx);
    }
}
```

---

## Example 5: Config editor callback

```csharp
private void DrawEntry(SulfurOptionsContext ctx, ConfigEntryModel entry)
{
    SulfurSettingHandle handle;

    SulfurSettingRow row = new SulfurSettingRow
    {
        Label = entry.DisplayName,
        Description = entry.Description,
        IsDirty = entry.IsDirty,
        DirtyText = "Pending",
        CleanText = "Unchanged",
        Message = entry.IsDirty ? pendingWarning : entry.RangeText,
        MessageKind = entry.IsDirty ? SulfurMessageKind.Warning : SulfurMessageKind.Info
    };

    ctx.AddSettingTextEx(
        row,
        entry.DraftValue,
        (value, h) =>
        {
            entry.SetDraft(value);
            SaveDraft(entry);

            h.SetDirty(entry.IsDirty, "Pending", "Unchanged");
            h.SetMessage(
                entry.IsDirty ? pendingWarning : entry.RangeText,
                entry.IsDirty ? SulfurMessageKind.Warning : SulfurMessageKind.Info);

            ctx.SetFooterStatus("Pending change: " + entry.Key);
        },
        out handle);
}
```

---

## Example 6: Aligned inline inputs (vanilla-style layout)

```csharp
private string host = "localhost";
private string port = "38281";
private string slot = "Father";
private string password = "";
private float timeout = 10f;

private void BuildPage(SulfurOptionsContext ctx)
{
    ctx.AddSection("Archipelago");
    ctx.AddDescription("Archipelago connection options.");

    // Same labelWidth => every input box starts at the same x position.
    ctx.AddInlineTextInput("Hostname", host, v => host = v);
    ctx.AddInlineTextInput("Port",     port, v => port = v);
    ctx.AddInlineTextInput("Slot",     slot, v => slot = v);
    ctx.AddInlineTextInput("Password", password, v => password = v, password: true);

    ctx.AddInlineNumberInput("Timeout", timeout, 0f, 60f, 1, v => timeout = v);
}
```

Use `AddInlineTextInput` / `AddInlineNumberInput` when you want the label and
input on one line. The stacked `AddTextInput` / `AddNumberInput` helpers are
unchanged, so existing pages keep their current layout.
