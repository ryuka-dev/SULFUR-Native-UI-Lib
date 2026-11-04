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

        /// <summary>As the float <c>AddSettingNumber</c>, for a double value (see the double <c>AddSettingNumberEx</c>).</summary>
        public static OptionsScreenOption AddSettingNumber(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            double value,
            double min,
            double max,
            int decimals,
            Action<double> onChanged)
        {
            SulfurSettingHandle ignored;
            return AddSettingNumberEx(ctx, row, value, min, max, decimals, delegate(double v, SulfurSettingHandle h)
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
            SulfurSettingParts parts = new SulfurSettingParts { Kind = SulfurSettingControlKind.Toggle };

            OptionsScreenOption option = ctx.AddToggleCore(
                row.Label,
                row.Description,
                value,
                delegate(bool v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                },
                out parts.Toggle,
                out parts.Binding);

            AddSettingMetaParts(ctx, row, parts);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row, parts);
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
            SulfurSettingParts parts = new SulfurSettingParts { Kind = SulfurSettingControlKind.Text };

            OptionsScreenOption option = ctx.AddTextInputCore(
                row.Label,
                row.Description,
                value,
                delegate(string v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                },
                out parts.TextInput);

            AddSettingMetaParts(ctx, row, parts);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row, parts);
            handle = localHandle;
            return option;
        }

        /// <summary>
        /// Adds a number setting row for a float value. Float holds about 7 significant
        /// digits: larger or more precise values are shown and reported rounded. Use the
        /// double overload for those.
        /// </summary>
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
            SulfurSettingParts parts = new SulfurSettingParts { Kind = SulfurSettingControlKind.Number };

            OptionsScreenOption option = ctx.AddNumberInputCore(
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
                },
                null,
                false,
                out parts.Number);

            AddSettingMetaParts(ctx, row, parts);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row, parts);
            handle = localHandle;
            return option;
        }

        /// <summary>
        /// Adds a number setting row for a double value: parsed, clamped, rounded and
        /// formatted in double with the invariant culture, exact up to 15 significant digits.
        /// Otherwise the same as the float overload.
        /// </summary>
        public static OptionsScreenOption AddSettingNumberEx(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row,
            double value,
            double min,
            double max,
            int decimals,
            Action<double, SulfurSettingHandle> onChanged,
            out SulfurSettingHandle handle)
        {
            row = Normalize(row);
            int start = GetChildCount(ctx);
            SulfurSettingHandle localHandle = null;
            SulfurSettingParts parts = new SulfurSettingParts { Kind = SulfurSettingControlKind.Number };

            OptionsScreenOption option = ctx.AddNumberInputCore(
                row.Label,
                row.Description,
                value,
                min,
                max,
                decimals,
                null,
                delegate(double v)
                {
                    if (onChanged != null)
                        onChanged(v, localHandle);
                },
                true,
                out parts.Number);

            AddSettingMetaParts(ctx, row, parts);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row, parts);
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
            SulfurSettingParts parts = new SulfurSettingParts { Kind = SulfurSettingControlKind.Cycle };

            OptionsScreenOption option = ctx.AddCycleCore(
                row.Label,
                row.Description,
                values,
                currentIndex,
                delegate(int i, string v)
                {
                    if (onChanged != null)
                        onChanged(i, v, localHandle);
                },
                out parts.Binding);

            AddSettingMetaParts(ctx, row, parts);
            ApplyIndentToNewChildren(ctx, start, row);

            localHandle = CreateHandle(ctx, start, row, parts);
            handle = localHandle;
            return option;
        }

        /// <summary>
        /// Adds the badge row, the message line (when <see cref="SulfurSettingRow.Message"/>
        /// is set) and the Default button (when <see cref="SulfurSettingRow.ShowDefaultButton"/>
        /// is true and <see cref="SulfurSettingRow.OnDefault"/> is set) of a setting row.
        /// </summary>
        public static void AddSettingMeta(
            this SulfurOptionsContext ctx,
            SulfurSettingRow row)
        {
            AddSettingMetaParts(ctx, row, null);
        }

        // With parts (a row that gets a handle) the message line and the Default button are
        // built also when they start hidden: an empty message line and a Default button with
        // ShowDefaultButton false are inactive (layout groups skip inactive rows, and an
        // inactive row is no navigation stop), so the handle can show them later. Without
        // parts nothing hidden is built (the original AddSettingMeta).
        private static void AddSettingMetaParts(
            SulfurOptionsContext ctx,
            SulfurSettingRow row,
            SulfurSettingParts parts)
        {
            row = Normalize(row);
            bool forHandle = parts != null;

            List<string> badges = BuildBadges(row);
            GameObject badgeRow = badges.Count > 0 ? ctx.AddBadgeRowObject(badges) : null;

            GameObject messageRow = ctx.AddMessageRow(row.Message, row.MessageKind, forHandle);

            GameObject defaultButtonRow = null;
            if (row.OnDefault != null && (row.ShowDefaultButton || forHandle))
            {
                defaultButtonRow = ctx.AddSmallButtonRow(
                    string.IsNullOrWhiteSpace(row.DefaultButtonText)
                        ? SulfurUiText.DefaultButton
                        : SulfurUiText.ForSettingRow(row.DefaultButtonText, SulfurSettingRow.BuiltInDefaultButtonText, SulfurUiText.DefaultButton),
                    row.OnDefault,
                    0f);

                if (defaultButtonRow != null && !row.ShowDefaultButton)
                    defaultButtonRow.SetActive(false);
            }

            if (parts != null)
            {
                parts.BadgeRow = badgeRow;
                parts.MessageRow = messageRow;
                parts.DefaultButtonRow = defaultButtonRow;
            }
        }

        /// <summary>
        /// The badge texts of <paramref name="row"/> as displayed: a text field still holding
        /// its built-in English default is returned in the player's language.
        /// </summary>
        public static List<string> BuildBadges(SulfurSettingRow row)
        {
            row = Normalize(row);

            List<string> badges = new List<string>();

            if (row.IsDirty)
            {
                AddBadge(badges, SulfurUiText.SettingDirtyText(row.DirtyText));
            }
            else if (row.ShowCleanBadge)
            {
                AddBadge(badges, SulfurUiText.SettingCleanText(row.CleanText));
            }

            if (row.RequiresRestart)
                AddBadge(badges, SulfurUiText.ForSettingRow(row.RestartRequiredText, SulfurSettingRow.BuiltInRestartRequiredText, SulfurUiText.RestartRequiredBadge));

            if (row.LiveApply)
                AddBadge(badges, SulfurUiText.ForSettingRow(row.LiveApplyText, SulfurSettingRow.BuiltInLiveApplyText, SulfurUiText.LiveApplyBadge));

            if (row.Advanced)
                AddBadge(badges, SulfurUiText.ForSettingRow(row.AdvancedText, SulfurSettingRow.BuiltInAdvancedText, SulfurUiText.AdvancedBadge));

            if (row.Hidden)
                AddBadge(badges, SulfurUiText.ForSettingRow(row.HiddenText, SulfurSettingRow.BuiltInHiddenText, SulfurUiText.HiddenBadge));

            if (row.Dangerous)
                AddBadge(badges, SulfurUiText.ForSettingRow(row.DangerousText, SulfurSettingRow.BuiltInDangerousText, SulfurUiText.DangerousBadge));

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
            SulfurSettingRow row,
            SulfurSettingParts parts)
        {
            // A closed context added nothing; its container is the live page's.
            if (ctx == null || !ctx.IsAlive)
                return new SulfurSettingHandle(row, null, parts);

            return new SulfurSettingHandle(row, ctx.GetChildrenFrom(startIndex).ToList(), parts);
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
            // A closed/rebuilt context added no rows; its container is the live page's.
            if (ctx == null || !ctx.IsAlive || ctx.OptionsContainer == null || row == null)
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
