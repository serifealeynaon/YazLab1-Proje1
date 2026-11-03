using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YazLab.Companion;
using YazLab.Game;
using YazLab.Interaction;
using YazLab.Player;
using YazLab.UI;

namespace YazLab.EditorTools
{
    public static class SceneBuilder
    {
        private const int GroundLayer = 6;
        private const int WallLayer = 7;
        private const int HazardLayer = 8;
        private const int InteractableLayer = 9;
        private const int ActorLayer = 10;
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Level01.unity";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static Font font;

        [MenuItem("YazLab/Sahneyi Kur")]
        public static void Build()
        {
            ConfigureLayers();
            ConfigureAgent();
            Materials.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();

            GameObject ground = Cube("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 50f),
                GroundLayer, "Ground", new Color(0.35f, 0.38f, 0.35f));
            CreateWalls();

            Door firstDoor = CreateDoor("Door_1", new Vector3(0f, 1.5f, -5f));
            Door secondDoor = CreateDoor("Door_2", new Vector3(0f, 1.5f, 10f));
            SwitchInteractable switchTarget = CreateSwitch(firstDoor);
            CreateCrate();
            FinishZone finishZone = CreateFinishZone();

            PlayerController player = CreatePlayer();
            CompanionController companion = CreateCompanion(player.transform);
            NpcGuard guard = CreateGuard(secondDoor);
            CreateCamera(player.transform);

            HudView hud;
            DialogueView dialogueView;
            EndScreenView endScreen;
            CreateUi(out hud, out dialogueView, out endScreen);

            GameObject guardObject = guard.gameObject;
            var presenter = guardObject.AddComponent<NpcDialoguePresenter>();
            Assign(presenter, "dialogueView", dialogueView);
            Assign(presenter, "guard", guard);
            var so = new SerializedObject(presenter);
            so.FindProperty("settings.systemPrompt").stringValue = NpcPrompt.System;
            so.ApplyModifiedPropertiesWithoutUndo();

            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            Assign(flow, "player", player);
            Assign(flow, "companion", companion);
            Assign(flow, "switchTarget", switchTarget);
            Assign(flow, "firstDoor", firstDoor);
            Assign(flow, "secondDoor", secondDoor);
            Assign(flow, "dialogue", presenter);
            Assign(flow, "finishZone", finishZone);
            Assign(flow, "hud", hud);
            Assign(flow, "endScreen", endScreen);

            BakeNavMesh(ground);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Level01 kuruldu.");
        }

