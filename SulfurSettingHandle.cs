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
    public sealed class SulfurSettingHandle
    {
        private readonly List<Transform> createdChildren = new List<Transform>();
        private readonly SulfurSettingRow row;
        private readonly TextMeshProUGUI statusBadgeText;
        private readonly TextMeshProUGUI messageText;
        private readonly Image messageBackground;

        internal SulfurSettingHandle(SulfurSettingRow row, IEnumerable<Transform> children)
        {
            this.row = row ?? new SulfurSettingRow();

            if (children != null)
            {
                foreach (Transform child in children)
                {
                    if (child != null)
                        createdChildren.Add(child);
                }
            }

            statusBadgeText = FindFirstBadgeText();
            messageText = FindMessageText();

            if (messageText != null)
                messageBackground = messageText.GetComponentInParent<Image>();
        }

        public void SetDirty(bool dirty)
        {
            SetDirty(dirty, row.DirtyText, row.CleanText);
        }

        public void SetDirty(bool dirty, string dirtyText, string cleanText)
        {
            row.IsDirty = dirty;

            if (statusBadgeText == null)
                return;

            string text = dirty ? dirtyText : cleanText;

            if (string.IsNullOrWhiteSpace(text))
                text = dirty ? "Dirty" : "Clean";

            statusBadgeText.text = text;
        }

        public void SetMessage(string text, SulfurMessageKind kind)
        {
            row.Message = text ?? "";
            row.MessageKind = kind;

            if (messageText == null)
                return;

            messageText.text = GetMessagePrefix(kind) + (text ?? "");

            if (messageBackground != null)
                messageBackground.color = GetMessageBackground(kind);
        }

        public void SetActive(bool active)
        {
            foreach (Transform child in createdChildren)
            {
                if (child != null)
                    child.gameObject.SetActive(active);
            }
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

        private static string GetMessagePrefix(SulfurMessageKind kind)
        {
            switch (kind)
            {
                case SulfurMessageKind.Warning:
                    return "WARNING: ";
                case SulfurMessageKind.Error:
                    return "ERROR: ";
                case SulfurMessageKind.Success:
                    return "OK: ";
                default:
                    return "";
            }
        }
    }
}
