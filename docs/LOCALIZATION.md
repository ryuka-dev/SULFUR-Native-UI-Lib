# Localization

Each mod should own its own localization files.

SULFUR Native UI Lib provides loading and lookup helpers. It should not centralize every mod's language files.

---

## Recommended structure

```text
BepInEx/plugins/MyMod/
├─ MyMod.dll
└─ lang/
   ├─ en.json
   ├─ ja.json
   └─ zh-CN.json
```

---

## JSON format

```json
{
  "entries": [
    {
      "key": "plugin.name",
      "value": "My Mod"
    },
    {
      "key": "plugin.description",
      "value": "My Mod description."
    },
    {
      "key": "section.General",
      "value": "General"
    },
    {
      "key": "entry.General.EnableMod.name",
      "value": "Enable Mod"
    },
    {
      "key": "entry.General.EnableMod.description",
      "value": "Enable this mod."
    },
    {
      "key": "value.General.Mode.Natural",
      "value": "Natural"
    }
  ]
}
```

---

## Loading localization

A mod can load its own localization:

```csharp
SulfurLocalization.LoadPluginLocalization(PluginGuid, Info.Location);
```

A config editor scanning other mods should load the target mod's localization:

```csharp
SulfurLocalization.LoadPluginLocalization(pluginGuid, pluginInfo.Location);
```

---

## Lookup

```csharp
string pluginName = SulfurLocalization.Get(
    pluginGuid,
    "plugin.name",
    fallbackName);
```

Setting name:

```csharp
string displayName = SulfurLocalization.Get(
    pluginGuid,
    "entry." + section + "." + key + ".name",
    key);
```

Setting description:

```csharp
string description = SulfurLocalization.Get(
    pluginGuid,
    "entry." + section + "." + key + ".description",
    originalDescription);
```

Value label:

```csharp
string valueLabel = SulfurLocalization.Get(
    pluginGuid,
    "value." + section + "." + key + "." + rawValue,
    rawValue);
```

---

## Language ownership rule

Correct:

```text
DeadeyeInstinct/lang/zh-CN.json
WeaponXpMultiplier/lang/zh-CN.json
SULFURConfig/lang/zh-CN.json
```

Incorrect:

```text
SULFURConfig/lang contains all translations for every mod
```

SULFUR Config should only own SULFUR Config's UI text.
