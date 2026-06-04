# Troubleshooting

## A setting edit takes more than one second

Cause:

```text
ctx.Rebuild() is probably called after every value change.
```

Fix:

- Use `AddSettingXXXEx`.
- Update the current row through `SulfurSettingHandle`.
- Use `ctx.SetFooterStatus(...)` instead of `ctx.Rebuild()`.

---

## Edited value goes back to the old value

Cause:

```text
The page rebuilt and rescanned config entries before the draft was applied.
```

Fix:

- Cache draft values.
- Restore drafts after scanning.
- Do not rebuild after ordinary edits.

---

## The whole themed group looks yellow

Cause:

```text
Unity Outline was used on the group Image.
```

Unity `Outline` duplicates the whole Image graphic, so it can look like a paint-bucket fill.

Fix:

- Remove `Outline`.
- Use `BorderOverlay` with four thin Image lines.
- Keep group background alpha very low or zero.

---

## The group border disappears

Possible causes:

- Border lines were created before setting rows and got covered.
- Border lines were affected by `VerticalLayoutGroup`.
- Stretch anchors conflicted with the native layout.

Fix:

- Put all border lines under `BorderOverlay`.
- Set `LayoutElement.ignoreLayout = true` on the overlay.
- Move `BorderOverlay` to the last sibling when the group scope ends.
- Use fixed calculated width instead of stretch anchors if the group disappears.

---

## Left border disappears

Possible causes:

- The line is clipped at x=0.
- It is covered by child backgrounds.

Fix:

```csharp
CreateBorderLine(overlay.transform, "BorderLeft", borderColor,
    new Vector2(0f, 0f), new Vector2(0f, 1f),
    new Vector2(2f, 0f), new Vector2(6f, 0f));
```

The border is moved inward by 2 px and has 4 px width.

---

## Foldout arrow disappears

Cause:

```text
The selected TMP font may not contain Unicode triangle glyphs.
```

Avoid:

```text
▶
▼
```

Use:

```text
>
v
```

Also render arrow and label as separate TMP_Text objects.

---

## TextAlignmentOptions.MidlineCenter compile error

Cause:

```text
The game's TextMeshPro version does not include MidlineCenter.
```

Fix:

```csharp
TextAlignmentOptions.Center
```

If `MidlineLeft` fails:

```csharp
TextAlignmentOptions.Left
```

---

## Section content frame is too short on the right

Tune width:

```csharp
float width = Mathf.Max(360f, GetNativeOptionWidth() - indentPixels + 24f);
```

Use:

```text
+12f  smaller
+24f  default
+48f  wider
```

---

## Section group disappears after switching to stretch anchors

Cause:

```text
Stretch anchors may conflict with native VerticalLayoutGroup + ContentSizeFitter.
```

Fix:

- Use fixed calculated width.
- Set `LayoutElement.minWidth`.
- Set `LayoutElement.preferredWidth`.
- Set `flexibleWidth = 0f`.

---

## The clean/pending badge does not update

Cause:

```text
You used old AddSettingXXX methods instead of Ex methods.
```

Fix:

```csharp
SulfurSettingHandle handle;

ctx.AddSettingToggleEx(row, value, (v, h) =>
{
    h.SetDirty(true, "Pending", "Unchanged");
}, out handle);
```

---

## Apply button text should be short

Recommended button text:

```text
Apply
```

Recommended Chinese:

```text
应用
```

Do not use overly long footer button labels when space is limited.
