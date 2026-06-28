# In-Game Popup Banner

A display-only HUD message banner, independent of the Options screen. It renders
during normal gameplay/combat, survives scene loads, and never pauses the game or
captures input.

This is the producer side of the LD-2c handoff from **SULFUR Together**
(`Docs/NativeUiConfirmPopupHandoff.md` in that repo). Status: **implemented** as of
this lib version. Only the §3.1 minimum (display-only banner) is built; the
optional §3.2 self-input confirm modal is intentionally not built yet.

## API

```csharp
namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurPopupApi
    {
        // Show, or replace the text of, the single persistent banner. Idempotent —
        // repeated calls update the text rather than stacking. Stays up until Hide.
        // Call from Unity's main thread.
        public static void ShowBanner(string text);

        // Hide the banner if shown. Safe when nothing is shown; never creates the
        // overlay if it was never used.
        public static void HideBanner();
    }
}
```

That is the whole surface. No handle, no callback, no input — the banner is purely
visual; the caller keeps ownership of any keypress/confirm logic.

## Behavior guarantees

- **HUD overlay**, not the Options screen — a dedicated `ScreenSpaceOverlay` canvas
  at a high `sortingOrder` (above the vanilla HUD). Renders during play and combat.
- **Persistent** until `HideBanner()` — it does not auto-dismiss on a timer.
- **Passive** — no `GraphicRaycaster`, every graphic has `raycastTarget = false`,
  no `Time.timeScale` change, no cursor-lock change. It cannot eat clicks or input.
- **Single instance** — repeated `ShowBanner` updates the text; it never stacks.
- **Cheap when hidden** — hiding deactivates the visual root; no per-frame cost.
- **Lazy** — the overlay is created on the first `ShowBanner`; `HideBanner` never
  creates it.
- **Language-correct font** — the label samples a live, localized game
  `TextMeshProUGUI` so non-Latin languages (CJK, Cyrillic, …) render correctly
  instead of as blank boxes. If no live text is available yet, it falls back to the
  TMP default font *without locking in*, so a later `ShowBanner` upgrades to the
  real localized font.

## Consumer wiring (SULFUR Together, LD-2c)

1. Add a BepInEx dependency on this plugin:

   ```csharp
   [BepInDependency("ryuka.sulfur.nativeui")] // hard, or soft + guarded
   ```

2. Assign the seam Actions in `Plugin` init:

   ```csharp
   ArenaLockdownManager.ShowPrompt = SulfurPopupApi.ShowBanner;
   ArenaLockdownManager.HidePrompt = SulfurPopupApi.HideBanner;
   ```

   `ShowPrompt(text)` fires at t0+10 s (e.g. `"Press [Return] to enter the arena"`);
   `HidePrompt()` fires on teleport-in (confirm / boss-death release) or on
   scene change / `Clear()`.

3. In-game verify: an out-of-room player at t0+10 s sees the centered prompt;
   pressing the confirm key teleports them in and the prompt disappears; boss death
   also clears it.

The confirm keypress stays owned by the mod (`ArenaLockdownManager.LocalTick()`
polling `Plugin.Cfg.ArenaEnterConfirmKey`). The banner is display-only.

## Source

- `Popup/SulfurPopupApi.cs` — public entry point.
- `Popup/SulfurPopupController.cs` — persistent self-built uGUI overlay
  (`Canvas` + `TextMeshProUGUI` + translucent `Image`).

## Not built (optional follow-up)

Handoff §3.2: `ShowConfirm(text, KeyCode, Action onConfirm)` returning an
`IPopupHandle` that owns its own input. If added later, LD-2c could drop its own
key-poll and switch to it. Not required for LD-2c.
