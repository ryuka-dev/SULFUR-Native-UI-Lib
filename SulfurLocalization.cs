using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurLocalization
    {
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> data
            = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        private static string cachedRawLanguage;
        private static string cachedLanguageCode = "en";
        private static DateTime lastLanguageCheckUtc = DateTime.MinValue;

        public static int LanguageVersion { get; private set; }

        public static string CurrentLanguageCode
        {
            get
            {
                RefreshCurrentLanguage(false);
                return cachedLanguageCode;
            }
        }

        public static void LoadPluginLocalization(string pluginGuid, string pluginAssemblyLocation)
        {
            if (string.IsNullOrWhiteSpace(pluginGuid) || string.IsNullOrWhiteSpace(pluginAssemblyLocation))
                return;

            string pluginDir = File.Exists(pluginAssemblyLocation)
                ? Path.GetDirectoryName(pluginAssemblyLocation)
                : pluginAssemblyLocation;

            if (string.IsNullOrWhiteSpace(pluginDir))
                return;

            string langDir = Path.Combine(pluginDir, "lang");

            if (!Directory.Exists(langDir))
                return;

            string[] files = Directory.GetFiles(langDir, "*.json", SearchOption.TopDirectoryOnly);

            Dictionary<string, Dictionary<string, string>> pluginMap;
            if (!data.TryGetValue(pluginGuid, out pluginMap))
            {
                pluginMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                data[pluginGuid] = pluginMap;
            }

            foreach (string file in files)
            {
                try
                {
                    string langCode = Path.GetFileNameWithoutExtension(file);
                    Dictionary<string, string> langMap = LoadLangFile(file);

                    if (langMap.Count == 0)
                    {
                        if (Plugin.Log != null)
                            Plugin.Log.LogWarning("Localization file has 0 entries: " + file);

                        continue;
                    }

                    pluginMap[langCode] = langMap;

                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("Loaded localization: " + pluginGuid + " / " + langCode + " / " + langMap.Count + " entries");
                }
                catch (Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogWarning("Failed to load localization file: " + file + " / " + ex.Message);
                }
            }
        }

        public static string Get(string pluginGuid, string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(pluginGuid) || string.IsNullOrWhiteSpace(key))
                return fallback ?? "";

            Dictionary<string, Dictionary<string, string>> pluginMap;
            if (!data.TryGetValue(pluginGuid, out pluginMap))
                return fallback ?? "";

            foreach (string langCode in GetLanguageFallbacks())
            {
                Dictionary<string, string> langMap;
                if (!pluginMap.TryGetValue(langCode, out langMap))
                    continue;

                string value;
                if (langMap.TryGetValue(key, out value))
                    return value ?? "";
            }

            return fallback ?? "";
        }

        public static void RefreshCurrentLanguage(bool force)
        {
            DateTime now = DateTime.UtcNow;

            if (!force && (now - lastLanguageCheckUtc).TotalSeconds < 0.5)
                return;

            lastLanguageCheckUtc = now;

            string raw = GetCurrentGameLanguageName();
            string code = MapGameLanguageToCode(raw);

            if (string.Equals(raw, cachedRawLanguage, StringComparison.Ordinal) &&
                string.Equals(code, cachedLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            cachedRawLanguage = raw;
            cachedLanguageCode = code;
            LanguageVersion++;
        }

        private static Dictionary<string, string> LoadLangFile(string file)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string json = File.ReadAllText(file, Encoding.UTF8);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SulfurLangFile));

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                SulfurLangFile langFile = serializer.ReadObject(stream) as SulfurLangFile;

                if (langFile == null || langFile.Entries == null)
                    return result;

                foreach (SulfurLangEntry entry in langFile.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                        continue;

                    result[entry.Key] = entry.Value ?? "";
                }
            }

            return result;
        }

        private static IEnumerable<string> GetLanguageFallbacks()
        {
            string current = CurrentLanguageCode;

            if (!string.IsNullOrWhiteSpace(current))
            {
                yield return current;

                int dash = current.IndexOf('-');
                if (dash > 0)
                    yield return current.Substring(0, dash);
            }

            yield return "en";
        }

        private static string GetCurrentGameLanguageName()
        {
            try
            {
                Type localizationManagerType = FindTypeByFullName("I2.Loc.LocalizationManager");
                if (localizationManagerType == null)
                    return "";

                PropertyInfo prop = localizationManagerType.GetProperty(
                    "CurrentLanguage",
                    BindingFlags.Public | BindingFlags.Static);

                if (prop == null)
                    return "";

                object value = prop.GetValue(null, null);
                return value != null ? value.ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        private static string MapGameLanguageToCode(string gameLanguage)
        {
            if (string.IsNullOrWhiteSpace(gameLanguage))
                return "en";

            string value = gameLanguage.Trim();

            if (value.Equals("Chinese (Simplified)", StringComparison.OrdinalIgnoreCase))
                return "zh-CN";

            if (value.Equals("Chinese (Traditional)", StringComparison.OrdinalIgnoreCase))
                return "zh-TW";

            if (value.Equals("Japanese", StringComparison.OrdinalIgnoreCase))
                return "ja";

            if (value.Equals("English", StringComparison.OrdinalIgnoreCase))
                return "en";

            if (value.Contains("简体") || value.Contains("简体中文") || value.Contains("简中"))
                return "zh-CN";

            if (value.Contains("繁體") || value.Contains("繁体") || value.Contains("繁中"))
                return "zh-TW";

            if (value.Contains("日本語") || value.Contains("日本"))
                return "ja";

            if (value.IndexOf("Simplified", StringComparison.OrdinalIgnoreCase) >= 0)
                return "zh-CN";

            if (value.IndexOf("Traditional", StringComparison.OrdinalIgnoreCase) >= 0)
                return "zh-TW";

            if (value.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0)
                return "zh-CN";

            if (value.IndexOf("Japanese", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ja";

            return "en";
        }

        private static Type FindTypeByFullName(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        [DataContract]
        private sealed class SulfurLangFile
        {
            [DataMember(Name = "entries")]
            public SulfurLangEntry[] Entries;
        }

        [DataContract]
        private sealed class SulfurLangEntry
        {
            [DataMember(Name = "key")]
            public string Key;

            [DataMember(Name = "value")]
            public string Value;
        }
    }
}
