using System;

namespace Ryuka.Sulfur.NativeUI
{
    public sealed class SulfurOptionsPage
    {
        public string PageId;
        public string DisplayName;
        public int SortOrder = 1000;

        public Func<string> GetDisplayName;
        public Action<SulfurOptionsContext> BuildPage;

        internal string ResolvedDisplayName
        {
            get
            {
                if (GetDisplayName != null)
                {
                    string value = GetDisplayName();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                return string.IsNullOrWhiteSpace(DisplayName) ? PageId : DisplayName;
            }
        }
    }
}