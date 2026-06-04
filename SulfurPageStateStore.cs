using System;
using System.Collections.Generic;

namespace Ryuka.Sulfur.NativeUI
{
    internal static class SulfurPageStateStore
    {
        private static readonly Dictionary<string, object> values =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static T Get<T>(string pageId, string key, T defaultValue)
        {
            string fullKey = BuildKey(pageId, key);

            object value;
            if (!values.TryGetValue(fullKey, out value))
                return defaultValue;

            if (value is T)
                return (T)value;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void Set<T>(string pageId, string key, T value)
        {
            values[BuildKey(pageId, key)] = value;
        }

        public static bool Has(string pageId, string key)
        {
            return values.ContainsKey(BuildKey(pageId, key));
        }

        public static void Remove(string pageId, string key)
        {
            values.Remove(BuildKey(pageId, key));
        }

        public static void ClearPage(string pageId)
        {
            string prefix = Normalize(pageId) + "::";

            List<string> keysToRemove = new List<string>();

            foreach (string key in values.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    keysToRemove.Add(key);
            }

            foreach (string key in keysToRemove)
                values.Remove(key);
        }

        private static string BuildKey(string pageId, string key)
        {
            return Normalize(pageId) + "::" + Normalize(key);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "_global" : value.Trim();
        }
    }
}
