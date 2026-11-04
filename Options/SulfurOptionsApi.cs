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

            SulfurOptionsPage existing = GetPage(page.PageId);
            if (existing != null && !ReferenceEquals(existing, page) && Plugin.Log != null)
            {
                // Page ids are global across mods; a second registration silently taking over
                // another mod's page is almost always an id collision.
                Plugin.Log.LogWarning(
                    "SULFUR native options page id '" + page.PageId + "' is already registered by " + Describe(existing) +
                    "; replacing it with the page from " + Describe(page) + ".");
            }

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

        // "'Display name' (Assembly: Type.Method)" of the page's BuildPage, for the
        // duplicate-id warning. Best effort; never throws.
        private static string Describe(SulfurOptionsPage page)
        {
            string name = "'" + (page.DisplayName ?? page.PageId) + "'";

            try
            {
                System.Reflection.MethodInfo method = page.BuildPage != null ? page.BuildPage.Method : null;
                if (method == null || method.DeclaringType == null)
                    return name;

                Type type = method.DeclaringType;
                return name + " (" + type.Assembly.GetName().Name + ": " + type.FullName + "." + method.Name + ")";
            }
            catch
            {
                return name;
            }
        }

        internal static SulfurOptionsPage GetPage(string pageId)
        {
            return pages.FirstOrDefault(x => string.Equals(x.PageId, pageId, StringComparison.OrdinalIgnoreCase));
        }
    }
}