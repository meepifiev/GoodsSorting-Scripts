using System.Collections.Generic;
using _Project.Composition;
using _Project.Core.Building;
using _Project.Core.Economy;
using _Project.Core.Progress;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using _Project.Infrastructure.Prime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace _Project.Editor.Building
{
    public static class BuildingImportMenu
    {
        private const string MenuRoot = "Tools/Goods Sorting/Building/";
        private const string DevMenuRoot = "Tools/Goods Sorting/Development/";
        private const int UnlockLevel = 3;
        private const int GemsPerWin = 100;
        private const int DevGemsAmount = 100000;

        private const int Area0 = 0;
        private const int Area0FirstGlobalDay = 0;
        private const int Area0FirstGlobalTask = 0;
        private const int Area1 = 1;
        private const int Area1FirstGlobalDay = 3;
        private const int Area1FirstGlobalTask = 47;
        private const int Area2 = 2;
        private const int Area2FirstGlobalDay = 7;
        private const int Area2FirstGlobalTask = 145;
        private const int Area3 = 3;
        private const int Area3FirstGlobalDay = 11;
        private const int Area3FirstGlobalTask = 241;

        [MenuItem(MenuRoot + "Import Area 0 (full: world + UI, from reference)")]
        public static void ImportArea0()
        {
            Debug.Log(CreateImporter(Area0, Area0FirstGlobalDay, Area0FirstGlobalTask).Run().ToString());
        }

        [MenuItem(MenuRoot + "Import Area 1 assets (from reference)")]
        public static void ImportArea1()
        {
            Debug.Log(CreateImporter(Area1, Area1FirstGlobalDay, Area1FirstGlobalTask).ImportAreaAssets().ToString());
        }

        [MenuItem(MenuRoot + "Import Area 2 assets (from reference)")]
        public static void ImportArea2()
        {
            Debug.Log(CreateImporter(Area2, Area2FirstGlobalDay, Area2FirstGlobalTask).ImportAreaAssets().ToString());
        }

        [MenuItem(MenuRoot + "Import Area 3 assets (decor only, from reference)")]
        public static void ImportArea3()
        {
            Debug.Log(CreateImporter(Area3, Area3FirstGlobalDay, Area3FirstGlobalTask).ImportAreaAssets().ToString());
        }

        [MenuItem(MenuRoot + "Install World Config (areas -> scene)")]
        public static void InstallWorldConfig()
        {
            BuildingImportReport report = new BuildingImportReport();
            BuildingWorldConfigInstaller installer = new BuildingWorldConfigInstaller(report);
            BuildingWorldConfig config = installer.BuildConfig();
            installer.InstallIntoScene(config);
            Debug.Log(report.ToString());
        }

        [MenuItem(MenuRoot + "Import Street Decor (from reference)")]
        public static void ImportDecor()
        {
            Debug.Log(CreateImporter(Area0, Area0FirstGlobalDay, Area0FirstGlobalTask).ImportDecor().ToString());
        }

        [MenuItem(MenuRoot + "Reimport Environment Tiles (from reference)")]
        public static void ReimportEnvironmentTiles()
        {
            Debug.Log(CreateImporter(Area0, Area0FirstGlobalDay, Area0FirstGlobalTask).ImportEnvironmentTiles().ToString());
        }

        [MenuItem(MenuRoot + "Fix Task Dependencies (global id -> same day)")]
        public static void FixDependencies()
        {
            BuildingImportReport report = new BuildingImportReport();
            new BuildingDependencyResolver(report).FixExistingConfigs(new BuildingAreaAssets().LoadAll());
            Debug.Log(report.ToString());
        }

        [MenuItem(MenuRoot + "Rebuild Building UI (scene)")]
        public static void RebuildUi()
        {
            Debug.Log(CreateImporter(Area0, Area0FirstGlobalDay, Area0FirstGlobalTask).RebuildUi().ToString());
        }

        [MenuItem(MenuRoot + "Install Menu & Win Integration")]
        public static void InstallIntegration()
        {
            BuildingImportReport report = new BuildingImportReport();
            new BuildingMenuIntegrationInstaller(report).Install();
            Debug.Log(report.ToString());
        }

        [MenuItem(MenuRoot + "Preview - Build Before State (from configs)")]
        public static void PreviewInitial()
        {
            new BuildingAreaPreview().ShowInitial();
        }

        [MenuItem(MenuRoot + "Preview - Build After State (from configs)")]
        public static void PreviewFinal()
        {
            new BuildingAreaPreview().ShowFinal();
        }

        [MenuItem(MenuRoot + "Re-sort Preview")]
        public static void ResortPreview()
        {
            new BuildingAreaPreview().Resort();
        }

        [MenuItem(MenuRoot + "Apply Preview To Area Configs")]
        public static void ApplyPreview()
        {
            new BuildingAreaPreview().ApplyToConfig();
        }

        [MenuItem(MenuRoot + "Clear Preview")]
        public static void ClearPreview()
        {
            new BuildingAreaPreview().Clear();
        }

        [MenuItem(DevMenuRoot + "Add 100000 Gems (Play Mode)")]
        public static void AddGems()
        {
            LifetimeScope root = FindRootScope();

            if (root == null)
            {
                return;
            }

            root.Container.Resolve<IWalletStorage>().Add(ResourceType.Gems, DevGemsAmount);
            Debug.Log("[Development] +" + DevGemsAmount + " gems.");
        }

        [MenuItem(DevMenuRoot + "Unlock Building (Play Mode)")]
        public static void UnlockBuilding()
        {
            LifetimeScope root = FindRootScope();

            if (root == null)
            {
                return;
            }

            IBuildingAccess access = root.Container.Resolve<IBuildingAccess>();
            IProgressStorage progress = root.Container.Resolve<IProgressStorage>();

            if (access.IsUnlocked)
            {
                Debug.Log("[Development] Building is already unlocked (level " + progress.Level + ").");
                return;
            }

            progress.Level = access.UnlockLevel;
            progress.Save();
            Debug.Log("[Development] Level set to " + progress.Level + ", building unlocked.");

            if (SceneManager.GetActiveScene().name == SceneId.Menu.ToString())
            {
                root.Container.Resolve<IGameLauncher>().GoToMenu();
            }
        }

        [MenuItem(DevMenuRoot + "Build All Building Tasks (Play Mode)")]
        public static void BuildAllBuildingTasks()
        {
            LifetimeScope root = FindRootScope();

            if (root == null)
            {
                return;
            }

            BuildingWorldConfig world = AssetDatabase.LoadAssetAtPath<BuildingWorldConfig>(BuildingAssetPaths.WorldConfigPath);

            if (world == null)
            {
                Debug.LogWarning("[Development] World config not found: " + BuildingAssetPaths.WorldConfigPath);
                return;
            }

            IBuildingProgressStorage storage = new BuildingProgressStorage();
            int tasks = 0;

            for (int areaIndex = 0; areaIndex < world.AreaCount; areaIndex++)
            {
                BuildingAreaConfig area = world.GetArea(areaIndex);
                List<KeyValuePair<BuildingTaskRef, int>> done = new List<KeyValuePair<BuildingTaskRef, int>>();

                for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
                {
                    BuildingDayConfig day = area.GetDay(dayIndex);

                    for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                    {
                        done.Add(new KeyValuePair<BuildingTaskRef, int>(new BuildingTaskRef(dayIndex, taskIndex), 0));
                        tasks++;
                    }
                }

                int lastDay = Mathf.Max(area.DayCount - 1, 0);
                storage.Save(area.AreaIndex, new BuildingAreaProgress(lastDay, true, done));
            }

            Debug.Log("[Development] All building tasks marked as built: " + tasks + " in " + world.AreaCount + " areas (theme 0). Re-enter the building scene to see it.");

            if (SceneManager.GetActiveScene().name == SceneId.Building.ToString())
            {
                root.Container.Resolve<IGameLauncher>().GoToBuilding();
            }
        }

        private static LifetimeScope FindRootScope()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("[Development] Enter Play Mode first.");
                return null;
            }

            LifetimeScope root = LifetimeScope.Find<RootLifetimeScope>();

            if (root == null || root.Container == null)
            {
                Debug.LogWarning("[Development] RootLifetimeScope not found.");
                return null;
            }

            return root;
        }

        private static BuildingImporter CreateImporter(int areaIndex, int firstGlobalDay, int firstGlobalTask)
        {
            return new BuildingImporter(areaIndex, firstGlobalDay, firstGlobalTask, UnlockLevel, GemsPerWin);
        }
    }
}
