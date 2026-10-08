using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using _Project.Core.Localization;
using UnityEditor;

namespace _Project.Editor.Building
{
    public class BuildingLocalizationImporter
    {
        private const int EnglishIndex = 0;
        private const int RussianIndex = 10;

        private static readonly Regex TaskTitleTerm = new Regex(
            @"- Term: TaskTitle/task_(\d+)\r?\n\s*TermType: \d+\r?\n\s*Languages:\r?\n((?:\s*- .*\r?\n)+)",
            RegexOptions.Compiled);

        private readonly BuildingImportReport _report;

        public BuildingLocalizationImporter(BuildingImportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        private static readonly string[][] StaticEntries =
        {
            new[] { "building.tasks.day", "День {0}", "Day {0}" },
            new[] { "building.tasks.title", "ЗАДАЧА", "TASK" },
            new[] { "building.tasks.day_word", "День", "Day" },
            new[] { "building.button.start", "Начать", "Start" },
            new[] { "building.tasks.empty", "Все задачи выполнены!", "All tasks are done!" },
            new[] { "building.tasks.locked", "Закрыто", "Locked" },
            new[] { "building.button.build", "Построить", "Build" },
            new[] { "building.theme.title", "Выбери оформление", "Choose a style" },
            new[] { "building.gems.title", "Не хватает кристаллов", "Out of gems" },
            new[] { "building.gems.text", "Проходи уровни, чтобы заработать кристаллы", "Complete levels to earn gems" },
            new[] { "building.button.play", "Играть", "Play" },
            new[] { "building.newday.title", "День {0}", "Day {0}" },
            new[] { "building.newday.completed", "Зона построена!", "Area completed!" },
            new[] { "building.newday.reward", "Награда: {0}", "Reward: {0}" },
            new[] { "building.reward.gold", "{0} золота", "{0} gold" },
            new[] { "building.reward.hammer", "молот ×{0}", "hammer ×{0}" },
            new[] { "building.reward.swap", "обмен ×{0}", "swap ×{0}" },
            new[] { "building.reward.replace", "замена ×{0}", "replace ×{0}" },
            new[] { "building.reward.freeze", "заморозка ×{0}", "freeze ×{0}" },
            new[] { "building.coming_soon.title", "Скоро будет", "Coming Soon" },
            new[] { "building.coming_soon.text", "Новые территории уже в пути!\nЖди свежих обновлений!", "More areas are on their way!\nStay tuned for new expansions!" },
            new[] { "nav.building", "Стройка", "Build" },
            new[] { "home.building", "СТРОЙКА", "BUILD" },
            new[] { "home.building.locked", "Разблок. после {0} уровня", "Unlocks after level {0}" },
            new[] { "win.gems", "+{0}", "+{0}" }
        };

        public void ImportStaticKeys()
        {
            LocalizationConfig config = LoadConfig();
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty entries = serialized.FindProperty("_entries");
            int added = 0;

            foreach (string[] entry in StaticEntries)
            {
                SerializedProperty property = FindEntry(entries, entry[0]);

                if (property == null)
                {
                    entries.arraySize++;
                    property = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                    property.FindPropertyRelative("_key").stringValue = entry[0];
                    added++;
                }

                property.FindPropertyRelative("_russian").stringValue = entry[1];
                property.FindPropertyRelative("_english").stringValue = entry[2];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            _report.Count("localization static keys added", added);
        }

        public void ImportTaskTitles(int firstGlobalTaskIndex, int taskCount)
        {
            LocalizationConfig config = LoadConfig();
            Dictionary<int, string[]> titles = ReadTitles();
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty entries = serialized.FindProperty("_entries");
            int added = 0;
            int updated = 0;

            for (int task = firstGlobalTaskIndex; task < firstGlobalTaskIndex + taskCount; task++)
            {
                if (titles.TryGetValue(task, out string[] languages) == false)
                {
                    _report.Warn("TaskTitle/task_" + task + " missing in I2Languages");
                    continue;
                }

                string key = BuildingAssetPaths.TaskTitleKeyPrefix + task;
                SerializedProperty entry = FindEntry(entries, key);

                if (entry == null)
                {
                    entries.arraySize++;
                    entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                    entry.FindPropertyRelative("_key").stringValue = key;
                    added++;
                }
                else
                {
                    updated++;
                }

                entry.FindPropertyRelative("_russian").stringValue = languages[RussianIndex];
                entry.FindPropertyRelative("_english").stringValue = languages[EnglishIndex];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            _report.Count("localization keys added", added);
            _report.Count("localization keys updated", updated);
        }

        private LocalizationConfig LoadConfig()
        {
            LocalizationConfig config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(BuildingAssetPaths.LocalizationConfigPath);

            if (config == null)
            {
                throw new FileNotFoundException(BuildingAssetPaths.LocalizationConfigPath);
            }

            return config;
        }

        private SerializedProperty FindEntry(SerializedProperty entries, string key)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);

                if (entry.FindPropertyRelative("_key").stringValue == key)
                {
                    return entry;
                }
            }

            return null;
        }

        private Dictionary<int, string[]> ReadTitles()
        {
            string text = File.ReadAllText(ReferenceExport.LocalizationPath);
            Dictionary<int, string[]> titles = new Dictionary<int, string[]>();

            foreach (Match match in TaskTitleTerm.Matches(text))
            {
                int task = int.Parse(match.Groups[1].Value);
                string[] lines = match.Groups[2].Value.Split('\n');
                List<string> languages = new List<string>();

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (line.StartsWith("- ") == false)
                    {
                        continue;
                    }

                    languages.Add(Unquote(line.Substring(2)));
                }

                if (languages.Count > RussianIndex)
                {
                    titles[task] = languages.ToArray();
                }
            }

            return titles;
        }

        private string Unquote(string value)
        {
            value = value.Trim();

            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            {
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                return Regex.Unescape(value.Substring(1, value.Length - 2));
            }

            return value;
        }
    }
}
