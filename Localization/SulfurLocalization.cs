using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurLocalization
    {
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> data
            = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        // Owned by SulfurLanguage: updated from OnLanguageChanged, never polled.
        private static string cachedLanguageCode = "en";

        // Lookup order for Get: exact code -> related language (zh-TW -> zh-CN) ->
        // language without region -> en.
        // Rebuilt on language change so Get does not allocate.
        private static string[] languageFallbacks = BuildLanguageFallbacks("en");

        /// <summary>
        /// Incremented every time <see cref="SulfurLanguage.Changed"/> is raised (first
        /// detection included). Compare against a stored value to know when to re-resolve text.
        /// </summary>
        public static int LanguageVersion { get; private set; }

        /// <summary>
        /// The current language code used for lookups (<see cref="SulfurLanguage.Code"/>,
        /// normalized). <c>"en"</c> until the game's language is known.
        /// </summary>
        public static string CurrentLanguageCode
        {
            get { return cachedLanguageCode; }
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

            Dictionary<string, Dictionary<string, string>> pluginMap;

            if (!data.TryGetValue(pluginGuid, out pluginMap))
            {
                pluginMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                data[pluginGuid] = pluginMap;
            }

            HashSet<string> loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string directory in GetLocalizationSearchDirectories(pluginDir))
            {
                LoadLocalizationDirectory(pluginGuid, pluginMap, directory, loadedFiles);
            }
        }

        public static string Get(string pluginGuid, string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(pluginGuid) || string.IsNullOrWhiteSpace(key))
                return fallback ?? "";

            Dictionary<string, Dictionary<string, string>> pluginMap;

            if (!data.TryGetValue(pluginGuid, out pluginMap))
                return fallback ?? "";

            string[] fallbacks = languageFallbacks;

            for (int i = 0; i < fallbacks.Length; i++)
            {
                Dictionary<string, string> langMap;

                if (!pluginMap.TryGetValue(fallbacks[i], out langMap))
                    continue;

                string value;

                if (langMap.TryGetValue(key, out value))
                    return value ?? "";
            }

            return fallback ?? "";
        }

        /// <summary>
        /// Kept for source compatibility. The language is no longer polled: it is tracked by
        /// <see cref="SulfurLanguage"/>, which updates <see cref="CurrentLanguageCode"/> and
        /// <see cref="LanguageVersion"/> when the game's language becomes known or changes.
        /// This only lets a caller that runs before the library's first frame pick up an
        /// already-known startup language; it is cheap and <paramref name="force"/> is ignored.
        /// </summary>
        public static void RefreshCurrentLanguage(bool force)
        {
            SulfurLanguage.ConvergeInitial();
        }

        /// <summary>Called by SulfurLanguage before it raises Changed.</summary>
        internal static void OnLanguageChanged(string languageCode)
        {
            string code = NormalizeLanguageCode(languageCode);

            if (string.IsNullOrWhiteSpace(code))
                code = "en";

            cachedLanguageCode = code;
            languageFallbacks = BuildLanguageFallbacks(code);
            LanguageVersion++;
        }

        private static IEnumerable<string> GetLocalizationSearchDirectories(string pluginDir)
        {
            if (string.IsNullOrWhiteSpace(pluginDir))
                yield break;

            // Preferred structure:
            // Plugin.dll
            // lang/en.json
            string langDir = Path.Combine(pluginDir, "lang");

            if (Directory.Exists(langDir))
                yield return langDir;

            // Flattened structure:
            // Plugin.dll
            // en.json
            // zh-CN.json
            //
            // This is needed for SULFUR Config itself and some mod-manager packages.
            if (Directory.Exists(pluginDir))
                yield return pluginDir;

            // Extra compatibility:
            // Some managers may put DLL in one folder but keep lang next to the package folder.
            DirectoryInfo parent = Directory.GetParent(pluginDir);

            if (parent != null)
            {
                string parentLangDir = Path.Combine(parent.FullName, "lang");

                if (Directory.Exists(parentLangDir))
                    yield return parentLangDir;
            }
        }

        private static void LoadLocalizationDirectory(
            string pluginGuid,
            Dictionary<string, Dictionary<string, string>> pluginMap,
            string directory,
            HashSet<string> loadedFiles)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;

            string[] files;

            try
            {
                files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Failed to list localization directory: " + directory + " / " + ex.Message);

                return;
            }

            foreach (string file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                if (loadedFiles.Contains(file))
                    continue;

                loadedFiles.Add(file);

                string langCode = Path.GetFileNameWithoutExtension(file);

                if (!IsLikelyLanguageFileName(langCode))
                    continue;

                try
                {
                    Dictionary<string, string> langMap = LoadLangFile(file);

                    if (langMap.Count == 0)
                    {
                        if (Plugin.Log != null)
                            Plugin.Log.LogWarning("Localization file has 0 entries: " + file);

                        continue;
                    }

                    MergeLangMap(pluginMap, langCode, langMap);

                    if (Plugin.Log != null)
                    {
                        Plugin.Log.LogInfo(
                            "Loaded localization: " +
                            pluginGuid + " / " +
                            langCode + " / " +
                            langMap.Count + " entries / " +
                            file);
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogWarning("Failed to load localization file: " + file + " / " + ex.Message);
                }
            }
        }

        private static void MergeLangMap(
            Dictionary<string, Dictionary<string, string>> pluginMap,
            string langCode,
            Dictionary<string, string> newMap)
        {
            if (pluginMap == null || string.IsNullOrWhiteSpace(langCode) || newMap == null || newMap.Count == 0)
                return;

            Dictionary<string, string> existingMap;

            if (!pluginMap.TryGetValue(langCode, out existingMap))
            {
                pluginMap[langCode] = new Dictionary<string, string>(newMap, StringComparer.OrdinalIgnoreCase);
                return;
            }

            // Search order is:
            // 1. lang/
            // 2. plugin root
            // 3. parent/lang/
            //
            // If an earlier directory already provided a key, later fallback directories should not override it.
            foreach (KeyValuePair<string, string> pair in newMap)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                if (!existingMap.ContainsKey(pair.Key))
                    existingMap[pair.Key] = pair.Value ?? "";
            }
        }

        private static bool IsLikelyLanguageFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string value = name.Trim();

            if (value.Equals("manifest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (value.Equals("package", StringComparison.OrdinalIgnoreCase))
                return false;

            if (value.Equals("config", StringComparison.OrdinalIgnoreCase))
                return false;

            if (value.Equals("localization_manifest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (value.Length < 2 || value.Length > 32)
                return false;

            string normalized = value.Replace('_', '-');
            string[] parts = normalized.Split('-');

            if (parts.Length == 0)
                return false;

            if (!IsAlpha(parts[0]))
                return false;

            if (parts[0].Length < 2 || parts[0].Length > 3)
                return false;

            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                    return false;

                if (parts[i].Length < 2 || parts[i].Length > 8)
                    return false;

                if (!IsAlphaNumeric(parts[i]))
                    return false;
            }

            return true;
        }

        private static bool IsAlpha(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsLetter(value[i]))
                    return false;
            }

            return true;
        }

        private static bool IsAlphaNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]))
                    return false;
            }

            return true;
        }

        private static Dictionary<string, string> LoadLangFile(string file)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string json = File.ReadAllText(file, Encoding.UTF8);
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(SulfurLangFile));

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

        private static string[] BuildLanguageFallbacks(string current)
        {
            List<string> result = new List<string>(4);

            if (!string.IsNullOrWhiteSpace(current))
            {
                result.Add(current);

                // Readers of Traditional Chinese are better served by Simplified Chinese
                // than by English, and mods rarely ship a zh-TW file yet.
                if (current == "zh-TW")
                    result.Add("zh-CN");

                int dash = current.IndexOf('-');

                if (dash > 0)
                    result.Add(current.Substring(0, dash));
            }

            if (!result.Contains("en"))
                result.Add("en");

            return result.ToArray();
        }

        private static string NormalizeLanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "";

            string value = code.Trim().Replace('_', '-');

            if (value.Equals("zh-cn", StringComparison.OrdinalIgnoreCase))
                return "zh-CN";

            if (value.Equals("zh-tw", StringComparison.OrdinalIgnoreCase))
                return "zh-TW";

            if (value.Equals("pt-br", StringComparison.OrdinalIgnoreCase))
                return "pt-BR";

            if (value.Equals("es-es", StringComparison.OrdinalIgnoreCase))
                return "es-ES";

            return value.ToLowerInvariant();
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
