# SULFUR Native UI Lib

Native OptionsScreen integration library for SULFUR BepInEx mods.

## v0.6.2 performance standard

- Caches sample TextMeshPro text and native option width per page context.
- Adds `SulfurSettingHandle` so config editors can update row state without rebuilding the whole page.
- Adds `AddSettingToggleEx`, `AddSettingTextEx`, `AddSettingNumberEx`, and `AddSettingCycleEx` extension APIs.
- Existing `AddSettingToggle/Text/Number/Cycle` APIs are preserved.
