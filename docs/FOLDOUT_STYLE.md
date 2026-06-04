# Foldout Style

Foldouts are used to represent hierarchy:

```text
Plugin / Mod       largest
Section            medium
Setting item       smallest
```

---

## Recommended sizes

Plugin foldout:

```text
height: about 66
font: largest, bold
```

Section foldout:

```text
height: about 48
font: medium
```

Setting row:

```text
height: about 50
font: smaller than section
```

---

## Arrow compatibility

Avoid Unicode arrows:

```text
▶
▼
```

Some SULFUR TMP fonts may not contain these glyphs.

Use ASCII arrows:

```text
> collapsed
v expanded
```

Recommended:

```csharp
arrow.text = expanded ? "v" : ">";
arrow.alignment = TextAlignmentOptions.Center;
arrow.fontStyle = FontStyles.Bold;
```

---

## TextMeshPro alignment compatibility

Some TMP versions do not include:

```csharp
TextAlignmentOptions.MidlineCenter
TextAlignmentOptions.MidlineLeft
```

Use these instead:

```csharp
TextAlignmentOptions.Center
TextAlignmentOptions.Left
```

---

## Separate arrow and label

Do not put arrow and label into the same TMP text.

Bad:

```csharp
text.text = (expanded ? "v  " : ">  ") + label;
```

Good:

```text
FoldoutArrow  separate TMP_Text
FoldoutLabel  separate TMP_Text
```

This prevents ellipsis or clipping from hiding the arrow.

---

## Recommended CreateFoldoutText pattern

```csharp
private static void CreateFoldoutText(
    Transform parent,
    string label,
    bool expanded,
    SulfurFoldoutStyle style,
    TextMeshProUGUI sample,
    Color baseColor)
{
    float left = style == SulfurFoldoutStyle.Plugin ? 26f : 32f;
    float arrowWidth = 34f;

    GameObject arrowObject = new GameObject(
        "FoldoutArrow",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(TextMeshProUGUI));

    arrowObject.transform.SetParent(parent, false);

    RectTransform arrowRt = arrowObject.GetComponent<RectTransform>();
    arrowRt.anchorMin = new Vector2(0f, 0f);
    arrowRt.anchorMax = new Vector2(0f, 1f);
    arrowRt.pivot = new Vector2(0f, 0.5f);
    arrowRt.offsetMin = new Vector2(left, 0f);
    arrowRt.offsetMax = new Vector2(left + arrowWidth, 0f);

    TextMeshProUGUI arrow = arrowObject.GetComponent<TextMeshProUGUI>();
    arrow.text = expanded ? "v" : ">";
    arrow.alignment = TextAlignmentOptions.Center;
    arrow.raycastTarget = false;
    arrow.textWrappingMode = TextWrappingModes.NoWrap;
    arrow.overflowMode = TextOverflowModes.Overflow;

    GameObject labelObject = new GameObject(
        "FoldoutLabel",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(TextMeshProUGUI));

    labelObject.transform.SetParent(parent, false);

    RectTransform labelRt = labelObject.GetComponent<RectTransform>();
    labelRt.anchorMin = Vector2.zero;
    labelRt.anchorMax = Vector2.one;
    labelRt.offsetMin = new Vector2(left + arrowWidth + 6f, 0f);
    labelRt.offsetMax = style == SulfurFoldoutStyle.Plugin
        ? new Vector2(-110f, 0f)
        : new Vector2(-24f, 0f);

    TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
    text.text = label ?? "";
    text.alignment = TextAlignmentOptions.Left;
    text.raycastTarget = false;
    text.textWrappingMode = TextWrappingModes.NoWrap;
    text.overflowMode = TextOverflowModes.Ellipsis;

    float fontSize;

    if (sample != null)
    {
        arrow.font = sample.font;
        arrow.fontSharedMaterial = sample.fontSharedMaterial;

        text.font = sample.font;
        text.fontSharedMaterial = sample.fontSharedMaterial;

        if (style == SulfurFoldoutStyle.Plugin)
            fontSize = Mathf.Max(25f, sample.fontSize * 1.22f);
        else if (style == SulfurFoldoutStyle.Section)
            fontSize = Mathf.Max(20f, sample.fontSize * 1.00f);
        else
            fontSize = sample.fontSize;

        Color textColor = style == SulfurFoldoutStyle.Section
            ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.86f)
            : baseColor;

        arrow.color = textColor;
        text.color = textColor;
    }
    else
    {
        fontSize = style == SulfurFoldoutStyle.Plugin ? 26f : 20f;

        arrow.color = baseColor;
        text.color = baseColor;
    }

    arrow.fontSize = fontSize + 2f;
    text.fontSize = fontSize;

    arrow.fontStyle = FontStyles.Bold;
    text.fontStyle = style == SulfurFoldoutStyle.Group ? FontStyles.Normal : FontStyles.Bold;
}
```
