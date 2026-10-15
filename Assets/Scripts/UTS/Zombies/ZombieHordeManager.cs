using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Praktikum5.FSM;

namespace UTS.Zombies
{
    [System.Serializable]
    public class TownSpawnZone
    {
        public string zoneName;
        public Vector3 center;
        public Vector3 extents; // Setengah ukuran kotak (X, Y, Z)
        public int zombieCount;

        public TownSpawnZone(string name, Vector3 center, Vector3 extents, int count)
        {
            this.zoneName = name;
            this.center = center;
            this.extents = extents;
            this.zombieCount = count;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ZombieHordeManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private Transform objectiveZone;
        [SerializeField] private Transform extractionZone;

        [Header("Zone-Based Spawning (Project Zomboid Town)")]
        [SerializeField] private bool useZoneBasedSpawning = true;
        [SerializeField] private float minZombieSpacing = 3.8f; // Aturan Poisson spacing >= 3.5m - 4.0m
        [SerializeField] private float playerSafeSpawnRadius = 22f; // Buffer aman zona safehouse player (>= 22m)
        [SerializeField] private float spawnPacingDelay = 0.05f; // Paced spawning via coroutine (0.05s per zombie)
        [SerializeField] private List<TownSpawnZone> spawnZones = new List<TownSpawnZone>();

        [Header("Fallback Spawning")]
        [SerializeField, Min(0)] private int startingZombieCount = 32;
        [SerializeField, Min(0)] private int extractionWaveCount = 20;
        [SerializeField, Min(1f)] private float spawnRadius = 30f;
        [SerializeField, Min(0f)] private float minimumPlayerDistance = 22f;
        [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 6f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
        [SerializeField, Min(1)] private int spawnAttemptsPerZombie = 50;

        [Header("Objective Zones")]
        [SerializeField, Min(0.1f)] private float zoneRadius = 3f;
        [SerializeField, Min(0f)] private float alarmAlertDuration = 10f;
        [SerializeField, Min(0.1f)] private float alarmHuntDuration = 12f;
        [SerializeField] private bool restartOnR = true;

        private SimplePlayerController playerController;
        private readonly List<Vector3> spawnedPositions = new List<Vector3>();
        private bool objectiveReached;
        private bool extractionReached;
        private bool gameOver;
        private bool warnedMissingPrefab;

        public bool IsGameOver => gameOver;
        public bool ObjectiveReached => objectiveReached;
        public bool ExtractionReached => extractionReached;
        public float PlayerSafeSpawnRadius => playerSafeSpawnRadius;
        public IReadOnlyList<TownSpawnZone> SpawnZones => spawnZones;
        public event Action OnObjectiveReached;
        public event Action OnExtractionReached;

        private void OnValidate()
        {
            EnsureDefaultSpawnZones();
        }

        private void Awake()
        {
            EnsureDefaultSpawnZones();
            EnsureZombiePrefab();
        }

        private void Start()
        {
            FindPlayerIfNeeded();
            if (player == null)
            {
                Debug.LogError("[ZombieHordeManager] Assign a Player or tag the player GameObject as Player.", this);
                return;
            }

            EnsureZombiePrefab();
            StartCoroutine(SpawnInitialHordeRoutine());
        }

        private void Update()
        {
            FindPlayerIfNeeded();
            if (player == null) return;

            if (playerController != null && playerController.IsDead)
            {
                gameOver = true;
            }

            if (gameOver)
            {
                if (restartOnR && IsRestartPressed())
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                return;
            }

            if (!objectiveReached && IsInsideZone(objectiveZone))
            {
                ActivateObjective();
            }

            if (UTS.Environment.GreenSmokeExtractionZone.Instance == null)
            {
                if (objectiveReached && !extractionReached && IsInsideZone(extractionZone))
                {
                    TriggerExtractionSuccess();
                }
            }
        }

        public void TriggerExtractionSuccess()
        {
            if (extractionReached) return;
            extractionReached = true;
            gameOver = true;
            Debug.Log("[ZombieHordeManager] Player extracted successfully.", this);
            OnExtractionReached?.Invoke();
        }

        private void FindPlayerIfNeeded()
        {
            if (player != null)
            {
                if (playerController == null)
                {
                    playerController = player.GetComponent<SimplePlayerController>();
                }
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerController = playerObject.GetComponent<SimplePlayerController>();
            }
        }

        private void EnsureZombiePrefab()
        {
            if (zombiePrefab != null) return;

#if UNITY_EDITOR
            zombiePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/UTS/Enemy.prefab");
#endif
            if (zombiePrefab == null)
            {
                ZombieSwarmAgent sceneAgent = FindAnyObjectByType<ZombieSwarmAgent>();
                if (sceneAgent != null)
                {
                    zombiePrefab = sceneAgent.gameObject;
                }
            }
        }

        public void EnsureDefaultSpawnZones(bool forceReset = false)
        {
            if (!forceReset && spawnZones != null && spawnZones.Count == 7) return;

            spawnZones = new List<TownSpawnZone>
            {
                // 1. Far West Yards (House 4 & 5): 4 zombies (Center -48, -6, Extents 6, 12 -> seluruh batas kotak >= 24m dari safehouse)
                new TownSpawnZone("Far West Yards (House 4 & 5)", new Vector3(-48f, 0f, -6f), new Vector3(6f, 1f, 12f), 4),

                // 2. West Alleys / Cottage (House 3): 4 zombies
                new TownSpawnZone("West Alleys / Cottage (House 3)", new Vector3(-20f, 0f, -22f), new Vector3(6f, 1f, 5f), 4),

                // 3. Duplex & Center Alleys (House 6): 4 zombies
                new TownSpawnZone("Duplex & Center Alleys (House 6)", new Vector3(0f, 0f, -20f), new Vector3(6f, 1f, 6f), 4),

                // 4. Spiffo Mart Supermarket & Parking: 7 zombies
                new TownSpawnZone("Spiffo Mart Supermarket & Parking", new Vector3(18f, 0f, -14f), new Vector3(8f, 1f, 6f), 7),

                // 5. PharmaCare Medical Clinic: 7 zombies
                new TownSpawnZone("PharmaCare Medical Clinic", new Vector3(45f, 0f, 16f), new Vector3(8f, 1f, 6f), 7),

                // 6. East Outskirts / Extraction Approach: 4 zombies
                new TownSpawnZone("East Outskirts / Extraction Approach", new Vector3(38f, 0f, 24f), new Vector3(7f, 1f, 5f), 4),

                // 7. Main Street Perimeter (Outer ends): 2 zombies
                new TownSpawnZone("Main Street Perimeter", new Vector3(55f, 0f, 0f), new Vector3(6f, 1f, 3.5f), 2)
            };
        }

        private IEnumerator SpawnInitialHordeRoutine()
        {
            EnsureZombiePrefab();

            if (zombiePrefab == null)
            {
                if (!warnedMissingPrefab)
                {
                    Debug.LogError("[ZombieHordeManager] Assign a zombie prefab with a NavMeshAgent and character model.", this);
                    warnedMissingPrefab = true;
                }
                yield break;
            }

            spawnedPositions.Clear();
            int totalSpawned = 0;

            if (useZoneBasedSpawning)
            {
                foreach (TownSpawnZone zone in spawnZones)
                {
                    int zoneSpawned = 0;
                    for (int i = 0; i < zone.zombieCount; i++)
                    {
                        if (TrySpawnZombieInZone(zone, minZombieSpacing))
                        {
                            zoneSpawned++;
                            totalSpawned++;
                            yield return new WaitForSeconds(spawnPacingDelay);
                        }
                        else if (TrySpawnZombieInZone(zone, minZombieSpacing * 0.7f)) // Relaksasi jarak jika padat
                        {
                            zoneSpawned++;
                            totalSpawned++;
                            yield return new WaitForSeconds(spawnPacingDelay);
                        }
                        else if (TrySpawnZombieFallback()) // Fallback jika titik zona terhalang obstacle interior
                        {
                            zoneSpawned++;
                            totalSpawned++;
                            yield return new WaitForSeconds(spawnPacingDelay);
                        }
                    }

                    if (zoneSpawned < zone.zombieCount)
                    {
                        Debug.LogWarning($"[ZombieHordeManager] Zona {zone.zoneName} spawn {zoneSpawned}/{zone.zombieCount} zombie.", this);
                    }
                }
            }
            else
            {
                for (int i = 0; i < startingZombieCount; i++)
                {
                    if (TrySpawnZombieFallback())
                    {
                        totalSpawned++;
                        yield return new WaitForSeconds(spawnPacingDelay);
                    }
                }
            }

            Debug.Log($"[ZombieHordeManager] Paced spawning selesai! Total {totalSpawned} zombie Project Zomboid berhasil disebar di kota.", this);
        }

        private bool TrySpawnZombieInZone(TownSpawnZone zone, float requiredSpacing)
        {
            NavMeshAgent prefabAgent = zombiePrefab.GetComponent<NavMeshAgent>();
            float baseOffset = prefabAgent != null ? prefabAgent.baseOffset : 0f;
            float effectiveSafeDist = Mathf.Max(minimumPlayerDistance, playerSafeSpawnRadius);
            Vector3 safehouseCenter = new Vector3(-20f, 0f, 16f);

            for (int attempt = 0; attempt < spawnAttemptsPerZombie; attempt++)
            {
                float randX = UnityEngine.Random.Range(-zone.extents.x, zone.extents.x);
                float randZ = UnityEngine.Random.Range(-zone.extents.z, zone.extents.z);
                Vector3 candidate = zone.center + new Vector3(randX, 0f, randZ);

                // Cek jarak aman dari pemain dan safehouse
                if (player != null && PlanarDistance(candidate, player.position) < effectiveSafeDist)
                {
                    continue;
                }
                if (PlanarDistance(candidate, safehouseCenter) < effectiveSafeDist)
                {
                    continue;
                }

                // Sample posisi di NavMesh
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, navMeshAreaMask))
                {
                    continue;
                }

                if (player != null && PlanarDistance(hit.position, player.position) < effectiveSafeDist)
                {
                    continue;
                }
                if (PlanarDistance(hit.position, safehouseCenter) < effectiveSafeDist)
                {
                    continue;
                }

                // Pengecekan Poisson Minimum Spacing Rule (>= 3.5m - 4.0m antar zombie)
                bool validSpacing = true;
                foreach (Vector3 spawnedPos in spawnedPositions)
                {
                    if (PlanarDistance(hit.position, spawnedPos) < requiredSpacing)
                    {
                        validSpacing = false;
                        break;
                    }
                }

                if (!validSpacing)
                {
                    continue;
                }

                // Sukses menemukan koordinat valid!
                Vector3 spawnPosition = hit.position + Vector3.up * baseOffset;
                SpawnZombieInstance(spawnPosition);
                spawnedPositions.Add(hit.position);
                return true;
            }

            return false;
        }

