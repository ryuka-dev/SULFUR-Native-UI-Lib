# Toast Notifications

Transient message toasts shown in the **top-right corner** during normal gameplay.
A second HUD surface alongside the popup banner, for short status messages that
appear, hold briefly, then animate away on their own. Multiple toasts stack.

This surface is built to serve the **SULFUR Together** co-op mod, but is generic —
the library knows nothing about the caller.

## API

```csharp
namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurToastApi
    {
        // Fire-and-forget. Newest stacks on top; each auto-dismisses.
        public static void Show(string message);
        public static void Show(string message, float durationSeconds);
        public static void Show(string title, string message);
        public static void Show(string title, string message, float durationSeconds);
    }
}
```

- `title` is optional — pass only a message for a single-line toast, or a title +
  message for a two-line card (title in amber, message in warm off-white).
- `durationSeconds` is the hold time between the enter and exit animations; a
  non-positive value (or the no-duration overload) uses the default (~4 s).
- No handle is returned — toasts are fire-and-forget. (A dismiss handle could be
  added later if a consumer needs to retract a specific toast early.)

```csharp
SulfurToastApi.Show("Player joined", "Cousin connected to the lobby.");
SulfurToastApi.Show("Saved.");
SulfurToastApi.Show("Reloading level…", 2.5f);
```

## Behavior

- **Top-right HUD overlay** on its own `ScreenSpaceOverlay` canvas, sorted above
  the popup banner so a toast is never hidden behind it.
- **Stacks** — newest on top. When a toast appears, the ones below glide down; when
  one expires, the ones below glide up to close the gap. At most ~5 are shown at
  once; older still-visible ones are retired early (they animate out, never pop).
- **Smooth, Apple-style motion**, all on `Time.unscaledDeltaTime` (independent of
  game time scale):
  - *Enter*: slide in from the right with a slight overshoot (`easeOutBack`),
    fade in, scale up from 0.94.
  - *Hold*: fully visible for its duration.
  - *Exit*: accelerate out to the right (`easeInCubic`), fade and shrink slightly.
  - *Restack*: vertical position eases toward its slot with `SmoothDamp`, so cards
    glide rather than jump when the stack changes.
- **Passive** — no `GraphicRaycaster`, `CanvasGroup.blocksRaycasts = false`, no
  `Time.timeScale` change, no cursor-lock change. Toasts never intercept input.
- **Language-correct font** — sampled from a live game `TextMeshProUGUI` (shared
  with the banner via `SulfurUiFont`), so non-Latin languages render correctly.
- **Lazy & cheap** — the overlay is created on the first `Show`; the per-frame
  update loop only runs while at least one toast exists.

## Styling

Shared `SulfurUiTheme` palette (also used by the banner): warm near-black panel
fill, SULFUR's signature amber for the title and the left accent bar, warm
off-white body text.

## Source

- `Toast/SulfurToastApi.cs` — public entry point.
- `Toast/SulfurToastController.cs` — persistent canvas, stacking layout, update loop.
- `Toast/SulfurToastView.cs` — one card and its animation state machine.
- `Core/SulfurUiTheme.cs`, `Core/SulfurUiFont.cs` — shared palette and font sampling.

## Consumer wiring (SULFUR Together)

Not yet wired on the co-op mod side. When ready: take the existing
`ryuka.sulfur.nativeui` dependency and call `SulfurToastApi.Show(...)` from
wherever a transient status message is wanted (player join/leave, level reload,
boss-phase callouts, etc.). No seam Actions are required — it is a direct call.
