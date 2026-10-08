using System;
using System.Collections.Generic;
using _Project.Features.Building;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Building
{
    [InitializeOnLoad]
    public static class BuildingPreviewSync
    {
        private const double CheckIntervalSeconds = 0.15;

        private static readonly Dictionary<int, HandleState> States = new Dictionary<int, HandleState>();
        private static double _nextCheckTime;

        private readonly struct HandleState : IEquatable<HandleState>
        {
            private readonly bool _flipped;
            private readonly int _layer;
            private readonly float _height;

            public HandleState(BuildingPlacementHandle handle)
            {
                _flipped = handle.Flipped;
                _layer = handle.SortingLayer;
                _height = handle.IsoHeight;
            }

            public bool Equals(HandleState other)
            {
                return _flipped == other._flipped && _layer == other._layer && _height.Equals(other._height);
            }

            public override bool Equals(object obj)
            {
                return obj is HandleState other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (_flipped ? 1 : 0) ^ (_layer * 397) ^ _height.GetHashCode();
            }
        }

        static BuildingPreviewSync()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            BuildingAreaPreview preview = new BuildingAreaPreview();

            if (preview.TryFindRoot(out Transform root) == false)
            {
                return;
            }

            preview.ApplyToConfig();
        }

        private static void OnEditorUpdate()
        {
            if (Application.isPlaying || EditorApplication.timeSinceStartup < _nextCheckTime)
            {
                return;
            }

            _nextCheckTime = EditorApplication.timeSinceStartup + CheckIntervalSeconds;
            BuildingAreaPreview preview = new BuildingAreaPreview();

            if (preview.TryFindRoot(out Transform root) == false)
            {
                States.Clear();
                return;
            }

            BuildingPlacementHandle[] handles = root.GetComponentsInChildren<BuildingPlacementHandle>();

            if (HasChanges(handles) == false)
            {
                return;
            }

            preview.Resort();
            ResetChangeFlags(handles);
        }

        private static bool HasChanges(BuildingPlacementHandle[] handles)
        {
            bool changed = false;

            foreach (BuildingPlacementHandle handle in handles)
            {
                if (handle.transform.hasChanged)
                {
                    changed = true;
                }

                int id = handle.GetInstanceID();
                HandleState current = new HandleState(handle);

                if (States.TryGetValue(id, out HandleState previous) == false || previous.Equals(current) == false)
                {
                    States[id] = current;
                    changed = true;
                }
            }

            return changed;
        }

        private static void ResetChangeFlags(BuildingPlacementHandle[] handles)
        {
            foreach (BuildingPlacementHandle handle in handles)
            {
                handle.transform.hasChanged = false;
            }
        }
    }
}
