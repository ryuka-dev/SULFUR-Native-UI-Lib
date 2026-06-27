using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurOptionsApi
    {
        private static readonly List<SulfurOptionsPage> pages = new List<SulfurOptionsPage>();

        public static IReadOnlyList<SulfurOptionsPage> Pages
        {
            get { return pages; }
        }

        public static void RegisterPage(SulfurOptionsPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            if (string.IsNullOrWhiteSpace(page.PageId))
                throw new ArgumentException("PageId is required.");

            if (page.BuildPage == null)
                throw new ArgumentException("BuildPage is required.");

            pages.RemoveAll(x => string.Equals(x.PageId, page.PageId, StringComparison.OrdinalIgnoreCase));
            pages.Add(page);

            pages.Sort((a, b) =>
            {
                int r = a.SortOrder.CompareTo(b.SortOrder);
                if (r != 0)
                    return r;

                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            if (Plugin.Log != null)
                Plugin.Log.LogInfo("Registered SULFUR native options page: " + page.PageId);
        }

        public static void UnregisterPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
                return;

            pages.RemoveAll(x => string.Equals(x.PageId, pageId, StringComparison.OrdinalIgnoreCase));
        }

        internal static SulfurOptionsPage GetPage(string pageId)
        {
            return pages.FirstOrDefault(x => string.Equals(x.PageId, pageId, StringComparison.OrdinalIgnoreCase));
        }
    }
}