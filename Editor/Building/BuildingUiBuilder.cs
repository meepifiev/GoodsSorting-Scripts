using System;
using System.Collections.Generic;
using System.IO;
using _Project.Composition;
using _Project.Core.Audio;
using _Project.Core.Building;
using _Project.Features.Building;
using _Project.UI.Building;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Editor.Building
{
    public class BuildingUiBuilder
    {
        private const string CanvasName = "UICanvas";
        private const string EventSystemName = "EventSystem";
        private const string ScopeName = "BuildingLifetimeScope";
        private const string FxName = "Fx";
        private const string WorldCanvasName = "WorldCanvas";
        private const int WorldCanvasSortingOrder = 500;
        private const float WorldCanvasScale = 0.005f;
        private const float MarkerHeightOffset = 0f;
        private static readonly Vector2 MarkerSize = new Vector2(200f, 235f);
        private const string FontPath = "Assets/_Project/Art/Fonts/LilitaOneRus_SDF.asset";
        private const string ClickSoundPath = "Assets/_Project/Configs/Audio/ButtunClickSound.asset";
        private const string BuildSoundPath = "Assets/_Project/Configs/Audio/SFX/Sfx_PutDownGood.asset";
        private const string DaySoundPath = "Assets/_Project/Configs/Audio/SFX/Sfx_Won.asset";
        private const string CloseButtonPath = "Assets/_Project/Art/Sprites/Menu/popup_close.png";
        private const string UiLayerName = "UI";
        private const float DimAlpha = 0.65f;
        private const int FxSortingOrder = 900;
        private const float ThemePopupHeight = 820f;
        private const float ThemeConfirmSlotOffset = 86f;
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        private readonly BuildingImportReport _report;
        private readonly Dictionary<string, Sprite> _refSprites;
        private readonly UiFactory _factory;
        private readonly AudioAsset _buildSound;
        private readonly AudioAsset _daySound;
        private readonly Sprite _closeSprite;

        public BuildingUiBuilder(BuildingImportReport report, Dictionary<string, Sprite> refSprites)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _refSprites = refSprites ?? throw new ArgumentNullException(nameof(refSprites));

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            if (font == null)
            {
                throw new FileNotFoundException(FontPath);
            }

            _factory = new UiFactory(font, AssetDatabase.LoadAssetAtPath<AudioAsset>(ClickSoundPath));
            _buildSound = AssetDatabase.LoadAssetAtPath<AudioAsset>(BuildSoundPath);
            _daySound = AssetDatabase.LoadAssetAtPath<AudioAsset>(DaySoundPath);
            _closeSprite = LoadProjectSprite(CloseButtonPath);
        }

        public void Build(Scene scene, BuildingWorldView world, BuildingWorldConfig areas, BuildingBalanceConfig balance, BuildingItemCatalog catalog)
        {
            RemoveGenerated(scene);

            BuildingItemAppearFx appearFx = CreateFx();
            GameObject canvas = CreateCanvas();
            BuildingHudView hud = CreateHud(canvas.transform);
            CreateWorldMarkers(world);
            Transform popups = _factory.CreateRect("Popups", canvas.transform, RectAnchor.Stretch, Vector2.zero, Vector2.zero);
            CreateThemePopup(popups);
            CreateNewDayBanner(popups);

            BuildingAudioPresenter audio = canvas.AddComponent<BuildingAudioPresenter>();
            audio.Initialize(_buildSound, _daySound);

            CreateEventSystem();
            CreateScope(world, appearFx, canvas, areas, balance, catalog);

            EditorSceneManager.MarkSceneDirty(scene);
            _report.Count("ui built");
        }

        private void RemoveGenerated(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == CanvasName || root.name == EventSystemName || root.name == ScopeName || root.name == FxName || root.name == WorldCanvasName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform));
            canvasObject.layer = LayerMask.NameToLayer(UiLayerName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private void CreateWorldMarkers(BuildingWorldView world)
        {
            GameObject canvasObject = new GameObject(WorldCanvasName, typeof(RectTransform));
            canvasObject.layer = LayerMask.NameToLayer(UiLayerName);
            canvasObject.transform.localScale = Vector3.one * WorldCanvasScale;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = world.CameraController != null ? world.CameraController.GetComponent<Camera>() : null;
            canvas.sortingOrder = WorldCanvasSortingOrder;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();

            Button button = _factory.CreateButton("TaskMarkerTemplate", canvasObject.transform, Sprite("2D_Outgame_Building_MainScene_task_button_2"), RectAnchor.Center, Vector2.zero, MarkerSize);
            _factory.CreateImage("GemIcon", button.transform, Sprite("2D_Outgame_Building_Popup_Task_gem_icon"), RectAnchor.Bottom, new Vector2(-46f, 36f), new Vector2(52f, 52f));
            TextMeshProUGUI cost = _factory.CreateLabel("Cost", button.transform, "0", 40f, RectAnchor.Bottom, new Vector2(26f, 36f), new Vector2(120f, 56f), TextAlignmentOptions.Center);

            BuildingTaskMarkerView template = button.gameObject.AddComponent<BuildingTaskMarkerView>();
            template.Initialize(button, cost);
            button.gameObject.SetActive(false);

            BuildingTaskMarkersView markers = canvasObject.AddComponent<BuildingTaskMarkersView>();
            markers.Initialize(template, group, MarkerHeightOffset);
        }

        private BuildingHudView CreateHud(Transform canvas)
        {
            RectTransform hud = _factory.CreateRect("HUD", canvas, RectAnchor.Stretch, Vector2.zero, Vector2.zero);

            Image gemBar = _factory.CreateImage("GemBar", hud, Sprite("2D_Default_resource_bar_Gem"), RectAnchor.TopRight, new Vector2(-40f, -60f), Vector2.zero);
            gemBar.rectTransform.pivot = new Vector2(1f, 1f);
            TextMeshProUGUI gemsLabel = _factory.CreateLabel("GemsLabel", gemBar.transform, "0", 44f, RectAnchor.Center, new Vector2(30f, 0f), new Vector2(200f, 80f), TextAlignmentOptions.Center);

            Image exitHole = _factory.CreateImage("ExitButtonHole", hud, Sprite("2D_Outgame_Building_MainScene_exit_button_hole"), RectAnchor.BottomRight, new Vector2(-40f, 40f), Vector2.zero);
            exitHole.rectTransform.pivot = new Vector2(1f, 0f);
            Button exitButton = _factory.CreateButton("ExitButton", exitHole.transform, Sprite("2D_Outgame_Building_MainScene_exit_button"), RectAnchor.Center, new Vector2(0f, 2f), Vector2.zero);

            BuildingHudView view = hud.gameObject.AddComponent<BuildingHudView>();
            view.Initialize(gemsLabel, exitButton);
            return view;
        }

        private void CreateThemePopup(Transform popups)
        {
            RectTransform root = _factory.CreateRect("ThemePopup", popups, RectAnchor.Stretch, Vector2.zero, Vector2.zero);
            _factory.CreateDim("Dim", root, DimAlpha);
            Image board = _factory.CreateImage("Content", root, Sprite("2D_Outgame_Building_Customize_board_9slice"), RectAnchor.Center, Vector2.zero, new Vector2(980f, ThemePopupHeight), true);
            CanvasGroup group = board.gameObject.AddComponent<CanvasGroup>();
            _factory.CreateLabel("Title", board.transform, "Choose a style", 52f, RectAnchor.Top, new Vector2(0f, -70f), new Vector2(800f, 100f), TextAlignmentOptions.Center, "building.theme.title");

            BuildingThemeOptionView[] options = new BuildingThemeOptionView[3];
            float[] xs = { -290f, 0f, 290f };

            for (int i = 0; i < options.Length; i++)
            {
                string optionName = "2D_Outgame_Building_Customize_option" + (i + 1);
                Sprite normal = Sprite(optionName);
                Sprite selected = Sprite(optionName + "_selected");
                Button button = _factory.CreateButton("Option" + (i + 1), board.transform, normal, RectAnchor.Center, new Vector2(xs[i], 20f), new Vector2(260f, 260f));
                Image preview = _factory.CreateImage("Preview", button.transform, null, RectAnchor.Center, new Vector2(0f, 10f), new Vector2(180f, 180f));
                BuildingThemeOptionView option = button.gameObject.AddComponent<BuildingThemeOptionView>();
                option.Initialize(i, button, button.image, preview, normal, selected);
                options[i] = option;
            }

            Button confirm = _factory.CreateButton("ConfirmButton", board.transform, Sprite("2D_Outgame_Building_Customize_agree_button"), RectAnchor.Center, Vector2.zero, Vector2.zero);
            RectTransform confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchoredPosition = new Vector2(0f, ThemeConfirmSlotOffset - ThemePopupHeight * 0.5f);
            Button close = _factory.CreateButton("CloseButton", board.transform, _closeSprite, RectAnchor.TopRight, new Vector2(-10f, 30f), new Vector2(110f, 110f));

            BuildingThemePopup popup = root.gameObject.AddComponent<BuildingThemePopup>();
            popup.InitializePopup(root.gameObject, board.rectTransform, group, close);
            popup.Initialize(options, confirm);
            root.gameObject.SetActive(false);
        }

        private void CreateNewDayBanner(Transform popups)
        {
            RectTransform root = _factory.CreateRect("NewDayBanner", popups, RectAnchor.Stretch, Vector2.zero, Vector2.zero);
            RectTransform content = _factory.CreateRect("Content", root, RectAnchor.Center, new Vector2(0f, 250f), new Vector2(900f, 400f));
            CanvasGroup group = content.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            _factory.CreateImage("Banner", content, Sprite("2D_Outgame_Building_NewDay"), RectAnchor.Center, new Vector2(0f, 90f), Vector2.zero);
            TextMeshProUGUI title = _factory.CreateLabel("Title", content, "Day 2", 60f, RectAnchor.Center, new Vector2(0f, -50f), new Vector2(800f, 100f), TextAlignmentOptions.Center);
            TextMeshProUGUI reward = _factory.CreateLabel("Reward", content, "", 40f, RectAnchor.Center, new Vector2(0f, -130f), new Vector2(900f, 100f), TextAlignmentOptions.Center);

            BuildingNewDayBanner banner = root.gameObject.AddComponent<BuildingNewDayBanner>();
            banner.Initialize(root.gameObject, group, content, title, reward);
            root.gameObject.SetActive(false);
        }

        private BuildingItemAppearFx CreateFx()
        {
            GameObject fxRoot = new GameObject(FxName);
            GameObject appear = new GameObject("ItemAppear");
            appear.transform.SetParent(fxRoot.transform, false);
            ParticleSystem particles = appear.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.7f;
            main.startSpeed = 2.5f;
            main.startSize = 0.18f;
            main.startColor = new Color(1f, 0.95f, 0.6f, 1f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystemRenderer renderer = appear.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            renderer.sortingOrder = FxSortingOrder;

            return appear.AddComponent<BuildingItemAppearFx>();
        }

        private void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject(EventSystemName);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void CreateScope(
            BuildingWorldView world,
            BuildingItemAppearFx appearFx,
            GameObject canvas,
            BuildingWorldConfig areas,
            BuildingBalanceConfig balance,
            BuildingItemCatalog catalog)
        {
            GameObject scopeObject = new GameObject(ScopeName);
            BuildingLifetimeScope scope = scopeObject.AddComponent<BuildingLifetimeScope>();
            SerializedObject serialized = new SerializedObject(scope);
            serialized.FindProperty("_areas").objectReferenceValue = areas;
            serialized.FindProperty("_balance").objectReferenceValue = balance;
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.FindProperty("_world").objectReferenceValue = world;
            serialized.FindProperty("_appearFx").objectReferenceValue = appearFx;
            SerializedProperty autoInject = serialized.FindProperty("autoInjectGameObjects");
            autoInject.arraySize = 1;
            autoInject.GetArrayElementAtIndex(0).objectReferenceValue = canvas;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            scopeObject.transform.SetAsFirstSibling();
        }

        private Sprite Sprite(string name)
        {
            if (_refSprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }

            _report.Warn("UI sprite missing: " + name);
            return null;
        }

        private Sprite LoadProjectSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                _report.Warn("Project sprite missing: " + path);
            }

            return sprite;
        }
    }
}
