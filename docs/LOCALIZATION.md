# Localization

Each mod ships its own translations in `lang/*.json` next to its DLL and looks them up
through `SulfurLocalization`. The library does not hold other mods' text.

## Files

```text
BepInEx/plugins/YourName-Example/
├─ YourName.Sulfur.Example.dll
└─ lang/
   ├─ en.json
   ├─ zh-CN.json
   └─ ...
```

- The file name is the game's language code: `en sv fr it de es pt ru pl ja ko zh-CN zh-TW tr ar`.
  Use a hyphen (`zh-CN.json`, not `zh_CN.json`).
- `en.json` is the reference and should hold every key.
- UTF-8 without BOM.

```json
{
  "entries": [
    { "key": "plugin.name", "value": "Example" },
    { "key": "section.General", "value": "General" }
  ]
}
```

`LoadPluginLocalization(guid, Info.Location)` reads every language file from these folders,
in order; a key found in an earlier folder wins:

1. `<dll folder>/lang/`
2. `<dll folder>/` (mod managers that flatten the package)
3. `<parent folder>/lang/`

Each file loaded is logged with its entry count; an empty or unreadable file is logged as a
warning.

## Lookup

```csharp
SulfurLocalization.LoadPluginLocalization(PluginGuid, Info.Location);   // once, in Awake

string text = SulfurLocalization.Get(PluginGuid, "page.footer.status", "Changes are saved immediately.");
```

`Get` tries the current language (`pt-BR`), then the language without region (`pt`), then
`en`, key by key, and returns `fallback` when none has the key. `zh-TW` has its own chain:
`zh-TW`, `zh-CN`, `zh`, `en`, so Traditional Chinese players see Simplified Chinese rather
than English for anything not translated yet. Keep every fallback equal to its `en.json`
value, so a missing file and a loaded English file look the same. `Get` does no
formatting: put placeholders in the value and fill them with `string.Format`. When to look
texts up again after a language change: [API reference: Language](API_REFERENCE.md#language).

## Key format

SULFUR Config loads the same files for every installed plugin and builds its config pages
from these keys. Use them for your BepInEx config entries so one file translates both your
own page and SULFUR Config's:

| Key | Text |
|---|---|
| `plugin.name` | mod name |
| `plugin.description` | mod description |
| `section.<Section>` | config section name |
| `entry.<Section>.<Key>.name` | config entry name |
| `entry.<Section>.<Key>.description` | config entry description |
| `value.<Section>.<Key>.<value>` | display text of one value (serialized form) |

`<Section>` and `<Key>` are the exact English strings passed to `Config.Bind(section, key,
...)`, spaces included. Keys for your own text are free (e.g. `page.*`, `hud.*`); keep them
out of the prefixes above.

## The library's own text

Message prefixes, input placeholders, "Default", "Apply", "MOD", the built-in setting-row
badges and the page-failed message come from the library's own `lang/` folder (keys
`ui.*`, all 15 languages). A missing folder makes them fall back to English.
