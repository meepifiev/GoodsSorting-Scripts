using System.Collections.Generic;
using _Project.Core.Building;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceAreaData
    {
        public class Task
        {
            public int DependsOnTaskIndex;
            public readonly List<BuildingItemPlacement> ItemsBefore = new List<BuildingItemPlacement>();
            public readonly List<BuildingItemPlacement> ItemsAfter = new List<BuildingItemPlacement>();
        }

        public class Day
        {
            public readonly List<Task> Tasks = new List<Task>();
        }

        public readonly List<Day> Days = new List<Day>();
        public readonly List<BuildingItemPlacement> DecorBefore = new List<BuildingItemPlacement>();
        public readonly List<BuildingItemPlacement> DecorAfter = new List<BuildingItemPlacement>();

        public int TaskCount
        {
            get
            {
                int count = 0;

                foreach (Day day in Days)
                {
                    count += day.Tasks.Count;
                }

                return count;
            }
        }

        public HashSet<int> CollectItemIds()
        {
            HashSet<int> ids = new HashSet<int>();

            foreach (BuildingItemPlacement placement in CollectAllPlacements())
            {
                ids.Add(placement.ItemId);
            }

            return ids;
        }

        public List<BuildingItemPlacement> CollectAllPlacements()
        {
            List<BuildingItemPlacement> placements = new List<BuildingItemPlacement>();

            foreach (Day day in Days)
            {
                foreach (Task task in day.Tasks)
                {
                    placements.AddRange(task.ItemsBefore);
                    placements.AddRange(task.ItemsAfter);
                }
            }

            placements.AddRange(DecorBefore);
            placements.AddRange(DecorAfter);
            return placements;
        }

        public void Load(UnityYamlNode areaFields)
        {
            Days.Clear();
            DecorBefore.Clear();
            DecorAfter.Clear();

            ReadPlacements(areaFields.GetOrEmpty("itemsBeforePool"), DecorBefore);
            ReadPlacements(areaFields.GetOrEmpty("itemsAfterPool"), DecorAfter);

            foreach (UnityYamlNode dayNode in areaFields.GetOrEmpty("days").Items)
            {
                Day day = new Day();

                foreach (UnityYamlNode taskNode in dayNode.GetOrEmpty("tasks").Items)
                {
                    Task task = new Task
                    {
                        DependsOnTaskIndex = taskNode.GetInt("dependOnTaskId", -1)
                    };

                    ReadPlacements(taskNode.GetOrEmpty("itemsBefore"), task.ItemsBefore);
                    ReadPlacements(taskNode.GetOrEmpty("itemsAfter"), task.ItemsAfter);
                    day.Tasks.Add(task);
                }

                Days.Add(day);
            }
        }

        private void ReadPlacements(UnityYamlNode list, List<BuildingItemPlacement> target)
        {
            foreach (UnityYamlNode item in list.Items)
            {
                target.Add(new BuildingItemPlacement(
                    item.GetInt("id"),
                    item.GetInt("flip") != 0,
                    item.GetVector3("pos")));
            }
        }
    }
}
