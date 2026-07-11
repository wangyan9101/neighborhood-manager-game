using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Core;
using NeighborhoodManager.Models;
using NeighborhoodManager.Scene;
using NeighborhoodManager.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace NeighborhoodManager.Editor
{
    public static class CommunityManagerMvpSetup
    {
        private const string RootPath = "Assets/_Project";
        private const string BalancePath = RootPath + "/Configs/Balance/GameBalanceConfig.asset";
        private const string ScenePath = RootPath + "/Scenes/Game_MVP.unity";
        private const string FontPath = RootPath + "/Art/MicrosoftYaHei-Dynamic.asset";
        private const string TmpSettingsPath = RootPath + "/Resources/TMP Settings.asset";
        private const string EnvironmentRootName = "NeighborhoodManager_MVP";
        private static bool continueFullSetupAfterTmpImport;
        private static bool tmpImportPending;

        private static readonly string[] RequiredFolders =
        {
            "Art/Materials", "Art/Models", "Art/Sprites", "Art/Placeholder", "Audio",
            "Configs/Events", "Configs/Workers", "Configs/Facilities", "Configs/Balance",
            "Prefabs/UI", "Prefabs/Facilities", "Prefabs/Core", "Scenes", "Scripts/Core",
            "Scripts/Models", "Scripts/Configs", "Scripts/Systems", "Scripts/UI", "Scripts/Scene",
            "Scripts/Platform", "Scripts/Save", "Scripts/Utilities", "Editor", "Tests/EditMode", "Resources"
        };

        [MenuItem("Tools/Community Manager/Setup MVP Project")]
        public static void SetupMvpProject()
        {
            EnsureFolders();
            CreateDefaultConfigs();
            CreateMaterials();
            if (!EnsureTmpEssentialsReady(true))
            {
                Debug.Log("[Community Manager] 正在导入 TMP Essential Resources，导入完成后将自动继续 Setup。");
                return;
            }
            CreateMvpScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Community Manager] MVP 初始化完成。可重复执行，不会覆盖已有配置。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Community Manager/Create Default Configs")]
        public static void CreateDefaultConfigs()
        {
            EnsureFolders();
            CreateBalanceConfig();
            CreateEventConfigs();
            CreateWorkerConfigs();
            AssetDatabase.SaveAssets();
            Debug.Log("[Community Manager] 默认配置检查完成。");
        }

        [MenuItem("Tools/Community Manager/Create MVP Scene")]
        public static void CreateMvpScene()
        {
            EnsureFolders();
            CreateDefaultConfigs();
            CreateMaterials();
            if (!EnsureTmpEssentialsReady(false))
            {
                Debug.Log("[Community Manager] 正在导入 TMP Essential Resources，导入完成后将自动继续创建场景。");
                return;
            }

            UnityScene scene;
            GameObject existingRoot = null;
            if (File.Exists(Path.GetFullPath(ScenePath)))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                existingRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnvironmentRootName);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (existingRoot != null && existingRoot.GetComponentInChildren<GameRoot>(true) != null)
            {
                EnsureSceneInBuildSettings();
                Debug.Log("[Community Manager] Game_MVP 场景已存在，未重复创建。");
                return;
            }

            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            TMP_FontAsset font = GetOrCreateChineseFont();
            GameObject root = new GameObject(EnvironmentRootName);
            CreateWorld(root.transform, font);
            GameUIController ui = CreateUi(root.transform, font);

            GameObject gameRootObject = new GameObject("GameRoot");
            gameRootObject.transform.SetParent(root.transform);
            GameRoot gameRoot = gameRootObject.AddComponent<GameRoot>();
            gameRoot.Configure(
                AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath),
                LoadAssets<EventConfig>(RootPath + "/Configs/Events"),
                LoadAssets<WorkerConfig>(RootPath + "/Configs/Workers"),
                ui);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            Selection.activeObject = gameRootObject;
            Debug.Log("[Community Manager] 已创建 Game_MVP 场景并连接 GameRoot 与 UI。");
        }

        [MenuItem("Tools/Community Manager/Validate MVP Setup")]
        public static void ValidateMvpSetup()
        {
            var errors = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath) == null)
                errors.Add("缺少 GameBalanceConfig");
            if (LoadAssets<EventConfig>(RootPath + "/Configs/Events").Count != 5)
                errors.Add("事件配置数量不是 5");
            if (LoadAssets<WorkerConfig>(RootPath + "/Configs/Workers").Count != 3)
                errors.Add("员工配置数量不是 3");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                errors.Add("缺少 Game_MVP 场景");
            if (!EditorBuildSettings.scenes.Any(item => item.enabled && item.path == ScenePath))
                errors.Add("Game_MVP 未加入构建列表");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                UnityScene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameRoot gameRoot = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameRoot>(true)).FirstOrDefault();
                if (gameRoot == null) errors.Add("场景中缺少 GameRoot");
                else
                {
                    if (gameRoot.BalanceConfig == null || gameRoot.EventConfigs.Any(item => item == null)
                        || gameRoot.WorkerConfigs.Any(item => item == null)) errors.Add("GameRoot 存在空配置引用");
                    if (gameRoot.GameUI == null || !gameRoot.GameUI.HasRequiredReferences)
                        errors.Add("GameUI 引用不完整");
                }
            }

            if (errors.Count == 0)
                Debug.Log("[Community Manager] 验证通过：配置、场景、GameRoot、UI 和构建列表完整。");
            else
                Debug.LogError("[Community Manager] 验证失败：\n- " + string.Join("\n- ", errors));
        }

        public static void ImportTmpEssentialResourcesForBatch()
        {
            EnsureTmpEssentialsReady(false);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootPath)) AssetDatabase.CreateFolder("Assets", "_Project");
            foreach (string relativePath in RequiredFolders)
            {
                string current = RootPath;
                foreach (string segment in relativePath.Split('/'))
                {
                    string child = current + "/" + segment;
                    if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(current, segment);
                    current = child;
                }
            }
        }

        private static void CreateBalanceConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath) != null) return;
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(config, BalancePath);
        }

        private static void CreateEventConfigs()
        {
            CreateEvent("ELEVATOR_BROKEN", "电梯故障", "电梯停止运行，需要尽快维修。", GameEventType.Fault,
                FacilityType.Elevator, EventUrgency.Urgent, 32f, 12f, 300, WorkerType.Repairman,
                0, 5, -2, 8, 0, -8, 3, -8);
            CreateEvent("PARKING_OCCUPIED", "固定车位被占", "居民固定车位被其他车辆占用。", GameEventType.Security,
                FacilityType.ParkingLot, EventUrgency.Normal, 30f, 10f, 100, WorkerType.Security,
                0, 3, -1, 0, 0, -5, 2, 0);
            CreateEvent("NOISE_COMPLAINT", "居民噪音投诉", "居民投诉持续噪音影响休息。", GameEventType.Complaint,
                FacilityType.General, EventUrgency.Normal, 28f, 8f, 80, WorkerType.CustomerService,
                0, 2, -1, 0, 0, -4, 2, 0);
            CreateEvent("LOCKER_STUCK", "快递柜卡件", "快递柜仓门无法正常开启。", GameEventType.Fault,
                FacilityType.ExpressLocker, EventUrgency.Normal, 30f, 10f, 150, WorkerType.Repairman,
                0, 2, -1, 4, 0, -4, 2, -2);
            CreateEvent("CAMERA_OFFLINE", "摄像头离线", "公共区域摄像头失去连接。", GameEventType.Security,
                FacilityType.Camera, EventUrgency.Urgent, 26f, 11f, 200, WorkerType.Security,
                0, 2, -1, 6, 0, -3, 1, -5);
        }

        private static void CreateEvent(string id, string displayName, string description, GameEventType eventType,
            FacilityType facilityType, EventUrgency urgency, float pending, float duration, int cost,
            WorkerType recommendedWorker, int successBudget, int successSatisfaction, int successComplaint,
            int successHealth, int failureBudget, int failureSatisfaction, int failureComplaint, int failureHealth)
        {
            string path = $"{RootPath}/Configs/Events/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<EventConfig>(path) != null) return;
            EventConfig config = ScriptableObject.CreateInstance<EventConfig>();
            config.EventId = id;
            config.DisplayName = displayName;
            config.Description = description;
            config.EventType = eventType;
            config.FacilityType = facilityType;
            config.Urgency = urgency;
            config.PendingTimeLimit = pending;
            config.BaseHandleDuration = duration;
            config.BudgetCost = cost;
            config.RecommendedWorkerType = recommendedWorker;
            config.SuccessBudgetDelta = successBudget;
            config.SuccessSatisfactionDelta = successSatisfaction;
            config.SuccessComplaintDelta = successComplaint;
            config.SuccessFacilityHealthDelta = successHealth;
            config.FailureBudgetDelta = failureBudget;
            config.FailureSatisfactionDelta = failureSatisfaction;
            config.FailureComplaintDelta = failureComplaint;
            config.FailureFacilityHealthDelta = failureHealth;
            AssetDatabase.CreateAsset(config, path);
        }

        private static void CreateWorkerConfigs()
        {
            CreateWorker("repairman_01", "维修工", WorkerType.Repairman);
            CreateWorker("security_01", "保安", WorkerType.Security);
            CreateWorker("service_01", "客服", WorkerType.CustomerService);
        }

        private static void CreateWorker(string id, string displayName, WorkerType type)
        {
            string path = $"{RootPath}/Configs/Workers/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<WorkerConfig>(path) != null) return;
            WorkerConfig config = ScriptableObject.CreateInstance<WorkerConfig>();
            config.WorkerId = id;
            config.DisplayName = displayName;
            config.WorkerType = type;
            AssetDatabase.CreateAsset(config, path);
        }

        private static void CreateMaterials()
        {
            CreateMaterial("Ground", new Color(0.28f, 0.42f, 0.30f));
            CreateMaterial("Building", new Color(0.55f, 0.62f, 0.68f));
            CreateMaterial("Facility", new Color(0.18f, 0.48f, 0.72f));
            CreateMaterial("Accent", new Color(0.95f, 0.62f, 0.18f));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{RootPath}/Art/Materials/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { color = color, name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static TMP_FontAsset GetOrCreateChineseFont()
        {
            if (!IsTmpEssentialsReady())
                throw new InvalidOperationException("TMP Essential Resources 尚未导入完成。");
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset("Microsoft YaHei", "Regular", 48)
                ?? TMP_FontAsset.CreateFontAsset("SimHei", "Regular", 48);
            if (fontAsset == null)
            {
                Debug.LogWarning("[Community Manager] 未找到微软雅黑或黑体，暂用 TMP 默认字体；请按 README 配置中文字体。");
                return TMP_Settings.defaultFontAsset;
            }

            fontAsset.name = "MicrosoftYaHei-Dynamic";
            AssetDatabase.CreateAsset(fontAsset, FontPath);
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (Texture2D texture in fontAsset.atlasTextures)
                if (texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            TMP_Settings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static bool EnsureTmpEssentialsReady(bool continueFullSetup)
        {
            if (IsTmpEssentialsReady()) return true;
            continueFullSetupAfterTmpImport |= continueFullSetup;
            if (tmpImportPending) return false;

            string packagePath = GetTmpEssentialPackagePath();
            if (!File.Exists(packagePath))
                throw new FileNotFoundException("找不到 TMP Essential Resources 包。", packagePath);

            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null)
                AssetDatabase.DeleteAsset(TmpSettingsPath);
            tmpImportPending = true;
            AssetDatabase.importPackageCompleted += OnTmpPackageImported;
            AssetDatabase.importPackageFailed += OnTmpPackageImportFailed;
            AssetDatabase.ImportPackage(packagePath, false);
            return false;
        }

        private static bool IsTmpEssentialsReady()
        {
            return TMP_Settings.instance != null && Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
        }

        private static void OnTmpPackageImported(string packageName)
        {
            UnsubscribeTmpImportCallbacks();
            AssetDatabase.Refresh();
            if (!IsTmpEssentialsReady())
            {
                Debug.LogError("[Community Manager] TMP Essential Resources 导入完成但资源仍不可用。");
                return;
            }

            bool runFullSetup = continueFullSetupAfterTmpImport;
            continueFullSetupAfterTmpImport = false;
            if (runFullSetup) SetupMvpProject();
            else CreateMvpScene();
        }

        private static void OnTmpPackageImportFailed(string packageName, string errorMessage)
        {
            UnsubscribeTmpImportCallbacks();
            Debug.LogError($"[Community Manager] TMP Essential Resources 导入失败：{errorMessage}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static void UnsubscribeTmpImportCallbacks()
        {
            tmpImportPending = false;
            AssetDatabase.importPackageCompleted -= OnTmpPackageImported;
            AssetDatabase.importPackageFailed -= OnTmpPackageImportFailed;
        }

        private static string GetTmpEssentialPackagePath()
        {
            return Path.Combine(EditorApplication.applicationContentsPath, "Resources", "PackageManager",
                "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage");
        }

        private static void CreateWorld(Transform root, TMP_FontAsset font)
        {
            Material ground = AssetDatabase.LoadAssetAtPath<Material>(RootPath + "/Art/Materials/Ground.mat");
            Material building = AssetDatabase.LoadAssetAtPath<Material>(RootPath + "/Art/Materials/Building.mat");
            Material facility = AssetDatabase.LoadAssetAtPath<Material>(RootPath + "/Art/Materials/Facility.mat");
            Material accent = AssetDatabase.LoadAssetAtPath<Material>(RootPath + "/Art/Materials/Accent.mat");

            CreatePrimitive("地面", PrimitiveType.Cube, root, new Vector3(0, -0.5f, 0), new Vector3(32, 1, 24), ground, font);
            CreatePrimitive("住宅楼 A", PrimitiveType.Cube, root, new Vector3(-9, 4, 4), new Vector3(6, 8, 6), building, font);
            CreatePrimitive("住宅楼 B", PrimitiveType.Cube, root, new Vector3(0, 5, 6), new Vector3(6, 10, 6), building, font);
            CreatePrimitive("住宅楼 C", PrimitiveType.Cube, root, new Vector3(9, 3.5f, 4), new Vector3(6, 7, 6), building, font);
            CreatePrimitive("停车区", PrimitiveType.Cube, root, new Vector3(-8, 0.05f, -6), new Vector3(10, 0.1f, 6), facility, font);
            CreatePrimitive("快递柜", PrimitiveType.Cube, root, new Vector3(2, 1, -5), new Vector3(3, 2, 1), accent, font);
            CreatePrimitive("摄像头", PrimitiveType.Cylinder, root, new Vector3(7, 2, -5), new Vector3(0.3f, 2, 0.3f), facility, font);
            CreatePrimitive("儿童区", PrimitiveType.Cylinder, root, new Vector3(9, 0.15f, -1), new Vector3(3, 0.15f, 3), accent, font);
            CreatePrimitive("充电桩", PrimitiveType.Cube, root, new Vector3(-1, 1, -8), new Vector3(0.8f, 2, 0.8f), facility, font);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root);
            cameraObject.transform.position = new Vector3(0, 24, -24);
            cameraObject.transform.rotation = Quaternion.Euler(42, 0, 0);

            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            lightObject.GetComponent<Light>().type = LightType.Directional;
            lightObject.GetComponent<Light>().intensity = 1.2f;
        }

        private static void CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, TMP_FontAsset font)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent);
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;

            GameObject labelObject = new GameObject("Label", typeof(TextMeshPro));
            labelObject.transform.SetParent(item.transform);
            labelObject.transform.localPosition = new Vector3(0, 0.65f, -0.55f);
            labelObject.transform.localRotation = Quaternion.Euler(35, 0, 0);
            labelObject.transform.localScale = Vector3.one * 0.15f;
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = name;
            label.font = font;
            label.fontSize = 6;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static GameUIController CreateUi(Transform root, TMP_FontAsset font)
        {
            GameObject canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUIController));
            canvasObject.transform.SetParent(root);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(root);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            TopResourceBar topBar = CreateTopBar(canvasObject.transform, font);
            EventListPanel eventPanel = CreateEventPanel(canvasObject.transform, font);
            WorkerListPanel workerPanel = CreateWorkerPanel(canvasObject.transform, font);
            LogPanel logPanel = CreateLogPanel(canvasObject.transform, font);
            DailyReportPanel reportPanel = CreateDailyReportPanel(canvasObject.transform, font);
            FinalResultPanel finalPanel = CreateFinalResultPanel(canvasObject.transform, font);

            GameUIController controller = canvasObject.GetComponent<GameUIController>();
            controller.Configure(topBar, eventPanel, workerPanel, logPanel, reportPanel, finalPanel);
            return controller;
        }

        private static TopResourceBar CreateTopBar(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreatePanel("Top Resource Bar", parent, new Color(0.05f, 0.08f, 0.12f, 0.94f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), Vector2.zero);
            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 18;
            layout.childForceExpandWidth = true;
            TMP_Text[] texts = Enumerable.Range(0, 6).Select(index => CreateText(panel.transform, "Resource " + index, "--", font, 28)).ToArray();
            TopResourceBar bar = panel.AddComponent<TopResourceBar>();
            bar.Configure(texts[0], texts[1], texts[2], texts[3], texts[4], texts[5]);
            return bar;
        }

        private static EventListPanel CreateEventPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreatePanel("Event List", parent, new Color(0.06f, 0.09f, 0.14f, 0.92f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0, 0.18f), new Vector2(0.48f, 0.9f), new Vector2(20, 0), new Vector2(-10, -10));
            AddVerticalLayout(panel, 12);
            CreateText(panel.transform, "Heading", "待处理事件", font, 32).fontStyle = FontStyles.Bold;
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(panel.transform, false);
            content.GetComponent<VerticalLayoutGroup>().spacing = 8;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EventItemView prefab = CreateEventItem(content.transform, font);
            EventListPanel result = panel.AddComponent<EventListPanel>();
            result.Configure(content.transform, prefab);
            return result;
        }

        private static EventItemView CreateEventItem(Transform parent, TMP_FontAsset font)
        {
            GameObject item = CreatePanel("Event Item Prototype", parent, new Color(0.12f, 0.16f, 0.22f, 0.95f));
            LayoutElement element = item.AddComponent<LayoutElement>();
            element.preferredHeight = 112;
            AddVerticalLayout(item, 8);
            TMP_Text title = CreateText(item.transform, "Title", "事件", font, 25);
            TMP_Text detail = CreateText(item.transform, "Detail", "详情", font, 19);
            Button button = CreateButton(item.transform, "Select", "选择事件", font);
            EventItemView view = item.AddComponent<EventItemView>();
            view.Configure(title, detail, button, item.GetComponent<Image>());
            return view;
        }

        private static WorkerListPanel CreateWorkerPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreatePanel("Worker List", parent, new Color(0.06f, 0.09f, 0.14f, 0.92f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(1, 0.9f), new Vector2(10, 0), new Vector2(-20, -10));
            AddVerticalLayout(panel, 12);
            CreateText(panel.transform, "Heading", "员工列表", font, 32).fontStyle = FontStyles.Bold;
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(panel.transform, false);
            content.GetComponent<VerticalLayoutGroup>().spacing = 8;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            WorkerItemView prefab = CreateWorkerItem(content.transform, font);
            WorkerListPanel result = panel.AddComponent<WorkerListPanel>();
            result.Configure(content.transform, prefab);
            return result;
        }

        private static WorkerItemView CreateWorkerItem(Transform parent, TMP_FontAsset font)
        {
            GameObject item = CreatePanel("Worker Item Prototype", parent, new Color(0.12f, 0.16f, 0.22f, 0.95f));
            item.AddComponent<LayoutElement>().preferredHeight = 105;
            AddVerticalLayout(item, 8);
            TMP_Text title = CreateText(item.transform, "Title", "员工", font, 25);
            TMP_Text detail = CreateText(item.transform, "Detail", "空闲", font, 19);
            Button button = CreateButton(item.transform, "Dispatch", "派工", font);
            WorkerItemView view = item.AddComponent<WorkerItemView>();
            view.Configure(title, detail, button);
            return view;
        }

        private static LogPanel CreateLogPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreatePanel("Log Panel", parent, new Color(0.03f, 0.05f, 0.08f, 0.95f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0.17f), new Vector2(20, 15), new Vector2(-20, -5));
            TMP_Text text = CreateText(panel.transform, "Log Text", "运营日志", font, 20);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 12), new Vector2(-18, -12));
            text.alignment = TextAlignmentOptions.TopLeft;
            LogPanel result = panel.AddComponent<LogPanel>();
            result.Configure(text);
            return result;
        }

        private static DailyReportPanel CreateDailyReportPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateModal("Daily Report", parent);
            AddVerticalLayout(panel, 18);
            TMP_Text text = CreateText(panel.transform, "Report", "每日结算", font, 28);
            Button button = CreateButton(panel.transform, "Continue", "继续下一天", font);
            DailyReportPanel result = panel.AddComponent<DailyReportPanel>();
            result.Configure(text, button);
            panel.SetActive(false);
            return result;
        }

        private static FinalResultPanel CreateFinalResultPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateModal("Final Result", parent);
            AddVerticalLayout(panel, 18);
            TMP_Text text = CreateText(panel.transform, "Result", "最终结算", font, 28);
            Button button = CreateButton(panel.transform, "Restart", "重新开始", font);
            FinalResultPanel result = panel.AddComponent<FinalResultPanel>();
            result.Configure(text, button);
            panel.SetActive(false);
            return result;
        }

        private static GameObject CreateModal(string name, Transform parent)
        {
            GameObject panel = CreatePanel(name, parent, new Color(0.04f, 0.07f, 0.12f, 0.98f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650, 520);
            rect.anchoredPosition = Vector2.zero;
            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, TMP_FontAsset font, float size)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            GameObject item = CreatePanel(name, parent, new Color(0.14f, 0.45f, 0.75f, 1f));
            item.AddComponent<LayoutElement>().preferredHeight = 42;
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            TMP_Text text = CreateText(item.transform, "Label", label, font, 20);
            text.alignment = TextAlignmentOptions.Center;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void AddVerticalLayout(GameObject target, int padding)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static List<T> LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(item => item != null)
                .OrderBy(item => item.name)
                .ToList();
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
