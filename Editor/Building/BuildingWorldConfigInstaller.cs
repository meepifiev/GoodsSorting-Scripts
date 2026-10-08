using System;
using System.Collections.Generic;
using _Project.Composition;
using _Project.Core.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Editor.Building
{
    public class BuildingWorldConfigInstaller
    {
        private const string AreasProperty = "_areas";

        private readonly BuildingImportReport _report;

        public BuildingWorldConfigInstaller(BuildingImportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public BuildingWorldConfig BuildConfig()
        {
            List<BuildingAreaConfig> areas = new BuildingAreaAssets().LoadAll();
            BuildingWorldConfig config = AssetDatabase.LoadAssetAtPath<BuildingWorldConfig>(BuildingAssetPaths.WorldConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildingWorldConfig>();
                AssetDatabase.CreateAsset(config, BuildingAssetPaths.WorldConfigPath);
            }

            config.Initialize(areas.ToArray());
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            _report.Count("world config areas", areas.Count);
            return config;
        }

        public void InstallIntoScene(BuildingWorldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            BuildingSceneFile sceneFile = new BuildingSceneFile();
            Scene scene = sceneFile.OpenOrCreate();
            BuildingLifetimeScope scope = UnityEngine.Object.FindObjectOfType<BuildingLifetimeScope>();

            if (scope == null)
            {
                throw new InvalidOperationException("Run the full import first: the Building scene has no lifetime scope");
            }

            SerializedObject serialized = new SerializedObject(scope);
            serialized.FindProperty(AreasProperty).objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sceneFile.Save(scene);
            _report.Count("scene: world config installed");
        }
    }
}
