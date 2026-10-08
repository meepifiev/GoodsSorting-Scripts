using System;
using System.Collections.Generic;
using System.IO;
using _Project.Core.Building;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class BuildingBalanceImporter
    {
        private const int EncryptionKey = 82380971;
        private const string BalanceAssetPath = ReferenceExport.MonoBehaviourFolder + "/SerializableSet.asset";
        private static readonly string[] CostFields = { "tiny", "small", "medium", "large", "giant" };

        private readonly ReferenceExport _export;
        private readonly BuildingImportReport _report;

        public BuildingBalanceImporter(ReferenceExport export, BuildingImportReport report)
        {
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public List<BuildingTaskType> TaskTypes { get; } = new List<BuildingTaskType>();

        public BuildingBalanceConfig Import(int unlockLevel, int gemsPerWin)
        {
            UnityYamlNode balance = _export.LoadMonoBehaviour(BalanceAssetPath);

            List<BuildingDayCosts> costs = new List<BuildingDayCosts>();

            foreach (UnityYamlNode day in balance.GetOrEmpty("BuildingResouceRequires").Items)
            {
                int[] byType = new int[CostFields.Length];

                for (int i = 0; i < CostFields.Length; i++)
                {
                    byType[i] = Decrypt(day.GetOrEmpty(CostFields[i]));
                }

                costs.Add(new BuildingDayCosts(byType));
            }

            List<BuildingDayReward> rewards = new List<BuildingDayReward>();

            foreach (UnityYamlNode day in balance.GetOrEmpty("BuildingRewardByDays").Items)
            {
                rewards.Add(new BuildingDayReward(
                    Decrypt(day.GetOrEmpty("gold")),
                    Decrypt(day.GetOrEmpty("hammer")),
                    Decrypt(day.GetOrEmpty("swap")),
                    Decrypt(day.GetOrEmpty("replace")),
                    Decrypt(day.GetOrEmpty("freeze"))));
            }

            TaskTypes.Clear();

            foreach (UnityYamlNode task in balance.GetOrEmpty("BuildingTaskTypes").Items)
            {
                TaskTypes.Add((BuildingTaskType)Decrypt(task.GetOrEmpty("type")));
            }

            if (costs.Count == 0 || rewards.Count == 0 || TaskTypes.Count == 0)
            {
                throw new InvalidDataException(BalanceAssetPath);
            }

            BuildingBalanceConfig config = LoadOrCreate();
            config.Initialize(unlockLevel, gemsPerWin, costs.ToArray(), rewards.ToArray());
            EditorUtility.SetDirty(config);

            _report.Count("balance days", costs.Count);
            _report.Count("balance task types", TaskTypes.Count);
            return config;
        }

        private BuildingBalanceConfig LoadOrCreate()
        {
            BuildingBalanceConfig config = AssetDatabase.LoadAssetAtPath<BuildingBalanceConfig>(BuildingAssetPaths.BalancePath);

            if (config != null)
            {
                return config;
            }

            if (Directory.Exists(BuildingAssetPaths.ConfigsFolder) == false)
            {
                Directory.CreateDirectory(BuildingAssetPaths.ConfigsFolder);
                AssetDatabase.Refresh();
            }

            config = ScriptableObject.CreateInstance<BuildingBalanceConfig>();
            AssetDatabase.CreateAsset(config, BuildingAssetPaths.BalancePath);
            return config;
        }

        private int Decrypt(UnityYamlNode encrypted)
        {
            return encrypted.GetInt("encryptedValue") ^ EncryptionKey;
        }
    }
}
