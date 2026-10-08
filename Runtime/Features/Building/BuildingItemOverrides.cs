using System.Collections.Generic;
using _Project.Core.Building;
using UnityEngine;

namespace _Project.Features.Building
{
    public static class BuildingItemOverrides
    {
        public static void Apply(BuildingItemView view, IReadOnlyList<BuildingChildOverride> overrides)
        {
            for (int i = 0; i < overrides.Count; i++)
            {
                BuildingChildOverride change = overrides[i];
                Transform child = Resolve(view.transform, change.Path);

                if (child == null)
                {
                    continue;
                }

                if (change.HasTransform)
                {
                    child.localPosition = change.LocalPosition;
                    child.localEulerAngles = change.LocalEulerAngles;
                    child.localScale = change.LocalScale;
                }

                if (change.HasActive)
                {
                    child.gameObject.SetActive(change.Active);
                }

                if (change.HasRenderer)
                {
                    Renderer renderer = child.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        renderer.enabled = change.RendererEnabled;
                        renderer.sortingOrder = change.SortingOrder;
                    }
                }
            }
        }

        public static Transform Resolve(Transform root, IReadOnlyList<int> path)
        {
            Transform current = root;

            for (int i = 0; i < path.Count; i++)
            {
                int index = path[i];

                if (index < 0 || index >= current.childCount)
                {
                    return null;
                }

                current = current.GetChild(index);
            }

            return current;
        }

        public static int[] PathTo(Transform root, Transform child)
        {
            List<int> indices = new List<int>();
            Transform current = child;

            while (current != null && current != root)
            {
                indices.Insert(0, current.GetSiblingIndex());
                current = current.parent;
            }

            return current == root ? indices.ToArray() : null;
        }
    }
}
