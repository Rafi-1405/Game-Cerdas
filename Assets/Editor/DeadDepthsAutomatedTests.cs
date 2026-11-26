using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using DeadDepths.AI;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;
using UTS.Audio;

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

                // 3. UI & Debug Systems in Scene
                Assert(UnityEngine.Object.FindAnyObjectByType<DeadDepthsHUD>() != null, "DeadDepthsHUD present");
                Assert(UnityEngine.Object.FindAnyObjectByType<CrateProgressBarUI>() != null, "CrateProgressBarUI present");
                Assert(UnityEngine.Object.FindAnyObjectByType<ItemNotificationManager>() != null, "ItemNotificationManager present");
                Assert(UnityEngine.Object.FindAnyObjectByType<CompletionScreenUI>() != null, "CompletionScreenUI present");
                Assert(UnityEngine.Object.FindAnyObjectByType<DeadDepthsDebugMode>() != null, "DeadDepthsDebugMode present");

                // 4. Camera & Isometric Settings
                var cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                Assert(cam != null, "Camera exists");
                var isoCam = UnityEngine.Object.FindAnyObjectByType<IsometricCameraController>();
                Assert(isoCam != null, "IsometricCameraController attached to Camera");
                Assert(isoCam.CamConfig.PitchAngle > 40f && isoCam.CamConfig.YawAngle == 45f, "Isometric Pitch/Yaw angles adhere to specification");

                // 5. Cluster-Based Procedural World Generation Execution
                var ddaProfile = diff.UpdateProfile(DifficultyTier.Balanced);
                Assert(ddaProfile != null, "DDA Profile generated for Level 1");
                worldGen.GenerateWorld(1, ddaProfile);

                Assert(worldGen.Clusters.Count >= 4 && worldGen.Clusters.Count <= 6,
                    $"Cluster map generated 4-6 clusters (count = {worldGen.Clusters.Count})");
                Assert(worldGen.Corridors.Count >= worldGen.Clusters.Count - 1,
                    $"Clusters connected with corridors (count = {worldGen.Corridors.Count})");
                Assert(worldGen.StartClusterId != worldGen.EvacuationClusterId,
                    $"Start cluster (#{worldGen.StartClusterId}) and Evacuation cluster (#{worldGen.EvacuationClusterId}) are separate");
                Assert(worldGen.ConnectivityValid, "BFS WorldValidator confirms Start -> Evacuation & Crates connectivity");

                var ground = GameObject.Find("PlayableGround");
                Assert(ground != null, "Playable ground generated");

                var trees = GameObject.Find("TreeBoundary");
                Assert(trees != null && trees.transform.childCount > 0, "Boundary trees generated framing clusters/corridors");

                var mudGroup = GameObject.Find("MuddyPatches");
                Assert(mudGroup != null && mudGroup.transform.childCount > 0, "Mud patches generated");
                var firstMud = mudGroup.GetComponentInChildren<MuddyPatch>();
                Assert(firstMud != null, "MuddyPatch component attached to mud geometry");

                var crates = GameObject.Find("SuppliesCrates");
                Assert(crates != null && crates.transform.childCount >= 3, $"Supplies crates spawned (count = {crates.transform.childCount})");

                var heli = GameObject.Find("ExtractionHelicopter");
                Assert(heli != null, "Extraction helicopter spawned in Evacuation cluster");
                var heliComp = heli.GetComponent<ExtractionHelicopter>();
                Assert(heliComp != null, "ExtractionHelicopter component attached to helicopter");

                // 6. Player Verification & Combat / God Mode / Reset
                var player = GameObject.FindGameObjectWithTag("Player");
                Assert(player != null, "Player actor spawned with tag 'Player'");
                var playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                Assert(playerCtrl != null, "DeadDepthsPlayerController attached to player");
                var charController = player.GetComponent<CharacterController>();
                Assert(charController != null, "CharacterController attached to player");

                playerCtrl.ResetStateForLevel();
                Assert(!playerCtrl.IsDead && Mathf.Approximately(playerCtrl.CurrentHealth, playerCtrl.MaxHealth),
                    "Player initializes alive at full health");

                // Damage & Heal Verification
                playerCtrl.TakeDamage(40f);
                Assert(Mathf.Approximately(playerCtrl.CurrentHealth, 60f), "Player TakeDamage reduces health accurately (100 -> 60)");
                playerCtrl.Heal(25f);
                Assert(Mathf.Approximately(playerCtrl.CurrentHealth, 85f), "Player Heal restores health accurately (60 -> 85)");

                // SuppliesCrate open & heal verification
                var testCrate = crates.GetComponentInChildren<SuppliesCrate>();
                Assert(testCrate != null, "Found SuppliesCrate to test interaction");
                if (testCrate != null)
                {
                    player.transform.position = testCrate.transform.position;
                    float hpBeforeCrate = playerCtrl.CurrentHealth;
                    testCrate.StartInteraction(player.transform);
                    testCrate.UpdateInteraction(testCrate.CrateConfig.OpenDuration + 0.1f);
                    Assert(testCrate.IsOpened, "SuppliesCrate transitions to IsOpened after full duration");
                    Assert(playerCtrl.CurrentHealth > hpBeforeCrate, $"Opening SuppliesCrate heals player ({hpBeforeCrate} -> {playerCtrl.CurrentHealth})");
                }

                // God Mode Verification (Invulnerable + Noclip)
                playerCtrl.SetGodMode(true);
                Assert(playerCtrl.IsGodMode && !charController.enabled, "God Mode enables and disables CharacterController for Noclip");
                float hpBeforeGodHit = playerCtrl.CurrentHealth;
                playerCtrl.TakeDamage(999f);
                Assert(Mathf.Approximately(playerCtrl.CurrentHealth, hpBeforeGodHit), "God Mode blocks incoming damage completely");
                playerCtrl.SetGodMode(false);
                Assert(!playerCtrl.IsGodMode && charController.enabled, "Disabling God Mode restores CharacterController");

                // Death & Reset Verification (B3 / Poin 4)
                playerCtrl.TakeDamage(999f);
                Assert(playerCtrl.IsDead, "Player enters IsDead when health reaches 0");
                worldGen.GenerateWorld(1, ddaProfile);
                Assert(!playerCtrl.IsDead && Mathf.Approximately(playerCtrl.CurrentHealth, playerCtrl.MaxHealth),
                    "Regenerating level revives player at full health");

                // 7. Zombie Archetypes & Stats Verification (B1 / Poin 5)
                var horde = GameObject.Find("ZombieHorde");
                Assert(horde != null && horde.transform.childCount > 0, "Zombie horde spawned");
                var zombies = horde.GetComponentsInChildren<ZombieController>();
                Assert(zombies.Length > 0, $"Zombie controllers spawned: count = {zombies.Length}");

                ZombieController walkerSample = null;
                ZombieController spitterSample = null;
                ZombieController chargerSample = null;

                foreach (var z in zombies)
                {
                    if (z.Archetype == ZombieArchetype.Walker) walkerSample = z;
                    if (z.Archetype == ZombieArchetype.Spitter) spitterSample = z;
                    if (z.Archetype == ZombieArchetype.Charger) chargerSample = z;
                }
                Assert(walkerSample != null, "Walker archetype present in horde");
                Assert(spitterSample != null, "Spitter archetype present in horde");
                Assert(chargerSample != null, "Charger archetype present in horde");

                if (walkerSample != null)
                {
                    Assert(Mathf.Approximately(walkerSample.AiConfig.MaxHealth, 80f) && Mathf.Approximately(walkerSample.AiConfig.BaseMoveSpeed, 3.5f),
                        $"Walker stats initialized accurately (HP={walkerSample.AiConfig.MaxHealth}, Speed={walkerSample.AiConfig.BaseMoveSpeed})");
                }
                if (spitterSample != null)
                {
                    Assert(Mathf.Approximately(spitterSample.AiConfig.MaxHealth, 60f) && Mathf.Approximately(spitterSample.AiConfig.BaseMoveSpeed, 6.2f),
                        $"Spitter stats initialized accurately (HP={spitterSample.AiConfig.MaxHealth}, Speed={spitterSample.AiConfig.BaseMoveSpeed})");
                }
                if (chargerSample != null)
                {
                    Assert(Mathf.Approximately(chargerSample.AiConfig.MaxHealth, 160f) && Mathf.Approximately(chargerSample.AiConfig.BaseMoveSpeed, 2.4f) && chargerSample.transform.localScale.x > 1.2f,
                        $"Charger stats & 1.3x scale initialized accurately (HP={chargerSample.AiConfig.MaxHealth}, Speed={chargerSample.AiConfig.BaseMoveSpeed}, Scale={chargerSample.transform.localScale.x:F2})");
                }

                // Zombie TakeDamage verification (B4 / Poin 3)
                if (walkerSample != null)
                {
                    float zHpBefore = walkerSample.CurrentHealth;
                    walkerSample.TakeDamage(25f, player.transform.position);
                    Assert(Mathf.Approximately(walkerSample.CurrentHealth, zHpBefore - 25f),
                        $"Zombie TakeDamage reduces HP cleanly ({zHpBefore} -> {walkerSample.CurrentHealth})");
                }

                // 8. Scoreboard Continue Run vs Retry Score Reset Verification
                gm.AddScore(350);
                int keptScore = gm.TotalScore;
                Assert(keptScore >= 350, $"GameManager tracks TotalScore ({keptScore})");
                gm.ProceedToNextLevel();
                Assert(gm.TotalScore == keptScore && gm.CurrentLevel == 2,
                    $"Continue Run keeps TotalScore ({gm.TotalScore}) and advances to Sector {gm.CurrentLevel}");
                gm.RetryRun();
                Assert(gm.TotalScore == 0 && gm.CurrentLevel == 1,
                    $"RetryRun resets TotalScore to 0 and restarts at Sector {gm.CurrentLevel}");

                // 9. DDA Performance Evaluation Math Verification
                float scoreHigh = PerformanceEvaluator.CalculateDirectScore(0.95f, 1.0f, 0.90f, 0.95f, 0.90f, out DifficultyTier tierHigh);
                Assert(tierHigh == DifficultyTier.PerformingWell, $"High performance classified as PerformingWell (Score: {scoreHigh:F2})");

                float scoreLow = PerformanceEvaluator.CalculateDirectScore(0.15f, 0.20f, 0.25f, 0.10f, 0.20f, out DifficultyTier tierLow);
                Assert(tierLow == DifficultyTier.Struggling, $"Low performance classified as Struggling (Score: {scoreLow:F2})");

                // 10. Mud Slowing Mechanic Verification & Upward Mesh Normals
                playerCtrl.SetTerrainSpeedMultiplier(0.70f);
                Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 0.70f), $"Mud speed multiplier is 0.70 (30% reduction)");
                playerCtrl.SetTerrainSpeedMultiplier(1.0f);
                Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 1.0f), "Mud speed restored to 1.0 when exiting mud");

                Mesh mudMesh = ProceduralGeometry.CreateMudPatchMesh(4f, 4f, 12);
                Vector3[] mv = mudMesh.vertices;
                int[] mt = mudMesh.triangles;
                Vector3 mudCross = Vector3.Cross(mv[mt[1]] - mv[mt[0]], mv[mt[2]] - mv[mt[0]]);
                Assert(mudCross.y > 0f, $"MudPatch mesh triangles face upward (+Y normal = {mudCross.y:F2})");

                // 11. Multi-Seed Cluster PCG & NavMesh Pathfinding Stress Verification (5 seeds)
                bool allSeedsValid = true;
                int minClustersSeen = int.MaxValue;
                int maxClustersSeen = 0;
                for (int seedStep = 1; seedStep <= 5; seedStep++)
                {
                    worldGen.GenerateWorld(seedStep, ddaProfile);
                    minClustersSeen = Mathf.Min(minClustersSeen, worldGen.Clusters.Count);
                    maxClustersSeen = Mathf.Max(maxClustersSeen, worldGen.Clusters.Count);

                    NavMeshPath navPath = new NavMeshPath();
                    bool startOnNav = NavMesh.SamplePosition(worldGen.PlayerSpawnPosition, out NavMeshHit startHit, 3.0f, NavMesh.AllAreas);
                    bool evacOnNav = NavMesh.SamplePosition(worldGen.ExtractionPosition + new Vector3(0f, 0f, -4.2f), out NavMeshHit evacHit, 5.0f, NavMesh.AllAreas);
                    bool navFound = startOnNav && evacOnNav && NavMesh.CalculatePath(startHit.position, evacHit.position, NavMesh.AllAreas, navPath);
                    bool seedOk = worldGen.Clusters.Count >= 4 && worldGen.Clusters.Count <= 6 &&
                                  worldGen.StartClusterId != worldGen.EvacuationClusterId &&
                                  worldGen.ConnectivityValid &&
                                  worldGen.TotalCratesSpawned >= 3 &&
                                  navFound && navPath.status == NavMeshPathStatus.PathComplete;
                    if (!seedOk)
                    {
                        allSeedsValid = false;
                        Debug.LogError($"[SEED FAIL] Level {seedStep}: Clusters={worldGen.Clusters.Count}, Connected={worldGen.ConnectivityValid}, Crates={worldGen.TotalCratesSpawned}, Nav={navFound}/{navPath.status}");
                    }
                }
                Assert(allSeedsValid, $"Multi-seed cluster PCG + NavMesh Complete Path valid across 5 levels (clusters={minClustersSeen}..{maxClustersSeen})");

                // Regression check for Seed 770591 (previously failed NavMesh PathComplete due to blocker/tree pinch)
                bool prevRandomSeed = worldGen.MapConfig.UseRandomSeed;
                int prevSeed = worldGen.MapConfig.Seed;
                worldGen.MapConfig.UseRandomSeed = false;
                worldGen.MapConfig.Seed = 770591 - 5 * 101;
                worldGen.GenerateWorld(5, ddaProfile);
                worldGen.MapConfig.UseRandomSeed = prevRandomSeed;
                worldGen.MapConfig.Seed = prevSeed;
                NavMeshPath regPath = new NavMeshPath();
                bool regStart = NavMesh.SamplePosition(worldGen.PlayerSpawnPosition, out NavMeshHit regStartHit, 3.0f, NavMesh.AllAreas);
                bool regEvac = NavMesh.SamplePosition(worldGen.ExtractionPosition + new Vector3(0f, 0f, -4.2f), out NavMeshHit regEvacHit, 5.0f, NavMesh.AllAreas);
                bool regNav = regStart && regEvac && NavMesh.CalculatePath(regStartHit.position, regEvacHit.position, NavMesh.AllAreas, regPath);
                Assert(worldGen.LastSeed == 770591 && worldGen.ConnectivityValid && regNav && regPath.status == NavMeshPathStatus.PathComplete,
                    $"PCG regression Seed 770591 produces valid NavMesh PathComplete (Connected={worldGen.ConnectivityValid}, Status={regPath.status})");

                // Refresh scene entities references after multi-seed loop
                player = GameObject.FindGameObjectWithTag("Player");
                playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                charController = player.GetComponent<CharacterController>();
                horde = GameObject.Find("ZombieHorde");
                zombies = horde.GetComponentsInChildren<ZombieController>();

                // 12. Player Punch Hit Detection against Zombie & Score Gain
                ZombieController targetWalker = null;
                foreach (var z in zombies)
                {
                    if (z.Archetype == ZombieArchetype.Walker && !z.IsDead)
                    {
                        targetWalker = z;
                        break;
                    }
                }
                Assert(targetWalker != null, "Found live Walker to test PerformPunchHitDetection");
                if (targetWalker != null)
                {
                    targetWalker.transform.position = player.transform.position + player.transform.forward * 1.2f;
                    Physics.SyncTransforms();
                    float zHpPre = targetWalker.CurrentHealth;
                    playerCtrl.PerformPunchHitDetection();
                    Assert(Mathf.Approximately(targetWalker.CurrentHealth, zHpPre - playerCtrl.PlayerConfig.PunchDamage),
                        $"Player PerformPunchHitDetection damages zombie in front arc ({zHpPre} -> {targetWalker.CurrentHealth})");
                }

                // 13. God Mode Exit-Overlap Safety Verification (Noclip -> Solid Wall -> Exit)
                var wallObj = GameObject.Find("StructureWall");
                if (wallObj != null)
                {
                    playerCtrl.SetGodMode(true);
                    player.transform.position = new Vector3(wallObj.transform.position.x, 0.05f, wallObj.transform.position.z);
                    Physics.SyncTransforms();
                    playerCtrl.SetGodMode(false);
                    Vector3 exitPos = player.transform.position;
                    float distMoved = Vector3.Distance(new Vector3(wallObj.transform.position.x, 0.05f, wallObj.transform.position.z), exitPos);
                    Assert(charController.enabled && distMoved > 0.3f,
                        $"Disabling God Mode inside StructureWall safely pushes player out ({distMoved:F2}m)");
                }

                // 14. Scoreboard CompleteLevel -> Continue Run vs RetryRun Full Flow
                gm.RetryRun();
                Assert(gm.TotalScore == 0 && gm.IsLevelActive, "RetryRun initializes clean active run with score 0");
                gm.RecordZombieKill(ZombieArchetype.Charger);
                gm.RecordCrateOpened();
                int preExtractScore = gm.TotalScore;
                Assert(preExtractScore == gm.ManagerConfig.ChargerKillScore + gm.ManagerConfig.CrateOpenScore,
                    $"Kill + Crate scoring accurate ({preExtractScore})");
                gm.CompleteLevel();
                var compScreen = UnityEngine.Object.FindAnyObjectByType<CompletionScreenUI>();
                Assert(compScreen != null && compScreen.IsVisible && gm.TotalScore > preExtractScore,
                    $"CompleteLevel shows Evacuation Scoreboard & adds level bonus ({preExtractScore} -> {gm.TotalScore})");
                int scoreAfterExtract = gm.TotalScore;
                gm.ProceedToNextLevel();
                Assert(!compScreen.IsVisible && gm.TotalScore == scoreAfterExtract && gm.CurrentLevel == 2 && Time.timeScale == 1f,
                    $"Continue Run closes Scoreboard, keeps score ({gm.TotalScore}), restores timeScale, advances to Sector 2");
                gm.RetryRun();

                // 15. Debug Mode Toggle Verification
                var debugMode = UnityEngine.Object.FindAnyObjectByType<DeadDepthsDebugMode>();
                Assert(debugMode != null, "DeadDepthsDebugMode component available");
                if (debugMode != null)
                {
                    debugMode.SetDebugVisible(true);
                    Assert(debugMode.IsDebugVisible, "DeadDepthsDebugMode toggles ON");
                    debugMode.SetDebugVisible(false);
                    Assert(!debugMode.IsDebugVisible, "DeadDepthsDebugMode toggles OFF");
                }

                // Refresh references after RetryRun() regenerated WorldRoot
                player = GameObject.FindGameObjectWithTag("Player");
                playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                charController = player.GetComponent<CharacterController>();
                horde = GameObject.Find("ZombieHorde");
                zombies = horde != null ? horde.GetComponentsInChildren<ZombieController>() : Array.Empty<ZombieController>();
                wallObj = GameObject.Find("StructureWall");

                // 16. Root Motion Disabled & Zero Visual Model Drift Verification
                var playerAnimator = player.GetComponentInChildren<Animator>();
                Assert(playerAnimator != null && !playerAnimator.applyRootMotion,
                    "Player Animator has applyRootMotion = false (prevents visual model drifting off screen center)");
                if (playerAnimator != null)
                {
                    playerAnimator.SetFloat("Speed", 5.2f);
                    for (int f = 0; f < 60; f++) playerAnimator.Update(0.02f);
                    playerAnimator.SetTrigger("Punch");
                    for (int f = 0; f < 40; f++) playerAnimator.Update(0.02f);
                    playerAnimator.SetBool("Crouching", true);
                    playerAnimator.SetFloat("Speed", 1.1f);
                    for (int f = 0; f < 40; f++) playerAnimator.Update(0.02f);

                    float playerDrift = playerAnimator.transform.localPosition.magnitude;
                    float playerRotDrift = Quaternion.Angle(playerAnimator.transform.localRotation, Quaternion.identity);
                    Assert(playerDrift < 0.001f && playerRotDrift < 0.1f,
                        $"PlayerModel stays locked to Player root after Walk/Run/Punch/Crouch cycles (posDrift={playerDrift:F5}m, rotDrift={playerRotDrift:F2}deg)");
                }

                var sampleZombie = horde != null ? horde.GetComponentInChildren<ZombieController>() : null;
                var zombieAnimator = sampleZombie != null ? sampleZombie.GetComponentInChildren<Animator>() : null;
                Assert(zombieAnimator != null && !zombieAnimator.applyRootMotion,
                    "Zombie Animator has applyRootMotion = false (prevents visual zombie drifting away from NavMeshAgent/Collider)");
                if (zombieAnimator != null)
                {
                    zombieAnimator.SetFloat("Speed", 2.3f);
                    for (int f = 0; f < 60; f++) zombieAnimator.Update(0.02f);
                    zombieAnimator.SetTrigger("Attack");
                    for (int f = 0; f < 40; f++) zombieAnimator.Update(0.02f);

                    float zDrift = zombieAnimator.transform.localPosition.magnitude;
                    float zRotDrift = Quaternion.Angle(zombieAnimator.transform.localRotation, Quaternion.identity);
                    Assert(zDrift < 0.001f && zRotDrift < 0.1f,
                        $"ZombieModel stays locked to Zombie root after Walk/Run/Attack cycles (posDrift={zDrift:F5}m, rotDrift={zRotDrift:F2}deg)");
                }

                // 17. Camera Centering & Snap Verification
                isoCam.SnapToTarget(player.transform);
                Vector3 playerViewport = cam.WorldToViewportPoint(player.transform.position + isoCam.CamConfig.TargetOffset);
                Assert(Mathf.Abs(playerViewport.x - 0.5f) < 0.02f && Mathf.Abs(playerViewport.y - 0.5f) < 0.02f && playerViewport.z > 0f,
                    $"IsometricCameraController centers player in viewport ({playerViewport.x:F3}, {playerViewport.y:F3})");

                // 18. Punch Occlusion Through Solid Wall & Multi-Zombie Cleave Verification
                if (wallObj != null && zombies.Length > 1)
                {
                    ZombieController zFront = null;
                    ZombieController zBehind = null;
                    ZombieController zOccluded = null;
                    foreach (var z in zombies)
                    {
                        if (z == null || z.IsDead) continue;
                        if (zFront == null) zFront = z;
                        else if (zBehind == null) zBehind = z;
                        else { zOccluded = z; break; }
                    }
                    if (zFront != null && zBehind != null)
                    {
                        // Place player in open space near start, two zombies inline in front
                        Vector3 openPos = worldGen.PlayerSpawnPosition + Vector3.up * 0.1f;
                        player.transform.position = openPos;
                        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                        zFront.transform.position = openPos + Vector3.forward * 1.1f;
                        zBehind.transform.position = openPos + Vector3.forward * 1.9f;
                        Physics.SyncTransforms();

                        float hp1 = zFront.CurrentHealth;
                        float hp2 = zBehind.CurrentHealth;
                        playerCtrl.PerformPunchHitDetection();
                        Assert(zFront.CurrentHealth < hp1 && zBehind.CurrentHealth < hp2,
                            $"Punch cleaves lined-up zombies without zombie-on-zombie occlusion ({hp1}->{zFront.CurrentHealth}, {hp2}->{zBehind.CurrentHealth})");
                    }
                    if (zOccluded != null)
                    {
                        Vector3 wallPos = wallObj.transform.position;
                        player.transform.position = new Vector3(wallPos.x, 0.1f, wallPos.z - 1.15f);
                        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                        zOccluded.transform.position = new Vector3(wallPos.x, 0.1f, wallPos.z + 1.15f);
                        Physics.SyncTransforms();

                        float hpBeforeWallPunch = zOccluded.CurrentHealth;
                        playerCtrl.PerformPunchHitDetection();
                        Assert(Mathf.Approximately(zOccluded.CurrentHealth, hpBeforeWallPunch),
                            "Player punch cannot hit zombie through a solid StructureWall");
                    }
                }

                // 19. Crate Distance Guard & Dead Player Extraction Safety
                var crateAfterRetry = GameObject.Find("SuppliesCrates")?.GetComponentInChildren<SuppliesCrate>();
                if (crateAfterRetry != null)
                {
                    player.transform.position = crateAfterRetry.transform.position + new Vector3(crateAfterRetry.CrateConfig.InteractionDistance + 0.35f, 0.1f, 0f);
                    crateAfterRetry.StartInteraction(player.transform);
                    crateAfterRetry.UpdateInteraction(0.1f);
                    Assert(!crateAfterRetry.IsInteracting,
                        "SuppliesCrate cancels interaction cleanly when player is beyond InteractionDistance");
                }

                // 20. IMGUI UI Scripts: GUIStyle & Runtime Texture2D NonSerialized + Reset Verification
                MonoBehaviour[] uiComponents = new MonoBehaviour[]
                {
                    UnityEngine.Object.FindAnyObjectByType<DeadDepthsHUD>(),
                    UnityEngine.Object.FindAnyObjectByType<CompletionScreenUI>(),
                    UnityEngine.Object.FindAnyObjectByType<DeadDepthsDebugMode>(),
                    UnityEngine.Object.FindAnyObjectByType<CrateProgressBarUI>(),
                    UnityEngine.Object.FindAnyObjectByType<ItemNotificationManager>()
                };
                bool allUiFieldsNonSerialized = true;
                bool allUiTexturesInitializedCleanly = true;
                foreach (var uiComp in uiComponents)
                {
                    if (uiComp == null)
                    {
                        allUiFieldsNonSerialized = false;
                        continue;
                    }
                    var type = uiComp.GetType();
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var f in fields)
                    {
                        if (f.FieldType == typeof(GUIStyle) || f.FieldType == typeof(Texture2D))
                        {
                            if (!f.IsNotSerialized)
                            {
                                allUiFieldsNonSerialized = false;
                                Debug.LogError($"[UI SERIALIZATION FAIL] {type.Name}.{f.Name} ({f.FieldType.Name}) must be [NonSerialized] to prevent empty GUIStyle/stale Texture2D across hot-reload and Fast Enter Play Mode.");
                            }
                        }
                    }

                    // Verify InitializeTextures/EnsureTextures & ResetStyles do not leave null textures or non-null styles
                    var initTexMethod = type.GetMethod("InitializeTextures", BindingFlags.Instance | BindingFlags.NonPublic);
                    var ensureTexMethod = type.GetMethod("EnsureTextures", BindingFlags.Instance | BindingFlags.NonPublic);
                    var resetStylesMethod = type.GetMethod("ResetStyles", BindingFlags.Instance | BindingFlags.NonPublic);
                    var destroyTexMethod = type.GetMethod("DestroyTextures", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (initTexMethod != null)
                    {
                        initTexMethod.Invoke(uiComp, null);
                        foreach (var f in fields)
                        {
                            if (f.FieldType == typeof(Texture2D) && f.Name != "minimapTexture")
                            {
                                var texVal = f.GetValue(uiComp) as Texture2D;
                                if (texVal == null)
                                {
                                    allUiTexturesInitializedCleanly = false;
                                    Debug.LogError($"[UI TEXTURE FAIL] {type.Name}.{f.Name} remained null after InitializeTextures().");
                                }
                            }
                        }
                    }

                    if (resetStylesMethod != null)
                    {
                        resetStylesMethod.Invoke(uiComp, null);
                        foreach (var f in fields)
                        {
                            if (f.FieldType == typeof(GUIStyle) && f.GetValue(uiComp) != null)
                            {
                                allUiTexturesInitializedCleanly = false;
                                Debug.LogError($"[UI STYLE RESET FAIL] {type.Name}.{f.Name} remained non-null after ResetStyles().");
                            }
                        }
                    }

                    if (destroyTexMethod != null)
                    {
                        destroyTexMethod.Invoke(uiComp, null);
                    }

                    if (ensureTexMethod != null)
                    {
                        ensureTexMethod.Invoke(uiComp, null);
                        foreach (var f in fields)
                        {
                            if (f.FieldType == typeof(Texture2D) && f.Name != "minimapTexture")
                            {
                                var texVal = f.GetValue(uiComp) as Texture2D;
                                if (texVal == null)
                                {
                                    allUiTexturesInitializedCleanly = false;
                                    Debug.LogError($"[UI TEXTURE ENSURE FAIL] {type.Name}.{f.Name} remained null after EnsureTextures().");
                                }
                            }
                        }
                        if (destroyTexMethod != null)
                        {
                            destroyTexMethod.Invoke(uiComp, null);
                        }
                    }
                }
                Assert(allUiFieldsNonSerialized,
                    "All DeadDepths.UI GUIStyle & runtime Texture2D fields are marked [NonSerialized]");
                Assert(allUiTexturesInitializedCleanly,
                    "All DeadDepths.UI components initialize runtime Texture2Ds and reset GUIStyles cleanly");

                // 21. Workstream A: Yughues Materials, 2-Submesh Tree, MainRotor, GreenSmoke ParticleSystem & Audio Cues
                Assert(worldGen.BarkMaterial != null && worldGen.BarkMaterial.name == "M_YFGM_Dry02",
                    $"WorldGenerator BarkMaterial wired to M_YFGM_Dry02 (actual={worldGen.BarkMaterial?.name})");
                Assert(worldGen.LeafMaterial != null && worldGen.LeafMaterial.name == "M_YFGM_SoilMoss",
                    $"WorldGenerator LeafMaterial wired to M_YFGM_SoilMoss (actual={worldGen.LeafMaterial?.name})");
                Assert(worldGen.BlockerMaterial != null && worldGen.BlockerMaterial.name == "M_YFAM_Plywood",
                    $"WorldGenerator BlockerMaterial wired to M_YFAM_Plywood (actual={worldGen.BlockerMaterial?.name})");
                Assert(worldGen.UnbreakableBlockerMaterial != null && worldGen.UnbreakableBlockerMaterial.name == "M_YFAM_GabionWall",
                    $"WorldGenerator UnbreakableBlockerMaterial wired to M_YFAM_GabionWall (actual={worldGen.UnbreakableBlockerMaterial?.name})");
                Assert(worldGen.HelicopterMaterial != null && worldGen.HelicopterMaterial.name == "M_YFAM_Ceiling",
                    $"WorldGenerator HelicopterMaterial wired to M_YFAM_Ceiling (actual={worldGen.HelicopterMaterial?.name})");

                var treeBoundary = GameObject.Find("TreeBoundary");
                var firstTree = treeBoundary != null && treeBoundary.transform.childCount > 0
                    ? treeBoundary.transform.GetChild(0)
                    : null;
                var firstTreeMf = firstTree != null ? firstTree.GetComponent<MeshFilter>() : null;
                var firstTreeMr = firstTree != null ? firstTree.GetComponent<MeshRenderer>() : null;
                Assert(firstTreeMf != null && firstTreeMf.sharedMesh != null && firstTreeMf.sharedMesh.subMeshCount == 2,
                    $"Procedural tree mesh split into 2 submeshes (subMeshCount={firstTreeMf?.sharedMesh?.subMeshCount})");
                Assert(firstTreeMr != null && firstTreeMr.sharedMaterials.Length == 2 &&
                       firstTreeMr.sharedMaterials[0] == worldGen.BarkMaterial &&
                       firstTreeMr.sharedMaterials[1] == worldGen.LeafMaterial,
                    "Tree MeshRenderer assigns [barkMaterial, leafMaterial] across submeshes 0 and 1");

                var blockerGroup = GameObject.Find("BlockingObjects");
                if (blockerGroup != null)
                {
                    bool blockerMatsOk = true;
                    foreach (Transform bChild in blockerGroup.transform)
                    {
                        var bMr = bChild.GetComponent<Renderer>();
                        if (bChild.name == "Breakable_Barricade" && bMr.sharedMaterial != worldGen.BlockerMaterial)
                            blockerMatsOk = false;
                        if (bChild.name == "Unbreakable_Pillar" && bMr.sharedMaterial != worldGen.UnbreakableBlockerMaterial)
                            blockerMatsOk = false;
                    }
                    Assert(blockerMatsOk, "BlockingObjects differentiate Breakable_Barricade (Plywood) vs Unbreakable_Pillar (GabionWall)");
                }

                var heliAfterRetry = GameObject.Find("ExtractionHelicopter");
                var mainRotor = heliAfterRetry != null ? heliAfterRetry.transform.Find("MainRotor") : null;
                var rotorMf = mainRotor != null ? mainRotor.GetComponent<MeshFilter>() : null;
                Assert(mainRotor != null && rotorMf != null && rotorMf.sharedMesh != null,
                    "ExtractionHelicopter contains separate MainRotor child with rotor mesh");

                var smokeCanister = heliAfterRetry != null ? heliAfterRetry.transform.Find("GreenSmokeCanister") : null;
                var smokePs = smokeCanister != null ? smokeCanister.GetComponent<ParticleSystem>() : null;
                var smokePsRend = smokeCanister != null ? smokeCanister.GetComponent<ParticleSystemRenderer>() : null;
                Assert(smokePs != null && smokePsRend != null && smokePsRend.sharedMaterial != null &&
                       smokePs.main.scalingMode == ParticleSystemScalingMode.Shape &&
                       smokePsRend.sharedMaterial.HasProperty("_Surface") &&
                       Mathf.Approximately(smokePsRend.sharedMaterial.GetFloat("_Surface"), 1f) &&
                       smokePsRend.sharedMaterial.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent,
                    "GreenSmokeCanister has configured URP Transparent ParticleSystem green smoke flare with Shape scaling");

                // Verify outward triangle winding across all procedural meshes
                bool AllTrianglesOutward(Mesh m)
                {
                    if (m == null) return false;
                    Vector3[] verts = m.vertices;
                    Vector3[] norms = m.normals;
                    for (int sub = 0; sub < m.subMeshCount; sub++)
                    {
                        int[] tris = m.GetTriangles(sub);
                        for (int t = 0; t + 2 < tris.Length; t += 3)
                        {
                            Vector3 e1 = verts[tris[t + 1]] - verts[tris[t]];
                            Vector3 e2 = verts[tris[t + 2]] - verts[tris[t]];
                            Vector3 faceCross = Vector3.Cross(e1, e2);
                            if (faceCross.sqrMagnitude < 1e-10f) continue;
                            Vector3 avgNorm = (norms[tris[t]] + norms[tris[t + 1]] + norms[tris[t + 2]]).normalized;
                            if (Vector3.Dot(faceCross, avgNorm) <= 0f) return false;
                        }
                    }
                    return true;
                }
                Assert(AllTrianglesOutward(ProceduralGeometry.CreateTreeMesh()) &&
                       AllTrianglesOutward(ProceduralGeometry.CreateCrateMesh()) &&
                       AllTrianglesOutward(ProceduralGeometry.CreateHelicopterMesh()) &&
                       AllTrianglesOutward(ProceduralGeometry.CreateHelicopterRotorMesh()) &&
                       AllTrianglesOutward(ProceduralGeometry.CreateMudPatchMesh()),
                    "All ProceduralGeometry meshes (Tree, Crate, Helicopter, Rotor, MudPatch) have outward clockwise triangle winding");

                var audioMgr = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
                Assert(audioMgr != null && audioMgr.SoundLibrary != null &&
                       audioMgr.SoundLibrary.Get(SfxCue.Wind)?.clips?.Length > 0 &&
                       audioMgr.SoundLibrary.Get(SfxCue.FootGrass)?.clips?.Length > 0,
                    "AudioManager SoundLibrary contains Wind ambient & FootGrass footstep clips");

                // 22. Workstream B: ZombieController StateChangeLog & CurrentTargetLabel
                ZombieController patrolWalker = null;
                foreach (var z in zombies)
                {
                    if (z != null && !z.IsDead && z.Archetype == ZombieArchetype.Walker)
                    {
                        patrolWalker = z;
                        if (z.CurrentState == ZombieState.IdlePatrol) break;
                    }
                }
                if (patrolWalker != null && patrolWalker.CurrentState != ZombieState.IdlePatrol)
                {
                    patrolWalker.TransitionToState(ZombieState.IdlePatrol, "ResetForLogTest");
                }
                ZombieController.ClearStateChangeLog();
                Assert(ZombieController.StateChangeLog.Count == 0, "ZombieController.ClearStateChangeLog resets transition ring buffer");
                Assert(patrolWalker != null, "Found patrolling Walker to verify StateChangeLog");
                if (patrolWalker != null)
                {
                    patrolWalker.TakeDamage(5f, player.transform.position);
                    Assert(patrolWalker.CurrentState == ZombieState.Chase &&
                           patrolWalker.CurrentTargetLabel == "Player" &&
                           ZombieController.StateChangeLog.Count >= 1 &&
                           ZombieController.StateChangeLog[ZombieController.StateChangeLog.Count - 1].ToState == ZombieState.Chase,
                        "ZombieController records IdlePatrol -> Chase transition in StateChangeLog and updates CurrentTargetLabel");

                    // Verify Dead transition & ring buffer capacity cap (10)
                    for (int logIdx = 0; logIdx < 12; logIdx++)
                    {
                        patrolWalker.TransitionToState(logIdx % 2 == 0 ? ZombieState.Search : ZombieState.Chase, "RingTest");
                    }
                    patrolWalker.TakeDamage(999f, player.transform.position);
                    Assert(patrolWalker.CurrentState == ZombieState.Dead &&
                           patrolWalker.CurrentTargetLabel == "None" &&
                           ZombieController.StateChangeLog.Count == 10 &&
                           ZombieController.StateChangeLog[ZombieController.StateChangeLog.Count - 1].ToState == ZombieState.Dead,
                        "ZombieController StateChangeLog stays bounded at 10 entries and logs Dead transition with Target=None");
                }

                // 23. Workstream A: ObstructionFadeController URP Transparent Swap & Restore (1-slot Wall, 2-slot Tree & Particle Skip)
                var fadeCtrl = cam.GetComponent<ObstructionFadeController>();
                Assert(fadeCtrl != null, "ObstructionFadeController attached to Main Camera");
                if (fadeCtrl != null && wallObj != null)
                {
                    var wallRend = wallObj.GetComponent<Renderer>();
                    Material origWallMat = wallRend.sharedMaterial;
                    Vector3 wallCenter = wallObj.transform.position;

                    cam.transform.position = wallCenter + new Vector3(0f, 3.0f, -5.0f);
                    player.transform.position = wallCenter + new Vector3(0f, 0f, 5.0f);
                    Physics.SyncTransforms();

                    fadeCtrl.UpdateFade(0.5f);
                    Material fadedMat = wallRend.sharedMaterial;
                    bool swappedToTransparent = fadedMat != null &&
                                                fadedMat != origWallMat &&
                                                fadedMat.HasProperty("_Surface") &&
                                                Mathf.Approximately(fadedMat.GetFloat("_Surface"), 1f) &&
                                                fadedMat.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    Assert(swappedToTransparent,
                        $"ObstructionFadeController swaps Opaque URP material to Transparent clone when obstructing (queue={fadedMat?.renderQueue})");

                    // Move player in front of wall so it no longer obstructs
                    player.transform.position = wallCenter + new Vector3(0f, 0f, -2.0f);
                    Physics.SyncTransforms();
                    for (int step = 0; step < 5; step++)
                    {
                        fadeCtrl.UpdateFade(0.5f);
                    }
                    Assert(wallRend.sharedMaterial == origWallMat,
                        "ObstructionFadeController restores OriginalSharedMaterial once obstruction clears");

                    // Verify 2-submesh BoundaryTree swap & restore
                    if (firstTree != null && firstTreeMr != null)
                    {
                        Vector3 treeCenter = firstTree.position + Vector3.up * 2.0f;
                        cam.transform.position = treeCenter + new Vector3(0f, 2.0f, -5.0f);
                        player.transform.position = treeCenter + new Vector3(0f, 0f, 5.0f);
                        Physics.SyncTransforms();

                        fadeCtrl.UpdateFade(0.5f);
                        Material[] fadedTreeMats = firstTreeMr.sharedMaterials;
                        bool treeSwapped = fadedTreeMats.Length == 2 &&
                                           fadedTreeMats[0] != worldGen.BarkMaterial &&
                                           fadedTreeMats[1] != worldGen.LeafMaterial &&
                                           Mathf.Approximately(fadedTreeMats[0].GetFloat("_Surface"), 1f) &&
                                           Mathf.Approximately(fadedTreeMats[1].GetFloat("_Surface"), 1f);
                        Assert(treeSwapped, "ObstructionFadeController swaps both Bark & Leaf submesh materials on BoundaryTree");

                        player.transform.position = treeCenter + new Vector3(0f, 0f, -2.0f);
                        Physics.SyncTransforms();
                        for (int step = 0; step < 5; step++)
                        {
                            fadeCtrl.UpdateFade(0.5f);
                        }
                        Assert(firstTreeMr.sharedMaterials[0] == worldGen.BarkMaterial &&
                               firstTreeMr.sharedMaterials[1] == worldGen.LeafMaterial,
                            "ObstructionFadeController restores both Bark & Leaf OriginalSharedMaterials on BoundaryTree");
                    }

                    // Verify ParticleSystemRenderer is never overridden when ExtractionHelicopter obstructs camera
                    if (heliAfterRetry != null && smokePsRend != null)
                    {
                        Material origSmokeMat = smokePsRend.sharedMaterial;
                        Vector3 heliCenter = heliAfterRetry.transform.position + Vector3.up * 1.5f;
                        cam.transform.position = heliCenter + new Vector3(0f, 2.0f, -6.0f);
                        player.transform.position = heliCenter + new Vector3(0f, 0f, 6.0f);
                        Physics.SyncTransforms();
                        fadeCtrl.UpdateFade(0.5f);
                        Assert(smokePsRend.sharedMaterial == origSmokeMat,
                            "ObstructionFadeController skips ParticleSystemRenderer on GreenSmokeCanister");
                    }

                    isoCam.SnapToTarget(player.transform);
                }

                // 24. Publish-Grade Polish Verification (URP Skybox/Volume, uGUI+InputSystem, MainMenu Isolation, Aim/Look-Ahead, Tap-Dodge, Quick-Belt, SafeZone 3/3+4s, Mud Overlap, Flashlight [T]/[L])
                Assert(File.Exists("Assets/Settings/DeadDepthsVolumeProfile.asset"),
                    "Dedicated DeadDepthsVolumeProfile.asset exists (DefaultVolumeProfile left intact)");
                var globalVol = UnityEngine.Object.FindAnyObjectByType<Volume>();
                Assert(globalVol != null && globalVol.isGlobal && globalVol.sharedProfile != null && globalVol.sharedProfile.name == "DeadDepthsVolumeProfile",
                    $"Global Volume configured with DeadDepthsVolumeProfile (actual={globalVol?.sharedProfile?.name})");

                var camUrpData = cam.GetComponent<UniversalAdditionalCameraData>();
                Assert(cam.clearFlags == CameraClearFlags.Skybox &&
                       camUrpData != null && camUrpData.renderPostProcessing &&
                       camUrpData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    "Main Camera configured with Skybox clearFlags, PostProcessing, and SMAA");
                Assert(RenderSettings.skybox != null && RenderSettings.fog,
                    "RenderSettings configured with NightSky skybox and atmospheric fog");

                var eventSys = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                Assert(eventSys != null &&
                       eventSys.GetComponent<InputSystemUIInputModule>() != null &&
                       eventSys.GetComponent<StandaloneInputModule>() == null,
                    "EventSystem uses InputSystemUIInputModule (no legacy StandaloneInputModule)");

                Assert(UnityEngine.Object.FindAnyObjectByType<DeadDepthsUIRoot>() != null,
                    "DeadDepthsUIRoot present on UIManager");

                // Player AimIK, Flashlight [T]/[L] (no Key.F conflict), and AnimSpeedMult on both Standing & Crouching
                var aimIk = player.GetComponentInChildren<PlayerAimIK>();
                var flashlight = player.GetComponent<DeadDepthsFlashlight>();
                Assert(aimIk != null && flashlight != null && player.GetComponent<DeadDepthsAimReticle>() != null,
                    "Player has PlayerAimIK on PlayerModel, DeadDepthsFlashlight, and DeadDepthsAimReticle");

                bool hasAnimSpeedMult = false;
                if (playerAnimator != null)
                {
                    foreach (var p in playerAnimator.parameters)
                    {
                        if (p.name == "AnimSpeedMult") { hasAnimSpeedMult = true; break; }
                    }
                }
                Assert(hasAnimSpeedMult, "Player.controller defines AnimSpeedMult parameter");

                bool prevFlash = flashlight.IsOn;
                flashlight.ToggleFlashlight();
                Assert(flashlight.IsOn != prevFlash, "DeadDepthsFlashlight toggles cleanly independently of Punch");
                flashlight.SetFlashlightState(prevFlash);

                // Main Menu Isolation & Timer Pause
                gm.EnterMainMenu();
                Assert(gm.IsInMainMenu && !gm.IsLevelActive, "GameManager.EnterMainMenu enters isolated Main Menu state");
                gm.StartLevel(1);
                Assert(!gm.IsInMainMenu && gm.IsLevelActive, "GameManager.StartLevel exits Main Menu cleanly for gameplay/tests");

                // Refresh player/camera references after StartLevel(1)
                player = GameObject.FindGameObjectWithTag("Player");
                playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                charController = player.GetComponent<CharacterController>();

                // RMB Aim + Camera Look-Ahead Deadzone & 3.2m Clamp + SnapToTarget reset
                playerCtrl.SetAimStateForTest(true, player.transform.position + Vector3.forward * 12f);
                isoCam.SetAimLookAheadEnabled(true, false);
                isoCam.SnapToTarget(player.transform);
                Assert(isoCam.CurrentLookAheadOffset.sqrMagnitude < 0.0001f,
                    "IsometricCameraController.SnapToTarget resets look-ahead offset to zero");
                isoCam.UpdateAimLookAhead(playerCtrl, new Vector2(0.05f, 0.05f), 0.2f);
                Assert(isoCam.CurrentLookAheadOffset.magnitude < 0.001f,
                    "Aim look-ahead ignores cursor inside 10% inner deadzone");
                for (int s = 0; s < 30; s++) isoCam.UpdateAimLookAhead(playerCtrl, new Vector2(0.48f, 0.48f), 0.1f);
                Assert(isoCam.CurrentLookAheadOffset.magnitude > 0.5f && isoCam.CurrentLookAheadOffset.magnitude <= 3.201f,
                    $"Aim look-ahead clamps within 3.2m max offset (actual={isoCam.CurrentLookAheadOffset.magnitude:F2}m)");
                isoCam.SnapToTarget(player.transform);
                playerCtrl.SetAimStateForTest(false, Vector3.zero);

                // Shift Tap-Dodge: cancels crate, uncrouches if clear, drains 22 stamina, grants i-frames, backsteps when stationary + Aim
                playerCtrl.ResetStateForLevel();
                var crateForDodge = GameObject.Find("SuppliesCrates")?.GetComponentInChildren<SuppliesCrate>();
                if (crateForDodge != null)
                {
                    player.transform.position = crateForDodge.transform.position;
                    crateForDodge.StartInteraction(player.transform);
                    Assert(crateForDodge.IsInteracting, "SuppliesCrate interaction active prior to Dodge");
                    playerCtrl.SetAimStateForTest(true, player.transform.position + Vector3.forward * 5f);
                    float stamBeforeDodge = playerCtrl.CurrentStamina;
                    bool dodged = playerCtrl.TryTriggerDodge();
                    Assert(dodged && playerCtrl.IsDodging && playerCtrl.IsInvulnerable &&
                           Mathf.Approximately(playerCtrl.CurrentStamina, stamBeforeDodge - playerCtrl.PlayerConfig.DodgeStaminaCost),
                        "TryTriggerDodge activates Ease-Out Dodge, i-frames, and consumes 22 Stamina");
                    float hpBeforeIframeHit = playerCtrl.CurrentHealth;
                    playerCtrl.TakeDamage(50f);
                    Assert(Mathf.Approximately(playerCtrl.CurrentHealth, hpBeforeIframeHit),
                        "Dodge i-frames block incoming damage completely");
                }
                playerCtrl.ResetStateForLevel();

                // Quick-Use Belt [1][2][3]
                gm.RetryRun();
                player = GameObject.FindGameObjectWithTag("Player");
                playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                playerCtrl.TakeDamage(50f);
                gm.AddBeltItem(BeltItemType.Bandage, 1);
                gm.AddBeltItem(BeltItemType.Medkit, 1);
                gm.AddBeltItem(BeltItemType.Adrenaline, 1);
                float hpBeforeBelt = playerCtrl.CurrentHealth;
                bool usedBandage = gm.TryUseBeltSlot(0);
                Assert(usedBandage && playerCtrl.CurrentHealth > hpBeforeBelt,
                    $"Quick-Belt [1] Bandage heals injured player ({hpBeforeBelt} -> {playerCtrl.CurrentHealth})");
                bool usedAdrenaline = gm.TryUseBeltSlot(2);
                Assert(usedAdrenaline && gm.IsAdrenalineActive,
                    "Quick-Belt [3] Adrenaline activates speed/stamina buff");

                // SafeZoneController: 3/3 Supply Intel + 4.0s hold + GameManager.OnPlayerEnteredSafeZone direct test trigger
                var safeZone = UnityEngine.Object.FindAnyObjectByType<SafeZoneController>();
                Assert(safeZone != null, "SafeZoneController attached to ExtractionHelicopter");
                if (safeZone != null)
                {
                    safeZone.SetPlayerInsideForTest(true);
                    safeZone.EvaluateExtractionHold(5.0f);
                    Assert(!safeZone.HasExtracted && gm.IsLevelActive,
                        $"SafeZoneController blocks extraction before 3/3 Supply Intel (have={gm.SupplyIntelCollected}/{gm.RequiredSupplyIntel})");

                    gm.GrantAllSupplyIntelForDemo();
                    safeZone.EvaluateExtractionHold(2.0f);
                    Assert(!safeZone.HasExtracted && safeZone.HoldProgress > 0.4f && safeZone.HoldProgress < 0.6f,
                        $"SafeZoneController tracks partial 4.0s extraction hold (progress={safeZone.HoldProgress:F2})");

                    safeZone.EvaluateExtractionHold(2.1f);
                    Assert(safeZone.HasExtracted && !gm.IsLevelActive,
                        "SafeZoneController completes extraction after 3/3 Intel + 4.0s hold");
                }

                // GameManager.OnPlayerEnteredSafeZone direct test trigger
                gm.RetryRun();
                Assert(gm.IsLevelActive, "RetryRun restores active level");
                gm.OnPlayerEnteredSafeZone();
                Assert(!gm.IsLevelActive, "GameManager.OnPlayerEnteredSafeZone direct test trigger completes level cleanly");
                gm.RetryRun();

                // MuddyPatch overlap tracking
                var mudPatches = GameObject.Find("MuddyPatches")?.GetComponentsInChildren<MuddyPatch>();
                player = GameObject.FindGameObjectWithTag("Player");
                playerCtrl = player.GetComponent<DeadDepthsPlayerController>();
                if (mudPatches != null && mudPatches.Length >= 2)
                {
                    mudPatches[0].RegisterActorEnter(playerCtrl);
                    mudPatches[1].RegisterActorEnter(playerCtrl);
                    Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 0.70f),
                        "MuddyPatch overlap applies 0.70x slow");
                    mudPatches[0].UnregisterActorExit(playerCtrl);
                    Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 0.70f),
                        "Exiting 1 of 2 overlapping MuddyPatches keeps 0.70x slow");
                    mudPatches[1].UnregisterActorExit(playerCtrl);
                    Assert(Mathf.Approximately(playerCtrl.TerrainMultiplier, 1.0f),
                        "Exiting final MuddyPatch restores 1.0x speed");
                }

                // World props & zombie spawn safety distance + bone accessories
                var heliCheck = GameObject.Find("ExtractionHelicopter");
                Assert(heliCheck != null &&
                       heliCheck.transform.Find("HelipadConcrete") != null &&
                       heliCheck.transform.Find("TailRotor") != null,
                    "ExtractionHelicopter includes HelipadConcrete and TailRotor child objects");

                var hordeCheck = GameObject.Find("ZombieHorde");
                bool accessoriesOk = true;
                if (hordeCheck != null)
                {
                    foreach (var z in hordeCheck.GetComponentsInChildren<ZombieController>())
                    {
                        if (z.Archetype == ZombieArchetype.Spitter && z.transform.GetComponentsInChildren<Transform>(true).Length < 3)
                            accessoriesOk = false;
                    }
                }
                Assert(accessoriesOk, "Zombie archetypes spawn cleanly with child accessories");

                // 25. Runtime uGUI Canvas, Raycast Isolation, Pause, VFX Pool, Audio 24-Voice Pool & Reticle Guard
                var uiRoot = UnityEngine.Object.FindAnyObjectByType<DeadDepthsUIRoot>();
                if (uiRoot != null)
                {
                    typeof(DeadDepthsUIRoot).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(uiRoot, null);
                    Assert(DeadDepthsUIRoot.IsCanvasActive && uiRoot.MasterCanvas != null,
                        "DeadDepthsUIRoot.Awake initializes 1920x1080 MasterCanvas and modular UI layers");

                    var scaler = uiRoot.MasterCanvas.GetComponent<CanvasScaler>();
                    Assert(scaler != null && scaler.referenceResolution == new Vector2(1920f, 1080f),
                        "MasterCanvas uses 1920x1080 reference resolution");

                    var hudComp = uiRoot.GameplayLayer.GetComponent<GameplayHUDCanvas>();
                    var menuComp = uiRoot.MainMenuLayer.GetComponent<MainMenuUI>();
                    var pauseComp = uiRoot.PauseLayer.GetComponent<PauseMenuUI>();
                    var settingsComp = uiRoot.SettingsLayer.GetComponent<SettingsModalUI>();
                    var zHpComp = uiRoot.WorldOverlayLayer.GetComponent<ZombieHealthBarUI>();
                    var dmgPopComp = uiRoot.WorldOverlayLayer.GetComponent<DamagePopupManager>();

                    Assert(hudComp != null && menuComp != null && pauseComp != null &&
                           settingsComp != null && zHpComp != null && dmgPopComp != null,
                        "All 6 uGUI controller modules attach cleanly under DeadDepthsUIRoot");

                    // In EditMode, AddComponent does not invoke Awake automatically, so invoke Awake on each UI controller
                    var awakeFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                    typeof(GameplayHUDCanvas).GetMethod("Awake", awakeFlags)?.Invoke(hudComp, null);
                    typeof(MainMenuUI).GetMethod("Awake", awakeFlags)?.Invoke(menuComp, null);
                    typeof(PauseMenuUI).GetMethod("Awake", awakeFlags)?.Invoke(pauseComp, null);
                    typeof(SettingsModalUI).GetMethod("Awake", awakeFlags)?.Invoke(settingsComp, null);
                    typeof(ZombieHealthBarUI).GetMethod("Awake", awakeFlags)?.Invoke(zHpComp, null);
                    typeof(DamagePopupManager).GetMethod("Awake", awakeFlags)?.Invoke(dmgPopComp, null);

                    // Audit passive GameplayHUDCanvas graphics: zero stray raycastTarget=true (only MinimapToggleBtn & DeathOverlay buttons should block)
                    int strayPassiveRaycasts = 0;
                    var hudRt = uiRoot.GameplayLayer.Find("GameplayHUDRoot");
                    Assert(hudRt != null, "GameplayHUDCanvas.Awake builds GameplayHUDRoot hierarchy");
                    if (hudRt != null)
                    {
                        foreach (var g in hudRt.GetComponentsInChildren<Graphic>(true))
                        {
                            if (!g.raycastTarget) continue;
                            if (g.GetComponent<Button>() != null) continue;
                            if (g.gameObject.name == "DeathOverlay" || g.gameObject.name == "DeathCard") continue;
                            strayPassiveRaycasts++;
                        }
                    }
                    Assert(hudRt != null && strayPassiveRaycasts == 0,
                        $"All passive GameplayHUD elements have raycastTarget = false (stray={strayPassiveRaycasts})");

                    // Verify VitalsCard status badges fit inside VitalsCard bounds (no right-edge overflow)
                    var vitalsRt = hudRt?.Find("VitalsCard") as RectTransform;
                    var adrBadgeRt = vitalsRt?.Find("Badge_ADR") as RectTransform;
                    Assert(vitalsRt != null && adrBadgeRt != null &&
                           (adrBadgeRt.anchoredPosition.x + adrBadgeRt.sizeDelta.x) <= vitalsRt.sizeDelta.x,
                        "VitalsCard status badges fit completely inside VitalsCard width");

                    // Verify CompletionScreenUI uGUI overlay Show/Hide
                    var compScreenUi = UnityEngine.Object.FindAnyObjectByType<CompletionScreenUI>();
                    Assert(compScreenUi != null, "CompletionScreenUI present on UIManager");
                    if (compScreenUi != null)
                    {
                        compScreenUi.Show(1);
                        var compScrim = uiRoot.CompletionLayer.Find("CompletionScrim");
                        Assert(compScreenUi.IsVisible && compScrim != null && compScrim.gameObject.activeSelf,
                            "CompletionScreenUI.Show activates Ranked Evacuation Scoreboard in uGUI");
                        compScreenUi.Hide();
                        Assert(!compScreenUi.IsVisible && compScrim != null && !compScrim.gameObject.activeSelf,
                            "CompletionScreenUI.Hide deactivates Evacuation Scoreboard cleanly");
                    }
                }

                // Pause isolation & ElapsedLevelTime freeze
                gm.RetryRun();
                gm.SetPaused(true);
                Assert(gm.IsPaused && Mathf.Approximately(Time.timeScale, 0f),
                    "GameManager.SetPaused(true) freezes Time.timeScale to 0");
                gm.SetPaused(false);
                Assert(!gm.IsPaused && Mathf.Approximately(Time.timeScale, 1f),
                    "GameManager.SetPaused(false) restores Time.timeScale to 1");

                // CombatVfxManager particle pool verification
                var vfxMgr = UnityEngine.Object.FindAnyObjectByType<CombatVfxManager>();
                Assert(vfxMgr != null, "CombatVfxManager present in scene");
                if (vfxMgr != null)
                {
                    typeof(CombatVfxManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(vfxMgr, null);
                    vfxMgr.PlayHitBlood(Vector3.zero, true);
                    vfxMgr.PlayDodgeDust(Vector3.zero);
                    vfxMgr.PlayWallShockwave(Vector3.zero);
                    vfxMgr.PlayAcidSplash(Vector3.zero);
                    vfxMgr.PlayHealBurst(Vector3.zero);
                    vfxMgr.PlayWoodSplinter(Vector3.zero, true);
                    Assert(vfxMgr.GetComponentsInChildren<ParticleSystem>(true).Length >= 16,
                        "CombatVfxManager initializes 16 pooled ParticleSystems and emits bursts cleanly");
                }

                // AudioManager 24-voice pool & AudioLowPassFilter verification
                var audioMgrCheck = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
                if (audioMgrCheck != null)
                {
                    typeof(AudioManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(audioMgrCheck, null);
                    int filterCount = audioMgrCheck.GetComponentsInChildren<AudioLowPassFilter>(true).Length;
                    Assert(filterCount >= 24,
                        $"AudioManager initializes 24-voice pooled AudioSources with AudioLowPassFilter (count={filterCount})");
                }

                // DeadDepthsAimReticle duplicate renderer guard
                player = GameObject.FindGameObjectWithTag("Player");
                var reticle = player?.GetComponent<DeadDepthsAimReticle>();
                if (reticle != null)
                {
                    var createMethod = typeof(DeadDepthsAimReticle).GetMethod("CreateRenderers", BindingFlags.Instance | BindingFlags.NonPublic);
                    createMethod?.Invoke(reticle, null);
                    createMethod?.Invoke(reticle, null);
                    int ringCount = 0;
                    foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "AimReticleRing") ringCount++;
                    }
                    Assert(ringCount == 1, $"DeadDepthsAimReticle creates exactly 1 AimReticleRing without duplicates (count={ringCount})");
                }

                // 26. Bug Fix & Optimization Regression Checks (MainMenu Hide on Deploy, Sector Zombie Scaling, Dynamic Initial Supply, Tree Canopy See-Through)
                var mainMenuCheck = MainMenuUI.Instance ?? UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();
                var pauseMenuCheck = PauseMenuUI.Instance ?? UnityEngine.Object.FindAnyObjectByType<PauseMenuUI>();
                if (mainMenuCheck != null)
                {
                    typeof(MainMenuUI).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(mainMenuCheck, null);
                    gm.EnterMainMenu();
                    Assert(gm.IsInMainMenu && mainMenuCheck.IsVisible, "MainMenuUI becomes visible when GameManager enters Main Menu");
                    gm.StartPlayFromMainMenu();
                    Assert(!gm.IsInMainMenu && gm.IsLevelActive && !mainMenuCheck.IsVisible,
                        "Deploy/Play (StartPlayFromMainMenu) hides MainMenuUI HUD cleanly");
                }
                if (pauseMenuCheck != null)
                {
                    typeof(PauseMenuUI).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(pauseMenuCheck, null);
                    gm.SetPaused(true);
                    Assert(gm.IsPaused && pauseMenuCheck.IsVisible, "PauseMenuUI becomes visible when paused");
                    gm.RetryRun();
                    Assert(!gm.IsPaused && !pauseMenuCheck.IsVisible, "RetryRun hides PauseMenuUI cleanly");
                }

                // Sector zombie scaling: Sector 1 < 5 zombies (4), +1 per next sector
                gm.RetryRun();
                int zLevel1 = worldGen.TotalZombiesSpawned;
                Assert(zLevel1 > 0 && zLevel1 < 5, $"Sector 1 spawns fewer than 5 zombies (actual={zLevel1})");
                gm.ProceedToNextLevel();
                int zLevel2 = worldGen.TotalZombiesSpawned;
                Assert(zLevel2 == zLevel1 + 1, $"Sector 2 increases zombie count by 1 ({zLevel1} -> {zLevel2})");
                gm.ProceedToNextLevel();
                int zLevel3 = worldGen.TotalZombiesSpawned;
                Assert(zLevel3 == zLevel2 + 1, $"Sector 3 increases zombie count by 1 ({zLevel2} -> {zLevel3})");

                // Dynamic initial supply scaling per sector passed & DDA tier
                gm.RetryRun();
                int s1Supply = gm.BandageCount + gm.MedkitCount + gm.AdrenalineCount;
                diff.UpdateProfile(DifficultyTier.Balanced);
                gm.ProceedToNextLevel(); // Sector 2
                int s2BalancedSupply = gm.BandageCount + gm.MedkitCount + gm.AdrenalineCount;
                Assert(s2BalancedSupply > s1Supply,
                    $"Advancing from Sector 1 to Sector 2 increases initial player supplies ({s1Supply} -> {s2BalancedSupply})");
                gm.ProceedToNextLevel(); // Sector 3
                int s3BalancedSupply = gm.BandageCount + gm.MedkitCount + gm.AdrenalineCount;
                Assert(s3BalancedSupply > s2BalancedSupply,
                    $"Advancing from Sector 2 to Sector 3 increases initial player supplies further ({s2BalancedSupply} -> {s3BalancedSupply})");

                gm.CalculateInitialSuppliesForSector(2, DifficultyTier.Struggling, out int strB, out int strM, out int strA);
                gm.CalculateInitialSuppliesForSector(2, DifficultyTier.PerformingWell, out int pwB, out int pwM, out int pwA);
                Assert((strB + strM + strA) > (pwB + pwM + pwA),
                    $"DDA grants more initial supplies when Struggling ({strB}/{strM}/{strA}) than PerformingWell ({pwB}/{pwM}/{pwA})");

                // Tree canopy see-through check: player offset 1.4m sideways behind pine canopy (outside 0.45m trunk collider)
                gm.RetryRun();
                player = GameObject.FindGameObjectWithTag("Player");
                var canopyFadeCtrl = cam != null ? cam.GetComponent<ObstructionFadeController>() : null;
                if (canopyFadeCtrl != null && player != null && worldGen.TreeRenderers.Count > 0)
                {
                    Renderer sampleTreeRend = worldGen.TreeRenderers[0];
                    Vector3 tPos = sampleTreeRend.transform.position;
                    // Place camera and player offset +1.35m on X so ray passes through pine canopy, missing 0.45m trunk capsule
                    cam.transform.position = tPos + new Vector3(1.35f, 4.5f, -5.0f);
                    player.transform.position = tPos + new Vector3(1.35f, 0f, 3.0f);
                    Physics.SyncTransforms();

                    canopyFadeCtrl.UpdateFade(0.5f);
                    Material[] canopyMats = sampleTreeRend.sharedMaterials;
                    bool canopyFaded = canopyMats.Length == 2 &&
                                       canopyMats[0] != worldGen.BarkMaterial &&
                                       canopyMats[1] != worldGen.LeafMaterial &&
                                       Mathf.Approximately(canopyMats[1].GetFloat("_Surface"), 1f) &&
                                       (!canopyMats[1].HasProperty("_BlendModePreserveSpecular") ||
                                        Mathf.Approximately(canopyMats[1].GetFloat("_BlendModePreserveSpecular"), 0f));
                    Assert(canopyFaded,
                        "ObstructionFadeController fades BoundaryTree when player is behind tree foliage canopy (offset from trunk)");

                    // Move player in front of tree and confirm clean material restore
                    player.transform.position = tPos + new Vector3(1.35f, 0f, -2.5f);
                    Physics.SyncTransforms();
                    for (int step = 0; step < 5; step++) canopyFadeCtrl.UpdateFade(0.5f);
                    Assert(sampleTreeRend.sharedMaterials[0] == worldGen.BarkMaterial &&
                           sampleTreeRend.sharedMaterials[1] == worldGen.LeafMaterial,
                        "ObstructionFadeController restores BoundaryTree materials after leaving canopy shadow");

                    isoCam.SnapToTarget(player.transform);
                }

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
