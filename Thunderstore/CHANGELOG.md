# Changelog

## 0.11.1
Fixed
- Traditional Chinese: a game started in Traditional Chinese is now recognised as such. Mods that read their text through the library show Traditional Chinese when they have it, and Simplified Chinese instead of English when they do not.
- A language change no longer breaks if you open a mod's Options page while the new language is loading. The page no longer loses text and is shown in the new language.
- The library follows a language change even if another mod's language handler fails.
- The summary at the bottom of a mod's Options page is no longer cut off early.

Added
- The library's own text is translated into Traditional Chinese, so all 15 game languages are covered.

## 0.11.0
Fixed
- Toasts and the popup banner no longer lose Chinese (and other non-Latin) characters, and no longer change font from one session to the next. They now use the game's own fonts for the current language and follow a language change while shown.
- Button rows in custom Options pages no longer show a stray vertical "1 0 0 %" next to the label.
- Keyboard and gamepad navigation on mod Options pages: every control can now be reached and used, hidden rows are skipped, and refreshed lists no longer cause errors or a wrong order.
- Typing a space in a mod's text box no longer rebuilds the page; toggles and sliders keep saving after being hidden; a mod page that fails to build shows a message instead of a blank page.
- Gamepad users get the Resume button selected in the pause menu again.

Added
- `SulfurFonts` for mod developers: `Get(role)` returns the game's font for one of its six font roles, `Bind(text, role)` keeps a text on that role across language changes, and `WithFallback(font)` gives a mod's own font the same CJK/symbol coverage.
- `SulfurLanguage.Follow(...)` for mod developers: one call to follow the game's language, including the startup language, notified after the game's own handlers.
- Row handles (`AddToggleRow` / `AddSliderRow` / `AddCycleRow`) with set-without-notify, lock, hide and relabel.
- The library's own text is translated into all 14 game languages.

## 0.10.1
Fixed
- Small-button width now actually applies. Previously the `minWidth` argument and the label-based auto-size had no effect on the rendered width, so every small button stayed at a fixed 120px and labels longer than ~10 characters were cut off with an ellipsis (e.g. `Open-source repo` → `Open-sourc…`). Buttons now size to their label, and an explicit `minWidth` widens them as intended.

## 0.10.0
Added
- `AddButtonRow(...)` for mod developers: lays several small buttons out left-to-right in one row instead of one stacked row per button. `AddSmallButton` is unchanged for the single-button case.
- Live-update row handles so a page can change parts of itself while open without a full rebuild: `AddTextRow` returns a `SulfurTextHandle`, `AddButtonRow` returns per-button `SulfurButtonHandle`s, and `AddList` returns a `SulfurListHandle` for refreshable sections.
- Automatic missing-glyph fallback built from installed OS fonts (a broad CJK face plus a symbol face), applied per-glyph on top of the game's active font. Fixes blank rows and missing symbols (e.g. checkmarks) in non-English languages without any bundled font assets.

Changed
- `AddButtonRow` now returns `IReadOnlyList<SulfurButtonHandle>` instead of `void`. It was introduced in this same release and had not shipped before, so no released API is affected.

## 0.9.0
Added
- New in-game toast notifications for mod developers (`SulfurToastApi.Show`): short messages that slide into the top-right corner during gameplay, hold briefly, then animate away. Supports an optional title and custom duration, and multiple toasts stack with smooth slide/fade animations. Display-only and passive — it never pauses the game, captures input, or changes the cursor.

## 0.8.0
Added
- New in-game popup banner for mod developers (`SulfurPopupApi.ShowBanner` / `HideBanner`): a single centered, persistent on-screen message that can appear during normal gameplay and combat, styled to match the game's UI. It is display-only — it never pauses the game, captures input, or changes the cursor, so the calling mod keeps full control of any keypress.

## 0.7.3
Fixed
- Fixed custom rows (badges, descriptions, section headers, text/number inputs) showing as blank boxes in non-English languages such as Chinese, Japanese, Korean, and Russian. They now use the game's current language font instead of the default Latin font.

## 0.7.2
Added
- New inline input rows for mod developers (`AddInlineTextInput` / `AddInlineNumberInput`): the label and input field sit on the same line, matching the vanilla Options layout, with optional password masking.

Fixed
- Fixed text and option font sizes shrinking a little every time a page refreshed (for example on each edit in a config editor), until the Options screen was reopened.

## 0.7.1
Fixed
- Fixed an issue where custom Native UI pages could disappear every second time the Options menu was opened.
- Custom option categories are now rebuilt more safely when the game Options screen is opened again.
- Improved custom category cleanup to avoid stale category buttons, invalid category references, or mismatched OptionsScreen state.
- Improved custom category label refreshing so labels remain valid after the game UI is rebuilt or re-enabled.

Changed
- Native UI custom categories are now recreated after OptionsScreen setup instead of relying on old injected category objects.
- This should make custom pages more stable when closing and reopening the Options menu with ESC.

## 0.7.0

- Reworked localization loading: searches multiple folders for language files, auto-detects language files by name, merges entries from several sources, and follows the current game language.

## 0.6.3

- Added themed section group documentation standard.
- Added guidance for large config editor pages.
- Improved foldout hierarchy styling standard.
- Added row-handle based update pattern for high-performance UI updates.
- Documented that normal value edits should not rebuild the whole page.
- Documented ASCII foldout arrows for TMP font compatibility.
- Documented themed group border behavior and Unity Outline pitfalls.

## 0.6.2

- Added `SulfurSettingHandle`.
- Added high-performance `AddSettingToggleEx`, `AddSettingTextEx`, `AddSettingNumberEx`, and `AddSettingCycleEx`.
- Cached sample text and native option width per page context.
- Preserved existing `AddSettingToggle`, `AddSettingText`, `AddSettingNumber`, and `AddSettingCycle` APIs.

## 0.6.1

- Added custom Options page registration.
- Added basic native-style row creation helpers.
- Added localization helper support.
