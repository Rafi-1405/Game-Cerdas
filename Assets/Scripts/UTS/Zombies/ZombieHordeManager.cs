using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Praktikum5.FSM;

namespace UTS.Zombies
{
    [DisallowMultipleComponent]
    public sealed class ZombieHordeManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private Transform objectiveZone;
        [SerializeField] private Transform extractionZone;

        [Header("Horde Spawning")]
        [Tooltip("Additional zombies. The placed Enemy can be the first zombie.")]
        [SerializeField, Min(0)] private int startingZombieCount = 34;
        [SerializeField, Min(0)] private int extractionWaveCount = 25;
        [SerializeField, Min(1f)] private float spawnRadius = 30f;
        [SerializeField, Min(0f)] private float minimumPlayerDistance = 14f;
        [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 8f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
        [SerializeField, Min(1)] private int spawnAttemptsPerZombie = 30;

        [Header("Objective Zones")]
        [SerializeField, Min(0.1f)] private float zoneRadius = 3f;
        [SerializeField, Min(0f)] private float alarmAlertDuration = 10f;
        [SerializeField] private bool restartOnR = true;

        private PlayerHealth playerHealth;
        private bool objectiveReached;
        private bool extractionReached;
        private bool gameOver;
        private bool warnedMissingPrefab;

        public bool IsGameOver => gameOver;
        public bool ObjectiveReached => objectiveReached;
        public bool ExtractionReached => extractionReached;
        public event Action OnObjectiveReached;
        public event Action OnExtractionReached;

        private void Start()
        {
            FindPlayerIfNeeded();
            if (player == null)
            {
                Debug.LogError("[ZombieHordeManager] Assign a Player or tag the player GameObject as Player.", this);
                return;
            }

            SpawnWave(startingZombieCount);
        }

        private void Update()
        {
            FindPlayerIfNeeded();
            if (player == null)
            {
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
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

            if (objectiveReached && !extractionReached && IsInsideZone(extractionZone))
            {
                extractionReached = true;
                gameOver = true;
                Debug.Log("[ZombieHordeManager] Player extracted successfully.", this);
                OnExtractionReached?.Invoke();
            }
        }

        private void FindPlayerIfNeeded()
        {
            if (player != null)
            {
                if (playerHealth == null)
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerHealth = playerObject.GetComponent<PlayerHealth>();
            }
        }

        private void ActivateObjective()
        {
            objectiveReached = true;
            Debug.Log("[ZombieHordeManager] Objective reached. Horde alarm activated.", this);
            SpawnWave(extractionWaveCount);

            foreach (ZombieSwarmAgent zombie in ZombieSwarmAgent.ActiveAgents)
            {
                if (zombie != null)
                {
                    zombie.ReceiveAlert(player.position, alarmAlertDuration);
                }
            }

            OnObjectiveReached?.Invoke();
        }

        private bool IsInsideZone(Transform zone)
        {
            if (zone == null)
            {
                return false;
            }

            Vector3 offset = player.position - zone.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= zoneRadius * zoneRadius;
        }

        private void SpawnWave(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (zombiePrefab == null)
            {
                if (!warnedMissingPrefab)
                {
                    Debug.LogError("[ZombieHordeManager] Assign a zombie prefab with a NavMeshAgent and character model.", this);
                    warnedMissingPrefab = true;
                }
                return;
            }

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                if (TrySpawnZombie())
                {
                    spawned++;
                }
            }

            if (spawned < count)
            {
                Debug.LogWarning($"[ZombieHordeManager] Spawned {spawned} of {count} zombies. Check the baked NavMesh and spawn radius.", this);
            }
        }

        private bool TrySpawnZombie()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : player.position;
            NavMeshAgent prefabAgent = zombiePrefab.GetComponent<NavMeshAgent>();
            float baseOffset = prefabAgent != null ? prefabAgent.baseOffset : 0f;

            for (int attempt = 0; attempt < spawnAttemptsPerZombie; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
                Vector3 candidate = new Vector3(center.x + offset.x, center.y, center.z + offset.y);
                if (PlanarDistance(candidate, player.position) < minimumPlayerDistance)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, navMeshAreaMask))
                {
                    continue;
                }

                if (PlanarDistance(hit.position, player.position) < minimumPlayerDistance)
                {
                    continue;
                }

                Vector3 spawnPosition = hit.position + Vector3.up * baseOffset;
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
                return true;
            }

            return false;
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