        private bool TrySpawnZombieFallback()
        {
            Vector3 center = (spawnCenter != null && spawnCenter != player) ? spawnCenter.position : Vector3.zero;
            float effectiveRadius = (spawnCenter == null || center == Vector3.zero) ? Mathf.Max(spawnRadius, 55f) : spawnRadius;
            NavMeshAgent prefabAgent = zombiePrefab.GetComponent<NavMeshAgent>();
            float baseOffset = prefabAgent != null ? prefabAgent.baseOffset : 0f;
            float effectiveSafeDist = Mathf.Max(minimumPlayerDistance, playerSafeSpawnRadius);
            Vector3 safehouseCenter = new Vector3(-20f, 0f, 16f);

            for (int attempt = 0; attempt < spawnAttemptsPerZombie; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * effectiveRadius;
                Vector3 candidate = new Vector3(center.x + offset.x, center.y, center.z + offset.y);

                if (player != null && PlanarDistance(candidate, player.position) < effectiveSafeDist)
                {
                    continue;
                }
                if (PlanarDistance(candidate, safehouseCenter) < effectiveSafeDist)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, navMeshAreaMask))
                {
                    continue;
                }

                if (player != null && PlanarDistance(hit.position, player.position) < effectiveSafeDist)
                {
                    continue;
                }
                if (PlanarDistance(hit.position, safehouseCenter) < effectiveSafeDist)
                {
                    continue;
                }

