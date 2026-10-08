using System;
using System.Collections.Generic;
using _Project.Core.Building;
using _Project.Features.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Editor.Building
{
    public class BuildingAreaPreview
    {
        private const string PreviewRootName = "Preview";
        private const string BeforeContainerName = "Before";
        private const string AfterContainerName = "After";
        private const string EditorOnlyTag = "EditorOnly";
        private const float PositionPrecision = 0.01f;
        private const float ChangeEpsilon = 0.0001f;
        private const float RotationEpsilon = 0.01f;
        private const int NoTask = -1;

        private readonly IsoProjection _projection = new IsoProjection();
        private readonly IsoLayout _layout;

        public BuildingAreaPreview()
        {
            _layout = new IsoLayout(new IsoDepthSorter(_projection));
        }

        public void ShowInitial()
        {
            BuildState(false);
        }

        public void ShowFinal()
        {
            BuildState(true);
        }

        public bool HasState(bool after)
        {
            return TryFindContainer(after, out _);
        }

        public bool IsStateVisible(bool after)
        {
            return TryFindContainer(after, out Transform container) && container.gameObject.activeSelf;
        }

        public void SetStateVisible(bool after, bool visible)
        {
            if (TryFindContainer(after, out Transform container) == false || container.gameObject.activeSelf == visible)
            {
                return;
            }

            Undo.RecordObject(container.gameObject, "Toggle Building Preview State");
            container.gameObject.SetActive(visible);
            Resort();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public void Clear()
        {
            if (TryFindRoot(out Transform root))
            {
                UnityEngine.Object.DestroyImmediate(root.gameObject);
                SetCloudsRendered(true);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        public bool TryFindRoot(out Transform root)
        {
            root = null;
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world == null || world.ItemsRoot == null)
            {
                return false;
            }

            root = world.ItemsRoot.Find(PreviewRootName);
            return root != null;
        }

        public void Resort()
        {
            if (TryFindRoot(out Transform root) == false)
            {
                return;
            }

            BuildingPlacementHandle[] handles = root.GetComponentsInChildren<BuildingPlacementHandle>();
            List<BuildingItemView> views = new List<BuildingItemView>();
            List<IsoBox> boxes = new List<IsoBox>();
            List<int> layers = new List<int>();

            foreach (BuildingPlacementHandle handle in handles)
            {
                BuildingItemView view = handle.GetComponent<BuildingItemView>();

                if (view == null)
                {
                    continue;
                }

                view.SetFlipped(handle.Flipped);
                views.Add(view);
                boxes.Add(new IsoBox(ReadIsoPosition(view, handle), view.FlippedIsoSize));
                layers.Add(handle.SortingLayer);
            }

            float[] depths = _layout.ComputeDepths(boxes, layers);

            for (int i = 0; i < views.Count; i++)
            {
                Vector3 local = views[i].transform.localPosition;
                views[i].SetScreenPosition(new Vector2(local.x, local.y), depths[i]);
            }
        }

        public void ApplyToConfig()
        {
            if (TryFindRoot(out Transform root) == false)
            {
                throw new InvalidOperationException("No building preview in the scene");
            }

            List<BuildingAreaConfig> areas = new BuildingAreaAssets().LoadAll();
            Dictionary<string, List<BuildingPlacementHandle>> buckets = new Dictionary<string, List<BuildingPlacementHandle>>();
            bool rebuildBefore = false;
            bool rebuildAfter = false;
            int skipped = 0;

            foreach (BuildingPlacementHandle handle in root.GetComponentsInChildren<BuildingPlacementHandle>(true))
            {
                if (handle.GetComponent<BuildingItemView>() == null || handle.Area < 0 || handle.Area >= areas.Count)
                {
                    skipped++;
                    continue;
                }

                bool after = IsAfterGroup(handle.Group);
                rebuildBefore |= after == false;
                rebuildAfter |= after;

                string key = BucketKey(handle.Area, handle.Group, handle.Day, handle.Task);

                if (buckets.TryGetValue(key, out List<BuildingPlacementHandle> bucket) == false)
                {
                    bucket = new List<BuildingPlacementHandle>();
                    buckets[key] = bucket;
                }

                bucket.Add(handle);
            }

            Undo.RecordObjects(areas.ToArray(), "Apply Building Preview");
            ApplyStats stats = new ApplyStats();

            for (int areaIndex = 0; areaIndex < areas.Count; areaIndex++)
            {
                BuildingAreaConfig area = areas[areaIndex];

                if (rebuildBefore)
                {
                    area.SetDecorBefore(Rebuild(buckets, BucketKey(areaIndex, BuildingPlacementGroup.DecorBefore, NoTask, NoTask), area.DecorBefore, stats));
                }

                if (rebuildAfter)
                {
                    area.SetDecorAfter(Rebuild(buckets, BucketKey(areaIndex, BuildingPlacementGroup.DecorAfter, NoTask, NoTask), area.DecorAfter, stats));
                }

                for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
                {
                    BuildingDayConfig day = area.GetDay(dayIndex);

                    for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                    {
                        BuildingTaskConfig task = day.GetTask(taskIndex);

                        if (rebuildBefore)
                        {
                            task.SetItemsBefore(Rebuild(buckets, BucketKey(areaIndex, BuildingPlacementGroup.TaskBefore, dayIndex, taskIndex), task.ItemsBefore, stats));
                        }

                        if (rebuildAfter)
                        {
                            task.SetItemsAfter(Rebuild(buckets, BucketKey(areaIndex, BuildingPlacementGroup.TaskAfter, dayIndex, taskIndex), task.ItemsAfter, stats));
                        }
                    }
                }

                EditorUtility.SetDirty(area);
            }

            AssetDatabase.SaveAssets();
            string state = (rebuildBefore ? "before" : string.Empty) + (rebuildBefore && rebuildAfter ? "+" : string.Empty) + (rebuildAfter ? "after" : string.Empty);
            Debug.Log("[Building preview] applied to " + areas.Count + " area configs: kept " + stats.Kept
                      + ", moved " + stats.Moved + ", added " + stats.Added + ", removed " + stats.Removed
                      + ", skipped " + skipped + " (" + state + " state)");
        }

        private class ApplyStats
        {
            public int Kept;
            public int Moved;
            public int Added;
            public int Removed;
        }

        private string BucketKey(int area, BuildingPlacementGroup group, int day, int task)
        {
            return area + "|" + (int)group + "|" + day + "|" + task;
        }

        private bool IsAfterGroup(BuildingPlacementGroup group)
        {
            return group == BuildingPlacementGroup.TaskAfter || group == BuildingPlacementGroup.DecorAfter;
        }

        private BuildingItemPlacement[] Rebuild(
            Dictionary<string, List<BuildingPlacementHandle>> buckets,
            string key,
            BuildingItemPlacement[] previous,
            ApplyStats stats)
        {
            if (buckets.TryGetValue(key, out List<BuildingPlacementHandle> handles) == false)
            {
                stats.Removed += previous.Length;
                return Array.Empty<BuildingItemPlacement>();
            }

            handles.Sort(CompareHandles);
            BuildingItemPlacement[] result = new BuildingItemPlacement[handles.Count];
            HashSet<int> matched = new HashSet<int>();
            Undo.RecordObjects(handles.ToArray(), "Apply Building Preview");

            for (int i = 0; i < handles.Count; i++)
            {
                BuildingPlacementHandle handle = handles[i];
                BuildingItemView view = handle.GetComponent<BuildingItemView>();
                Vector3 iso = RoundPosition(ReadIsoPosition(view, handle));
                BuildingChildOverride[] overrides = ReadChildOverrides(view);
                BuildingItemPlacement placement = new BuildingItemPlacement(view.ItemId, handle.Flipped, iso);
                placement.SetLayout(iso, handle.Flipped, handle.SortingLayer);
                placement.SetChildOverrides(overrides);
                result[i] = placement;

                bool matchesPrevious = handle.Index >= 0
                                       && handle.Index < previous.Length
                                       && matched.Contains(handle.Index) == false
                                       && previous[handle.Index].ItemId == view.ItemId;

                if (matchesPrevious)
                {
                    matched.Add(handle.Index);

                    if (IsDifferent(previous[handle.Index], iso, handle, overrides))
                    {
                        stats.Moved++;
                    }
                    else
                    {
                        stats.Kept++;
                    }
                }
                else
                {
                    stats.Added++;
                }
            }

            stats.Removed += previous.Length - matched.Count;

            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].Index != i)
                {
                    handles[i].SetIndex(i);
                    EditorUtility.SetDirty(handles[i]);
                }
            }

            return result;
        }

        private int CompareHandles(BuildingPlacementHandle left, BuildingPlacementHandle right)
        {
            int byIndex = left.Index.CompareTo(right.Index);
            return byIndex != 0 ? byIndex : left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
        }

        private void BuildState(bool after)
        {
            List<BuildingAreaConfig> areas = new BuildingAreaAssets().LoadAll();
            BuildingItemCatalog catalog = LoadCatalog();
            Transform root = EnsureRoot();

            if (TryFindContainer(after, out Transform existing))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            Transform container = EnsureContainer(root, after);
            int spawned = 0;

            for (int areaIndex = 0; areaIndex < areas.Count; areaIndex++)
            {
                BuildingAreaConfig area = areas[areaIndex];
                BuildingItemPlacement[] decor = after ? area.DecorAfter : area.DecorBefore;
                BuildingPlacementGroup decorGroup = after ? BuildingPlacementGroup.DecorAfter : BuildingPlacementGroup.DecorBefore;

                for (int index = 0; index < decor.Length; index++)
                {
                    if (SpawnPreviewItem(catalog, container, decor[index], areaIndex, decorGroup, NoTask, NoTask, index))
                    {
                        spawned++;
                    }
                }

                for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
                {
                    BuildingDayConfig day = area.GetDay(dayIndex);

                    for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                    {
                        BuildingTaskConfig task = day.GetTask(taskIndex);
                        BuildingItemPlacement[] placements = after ? task.ItemsAfter : task.ItemsBefore;
                        BuildingPlacementGroup group = after ? BuildingPlacementGroup.TaskAfter : BuildingPlacementGroup.TaskBefore;

                        for (int index = 0; index < placements.Length; index++)
                        {
                            if (SpawnPreviewItem(catalog, container, placements[index], areaIndex, group, dayIndex, taskIndex, index))
                            {
                                spawned++;
                            }
                        }
                    }
                }
            }

            Resort();
            SetCloudsRendered(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Building preview] built " + (after ? "after" : "before") + " state: " + spawned + " items from " + areas.Count + " areas");
        }

        private Transform EnsureRoot()
        {
            Transform itemsRoot = FindItemsRoot();
            Transform root = itemsRoot.Find(PreviewRootName);

            if (root == null)
            {
                GameObject rootObject = new GameObject(PreviewRootName);
                rootObject.tag = EditorOnlyTag;
                rootObject.transform.SetParent(itemsRoot, false);
                root = rootObject.transform;
            }

            MigrateLooseHandles(root);
            return root;
        }

        private void MigrateLooseHandles(Transform root)
        {
            List<Transform> loose = new List<Transform>();

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.GetComponent<BuildingPlacementHandle>() != null)
                {
                    loose.Add(child);
                }
            }

            foreach (Transform child in loose)
            {
                bool after = IsAfterGroup(child.GetComponent<BuildingPlacementHandle>().Group);
                child.SetParent(EnsureContainer(root, after), true);
            }
        }

        private Transform EnsureContainer(Transform root, bool after)
        {
            string name = after ? AfterContainerName : BeforeContainerName;
            Transform container = root.Find(name);

            if (container == null)
            {
                GameObject containerObject = new GameObject(name);
                containerObject.transform.SetParent(root, false);
                container = containerObject.transform;
            }

            return container;
        }

        private bool TryFindContainer(bool after, out Transform container)
        {
            container = null;

            if (TryFindRoot(out Transform root) == false)
            {
                return false;
            }

            container = root.Find(after ? AfterContainerName : BeforeContainerName);
            return container != null;
        }

        private bool SpawnPreviewItem(
            BuildingItemCatalog catalog,
            Transform container,
            BuildingItemPlacement placement,
            int areaIndex,
            BuildingPlacementGroup group,
            int dayIndex,
            int taskIndex,
            int index)
        {
            if (catalog.Contains(placement.ItemId) == false)
            {
                Debug.LogWarning("[Building preview] item " + placement.ItemId + " missing in catalog");
                return false;
            }

            BuildingItemView prefab = BuildingItemPrefabLookup.Get(placement.ItemId);

            if (prefab == null)
            {
                Debug.LogWarning("[Building preview] prefab missing for item " + placement.ItemId);
                return false;
            }

            BuildingItemView view = (BuildingItemView)PrefabUtility.InstantiatePrefab(prefab, container);
            view.name = BuildName(areaIndex, group, dayIndex, taskIndex, index, view.name);
            view.SetTheme(0);
            BuildingItemOverrides.Apply(view, placement.ChildOverrides);
            view.SetFlipped(placement.Flipped);
            view.SetScreenPosition(_projection.IsoToScreen(placement.IsoPosition), 0f);

            BuildingPlacementHandle handle = view.gameObject.AddComponent<BuildingPlacementHandle>();
            handle.Initialize(areaIndex, group, dayIndex, taskIndex, index, placement.IsoPosition.z, placement.Flipped, placement.SortingLayer);
            return true;
        }

        private string BuildName(int areaIndex, BuildingPlacementGroup group, int dayIndex, int taskIndex, int index, string prefabName)
        {
            string location = dayIndex == NoTask
                ? "decor"
                : "d" + dayIndex + ".t" + taskIndex;
            string state = IsAfterGroup(group) ? "after" : "before";
            return "a" + areaIndex + "." + location + "." + state + "[" + index + "] " + prefabName;
        }

        private BuildingChildOverride[] ReadChildOverrides(BuildingItemView view)
        {
            Transform sourceRoot = PrefabUtility.GetCorrespondingObjectFromSource(view.transform);

            if (sourceRoot == null)
            {
                return Array.Empty<BuildingChildOverride>();
            }

            List<BuildingChildOverride> result = new List<BuildingChildOverride>();

            foreach (Transform child in view.GetComponentsInChildren<Transform>(true))
            {
                if (child == view.transform)
                {
                    continue;
                }

                Transform source = PrefabUtility.GetCorrespondingObjectFromSource(child);
                int[] path = source != null ? BuildingItemOverrides.PathTo(sourceRoot, source) : null;

                if (path == null)
                {
                    continue;
                }

                BuildingChildOverride change = new BuildingChildOverride(path);
                ReadTransform(view, child, source, change);

                if (view.IsThemeRoot(child.gameObject) == false && child.gameObject.activeSelf != source.gameObject.activeSelf)
                {
                    change.SetActive(child.gameObject.activeSelf);
                }

                Renderer renderer = child.GetComponent<Renderer>();
                Renderer sourceRenderer = source.GetComponent<Renderer>();

                if (renderer != null && sourceRenderer != null
                    && (renderer.enabled != sourceRenderer.enabled || renderer.sortingOrder != sourceRenderer.sortingOrder))
                {
                    change.SetRenderer(renderer.enabled, renderer.sortingOrder);
                }

                if (change.IsEmpty == false)
                {
                    result.Add(change);
                }
            }

            foreach (RemovedGameObject removed in PrefabUtility.GetRemovedGameObjects(view.gameObject))
            {
                int[] path = BuildingItemOverrides.PathTo(sourceRoot, removed.assetGameObject.transform);

                if (path == null)
                {
                    continue;
                }

                BuildingChildOverride change = new BuildingChildOverride(path);
                change.SetActive(false);
                result.Add(change);
            }

            foreach (RemovedComponent removed in PrefabUtility.GetRemovedComponents(view.gameObject))
            {
                Renderer renderer = removed.assetComponent as Renderer;
                int[] path = renderer != null ? BuildingItemOverrides.PathTo(sourceRoot, renderer.transform) : null;

                if (path == null)
                {
                    continue;
                }

                BuildingChildOverride change = new BuildingChildOverride(path);
                change.SetRenderer(false, renderer.sortingOrder);
                result.Add(change);
            }

            return result.ToArray();
        }

        private void ReadTransform(BuildingItemView view, Transform child, Transform source, BuildingChildOverride change)
        {
            Vector3 scale = child.localScale;
            Vector3 sourceScale = source.localScale;

            if (child == view.VisualRoot)
            {
                scale.x = Mathf.Abs(scale.x);
                sourceScale.x = Mathf.Abs(sourceScale.x);
            }

            bool moved = (child.localPosition - source.localPosition).sqrMagnitude > ChangeEpsilon
                         || (scale - sourceScale).sqrMagnitude > ChangeEpsilon
                         || Quaternion.Angle(child.localRotation, source.localRotation) > RotationEpsilon;

            if (moved)
            {
                change.SetTransform(child.localPosition, child.localEulerAngles, scale);
            }
        }

        private bool OverridesDiffer(IReadOnlyList<BuildingChildOverride> previous, BuildingChildOverride[] current)
        {
            if (previous.Count != current.Length)
            {
                return true;
            }

            for (int i = 0; i < current.Length; i++)
            {
                if (previous[i].Matches(current[i]) == false)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 ReadIsoPosition(BuildingItemView view, BuildingPlacementHandle handle)
        {
            Vector3 local = view.transform.localPosition;
            return _projection.ScreenToIso(new Vector2(local.x, local.y), handle.IsoHeight);
        }

        private Vector3 RoundPosition(Vector3 iso)
        {
            return new Vector3(
                Mathf.Round(iso.x / PositionPrecision) * PositionPrecision,
                Mathf.Round(iso.y / PositionPrecision) * PositionPrecision,
                Mathf.Round(iso.z / PositionPrecision) * PositionPrecision);
        }

        private bool IsDifferent(BuildingItemPlacement placement, Vector3 iso, BuildingPlacementHandle handle, BuildingChildOverride[] overrides)
        {
            return (placement.IsoPosition - iso).sqrMagnitude > ChangeEpsilon
                   || placement.Flipped != handle.Flipped
                   || placement.SortingLayer != handle.SortingLayer
                   || OverridesDiffer(placement.ChildOverrides, overrides);
        }

        private void SetCloudsRendered(bool rendered)
        {
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world != null)
            {
                world.SetCloudsRendered(rendered);
            }
        }

        private Transform FindItemsRoot()
        {
            BuildingWorldView world = UnityEngine.Object.FindObjectOfType<BuildingWorldView>();

            if (world == null || world.ItemsRoot == null)
            {
                throw new InvalidOperationException("Open the Building scene first");
            }

            return world.ItemsRoot;
        }

        private BuildingItemCatalog LoadCatalog()
        {
            BuildingItemCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingItemCatalog>(BuildingAssetPaths.CatalogPath);

            if (catalog == null)
            {
                throw new InvalidOperationException(BuildingAssetPaths.CatalogPath);
            }

            return catalog;
        }
    }
}
