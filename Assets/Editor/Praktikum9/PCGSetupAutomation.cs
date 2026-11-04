using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Praktikum9.PCG;

namespace Praktikum9.Editor
{
    /// <summary>
    /// Skrip Otomasi Editor 1-Klik untuk Praktikum 9.
    /// Membangun Material, Prefab, Konfigurasi Scene, dan Setup PCGManager secara deterministik.
    /// </summary>
    public static class PCGSetupAutomation
    {
        private const string SCENE_PATH = "Assets/Scenes/praktikum-9-real.unity";
        private const string MAT_DIR = "Assets/Materials/Praktikum9";
        private const string PREFAB_ENEMY_DIR = "Assets/Prefabs/Praktikum9/Enemies";
        private const string PREFAB_ITEM_DIR = "Assets/Prefabs/Praktikum9/Items";
        private const string SETUP_PREF_KEY = "Praktikum9_AutoSetup_v1";

        [InitializeOnLoadMethod]
        private static void OnEditorLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(SETUP_PREF_KEY, false))
                {
                    Debug.Log("[PCGSetupAutomation] Triggering automatic initial setup for Praktikum 9...");
                    ExecuteSetup();
                    EditorPrefs.SetBool(SETUP_PREF_KEY, true);
                }
            };
        }

        [MenuItem("Tools/Game Cerdas/1-Click Setup Praktikum 9", priority = 100)]
        public static void ExecuteSetup()
        {
            Debug.Log("[PCGSetupAutomation] === Memulai Setup Praktikum 9 ===");

            // 1. Pastikan folder-folder target tersedia
            EnsureDirectoryExists(MAT_DIR);
            EnsureDirectoryExists(PREFAB_ENEMY_DIR);
            EnsureDirectoryExists(PREFAB_ITEM_DIR);
            EnsureDirectoryExists("Assets/Scenes");

            // 2. Buat atau perbarui Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material matGround = GetOrCreateMaterial($"{MAT_DIR}/M_PCG_Ground.mat", litShader, new Color(0.20f, 0.22f, 0.26f));
            Material matObstacle = GetOrCreateMaterial($"{MAT_DIR}/M_PCG_Obstacle.mat", litShader, new Color(0.55f, 0.55f, 0.60f));
            Material matPlayer = GetOrCreateMaterial($"{MAT_DIR}/M_PCG_Player.mat", litShader, new Color(0.05f, 0.75f, 0.85f));

            Material matEnemyRed = GetOrCreateMaterial($"{MAT_DIR}/M_Enemy_Red.mat", litShader, new Color(0.90f, 0.15f, 0.15f));
            Material matEnemyBlue = GetOrCreateMaterial($"{MAT_DIR}/M_Enemy_Blue.mat", litShader, new Color(0.15f, 0.45f, 0.95f));
            Material matEnemyYellow = GetOrCreateMaterial($"{MAT_DIR}/M_Enemy_Yellow.mat", litShader, new Color(0.95f, 0.85f, 0.10f));

            Material matItemCoin = GetOrCreateMaterial($"{MAT_DIR}/M_Item_Coin.mat", litShader, new Color(1.00f, 0.82f, 0.00f));
            Material matItemHealth = GetOrCreateMaterial($"{MAT_DIR}/M_Item_Health.mat", litShader, new Color(0.15f, 0.85f, 0.30f));
            Material matItemRare = GetOrCreateMaterial($"{MAT_DIR}/M_Item_Rare.mat", litShader, new Color(0.75f, 0.15f, 0.95f));

            AssetDatabase.SaveAssets();

            // 3. Buat Prefabs Enemy & Item
            GameObject enemyRedPrefab = CreateOrUpdatePrefab($"{PREFAB_ENEMY_DIR}/Enemy_Red.prefab", PrimitiveType.Capsule, new Vector3(0.8f, 0.8f, 0.8f), matEnemyRed);
            GameObject enemyBluePrefab = CreateOrUpdatePrefab($"{PREFAB_ENEMY_DIR}/Enemy_Blue.prefab", PrimitiveType.Capsule, new Vector3(0.9f, 0.9f, 0.9f), matEnemyBlue);
            GameObject enemyYellowPrefab = CreateOrUpdatePrefab($"{PREFAB_ENEMY_DIR}/Enemy_Yellow.prefab", PrimitiveType.Capsule, new Vector3(0.7f, 0.7f, 0.7f), matEnemyYellow);

            GameObject itemCoinPrefab = CreateOrUpdatePrefab($"{PREFAB_ITEM_DIR}/Item_Coin.prefab", PrimitiveType.Cylinder, new Vector3(0.6f, 0.1f, 0.6f), matItemCoin);
            GameObject itemHealthPrefab = CreateOrUpdatePrefab($"{PREFAB_ITEM_DIR}/Item_Health.prefab", PrimitiveType.Cube, new Vector3(0.5f, 0.5f, 0.5f), matItemHealth);
            GameObject itemRarePrefab = CreateOrUpdatePrefab($"{PREFAB_ITEM_DIR}/Item_Rare.prefab", PrimitiveType.Sphere, new Vector3(0.6f, 0.6f, 0.6f), matItemRare);

            AssetDatabase.SaveAssets();

            // 4. Buat Scene Baru Terisolasi
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 5. Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 22f, -18f);
            camObj.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            camObj.AddComponent<AudioListener>();

            // 6. Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1f, 0.96f, 0.88f);
            lightComp.intensity = 1.2f;
            lightComp.shadows = LightShadows.Soft;

            // 7. Environment & Ground (30m x 30m)
            GameObject envObj = new GameObject("Environment");
            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "Ground";
            groundObj.transform.SetParent(envObj.transform);
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = new Vector3(3f, 1f, 3f);
            groundObj.GetComponent<MeshRenderer>().sharedMaterial = matGround;

            // 8. Obstacles (Layer 7: Obstacle)
            int obstacleLayer = 7; // Sesuai TagManager.asset Layer 7 = Obstacle
            CreateObstacle("Obstacle_01", new Vector3(5f, 1f, 5f), new Vector3(3f, 2f, 1f), matObstacle, envObj.transform, obstacleLayer);
            CreateObstacle("Obstacle_02", new Vector3(-6f, 1f, 4f), new Vector3(2f, 2f, 3f), matObstacle, envObj.transform, obstacleLayer);
            CreateObstacle("Obstacle_03", new Vector3(5f, 1f, -6f), new Vector3(4f, 2f, 2f), matObstacle, envObj.transform, obstacleLayer);
            CreateObstacle("Obstacle_04", new Vector3(-4f, 1f, -5f), new Vector3(2f, 2f, 2f), matObstacle, envObj.transform, obstacleLayer);

            // 9. Player
            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.transform.position = new Vector3(0f, 1f, 0f);
            playerObj.GetComponent<MeshRenderer>().sharedMaterial = matPlayer;
            // Lepas default capsule collider agar tidak konflik dengan CharacterController
            Object.DestroyImmediate(playerObj.GetComponent<CapsuleCollider>());
            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.radius = 0.5f;
            cc.height = 2f;
            playerObj.AddComponent<PCGPlayerController>();

            // 10. GeneratedObjects Root
            GameObject genRootObj = new GameObject("GeneratedObjects");
            genRootObj.transform.position = Vector3.zero;

            // 11. PCGManager
            GameObject pcgManagerObj = new GameObject("PCGManager");
            pcgManagerObj.transform.position = Vector3.zero;
            PCGSpawner spawner = pcgManagerObj.AddComponent<PCGSpawner>();

            // Kaitkan Serialized Properties PCGSpawner
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("player").objectReferenceValue = playerObj.transform;
            spawnerSO.FindProperty("generatedRoot").objectReferenceValue = genRootObj.transform;
            spawnerSO.FindProperty("spawnAreaSize").vector2Value = new Vector2(24f, 24f);
            spawnerSO.FindProperty("spawnY").floatValue = 0.5f;
            spawnerSO.FindProperty("enemyCount").intValue = 12;
            spawnerSO.FindProperty("itemCount").intValue = 8;
            spawnerSO.FindProperty("minDistanceFromPlayer").floatValue = 5f;
            spawnerSO.FindProperty("checkRadius").floatValue = 0.75f;
            spawnerSO.FindProperty("minimumSpacing").floatValue = 1.5f;
            spawnerSO.FindProperty("maxAttemptsPerObject").intValue = 50;
            spawnerSO.FindProperty("seed").intValue = 12345;
            spawnerSO.FindProperty("useRandomSeed").boolValue = false;
            spawnerSO.FindProperty("obstacleMask").intValue = 1 << obstacleLayer;

            // Setup Enemy Table (60:30:10)
            SerializedProperty enemyProp = spawnerSO.FindProperty("enemyEntries");
            enemyProp.arraySize = 3;
            SetSpawnEntry(enemyProp.GetArrayElementAtIndex(0), "Enemy_Red (Common)", enemyRedPrefab, 60);
            SetSpawnEntry(enemyProp.GetArrayElementAtIndex(1), "Enemy_Blue (Uncommon)", enemyBluePrefab, 30);
            SetSpawnEntry(enemyProp.GetArrayElementAtIndex(2), "Enemy_Yellow (Rare)", enemyYellowPrefab, 10);

            // Setup Item Table (60:30:10)
            SerializedProperty itemProp = spawnerSO.FindProperty("itemEntries");
            itemProp.arraySize = 3;
            SetSpawnEntry(itemProp.GetArrayElementAtIndex(0), "Item_Coin (Common)", itemCoinPrefab, 60);
            SetSpawnEntry(itemProp.GetArrayElementAtIndex(1), "Item_Health (Uncommon)", itemHealthPrefab, 30);
            SetSpawnEntry(itemProp.GetArrayElementAtIndex(2), "Item_Rare (Legendary)", itemRarePrefab, 10);

            spawnerSO.ApplyModifiedProperties();

            // 12. Simpan Scene ke Disk
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[PCGSetupAutomation] Scene berhasil disimpan ke: {SCENE_PATH}");

            // 13. Tambahkan ke EditorBuildSettings jika belum terdaftar
            AddSceneToBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PCGSetupAutomation] === Setup Praktikum 9 SELESAI DENGAN SUKSES! (100% Siap Uji) ===");
        }

        [MenuItem("Tools/Game Cerdas/Force Re-Run Setup Praktikum 9", priority = 101)]
        public static void ForceReRun()
        {
            EditorPrefs.DeleteKey(SETUP_PREF_KEY);
            ExecuteSetup();
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                string folder = Path.GetFileName(assetPath);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectoryExists(parent);
                }
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject CreateOrUpdatePrefab(string prefabPath, PrimitiveType primitiveType, Vector3 scale, Material material)
        {
            GameObject tempGo = GameObject.CreatePrimitive(primitiveType);
            tempGo.transform.localScale = scale;
            tempGo.GetComponent<MeshRenderer>().sharedMaterial = material;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);
            return prefab;
        }

        private static void CreateObstacle(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, int layer)
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = name;
            obs.transform.SetParent(parent);
            obs.transform.position = pos;
            obs.transform.localScale = scale;
            obs.layer = layer;
            obs.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void SetSpawnEntry(SerializedProperty element, string name, GameObject prefab, int weight)
        {
            element.FindPropertyRelative("name").stringValue = name;
            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            element.FindPropertyRelative("weight").intValue = weight;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            foreach (var s in originalScenes)
            {
                if (s.path == scenePath) return;
            }

            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
            System.Array.Copy(originalScenes, newScenes, originalScenes.Length);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
