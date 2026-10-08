using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace _Project.Editor.Building
{
    public class BuildingSceneFile
    {
        public Scene OpenOrCreate()
        {
            Scene scene;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildingAssetPaths.ScenePath) != null)
            {
                scene = SceneManager.GetSceneByPath(BuildingAssetPaths.ScenePath);

                if (scene.isLoaded == false)
                {
                    scene = EditorSceneManager.OpenScene(BuildingAssetPaths.ScenePath, OpenSceneMode.Additive);
                }
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, BuildingAssetPaths.ScenePath);
            }

            SceneManager.SetActiveScene(scene);
            return scene;
        }

        public void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BuildingAssetPaths.ScenePath);
        }
    }
}
