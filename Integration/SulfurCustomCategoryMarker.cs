using System;
using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Marks an options-screen category button as one of the library's custom pages and keeps
    /// its label showing the page's display name.
    ///
    /// Event-driven, no per-frame work. The game rewrites a category label only from
    /// <c>OptionsScreenCategory.UpdateLabel</c> (called by <c>SetToCategory</c> and by the game's
    /// language-change handler); a postfix on it calls <see cref="ForceApplyLabel"/>. The page
    /// name itself may be localized by the owning mod, so it is re-resolved on
    /// <see cref="SulfurLanguage.Changed"/>, which runs after the game's own handlers.
    /// </summary>
    internal sealed class SulfurCustomCategoryMarker : MonoBehaviour
    {
        public string PageId;

        private TextMeshProUGUI[] labels;
        private Action<string> onLanguageChanged;

        public void Initialize(string pageId)
        {
            PageId = pageId;
            CacheLabels();
            ForceApplyLabel();
        }

        private void OnEnable()
        {
            if (onLanguageChanged == null)
                onLanguageChanged = OnLanguageChanged;

            SulfurLanguage.Changed += onLanguageChanged;

            // The language may have changed while this button was inactive.
            ForceApplyLabel();
        }

        private void OnDisable()
        {
            if (onLanguageChanged != null)
                SulfurLanguage.Changed -= onLanguageChanged;
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

            if (labels == null)
                CacheLabels();

            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null && label.text != text)
                    label.text = text;
            }
        }

        private void OnLanguageChanged(string languageCode)
        {
            ForceApplyLabel();

            // Normally the page is not open: the language selector is on the General page. But
            // the game commits after loading the language's fonts, so the player can reach a
            // custom page first, and that page's text and fonts are then the old language's.
            SulfurOptionsScreenBridge.RebuildCustomPageIfCurrent(PageId);
        }

        private void CacheLabels()
        {
            labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        }
    }
}
