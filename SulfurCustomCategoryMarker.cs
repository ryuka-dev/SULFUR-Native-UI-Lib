using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurCustomCategoryMarker : MonoBehaviour
    {
        public string PageId;

        private TextMeshProUGUI[] labels;
        private string lastAppliedText;
        private int lastLanguageVersion = -1;

        public void Initialize(string pageId)
        {
            PageId = pageId;
            CacheLabels();
            ForceApplyLabel();
        }

        private void OnEnable()
        {
            CacheLabels();
            ApplyLabelIfNeeded(true);
        }

        private void LateUpdate()
        {
            ApplyLabelIfNeeded(false);
        }

        public void ApplyLabel()
        {
            ForceApplyLabel();
        }

        public void ForceApplyLabel()
        {
            if (string.IsNullOrWhiteSpace(PageId))
                return;

            SulfurOptionsPage page = SulfurOptionsApi.GetPage(PageId);
            if (page == null)
                return;

            string text = page.ResolvedDisplayName;

            if (string.IsNullOrWhiteSpace(text))
                return;

            lastAppliedText = text;
            lastLanguageVersion = SulfurLocalization.LanguageVersion;

            CacheLabels();

            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null)
                    label.text = text;
            }
        }

        private void ApplyLabelIfNeeded(bool force)
        {
            if (string.IsNullOrWhiteSpace(PageId))
                return;

            SulfurLocalization.RefreshCurrentLanguage(false);

            if (!force && lastLanguageVersion == SulfurLocalization.LanguageVersion)
            {
                RestoreLabelIfOverwritten();
                return;
            }

            ForceApplyLabel();
        }

        private void RestoreLabelIfOverwritten()
        {
            if (string.IsNullOrWhiteSpace(lastAppliedText))
                return;

            CacheLabels();

            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null && label.text != lastAppliedText)
                    label.text = lastAppliedText;
            }
        }

        private void CacheLabels()
        {
            labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        }
    }
}
