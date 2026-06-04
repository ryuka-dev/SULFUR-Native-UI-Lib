using System;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    public enum SulfurFoldoutStyle
    {
        Plugin,
        Section,
        Group
    }

    public static class SulfurOptionsFoldoutExtensions
    {
        public static bool AddFoldout(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded)
        {
            return AddStyledFoldout(ctx, key, label, defaultExpanded, false, null, SulfurFoldoutStyle.Section);
        }

        public static bool AddFoldout(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded)
        {
            return AddStyledFoldout(ctx, key, label, defaultExpanded, forceExpanded, null, SulfurFoldoutStyle.Section);
        }

        public static bool AddFoldout(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded,
            string description)
        {
            return AddStyledFoldout(ctx, key, label, defaultExpanded, forceExpanded, description, SulfurFoldoutStyle.Section);
        }

        public static bool AddPluginFoldout(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded)
        {
            return AddStyledFoldout(ctx, key, label, defaultExpanded, forceExpanded, null, SulfurFoldoutStyle.Plugin);
        }

        public static bool AddSectionFoldout(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded)
        {
            return AddStyledFoldout(ctx, key, label, defaultExpanded, forceExpanded, null, SulfurFoldoutStyle.Section);
        }

        public static bool AddFoldoutWithBadges(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded,
            params string[] badges)
        {
            return AddPluginFoldoutWithBadges(ctx, key, label, defaultExpanded, forceExpanded, badges);
        }

        public static bool AddPluginFoldoutWithBadges(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded,
            params string[] badges)
        {
            bool expanded = AddStyledFoldout(
                ctx,
                key,
                label,
                defaultExpanded,
                forceExpanded,
                null,
                SulfurFoldoutStyle.Plugin);

            if (badges != null && badges.Length > 0)
                ctx.AddBadgeRow(badges);

            return expanded;
        }

        public static bool AddSectionFoldoutWithBadges(
            this SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded,
            params string[] badges)
        {
            bool expanded = AddStyledFoldout(
                ctx,
                key,
                label,
                defaultExpanded,
                forceExpanded,
                null,
                SulfurFoldoutStyle.Section);

            if (badges != null && badges.Length > 0)
                ctx.AddBadgeRow(badges);

            return expanded;
        }

        private static bool AddStyledFoldout(
            SulfurOptionsContext ctx,
            string key,
            string label,
            bool defaultExpanded,
            bool forceExpanded,
            string description,
            SulfurFoldoutStyle style)
        {
            if (ctx == null)
                return defaultExpanded;

            string stateKey = "foldout." + (key ?? label ?? "unnamed");
            bool expanded = forceExpanded || ctx.GetState(stateKey, defaultExpanded);

            CreateFoldoutRow(
                ctx,
                key,
                label,
                expanded,
                style,
                delegate
                {
                    bool current = ctx.GetState(stateKey, defaultExpanded);
                    ctx.SetState(stateKey, !current);
                    ctx.Rebuild();
                });

            if (!string.IsNullOrWhiteSpace(description))
                ctx.AddDescription(description);

            return expanded;
        }

        private static void CreateFoldoutRow(
            SulfurOptionsContext ctx,
            string key,
            string label,
            bool expanded,
            SulfurFoldoutStyle style,
            UnityAction onPressed)
        {
            RectTransform parent = ctx.OptionsContainer;
            if (parent == null)
                return;

            float width = GetRowWidth(parent);
            float height = style == SulfurFoldoutStyle.Plugin ? 58f : 42f;
            float rowIndent = style == SulfurFoldoutStyle.Plugin ? 0f : 26f;
            float rowWidth = Mathf.Max(200f, width - rowIndent);

            GameObject row = new GameObject(
                "SULFUR_Foldout_" + style + "_" + SafeName(key ?? label),
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            row.transform.SetParent(parent, false);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(rowIndent, 0f);
            rowRt.sizeDelta = new Vector2(rowWidth, height);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.minWidth = rowWidth;
            layout.preferredWidth = rowWidth;
            layout.flexibleWidth = 0f;

            TextMeshProUGUI sample = FindSampleText(parent);
            Color baseColor = sample != null ? sample.color : new Color(1f, 0.65f, 0.15f, 1f);

            Image background = row.GetComponent<Image>();
            if (style == SulfurFoldoutStyle.Plugin)
                background.color = new Color(1f, 1f, 1f, 0.075f);
            else if (style == SulfurFoldoutStyle.Section)
                background.color = new Color(1f, 1f, 1f, 0.028f);
            else
                background.color = new Color(1f, 1f, 1f, 0.04f);

            background.raycastTarget = true;

            Button button = row.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.35f);
            colors.pressedColor = new Color(1f, 1f, 1f, 1.65f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1.35f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;

            button.onClick.RemoveAllListeners();
            if (onPressed != null)
                button.onClick.AddListener(onPressed);

            if (style == SulfurFoldoutStyle.Plugin)
                CreateAccentLine(row.transform, baseColor);

            CreateFoldoutText(row.transform, label, expanded, style, sample, baseColor);

            if (style == SulfurFoldoutStyle.Plugin)
                CreateRightTag(row.transform, "MOD", sample, baseColor);
        }

        private static void CreateAccentLine(Transform parent, Color color)
        {
            GameObject lineObject = new GameObject(
                "AccentLine",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            lineObject.transform.SetParent(parent, false);

            RectTransform rt = lineObject.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(0f, 6f);
            rt.offsetMax = new Vector2(5f, -6f);

            Image image = lineObject.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.95f);
            image.raycastTarget = false;
        }

        private static void CreateFoldoutText(
            Transform parent,
            string label,
            bool expanded,
            SulfurFoldoutStyle style,
            TextMeshProUGUI sample,
            Color baseColor)
        {
            GameObject textObject = new GameObject(
                "FoldoutText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(parent, false);

            RectTransform rt = textObject.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            if (style == SulfurFoldoutStyle.Plugin)
                rt.offsetMin = new Vector2(22f, 0f);
            else if (style == SulfurFoldoutStyle.Section)
                rt.offsetMin = new Vector2(34f, 0f);
            else
                rt.offsetMin = new Vector2(28f, 0f);

            rt.offsetMax = style == SulfurFoldoutStyle.Plugin
                ? new Vector2(-110f, 0f)
                : new Vector2(-24f, 0f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = (expanded ? "▼  " : "▶  ") + (label ?? "");
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;

                if (style == SulfurFoldoutStyle.Plugin)
                    text.fontSize = Mathf.Max(21f, sample.fontSize * 1.08f);
                else if (style == SulfurFoldoutStyle.Section)
                    text.fontSize = Mathf.Max(17f, sample.fontSize * 0.86f);
                else
                    text.fontSize = sample.fontSize;

                text.color = style == SulfurFoldoutStyle.Section
                    ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.82f)
                    : baseColor;
            }
            else
            {
                text.fontSize = style == SulfurFoldoutStyle.Plugin ? 22f : 17f;
                text.color = baseColor;
            }

            text.fontStyle = style == SulfurFoldoutStyle.Plugin ? FontStyles.Bold : FontStyles.Normal;
        }

        private static void CreateRightTag(
            Transform parent,
            string tag,
            TextMeshProUGUI sample,
            Color baseColor)
        {
            GameObject tagObject = new GameObject(
                "FoldoutRightTag",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            tagObject.transform.SetParent(parent, false);

            RectTransform rt = tagObject.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(92f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 0f);

            TextMeshProUGUI text = tagObject.GetComponent<TextMeshProUGUI>();
            text.text = tag ?? "";
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontStyle = FontStyles.Bold;

            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
                text.fontSize = Mathf.Max(13f, sample.fontSize * 0.62f);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.62f);
            }
            else
            {
                text.fontSize = 13f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.62f);
            }
        }

        private static TextMeshProUGUI FindSampleText(RectTransform parent)
        {
            if (parent == null)
                return null;

            TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI text in texts)
            {
                if (text != null && text.font != null)
                    return text;
            }

            return null;
        }

        private static float GetRowWidth(RectTransform parent)
        {
            if (parent != null && parent.rect.width > 100f)
                return parent.rect.width;

            return 900f;
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();

            foreach (char c in invalid)
                value = value.Replace(c, '_');

            return value.Replace(' ', '_');
        }
    }
}
