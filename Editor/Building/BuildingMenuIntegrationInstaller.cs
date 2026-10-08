using System;
using System.Collections.Generic;
using System.IO;
using _Project.Composition;
using _Project.Core.Building;
using _Project.UI.Common;
using _Project.UI.Gameplay.FinishLevels;
using _Project.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Editor.Building
{
    public class BuildingMenuIntegrationInstaller
    {
        private const string RootPrefabPath = "Assets/_Project/Prefabs/RootLifetimeScope.prefab";
        private const string MenuScenePath = "Assets/_Project/Scenes/Menu.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string NavBarPath = "UICanvas/NavBar";
        private const string NavBuildingButtonName = "BuildingNavButton";
        private const string HomePanelPath = "UICanvas/TabHub/Viewport/TabsContent/HomePanel";
        private const string PlayButtonName = "PlayButton";
        private const string PlayFrameName = "Frame";
        private const string HomeBuildingButtonName = "BuildingButton";
        private const string ResourceBarPath = "UICanvas/ResourceBar";
        private const string LivesBarName = "LivesButton";
        private const string GoldBarName = "GoldButton";
        private const string ResourceValueName = "ValueText";
        private const string GemsBarName = "GemsBar";
        private const string GemsBarSpriteName = "2D_Default_resource_bar_Gem";
        private const string InnerButtonName = "Button";
        private const string LabelsName = "Labels";
        private const string LabelName = "LabelText";
        private const string HintName = "LockedHint";
        private const string LockName = "Lock";
        private const string UnlockedSpriteName = "2D_Outgame_Home_Building_button";
        private const string LockedSpriteName = "2D_Outgame_Home_Building_button_disable";
        private const string PlainSuffix = "_plain";
        private const string LockIconPath = "Assets/_Project/Art/Sprites/UI/2D_Frame_Lock.png";
        private const string GemIconPath = BuildingUiSpriteImporter.UiSpritesFolder + "/2D_Outgame_Building_Popup_Task_gem_icon.png";
        private const string FontPath = "Assets/_Project/Art/Fonts/LilitaOneRus_SDF.asset";
        private const string WinSafeAreaPath = "WinShowcase/'[Safe_Area]'";
        private const string GemsRewardName = "GemsReward";
        private const string GemsRewardIconName = "Icon";
        private const string WinTopBarsName = "TopBars";
        private const string WinGemBarName = "GemBar";
        private const string WinBarTargetName = "Target";
        private const string WinGemsTextName = "GemsText";
        private const string FlySpriteProperty = "_coinSprite";
        private const string ClickSoundProperty = "_clickSound";
        private const float HomeButtonY = 388f;
        private const float HomeButtonOffsetX = 270f;
        private const float HomeInnerOffsetY = 6.5f;
        private static readonly Vector2 HomeButtonSize = new Vector2(461f, 217f);
        private static readonly Vector2 HomeInnerSize = new Vector2(427f, 177f);
        private const float HomeLabelMinFontSize = 30f;
        private static readonly Vector2 HomeLabelPosition = new Vector2(0f, 2f);
        private static readonly Vector2 HomeLabelsSize = new Vector2(380f, 150f);
        private const float HomeLabelHeight = 100f;
        private const float HomeHintHeight = 34f;
        private const float HomeHintFontSize = 26f;
        private const float HomeLabelsSpacing = -10f;
        private static readonly Vector2 HomeLockPosition = new Vector2(150f, 50f);
        private static readonly Vector2 LockSize = new Vector2(64f, 64f);
        private static readonly Vector2 GemsRewardPosition = new Vector2(0f, 470f);
        private static readonly Vector2 WinTopBarsSize = new Vector2(900f, 138f);
        private const float WinTopBarsSpacing = 30f;
        private static readonly Vector2 WinGemBarSize = new Vector2(303f, 138f);
        private static readonly Vector2 WinBarTargetPosition = new Vector2(-73f, 0f);
        private static readonly Vector2 WinBarTargetSize = new Vector2(100f, 100f);
        private static readonly Vector2 WinGemsTextPosition = new Vector2(62f, 2f);
        private static readonly Vector2 WinGemsTextSize = new Vector2(150f, 90f);
        private const float WinGemsTextFontSize = 56f;
        private const float WinGemsTextMinFontSize = 26f;
        private const float LivesBarAnchorX = 0.16f;
        private const float GoldBarAnchorX = 0.455f;
        private const float GemsBarAnchorX = 0.75f;
        private static readonly Vector2 GemsBarSize = new Vector2(230f, 92f);
        private static readonly Vector4 GemsBarBorder = new Vector4(134f, 0f, 52f, 0f);
        private static readonly Vector2 GemsValuePosition = new Vector2(46f, 2f);
        private static readonly Vector2 GemsValueSize = new Vector2(120f, 80f);
        private const float GemsValueFontSize = 40f;
        private const float GemsValueMinFontSize = 20f;
        private const float GemsValueMaxFontSize = 43f;
        private static readonly Color GemsValueColor = new Color(0.4f, 0.02f, 0.19f, 1f);

        private readonly BuildingImportReport _report;

        public BuildingMenuIntegrationInstaller(BuildingImportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Install()
        {
            BuildingBalanceConfig balance = AssetDatabase.LoadAssetAtPath<BuildingBalanceConfig>(BuildingAssetPaths.BalancePath);

            if (balance == null)
            {
                throw new InvalidOperationException("Run the Area 0 import first: balance config missing");
            }

            Dictionary<string, Sprite> uiSprites = new BuildingUiSpriteImporter(new ReferenceExport(), _report).Import();
            new BuildingLocalizationImporter(_report).ImportStaticKeys();
            InstallRootConfig(balance);
            InstallMenu(uiSprites);
            InstallWinReward(uiSprites);
            AssetDatabase.SaveAssets();
        }

        private void InstallRootConfig(BuildingBalanceConfig balance)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RootPrefabPath);

            try
            {
                RootLifetimeScope scope = root.GetComponent<RootLifetimeScope>();
                SerializedObject serialized = new SerializedObject(scope);
                serialized.FindProperty("_buildingBalanceConfig").objectReferenceValue = balance;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                _report.Count("root prefab: balance config assigned");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void InstallMenu(Dictionary<string, Sprite> uiSprites)
        {
            Scene scene = OpenAdditive(MenuScenePath, out bool opened);

            try
            {
                RemoveNavButton(scene);
                InstallHomeButton(scene, uiSprites);
                InstallGemsBar(scene, uiSprites);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private void RemoveNavButton(Scene scene)
        {
            GameObject navBar = FindInScene(scene, NavBarPath);
            Transform existing = navBar.transform.Find(NavBuildingButtonName);

            if (existing == null)
            {
                return;
            }

            MenuNavButton navButton = existing.GetComponent<MenuNavButton>();
            MenuNavBarView navBarView = navBar.GetComponent<MenuNavBarView>();
            SerializedObject barSerialized = new SerializedObject(navBarView);
            SerializedProperty buttons = barSerialized.FindProperty("_buttons");
            List<MenuNavButton> kept = new List<MenuNavButton>();

            for (int i = 0; i < buttons.arraySize; i++)
            {
                MenuNavButton current = buttons.GetArrayElementAtIndex(i).objectReferenceValue as MenuNavButton;

                if (current != null && current != navButton)
                {
                    kept.Add(current);
                }
            }

            buttons.arraySize = kept.Count;

            for (int i = 0; i < kept.Count; i++)
            {
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = kept[i];
            }

            barSerialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
            _report.Count("menu: Building nav button removed");
        }

        private void InstallHomeButton(Scene scene, Dictionary<string, Sprite> uiSprites)
        {
            GameObject homePanel = FindInScene(scene, HomePanelPath);
            Transform play = homePanel.transform.Find(PlayButtonName);

            if (play == null)
            {
                throw new InvalidOperationException("Home play button not found: " + HomePanelPath + "/" + PlayButtonName);
            }

            RectTransform playRect = (RectTransform)play;
            playRect.localScale = Vector3.one;
            playRect.sizeDelta = HomeButtonSize;
            playRect.anchoredPosition = new Vector2(-HomeButtonOffsetX, HomeButtonY);

            RectTransform playFrame = play.Find(PlayFrameName) as RectTransform;

            if (playFrame != null)
            {
                playFrame.sizeDelta = HomeInnerSize;
                playFrame.anchoredPosition = new Vector2(0f, HomeButtonSize.y * 0.5f + HomeInnerOffsetY);
            }

            Transform existing = homePanel.transform.Find(HomeBuildingButtonName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            Image playImage = play.GetComponent<Image>();
            TMP_Text playLabel = play.GetComponentInChildren<TMP_Text>(true);

            RectTransform root = CreateRect(HomeBuildingButtonName, homePanel.transform, new Vector2(0.5f, 0f), new Vector2(HomeButtonOffsetX, HomeButtonY), HomeButtonSize);
            root.SetSiblingIndex(play.GetSiblingIndex() + 1);
            Image frame = root.gameObject.AddComponent<Image>();
            frame.sprite = playImage != null ? playImage.sprite : null;
            frame.type = playImage != null ? playImage.type : Image.Type.Simple;
            frame.raycastTarget = false;

            RectTransform inner = CreateRect(InnerButtonName, root, new Vector2(0.5f, 0.5f), new Vector2(0f, HomeInnerOffsetY), HomeInnerSize);
            Sprite unlockedSprite = BakePlainButton(Sprite(uiSprites, UnlockedSpriteName));
            Sprite lockedSprite = BakePlainButton(Sprite(uiSprites, LockedSpriteName));
            Image innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.sprite = unlockedSprite;
            innerImage.type = Image.Type.Simple;
            innerImage.preserveAspect = true;
            Button button = inner.gameObject.AddComponent<Button>();
            button.targetGraphic = innerImage;
            CopyClickSound(play, inner.gameObject);

            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;

            RectTransform labels = CreateRect(LabelsName, inner, new Vector2(0.5f, 0.5f), HomeLabelPosition, HomeLabelsSize);
            VerticalLayoutGroup layout = labels.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = HomeLabelsSpacing;

            TextMeshProUGUI label = CreateHomeLabel(LabelName, labels, playLabel, HomeLabelHeight, "СТРОЙКА");
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = HomeLabelMinFontSize;

            TextMeshProUGUI hint = CreateHomeLabel(HintName, labels, playLabel, HomeHintHeight, "Разблок. после 3 уровня");
            hint.enableAutoSizing = false;
            hint.fontSize = HomeHintFontSize;

            RectTransform lockIcon = CreateImage(LockName, inner, AssetDatabase.LoadAssetAtPath<Sprite>(LockIconPath), HomeLockPosition, LockSize);

            BuildingHomeButtonView view = root.gameObject.AddComponent<BuildingHomeButtonView>();
            view.Initialize(button, innerImage, unlockedSprite, lockedSprite, label, hint, lockIcon.gameObject);
            _report.Count("menu: home Building button installed");
        }

        private void InstallGemsBar(Scene scene, Dictionary<string, Sprite> uiSprites)
        {
            GameObject bar = FindInScene(scene, ResourceBarPath);
            RectTransform lives = bar.transform.Find(LivesBarName) as RectTransform;
            RectTransform gold = bar.transform.Find(GoldBarName) as RectTransform;

            if (lives == null || gold == null)
            {
                throw new InvalidOperationException("Resource bar buttons not found: " + ResourceBarPath);
            }

            SetAnchorX(lives, LivesBarAnchorX);
            SetAnchorX(gold, GoldBarAnchorX);

            Transform existing = bar.transform.Find(GemsBarName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            Sprite barSprite = EnsureSpriteBorder(Sprite(uiSprites, GemsBarSpriteName), GemsBarBorder);
            RectTransform root = CreateRect(GemsBarName, bar.transform, new Vector2(GemsBarAnchorX, 0.5f), Vector2.zero, GemsBarSize);
            root.SetSiblingIndex(gold.GetSiblingIndex() + 1);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = barSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            TMP_Text goldValue = gold.GetComponentInChildren<TMP_Text>(true);
            RectTransform valueRect = CreateRect(ResourceValueName, root, new Vector2(0.5f, 0.5f), GemsValuePosition, GemsValueSize);
            TextMeshProUGUI value = valueRect.gameObject.AddComponent<TextMeshProUGUI>();
            CopyTextStyle(goldValue, value);
            value.color = GemsValueColor;
            value.fontSize = GemsValueFontSize;
            value.enableAutoSizing = true;
            value.fontSizeMin = GemsValueMinFontSize;
            value.fontSizeMax = GemsValueMaxFontSize;
            value.alignment = TextAlignmentOptions.Center;
            value.enableWordWrapping = false;
            value.text = "0";
            value.raycastTarget = false;

            GemsView view = root.gameObject.AddComponent<GemsView>();
            view.Initialize(value);
            _report.Count("menu: gems bar installed");
        }

        private void SetAnchorX(RectTransform rect, float anchorX)
        {
            rect.anchorMin = new Vector2(anchorX, rect.anchorMin.y);
            rect.anchorMax = new Vector2(anchorX, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }

        private Sprite EnsureSpriteBorder(Sprite sprite, Vector4 border)
        {
            if (sprite == null)
            {
                return null;
            }

            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null || importer.spriteBorder == border)
            {
                return sprite;
            }

            importer.spriteBorder = border;
            importer.SaveAndReimport();
            _report.Count("sprite borders set");
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private Sprite BakePlainButton(Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string targetPath = Path.GetDirectoryName(sourcePath).Replace(Path.DirectorySeparatorChar, '/') + "/" + Path.GetFileNameWithoutExtension(sourcePath) + PlainSuffix + ".png";
            Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            decoded.LoadImage(File.ReadAllBytes(sourcePath));

            int width = decoded.width;
            int height = decoded.height;
            int half = width / 2;
            Color32[] pixels = decoded.GetPixels32();
            Color32[] mirrored = new Color32[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceX = x < half ? x : width - 1 - x;
                    mirrored[y * width + x] = pixels[y * width + sourceX];
                }
            }

            Texture2D baked = new Texture2D(width, height, TextureFormat.RGBA32, false);
            baked.SetPixels32(mirrored);
            File.WriteAllBytes(targetPath, baked.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(decoded);
            UnityEngine.Object.DestroyImmediate(baked);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = sourceImporter != null ? sourceImporter.spritePixelsPerUnit : 100f;
                importer.spritePivot = sourceImporter != null ? sourceImporter.spritePivot : new Vector2(0.5f, 0.5f);
                importer.SaveAndReimport();
            }

            _report.Count("plain button sprites baked");
            return AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
        }

        private void CopyClickSound(Transform source, GameObject target)
        {
            ClickSoundButton sourceSound = source.GetComponent<ClickSoundButton>();

            if (sourceSound == null)
            {
                return;
            }

            ClickSoundButton sound = target.AddComponent<ClickSoundButton>();
            SerializedObject from = new SerializedObject(sourceSound);
            SerializedObject to = new SerializedObject(sound);
            to.FindProperty(ClickSoundProperty).objectReferenceValue = from.FindProperty(ClickSoundProperty).objectReferenceValue;
            to.ApplyModifiedPropertiesWithoutUndo();
        }

        private TextMeshProUGUI CreateHomeLabel(string name, RectTransform parent, TMP_Text style, float height, string text)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(HomeLabelsSize.x, height));
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            CopyTextStyle(style, label);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        private void CopyTextStyle(TMP_Text source, TMP_Text target)
        {
            if (source == null)
            {
                target.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                target.fontSize = 52f;
                return;
            }

            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
            target.fontSize = source.fontSize;
            target.enableAutoSizing = source.enableAutoSizing;
            target.fontSizeMin = source.fontSizeMin;
            target.fontSizeMax = source.fontSizeMax;
            target.fontStyle = source.fontStyle;
            target.color = source.color;
        }

        private void InstallWinReward(Dictionary<string, Sprite> uiSprites)
        {
            Scene scene = OpenAdditive(GameScenePath, out bool opened);

            try
            {
                WinWindow winWindow = FindWinWindow(scene);
                GameObject safeArea = FindWinSafeArea(winWindow);
                Transform existing = safeArea.transform.Find(GemsRewardName);

                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                UiFactory factory = new UiFactory(font, null);
                Sprite gemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);

                RectTransform root = factory.CreateRect(GemsRewardName, safeArea.transform, RectAnchor.Center, GemsRewardPosition, new Vector2(300f, 90f));
                Image icon = factory.CreateImage(GemsRewardIconName, root, gemIcon, RectAnchor.Center, new Vector2(-90f, 0f), new Vector2(72f, 72f));
                TextMeshProUGUI amount = factory.CreateLabel("Amount", root, "+100", 52f, RectAnchor.Center, new Vector2(40f, 0f), new Vector2(200f, 90f), TextAlignmentOptions.Left);
                amount.color = new Color(0.98f, 0.55f, 1f, 1f);

                RectTransform topBars = InstallWinTopBars(winWindow);
                Sprite barSprite = EnsureSpriteBorder(Sprite(uiSprites, GemsBarSpriteName), GemsBarBorder);
                Image gemBar = factory.CreateImage(WinGemBarName, topBars, barSprite, RectAnchor.Center, Vector2.zero, WinGemBarSize);
                RectTransform target = factory.CreateRect(WinBarTargetName, gemBar.transform, RectAnchor.Center, WinBarTargetPosition, WinBarTargetSize);
                TextMeshProUGUI gemsText = factory.CreateLabel(WinGemsTextName, gemBar.transform, "0", WinGemsTextFontSize, RectAnchor.Center, WinGemsTextPosition, WinGemsTextSize, TextAlignmentOptions.Center);
                gemsText.enableWordWrapping = false;
                gemsText.enableAutoSizing = true;
                gemsText.fontSizeMax = WinGemsTextFontSize;
                gemsText.fontSizeMin = WinGemsTextMinFontSize;

                WinRewardPresenter presenter = winWindow.GetComponent<WinRewardPresenter>();
                CoinFlyEffect coinFly = winWindow.GetComponent<CoinFlyEffect>();

                if (presenter == null || coinFly == null)
                {
                    throw new InvalidOperationException("WinRewardPresenter or CoinFlyEffect not found on " + winWindow.name);
                }

                CoinFlyEffect gemFly = root.gameObject.AddComponent<CoinFlyEffect>();
                EditorUtility.CopySerialized(coinFly, gemFly);
                SerializedObject flySerialized = new SerializedObject(gemFly);
                flySerialized.FindProperty(FlySpriteProperty).objectReferenceValue = gemIcon;
                flySerialized.ApplyModifiedPropertiesWithoutUndo();

                WinGemsRewardView view = root.gameObject.AddComponent<WinGemsRewardView>();
                view.Initialize(root.gameObject, amount, gemBar.gameObject, gemsText, presenter, gemFly, icon.rectTransform, target);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                _report.Count("game: win gems reward view installed");
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private RectTransform InstallWinTopBars(WinWindow winWindow)
        {
            PiggyBankView coinBarView = winWindow.GetComponentInChildren<PiggyBankView>(true);

            if (coinBarView == null)
            {
                throw new InvalidOperationException("Win coin bar (PiggyBankView) not found under " + winWindow.name);
            }

            RectTransform coinBar = (RectTransform)coinBarView.transform;
            Transform existingBars = winWindow.transform.Find(WinTopBarsName);
            RectTransform topBars;

            if (existingBars != null)
            {
                topBars = (RectTransform)existingBars;
                Transform oldGemBar = topBars.Find(WinGemBarName);

                if (oldGemBar != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldGemBar.gameObject);
                }
            }
            else
            {
                topBars = new GameObject(WinTopBarsName, typeof(RectTransform)).GetComponent<RectTransform>();
                topBars.gameObject.layer = winWindow.gameObject.layer;
                topBars.SetParent(winWindow.transform, false);
                topBars.SetSiblingIndex(coinBar.GetSiblingIndex());
                topBars.anchorMin = new Vector2(0.5f, 0.5f);
                topBars.anchorMax = new Vector2(0.5f, 0.5f);
                topBars.pivot = new Vector2(0.5f, 0.5f);
                topBars.anchoredPosition = coinBar.anchoredPosition;
                topBars.sizeDelta = WinTopBarsSize;

                HorizontalLayoutGroup layout = topBars.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = WinTopBarsSpacing;
            }

            if (coinBar.parent != topBars)
            {
                coinBar.SetParent(topBars, false);
                coinBar.SetAsFirstSibling();
            }

            return topBars;
        }

        private WinWindow FindWinWindow(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                WinWindow winWindow = root.GetComponentInChildren<WinWindow>(true);

                if (winWindow != null)
                {
                    return winWindow;
                }
            }

            throw new InvalidOperationException("Win window not found in scene " + scene.name);
        }

        private GameObject FindWinSafeArea(WinWindow winWindow)
        {
            Transform safeArea = winWindow.transform.Find(WinSafeAreaPath);

            if (safeArea == null)
            {
                throw new InvalidOperationException("Win window safe area not found under " + winWindow.name);
            }

            return safeArea.gameObject;
        }

        private Sprite Sprite(Dictionary<string, Sprite> uiSprites, string name)
        {
            if (uiSprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }

            _report.Warn("UI sprite missing: " + name);
            return null;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private RectTransform CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        private Scene OpenAdditive(string path, out bool opened)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            opened = false;

            if (scene.isLoaded == false)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                opened = true;
            }

            return scene;
        }

        private GameObject FindInScene(Scene scene, string path)
        {
            string[] parts = path.Split('/');

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0])
                {
                    continue;
                }

                Transform current = root.transform;

                for (int i = 1; i < parts.Length && current != null; i++)
                {
                    current = current.Find(parts[i]);
                }

                if (current != null)
                {
                    return current.gameObject;
                }
            }

            throw new InvalidOperationException("Object not found in scene " + scene.name + ": " + path);
        }
    }
}
