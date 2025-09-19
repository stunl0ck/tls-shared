using System;
using System.Collections.Generic;
using System.IO;
using TPLib.Localization;

namespace Stunl0ck.TLS.Shared
{
    /// <summary>
    /// CSV->Localizer merger for TLS ecosystem.
    /// Call from loaders (MCM, ModKit) after the game's languages are loaded.
    /// </summary>
    public static class Localization
    {
        // Optional logger delegates (wire BepInEx logger or custom)
        public sealed class Logger
        {
            public Action<string> Info  { get; }
            public Action<string> Warn  { get; }
            public Action<string> Error { get; }

            public Logger(Action<string> info = null, Action<string> warn = null, Action<string> error = null)
            {
                Info  = info  ?? (_ => { });
                Warn  = warn  ?? (_ => { });
                Error = error ?? (_ => { });
            }
        }

        private static readonly HashSet<string> _mergedPaths = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();

        /// <summary>
        /// Scan each plugin folder under <paramref name="pluginsRoot"/> and merge a CSV at <paramref name="relativeCsvPath"/>.
        /// Example: MergeCsvsUnder(Paths.PluginPath, "MCM/languages.csv") or "ModKit/languages.csv"
        /// </summary>
        public static int MergeCsvsUnder(string pluginsRoot, string relativeCsvPath, Logger log = null)
        {
            log ??= new Logger();
            if (string.IsNullOrWhiteSpace(pluginsRoot) || !Directory.Exists(pluginsRoot))
            {
                log.Error?.Invoke($"[TLS.Shared] pluginsRoot does not exist: {pluginsRoot}");
                return 0;
            }

            int totalMerged = 0;
            foreach (var dir in Directory.GetDirectories(pluginsRoot))
            {
                var csv = Path.Combine(dir, relativeCsvPath);
                if (File.Exists(csv))
                {
                    var label = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    totalMerged += MergeCsv(csv, label, log);
                }
            }

            if (totalMerged > 0)
                log.Info?.Invoke($"[TLS.Shared] Merged translations from {totalMerged} file(s) at '{relativeCsvPath}'.");
            return totalMerged;
        }

        /// <summary>
        /// Merge a single CSV file into TPLib's Localizer dictionary.
        /// Header must be: Key;English;Français;...
        /// </summary>
        public static int MergeCsv(string csvPath, string label = null, Logger log = null)
        {
            log ??= new Logger();

            string fullPath;
            try { fullPath = Path.GetFullPath(csvPath); }
            catch { fullPath = csvPath; }

            lock (_lock)
            {
                if (!_mergedPaths.Add(fullPath))
                    return 0; // already processed
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(csvPath);
            }
            catch (Exception ex)
            {
                log.Error?.Invoke($"[TLS.Shared] Failed to read {csvPath}: {ex}");
                return 0;
            }

            if (lines.Length <= 1)
            {
                log.Warn?.Invoke($"[TLS.Shared] {label ?? csvPath} has no rows.");
                return 0;
            }

            var header = lines[0].Split(';');                // Key;English;Français;...
            var langs  = Localizer.knownLanguages;           // authoritative order
            var dict   = Localizer.dictionary;               // key -> per-language array

            int touchedKeys = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line[0] == '#' || (line.Length > 1 && line[0] == '/' && line[1] == '/')) continue; // comment

                var cols = line.Split(';');
                if (cols.Length == 0) continue;

                var key = cols[0]?.Trim();
                if (string.IsNullOrEmpty(key)) continue;

                if (!dict.TryGetValue(key, out var translations) || translations == null || translations.Length != langs.Length)
                {
                    translations = new string[langs.Length];
                    dict[key] = translations;
                }

                // Align CSV header languages to Localizer.knownLanguages order
                for (int c = 1; c < header.Length && c < cols.Length; c++)
                {
                    var headerLang = header[c]?.Trim();
                    if (string.IsNullOrEmpty(headerLang)) continue;

                    int langIndex = Array.IndexOf(langs, headerLang);
                    if (langIndex < 0 || langIndex >= translations.Length) continue;

                    var value = cols[c];
                    if (!string.IsNullOrEmpty(value))
                        translations[langIndex] = value;
                }

                touchedKeys++;
            }

            log.Info?.Invoke($"[TLS.Shared] Injected/updated {touchedKeys} keys from {(label ?? Path.GetFileName(csvPath))}.");
            return touchedKeys;
        }

        /// <summary>
        /// Lookup helper: returns localized text for a key; falls back to English, then to <paramref name="fallback"/>.
        /// </summary>
        public static string LocalizeOrDefault(string key, string fallback = "")
        {
            var dict  = Localizer.dictionary;
            var langs = Localizer.knownLanguages;
            var cur   = Localizer.language;

            int idx = Array.IndexOf(langs, cur);
            if (idx < 0) idx = 0;

            if (dict.TryGetValue(key, out var arr) && arr != null)
            {
                if (idx < arr.Length && !string.IsNullOrEmpty(arr[idx])) return arr[idx];

                int enIdx = Array.IndexOf(langs, "English");
                if (enIdx >= 0 && enIdx < arr.Length && !string.IsNullOrEmpty(arr[enIdx])) return arr[enIdx];
            }
            return fallback ?? string.Empty;
        }

        /// <summary>
        /// MCM-style helper: build loc key as modId_optionKey_field.
        /// </summary>
        public static string LocalizeOrDefault(string modId, string optionKey, string field, string fallback)
        {
            var locKey = $"{modId}_{optionKey}_{field}";
            return LocalizeOrDefault(locKey, fallback);
        }

        /// <summary>
        /// MCM-style helper: build loc key as modId_field.
        /// </summary>
        public static string LocalizeOrDefaultForModDescription(string modId, string fallback)
        {
            var locKey = $"{modId}_description";
            return LocalizeOrDefault(locKey, fallback);
        }
    }
}
