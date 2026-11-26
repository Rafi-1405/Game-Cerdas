using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
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
    /// Editor utility that builds and configures the DeadDepths master scene,
    /// URP Volume Profile, Skybox/Fog, Prefabs with IK & Flashlight, and uGUI/EventSystem.
    /// </summary>
    public static class DeadDepthsSceneBuilder
    {
        private const string VolumeProfilePath = "Assets/Settings/DeadDepthsVolumeProfile.asset";

        [MenuItem("DeadDepths/Build Main Scene")]
        public static void BuildScene()
        {
            string scenesDir = "Assets/Scenes";
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            string scenePath = "Assets/Scenes/DeadDepths.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Moonlight Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.68f, 0.78f, 0.98f);
            light.intensity = 0.72f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // 1b. Night Skybox, Ambient Trilight & Atmospheric Fog
            var nightSkyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalUpgrade/Sky/NightSky.mat");
            if (nightSkyMat != null)
            {
                RenderSettings.skybox = nightSkyMat;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.11f, 0.14f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.07f, 0.10f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.06f, 0.10f);
            RenderSettings.fogDensity = 0.014f;

            // 1c. Global URP Post-Processing Volume (Dedicated DeadDepthsVolumeProfile.asset)
            var volumeProfile = CreateOrUpdateDeadDepthsVolumeProfile();
            GameObject volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = volumeProfile;

            // 2. Camera Setup (Skybox + SMAA + PostProcessing)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 500f;
            camGo.AddComponent<AudioListener>();

            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;

            camGo.AddComponent<IsometricCameraController>();
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

            // 5. Core Game Manager, VFX & Procedural Systems
            GameObject coreGo = new GameObject("GameManager");
            coreGo.AddComponent<GameManager>();
            coreGo.AddComponent<DifficultyController>();
            coreGo.AddComponent<PlayerPerformanceTracker>();
            var worldGen = coreGo.AddComponent<WorldGenerator>();
            coreGo.AddComponent<NavMeshRuntimeBaker>();
            var vfxMgr = coreGo.AddComponent<CombatVfxManager>();

            var litShaderAsset = Shader.Find("Universal Render Pipeline/Lit");
            var particleUnlitShaderAsset = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            var unlitShaderAsset = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            var urpLitTransTemplate = CreateOrUpdateTemplateMaterial(
                "Assets/Settings/DeadDepths_URPLit_TransparentTemplate.mat",
                litShaderAsset,
                mat =>
                {
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                    if (mat.HasProperty("_BlendModePreserveSpecular")) mat.SetFloat("_BlendModePreserveSpecular", 0f);
                    if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                    if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                });

            var urpLitEmissionTemplate = CreateOrUpdateTemplateMaterial(
                "Assets/Settings/DeadDepths_URPLit_EmissionTemplate.mat",
                litShaderAsset,
                mat =>
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.2f, 1.0f, 0.3f) * 1.4f);
                });

            var particleTransTemplate = CreateOrUpdateTemplateMaterial(
                "Assets/Settings/DeadDepths_ParticlesUnlit_TransparentTemplate.mat",
                particleUnlitShaderAsset,
                mat =>
                {
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                    if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                });

            if (fadeCtrl != null && urpLitTransTemplate != null)
            {
                var fadeSo = new SerializedObject(fadeCtrl);
                var tmplProp = fadeSo.FindProperty("transparentVariantTemplate");
                if (tmplProp != null) tmplProp.objectReferenceValue = urpLitTransTemplate;
                fadeSo.ApplyModifiedProperties();
            }

            if (vfxMgr != null)
            {
                var vfxSo = new SerializedObject(vfxMgr);
                var pProp = vfxSo.FindProperty("bundledParticleShader");
                if (pProp != null) pProp.objectReferenceValue = particleUnlitShaderAsset;
                var fProp = vfxSo.FindProperty("bundledFallbackShader");
                if (fProp != null) fProp.objectReferenceValue = unlitShaderAsset;
                var ptProp = vfxSo.FindProperty("particleTransparentTemplate");
                if (ptProp != null) ptProp.objectReferenceValue = particleTransTemplate;
                vfxSo.ApplyModifiedProperties();
            }

            // Configure WorldGenerator references
            var genSo = new SerializedObject(worldGen);
            if (playerPrefab != null) genSo.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            if (zombiePrefab != null) genSo.FindProperty("zombiePrefab").objectReferenceValue = zombiePrefab;
            var genLitProp = genSo.FindProperty("bundledLitShader");
            if (genLitProp != null) genLitProp.objectReferenceValue = litShaderAsset;
            var genPartProp = genSo.FindProperty("bundledParticleUnlitShader");
            if (genPartProp != null) genPartProp.objectReferenceValue = particleUnlitShaderAsset;
            var genUnlitProp = genSo.FindProperty("bundledUnlitShader");
            if (genUnlitProp != null) genUnlitProp.objectReferenceValue = unlitShaderAsset;
            var genEmissProp = genSo.FindProperty("emissionVariantTemplate");
            if (genEmissProp != null) genEmissProp.objectReferenceValue = urpLitEmissionTemplate;
            var genPartTmplProp = genSo.FindProperty("particleTransparentTemplate");
            if (genPartTmplProp != null) genPartTmplProp.objectReferenceValue = particleTransTemplate;

            AssignMaterialProperty(genSo, "groundMaterial", "Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_Ground01.mat");
            AssignMaterialProperty(genSo, "wallMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_BricksWeathered.mat");
            AssignMaterialProperty(genSo, "cabinFloorMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_WoodFlooring.mat");
            AssignMaterialProperty(genSo, "mudMaterial", "Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_Clay.mat");
            AssignMaterialProperty(genSo, "crateMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_WoodFlooring.mat");
            AssignMaterialProperty(genSo, "barkMaterial", "Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_Dry02.mat");
            AssignMaterialProperty(genSo, "leafMaterial", "Assets/YughuesFreeGroundMaterials/Materials/M_YFGM_SoilMoss.mat");
            AssignMaterialProperty(genSo, "blockerMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_Plywood.mat");
            AssignMaterialProperty(genSo, "unbreakableBlockerMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_GabionWall.mat");
            AssignMaterialProperty(genSo, "helicopterMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_Ceiling.mat");
            AssignMaterialProperty(genSo, "helipadMaterial", "Assets/YughuesFreeArchitecturalMaterials/Materials/M_YFAM_TilesConcave.mat");

            AssignPrefabProperty(genSo, "medkitItemPrefab", "Assets/SurvivalUpgrade/Items/Prefabs/Medkit.prefab");
            AssignPrefabProperty(genSo, "bandageItemPrefab", "Assets/SurvivalUpgrade/Items/Prefabs/Bandage.prefab");
            AssignPrefabProperty(genSo, "firstAidItemPrefab", "Assets/SurvivalUpgrade/Items/Prefabs/FirstAidKit.prefab");

            genSo.ApplyModifiedProperties();

            // 6. In-Game UI Systems (Legacy IMGUI reflection bridge + Publish-Grade uGUI Root + InputSystem EventSystem)
            GameObject uiGo = new GameObject("UIManager");
            uiGo.AddComponent<DeadDepthsHUD>();
            uiGo.AddComponent<CrateProgressBarUI>();
            uiGo.AddComponent<ItemNotificationManager>();
            uiGo.AddComponent<CompletionScreenUI>();
            uiGo.AddComponent<DeadDepthsDebugMode>();
            uiGo.AddComponent<DeadDepthsUIRoot>();

            DeadDepthsUIRoot.EnsureInputSystemEventSystem();

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

        private static Material CreateOrUpdateTemplateMaterial(string assetPath, Shader shader, System.Action<Material> configure)
        {
            if (shader == null) return null;
            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                configure?.Invoke(mat);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                mat.shader = shader;
                configure?.Invoke(mat);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void AssignMaterialProperty(SerializedObject so, string propName, string assetPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var prop = so.FindProperty(propName);
            if (mat != null && prop != null) prop.objectReferenceValue = mat;
        }

        private static void AssignPrefabProperty(SerializedObject so, string propName, string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var prop = so.FindProperty(propName);
            if (prefab != null && prop != null) prop.objectReferenceValue = prefab;
        }

        private static VolumeProfile CreateOrUpdateDeadDepthsVolumeProfile()
        {
            string settingsDir = "Assets/Settings";
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            else
            {
                // Remove existing sub-assets cleanly before re-populating
                foreach (var comp in profile.components)
                {
                    if (comp != null) Object.DestroyImmediate(comp, true);
                }
                profile.components.Clear();
            }

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.92f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.65f);

            var vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.02f, 0.03f, 0.06f, 1f));
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.22f);
            colorAdj.contrast.Override(12f);
            colorAdj.colorFilter.Override(new Color(0.93f, 0.96f, 1.0f, 1f));
            colorAdj.saturation.Override(-8f);

            foreach (var comp in profile.components)
            {
                if (comp != null && !AssetDatabase.Contains(comp))
                {
                    AssetDatabase.AddObjectToAsset(comp, profile);
                }
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        [MenuItem("DeadDepths/Build Windows Standalone")]
        public static void BuildWindowsStandalone()
        {
            string buildDir = "Builds";
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/DeadDepths.unity" },
                locationPathName = Path.Combine(buildDir, "DeadDepths.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[DeadDepths] Windows build result: {report.summary.result} | Errors: {report.summary.totalErrors} | Size: {report.summary.totalSize} bytes");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
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
            root.AddComponent<DeadDepthsFlashlight>();
            var reticle = root.AddComponent<DeadDepthsAimReticle>();
            var retSo = new SerializedObject(reticle);
            var retShaderProp = retSo.FindProperty("bundledLineShader");
            if (retShaderProp != null)
            {
                retShaderProp.objectReferenceValue = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
                retSo.ApplyModifiedProperties();
            }

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
                anim.applyRootMotion = false;

                if (model.GetComponent<PlayerAimIK>() == null)
                {
                    model.AddComponent<PlayerAimIK>();
                }
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
            agent.angularSpeed = 360f;
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
                anim.applyRootMotion = false;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