        private static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
            layers.GetArrayElementAtIndex(WallLayer).stringValue = "Wall";
            layers.GetArrayElementAtIndex(HazardLayer).stringValue = "Hazard";
            layers.GetArrayElementAtIndex(InteractableLayer).stringValue = "Interactable";
            layers.GetArrayElementAtIndex(ActorLayer).stringValue = "Actor";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAgent()
        {
            var settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings"));
            SerializedProperty agent = settings.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = 0.3f;
            agent.FindPropertyRelative("agentHeight").floatValue = 1f;
            agent.FindPropertyRelative("agentClimb").floatValue = 0.3f;
            settings.FindProperty("m_SettingNames").GetArrayElementAtIndex(0).stringValue = "Companion";
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateWalls()
        {
            var parent = new GameObject("Walls").transform;
            Color color = new Color(0.55f, 0.55f, 0.6f);
            Wall(parent, "Wall_N", new Vector3(0f, 1.5f, 25f), new Vector3(20f, 3f, 0.5f), color);
            Wall(parent, "Wall_S", new Vector3(0f, 1.5f, -25f), new Vector3(20f, 3f, 0.5f), color);
            Wall(parent, "Wall_E", new Vector3(10f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f), color);
            Wall(parent, "Wall_W", new Vector3(-10f, 1.5f, 0f), new Vector3(0.5f, 3f, 50f), color);
            Wall(parent, "Wall_A1", new Vector3(-6f, 1.5f, -5f), new Vector3(8f, 3f, 0.5f), color);
            Wall(parent, "Wall_A2", new Vector3(4f, 1.5f, -5f), new Vector3(4f, 3f, 0.5f), color);
            Wall(parent, "Wall_A3", new Vector3(8.6f, 1.5f, -5f), new Vector3(2.8f, 3f, 0.5f), color);
            Wall(parent, "Wall_Lintel", new Vector3(6.6f, 2.1f, -5f), new Vector3(1.2f, 1.8f, 0.5f), color);
            Wall(parent, "Wall_B1", new Vector3(-6f, 1.5f, 10f), new Vector3(8f, 3f, 0.5f), color);
            Wall(parent, "Wall_B2", new Vector3(6f, 1.5f, 10f), new Vector3(8f, 3f, 0.5f), color);
        }

        private static void Wall(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            Cube(name, position, scale, WallLayer, "Wall", color).transform.SetParent(parent);
        }

        private static Door CreateDoor(string name, Vector3 position)
        {
            GameObject door = Cube(name, position, new Vector3(4f, 3f, 0.5f), WallLayer, "Door", new Color(0.75f, 0.15f, 0.15f));
            door.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var obstacle = door.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            return door.AddComponent<Door>();
        }

        private static SwitchInteractable CreateSwitch(Door door)
        {
            GameObject switchObject = Cube("Switch_1", new Vector3(6.6f, 0.75f, -2f), new Vector3(1f, 1.5f, 0.3f),
                InteractableLayer, "Switch", new Color(0.95f, 0.8f, 0.1f));
            var point = new GameObject("InteractPoint").transform;
            point.position = new Vector3(6.6f, 0f, -3.2f);
            point.SetParent(switchObject.transform, true);
            var component = switchObject.AddComponent<SwitchInteractable>();
            Assign(component, "interactPoint", point);
            Assign(component, "door", door);
            Assign(component, "indicator", switchObject.GetComponent<Renderer>());
            return component;
        }

        private static void CreateCrate()
        {
            GameObject crate = Cube("Crate", new Vector3(5.6f, 0.5f, -12f), new Vector3(1.5f, 1f, 1.5f),
                HazardLayer, "Crate", new Color(0.55f, 0.35f, 0.15f));
            var obstacle = crate.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;
            crate.AddComponent<DiscoverableCrate>();
        }

        private static FinishZone CreateFinishZone()
        {
            GameObject zone = Cube("FinishZone", new Vector3(0f, 0.5f, 18f), new Vector3(8f, 1f, 4f),
                0, "Finish", new Color(0.2f, 0.8f, 0.3f));
            zone.GetComponent<BoxCollider>().isTrigger = true;
            return zone.AddComponent<FinishZone>();
        }

        private static PlayerController CreatePlayer()
        {
            var root = new GameObject("Player");
            root.layer = ActorLayer;
            root.transform.position = new Vector3(-4f, 1.1f, -20f);
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            Visual(root.transform, PrimitiveType.Capsule, Vector3.zero, Vector3.one, "Player", new Color(0.2f, 0.45f, 0.95f));
            return root.AddComponent<PlayerController>();
        }

        private static CompanionController CreateCompanion(Transform player)
        {
            var root = new GameObject("Companion");
            root.layer = ActorLayer;
            root.transform.position = new Vector3(4f, 0.5f, -20f);
            Visual(root.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f), "Companion", new Color(0.2f, 0.8f, 0.35f));
            Visual(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.3f), new Vector3(0.2f, 0.15f, 0.2f), "Nose", Color.white);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1f;
            agent.baseOffset = 0.5f;
            agent.speed = 3.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0f;
            agent.enabled = false;

            var sensor = root.AddComponent<RaycastSensor>();
            var sensorObject = new SerializedObject(sensor);
            sensorObject.FindProperty("wallMask").intValue = 1 << WallLayer;
            sensorObject.FindProperty("hazardMask").intValue = 1 << HazardLayer;
            sensorObject.FindProperty("targetMask").intValue = 1 << InteractableLayer;
            sensorObject.ApplyModifiedPropertiesWithoutUndo();

            var companion = root.AddComponent<CompanionController>();
            Assign(companion, "player", player);
            return companion;
        }

        private static NpcGuard CreateGuard(Door door)
        {
            var root = new GameObject("NPC_Guard");
            root.layer = ActorLayer;
            root.transform.position = new Vector3(0f, 1f, 7f);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            root.AddComponent<CapsuleCollider>();
            GameObject body = Visual(root.transform, PrimitiveType.Capsule, Vector3.zero, Vector3.one, "Guard", new Color(0.55f, 0.3f, 0.8f));
            var guard = root.AddComponent<NpcGuard>();
            Assign(guard, "door", door);
            Assign(guard, "body", body.GetComponent<Renderer>());
            return guard;
        }

