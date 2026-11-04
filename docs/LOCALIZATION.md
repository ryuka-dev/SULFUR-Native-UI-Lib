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

## Following the game's language

Text a mod shows itself (HUD, overlays, its own labels) has to follow the player's
language. Use `SulfurLanguage.Follow`; do not read I2 Localization or subscribe to
the game's `AsyncAssetLoading.onLanguageChange` yourself, and do not poll.

```csharp
private IDisposable languageFollow;

private void ShowOverlay()
{
    languageFollow = SulfurLanguage.Follow(_ => ApplyTexts());
}

private void ApplyTexts()
{
    titleText.text = SulfurLanguage.FixRtl(
        SulfurLocalization.Get(PluginGuid, "overlay.title", "Overlay"));
}

private void HideOverlay()
{
    languageFollow?.Dispose();
    languageFollow = null;
}
```

`Follow` handles what each mod used to handle on its own:

- **Startup language.** The game sets its startup language without raising any
  event, so a player who starts in Chinese never sees a "language changed"
  notification. `Follow` calls back as soon as the language is known: right away
  if it already is, otherwise once the game finishes loading.
- **Ordering.** On a change, the callback runs after all of the game's own
  language-change handlers, so text you set is not overwritten by the game.
- **Lifetime and errors.** `Dispose` unsubscribes. An exception in a callback is
  logged as a warning and does not affect other subscribers.
- **Lookup state.** `SulfurLocalization.Get` already uses the new language inside
  the callback.

`SulfurLanguage.FixRtl` shapes right-to-left text (Arabic) the way the game shapes
its own translations, and returns other text unchanged. Apply it once when the text
is resolved, not every frame. Text passed to options-page rows is resolved when the
page is built.

Fallback chain of `Get`: the exact language code, then the language without its
region (`zh-TW` -> `zh`), then `en`, key by key. `SulfurLanguage.Code` is `"en"`
until `SulfurLanguage.IsReady`.

See [API Reference: Language API](API_REFERENCE.md#language-api).

---

## The library's own text

The few strings the library itself renders (message prefixes, input placeholder,
"No options.", badge fallbacks) live in this repository's `lang/<code>.json`, under
the library's own plugin GUID, with keys prefixed `ui.`. They are deployed next to
the dll as `lang/`.

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
