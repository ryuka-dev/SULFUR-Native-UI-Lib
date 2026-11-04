# Config editor guide

How to build a large page, such as an in-game editor for every installed mod's BepInEx
config, that stays fast with hundreds of settings. The library only draws rows; scanning,
dirty state, saving and backups belong to your mod.

## Rules

1. **Never rebuild for a value change.** Destroying and recreating hundreds of rows takes
   seconds. Update the changed row through its handle (`AddSetting*Ex`, `AddBadgeRowEx`,
   the `out` foldout overloads) and the footer through `SetFooterStatus`.
2. **Rebuild only when the structure changes:** a filter applied or cleared, a rescan, after
   Apply, a mode switch. Foldout clicks rebuild by themselves.
3. **Collapse by default.** Plugins and sections start closed; pass `forceExpanded: true`
   while a filter is active so results are visible. A forced-open foldout ignores clicks and
   keeps the player's own state for when the filter is cleared.
4. **Scan once, keep drafts outside the page.** `BuildPage` runs on every open, rebuild and
   foldout click; read the configs once and keep unsaved edits in your own model, so a
   rebuild or rescan never loses them.
5. **Text inputs report every keystroke.** Keep a filter as a draft and apply it with a
   button; rebuilding from the input itself would destroy the field while the player types.
6. **Keep rows few.** A setting can be up to five rows (control, description, badges,
   message line, Default button). Keep descriptions short; leave `Message` empty unless
   there is something to say (the hidden line of an `Ex` row takes no space).
7. **Handles die with their context.** Clear every dictionary of handles at the start of
   `BuildPage`.

Hierarchy that reads well: plugin foldout (largest) → section foldout → setting rows inside
a themed group. Do not give every setting its own foldout.

## Example

The model is yours: scan `Chainloader.PluginInfos` once, keep one draft per
`ConfigEntryBase`, write it back with `SetSerializedValue` and `ConfigFile.Save()` on Apply.
The stub below stands in for it; the page part is complete.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Ryuka.Sulfur.NativeUI;

// Stub of your model: bool entries only, grouped by section.
public sealed class EntryView
{
    public string Key, Description;
    public bool Draft, Saved;
    public bool IsDirty { get { return Draft != Saved; } }
}

public sealed class PluginView
{
    public string Guid, Name;
    public SortedDictionary<string, List<EntryView>> Sections = new SortedDictionary<string, List<EntryView>>();
    public int Pending { get { return Sections.Values.SelectMany(s => s).Count(e => e.IsDirty); } }
}

public sealed class ConfigEditorPage
{
    private readonly List<PluginView> plugins = new List<PluginView>();   // filled by your scan
    private readonly Dictionary<string, SulfurBadgeRowHandle> pluginBadges = new Dictionary<string, SulfurBadgeRowHandle>();
    private string filterDraft = "";
    private string activeFilter = "";

    public void BuildPage(SulfurOptionsContext ctx)
    {
        pluginBadges.Clear();   // handles of the previous build are dead

        // Filter: typing only updates the draft; the buttons rebuild.
        ctx.AddInlineTextInput("Filter", filterDraft, v => filterDraft = v);
        ctx.AddButtonRow(
            new SulfurButton("Apply filter", () => { activeFilter = filterDraft.Trim(); ctx.Rebuild(); }),
            new SulfurButton("Clear", () => { filterDraft = activeFilter = ""; ctx.Rebuild(); }));

        bool filtering = activeFilter.Length > 0;

        foreach (PluginView plugin in plugins)
        {
            SulfurBadgeRowHandle badges;
            bool open = ctx.AddPluginFoldoutWithBadges("plugin." + plugin.Guid, plugin.Name,
                false, filtering, new[] { PendingBadge(plugin) }, out badges);
            pluginBadges[plugin.Guid] = badges;

            if (!open)
                continue;

            foreach (KeyValuePair<string, List<EntryView>> section in plugin.Sections)
            {
                if (!ctx.AddSectionFoldout("plugin." + plugin.Guid + ".section." + section.Key, section.Key, false, filtering))
                    continue;

                using (ctx.BeginThemedGroup("section." + plugin.Guid + "." + section.Key))
                {
                    foreach (EntryView entry in section.Value)
                        if (!filtering || entry.Key.IndexOf(activeFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                            DrawEntry(ctx, plugin, entry);
                }
            }
        }

        ctx.SetFooter("Pending: " + CountPending(), "Changes are not saved until you press Apply.", "Apply", () =>
        {
            ApplyAll();
            ctx.Rebuild();   // structure-level refresh after saving
        });
    }

    private void DrawEntry(SulfurOptionsContext ctx, PluginView plugin, EntryView entry)
    {
        SulfurSettingRow row = new SulfurSettingRow
        {
            Label = entry.Key,
            Description = entry.Description,
            IndentLevel = 1,
            IsDirty = entry.IsDirty,
            DirtyText = "Pending",
            CleanText = "Unchanged"
        };

        SulfurSettingHandle handle;
        ctx.AddSettingToggleEx(row, entry.Draft, (v, h) =>
        {
            entry.Draft = v;

            // Update in place: this row, the plugin's badge, the footer. No rebuild.
            h.SetDirty(entry.IsDirty);
            h.SetMessage(entry.IsDirty ? "Not saved until you press Apply." : "", SulfurMessageKind.Warning);

            SulfurBadgeRowHandle badges;
            if (pluginBadges.TryGetValue(plugin.Guid, out badges))
                badges.SetBadges(PendingBadge(plugin));

            ctx.SetFooterStatus("Pending: " + CountPending());
        }, out handle);
    }

    private static string PendingBadge(PluginView plugin)
    {
        return plugin.Pending > 0 ? "Pending: " + plugin.Pending : "Unchanged";
    }

    private int CountPending()
    {
        return plugins.Sum(p => p.Pending);
    }

    private void ApplyAll()
    {
        // Write each dirty draft to its ConfigEntryBase and save its ConfigFile.
        foreach (EntryView entry in plugins.SelectMany(p => p.Sections.Values).SelectMany(s => s))
            entry.Saved = entry.Draft;
    }
}
```

Register it like any page (`BuildPage = editor.BuildPage`). Badge rows of foldouts, setting
rows and the footer all update in place; only the filter buttons and Apply rebuild.

## Themed groups

`BeginThemedGroup` frames everything added inside its `using` block, so the end of a
section is visible while scrolling. The 3-argument overload sets the frame colour and
indent: `BeginThemedGroup(name, themeColor, indentPixels)`. See
[API reference: Themed groups](API_REFERENCE.md#themed-groups).

## Setting rows

Fields, badge order, defaults and every handle member:
[API reference: Setting rows](API_REFERENCE.md#setting-rows). For config editors, set
`DirtyText` / `CleanText` once on the row and call `handle.SetDirty(bool)`; that call needs
the row's first badge to be the status badge, so leave `ShowCleanBadge = true`.
