using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum9.PCG
{
    /// <summary>
    /// Generator Konten Prosedural (PCG) Cerdas untuk Enemy dan Item.
    /// Mengimplementasikan controlled randomness, seeded pseudo-random generation,
    /// weighted random distribution, dan generate-and-test spatial validation.
    /// </summary>
    public class PCGSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnEntry
        {
            public string name;
            public GameObject prefab;

            [Min(0)]
            public int weight = 1;
        }

        [Header("Seed Settings")]
        [Tooltip("Gunakan seed acak baru setiap kali regenerate jika true; gunakan fixed seed jika false.")]
        [SerializeField] private bool useRandomSeed = false;

        [Tooltip("Nilai seed untuk generator pseudo-random. Seed yang sama menjamin layout yang identik.")]
        [SerializeField] private int seed = 12345;

        [Header("References")]
        [Tooltip("Transform Player untuk perhitungan safe spawn distance (Rule 1).")]
        [SerializeField] private Transform player;

        [Tooltip("Parent transform untuk menampung seluruh objek hasil spawn agar hierarki tetap rapi.")]
        [SerializeField] private Transform generatedRoot;

        [Header("Spawn Area")]
        [Tooltip("Ukuran area X dan Z untuk penempatan kandidat spawn.")]
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(24f, 24f);

        [Tooltip("Ketinggian Y posisi spawn dari permukaan lantai.")]
        [SerializeField] private float spawnY = 0.5f;

        [Header("Spawn Counts")]
        [Tooltip("Target jumlah enemy yang akan di-spawn.")]
        [SerializeField] private int enemyCount = 12;

        [Tooltip("Target jumlah item collectible yang akan di-spawn.")]
        [SerializeField] private int itemCount = 8;

        [Header("Validation Rules (Generate-and-Test)")]
        [Tooltip("Rule 1: Jarak minimal titik spawn dari posisi player.")]
        [SerializeField] private float minDistanceFromPlayer = 5f;

        [Tooltip("Rule 2: Radius virtual sphere untuk mendeteksi rintangan via Physics.CheckSphere.")]
        [SerializeField] private float checkRadius = 0.75f;

        [Tooltip("Rule 3: Jarak minimal antar objek prosedural agar tidak saling bertumpuk.")]
        [SerializeField] private float minimumSpacing = 1.5f;

        [Tooltip("LayerMask untuk menyaring objek rintangan/obstacle (Layer 7).")]
        [SerializeField] private LayerMask obstacleMask;

        [Tooltip("Rule 4: Batas maksimal percobaan pencarian posisi valid sebelum dianggap gagal.")]
        [SerializeField] private int maxAttemptsPerObject = 50;

        [Header("Enemy Weighted Table")]
        [Tooltip("Daftar prefab enemy dan bobot probabilitas kemunculannya.")]
        [SerializeField] private SpawnEntry[] enemyEntries;

        [Header("Item Weighted Table")]
        [Tooltip("Daftar prefab item dan bobot probabilitas kemunculannya.")]
        [SerializeField] private SpawnEntry[] itemEntries;

        [Header("Runtime Debug & Diagnostics")]
        [SerializeField] private int currentSeed;
        [SerializeField] private int successfulEnemySpawns;
        [SerializeField] private int successfulItemSpawns;
        [SerializeField] private int rejectedPositions;

        // Modifikasi 2: Spawn Statistics Tracking
        private readonly Dictionary<string, int> spawnStatistics = new Dictionary<string, int>();
        private readonly List<Vector3> usedPositions = new List<Vector3>();

        #region Unity Callbacks

        private void Start()
        {
            Generate();
        }

        private void Update()
        {
            bool triggerRegeneration = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                triggerRegeneration = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.R))
            {
                triggerRegeneration = true;
            }
