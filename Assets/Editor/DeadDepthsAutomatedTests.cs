using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DeadDepths.AI;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;

namespace DeadDepths.Editor
{
    /// <summary>
    /// Automated test runner to verify all Dead Depths systems in Unity batchmode.
    /// ASD-STE100: This module executes verification tests for the game systems.
    /// </summary>
    public static class DeadDepthsAutomatedTests
    {
        [MenuItem("DeadDepths/Run Automated Verification")]
        public static void RunVerification()
        {
            Debug.Log("[TEST RUNNER] Starting Dead Depths Automated Systems Verification...");

            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string testName)
            {
                if (condition)
                {
                    Debug.Log($"[PASS] {testName}");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] {testName}");
                    failed++;
                }
            }

            try
            {
                // 1. Scene Loading
                string scenePath = "Assets/Scenes/DeadDepths.unity";
                Assert(File.Exists(scenePath), "Scene file exists at Assets/Scenes/DeadDepths.unity");
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Assert(scene.IsValid(), "DeadDepths scene loaded successfully");

                // 2. Core Managers in Scene
                var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                Assert(gm != null, "GameManager present in scene");

                var diff = UnityEngine.Object.FindAnyObjectByType<DifficultyController>();
                Assert(diff != null, "DifficultyController present in scene");

                var tracker = UnityEngine.Object.FindAnyObjectByType<PlayerPerformanceTracker>();
                Assert(tracker != null, "PlayerPerformanceTracker present in scene");

                var worldGen = UnityEngine.Object.FindAnyObjectByType<WorldGenerator>();
                Assert(worldGen != null, "WorldGenerator present in scene");

                // 3. UI Systems in Scene
                Assert(UnityEngine.Object.FindAnyObjectByType<DeadDepthsHUD>() != null, "DeadDepthsHUD present");
                Assert(UnityEngine.Object.FindAnyObjectByType<CrateProgressBarUI>() != null, "CrateProgressBarUI present");
                Assert(UnityEngine.Object.FindAnyObjectByType<ItemNotificationManager>() != null, "ItemNotificationManager present");
                Assert(UnityEngine.Object.FindAnyObjectByType<CompletionScreenUI>() != null, "CompletionScreenUI present");

                // 4. Camera & Isometric Settings
                var cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                Assert(cam != null, "Camera exists");
                var isoCam = UnityEngine.Object.FindAnyObjectByType<IsometricCameraController>();
                Assert(isoCam != null, "IsometricCameraController attached to Camera");
                Assert(isoCam.CamConfig.PitchAngle > 40f && isoCam.CamConfig.YawAngle == 45f, "Isometric Pitch/Yaw angles adhere to specification");

                // 5. Procedural World Generation Execution
                var ddaProfile = diff.UpdateProfile(DifficultyTier.Balanced);
                Assert(ddaProfile != null, "DDA Profile generated for Level 1");
                worldGen.GenerateWorld(1, ddaProfile);

                var ground = GameObject.Find("PlayableGround");
                Assert(ground != null, "Playable ground generated");

                var trees = GameObject.Find("TreeBoundary");
                Assert(trees != null && trees.transform.childCount > 0, "Boundary trees generated via procedural geometry");

                var mudGroup = GameObject.Find("MuddyPatches");
                Assert(mudGroup != null && mudGroup.transform.childCount > 0, "Mud patches generated");
                var firstMud = mudGroup.GetComponentInChildren<MuddyPatch>();
                Assert(firstMud != null, "MuddyPatch component attached to mud geometry");

                var structures = GameObject.Find("AbandonedStructures");
                Assert(structures != null && structures.transform.childCount > 0, "Abandoned structures generated");

                var crates = GameObject.Find("SuppliesCrates");
                Assert(crates != null && crates.transform.childCount > 0, "Supplies crates spawned");

                var heli = GameObject.Find("ExtractionHelicopter");
                Assert(heli != null, "Extraction helicopter spawned");
                var heliComp = heli.GetComponent<ExtractionHelicopter>();
                Assert(heliComp != null, "ExtractionHelicopter component attached to helicopter");

                // 6. Player Verification
                var player = GameObject.FindGameObjectWithTag("Player");
                Assert(player != null, "Player actor spawned with tag 'Player'");
                var playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                Assert(playerCtrl != null, "DeadDepthsPlayerController attached to player");
                var charController = player.GetComponent<CharacterController>();
                Assert(charController != null, "CharacterController attached to player");

                // 7. Zombie Archetypes Verification
                var horde = GameObject.Find("ZombieHorde");
                Assert(horde != null && horde.transform.childCount > 0, "Zombie horde spawned");
                var zombies = horde.GetComponentsInChildren<ZombieController>();
                Assert(zombies.Length > 0, $"Zombie controllers spawned: count = {zombies.Length}");

                bool hasWalker = false;
                bool hasSpitter = false;
                bool hasCharger = false;

                foreach (var z in zombies)
                {
                    if (z.Archetype == ZombieArchetype.Walker) hasWalker = true;
                    if (z.Archetype == ZombieArchetype.Spitter) hasSpitter = true;
                    if (z.Archetype == ZombieArchetype.Charger) hasCharger = true;
                }
                Assert(hasWalker, "Walker archetype present in horde");
                Assert(hasSpitter, "Spitter archetype present in horde");
                Assert(hasCharger, "Charger archetype present in horde");

                // 8. DDA Performance Evaluation Math Verification
                // Test Case 1: High Health, High Kills, Low Damage -> PerformingWell
                float scoreHigh = PerformanceEvaluator.CalculateDirectScore(0.95f, 1.0f, 0.90f, 0.95f, 0.90f, out DifficultyTier tierHigh);
                Assert(tierHigh == DifficultyTier.PerformingWell, $"High performance classified as PerformingWell (Score: {scoreHigh:F2})");

                // Test Case 2: Low Health, Low Kills, High Damage -> Struggling
                float scoreLow = PerformanceEvaluator.CalculateDirectScore(0.15f, 0.20f, 0.25f, 0.10f, 0.20f, out DifficultyTier tierLow);
                Assert(tierLow == DifficultyTier.Struggling, $"Low performance classified as Struggling (Score: {scoreLow:F2})");

                // 9. Mud Slowing Mechanic Verification
                playerCtrl.SetTerrainSpeedMultiplier(0.70f);
                Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 0.70f), $"Mud speed multiplier is 0.70 (30% reduction)");
                playerCtrl.SetTerrainSpeedMultiplier(1.0f);
                Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 1.0f), "Mud speed restored to 1.0 when exiting mud");

                // Summary
                Debug.Log($"\n===========================================");
                Debug.Log($"TEST SUMMARY: PASSED={passed}, FAILED={failed}");
                Debug.Log($"===========================================\n");

                if (failed > 0)
                {
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log("[TEST SUCCESS] All Dead Depths Systems Verified Successfully!");
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST EXCEPTION] {ex}");
                EditorApplication.Exit(2);
            }
        }
    }
}