                bool validSpacing = true;
                foreach (Vector3 spawnedPos in spawnedPositions)
                {
                    if (PlanarDistance(hit.position, spawnedPos) < minZombieSpacing)
                    {
                        validSpacing = false;
                        break;
                    }
                }

                if (!validSpacing) continue;

                Vector3 spawnPosition = hit.position + Vector3.up * baseOffset;
                SpawnZombieInstance(spawnPosition);
                spawnedPositions.Add(hit.position);
                return true;
            }

            return false;
        }

        private void SpawnZombieInstance(Vector3 spawnPosition)
        {
            GameObject zombieObject = Instantiate(
                zombiePrefab,
                spawnPosition,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
            );

            ZombieSwarmAgent swarmAgent = zombieObject.GetComponent<ZombieSwarmAgent>();
            if (swarmAgent == null)
            {
                swarmAgent = zombieObject.AddComponent<ZombieSwarmAgent>();
            }

            swarmAgent.Initialize(player, this);

            if (objectiveReached)
            {
                swarmAgent.ReceiveHuntOrder(Mathf.Max(alarmAlertDuration, alarmHuntDuration));
            }
        }

        private void ActivateObjective()
        {
            objectiveReached = true;
            Debug.Log("[ZombieHordeManager] Objective reached. Horde alarm activated.", this);

            StartCoroutine(SpawnExtractionWaveRoutine());

            foreach (ZombieSwarmAgent zombie in ZombieSwarmAgent.ActiveAgents)
            {
                if (zombie != null)
                {
                    zombie.ReceiveHuntOrder(Mathf.Max(alarmAlertDuration, alarmHuntDuration));
                }
            }

            OnObjectiveReached?.Invoke();
        }

        private IEnumerator SpawnExtractionWaveRoutine()
        {
            int spawned = 0;
            for (int i = 0; i < extractionWaveCount; i++)
            {
                if (TrySpawnZombieFallback())
                {
                    spawned++;
                    yield return new WaitForSeconds(spawnPacingDelay);
                }
            }
            Debug.Log($"[ZombieHordeManager] Gelombang ekstraksi: {spawned} zombie spawned.", this);
        }

        private bool IsInsideZone(Transform zone)
        {
            if (zone == null || player == null) return false;

            Vector3 offset = player.position - zone.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= zoneRadius * zoneRadius;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private bool IsRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private void OnGUI()
        {
            if (UTS.Environment.GreenSmokeExtractionZone.Instance != null)
            {
                if (playerController != null && playerController.IsDead)
                {
                    GUIStyle deadStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 20,
                        fontStyle = FontStyle.Bold
                    };
                    deadStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f, 1f);
                    GUI.Label(new Rect((Screen.width - 420f) * 0.5f, Screen.height * 0.42f, 420f, 50f), "YOU DIED\nTekan [R] untuk Restart", deadStyle);
                }
                return;
            }

            string status = gameOver
                ? extractionReached ? "EXTRACTED! Press R to restart" : "YOU DIED - Press R to restart"
                : objectiveReached ? "ALARM! Horde incoming - reach extraction" : "Reach the objective";

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect((Screen.width - 520f) * 0.5f, 12f, 520f, 34f), status, style);
        }

        private void OnDrawGizmosSelected()
        {
            if (useZoneBasedSpawning && spawnZones != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
                foreach (TownSpawnZone zone in spawnZones)
                {
                    Gizmos.DrawWireCube(zone.center + Vector3.up * 0.5f, zone.extents * 2f);
                }
            }

            if (spawnCenter != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius);
            }

            if (objectiveZone != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(objectiveZone.position, zoneRadius);
            }

            if (extractionZone != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(extractionZone.position, zoneRadius);
            }
        }
    }
}
