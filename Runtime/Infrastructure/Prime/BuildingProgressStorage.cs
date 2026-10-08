using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using _Project.Core.Building;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class BuildingProgressStorage : IBuildingProgressStorage
    {
        private const string KeyPrefix = "building_area_";
        private const char SectionSeparator = '|';
        private const char TaskSeparator = ';';
        private const char ThemeSeparator = '=';
        private const char DayTaskSeparator = '.';

        public BuildingAreaProgress Load(int areaIndex)
        {
            string raw = PrimeSDK.Data.GetString(KeyFor(areaIndex), string.Empty);

            if (string.IsNullOrEmpty(raw))
            {
                return new BuildingAreaProgress();
            }

            try
            {
                return Parse(raw);
            }
            catch (Exception)
            {
                return new BuildingAreaProgress();
            }
        }

        public void Save(int areaIndex, BuildingAreaProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            PrimeSDK.Data.SetString(KeyFor(areaIndex), Serialize(progress), important: true);
            PrimeSDK.Data.Save();
        }

        private string Serialize(BuildingAreaProgress progress)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(progress.CurrentDay.ToString(CultureInfo.InvariantCulture));
            builder.Append(SectionSeparator);
            builder.Append(progress.Completed ? '1' : '0');
            builder.Append(SectionSeparator);
            bool first = true;

            foreach (KeyValuePair<BuildingTaskRef, int> pair in progress.DoneTasks)
            {
                if (first == false)
                {
                    builder.Append(TaskSeparator);
                }

                first = false;
                builder.Append(pair.Key.Day.ToString(CultureInfo.InvariantCulture));
                builder.Append(DayTaskSeparator);
                builder.Append(pair.Key.Task.ToString(CultureInfo.InvariantCulture));
                builder.Append(ThemeSeparator);
                builder.Append(pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private BuildingAreaProgress Parse(string raw)
        {
            string[] sections = raw.Split(SectionSeparator);
            int currentDay = int.Parse(sections[0], CultureInfo.InvariantCulture);
            bool completed = sections.Length > 1 && sections[1] == "1";
            List<KeyValuePair<BuildingTaskRef, int>> done = new List<KeyValuePair<BuildingTaskRef, int>>();

            if (sections.Length > 2 && sections[2].Length > 0)
            {
                foreach (string entry in sections[2].Split(TaskSeparator))
                {
                    string[] taskAndTheme = entry.Split(ThemeSeparator);
                    string[] dayAndTask = taskAndTheme[0].Split(DayTaskSeparator);
                    BuildingTaskRef task = new BuildingTaskRef(
                        int.Parse(dayAndTask[0], CultureInfo.InvariantCulture),
                        int.Parse(dayAndTask[1], CultureInfo.InvariantCulture));
                    int theme = taskAndTheme.Length > 1 ? int.Parse(taskAndTheme[1], CultureInfo.InvariantCulture) : 0;
                    done.Add(new KeyValuePair<BuildingTaskRef, int>(task, theme));
                }
            }

            return new BuildingAreaProgress(currentDay, completed, done);
        }

        private string KeyFor(int areaIndex)
        {
            return KeyPrefix + areaIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}
