using System.Collections.Generic;
using UnityEngine;

namespace UTS.Inventory
{
    [DisallowMultipleComponent]
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private int bandageCount = 5;
        [SerializeField] private int firstAidCount = 3;
        [SerializeField] private int medkitCount = 2;

        [Header("Spawn Area")]
        [SerializeField] private Vector3 areaCenter = Vector3.zero;
        [SerializeField] private Vector2 areaSize = new Vector2(100f, 45f);
        [SerializeField] private float itemElevation = 0.5f;

        [Header("Fixed Building Spawn Points (Maximal 1 Item per Building)")]
        [Tooltip("Daftar titik spawn di dalam bangunan. Jika kosong, sistem otomatis memakai 28 titik interior bangunan default kota.")]
        [SerializeField] private Transform[] fixedSpawnPoints;

        [Header("Textured pickup prefabs (Bandage, First Aid Kit, Medkit)")]
        [SerializeField] private ScavengeItemPickup[] itemPrefabs;

        // Koordinat interior akurat untuk 28 bangunan kota (22 Rumah Warga + 6 Fasilitas/Toko)
        public static readonly (string name, Vector3 pos)[] DefaultBuildingSpawnPoints = new (string, Vector3)[]
        {
            // --- Kompleks Perumahan Timur (Willow Street) ---
            ("Willow_15_GrayModern", new Vector3(63.84f, 0.55f, 58.24f)),
            ("Willow_14_SageFarmhouse", new Vector3(63.84f, 0.55f, 38.92f)),
            ("Willow_13_NavyCabin", new Vector3(63.84f, 0.55f, 19.60f)),
            ("Willow_16_RedBrickBungalow", new Vector3(87.36f, 0.55f, 19.04f)),
            ("Willow_17_OchreGable", new Vector3(100.58f, 0.95f, 47.32f)),
            ("Willow_18_ContemporaryStucco", new Vector3(93.52f, 0.65f, 52.85f)),

            // --- Kompleks Perumahan Barat (Ash Street Utara) ---
            ("Ash_01_CedarCottage", new Vector3(-71.96f, 0.55f, 19.32f)),
            ("Ash_02_OchreGable", new Vector3(-71.96f, 0.55f, 38.36f)),
            ("Ash_03_WhiteRanch", new Vector3(-82.74f, 1.25f, 49.00f)),
            ("Ash_04_SageFarmhouse", new Vector3(-48.11f, 1.15f, 26.04f)),
            ("Ash_05_ContemporaryStucco", new Vector3(-44.14f, 0.95f, 46.34f)),
            ("Ash_06_RedBrickBungalow", new Vector3(-48.44f, 0.55f, 57.40f)),

            // --- Kompleks Perumahan Barat (Ash Street Selatan) ---
            ("Ash_07_GrayModern", new Vector3(-71.96f, 0.55f, -19.32f)),
            ("Ash_08_RedBrickBungalow", new Vector3(-85.40f, 1.15f, -39.66f)),
            ("Ash_09_NavyCabin", new Vector3(-72.24f, 0.95f, -66.92f)),
            ("Ash_10_ContemporaryStucco", new Vector3(-48.44f, 0.55f, -20.44f)),
            ("Ash_11_CedarCottage", new Vector3(-48.44f, 0.55f, -40.04f)),
            ("Ash_12_WhiteRanch", new Vector3(-48.44f, 0.55f, -59.36f)),

            // --- Kompleks Perumahan Tengah (Civic Area Selatan) ---
            ("Civic_19_CedarCottage", new Vector3(-7.00f, 0.55f, -46.20f)),
            ("Civic_20_NavyCabin", new Vector3(-21.84f, 1.15f, -65.28f)),
            ("Civic_21_SageFarmhouse", new Vector3(10.64f, 0.95f, -48.33f)),
            ("Civic_22_WhiteRanch", new Vector3(18.20f, 0.55f, -65.52f)),

            // --- Fasilitas Publik & Pertokoan ---
            ("Minimarket_SpiffoMart", new Vector3(-24.08f, 2.25f, -18.69f)),
            ("Clinic_PharmaCare", new Vector3(22.68f, 2.60f, -19.69f)),
            ("FastFood_Restaurant", new Vector3(-7.28f, 2.50f, -20.44f)),
            ("Service_Station", new Vector3(49.92f, 2.65f, -15.49f)),
            ("Town_Hall", new Vector3(-18.06f, 3.00f, 29.48f)),
            ("Fortress_Camp", new Vector3(5.60f, 0.55f, 68.60f))
        };

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAllItems();
            }
        }

        [ContextMenu("Spawn Items Now")]
        public void SpawnAllItems()
        {
            // Hapus item lama yang mungkin masih ada di bawah transform ini (jika dipanggil ulang)
            CleanExistingSpawnedItems();

            // Kumpulkan semua titik interior bangunan yang tersedia
            List<Vector3> buildingPositions = GetAvailableBuildingPositions();

            if (buildingPositions == null || buildingPositions.Count == 0)
            {
                Debug.LogWarning("[ItemSpawner] Tidak ada titik bangunan yang ditemukan! Menggunakan fallback area.", this);
                SpawnFallbackItems();
                return;
            }

            // Acak urutan bangunan menggunakan Fisher-Yates Shuffle
            ShuffleList(buildingPositions);

            int currentBuildingIndex = 0;

            // 1. Prioritas Utama: Medkit (Loot paling berharga / langka)
            for (int i = 0; i < medkitCount && currentBuildingIndex < buildingPositions.Count; i++)
            {
                SpawnItem(ScavengeItemType.Medkit, buildingPositions[currentBuildingIndex++]);
            }

            // 2. Prioritas Kedua: First Aid Kit (P3K)
            for (int i = 0; i < firstAidCount && currentBuildingIndex < buildingPositions.Count; i++)
            {
                SpawnItem(ScavengeItemType.FirstAidKit, buildingPositions[currentBuildingIndex++]);
            }

            // 3. Prioritas Ketiga: Bandage (Perban standar)
            for (int i = 0; i < bandageCount && currentBuildingIndex < buildingPositions.Count; i++)
            {
                SpawnItem(ScavengeItemType.Bandage, buildingPositions[currentBuildingIndex++]);
            }

            Debug.Log($"[ItemSpawner] Berhasil memunculkan {currentBuildingIndex} item di {currentBuildingIndex} bangunan unik (aturan: maksimal 1 item per bangunan).", this);
        }

        public GameObject SpawnItem(ScavengeItemType type, Vector3 position)
        {
            int index = (int)type;
            if (itemPrefabs != null && index >= 0 && index < itemPrefabs.Length && itemPrefabs[index] != null)
            {
                var model = Instantiate(itemPrefabs[index], position, Quaternion.identity, transform);
                model.name = "Item_" + ScavengeItemInfo.GetName(type);
                model.SetItemType(type);
                return model.gameObject;
            }

            GameObject itemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObj.name = $"Item_{ScavengeItemInfo.GetName(type)}";
            itemObj.transform.position = position;
            itemObj.transform.SetParent(transform);

            BoxCollider col = itemObj.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size = Vector3.one * 1.5f;
            }

            ScavengeItemPickup pickup = itemObj.AddComponent<ScavengeItemPickup>();
            pickup.SetItemType(type);

            return itemObj;
        }

        private List<Vector3> GetAvailableBuildingPositions()
        {
            List<Vector3> list = new List<Vector3>();

            if (fixedSpawnPoints != null && fixedSpawnPoints.Length > 0)
            {
                for (int i = 0; i < fixedSpawnPoints.Length; i++)
                {
                    if (fixedSpawnPoints[i] != null)
                    {
                        list.Add(fixedSpawnPoints[i].position);
                    }
                }
            }

            // Jika fixedSpawnPoints belum dipasang di Inspector, gunakan seluruh 28 titik interior default
            if (list.Count == 0)
            {
                for (int i = 0; i < DefaultBuildingSpawnPoints.Length; i++)
                {
                    list.Add(DefaultBuildingSpawnPoints[i].pos);
                }
            }

            return list;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[rnd];
                list[rnd] = temp;
            }
        }

        private void CleanExistingSpawnedItems()
        {
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Item_") || child.GetComponent<ScavengeItemPickup>() != null)
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(toDestroy[i]);
                }
                else
                {
                    DestroyImmediate(toDestroy[i]);
                }
            }
        }

        private void SpawnFallbackItems()
        {
            for (int i = 0; i < bandageCount; i++)
            {
                SpawnItem(ScavengeItemType.Bandage, GetRandomAreaPosition());
            }
            for (int i = 0; i < firstAidCount; i++)
            {
                SpawnItem(ScavengeItemType.FirstAidKit, GetRandomAreaPosition());
            }
            for (int i = 0; i < medkitCount; i++)
            {
                SpawnItem(ScavengeItemType.Medkit, GetRandomAreaPosition());
            }
        }

        private Vector3 GetRandomAreaPosition()
        {
            float x = areaCenter.x + Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = areaCenter.z + Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
            return new Vector3(x, areaCenter.y + itemElevation, z);
        }

        private void OnDrawGizmosSelected()
        {
            var points = GetAvailableBuildingPositions();
            if (points != null)
            {
                Gizmos.color = new Color(0.2f, 0.85f, 0.4f, 0.7f);
                for (int i = 0; i < points.Count; i++)
                {
                    Gizmos.DrawWireSphere(points[i], 0.6f);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Create Building Spawn Points In Scene")]
        public void CreateBuildingSpawnPointsInScene()
        {
            Transform existingHolder = transform.Find("BuildingSpawnPoints");
            if (existingHolder != null)
            {
                DestroyImmediate(existingHolder.gameObject);
            }

            GameObject holder = new GameObject("BuildingSpawnPoints");
            holder.transform.SetParent(transform, false);

            Transform[] pts = new Transform[DefaultBuildingSpawnPoints.Length];
            for (int i = 0; i < DefaultBuildingSpawnPoints.Length; i++)
            {
                GameObject ptObj = new GameObject("SpawnPoint_" + DefaultBuildingSpawnPoints[i].name);
                ptObj.transform.SetParent(holder.transform, false);
                ptObj.transform.position = DefaultBuildingSpawnPoints[i].pos;
                pts[i] = ptObj.transform;
            }

            fixedSpawnPoints = pts;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemSpawner] Berhasil membuat {pts.Length} titik spawn interior bangunan di scene.", this);
        }
#endif
    }
}
