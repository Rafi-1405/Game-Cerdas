using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
