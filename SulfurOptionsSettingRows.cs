using System;
using System.Collections.Generic;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// v0.6.1 setting-row convenience API.
    /// This is intentionally implemented as extension methods so existing v0.4 SulfurOptionsContext
    /// does not need to be rewritten.
    /// </summary>
    public static class SulfurOptionsSettingRows
    {
        public static OptionsScreenOption AddSettingToggle(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            bool value,
            Action<bool> onChanged)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);

            OptionsScreenOption option = ctx.AddToggle(
                row.Label,
                row.Description,
                value,
                onChanged);

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            return option;
        }

        public static OptionsScreenOption AddSettingText(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            string value,
            Action<string> onChanged)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);

            OptionsScreenOption option = ctx.AddTextInput(
                row.Label,
                row.Description,
                value,
                onChanged);

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            return option;
        }

        public static OptionsScreenOption AddSettingNumber(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            float value,
            float min,
            float max,
            int decimals,
            Action<float> onChanged)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);

            OptionsScreenOption option = ctx.AddNumberInput(
                row.Label,
                row.Description,
                value,
                min,
                max,
                decimals,
                onChanged);

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            return option;
        }

        public static OptionsScreenOption AddSettingCycle(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);

            OptionsScreenOption option = ctx.AddCycle(
                row.Label,
                row.Description,
                values,
                currentIndex,
                onChanged);

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            return option;
        }

        public static OptionsScreenOption AddSettingEnum<TEnum>(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            TEnum value,
            Action<TEnum> onChanged)
        {
            string[] names = Enum.GetNames(typeof(TEnum));
            int index = Array.IndexOf(names, value != null ? value.ToString() : "");

            if (index < 0)
                index = 0;

            return ctx.AddSettingCycle(
                row,
                names,
                index,
                delegate(int newIndex, string selected)
                {
                    try
                    {
                        object parsed = Enum.Parse(typeof(TEnum), selected);
                        if (onChanged != null)
                            onChanged((TEnum)parsed);
                    }
                    catch
                    {
                    }
                });
        }

        public static void AddSettingMeta(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row)
        {
            row = Normalize(row);

            List<string> badges = BuildBadges(row);
            if (badges.Count > 0)
                ctx.AddBadgeRow(badges);

            if (!string.IsNullOrWhiteSpace(row.Message))
                ctx.AddMessage(row.Message, row.MessageKind);

            if (row.ShowDefaultButton && row.OnDefault != null)
            {
                ctx.AddSmallButton(
                    string.IsNullOrWhiteSpace(row.DefaultButtonText) ? "Default" : row.DefaultButtonText,
                    row.OnDefault);
            }
        }

        public static List<string> BuildBadges(SulfurSettingRow row)
        {
            row = Normalize(row);

            List<string> badges = new List<string>();

            if (row.IsDirty)
            {
                AddBadge(badges, row.DirtyText);
            }
            else if (row.ShowCleanBadge)
            {
                AddBadge(badges, row.CleanText);
            }

            if (row.RequiresRestart)
                AddBadge(badges, row.RestartRequiredText);

            if (row.LiveApply)
                AddBadge(badges, row.LiveApplyText);

            if (row.Advanced)
                AddBadge(badges, row.AdvancedText);

            if (row.Hidden)
                AddBadge(badges, row.HiddenText);

            if (row.Dangerous)
                AddBadge(badges, row.DangerousText);

            if (row.ExtraBadges != null)
            {
                foreach (string extra in row.ExtraBadges)
                    AddBadge(badges, extra);
            }

            return badges;
        }

        private static SulfurSettingRow Normalize(SulfurSettingRow row)
        {
            if (row == null)
                row = new SulfurSettingRow();

            if (row.Label == null)
                row.Label = "";

            if (row.Description == null)
                row.Description = "";

            if (row.IndentLevel < 0)
                row.IndentLevel = 0;

            if (row.IndentPixels <= 0f)
                row.IndentPixels = 28f;

            return row;
        }

        private static void AddBadge(List<string> badges, string text)
        {
            if (badges == null || string.IsNullOrWhiteSpace(text))
                return;

            badges.Add(text.Trim());
        }

        private static int GetChildCount(SulfurOptionsContext ctx)
        {
            if (ctx == null || ctx.OptionsContainer == null)
                return 0;

            return ctx.OptionsContainer.childCount;
        }

        private static void ApplyIndentToNewChildren(
            SulfurOptionsContext ctx,
            int startIndex,
            SulfurSettingRow row)
        {
            if (ctx == null || ctx.OptionsContainer == null || row == null)
                return;

            if (row.IndentLevel <= 0)
                return;

            float indent = row.IndentLevel * row.IndentPixels;

            for (int i = startIndex; i < ctx.OptionsContainer.childCount; i++)
            {
                Transform child = ctx.OptionsContainer.GetChild(i);
                if (child == null)
                    continue;

                RectTransform rt = child as RectTransform;
                if (rt == null)
                    rt = child.GetComponent<RectTransform>();

                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + indent, rt.anchoredPosition.y);

                    if (rt.sizeDelta.x > indent + 120f)
                        rt.sizeDelta = new Vector2(rt.sizeDelta.x - indent, rt.sizeDelta.y);
                }

                LayoutElement layout = child.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    if (layout.minWidth > indent + 120f)
                        layout.minWidth -= indent;

                    if (layout.preferredWidth > indent + 120f)
                        layout.preferredWidth -= indent;
                }
            }
        }
    }
}