        private static void CreateCamera(Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position + new Vector3(0f, 14f, -9f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<CameraFollow>();
            Assign(follow, "target", target);
        }

        private static void BakeNavMesh(GameObject ground)
        {
            var surface = ground.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = (1 << GroundLayer) | (1 << WallLayer) | (1 << InteractableLayer);
            surface.BuildNavMesh();

            string path = Root + "/Scenes/Level01_NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorUtility.SetDirty(surface);
        }

        private static void CreateUi(out HudView hud, out DialogueView dialogue, out EndScreenView endScreen)
        {
            var canvasObject = new GameObject("UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            hud = canvasObject.AddComponent<HudView>();
            Assign(hud, "objectiveText", Label(canvasObject.transform, "Objective", new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(20f, -110f), new Vector2(0f, -15f), 22, TextAnchor.UpperLeft, Color.white));
            Assign(hud, "statusText", Label(canvasObject.transform, "Status", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(20f, 15f), new Vector2(0f, 50f), 18, TextAnchor.LowerLeft, new Color(0.7f, 1f, 0.75f)));
            Assign(hud, "fpsText", Label(canvasObject.transform, "Fps", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -50f), new Vector2(-15f, -15f), 18, TextAnchor.UpperRight, new Color(1f, 1f, 0.6f)));

            dialogue = CreateDialogue(canvasObject.transform);
            endScreen = CreateEndScreen(canvasObject.transform);
        }

        private static DialogueView CreateDialogue(Transform canvas)
        {
            GameObject panel = Panel(canvas, "DialoguePanel", new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.72f), new Color(0.05f, 0.05f, 0.1f, 0.92f));
            var logText = Label(panel.transform, "Log", new Vector2(0f, 0.28f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-70f, -15f), 20, TextAnchor.LowerLeft, Color.white);
            logText.supportRichText = true;
            logText.verticalOverflow = VerticalWrapMode.Truncate;
            var thinking = Label(panel.transform, "Thinking", new Vector2(0f, 0.18f), new Vector2(1f, 0.28f), new Vector2(20f, 0f), new Vector2(-20f, 0f), 18, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.5f));

            GameObject inputObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputObject.name = "Input";
            Place(inputObject, panel.transform, new Vector2(0f, 0.03f), new Vector2(1f, 0.17f), new Vector2(20f, 0f), new Vector2(-150f, 0f));
            var input = inputObject.GetComponent<InputField>();
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 300;
            ((Text)input.placeholder).text = "Muhafıza bir şey yaz...";
            ApplyFont(inputObject);
            foreach (Text text in inputObject.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 18;
            }

            Button send = CreateButton(panel.transform, "Send", "Gönder", new Vector2(1f, 0.03f), new Vector2(1f, 0.17f), new Vector2(-140f, 0f), new Vector2(-20f, 0f));
            Button close = CreateButton(panel.transform, "Close", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-55f, -50f), new Vector2(-15f, -10f));

            var holder = canvas.gameObject.AddComponent<DialogueView>();
            Assign(holder, "panel", panel);
            Assign(holder, "logText", logText);
            Assign(holder, "thinkingText", thinking);
            Assign(holder, "inputField", input);
            Assign(holder, "sendButton", send);
            Assign(holder, "closeButton", close);
            return holder;
        }

        private static EndScreenView CreateEndScreen(Transform canvas)
        {
            GameObject panel = Panel(canvas, "EndPanel", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.85f));
            var summary = Label(panel.transform, "Summary", new Vector2(0f, 0.5f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero, 36, TextAnchor.MiddleCenter, Color.white);
            Button restart = CreateButton(panel.transform, "Restart", "Tekrar Oyna", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(-170f, -25f), new Vector2(-10f, 25f));
            Button quit = CreateButton(panel.transform, "Quit", "Çıkış", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(10f, -25f), new Vector2(170f, 25f));

            var holder = canvas.gameObject.AddComponent<EndScreenView>();
            Assign(holder, "panel", panel);
            Assign(holder, "summaryText", summary);
            Assign(holder, "restartButton", restart);
            Assign(holder, "quitButton", quit);
            return holder;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            Place(panel, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, TextAnchor anchor, Color color)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            Place(labelObject, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            Place(buttonObject, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            ApplyFont(buttonObject);
            var text = buttonObject.GetComponentInChildren<Text>();
            text.text = label;
            text.fontSize = 18;
            return buttonObject.GetComponent<Button>();
        }

        private static void Place(GameObject target, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void ApplyFont(GameObject target)
        {
            foreach (Text text in target.GetComponentsInChildren<Text>(true))
            {
                text.font = font;
            }
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, int layer, string materialName, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.layer = layer;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
            return cube;
        }

        private static GameObject Visual(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, string materialName, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
            return visual;
        }

        private static Material GetMaterial(string name, Color color)
        {
            if (Materials.TryGetValue(name, out Material material))
            {
                return material;
            }

            string folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(Root, "Materials");
            }

            string path = $"{folder}/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            material = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            Materials[name] = material;
            return material;
        }

        private static void Assign(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
