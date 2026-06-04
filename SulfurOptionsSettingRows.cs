using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using PerfectRandom.Sulfur.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Setting-row convenience API.
    /// The non-Ex methods preserve the original API.
    /// The Ex methods return a SulfurSettingHandle so callers can update one row without ctx.Rebuild().
    /// </summary>
    public static class SulfurOptionsSettingRows
    {
        public static OptionsScreenOption AddSettingToggle(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            bool value,
            Action<bool> onChanged)
        {
            SulfurSettingHandle ignored;
            return AddSettingToggleEx(ctx, row, value, delegate(bool v, SulfurSettingHandle h)
            {
                if (onChanged != null)
                    onChanged(v);
            }, out ignored);
        }

        public static OptionsScreenOption AddSettingText(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            string value,
            Action<string> onChanged)
        {
            SulfurSettingHandle ignored;
            return AddSettingTextEx(ctx, row, value, delegate(string v, SulfurSettingHandle h)
            {
                if (onChanged != null)
                    onChanged(v);
            }, out ignored);
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
            SulfurSettingHandle ignored;
            return AddSettingNumberEx(ctx, row, value, min, max, decimals, delegate(float v, SulfurSettingHandle h)
            {
                if (onChanged != null)
                    onChanged(v);
            }, out ignored);
        }

        public static OptionsScreenOption AddSettingCycle(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string> onChanged)
        {
            SulfurSettingHandle ignored;
            return AddSettingCycleEx(ctx, row, values, currentIndex, delegate(int i, string v, SulfurSettingHandle h)
            {
                if (onChanged != null)
                    onChanged(i, v);
            }, out ignored);
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

        public static OptionsScreenOption AddSettingToggleEx(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            bool value,
            Action<bool, SulfurSettingHandle> onChanged,
            out SulfurSettingHandle handle)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);
            SulfurSettingHandle localHandle = null;

            OptionsScreenOption option = ctx.AddToggle(
                row.Label,
                row.Description,
                value,
                delegate(bool v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                });

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row);
            handle = localHandle;
            return option;
        }

        public static OptionsScreenOption AddSettingTextEx(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            string value,
            Action<string, SulfurSettingHandle> onChanged,
            out SulfurSettingHandle handle)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);
            SulfurSettingHandle localHandle = null;

            OptionsScreenOption option = ctx.AddTextInput(
                row.Label,
                row.Description,
                value,
                delegate(string v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                });

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row);
            handle = localHandle;
            return option;
        }

        public static OptionsScreenOption AddSettingNumberEx(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            float value,
            float min,
            float max,
            int decimals,
            Action<float, SulfurSettingHandle> onChanged,
            out SulfurSettingHandle handle)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);
            SulfurSettingHandle localHandle = null;

            OptionsScreenOption option = ctx.AddNumberInput(
                row.Label,
                row.Description,
                value,
                min,
                max,
                decimals,
                delegate(float v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                });

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row);
            handle = localHandle;
            return option;
        }

        public static OptionsScreenOption AddSettingCycleEx(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            IReadOnlyList<string> values,
            int currentIndex,
            Action<int, string, SulfurSettingHandle> onChanged,
            out SulfurSettingHandle handle)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);
            SulfurSettingHandle localHandle = null;

            OptionsScreenOption option = ctx.AddCycle(
                row.Label,
                row.Description,
                values,
                currentIndex,
                delegate(int i, string v)
                {
                    if (onChanged != null)
                        onChanged(i, v, localHandle);
                });

            AddSettingMeta(ctx, row);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row);
            handle = localHandle;
            return option;
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

        private static SulfurSettingHandle CreateHandle(
            SulfurOptionsContext ctx,
            int startIndex,
            SulfurSettingRow row)
        {
            if (ctx == null)
                return new SulfurSettingHandle(row, null);

            return new SulfurSettingHandle(row, ctx.GetChildrenFrom(startIndex).ToList());
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
            if (ctx == null)
                return 0;

            return ctx.GetChildCount();
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

                ApplyCompactSettingChildStyle(child);
            }
        }

        private static void ApplyCompactSettingChildStyle(Transform child)
        {
            if (child == null)
                return;

            string name = child.name ?? "";
            TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts == null || texts.Length == 0)
                return;

            bool isBadge = name == "SULFUR_BadgeRow";
            bool isMessage = name.StartsWith("SULFUR_Message_");
            bool isDescription = name == "Description";
            bool isSmallButton = name == "SULFUR_SmallButtonRow";
            bool isOption = child.GetComponent<OptionsScreenOption>() != null;

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null)
                    continue;

                text.enableAutoSizing = false;
                text.textWrappingMode = isDescription || isMessage
                    ? TextWrappingModes.Normal
                    : TextWrappingModes.NoWrap;

                if (isOption)
                {
                    // Setting item title/value should be visibly smaller than section and mod foldouts.
                    text.fontSize = Mathf.Clamp(text.fontSize * 0.82f, 15f, 18f);
                    text.fontStyle = FontStyles.Normal;
                }
                else if (isDescription)
                {
                    text.fontSize = Mathf.Clamp(text.fontSize * 0.88f, 12f, 15f);
                }
                else if (isBadge)
                {
                    text.fontSize = Mathf.Clamp(text.fontSize * 0.82f, 11f, 13f);
                }
                else if (isMessage)
                {
                    text.fontSize = Mathf.Clamp(text.fontSize * 0.88f, 12f, 15f);
                }
                else if (isSmallButton)
                {
                    text.fontSize = Mathf.Clamp(text.fontSize * 0.84f, 12f, 14f);
                }
            }

            RectTransform rt = child as RectTransform;
            if (rt == null)
                rt = child.GetComponent<RectTransform>();

            LayoutElement layout = child.GetComponent<LayoutElement>();

            if (isBadge)
                SetRowHeight(rt, layout, 28f);
            else if (isMessage)
                SetRowHeight(rt, layout, 38f);
            else if (isDescription)
                SetRowHeight(rt, layout, 34f);
            else if (isSmallButton)
                SetRowHeight(rt, layout, 36f);
            else if (isOption)
                SetRowHeight(rt, layout, 50f);
        }

        private static void SetRowHeight(RectTransform rt, LayoutElement layout, float height)
        {
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

            if (layout != null)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
            }
        }
    }
}
