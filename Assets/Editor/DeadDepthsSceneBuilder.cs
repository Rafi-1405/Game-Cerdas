using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;
using DeadDepths.AI;
using UTS.Audio;

namespace DeadDepths.Editor
{
    /// <summary>
    /// Editor utility that builds and configures the DeadDepths master scene.
    /// ASD-STE100: This editor tool creates the main scene and assigns components.
    /// </summary>
    public static class DeadDepthsSceneBuilder
    {
        [MenuItem("DeadDepths/Build Main Scene")]
        public static void BuildScene()
        {
            string scenesDir = "Assets/Scenes";
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            string scenePath = "Assets/Scenes/DeadDepths.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light (Isometric lighting)
            GameObject lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.95f, 0.95f, 0.98f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // 2. Camera Setup
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 500f;
            camGo.AddComponent<AudioListener>();

            // Universal Additional Camera Data if URP
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var isoCam = camGo.AddComponent<IsometricCameraController>();
            var fadeCtrl = camGo.AddComponent<ObstructionFadeController>();
            camGo.transform.position = new Vector3(0f, 22f, -22f);
            camGo.transform.rotation = Quaternion.Euler(52f, 45f, 0f);

            // 3. Audio Director
            GameObject audioGo = new GameObject("AudioManager");
            var audioMgr = audioGo.AddComponent<AudioManager>();
            var soundLib = AssetDatabase.LoadAssetAtPath<SurvivalSoundLibrary>("Assets/SurvivalUpgrade/Audio/SurvivalSoundLibrary.asset");
            if (soundLib != null)
            {
                var audioSo = new SerializedObject(audioMgr);
                var libProp = audioSo.FindProperty("soundLibrary");
                if (libProp != null) libProp.objectReferenceValue = soundLib;
                audioSo.ApplyModifiedProperties();
            }

            // 4. Create and Configure Prefabs
            GameObject playerPrefab = CreateOrUpdatePlayerPrefab();
            GameObject zombiePrefab = CreateOrUpdateZombiePrefab();

            // 5. Core Game Manager & Procedural Systems
            GameObject coreGo = new GameObject("GameManager");
            var gm = coreGo.AddComponent<GameManager>();
            var diff = coreGo.AddComponent<DifficultyController>();
            var tracker = coreGo.AddComponent<PlayerPerformanceTracker>();
            var worldGen = coreGo.AddComponent<WorldGenerator>();
            var navBaker = coreGo.AddComponent<NavMeshRuntimeBaker>();

            // Configure WorldGenerator references
            var genSo = new SerializedObject(worldGen);
            if (playerPrefab != null) genSo.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            if (zombiePrefab != null) genSo.FindProperty("zombiePrefab").objectReferenceValue = zombiePrefab;

            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_Ground01.mat");
            if (groundMat != null) genSo.FindProperty("groundMaterial").objectReferenceValue = groundMat;

            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_BricksWeathered.mat");
            if (wallMat != null) genSo.FindProperty("wallMaterial").objectReferenceValue = wallMat;

            var mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_Clay.mat");
            if (mudMat != null) genSo.FindProperty("mudMaterial").objectReferenceValue = mudMat;

            var crateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_WoodFlooring.mat");
            if (crateMat != null) genSo.FindProperty("crateMaterial").objectReferenceValue = crateMat;

            genSo.ApplyModifiedProperties();

            // 6. In-Game UI Systems
            GameObject uiGo = new GameObject("UIManager");
            uiGo.AddComponent<DeadDepthsHUD>();
            uiGo.AddComponent<CrateProgressBarUI>();
            uiGo.AddComponent<ItemNotificationManager>();
            uiGo.AddComponent<CompletionScreenUI>();

            // 7. Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[DeadDepths] Successfully created scene at: {scenePath}");

            // 8. Update EditorBuildSettings
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = newScenes;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateOrUpdatePlayerPrefab()
        {
            string prefabDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = "Assets/Prefabs/DeadDepths_Player.prefab";

            GameObject root = new GameObject("DeadDepths_Player");
            root.tag = "Player";

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<DeadDepthsPlayerController>();

            var modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterUpgrade/Models/PlayerChar.fbx");
            if (modelFbx != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelFbx, root.transform);
                model.name = "PlayerModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/CharacterUpgrade/Controllers/Player.controller");
                if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateOrUpdateZombiePrefab()
        {
            string prefabDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = "Assets/Prefabs/DeadDepths_Zombie.prefab";

            GameObject root = new GameObject("DeadDepths_Zombie");
            root.tag = "Enemy";

            var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.height = 1.8f;
            agent.radius = 0.45f;
            agent.speed = 3.2f;
            agent.stoppingDistance = 1.2f;

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.45f;
            col.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<ZombieController>();

            var modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterUpgrade/Models/ZombieChar.fbx");
            if (modelFbx != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelFbx, root.transform);
                model.name = "ZombieModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/CharacterUpgrade/Controllers/Zombie.controller");
                if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
