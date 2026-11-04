using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a setting row block.
    /// This allows config editors to update the current row state without rebuilding the whole options page.
    /// </summary>
    /// <remarks>
    /// The value setters work only on the kind of row the handle was built for
    /// (<c>AddSettingToggleEx</c>: bool, <c>AddSettingTextEx</c>: string,
    /// <c>AddSettingNumberEx</c>: float/double, <c>AddSettingCycleEx</c>: index). On another
    /// kind, or once the row is gone (its page closed or rebuilt), they do nothing and return
    /// false; they never throw.
    /// </remarks>
    public sealed class SulfurSettingHandle
    {
        private readonly List<Transform> createdChildren = new List<Transform>();
        private readonly SulfurSettingRow row;
        private readonly SulfurSettingParts parts;
        private readonly GameObject badgeRow;
        private readonly SulfurOptionsContext context;
        private GameObject statusBadge;

        // Rows found by name (no build-time parts): the first badge label, changed in place.
        private readonly TextMeshProUGUI legacyStatusText;

        private readonly TextMeshProUGUI messageText;
        private readonly Image messageBackground;
        private readonly GameObject messageRow;
        private readonly GameObject defaultButtonRow;

        // What SetActive(true) brings back: the message line only while it has a message,
        // the Default button only while it is meant to be shown.
        private bool rowActive = true;
        private bool messageVisible;
        private bool defaultButtonVisible;

        internal SulfurSettingHandle(SulfurSettingRow row, IEnumerable<Transform> children)
            : this(row, children, null)
        {
        }

        internal SulfurSettingHandle(SulfurSettingRow row, IEnumerable<Transform> children, SulfurSettingParts parts)
        {
            bool hasParts = parts != null;
            this.row = row ?? new SulfurSettingRow();
            this.parts = parts ?? new SulfurSettingParts();

            if (children != null)
            {
                foreach (Transform child in children)
                {
                    if (child != null)
                        createdChildren.Add(child);
                }
            }

            badgeRow = this.parts.BadgeRow;
            context = this.parts.Context;
            statusBadge = this.parts.StatusBadge;

            if (!hasParts)
                legacyStatusText = FindFirstBadgeText();

            messageRow = this.parts.MessageRow;
            messageText = messageRow != null ? FirstText(messageRow) : FindMessageText();

            if (messageRow != null)
                messageBackground = messageRow.GetComponent<Image>();
            else if (messageText != null)
                messageBackground = messageText.GetComponentInParent<Image>();

            messageVisible = !string.IsNullOrWhiteSpace(this.row.Message);

            defaultButtonRow = this.parts.DefaultButtonRow;
            defaultButtonVisible = this.row.ShowDefaultButton;
        }

        public void SetDirty(bool dirty)
        {
            SetDirty(dirty, row.DirtyText, row.CleanText);
        }

        /// <summary>
        /// Shows the row's status badge for <paramref name="dirty"/>: <paramref name="dirtyText"/>
        /// while dirty, <paramref name="cleanText"/> while clean (none while clean when the row
        /// has <see cref="SulfurSettingRow.ShowCleanBadge"/> false). The status badge is always
        /// the leftmost one; it is added or removed as needed, and the row's other badges stay
        /// as built.
        /// </summary>
        public void SetDirty(bool dirty, string dirtyText, string cleanText)
        {
            row.IsDirty = dirty;

            // The row's built-in English defaults are shown in the player's language.
            string text = dirty ? SulfurUiText.SettingDirtyText(dirtyText) : SulfurUiText.SettingCleanText(cleanText);

            if (string.IsNullOrWhiteSpace(text))
                text = dirty ? SulfurUiText.DirtyBadge : SulfurUiText.CleanBadge;

            if (badgeRow == null)
            {
                // Gone (page closed or rebuilt), or a row found by name.
                if (legacyStatusText != null)
                    legacyStatusText.text = text;
                return;
            }

            if (!dirty && !row.ShowCleanBadge)
            {
                RemoveStatusBadge();
                return;
            }

            if (statusBadge != null)
            {
                TextMeshProUGUI label = statusBadge.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = text;
                return;
            }

            AddStatusBadge(text);
        }

        private void AddStatusBadge(string text)
        {
            if (context == null || !context.IsAlive)
                return;

            // A badge built with the row has the row's text style (compact when indented).
            TextMeshProUGUI sibling = FirstText(badgeRow);

            Transform rowTransform = badgeRow.transform;
            context.CreateBadge(rowTransform, text.Trim());

            Transform badge = rowTransform.GetChild(rowTransform.childCount - 1);
            badge.SetAsFirstSibling();
            statusBadge = badge.gameObject;

            TextMeshProUGUI label = badge.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                if (sibling != null)
                {
                    label.fontSize = sibling.fontSize;
                    label.enableAutoSizing = sibling.enableAutoSizing;
                }
                else if (row.IndentLevel > 0)
                {
                    label.enableAutoSizing = false;
                    label.fontSize = SulfurOptionsSettingRows.CompactBadgeFontSize(label.fontSize);
                }
            }

            ApplyBadgeRowVisibility();
        }

        private void RemoveStatusBadge()
        {
            if (statusBadge == null)
                return;

            // Out of the row (and its layout) now; the object goes at the end of the frame.
            statusBadge.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(statusBadge);
            statusBadge = null;

            ApplyBadgeRowVisibility();
        }

        // A badge row without badges takes no space.
        private void ApplyBadgeRowVisibility()
        {
            if (badgeRow != null)
                badgeRow.SetActive(rowActive && badgeRow.transform.childCount > 0);
        }

        /// <summary>
        /// Shows <paramref name="text"/> on the row's message line, styled for
        /// <paramref name="kind"/>. An empty (or whitespace) text hides the line; a non-empty
        /// one shows it again. The line exists for every row built with an <c>Ex</c> method,
        /// also when the row was built without a message.
        /// </summary>
        public void SetMessage(string text, SulfurMessageKind kind)
        {
            row.Message = text ?? "";
            row.MessageKind = kind;

            if (messageText == null)
                return;

            bool empty = string.IsNullOrWhiteSpace(text);

            // Rows found by name (no build-time parts) keep the original behaviour: always shown.
            if (messageRow == null)
                messageText.text = SulfurUiText.GetMessagePrefix(kind) + (text ?? "");
            else
                messageText.text = empty ? "" : SulfurUiText.GetMessagePrefix(kind) + text;

            if (messageBackground != null)
                messageBackground.color = GetMessageBackground(kind);

            if (messageRow != null)
            {
                messageVisible = !empty;
                messageRow.SetActive(rowActive && messageVisible);
            }

            // A longer or shorter message changes the line count of a wrapping message line.
            SulfurRowHeightFitter.Refit(messageText);
        }

        /// <summary>
        /// Shows or hides every row of the setting block. Showing it again keeps an empty
        /// message line and a Default button hidden with <see cref="SetDefaultButtonVisible"/>
        /// hidden.
        /// </summary>
        public void SetActive(bool active)
        {
            rowActive = active;

            foreach (Transform child in createdChildren)
            {
                if (child == null)
                    continue;

                GameObject go = child.gameObject;
                bool wanted = active;

                if (messageRow != null && go == messageRow)
                    wanted = active && messageVisible;
                else if (badgeRow != null && go == badgeRow)
                    wanted = active && badgeRow.transform.childCount > 0;
                else if (defaultButtonRow != null && go == defaultButtonRow)
                    wanted = active && defaultButtonVisible;

                go.SetActive(wanted);
            }
        }

        /// <summary>
        /// Shows or hides the row's Default button. The button exists when the row was built
        /// with an <see cref="SulfurSettingRow.OnDefault"/> action, whether or not
        /// <see cref="SulfurSettingRow.ShowDefaultButton"/> was true; returns false when it
        /// does not exist (no OnDefault) or is gone. A hidden button is no navigation stop.
        /// </summary>
        public bool SetDefaultButtonVisible(bool visible)
        {
            row.ShowDefaultButton = visible;

            if (defaultButtonRow == null)
                return false;

            defaultButtonVisible = visible;
            defaultButtonRow.SetActive(rowActive && visible);
            return true;
        }

        /// <summary>
        /// Toggle rows: shows <paramref name="value"/> without calling the row's onChanged
        /// (also while the row is locked). False on another kind of row or when the row is gone.
        /// </summary>
        public bool SetValueWithoutNotify(bool value)
        {
            if (parts.Kind != SulfurSettingControlKind.Toggle)
                return false;

            if (parts.Binding != null && parts.Binding.SetToggleValueWithoutNotify(value))
                return true;

            if (parts.Toggle == null)
                return false;

            parts.Toggle.SetIsOnWithoutNotify(value);
            return true;
        }

        /// <summary>
        /// Number rows: shows <paramref name="value"/> formatted with the row's decimals
        /// (not clamped, like the build value), without calling onChanged, and makes it the
        /// value unparseable input reverts to. A double row takes the float as is. False on
        /// another kind of row or when the row is gone.
        /// </summary>
        public bool SetValueWithoutNotify(float value)
        {
            return SetNumber(value);
        }

        /// <summary>
        /// Number rows: as <see cref="SetValueWithoutNotify(float)"/>. A float row stores the
        /// value as float (about 7 significant digits); a double row exactly.
        /// </summary>
        public bool SetValueWithoutNotify(double value)
        {
            return SetNumber(value);
        }

        /// <summary>
        /// Text rows: shows <paramref name="value"/> (null as "") without calling onChanged.
        /// False on another kind of row or when the row is gone.
        /// </summary>
        public bool SetValueWithoutNotify(string value)
        {
            if (parts.Kind != SulfurSettingControlKind.Text || parts.TextInput == null)
                return false;

            parts.TextInput.SetTextWithoutNotify(value ?? "");
            return true;
        }

        /// <summary>
        /// Cycle rows: shows the value at <paramref name="index"/> (clamped) without calling
        /// onChanged. Named apart from <c>SetValueWithoutNotify</c> so an integer literal meant
        /// for a number row cannot pick it. False on another kind of row or when the row is gone.
        /// </summary>
        public bool SetIndexWithoutNotify(int index)
        {
            if (parts.Kind != SulfurSettingControlKind.Cycle || parts.Binding == null)
                return false;

            parts.Binding.SetCycleIndexWithoutNotify(index);
            return true;
        }

        private bool SetNumber(double value)
        {
            if (parts.Kind != SulfurSettingControlKind.Number || parts.Number == null)
                return false;

            return parts.Number.SetValueWithoutNotify(value);
        }

        private static TextMeshProUGUI FirstText(GameObject root)
        {
            if (root == null)
                return null;

            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            return texts != null && texts.Length > 0 ? texts[0] : null;
        }

        private TextMeshProUGUI FindFirstBadgeText()
        {
            foreach (Transform child in createdChildren)
            {
                if (child == null || child.name != "SULFUR_BadgeRow")
                    continue;

                TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts != null && texts.Length > 0)
                    return texts[0];
            }

            return null;
        }

        private TextMeshProUGUI FindMessageText()
        {
            foreach (Transform child in createdChildren)
            {
                if (child == null || !child.name.StartsWith("SULFUR_Message_"))
                    continue;

                TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts != null && texts.Length > 0)
                    return texts[0];
            }

            return null;
        }

        private static Color GetMessageBackground(SulfurMessageKind kind)
        {
            switch (kind)
            {
                case SulfurMessageKind.Warning:
                    return new Color(1f, 0.72f, 0.18f, 0.12f);
                case SulfurMessageKind.Error:
                    return new Color(1f, 0.18f, 0.18f, 0.12f);
                case SulfurMessageKind.Success:
                    return new Color(0.28f, 1f, 0.46f, 0.10f);
                default:
                    return new Color(1f, 1f, 1f, 0.06f);
            }
        }
    }
}
