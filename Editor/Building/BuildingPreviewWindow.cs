using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class BuildingPreviewWindow : EditorWindow
    {
        private const string WindowTitle = "Building Preview";
        private const int DialogApplyAndRebuild = 0;
        private const int DialogRebuild = 1;

        [MenuItem("Tools/Goods Sorting/Building/Preview Window")]
        public static void Open()
        {
            GetWindow<BuildingPreviewWindow>(WindowTitle);
        }

        private void OnGUI()
        {
            BuildingAreaPreview preview = new BuildingAreaPreview();

            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            DrawState(preview, false, "Before (broken)");
            DrawState(preview, true, "After (built)");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(preview.TryFindRoot(out _) == false))
            {
                if (GUILayout.Button("Re-sort depth"))
                {
                    preview.Resort();
                }

                if (GUILayout.Button("Apply scene to configs"))
                {
                    preview.ApplyToConfig();
                }

                if (GUILayout.Button("Clear preview"))
                {
                    preview.Clear();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Move items in the Scene view; depth re-sorts live. Edits are written to the area configs "
                + "automatically when you press Play from this scene, or with Apply. Rebuilding a state from configs "
                + "discards unsaved edits of that state.",
                MessageType.Info);
        }

        private void DrawState(BuildingAreaPreview preview, bool after, string label)
        {
            bool exists = preview.HasState(after);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(exists == false))
                {
                    bool visible = preview.IsStateVisible(after);
                    bool requested = EditorGUILayout.ToggleLeft(label, visible, GUILayout.Width(160f));

                    if (requested != visible)
                    {
                        preview.SetStateVisible(after, requested);
                    }
                }

                if (GUILayout.Button(exists ? "Rebuild from configs" : "Build from configs"))
                {
                    Build(preview, after, exists, label);
                }
            }
        }

        private void Build(BuildingAreaPreview preview, bool after, bool exists, string label)
        {
            if (exists == false)
            {
                BuildState(preview, after);
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                WindowTitle,
                "Rebuild \"" + label + "\" from configs? Unsaved scene edits of this state will be lost.",
                "Apply first, then rebuild",
                "Rebuild",
                "Cancel");

            if (choice == DialogApplyAndRebuild)
            {
                preview.ApplyToConfig();
                BuildState(preview, after);
            }
            else if (choice == DialogRebuild)
            {
                BuildState(preview, after);
            }
        }

        private void BuildState(BuildingAreaPreview preview, bool after)
        {
            if (after)
            {
                preview.ShowFinal();
            }
            else
            {
                preview.ShowInitial();
            }
        }
    }
}
