using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using DungeonGenerator.UI;
using DungeonGenerator.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DungeonGenerator.Editor
{
    /// <summary>
    /// Custom Inspector editor and automated scene setup tool for procedural dungeons.
    /// </summary>
    [CustomEditor(typeof(DungeonManager))]
    public class DungeonEditorTool : UnityEditor.Editor
    {
        #region CONFIG
        public static class EDITOR_CONFIG
        {
            public const string TARGET_SCENE_PATH = "Assets/Scenes/Praktikum10_ProceduralDungeon.unity";
            public const string MENU_SETUP_SCENE = "Tools/Dungeon Generator/Setup Praktikum 10 Scene";
            public const string MENU_GENERATE_NOW = "Tools/Dungeon Generator/Generate Dungeon Now";
        }
        #endregion

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonManager manager = (DungeonManager)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Dungeon Generation Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Dungeon (New/Random)", GUILayout.Height(30)))
            {
                manager.GenerateDungeon();
            }

            if (GUILayout.Button("Regenerate Same Seed", GUILayout.Height(26)))
            {
                manager.RegenerateSameSeed();
            }

            if (GUILayout.Button("Clear Generated Dungeon", GUILayout.Height(24)))
            {
                DungeonSceneBuilder builder = manager.GetComponent<DungeonSceneBuilder>();
                if (builder != null)
                {
                    builder.ClearDungeon();
                }
            }

            EditorGUILayout.Space(8);
            if (manager.CurrentValidationReport != null)
            {
                EditorGUILayout.HelpBox(
                    $"Validation: {(manager.CurrentValidationReport.IsFullyValid ? "PASS" : "FAIL")}\n" +
                    $"BFS Shortest Path: {manager.CurrentValidationReport.PathLength} steps\n" +
                    $"Reachable Cells: {manager.CurrentValidationReport.ReachableFloorsCount} / {manager.CurrentValidationReport.TotalFloorsCount}\n" +
                    $"Retries: {manager.CurrentValidationReport.RetryAttempts}",
                    manager.CurrentValidationReport.IsFullyValid ? MessageType.Info : MessageType.Warning
                );
            }
        }

        /// <summary>
        /// Automatically constructs and configures the complete Praktikum 10 Unity Scene.
        /// </summary>
        [MenuItem(EDITOR_CONFIG.MENU_SETUP_SCENE)]
        public static void SetupPraktikum10Scene()
        {
            // 1. Create a new empty scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.95f, 0.95f, 1.0f);
            dirLight.intensity = 1.0f;
            dirLight.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);

            // 3. Setup Main Camera with DungeonCameraController
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
            camObj.AddComponent<AudioListener>();
            UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.renderPostProcessing = true;
            DungeonCameraController camController = camObj.AddComponent<DungeonCameraController>();
            camObj.transform.position = new Vector3(0.0f, 20.0f, -15.0f);
            camObj.transform.rotation = Quaternion.Euler(55.0f, 0.0f, 0.0f);

            // 4. Setup DungeonManager hierarchy
            GameObject managerObj = new GameObject("DungeonManager");
            DungeonManager manager = managerObj.AddComponent<DungeonManager>();
            DungeonSceneBuilder builder = managerObj.AddComponent<DungeonSceneBuilder>();
            DungeonMinimapUI minimap = managerObj.AddComponent<DungeonMinimapUI>();
            DungeonGizmos gizmos = managerObj.AddComponent<DungeonGizmos>();
            DungeonHUD hud = managerObj.AddComponent<DungeonHUD>();

            // Generate initial dungeon
            manager.GenerateDungeon(DungeonManager.MANAGER_CONFIG.INITIAL_DEFAULT_SEED);

            // 5. Save scene
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, EDITOR_CONFIG.TARGET_SCENE_PATH);

            // 6. Update Build Settings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(EDITOR_CONFIG.TARGET_SCENE_PATH, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            Debug.Log($"[DungeonEditorTool] Praktikum 10 scene generated and saved at {EDITOR_CONFIG.TARGET_SCENE_PATH}");
        }

        [MenuItem(EDITOR_CONFIG.MENU_GENERATE_NOW)]
        public static void GenerateNow()
        {
            DungeonManager manager = Object.FindAnyObjectByType<DungeonManager>();
            if (manager != null)
            {
                manager.GenerateDungeon();
            }
            else
            {
                SetupPraktikum10Scene();
            }
        }
    }
}
