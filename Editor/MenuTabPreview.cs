using _Project.Core.Menu;
using _Project.UI.Menu;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor
{
    public static class MenuTabPreview
    {
        private const float FallbackPanelWidth = 1080f;

        [MenuItem("Tools/Menu Tabs/Магазин")]
        public static void PreviewShop()
        {
            Preview(MenuTab.Shop);
        }

        [MenuItem("Tools/Menu Tabs/Главный экран")]
        public static void PreviewHome()
        {
            Preview(MenuTab.Home);
        }

        [MenuItem("Tools/Menu Tabs/Лидерборд")]
        public static void PreviewLeaderboard()
        {
            Preview(MenuTab.Leaderboard);
        }

        private static void Preview(MenuTab tab)
        {
            ScrollHub(tab);
            SelectNavButton(tab);
        }

        private static void ScrollHub(MenuTab tab)
        {
            MenuTabHubView hub = Object.FindObjectOfType<MenuTabHubView>(true);

            if (hub == null)
            {
                Debug.LogWarning("MenuTabPreview: MenuTabHubView не найден — открой сцену Menu.");
                return;
            }

            SerializedObject so = new SerializedObject(hub);
            RectTransform content = so.FindProperty("_content").objectReferenceValue as RectTransform;
            RectTransform viewport = so.FindProperty("_viewport").objectReferenceValue as RectTransform;
            SerializedProperty order = so.FindProperty("_tabOrder");

            if (content == null || order == null)
            {
                Debug.LogWarning("MenuTabPreview: у MenuTabHubView не назначены _content/_tabOrder.");
                return;
            }

            int index = 0;

            for (int i = 0; i < order.arraySize; i++)
            {
                if (order.GetArrayElementAtIndex(i).enumValueIndex == (int)tab)
                {
                    index = i;
                    break;
                }
            }

            float width = viewport != null && viewport.rect.width > 1f ? viewport.rect.width : FallbackPanelWidth;

            Undo.RecordObject(content, "Preview Menu Tab");
            content.anchoredPosition = new Vector2(-index * width, content.anchoredPosition.y);
            EditorUtility.SetDirty(content);
        }

        private static void SelectNavButton(MenuTab tab)
        {
            MenuNavBarView nav = Object.FindObjectOfType<MenuNavBarView>(true);

            if (nav == null)
            {
                return;
            }

            nav.EditorPreviewSelect(tab);
            EditorUtility.SetDirty(nav);
        }
    }
}