#endif

            if (triggerRegeneration)
            {
                Generate();
            }
        }

        #endregion

        #region Core PCG Pipeline

        /// <summary>
        /// Mengeksekusi pipeline procedural content generation secara penuh:
        /// Clear -> Initialize Seed -> Spawn Enemies -> Spawn Items -> Diagnostics.
        /// </summary>
        public void Generate()
        {
            ClearGeneratedObjects();
            usedPositions.Clear();
            spawnStatistics.Clear();

            successfulEnemySpawns = 0;
            successfulItemSpawns = 0;
            rejectedPositions = 0;

            // Inisialisasi Seed
            if (useRandomSeed)
            {
                seed = System.Environment.TickCount;
            }

            currentSeed = seed;
            Random.InitState(currentSeed);

            // Generate Enemy Group
            SpawnGroup(enemyEntries, enemyCount, true);

            // Generate Item Group
            SpawnGroup(itemEntries, itemCount, false);

            Debug.Log(
                $"[PCGSpawner] Generation Complete | Seed: {currentSeed} | " +
                $"Enemies: {successfulEnemySpawns}/{enemyCount} | " +
                $"Items: {successfulItemSpawns}/{itemCount} | " +
                $"Rejected Positions: {rejectedPositions}"
            );
        }

        /// <summary>
        /// Algoritma Generate-and-Test untuk menempatkan sekelompok objek secara prosedural.
        /// </summary>
        private void SpawnGroup(SpawnEntry[] entries, int count, bool enemyGroup)
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogWarning("[PCGSpawner] Tabel spawn kosong, proses dibatalkan.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                bool spawned = false;

                for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
                {
                    Vector3 candidate = GenerateRandomPosition();

                    // Generate-and-Test validation filter
                    if (!IsPositionValid(candidate))
                    {
                        rejectedPositions++;
                        continue;
                    }

                    // Weighted Random Selection
                    SpawnEntry selectedEntry = SelectWeightedSpawnEntry(entries);
                    if (selectedEntry == null || selectedEntry.prefab == null)
                    {
                        Debug.LogWarning("[PCGSpawner] Tidak ada prefab valid pada weighted table.");
                        return;
                    }

                    // Modifikasi 1: Random Rotation pada sumbu Y (0 - 360 derajat)
                    Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    Transform parent = (generatedRoot != null) ? generatedRoot : transform;
                    GameObject spawnedObject = Instantiate(
                        selectedEntry.prefab,
                        candidate,
                        randomRotation,
                        parent
                    );

                    spawnedObject.name = $"{selectedEntry.prefab.name}_Generated_{i}";
                    usedPositions.Add(candidate);

                    // Modifikasi 2: Catat statistik kuota kemunculan prefab
                    string entryName = string.IsNullOrEmpty(selectedEntry.name) ? selectedEntry.prefab.name : selectedEntry.name;
                    if (!spawnStatistics.ContainsKey(entryName))
                    {
                        spawnStatistics[entryName] = 0;
                    }
                    spawnStatistics[entryName]++;

                    if (enemyGroup)
                    {
                        successfulEnemySpawns++;
                    }
                    else
                    {
                        successfulItemSpawns++;
                    }

                    spawned = true;
                    break;
                }

                if (!spawned)
                {
                    Debug.LogWarning(
                        $"[PCGSpawner] Gagal spawn object {(enemyGroup ? "Enemy" : "Item")} ke-{i}. " +
                        $"Tidak menemukan posisi valid setelah {maxAttemptsPerObject} percobaan."
                    );
                }
            }
        }

        /// <summary>
        /// Menghasilkan koordinat posisi acak di dalam batas spawnAreaSize.
        /// </summary>
        private Vector3 GenerateRandomPosition()
        {
            float halfWidth = spawnAreaSize.x * 0.5f;
            float halfDepth = spawnAreaSize.y * 0.5f;

            float randomX = Random.Range(-halfWidth, halfWidth);
            float randomZ = Random.Range(-halfDepth, halfDepth);

            return transform.position + new Vector3(randomX, spawnY, randomZ);
        }

        /// <summary>
        /// Evaluasi Generate-and-Test terhadap 3 Aturan Spasial:
        /// Rule 1: Safe distance from player
        /// Rule 2: Obstacle avoidance via Physics.CheckSphere
        /// Rule 3: Anti-clumping minimum spacing
        /// </summary>
        private bool IsPositionValid(Vector3 candidate)
        {
            // Rule 1: Cek jarak aman dari Player pada bidang datar XZ
            if (player != null)
            {
                Vector3 playerFlat = new Vector3(player.position.x, candidate.y, player.position.z);
                float distanceToPlayer = Vector3.Distance(candidate, playerFlat);

                if (distanceToPlayer < minDistanceFromPlayer)
                {
                    return false;
                }
            }

            // Rule 2: Cek overlap dengan rintangan/obstacle menggunakan Physics.CheckSphere
            bool blocked = Physics.CheckSphere(
                candidate,
                checkRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

            if (blocked)
            {
                return false;
            }

            // Rule 3: Cek jarak minimal terhadap objek prosedural yang telah di-spawn sebelumnya
            for (int i = 0; i < usedPositions.Count; i++)
            {
                float distance = Vector3.Distance(candidate, usedPositions[i]);
                if (distance < minimumSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Memilih satu SpawnEntry dari tabel menggunakan algoritma Weighted Random (Roulette Wheel Selection).
        /// </summary>
        private SpawnEntry SelectWeightedSpawnEntry(SpawnEntry[] entries)
        {
            int totalWeight = 0;
            foreach (SpawnEntry entry in entries)
            {
                if (entry != null && entry.prefab != null && entry.weight > 0)
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            foreach (SpawnEntry entry in entries)
            {
                if (entry == null || entry.prefab == null || entry.weight <= 0)
                {
                    continue;
                }

                cumulativeWeight += entry.weight;
                if (randomValue < cumulativeWeight)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Menghapus seluruh objek prosedural terdahulu dari generatedRoot.
        /// Kompatibel dengan Runtime dan EditMode.
        /// </summary>
        private void ClearGeneratedObjects()
        {
            if (generatedRoot == null)
            {
                return;
            }

            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = generatedRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        #endregion

        #region Debug GUI & Gizmos

        private void OnGUI()
        {
            // Panel Utama Debug & Kontrol
            int panelWidth = 330;
            int panelHeight = 220 + (spawnStatistics.Count * 18);

            GUI.Box(new Rect(10, 10, panelWidth, panelHeight), "<b>PCG Smart Spawner — Realtime HUD</b>");

            int yOffset = 35;
            GUI.Label(new Rect(20, yOffset, 300, 20), $"<b>Current Seed:</b> {currentSeed} ({(useRandomSeed ? "Random Mode" : "Fixed Mode")})");
            yOffset += 22;
            GUI.Label(new Rect(20, yOffset, 300, 20), $"<b>Enemy Spawned:</b> {successfulEnemySpawns}/{enemyCount}");
            yOffset += 22;
            GUI.Label(new Rect(20, yOffset, 300, 20), $"<b>Item Spawned:</b> {successfulItemSpawns}/{itemCount}");
            yOffset += 22;
            GUI.Label(new Rect(20, yOffset, 300, 20), $"<b>Rejected Candidates:</b> <color=orange>{rejectedPositions}</color>");
            yOffset += 24;

            // Modifikasi 2: Statistik Distribusi Kuota Riil
            GUI.Label(new Rect(20, yOffset, 300, 20), "<b>--- Realtime Spawn Breakdown ---</b>");
            yOffset += 20;

            foreach (var kvp in spawnStatistics)
            {
                GUI.Label(new Rect(25, yOffset, 290, 18), $"• {kvp.Key}: {kvp.Value}");
                yOffset += 18;
            }

            yOffset += 8;

            // Modifikasi 3: Tombol Regenerate UI
            if (GUI.Button(new Rect(20, yOffset, 170, 32), "<b>Regenerate (UI)</b>"))
            {
                Generate();
            }

            GUI.Label(new Rect(200, yOffset + 6, 120, 25), "<i>Shortcut: [R]</i>");
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisasi Spawn Area
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position + Vector3.up * spawnY;
            Vector3 size = new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y);
            Gizmos.DrawWireCube(center, size);

            // Visualisasi Player Safe Zone
            if (player != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
            }

            // Visualisasi Spacing Antar Objek Terdaftar
            if (usedPositions != null && usedPositions.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
                foreach (Vector3 pos in usedPositions)
                {
                    Gizmos.DrawWireSphere(pos, minimumSpacing * 0.5f);
                }
            }
        }

        #endregion

        #region Public Setters for Automation/Testing

        public void Configure(
            Transform playerRef,
            Transform rootRef,
            LayerMask obstacleLayer,
            SpawnEntry[] enemies,
            SpawnEntry[] items,
            int targetSeed = 12345)
        {
            player = playerRef;
            generatedRoot = rootRef;
            obstacleMask = obstacleLayer;
            enemyEntries = enemies;
            itemEntries = items;
            seed = targetSeed;
            useRandomSeed = false;
        }

        #endregion
    }
}
