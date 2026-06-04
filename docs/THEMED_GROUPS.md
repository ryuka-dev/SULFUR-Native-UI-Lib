# Themed Groups

Themed groups visually show the contents of one foldout section.

They are useful when a page has this hierarchy:

```text
Plugin / Mod
  Section
    Setting
    Setting
    Setting
```

Without a group, it is hard to see where a section's settings end when scrolling.

---

## Usage

```csharp
bool sectionExpanded = ctx.AddSectionFoldout(
    "plugin." + plugin.Guid + ".section." + section.Name,
    section.DisplayName,
    false,
    forceExpanded);

if (!sectionExpanded)
    return;

using (ctx.BeginThemedGroup("section.content." + plugin.Guid + "." + section.Name))
{
    ctx.AddBadgeRow(
        "Entries: " + visibleSectionEntries.Count,
        sectionPending > 0 ? "Pending: " + sectionPending : "Unchanged");

    foreach (ConfigEntryModel entry in visibleSectionEntries)
        DrawEntry(ctx, entry);
}
```

---

## Expected visual result

```text
v Section Name
┌──────────────────────────────┐
│ Entries: 3 / Unchanged        │
│ Setting 1                     │
│ Setting 2                     │
│ Setting 3                     │
└──────────────────────────────┘
```

---

## Implementation standard

Use a transparent group background plus a `BorderOverlay`.

Recommended structure:

```text
SULFUR_ThemedGroup
├─ Image               very transparent background only
├─ VerticalLayoutGroup
├─ ContentSizeFitter
├─ BorderOverlay       ignoreLayout, last sibling
│  ├─ BorderLeft
│  ├─ BorderRight
│  ├─ BorderTop
│  └─ BorderBottom
├─ BadgeRow
├─ Setting row
└─ Setting row
```

---

## Do not use Unity Outline

Do not use:

```csharp
typeof(Image),
typeof(Outline)
```

Unity `Outline` duplicates the whole `Image` graphic. This can make the entire group look filled with theme color instead of drawing a clean border.

Use four thin `Image` lines instead.

---

## Recommended BeginThemedGroup implementation pattern

```csharp
public IDisposable BeginThemedGroup(string name, Color themeColor, float indentPixels)
{
    RectTransform parent = OptionsContainer;
    if (parent == null)
        return new SulfurContainerScope(this, null, false);

    float width = Mathf.Max(360f, GetNativeOptionWidth() - indentPixels + 24f);

    GameObject group = new GameObject(
        string.IsNullOrWhiteSpace(name) ? "SULFUR_ThemedGroup" : name,
        typeof(RectTransform),
        typeof(LayoutElement),
        typeof(CanvasRenderer),
        typeof(Image),
        typeof(VerticalLayoutGroup),
        typeof(ContentSizeFitter));

    group.transform.SetParent(parent, false);

    RectTransform rt = group.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 1f);
    rt.anchorMax = new Vector2(0f, 1f);
    rt.pivot = new Vector2(0f, 1f);
    rt.anchoredPosition = new Vector2(indentPixels, 0f);
    rt.sizeDelta = new Vector2(width, 0f);

    LayoutElement groupLayout = group.GetComponent<LayoutElement>();
    groupLayout.minWidth = width;
    groupLayout.preferredWidth = width;
    groupLayout.flexibleWidth = 0f;

    Image image = group.GetComponent<Image>();
    image.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.004f);
    image.raycastTarget = false;

    VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
    layout.padding = new RectOffset(18, 14, 10, 12);
    layout.spacing = 6f;
    layout.childAlignment = TextAnchor.UpperLeft;
    layout.childControlWidth = true;
    layout.childControlHeight = false;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;

    ContentSizeFitter fitter = group.GetComponent<ContentSizeFitter>();
    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    Color borderColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);

    GameObject overlay = CreateBorderOverlay(group.transform, borderColor);
    overlay.name = "BorderOverlay";

    containerStack.Push(currentOptionsContainer);
    currentOptionsContainer = rt;

    return new SulfurContainerScope(this, rt, true);
}
```

---

## Recommended BorderOverlay

```csharp
private static GameObject CreateBorderOverlay(Transform parent, Color borderColor)
{
    GameObject overlay = new GameObject(
        "BorderOverlay",
        typeof(RectTransform),
        typeof(LayoutElement));

    overlay.transform.SetParent(parent, false);

    RectTransform overlayRt = overlay.GetComponent<RectTransform>();
    overlayRt.anchorMin = Vector2.zero;
    overlayRt.anchorMax = Vector2.one;
    overlayRt.offsetMin = Vector2.zero;
    overlayRt.offsetMax = Vector2.zero;

    LayoutElement overlayLayout = overlay.GetComponent<LayoutElement>();
    overlayLayout.ignoreLayout = true;

    CreateBorderLine(overlay.transform, "BorderLeft", borderColor,
        new Vector2(0f, 0f), new Vector2(0f, 1f),
        new Vector2(2f, 0f), new Vector2(6f, 0f));

    CreateBorderLine(overlay.transform, "BorderRight", borderColor,
        new Vector2(1f, 0f), new Vector2(1f, 1f),
        new Vector2(-6f, 0f), new Vector2(-2f, 0f));

    CreateBorderLine(overlay.transform, "BorderTop", borderColor,
        new Vector2(0f, 1f), new Vector2(1f, 1f),
        new Vector2(2f, -3f), new Vector2(-2f, -1f));

    CreateBorderLine(overlay.transform, "BorderBottom", borderColor,
        new Vector2(0f, 0f), new Vector2(1f, 0f),
        new Vector2(2f, 1f), new Vector2(-2f, 3f));

    return overlay;
}
```

---

## Bring border to front

At the end of the scope, move `BorderOverlay` to the last sibling:

```csharp
private static void BringBordersToFront(RectTransform root)
{
    if (root == null)
        return;

    MoveLast(root, "BorderOverlay");
}

private static void MoveLast(Transform root, string childName)
{
    Transform child = root.Find(childName);
    if (child != null)
        child.SetAsLastSibling();
}
```

This prevents setting-row backgrounds from covering the border.

---

## Width tuning

The width is controlled by:

```csharp
float width = Mathf.Max(360f, GetNativeOptionWidth() - indentPixels + 24f);
```

Common tuning:

```text
+12f  smaller
+24f  default
+48f  wider
```

Do not use stretch anchors if the group disappears under native `VerticalLayoutGroup + ContentSizeFitter`.

---

## Color tuning

Background:

```csharp
image.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.004f);
```

Border:

```csharp
Color borderColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);
```

If the background looks like a paint bucket fill, reduce background alpha or remove the group background entirely.
