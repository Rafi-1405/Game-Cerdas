gine:** Unity 6  
**Bahasa Pemrograman:** C#  

---

# 1. Identitas Praktikum

## Nama Praktikum

**Procedural Content Generation — Smart Procedural Spawning**

## Nama Project Unity

\`\`\`text
GameCerdas_P09_PCG_SmartSpawner
\`\`\`

## Nama Scene

\`\`\`text
PCG_SpawnArena
\`\`\`

## Topik Utama

Praktikum ini mempelajari:

- Procedural Content Generation,
- randomness,
- controlled randomness,
- seed,
- reproducibility,
- procedural spawning,
- prefab,
- weighted random,
- probability,
- spawn area,
- spawn validation,
- Layer dan LayerMask,
- Physics.CheckSphere,
- minimum spawn distance,
- maximum generation attempts,
- parameter tuning,
- debug visualization.

---

# 2. Posisi Praktikum dalam Mata Kuliah

Pada praktikum sebelumnya mahasiswa lebih banyak membangun **AI untuk agent atau NPC**:

\`\`\`text
Praktikum 1 : Perception
Praktikum 2 : Sensor + Memory + Decision
Praktikum 3 : Movement AI & Steering
Praktikum 4 : Pathfinding & Navigation
Praktikum 5 : Finite State Machine
Praktikum 6 : Behavior Tree & Utility AI
Praktikum 7 : Tactical AI Integration
Praktikum 8 : UTS / Mini Project
\`\`\`

Pada Praktikum 9 fokus mulai bergeser dari:

\`\`\`text
AI yang mengendalikan agent
\`\`\`

menjadi:

\`\`\`text
algoritma yang menghasilkan content game
\`\`\`

Inilah konsep dasar:

\`\`\`text
Procedural Content Generation
\`\`\`

Praktikum ini sengaja **tidak langsung membuat procedural dungeon yang kompleks**, karena teknik:

- random walk,
- BSP,
- cellular automata,
- dungeon generation,
- constraint-based level generation,

lebih tepat diperdalam pada Pertemuan 10.

---

# 3. Tujuan Praktikum

Setelah menyelesaikan praktikum ini, mahasiswa diharapkan mampu:

1. Menjelaskan konsep Procedural Content Generation.
2. Membedakan random biasa dan controlled randomness.
3. Menggunakan \`Random.Range()\`.
4. Menggunakan seed dengan \`Random.InitState()\`.
5. Menjelaskan reproducibility.
6. Membuat spawn area procedural.
7. Membuat sistem procedural spawning.
8. Menggunakan prefab sebagai template content.
9. Menggunakan Layer dan LayerMask untuk filtering physics.
10. Menggunakan \`Physics.CheckSphere()\` untuk mengecek posisi spawn.
11. Menghindari spawn terlalu dekat dengan player.
12. Menghindari spawn di dalam obstacle.
13. Menghindari object procedural saling bertumpuk.
14. Mengimplementasikan weighted random.
15. Menggunakan \`maxAttempts\` sebagai pengaman algoritma.
16. Membuat parameter PCG dapat diatur melalui Inspector.
17. Membandingkan hasil generation menggunakan seed berbeda.
18. Melakukan debugging terhadap sistem procedural.

---

# 4. Gambaran Hasil Akhir

Setelah praktikum selesai, scene akan memiliki struktur seperti berikut:

\`\`\`text
PCG_SpawnArena
│
├── Main Camera
├── Directional Light
│
├── Environment
│   ├── Ground
│   ├── Obstacle_01
│   ├── Obstacle_02
│   ├── Obstacle_03
│   └── Obstacle_04
│
├── Player
│
├── PCGManager
│
└── GeneratedObjects
    ├── Enemy...
    └── Item...
\`\`\`

Ketika game dijalankan:

\`\`\`text
PCGManager
     ↓
Initialize Seed
     ↓
Generate Enemy
     ↓
Generate Item
     ↓
Random Position
     ↓
Validation
     ├── terlalu dekat player? → gagal
     ├── mengenai obstacle? → gagal
     ├── terlalu dekat object lain? → gagal
     └── valid → Instantiate
\`\`\`

---

# 5. Konsep Dasar PCG

Procedural Content Generation adalah proses menghasilkan content game menggunakan:

\`\`\`text
Algorithm
+
Parameter
+
Randomness
+
Rules
+
Constraints
\`\`\`

Contoh content yang dapat dihasilkan secara procedural:

- enemy,
- item,
- collectible,
- obstacle,
- vegetation,
- dungeon,
- terrain,
- quest,
- loot,
- level,
- decoration.

Pada praktikum ini content yang dihasilkan adalah:

\`\`\`text
Enemy
dan
Item
\`\`\`

---

# 6. PCG Bukan Random Murni

Misalnya kita membuat posisi:

\`\`\`csharp
float x = Random.Range(-10f, 10f);
float z = Random.Range(-10f, 10f);
\`\`\`

Secara teknis kita sudah mendapatkan posisi random.

Tetapi posisi tersebut mungkin:

- berada di dalam tembok,
- terlalu dekat player,
- berada di atas object lain,
- berada di luar area yang diinginkan.

Maka:

\`\`\`text
Random Position
\`\`\`

belum cukup.

PCG yang lebih baik menggunakan:

\`\`\`text
Random Position
       +
Validation
       +
Constraint
\`\`\`

---

# 7. Controlled Randomness

Pada praktikum ini kita menerapkan beberapa aturan.

## Aturan 1

Enemy tidak boleh muncul terlalu dekat dengan player.

\`\`\`text
distance(spawn, player)
>=
minimumPlayerDistance
\`\`\`

---

## Aturan 2

Enemy atau item tidak boleh berada di dalam obstacle.

Digunakan:

\`\`\`csharp
Physics.CheckSphere()
\`\`\`

---

## Aturan 3

Object procedural tidak boleh terlalu dekat dengan object procedural sebelumnya.

Digunakan:

\`\`\`text
minimumSpacing
\`\`\`

---

## Aturan 4

Pencarian posisi tidak boleh dilakukan tanpa batas.

Digunakan:

\`\`\`text
maxAttemptsPerObject
\`\`\`

---

# 8. Seed dan Reproducibility

Seed adalah nilai awal untuk generator pseudo-random.

Contoh:

\`\`\`text
Seed = 12345
\`\`\`

Jika:

- seed sama,
- urutan pemanggilan random sama,
- parameter sama,

maka hasil random dapat direproduksi.

Contoh:

\`\`\`text
Seed 100
→ Layout A

Seed 200
→ Layout B

Seed 100
→ Layout A kembali
\`\`\`

Hal ini sangat berguna untuk:

- debugging,
- testing,
- reproduksi bug,
- challenge seed,
- sharing procedural configuration.

Unity 6 menyediakan \`Random.InitState(int seed)\` untuk menginisialisasi state generator pseudo-random. Dokumentasi resmi Unity juga menjelaskan bahwa seed tertentu dapat digunakan untuk menghasilkan kembali pola pseudo-random yang sama.

---

# 9. Random.Range()

Unity menyediakan:

\`\`\`csharp
Random.Range(min, max);
\`\`\`

Contoh:

\`\`\`csharp
float x = Random.Range(-10f, 10f);
\`\`\`

dan:

\`\`\`csharp
int index = Random.Range(0, 5);
\`\`\`

Catatan penting:

### Integer

\`\`\`csharp
Random.Range(0, 5)
\`\`\`

menghasilkan:

\`\`\`text
0
1
2
3
4
\`\`\`

### Float

\`\`\`csharp
Random.Range(0f, 5f)
\`\`\`

menghasilkan nilai floating point pada rentang yang diberikan. Perbedaan perilaku batas integer dan float ini didokumentasikan pada Unity Scripting API.

---

# 10. Istilah Teknis Unity yang Digunakan

Sebelum implementasi, mahasiswa perlu memahami beberapa istilah berikut.

---

## 10.1 GameObject

\`GameObject\` adalah object dasar dalam sebuah Unity Scene.

Contoh:

\`\`\`text
Player
Enemy
Camera
Light
Ground
Obstacle
\`\`\`

GameObject menjadi container bagi berbagai Component.

---

## 10.2 Component

Component memberikan kemampuan kepada GameObject.

Contoh:

\`\`\`text
Transform
Mesh Renderer
Collider
Rigidbody
Character Controller
Script
\`\`\`

Contoh:

\`\`\`text
Enemy
├── Transform
├── Mesh Renderer
├── Capsule Collider
└── EnemyAI.cs
\`\`\`

---

## 10.3 Transform

Semua GameObject memiliki \`Transform\`.

Transform menyimpan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

Contoh:

\`\`\`csharp
transform.position
\`\`\`

---

## 10.4 Prefab

Prefab adalah template GameObject yang dapat digunakan kembali.

Contoh:

\`\`\`text
Enemy_Red.prefab
Enemy_Blue.prefab
HealthPotion.prefab
Coin.prefab
\`\`\`

PCG sangat sering menggunakan prefab karena algoritma dapat membuat object menggunakan:

\`\`\`csharp
Instantiate(prefab);
\`\`\`

\`Instantiate\` membuat salinan object/prefab pada runtime.

---

## 10.5 Collider

Collider menentukan area fisik object.

Contoh:

\`\`\`text
Box Collider
Sphere Collider
Capsule Collider
Mesh Collider
\`\`\`

Collider diperlukan agar Physics System mengetahui keberadaan object.

---

## 10.6 Layer

Layer adalah kategori GameObject.

Contoh:

\`\`\`text
Default
Player
Obstacle
Enemy
Item
\`\`\`

Layer terutama berguna untuk:

- collision filtering,
- raycast filtering,
- physics query filtering,
- camera culling.

Pada praktikum ini kita menggunakan layer:

\`\`\`text
Obstacle
\`\`\`

---

## 10.7 LayerMask

\`LayerMask\` digunakan script untuk memilih layer mana yang akan diperiksa.

Contoh:

\`\`\`csharp
[SerializeField]
private LayerMask obstacleMask;
\`\`\`

Di Inspector kita dapat menentukan bahwa:

\`\`\`text
obstacleMask = Obstacle
\`\`\`

---

## 10.8 SerializeField

Field:

\`\`\`csharp
[SerializeField]
private float spawnRadius;
\`\`\`

tetap \`private\`, tetapi dapat ditampilkan di Inspector.

Ini sangat berguna pada PCG karena parameter harus mudah di-tuning oleh designer.

---

## 10.9 Inspector

Inspector adalah panel Unity Editor untuk melihat dan mengubah:

- Transform,
- Component,
- Script parameter,
- Collider,
- Material,
- Layer,
- Tag.

PCG sebaiknya memiliki parameter generation pada Inspector agar mahasiswa dapat melakukan eksperimen tanpa mengubah source code.

---

## 10.10 Physics.CheckSphere

Digunakan untuk mengetahui apakah sebuah sphere virtual bertabrakan/overlap dengan Collider tertentu.

Contoh:

\`\`\`csharp
bool blocked = Physics.CheckSphere(
    position,
    checkRadius,
    obstacleMask
);
\`\`\`

Jika:

\`\`\`text
blocked = true
\`\`\`

berarti posisi tersebut tidak boleh digunakan.

Unity mendefinisikan \`Physics.CheckSphere\` sebagai physics query untuk memeriksa apakah ada collider yang overlap dengan sphere tertentu.

---

## 10.11 Instantiate

Digunakan untuk membuat instance GameObject saat game berjalan.

\`\`\`csharp
Instantiate(
    enemyPrefab,
    spawnPosition,
    Quaternion.identity
);
\`\`\`

---

## 10.12 Quaternion.identity

\`\`\`csharp
Quaternion.identity
\`\`\`

berarti object dibuat tanpa rotasi tambahan.

Secara sederhana:

\`\`\`text
rotation = default
\`\`\`

---

## 10.13 Gizmos

Gizmos adalah visual debugging di Scene View.

Dapat digunakan untuk menggambar:

- spawn area,
- detection radius,
- safe zone,
- waypoint,
- collider approximation.

Gizmos membantu developer memahami algoritma tanpa menjadi bagian visual final game.

---

# 11. Algoritma yang Akan Dibuat

Pseudocode utama:

\`\`\`text
SET seed

FOR setiap enemy:
    attempt = 0

    WHILE attempt < maxAttempts:
        generate random position

        IF terlalu dekat player:
            reject

        ELSE IF mengenai obstacle:
            reject

        ELSE IF terlalu dekat object sebelumnya:
            reject

        ELSE:
            pilih enemy dengan weighted random
            spawn enemy
            simpan posisi
            selesai

        attempt++
\`\`\`

Kemudian algoritma yang sama digunakan untuk item.

---

# 12. Persiapan Project Unity

Buka Unity Hub.

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D Core
\`\`\`

Untuk praktikum ini **Universal 3D direkomendasikan** jika ingin hasil visual lebih menarik.

Nama project:

\`\`\`text
GameCerdas_P09_PCG_SmartSpawner
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 13. Membuat Struktur Folder

Pada panel Project:

\`\`\`text
Assets
\`\`\`

buat folder:

\`\`\`text
Assets
│
├── Materials
├── Prefabs
│   ├── Enemies
│   └── Items
│
├── Scenes
├── Scripts
│
└── Settings
\`\`\`

Tujuan:

- asset lebih rapi,
- prefab mudah ditemukan,
- script tidak bercampur,
- project mudah dikembangkan.

---

# 14. Menyimpan Scene

Simpan scene melalui:

\`\`\`text
File
→ Save As
\`\`\`

Nama:

\`\`\`text
PCG_SpawnArena
\`\`\`

Lokasi:

\`\`\`text
Assets/Scenes/
\`\`\`

---

# 15. Membuat Arena

## Langkah 1 — Ground

Pilih:

\`\`\`text
Hierarchy
→ Create
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
Ground
\`\`\`

Atur:

\`\`\`text
Position
X = 0
Y = 0
Z = 0

Scale
X = 3
Y = 1
Z = 3
\`\`\`

Plane default memiliki ukuran cukup besar setelah scale tersebut untuk arena praktikum.

---

# 16. Membuat Material Ground

Di:

\`\`\`text
Assets/Materials
\`\`\`

buat:

\`\`\`text
M_Ground
\`\`\`

Berikan warna sesuai keinginan.

Drag material ke:

\`\`\`text
Ground
\`\`\`

---

# 17. Membuat Player

Pilih:

\`\`\`text
Hierarchy
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Set posisi:

\`\`\`text
Position
X = 0
Y = 1
Z = 0
\`\`\`

---

# 18. Membuat Layer Player

Pilih GameObject:

\`\`\`text
Player
\`\`\`

Pada Inspector:

\`\`\`text
Layer
→ Add Layer
\`\`\`

Tambahkan:

\`\`\`text
Player
Obstacle
\`\`\`

Kembali pilih Player.

Set:

\`\`\`text
Layer = Player
\`\`\`

---

# 19. Membuat Player Material

Buat:

\`\`\`text
M_Player
\`\`\`

Berikan warna yang mudah dibedakan dari enemy.

Assign ke Player.

---

# 20. Membuat Player Controller

Agar praktikum dapat dijalankan secara mandiri, kita membuat controller sederhana.

Tambahkan:

\`\`\`text
Character Controller
\`\`\`

pada Player.

Kemudian buat script:

\`\`\`text
SimplePlayerController.cs
\`\`\`

di:

\`\`\`text
Assets/Scripts/
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical).normalized;

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );
    }
}
\`\`\`

---

# 21. Fungsi Player Controller

Bagian:

\`\`\`csharp
Input.GetAxisRaw("Horizontal")
\`\`\`

membaca input horizontal.

Umumnya:

\`\`\`text
A / Left Arrow  → -1
D / Right Arrow → 1
\`\`\`

Sedangkan:

\`\`\`csharp
Input.GetAxisRaw("Vertical")
\`\`\`

umumnya:

\`\`\`text
S / Down Arrow → -1
W / Up Arrow   → 1
\`\`\`

---

## Catatan Input System

Jika project Unity menggunakan konfigurasi Input System baru dan script legacy Input Manager tidak aktif, atur Input Handling project sesuai konfigurasi praktikum atau gunakan controller dari praktikum sebelumnya.

Tujuan controller pada modul ini hanya agar Player dapat dipindah untuk mengamati safe spawn radius.

---

# 22. Membuat Obstacle

Buat:

\`\`\`text
Hierarchy
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
Obstacle_01
\`\`\`

Contoh posisi:

\`\`\`text
X = 5
Y = 1
Z = 5
\`\`\`

Scale:

\`\`\`text
X = 3
Y = 2
Z = 1
\`\`\`

---

# 23. Layer Obstacle

Set:

\`\`\`text
Layer = Obstacle
\`\`\`

Pastikan Cube memiliki:

\`\`\`text
Box Collider
\`\`\`

Tanpa Collider:

\`\`\`text
Physics.CheckSphere
\`\`\`

tidak dapat mendeteksi obstacle tersebut sebagai collider.

---

# 24. Duplikasi Obstacle

Buat beberapa obstacle.

Contoh:

\`\`\`text
Obstacle_01 → ( 5, 1,  5)
Obstacle_02 → (-6, 1,  4)
Obstacle_03 → ( 5, 1, -6)
Obstacle_04 → (-4, 1, -5)
\`\`\`

Silakan ubah ukuran masing-masing obstacle.

Tujuannya membuat arena memiliki area yang tidak boleh digunakan sebagai spawn point.

---

# 25. Struktur Environment

Buat Empty GameObject:

\`\`\`text
Environment
\`\`\`

Masukkan:

\`\`\`text
Ground
Obstacle_01
Obstacle_02
Obstacle_03
Obstacle_04
\`\`\`

menjadi child.

Hierarchy:

\`\`\`text
Environment
├── Ground
├── Obstacle_01
├── Obstacle_02
├── Obstacle_03
└── Obstacle_04
\`\`\`

---

# 26. Membuat Enemy Prefab Pertama

Buat:

\`\`\`text
3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Enemy_Red
\`\`\`

Atur ukuran misalnya:

\`\`\`text
Scale
X = 0.8
Y = 0.8
Z = 0.8
\`\`\`

Tambahkan material:

\`\`\`text
M_Enemy_Red
\`\`\`

---

# 27. Membuat Prefab

Drag:

\`\`\`text
Enemy_Red
\`\`\`

dari Hierarchy menuju:

\`\`\`text
Assets/Prefabs/Enemies/
\`\`\`

Sekarang GameObject tersebut menjadi prefab.

Hapus Enemy_Red dari scene.

---

# 28. Membuat Enemy Lain

Duplicate prefab atau buat object baru:

\`\`\`text
Enemy_Blue
Enemy_Yellow
\`\`\`

Gunakan material berbeda agar hasil weighted random mudah diamati.

Hasil:

\`\`\`text
Prefabs/Enemies
├── Enemy_Red.prefab
├── Enemy_Blue.prefab
└── Enemy_Yellow.prefab
\`\`\`

---

# 29. Membuat Item Prefab

Buat beberapa object sederhana.

Contoh:

\`\`\`text
Item_Coin
Item_Health
Item_Rare
\`\`\`

Bentuk dapat menggunakan:

\`\`\`text
Sphere
Cube
Cylinder
\`\`\`

Simpan sebagai prefab di:

\`\`\`text
Assets/Prefabs/Items/
\`\`\`

---

# 30. Konsep Weighted Random

Kita tidak ingin semua prefab memiliki kemungkinan sama.

Misalnya:

\`\`\`text
Enemy             Weight

Enemy_Red          60
Enemy_Blue         30
Enemy_Yellow       10
\`\`\`

Artinya secara statistik:

\`\`\`text
Red lebih sering muncul
Blue lebih jarang
Yellow paling jarang
\`\`\`

Ini disebut:

\`\`\`text
Weighted Random
\`\`\`

---

# 31. Membuat GeneratedObjects

Pada Hierarchy:

\`\`\`text
Create Empty
\`\`\`

Rename:

\`\`\`text
GeneratedObjects
\`\`\`

Tujuan:

semua object procedural menjadi child object ini.

Keuntungan:

- Hierarchy lebih rapi,
- object procedural mudah dihapus,
- mudah melakukan regenerate.

---

# 32. Membuat PCGManager

Buat Empty GameObject:

\`\`\`text
PCGManager
\`\`\`

Reset Transform:

\`\`\`text
Position = (0,0,0)
Rotation = (0,0,0)
Scale    = (1,1,1)
\`\`\`

---

# 33. Membuat Script Utama

Buat:

\`\`\`text
Assets/Scripts/PCGSpawner.cs
\`\`\`

Script ini menjadi inti praktikum.

---

# 34. Script PCGSpawner Lengkap

\`\`\`csharp
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Seed")]
    [SerializeField]
    private bool useRandomSeed = false;

    [SerializeField]
    private int seed = 12345;


    [Header("References")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private Transform generatedRoot;


    [Header("Spawn Area")]
    [SerializeField]
    private Vector2 spawnAreaSize =
        new Vector2(24f, 24f);

    [SerializeField]
    private float spawnY = 0.5f;


    [Header("Spawn Counts")]
    [SerializeField]
    private int enemyCount = 12;

    [SerializeField]
    private int itemCount = 8;


    [Header("Validation")]
    [SerializeField]
    private float minDistanceFromPlayer = 5f;

    [SerializeField]
    private float checkRadius = 0.75f;

    [SerializeField]
    private float minimumSpacing = 1.5f;

    [SerializeField]
    private LayerMask obstacleMask;

    [SerializeField]
    private int maxAttemptsPerObject = 50;


    [Header("Enemy Weighted Table")]
    [SerializeField]
    private SpawnEntry[] enemyEntries;


    [Header("Item Weighted Table")]
    [SerializeField]
    private SpawnEntry[] itemEntries;


    [Header("Runtime Debug")]
    [SerializeField]
    private int currentSeed;

    [SerializeField]
    private int successfulEnemySpawns;

    [SerializeField]
    private int successfulItemSpawns;

    [SerializeField]
    private int rejectedPositions;


    private readonly List<Vector3> usedPositions =
        new List<Vector3>();


    private void Start()
    {
        Generate();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Generate();
        }
    }


    public void Generate()
    {
        ClearGeneratedObjects();

        usedPositions.Clear();

        successfulEnemySpawns = 0;
        successfulItemSpawns = 0;
        rejectedPositions = 0;


        if (useRandomSeed)
        {
            seed = System.Environment.TickCount;
        }

        currentSeed = seed;

        Random.InitState(currentSeed);


        SpawnGroup(
            enemyEntries,
            enemyCount,
            true
        );

        SpawnGroup(
            itemEntries,
            itemCount,
            false
        );


        Debug.Log(
            $"PCG Generation Complete | " +
            $"Seed: {currentSeed} | " +
            $"Enemies: {successfulEnemySpawns}/{enemyCount} | " +
            $"Items: {successfulItemSpawns}/{itemCount} | " +
            $"Rejected Positions: {rejectedPositions}"
        );
    }


    private void SpawnGroup(
        SpawnEntry[] entries,
        int count,
        bool enemyGroup)
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning(
                "Spawn table kosong."
            );

            return;
        }


        for (int i = 0; i < count; i++)
        {
            bool spawned = false;


            for (
                int attempt = 0;
                attempt < maxAttemptsPerObject;
                attempt++)
            {
                Vector3 candidate =
                    GenerateRandomPosition();


                if (!IsPositionValid(candidate))
                {
                    rejectedPositions++;
                    continue;
                }


                GameObject selectedPrefab =
                    SelectWeightedPrefab(entries);


                if (selectedPrefab == null)
                {
                    Debug.LogWarning(
                        "Tidak ada prefab valid pada weighted table."
                    );

                    return;
                }


                GameObject spawnedObject =
                    Instantiate(
                        selectedPrefab,
                        candidate,
                        Quaternion.identity,
                        generatedRoot
                    );


                spawnedObject.name =
                    $"{selectedPrefab.name}_Generated_{i}";


                usedPositions.Add(candidate);


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
                    $"Gagal spawn object ke-{i}. " +
                    $"Tidak menemukan posisi valid setelah " +
                    $"{maxAttemptsPerObject} percobaan."
                );
            }
        }
    }


    private Vector3 GenerateRandomPosition()
    {
        float halfWidth =
            spawnAreaSize.x * 0.5f;

        float halfDepth =
            spawnAreaSize.y * 0.5f;


        float randomX =
            Random.Range(-halfWidth, halfWidth);

        float randomZ =
            Random.Range(-halfDepth, halfDepth);


        return transform.position +
            new Vector3(
                randomX,
                spawnY,
                randomZ
            );
    }


    private bool IsPositionValid(
        Vector3 candidate)
    {
        // Rule 1:
        // Jangan spawn terlalu dekat dengan player.
        if (player != null)
        {
            Vector3 playerFlat =
                new Vector3(
                    player.position.x,
                    candidate.y,
                    player.position.z
                );


            float distanceToPlayer =
                Vector3.Distance(
                    candidate,
                    playerFlat
                );


            if (distanceToPlayer <
                minDistanceFromPlayer)
            {
                return false;
            }
        }


        // Rule 2:
        // Jangan spawn di dalam obstacle.
        bool blocked =
            Physics.CheckSphere(
                candidate,
                checkRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );


        if (blocked)
        {
            return false;
        }


        // Rule 3:
        // Jangan terlalu dekat dengan
        // procedural object sebelumnya.
        for (
            int i = 0;
            i < usedPositions.Count;
            i++)
        {
            float distance =
                Vector3.Distance(
                    candidate,
                    usedPositions[i]
                );


            if (distance < minimumSpacing)
            {
                return false;
            }
        }


        return true;
    }


    private GameObject SelectWeightedPrefab(
        SpawnEntry[] entries)
    {
        int totalWeight = 0;


        foreach (SpawnEntry entry in entries)
        {
            if (
                entry.prefab != null &&
                entry.weight > 0)
            {
                totalWeight += entry.weight;
            }
        }


        if (totalWeight <= 0)
        {
            return null;
        }


        int randomValue =
            Random.Range(0, totalWeight);


        int cumulativeWeight = 0;


        foreach (SpawnEntry entry in entries)
        {
            if (
                entry.prefab == null ||
                entry.weight <= 0)
            {
                continue;
            }


            cumulativeWeight +=
                entry.weight;


            if (
                randomValue <
                cumulativeWeight)
            {
                return entry.prefab;
            }
        }


        return null;
    }


    private void ClearGeneratedObjects()
    {
        if (generatedRoot == null)
        {
            return;
        }


        for (
            int i =
                generatedRoot.childCount - 1;
            i >= 0;
            i--)
        {
            GameObject child =
                generatedRoot
                    .GetChild(i)
                    .gameObject;

            child.SetActive(false);

            Destroy(child);
        }
    }


    private void OnGUI()
    {
        GUI.Box(
            new Rect(10, 10, 310, 130),
            "PCG Debug"
        );


        GUI.Label(
            new Rect(20, 40, 280, 20),
            $"Seed: {currentSeed}"
        );


        GUI.Label(
            new Rect(20, 60, 280, 20),
            $"Enemy: {successfulEnemySpawns}/{enemyCount}"
        );


        GUI.Label(
            new Rect(20, 80, 280, 20),
            $"Item: {successfulItemSpawns}/{itemCount}"
        );


        GUI.Label(
            new Rect(20, 100, 280, 20),
            $"Rejected: {rejectedPositions}"
        );


        GUI.Label(
            new Rect(20, 120, 280, 20),
            "Press R to regenerate"
        );
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;


        Vector3 center =
            transform.position +
            Vector3.up * spawnY;


        Vector3 size =
            new Vector3(
                spawnAreaSize.x,
                0.1f,
                spawnAreaSize.y
            );


        Gizmos.DrawWireCube(
            center,
            size
        );


        if (player != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                player.position,
                minDistanceFromPlayer
            );
        }
    }
}
\`\`\`

---

# 35. Memahami SpawnEntry

Perhatikan:

\`\`\`csharp
[System.Serializable]
public class SpawnEntry
{
    public string name;
    public GameObject prefab;
    public int weight = 1;
}
\`\`\`

Class ini menyimpan:

\`\`\`text
Nama
Prefab
Weight
\`\`\`

Contoh:

\`\`\`text
name   = Red Enemy
prefab = Enemy_Red
weight = 60
\`\`\`

---

# 36. Mengapa Menggunakan System.Serializable?

Tanpa:

\`\`\`csharp
[System.Serializable]
\`\`\`

class \`SpawnEntry\` tidak otomatis ditampilkan sebagai struktur editable pada Inspector.

Dengan atribut tersebut kita dapat mengatur:

\`\`\`text
Enemy Entries
    Element 0
        Name
        Prefab
        Weight

    Element 1
        Name
        Prefab
        Weight
\`\`\`

Ini merupakan contoh **designer-friendly PCG parameterization**.

---

# 37. Header pada Inspector

Contoh:

\`\`\`csharp
[Header("Seed")]
\`\`\`

digunakan untuk membuat separator pada Inspector.

Tidak memengaruhi algoritma.

Tujuannya hanya meningkatkan keterbacaan parameter.

---

# 38. Min Attribute

\`\`\`csharp
[Min(0)]
public int weight = 1;
\`\`\`

digunakan agar Inspector tidak mudah menerima nilai weight negatif.

---

# 39. Bagian Seed

\`\`\`csharp
[SerializeField]
private bool useRandomSeed = false;

[SerializeField]
private int seed = 12345;
\`\`\`

Jika:

\`\`\`text
useRandomSeed = false
\`\`\`

maka script menggunakan seed dari Inspector.

Ini sangat berguna untuk:

\`\`\`text
Debugging
Reproducibility
Experiment
\`\`\`

---

# 40. Random Seed

Jika:

\`\`\`text
useRandomSeed = true
\`\`\`

script menjalankan:

\`\`\`csharp
seed = System.Environment.TickCount;
\`\`\`

Sehingga seed berubah antar-generation.

Kemudian:

\`\`\`csharp
Random.InitState(currentSeed);
\`\`\`

menginisialisasi generator random Unity.

---

# 41. Mengapa Seed Disimpan?

\`\`\`csharp
currentSeed = seed;
\`\`\`

bertujuan agar seed generation dapat:

- dilihat,
- dicatat,
- dilaporkan,
- diuji kembali.

Contoh:

\`\`\`text
Mahasiswa menemukan spawn yang aneh.

Seed = 78321
\`\`\`

Mahasiswa cukup memasukkan:

\`\`\`text
78321
\`\`\`

dan mencoba generation kembali.

---

# 42. GenerateRandomPosition()

Method:

\`\`\`csharp
GenerateRandomPosition()
\`\`\`

bertanggung jawab membuat posisi kandidat.

Pertama:

\`\`\`csharp
float halfWidth =
    spawnAreaSize.x * 0.5f;
\`\`\`

Jika:

\`\`\`text
spawnAreaSize.x = 24
\`\`\`

maka:

\`\`\`text
halfWidth = 12
\`\`\`

Random X menjadi:

\`\`\`text
-12 sampai +12
\`\`\`

---

# 43. Candidate Position

\`\`\`csharp
return transform.position +
    new Vector3(
        randomX,
        spawnY,
        randomZ
    );
\`\`\`

Posisi spawn dihitung relatif terhadap posisi:

\`\`\`text
PCGManager
\`\`\`

Keuntungannya:

jika PCGManager dipindahkan, spawn area ikut berpindah.

---

# 44. Validation Rule 1 — Player Safe Zone

Script:

\`\`\`csharp
if (distanceToPlayer <
    minDistanceFromPlayer)
{
    return false;
}
\`\`\`

Misalnya:

\`\`\`text
minDistanceFromPlayer = 5
\`\`\`

maka radius lima Unity unit di sekitar Player merupakan:

\`\`\`text
Safe Zone
\`\`\`

Object procedural tidak boleh muncul di area tersebut.

---

# 45. Mengapa Safe Zone Penting?

Tanpa safe zone:

\`\`\`text
Enemy dapat muncul tepat di depan Player.
\`\`\`

Secara teknis random benar.

Tetapi secara gameplay:

\`\`\`text
hasil PCG terasa tidak adil.
\`\`\`

Inilah contoh bahwa:

\`\`\`text
Valid secara algoritmik
≠
baik secara game design
\`\`\`

---

# 46. Validation Rule 2 — Obstacle

Kode:

\`\`\`csharp
bool blocked =
    Physics.CheckSphere(
        candidate,
        checkRadius,
        obstacleMask,
        QueryTriggerInteraction.Ignore
    );
\`\`\`

Script membuat sphere virtual pada candidate position.

Jika sphere menyentuh Collider pada:

\`\`\`text
Obstacle Layer
\`\`\`

maka:

\`\`\`csharp
blocked == true
\`\`\`

dan posisi ditolak.

---

# 47. checkRadius

Parameter:

\`\`\`csharp
checkRadius
\`\`\`

menentukan seberapa besar ruang kosong yang harus tersedia.

Contoh:

\`\`\`text
checkRadius = 0.75
\`\`\`

Semakin besar nilainya:

\`\`\`text
spawn semakin jauh dari obstacle
\`\`\`

tetapi semakin sulit menemukan posisi valid.

---

# 48. QueryTriggerInteraction.Ignore

Parameter:

\`\`\`csharp
QueryTriggerInteraction.Ignore
\`\`\`

berarti physics query mengabaikan Collider yang hanya bertindak sebagai Trigger.

Pada praktikum ini kita ingin memeriksa obstacle fisik.

---

# 49. Validation Rule 3 — Minimum Spacing

Posisi object yang sudah berhasil dibuat disimpan:

\`\`\`csharp
usedPositions.Add(candidate);
\`\`\`

Kemudian candidate baru dibandingkan terhadap seluruh posisi sebelumnya.

\`\`\`csharp
if (distance < minimumSpacing)
{
    return false;
}
\`\`\`

Tujuan:

\`\`\`text
Enemy
Enemy
Enemy
\`\`\`

tidak terkumpul tepat pada posisi yang hampir sama.

---

# 50. Mengapa Tidak Hanya Menggunakan Physics.CheckSphere?

Pada modul ini \`Physics.CheckSphere\` digunakan terutama untuk obstacle.

Jarak antar-object generated disimpan secara eksplisit melalui:

\`\`\`text
usedPositions
\`\`\`

Keuntungannya:

- algoritma lebih mudah dipahami,
- tidak tergantung physics synchronization untuk object yang baru dibuat,
- jelas memperlihatkan konsep validation berbasis data.

---

# 51. maxAttemptsPerObject

Bagian penting:

\`\`\`csharp
for (
    int attempt = 0;
    attempt < maxAttemptsPerObject;
    attempt++)
\`\`\`

Mengapa diperlukan?

Bayangkan parameter:

\`\`\`text
Spawn Area sangat kecil
Obstacle sangat banyak
minimumSpacing sangat besar
enemyCount = 1000
\`\`\`

Mungkin tidak tersedia posisi valid.

Tanpa batas percobaan:

\`\`\`text
algoritma dapat terus mencoba.
\`\`\`

Dengan:

\`\`\`text
maxAttemptsPerObject = 50
\`\`\`

algoritma menyerah setelah 50 percobaan.

---

# 52. Generate-and-Test

Bagian ini merupakan contoh sederhana:

\`\`\`text
Generate Candidate
       ↓
Test Candidate
       ↓
Valid?
├── Ya → Use
└── Tidak → Generate lagi
\`\`\`

Ini adalah:

\`\`\`text
Generate-and-Test PCG
\`\`\`

---

# 53. Constructive + Generate-and-Test

Praktikum ini sebenarnya menggunakan pendekatan hybrid.

### Constructive

Sistem langsung membuat content:

\`\`\`text
enemy
item
\`\`\`

berdasarkan jumlah yang diminta.

### Generate-and-Test

Setiap posisi yang dihasilkan diuji terlebih dahulu.

Sehingga pendekatannya:

\`\`\`text
Constructive PCG
+
Simple Generate-and-Test
\`\`\`

---

# 54. Weighted Random Algorithm

Misalnya:

\`\`\`text
Red    = 60
Blue   = 30
Yellow = 10
\`\`\`

Total:

\`\`\`text
100
\`\`\`

Sistem memilih:

\`\`\`csharp
Random.Range(0, 100);
\`\`\`

Kemungkinan sederhananya:

\`\`\`text
0  – 59 → Red
60 – 89 → Blue
90 – 99 → Yellow
\`\`\`

---

# 55. Kelebihan Weight Dibanding Probability Tetap

Kita tidak harus memasukkan:

\`\`\`text
0.60
0.30
0.10
\`\`\`

Weight dapat:

\`\`\`text
6
3
1
\`\`\`

dan tetap memiliki rasio yang sama.

Atau:

\`\`\`text
600
300
100
\`\`\`

Karena algoritma menggunakan:

\`\`\`text
relative weight
\`\`\`

bukan persentase absolut.

---

# 56. Mengatur Script pada PCGManager

Drag:

\`\`\`text
PCGSpawner.cs
\`\`\`

ke:

\`\`\`text
PCGManager
\`\`\`

Pada Inspector akan muncul beberapa bagian.

---

# 57. Mengisi References

Set:

\`\`\`text
Player
→ drag Player dari Hierarchy

Generated Root
→ drag GeneratedObjects
\`\`\`

---

# 58. Mengatur Spawn Area

Gunakan:

\`\`\`text
Spawn Area Size
X = 24
Y = 24

Spawn Y = 0.5
\`\`\`

Catatan:

\`Vector2.y\` pada parameter \`spawnAreaSize\` digunakan sebagai ukuran arah Z di dunia 3D.

---

# 59. Mengatur Spawn Count

Contoh:

\`\`\`text
Enemy Count = 15

Item Count = 10
\`\`\`

---

# 60. Mengatur Validation

Gunakan nilai awal:

\`\`\`text
Min Distance From Player = 5

Check Radius = 0.75

Minimum Spacing = 1.5

Max Attempts Per Object = 50
\`\`\`

---

# 61. Mengatur Obstacle Mask

Pada:

\`\`\`text
Obstacle Mask
\`\`\`

pilih:

\`\`\`text
Obstacle
\`\`\`

Ini sangat penting.

Jika tidak dipilih:

\`\`\`text
Physics.CheckSphere
\`\`\`

tidak akan memfilter obstacle sesuai yang diinginkan.

---

# 62. Mengatur Enemy Weighted Table

Set:

\`\`\`text
Enemy Entries
Size = 3
\`\`\`

### Element 0

\`\`\`text
Name   = Common Red
Prefab = Enemy_Red
Weight = 60
\`\`\`

### Element 1

\`\`\`text
Name   = Uncommon Blue
Prefab = Enemy_Blue
Weight = 30
\`\`\`

### Element 2

\`\`\`text
Name   = Rare Yellow
Prefab = Enemy_Yellow
Weight = 10
\`\`\`

---

# 63. Mengatur Item Weighted Table

Set:

\`\`\`text
Item Entries
Size = 3
\`\`\`

Contoh:

\`\`\`text
Coin
Weight = 70

Health
Weight = 25

Rare
Weight = 5
\`\`\`

---

# 64. Konfigurasi Seed Awal

Atur:

\`\`\`text
Use Random Seed = false

Seed = 12345
\`\`\`

---

# 65. Menjalankan Praktikum

Tekan:

\`\`\`text
Play
\`\`\`

Hasil yang diharapkan:

- enemy muncul,
- item muncul,
- tidak ada spawn di tengah obstacle,
- tidak ada spawn dalam safe radius Player,
- object tidak saling bertumpuk berlebihan,
- panel debug muncul,
- seed terlihat.

---

# 66. Debug Panel

Bagian kiri atas Game View akan menampilkan:

\`\`\`text
PCG Debug

Seed: 12345
Enemy: 15/15
Item: 10/10
Rejected: 14

Press R to regenerate
\`\`\`

---

# 67. Rejected Positions

Contoh:

\`\`\`text
Rejected: 14
\`\`\`

berarti generator pernah menemukan 14 candidate positions yang gagal karena:

- terlalu dekat player,
- terkena obstacle,
- terlalu dekat generated object lainnya.

Rejected bukan berarti bug.

Sebaliknya, ini menunjukkan:

\`\`\`text
validation bekerja.
\`\`\`

---

# 68. Uji Reproducibility

Sekarang lakukan eksperimen.

Set:

\`\`\`text
Use Random Seed = false
Seed = 12345
\`\`\`

Jalankan game.

Amati posisi enemy dan item.

Stop.

Play kembali.

Jika konfigurasi dan urutan generation sama:

\`\`\`text
layout harus konsisten.
\`\`\`

---

# 69. Uji Seed Berbeda

Ganti:

\`\`\`text
Seed = 100
\`\`\`

Play.

Kemudian:

\`\`\`text
Seed = 200
\`\`\`

Play.

Bandingkan hasil.

Mahasiswa seharusnya melihat:

\`\`\`text
Seed berbeda
→ posisi dan pemilihan object berbeda
\`\`\`

---

# 70. Tombol Regenerate

Pada runtime tekan:

\`\`\`text
R
\`\`\`

Jika:

\`\`\`text
Use Random Seed = false
\`\`\`

dan seed tetap sama:

hasil seharusnya kembali mengikuti urutan random yang sama.

Jika:

\`\`\`text
Use Random Seed = true
\`\`\`

seed baru akan digunakan.

---

# 71. Uji Player Safe Zone

Pilih PCGManager pada Scene View.

Karena:

\`\`\`csharp
OnDrawGizmosSelected()
\`\`\`

aktif, akan terlihat:

\`\`\`text
wireframe spawn area
\`\`\`

dan safe radius di sekitar Player.

Tekan:

\`\`\`text
R
\`\`\`

beberapa kali menggunakan random seed.

Pastikan enemy tidak berada di safe radius.

---

# 72. Uji Obstacle Validation

Tambahkan obstacle besar di tengah arena.

Set Layer:

\`\`\`text
Obstacle
\`\`\`

Pastikan ada Collider.

Generate ulang.

Object tidak seharusnya muncul menembus obstacle tersebut.

---

# 73. Eksperimen checkRadius

Bandingkan:

\`\`\`text
checkRadius = 0.25
\`\`\`

dengan:

\`\`\`text
checkRadius = 2
\`\`\`

Pertanyaan:

> Apa pengaruhnya terhadap jumlah rejected position?

Biasanya radius lebih besar membuat aturan menjadi lebih ketat.

---

# 74. Eksperimen minimumSpacing

Coba:

\`\`\`text
minimumSpacing = 0.5
\`\`\`

kemudian:

\`\`\`text
minimumSpacing = 3
\`\`\`

Bandingkan distribusi object.

---

# 75. Eksperimen maxAttempts

Gunakan:

\`\`\`text
Enemy Count = 50

Minimum Spacing = 5

Max Attempts = 5
\`\`\`

Kemungkinan beberapa enemy gagal dibuat.

Console akan menampilkan:

\`\`\`text
Gagal spawn object...
\`\`\`

Ini bukan error script.

Ini contoh:

\`\`\`text
constraint terlalu ketat
\`\`\`

---

# 76. Parameter Tuning

Dalam PCG, designer sering mencari keseimbangan antara:

\`\`\`text
Variety
Constraint
Density
Fairness
Performance
\`\`\`

Contoh:

Jika:

\`\`\`text
enemyCount terlalu tinggi
\`\`\`

generator kesulitan mencari ruang.

Jika:

\`\`\`text
minimumSpacing terlalu rendah
\`\`\`

object terlalu berdekatan.

Jika:

\`\`\`text
minimumSpacing terlalu tinggi
\`\`\`

jumlah spawn berhasil menurun.

---

# 77. Mengapa Parameter Diletakkan di Inspector?

Bayangkan designer ingin menguji:

\`\`\`text
Enemy Count 10
Enemy Count 20
Enemy Count 50
\`\`\`

Jika nilai ditulis langsung:

\`\`\`csharp
int enemyCount = 10;
\`\`\`

dan tidak expose ke Inspector, designer harus mengedit script.

Dengan:

\`\`\`csharp
[SerializeField]
private int enemyCount;
\`\`\`

designer cukup mengubah Inspector.

Inilah bentuk:

\`\`\`text
designer control
\`\`\`

dalam PCG.

---

# 78. Pengujian Weighted Random

Gunakan:

\`\`\`text
Enemy Count = 100
\`\`\`

Weight:

\`\`\`text
Red    = 60
Blue   = 30
Yellow = 10
\`\`\`

Generate beberapa kali.

Catat jumlah tiap jenis.

Hasil tidak harus persis:

\`\`\`text
60
30
10
\`\`\`

karena probability berlaku secara statistik.

Tetapi dalam jumlah sampel lebih besar distribusi biasanya mulai mendekati bobot relatif.

---

# 79. Eksperimen Weighted Random

Percobaan 1:

\`\`\`text
Red    80
Blue   15
Yellow 5
\`\`\`

Percobaan 2:

\`\`\`text
Red    33
Blue   33
Yellow 34
\`\`\`

Percobaan 3:

\`\`\`text
Red    10
Blue   20
Yellow 70
\`\`\`

Bandingkan bagaimana komposisi arena berubah.

---

# 80. PCG Taxonomy Praktikum Ini

Sistem yang dibuat dapat dikategorikan sebagai:

| Dimensi | Praktikum |
|---|---|
| Waktu generation | Online |
| Peran PCG | Optional |
| Randomness | Stochastic + Seed |
| Generation | Constructive |
| Validation | Generate-and-Test sederhana |
| Designer interaction | Automatic |
| Adaptasi player | Generic |

---

# 81. Mengapa Online PCG?

Karena content dibuat:

\`\`\`text
ketika game berjalan
\`\`\`

tepatnya ketika:

\`\`\`csharp
Start()
\`\`\`

memanggil:

\`\`\`csharp
Generate();
\`\`\`

---

# 82. Mengapa Stochastic?

Karena menggunakan:

\`\`\`csharp
Random.Range()
\`\`\`

sehingga terdapat variasi.

Tetapi variasi tersebut masih dapat direproduksi menggunakan:

\`\`\`text
seed
\`\`\`

---

# 83. Generic PCG

Generator belum mengubah difficulty berdasarkan skill player.

Artinya:

\`\`\`text
Player A
Player B
Player C
\`\`\`

dengan seed dan parameter sama akan mendapat aturan generation yang sama.

Ini disebut:

\`\`\`text
Generic PCG
\`\`\`

Pada pembahasan berikutnya, sistem seperti ini dapat dikembangkan menjadi:

\`\`\`text
Adaptive PCG
\`\`\`

yang berhubungan dengan Dynamic Difficulty Adjustment.

---

# 84. Visual Debugging dengan Gizmos

Method:

\`\`\`csharp
OnDrawGizmosSelected()
\`\`\`

digunakan agar debug hanya tampil saat PCGManager dipilih.

Spawn area digambar dengan:

\`\`\`csharp
Gizmos.DrawWireCube()
\`\`\`

Sedangkan safe zone Player:

\`\`\`csharp
Gizmos.DrawWireSphere()
\`\`\`

---

# 85. Kenapa Visual Debug Penting?

Tanpa Gizmo, developer hanya melihat:

\`\`\`text
object muncul / object tidak muncul
\`\`\`

Dengan Gizmo kita melihat:

\`\`\`text
batas spawn
safe zone
hubungan posisi
\`\`\`

Debug visual sangat membantu pada sistem AI dan PCG.

---

# 86. Kesalahan Umum 1 — Enemy Muncul di Obstacle

Periksa:

\`\`\`text
Obstacle memiliki Collider?
\`\`\`

dan:

\`\`\`text
Layer obstacle sudah = Obstacle?
\`\`\`

kemudian:

\`\`\`text
PCGManager
→ Obstacle Mask
→ Obstacle
\`\`\`

---

# 87. Kesalahan Umum 2 — Semua Spawn Gagal

Kemungkinan:

\`\`\`text
Spawn area terlalu kecil
Minimum spacing terlalu besar
Safe zone terlalu besar
Obstacle terlalu banyak
checkRadius terlalu besar
\`\`\`

Solusi:

longgarkan constraint.

---

# 88. Kesalahan Umum 3 — Prefab Tidak Muncul

Periksa:

\`\`\`text
Enemy Entries
Item Entries
\`\`\`

Pastikan setiap element memiliki:

\`\`\`text
Prefab != None
Weight > 0
\`\`\`

---

# 89. Kesalahan Umum 4 — Weighted Random Tidak Bekerja

Jika semua:

\`\`\`text
Weight = 0
\`\`\`

maka:

\`\`\`csharp
totalWeight <= 0
\`\`\`

dan generator tidak dapat memilih prefab.

Gunakan weight positif.

---

# 90. Kesalahan Umum 5 — Player Tidak Bergerak

Pastikan Player memiliki:

\`\`\`text
Character Controller
SimplePlayerController
\`\`\`

Jika Input API menghasilkan error, periksa konfigurasi Active Input Handling pada project atau gunakan Player Controller dari praktikum sebelumnya.

---

# 91. Kesalahan Umum 6 — Seed Sama tetapi Hasil Berbeda

Reproducibility membutuhkan:

\`\`\`text
seed sama
parameter sama
urutan Random call sama
jumlah object sama
weighted table sama
\`\`\`

Jika salah satu berubah, urutan penggunaan random dapat berubah sehingga layout berikutnya juga berubah.

---

# 92. Kesalahan Umum 7 — Object Terlihat Mengambang

Atur:

\`\`\`text
spawnY
\`\`\`

berdasarkan pivot prefab.

Contoh:

Capsule dengan pivot tengah mungkin membutuhkan:

\`\`\`text
Y = 1
\`\`\`

sedangkan sphere kecil mungkin hanya:

\`\`\`text
Y = 0.5
\`\`\`

Untuk project lebih lanjut dapat dibuat:

\`\`\`text
ground sampling
\`\`\`

atau masing-masing prefab mempunyai offset Y sendiri.

---

# 93. Challenge 1 — Random Rotation

Tambahkan rotasi Y random.

Ganti:

\`\`\`csharp
Quaternion.identity
\`\`\`

dengan misalnya:

\`\`\`csharp
Quaternion rotation =
    Quaternion.Euler(
        0f,
        Random.Range(0f, 360f),
        0f
    );
\`\`\`

Kemudian:

\`\`\`csharp
Instantiate(
    selectedPrefab,
    candidate,
    rotation,
    generatedRoot
);
\`\`\`

Cocok untuk:

- rock,
- vegetation,
- decoration.

---

# 94. Challenge 2 — Per-Prefab Spawn Height

Tambahkan pada \`SpawnEntry\`:

\`\`\`csharp
public float yOffset;
\`\`\`

Sehingga enemy dan item dapat memiliki ketinggian berbeda.

---

# 95. Challenge 3 — Spawn Zone

Buat dua PCGManager:

\`\`\`text
EnemySpawner_North

EnemySpawner_South
\`\`\`

dengan:

\`\`\`text
spawn area
weight
enemy count
\`\`\`

berbeda.

Contoh:

\`\`\`text
North
→ enemy mudah

South
→ enemy langka lebih banyak
\`\`\`

---

# 96. Challenge 4 — Difficulty Budget

Setiap enemy diberikan cost.

Contoh:

\`\`\`text
Slime       = 1
Goblin      = 2
Skeleton    = 3
MiniBoss    = 8
\`\`\`

Kemudian generator diberi:

\`\`\`text
difficultyBudget = 20
\`\`\`

Generator hanya boleh memilih kombinasi enemy sampai total cost mencapai budget.

Ini merupakan pengembangan dari:

\`\`\`text
weighted random
\`\`\`

menuju controlled procedural encounter generation.

---

# 97. Challenge 5 — NavMesh Validation

Untuk project yang menggunakan navigation, posisi random dapat diproyeksikan atau diperiksa ke NavMesh menggunakan:

\`\`\`csharp
NavMesh.SamplePosition()
\`\`\`

Unity 6 menyediakan \`NavMesh.SamplePosition\` untuk mencari posisi terdekat pada NavMesh dari posisi yang diberikan.

Konsep:

\`\`\`text
Random Candidate
      ↓
NavMesh.SamplePosition
      ↓
Position on walkable NavMesh?
├── No  → reject
└── Yes → lanjut validation
\`\`\`

Pengembangan ini sangat cocok untuk menghubungkan PCG dengan Praktikum 4 tentang:

\`\`\`text
Pathfinding & Navigation
\`\`\`

---

# 98. Mengapa NavMesh Tidak Dijadikan Bagian Wajib?

Tujuan utama Praktikum 9 adalah:

\`\`\`text
PCG fundamentals
\`\`\`

bukan kembali membahas navigation secara mendalam.

Karena itu:

\`\`\`text
Physics.CheckSphere
+
Distance Validation
+
Seed
+
Weighted Random
\`\`\`

sudah cukup sebagai requirement utama.

NavMesh dijadikan enhancement.

---

# 99. Challenge 6 — Spawn Statistik

Tambahkan penghitung:

\`\`\`text
Jumlah Red
Jumlah Blue
Jumlah Yellow
\`\`\`

Kemudian tampilkan pada debug panel.

Lakukan generation:

\`\`\`text
100
500
1000
\`\`\`

enemy.

Amati hubungan antara:

\`\`\`text
Weight
\`\`\`

dan:

\`\`\`text
hasil statistik.
\`\`\`

---

# 100. Challenge 7 — Seed History

Simpan beberapa seed terakhir:

\`\`\`text
12345
71232
88501
\`\`\`

Mahasiswa dapat memilih seed yang dianggap menghasilkan layout paling menarik.

Konsep ini menyerupai workflow designer:

\`\`\`text
Generate
→ Evaluate
→ Select
\`\`\`

---

# 101. Challenge 8 — Spawn Rarity

Ubah weighted table item:

\`\`\`text
Common Coin       70
Health Potion     20
Rare Crystal       8
Legendary Gem      2
\`\`\`

Tambahkan visual berbeda untuk menunjukkan rarity.

---

# 102. Analisis PCG

Setelah praktikum selesai, mahasiswa harus memahami bahwa sistem ini bukan sekadar:

\`\`\`text
Random.Range()
\`\`\`

melainkan pipeline:

\`\`\`text
Parameters
     ↓
Seed
     ↓
Pseudo Random
     ↓
Candidate
     ↓
Validation
     ↓
Weighted Selection
     ↓
Instantiation
     ↓
Evaluation
\`\`\`

---

# 103. Hubungan dengan Praktikum Sebelumnya

Sistem ini dapat digabungkan dengan:

## Movement AI

Enemy hasil procedural spawn dapat menggunakan:

\`\`\`text
Seek
Flee
Arrive
Steering
\`\`\`

---

## Navigation

Enemy dapat di-spawn pada NavMesh.

---

## FSM

Enemy procedural dapat memiliki state:

\`\`\`text
Patrol
Chase
Attack
Search
\`\`\`

---

## Behavior Tree

Prefab enemy dapat memiliki Behavior Tree.

---

## Utility AI

Enemy hasil procedural dapat memilih action berdasarkan utility score.

---

## Tactical AI

PCG dapat menghasilkan:

- enemy positions,
- tactical encounter composition,
- resource positions,
- cover distribution.

---

# 104. Hubungan dengan Pertemuan 10

Praktikum ini berfokus pada:

\`\`\`text
Entity Generation
\`\`\`

yaitu:

\`\`\`text
Enemy
Item
\`\`\`

Pertemuan berikutnya akan berpindah pada:

\`\`\`text
Space Generation
\`\`\`

yaitu:

\`\`\`text
Level
Dungeon
Room
Corridor
Cave
\`\`\`

Sehingga urutan pembelajaran menjadi jelas:

\`\`\`text
Pertemuan 9
PCG Fundamental
+
Procedural Spawning

        ↓

Pertemuan 10
PCG Level & Dungeon Generation
\`\`\`

---

# 105. Eksperimen Wajib Mahasiswa

Mahasiswa melakukan minimal lima eksperimen.

## Eksperimen 1 — Seed

Gunakan:

\`\`\`text
Seed 100
Seed 200
Seed 300
\`\`\`

Bandingkan hasil.

---

## Eksperimen 2 — Reproducibility

Gunakan:

\`\`\`text
Seed 500
\`\`\`

sebanyak tiga kali.

Apakah layout konsisten?

---

## Eksperimen 3 — Safe Radius

Bandingkan:

\`\`\`text
3
5
8
\`\`\`

untuk:

\`\`\`text
minDistanceFromPlayer
\`\`\`

---

## Eksperimen 4 — Density

Bandingkan:

\`\`\`text
minimumSpacing = 0.5
1.5
3.0
\`\`\`

---

## Eksperimen 5 — Weight

Bandingkan:

\`\`\`text
60 : 30 : 10
\`\`\`

dengan:

\`\`\`text
33 : 33 : 34
\`\`\`

---

# 106. Tabel Hasil Eksperimen

Mahasiswa dapat membuat tabel seperti:

| Seed | Enemy | Item | Rejected | Catatan |
|---:|---:|---:|---:|---|
| 100 | 15 | 10 | ... | ... |
| 200 | 15 | 10 | ... | ... |
| 300 | 15 | 10 | ... | ... |

Kemudian analisis:

- seed mana yang menghasilkan distribusi terbaik,
- seed mana yang menghasilkan rejection paling tinggi,
- apakah obstacle memengaruhi hasil,
- apakah constraint terlalu ketat.

---

# 107. Pertanyaan Analisis

Jawab pertanyaan berikut.

1. Mengapa PCG tidak sama dengan random murni?
2. Apa fungsi seed?
3. Apa yang dimaksud reproducibility?
4. Mengapa seed penting untuk debugging?
5. Apa fungsi \`Random.InitState()\`?
6. Apa fungsi \`Random.Range()\`?
7. Apa yang dimaksud controlled randomness?
8. Mengapa spawn membutuhkan validation?
9. Apa fungsi LayerMask?
10. Apa fungsi \`Physics.CheckSphere()\`?
11. Mengapa obstacle harus mempunyai Collider?
12. Apa fungsi \`minimumSpacing\`?
13. Apa fungsi \`minDistanceFromPlayer\`?
14. Mengapa diperlukan \`maxAttemptsPerObject\`?
15. Apa yang terjadi jika constraint terlalu ketat?
16. Apa itu weighted random?
17. Mengapa weighted random cocok untuk loot atau enemy selection?
18. Apa hubungan PCG dengan pathfinding?
19. Apa perbedaan constructive PCG dan generate-and-test?
20. Di bagian mana kedua konsep tersebut muncul pada praktikum ini?

---

# 108. Tugas Modifikasi

Modifikasi project agar memenuhi minimal tiga fitur berikut:

- random rotation,
- fourth enemy type,
- legendary item,
- minimum distance item dari enemy,
- enemy spawn zone berbeda,
- different weight per zone,
- NavMesh validation,
- spawn statistics,
- regeneration button UI,
- random seed history,
- difficulty budget.

---

# 109. Kriteria Keberhasilan Praktikum

Praktikum dianggap berhasil apabila:

- [ ] Project dapat dijalankan tanpa compile error.
- [ ] Enemy berhasil di-spawn secara procedural.
- [ ] Item berhasil di-spawn secara procedural.
- [ ] Spawn area dapat diubah melalui Inspector.
- [ ] Seed dapat ditentukan.
- [ ] Seed sama menghasilkan generation yang reproducible.
- [ ] Seed berbeda menghasilkan variasi.
- [ ] Enemy tidak muncul terlalu dekat Player.
- [ ] Object tidak muncul di dalam obstacle.
- [ ] Object tidak terlalu bertumpuk.
- [ ] Weighted random bekerja.
- [ ] \`maxAttempts\` digunakan.
- [ ] Debug seed ditampilkan.
- [ ] Rejected candidate dapat diamati.
- [ ] Spawn area terlihat melalui Gizmos.
- [ ] Mahasiswa mampu menjelaskan algoritma yang dibuat.

---

# 110. Rekomendasi Penilaian

## A. Implementasi Dasar — 40%

| Komponen | Bobot |
|---|---:|
| Project dan Scene benar | 5% |
| Procedural spawning | 10% |
| Seed | 10% |
| Enemy + item prefab | 10% |
| Inspector parameters | 5% |

---

## B. Controlled Randomness — 30%

| Komponen | Bobot |
|---|---:|
| Player safe distance | 10% |
| Obstacle validation | 10% |
| Minimum spacing | 5% |
| maxAttempts | 5% |

---

## C. PCG Tambahan — 15%

| Komponen | Bobot |
|---|---:|
| Weighted random | 10% |
| Debug visualization | 5% |

---

## D. Analisis — 15%

| Komponen | Bobot |
|---|---:|
| Eksperimen seed | 5% |
| Parameter tuning | 5% |
| Penjelasan hasil | 5% |

Total:

\`\`\`text
100%
\`\`\`

---

# 111. Rekomendasi Asset

Untuk tahap awal, primitive Unity sebaiknya digunakan terlebih dahulu agar mahasiswa fokus memahami algoritma.

Setelah algoritma berjalan, prefab dapat diganti dengan asset yang lebih menarik.

Contoh tema:

\`\`\`text
Fantasy Arena
Sci-Fi Arena
Dungeon Encounter
Forest Survival
Alien Planet
Robot Training Ground
\`\`\`

Contoh:

\`\`\`text
Enemy_Red
\`\`\`

dapat diganti menjadi:

\`\`\`text
Goblin
Robot
Alien
Skeleton
\`\`\`

tanpa mengubah algoritma PCG utama.

---

# 112. Rekomendasi Praktikum Terbaik

Dari dua pilihan materi:

\`\`\`text
A. Procedural Spawning

B. Random Level Sederhana
\`\`\`

**pilihan yang paling direkomendasikan untuk Praktikum 09 adalah A — Procedural Spawning**, tetapi ditingkatkan menjadi:

# Seeded, Weighted & Validated Procedural Spawning

Alasannya:

1. Fokus pada konsep fundamental PCG.
2. Seed mudah diamati.
3. Reproducibility mudah diuji.
4. Controlled randomness terlihat jelas.
5. Weighted random dapat dipraktikkan.
6. Validation dapat langsung diamati.
7. \`Physics.CheckSphere\` memperkenalkan physics query yang relevan.
8. LayerMask digunakan secara nyata.
9. \`maxAttempts\` mengajarkan robustness.
10. Scope masih dapat diselesaikan sebagai praktikum satu pertemuan.
11. Tidak terlalu tumpang tindih dengan materi PCG level/dungeon pada Pertemuan 10.

Dengan demikian urutan materi menjadi lebih baik:

\`\`\`text
P09
PCG Fundamental
→ Procedural Entity Spawning

P10
PCG Level & Dungeon
→ Procedural Space Generation
\`\`\`

---

# 113. Nama Project yang Direkomendasikan

## Pilihan Utama

\`\`\`text
GameCerdas_P09_PCG_SmartSpawner
\`\`\`

Nama ini direkomendasikan karena:

\`\`\`text
GameCerdas
\`\`\`

menunjukkan mata kuliah.

\`\`\`text
P09
\`\`\`

menunjukkan pertemuan/praktikum.

\`\`\`text
PCG
\`\`\`

menunjukkan konsep utama.

\`\`\`text
SmartSpawner
\`\`\`

menggambarkan bahwa spawner tidak hanya random, tetapi memiliki rules dan validation.

---

## Alternatif Nama

\`\`\`text
GameCerdas_P09_ProceduralSpawnLab
\`\`\`

atau:

\`\`\`text
GameCerdas_P09_SeededSpawner
\`\`\`

atau:

\`\`\`text
GameCerdas_P09_PCGSpawnArena
\`\`\`

Tetapi rekomendasi utama tetap:

# \`GameCerdas_P09_PCG_SmartSpawner\`

---

# 114. Nama Scene yang Direkomendasikan

\`\`\`text
PCG_SpawnArena
\`\`\`

Alternatif:

\`\`\`text
PCG_Demo
ProceduralSpawnLab
SmartSpawnerDemo
\`\`\`

Rekomendasi:

# \`PCG_SpawnArena\`

---

# 115. Kesimpulan

Pada praktikum ini mahasiswa tidak sekadar membuat object muncul secara acak.

Mahasiswa membangun pipeline:

\`\`\`text
Seed
 ↓
Random Generation
 ↓
Candidate Position
 ↓
Validation
 ↓
Controlled Randomness
 ↓
Weighted Selection
 ↓
Prefab Instantiation
 ↓
Debugging
 ↓
Evaluation
\`\`\`

Konsep paling penting yang harus dipahami adalah:

> **PCG yang baik bukan random murni. PCG adalah randomness yang dikendalikan oleh parameter, rule, constraint, validation, dan tujuan game design.**

Dengan pondasi ini mahasiswa siap memasuki materi berikutnya:

\`\`\`text
PCG for Level & Dungeon Generation
\`\`\`

dengan teknik yang lebih kompleks seperti:

\`\`\`text
Grid-Based Generation
Random Walk
BSP
Cellular Automata
Constraint-Based Generation
\`\`\``,Ac=`# MODUL PRAKTIKUM 1
## NPC Detector: Perception dan Parameter AI di Unity 6

**Mata Kuliah:**  Game Cerdas
**Program Studi:**  S1 Teknik Informatika
**Game Engine:**  Unity 6
**Bahasa Pemrograman:**  C#

---

# 1. Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat prototype**NPC Detector** sederhana.

NPC dapat:

- mendeteksi Player berdasarkan jarak,
- memiliki state \`IDLE\` dan \`ALERT\`,
- berubah warna sesuai state,
- memiliki parameter \`Detection Radius\`,
- menampilkan area deteksi menggunakan Gizmos.

Player dapat digerakkan menggunakan:

\`\`\`text
W A S D
\`\`\`

atau:

\`\`\`text
Arrow Key
↑ ↓ ← →
\`\`\`

Praktikum ini digunakan untuk memahami konsep dasar:

\`\`\`text
Perception → Decision → Action
\`\`\`

---

# 2. Hasil Akhir yang Akan Dibuat

Scene terdiri atas:

\`\`\`text
Main Camera
Directional Light
Ground
Player
Enemy
\`\`\`

Player dapat bergerak menggunakan keyboard.

Enemy memiliki radius deteksi.

\`\`\`text
                 Player
                   ●
                   │
                   │ bergerak
                   ▼

          .-------------------.
       .'                       '.
      /     Detection Radius      \\
     |                             |
     |            Enemy            |
     |              ■              |
     |                             |
      \\                           /
       '.                       .'
          '-------------------'
\`\`\`

Jika Player di luar radius:

\`\`\`text
Enemy = IDLE
Warna = Biru
\`\`\`

Jika Player masuk radius:

\`\`\`text
Enemy = ALERT
Warna = Merah
\`\`\`

---

# 3. Konsep Perception–Decision–Action

NPC dapat dianggap sebagai sebuah**intelligent agent**.

\`\`\`text
Environment
     │
     ▼
Perception
     │
     ▼
Decision
     │
     ▼
Action
     │
     ▼
Environment
\`\`\`

Pada praktikum ini:

\`\`\`text
PERCEPTION
Menghitung jarak Player
        ↓
DECISION
Apakah Player berada
di dalam Detection Radius?
        ↓
   ┌�”€─�”€─�”´─�”€─�”€┐
   ▼         ▼
 IDLE      ALERT
   │         │
   └�”€─�”€─�”¬─�”€─�”€┘
        ▼
ACTION
Mengubah warna Enemy
\`\`\`

---

# 4. Membuat Project Unity

Buka:

\`\`\`text
Unity Hub
\`\`\`

Klik:

\`\`\`text
New Project
\`\`\`

Pilih template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D Core
\`\`\`

Nama project:

\`\`\`text
GameCerdas_Praktikum01_NPCDetector
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 5. Menyimpan Scene

Setelah Unity terbuka, pilih:

\`\`\`text
File
→ Save As
\`\`\`

Buat folder:

\`\`\`text
Assets/Scenes
\`\`\`

Simpan scene dengan nama:

\`\`\`text
NPCDetector
\`\`\`

---

# 6. Membuat Struktur Folder

Pada folder \`Assets\`, buat:

\`\`\`text
Assets
│
├�”€─ Materials
├�”€─ Scenes
└�”€─ Scripts
\`\`\`

Folder digunakan agar project lebih terstruktur.

---

# 7. Membuat Ground

Pada Hierarchy:

\`\`\`text
Right Click
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
Ground
\`\`\`

Atur Transform:

\`\`\`text
Position
X = 0
Y = 0
Z = 0
\`\`\`

Scale:

\`\`\`text
X = 2
Y = 1
Z = 2
\`\`\`

---

# 8. Membuat Player

Pada Hierarchy:

\`\`\`text
Right Click
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Atur Transform:

\`\`\`text
Position
X = 0
Y = 1
Z = -6
\`\`\`

Player akan digunakan untuk menguji kemampuan Enemy mendeteksi objek.

---

# 9. Membuat Player Controller

Agar pengujian NPC lebih mudah, Player akan dibuat dapat bergerak menggunakan:

\`\`\`text
WASD
\`\`\`

atau:

\`\`\`text
Arrow Key
\`\`\`

Kontrol:

| Aksi | WASD | Arrow Key |
|---|---|---|
| Maju | W | ↑ |
| Mundur | S | ↓ |
| Kiri | A | ← |
| Kanan | D | → |

---

# 10. Memastikan Input System Tersedia

Buka:

\`\`\`text
Window
→ Package Manager
\`\`\`

Cari:

\`\`\`text
Input System
\`\`\`

Jika belum terpasang:

\`\`\`text
Install
\`\`\`

Jika Unity meminta restart atau mengganti input system, izinkan Unity melakukan restart.

Jika diperlukan, cek:

\`\`\`text
Edit
→ Project Settings
→ Player
→ Active Input Handling
\`\`\`

Gunakan:

\`\`\`text
Input System Package (New)
\`\`\`

atau:

\`\`\`text
Both
\`\`\`

---

# 11. Membuat Script PlayerController

Masuk ke folder:

\`\`\`text
Assets/Scripts
\`\`\`

Klik kanan:

\`\`\`text
Create
→ MonoBehaviour Script
\`\`\`

Beri nama:

\`\`\`text
PlayerController
\`\`\`

Buka file:

\`\`\`text
PlayerController.cs
\`\`\`

Ganti isinya dengan kode berikut:

\`\`\`csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5f;

    void Update()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (Keyboard.current == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        // Kiri
        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal = -1f;
        }

        // Kanan
        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal = 1f;
        }

        // Maju
        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical = 1f;
        }

        // Mundur
        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            vertical = -1f;
        }

        Vector3 movement =
            new Vector3(horizontal, 0f, vertical);

        movement = movement.normalized;

        transform.position +=
            movement * moveSpeed * Time.deltaTime;
    }
}
\`\`\`

Simpan:

\`\`\`text
Ctrl + S
\`\`\`

---

# 12. Penjelasan PlayerController

Parameter:

\`\`\`csharp
[SerializeField]
private float moveSpeed = 5f;
\`\`\`

akan muncul pada Inspector sebagai:

\`\`\`text
Move Speed
\`\`\`

Nilainya dapat diubah tanpa mengedit program.

Contoh:

\`\`\`text
Move Speed = 3
\`\`\`

Player bergerak lambat.

\`\`\`text
Move Speed = 5
\`\`\`

Player bergerak normal.

\`\`\`text
Move Speed = 10
\`\`\`

Player bergerak lebih cepat.

---

## Membaca Keyboard

Contoh:

\`\`\`csharp
Keyboard.current.wKey.isPressed
\`\`\`

berarti:

> Apakah tombol W sedang ditekan?

Sedangkan:

\`\`\`csharp
Keyboard.current.upArrowKey.isPressed
\`\`\`

berarti:

> Apakah tombol ↑ sedang ditekan?

Operator:

\`\`\`csharp
||
\`\`\`

berarti:

\`\`\`text
OR / ATAU
\`\`\`

Sehingga:

\`\`\`csharp
Keyboard.current.wKey.isPressed ||
Keyboard.current.upArrowKey.isPressed
\`\`\`

berarti:

> W atau Arrow Up dapat digunakan untuk bergerak maju.

---

# 13. Mengapa Menggunakan normalized?

Kode:

\`\`\`csharp
movement = movement.normalized;
\`\`\`

digunakan agar Player tidak bergerak lebih cepat ketika bergerak diagonal.

Contoh:

\`\`\`text
W + D
\`\`\`

menghasilkan:

\`\`\`text
↗
\`\`\`

Tanpa normalisasi, kecepatan diagonal dapat lebih tinggi daripada gerakan lurus.

---

# 14. Mengapa Menggunakan Time.deltaTime?

Kode:

\`\`\`csharp
movement * moveSpeed * Time.deltaTime
\`\`\`

membuat gerakan lebih konsisten terhadap frame rate.

Dengan demikian game pada:

\`\`\`text
30 FPS
60 FPS
120 FPS
\`\`\`

tidak menyebabkan perbedaan kecepatan Player yang terlalu besar.

---

# 15. Memasang PlayerController

Pilih:

\`\`\`text
Player
\`\`\`

Drag:

\`\`\`text
PlayerController.cs
\`\`\`

ke Inspector Player.

Atau:

\`\`\`text
Add Component
→ Player Controller
\`\`\`

Atur:

\`\`\`text
Move Speed = 5
\`\`\`

---

# 16. Menguji Player

Tekan:

\`\`\`text
Play
\`\`\`

Klik terlebih dahulu area:

\`\`\`text
Game
\`\`\`

Coba:

\`\`\`text
W
A
S
D
\`\`\`

Kemudian:

\`\`\`text
↑
←
↓
→
\`\`\`

Kontrol:

\`\`\`text
               W / ↑
                 ↑

       A / ←   PLAYER   D / →

                 ↓
               S / ↓
\`\`\`

Coba juga gerakan diagonal:

\`\`\`text
W + D → ↗
W + A → ↖
S + D → ↘
S + A → ↙
\`\`\`

Jika Player dapat bergerak dengan baik, lanjutkan.

---

# 17. Membuat Enemy

Keluar dari Play Mode.

Pada Hierarchy:

\`\`\`text
Right Click
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Enemy
\`\`\`

Atur:

\`\`\`text
Position
X = 0
Y = 1
Z = 2
\`\`\`

Posisi awal menjadi:

\`\`\`text
                Enemy
                  ■

                  ↑
                  │
                  │
                  │

                Player
                  ●
\`\`\`

---

# 18. Membuat Material Enemy

Buka:

\`\`\`text
Assets/Materials
\`\`\`

Klik kanan:

\`\`\`text
Create
→ Material
\`\`\`

Nama:

\`\`\`text
EnemyMaterial
\`\`\`

Drag material tersebut ke:

\`\`\`text
Enemy
\`\`\`

Material akan digunakan untuk menunjukkan perubahan state AI.

---

# 19. Membuat EnemyDetector

Buka:

\`\`\`text
Assets/Scripts
\`\`\`

Klik kanan:

\`\`\`text
Create
→ MonoBehaviour Script
\`\`\`

Nama:

\`\`\`text
EnemyDetector
\`\`\`

Buka:

\`\`\`text
EnemyDetector.cs
\`\`\`

Masukkan kode berikut:

\`\`\`csharp
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Alert
    }

    [Header("Target")]
    [SerializeField]
    private Transform player;

    [Header("AI Parameters")]
    [SerializeField]
    [Min(0f)]
    private float detectionRadius = 5f;

    [Header("Visual")]
    [SerializeField]
    private Color idleColor = Color.blue;

    [SerializeField]
    private Color alertColor = Color.red;

    [Header("Debug")]
    [SerializeField]
    private EnemyState currentState;

    [SerializeField]
    private float currentDistance;

    private Renderer enemyRenderer;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();

        currentState = EnemyState.Alert;
        SetState(EnemyState.Idle);
    }

    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
    {
        if (player == null)
            return;

        currentDistance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (currentDistance <= detectionRadius)
        {
            SetState(EnemyState.Alert);
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }

    void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log(
            "Enemy State → " + currentState
        );

        if (enemyRenderer == null)
            return;

        if (currentState == EnemyState.Alert)
        {
            enemyRenderer.material.color = alertColor;
        }
        else
        {
            enemyRenderer.material.color = idleColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}
\`\`\`

Simpan script.

---

# 20. Memasang EnemyDetector

Pilih:

\`\`\`text
Enemy
\`\`\`

Drag:

\`\`\`text
EnemyDetector.cs
\`\`\`

ke Inspector Enemy.

Component akan menampilkan:

\`\`\`text
Enemy Detector

Target
    Player

AI Parameters
    Detection Radius

Visual
    Idle Color
    Alert Color

Debug
    Current State
    Current Distance
\`\`\`

---

# 21. Menghubungkan Player

Pada Hierarchy terdapat:

\`\`\`text
Player
Enemy
\`\`\`

Pilih:

\`\`\`text
Enemy
\`\`\`

Pada:

\`\`\`text
Enemy Detector
→ Player
\`\`\`

drag GameObject:

\`\`\`text
Player
\`\`\`

ke field tersebut.

Hasil:

\`\`\`text
Player = Player
\`\`\`

Jangan biarkan:

\`\`\`text
Player = None
\`\`\`

---

# 22. Mengatur Parameter Enemy

Pada Inspector Enemy:

\`\`\`text
Detection Radius = 5
\`\`\`

Atur:

\`\`\`text
Idle Color = Blue
Alert Color = Red
\`\`\`

Sekarang parameter perilaku NPC dapat diubah melalui Inspector.

---

# 23. Perception pada NPC

Perception dilakukan dengan:

\`\`\`csharp
currentDistance = Vector3.Distance(
    transform.position,
    player.position
);
\`\`\`

Kode menghitung jarak:

\`\`\`text
Enemy
  ■
  │
  │
  │ Current Distance
  │
  ●
Player
\`\`\`

Informasi yang diketahui NPC adalah:

\`\`\`text
Seberapa jauh Player dari Enemy?
\`\`\`

---

# 24. Decision pada NPC

Kode:

\`\`\`csharp
if (currentDistance <= detectionRadius)
{
    SetState(EnemyState.Alert);
}
else
{
    SetState(EnemyState.Idle);
}
\`\`\`

membentuk aturan:

\`\`\`text
            Hitung jarak
                 │
                 ▼
     distance <= detectionRadius?
            /             \\
          YA               TIDAK
          │                  │
          ▼                  ▼
        ALERT               IDLE
\`\`\`

---

# 25. Action pada NPC

Ketika:

\`\`\`text
State = IDLE
\`\`\`

Enemy menggunakan:

\`\`\`text
Idle Color
\`\`\`

Ketika:

\`\`\`text
State = ALERT
\`\`\`

Enemy menggunakan:

\`\`\`text
Alert Color
\`\`\`

Sehingga:

\`\`\`text
PERCEPTION
   ↓
Menghitung jarak

DECISION
   ↓
IDLE / ALERT

ACTION
   ↓
Mengubah warna
\`\`\`

---

# 26. Menguji NPC Detector

Tekan:

\`\`\`text
Play
\`\`\`

Klik Game View.

Player berada di luar radius.

Enemy seharusnya:

\`\`\`text
State = Idle
Color = Blue
\`\`\`

Gerakkan Player menuju Enemy:

\`\`\`text
W
\`\`\`

atau:

\`\`\`text
↑
\`\`\`

Ketika Player masuk ke Detection Radius:

\`\`\`text
State = Alert
Color = Red
\`\`\`

Kemudian menjauh:

\`\`\`text
S
\`\`\`

atau:

\`\`\`text
↓
\`\`\`

Enemy kembali:

\`\`\`text
State = Idle
Color = Blue
\`\`\`

---

# 27. Mengamati Current Distance

Saat game berjalan, pilih:

\`\`\`text
Enemy
\`\`\`

Perhatikan Inspector.

Contoh:

\`\`\`text
Detection Radius = 5

Current Distance = 8.4
Current State    = Idle
\`\`\`

Ketika mendekat:

\`\`\`text
Current Distance = 5.7
Current State    = Idle
\`\`\`

Setelah melewati radius:

\`\`\`text
Current Distance = 4.8
Current State    = Alert
\`\`\`

Karena:

\`\`\`text
4.8 <= 5
\`\`\`

maka Player terdeteksi.

---

# 28. Menampilkan Detection Radius dengan Gizmos

Pada Scene View, pilih Enemy.

Pastikan:

\`\`\`text
Gizmos
\`\`\`

aktif.

Kode:

\`\`\`csharp
void OnDrawGizmosSelected()
{
    Gizmos.color = Color.yellow;

    Gizmos.DrawWireSphere(
        transform.position,
        detectionRadius
    );
}
\`\`\`

akan menggambar area:

\`\`\`text
             Detection Radius
          .-------------------.
       .'                       '.
      /                           \\
     |                             |
     |            Enemy            |
     |              ■              |
     |                             |
      \\                           /
       '.                       .'
          '-------------------'
\`\`\`

Gizmos hanya digunakan untuk membantu developer saat debugging.

Gizmos tidak menjadi objek game sebenarnya.

---

# 29. Menguji Detection Radius

Ubah:

\`\`\`text
Detection Radius = 3
\`\`\`

Jalankan game.

Player harus lebih dekat untuk dideteksi.

Kemudian coba:

\`\`\`text
Detection Radius = 8
\`\`\`

Player dapat dideteksi dari jarak lebih jauh.

Bandingkan:

| Radius | Perilaku |
|---:|---|
| 3 | Enemy mendeteksi dari dekat |
| 5 | Jarak sedang |
| 8 | Enemy mendeteksi lebih jauh |

Hal ini menunjukkan:

\`\`\`text
AI Parameter
     ↓
AI Behavior
\`\`\`

---

# 30. Mengamati Console

Buka:

\`\`\`text
Window
→ General
→ Console
\`\`\`

Saat Player masuk radius:

\`\`\`text
Enemy State → Alert
\`\`\`

Saat keluar:

\`\`\`text
Enemy State → Idle
\`\`\`

Log hanya ditampilkan ketika state berubah.

Ini mempermudah debugging.

---

# 31. Arsitektur Sistem Akhir

Keseluruhan sistem yang dibuat:

\`\`\`text
           KEYBOARD INPUT
          WASD / Arrow Key
                 │
                 ▼
        PlayerController
                 │
                 ▼
          Player bergerak
                 │
                 ▼
          Player Position
                 │
                 ▼
        ┌�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€┐
        │   PERCEPTION    │
        │Vector3.Distance │
        └�”€─�”€─�”€─�”€─�”¬─�”€─�”€─�”€─�”€┘
                 │
                 ▼
          Current Distance
                 │
                 ▼
        ┌�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€┐
        │    DECISION     │
        │ distance <=     │
        │ detectionRadius │
        └�”€─�”€─�”€─�”€─�”¬─�”€─�”€─�”€─�”€┘
                 │
          ┌�”€─�”€─�”€─�”´─�”€─�”€─�”€┐
          ▼             ▼
        IDLE          ALERT
          │             │
          └�”€─�”€─�”€─�”¬─�”€─�”€─�”€┘
                 ▼
        ┌�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€┐
        │     ACTION      │
        │  Change Color   │
        └�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€─�”€┘
\`\`\`

---

# 32. Hubungan dengan Intelligent Agent

| Konsep | Implementasi |
|---|---|
| Environment | Scene Unity |
| Player | Object yang diamati |
| Agent | Enemy |
| Perception | \`Vector3.Distance()\` |
| Sensor Data | \`currentDistance\` |
| AI Parameter | \`detectionRadius\` |
| Internal State | \`Idle\`, \`Alert\` |
| Decision | Membandingkan distance dan radius |
| Action | Mengubah warna |
| Debugging | Inspector, Console, Gizmos |

---

# 33. Struktur Hierarchy Akhir

Hierarchy:

\`\`\`text
NPCDetector
│
├�”€─ Main Camera
├�”€─ Directional Light
├�”€─ Ground
├�”€─ Player
│   └�”€─ PlayerController
│
└�”€─ Enemy
    └�”€─ EnemyDetector
\`\`\`

---

# 34. Struktur Folder Akhir

Project:

\`\`\`text
Assets
│
├�”€─ Materials
│   └�”€─ EnemyMaterial
│
├�”€─ Scenes
│   └�”€─ NPCDetector.unity
│
└�”€─ Scripts
    ├�”€─ PlayerController.cs
    └�”€─ EnemyDetector.cs
\`\`\`

---

# 35. Troubleshooting

## Player tidak bergerak

Pastikan Player memiliki:

\`\`\`text
Player Controller
\`\`\`

Pastikan:

\`\`\`text
Move Speed > 0
\`\`\`

Klik Game View sebelum menekan keyboard.

---

## Error InputSystem

Jika muncul error:

\`\`\`text
The type or namespace name 'InputSystem'
could not be found
\`\`\`

buka:

\`\`\`text
Window
→ Package Manager
→ Input System
\`\`\`

Install Input System.

---

## Player hanya bergerak dengan salah satu tombol

Periksa kode PlayerController.

Harus terdapat:

\`\`\`csharp
wKey
aKey
sKey
dKey
\`\`\`

dan:

\`\`\`csharp
upArrowKey
downArrowKey
leftArrowKey
rightArrowKey
\`\`\`

---

## Enemy tidak berubah menjadi ALERT

Periksa:

\`\`\`text
Enemy
→ Enemy Detector
→ Player
\`\`\`

Pastikan:

\`\`\`text
Player = Player
\`\`\`

bukan:

\`\`\`text
None
\`\`\`

---

## Enemy selalu ALERT

Perhatikan:

\`\`\`text
Current Distance
\`\`\`

Jika:

\`\`\`text
Current Distance <= Detection Radius
\`\`\`

maka kondisi tersebut benar.

Coba jauhkan Player.

---

## Gizmos tidak tampil

Pastikan:

- Enemy dipilih,
- Scene View aktif,
- tombol Gizmos aktif.

---

## Console menunjukkan error merah

Buka:

\`\`\`text
Window
→ General
→ Console
\`\`\`

Selesaikan semua error sebelum melanjutkan.

---

# 36. Eksperimen

## Eksperimen 1 — Detection Radius

Uji:

\`\`\`text
3
5
8
\`\`\`

Catat pengaruhnya.

---

## Eksperimen 2 — Player Speed

Uji:

\`\`\`text
Move Speed = 2
Move Speed = 5
Move Speed = 10
\`\`\`

Apakah perubahan kecepatan Player mengubah logika AI?

Tidak.

Yang berubah adalah kecepatan perubahan kondisi lingkungan yang diterima AI.

---

## Eksperimen 3 — Warna Enemy

Ubah:

\`\`\`text
Idle Color
\`\`\`

dan:

\`\`\`text
Alert Color
\`\`\`

Perhatikan bahwa perubahan visual tidak mengubah logika perception.

---

# 37. Pertanyaan Analisis

Jawab pertanyaan berikut.

1. Apa yang menjadi**Perception** pada praktikum ini?
2. Apa yang menjadi**Decision**?
3. Apa yang menjadi**Action**?
4. Apa fungsi \`Vector3.Distance()\`?
5. Apa fungsi \`Detection Radius\`?
6. Mengapa \`Detection Radius\` sebaiknya dapat diubah melalui Inspector?
7. Apa manfaat \`enum EnemyState\`?
8. Apa fungsi \`currentDistance\`?
9. Apa fungsi Gizmos?
10. Apa fungsi \`Time.deltaTime\` pada PlayerController?
11. Mengapa movement menggunakan \`.normalized\`?
12. Apakah Detection Radius yang lebih besar selalu berarti NPC lebih pintar?
13. Apakah NPC benar-benar "melihat" Player seperti manusia? Jelaskan.

---

# 38. Tantangan Tambahan — State SUSPICIOUS

Jika prototype dasar sudah selesai, tambahkan satu state:

\`\`\`text
Suspicious
\`\`\`

Sehingga:

\`\`\`text
Idle
Suspicious
Alert
\`\`\`

Aturan:

\`\`\`text
Distance > 8
→ IDLE
\`\`\`

\`\`\`text
Distance <= 8
→ SUSPICIOUS
\`\`\`

\`\`\`text
Distance <= 4
→ ALERT
\`\`\`

Visual:

\`\`\`text
IDLE        = Blue
SUSPICIOUS  = Yellow
ALERT       = Red
\`\`\`

Ilustrasi:

\`\`\`text
               Radius 8
         .-------------------.
       .'                     '.
      /      SUSPICIOUS         \\
     |                           |
     |         Radius 4          |
     |       .----------.        |
     |      /            \\       |
     |     |    ALERT     |      |
     |     |      ■       |      |
     |      \\            /       |
     |       '----------'        |
      \\                         /
       '.                     .'
         '-------------------'

Di luar Radius 8:

IDLE
\`\`\`

Tantangan ini bersifat tambahan dan tidak menjadi requirement minimum.

---

# 39. Checklist Praktikum

Pastikan:

- [ ] Project berhasil dibuat.
- [ ] Scene \`NPCDetector\` dibuat.
- [ ] Ground dibuat.
- [ ] Player dibuat.
- [ ] Enemy dibuat.
- [ ] Input System aktif.
- [ ] \`PlayerController.cs\` dibuat.
- [ ] Player dapat bergerak dengan WASD.
- [ ] Player dapat bergerak dengan Arrow Key.
- [ ] Gerak diagonal bekerja.
- [ ] \`EnemyDetector.cs\` dibuat.
- [ ] Player sudah dimasukkan ke EnemyDetector.
- [ ] Detection Radius dapat diubah.
- [ ] Current Distance muncul.
- [ ] Enemy memiliki state IDLE.
- [ ] Enemy memiliki state ALERT.
- [ ] Enemy berubah warna sesuai state.
- [ ] Gizmos menampilkan Detection Radius.
- [ ] Console menampilkan perubahan state.
- [ ] Tidak terdapat error merah pada Console.

---

# 40. Output Praktikum

Mahasiswa mengumpulkan:

## Project Unity

Nama project:

\`\`\`text
NRP_Nama_Praktikum01_NPCDetector
\`\`\`

---

## Screenshot 1

Menunjukkan:

\`\`\`text
Player berada di luar radius
Enemy = IDLE
\`\`\`

---

## Screenshot 2

Menunjukkan:

\`\`\`text
Player berada di dalam radius
Enemy = ALERT
\`\`\`

---

## Screenshot 3

Menunjukkan Inspector Enemy:

\`\`\`text
Detection Radius
Current State
Current Distance
\`\`\`

---

## Screenshot 4

Menunjukkan:

\`\`\`text
Detection Radius Gizmos
\`\`\`

---

## Video/GIF

Durasi sekitar:

\`\`\`text
10–30 detik
\`\`\`

Menampilkan:

\`\`\`text
Player bergerak
      ↓
Enemy IDLE
      ↓
Player masuk radius
      ↓
Enemy ALERT
      ↓
Player keluar radius
      ↓
Enemy IDLE
\`\`\`

---

# 41. Laporan Singkat

Buat laporan sekitar**1–2 halaman**, terdiri atas:

1. Tujuan praktikum.
2. Konsep Perception–Decision–Action.
3. Implementasi Player Controller.
4. Implementasi NPC Detector.
5. Hasil eksperimen Detection Radius.
6. Screenshot hasil.
7. Kendala yang ditemukan.
8. Kesimpulan.

---

# 42. Kriteria Penilaian

| Komponen | Bobot |
|---|---:|
| Setup Scene dan GameObject | 10% |
| Player Controller WASD/Arrow | 10% |
| Perception / Distance Detection | 20% |
| Decision IDLE/ALERT | 15% |
| Visual Action / perubahan warna | 10% |
| Parameter Inspector | 10% |
| Gizmos dan Debugging | 10% |
| Kerapian kode | 5% |
| Analisis dan laporan | 10% |
|**TOTAL** |**100%** |

---

# 43. Ringkasan

Pada Praktikum 1 mahasiswa telah membuat sistem:

\`\`\`text
Keyboard
WASD / Arrow
      ↓
Player Controller
      ↓
Player bergerak
      ↓
Posisi Player berubah
      ↓
Enemy melakukan Perception
      ↓
Menghitung Distance
      ↓
Decision
      ↓
IDLE / ALERT
      ↓
Action
      ↓
Perubahan warna
\`\`\`

Komponen utama AI yang dipelajari adalah:

\`\`\`text
PERCEPTION
Vector3.Distance()

        ↓

DECISION
distance <= detectionRadius

        ↓

STATE
IDLE / ALERT

        ↓

ACTION
Change Color
\`\`\`

Debugging dilakukan menggunakan:

\`\`\`text
Current Distance
Current State
Console Log
Gizmos
\`\`\`

---

# 44. Kesimpulan

Praktikum ini menunjukkan bahwa NPC sederhana sudah dapat dimodelkan sebagai**intelligent agent**.

NPC menerima informasi dari lingkungan:

\`\`\`text
Player Position
\`\`\`

kemudian melakukan perception:

\`\`\`text
Vector3.Distance()
\`\`\`

selanjutnya menentukan keputusan berdasarkan:

\`\`\`text
Detection Radius
\`\`\`

dan menghasilkan state:

\`\`\`text
IDLE
\`\`\`

atau:

\`\`\`text
ALERT
\`\`\`

State tersebut kemudian diterjemahkan menjadi action berupa perubahan warna Enemy.

Player Controller dengan:

\`\`\`text
WASD
\`\`\`

atau:

\`\`\`text
Arrow Key
\`\`\`

memungkinkan mahasiswa menguji sistem AI secara langsung dan melihat bagaimana perubahan kondisi lingkungan menyebabkan perubahan state NPC.

Prototype ini menjadi fondasi untuk praktikum selanjutnya ketika NPC mulai memiliki kemampuan yang lebih kompleks seperti:

\`\`\`text
Sensor
   ↓
Memory
   ↓
Decision
   ↓
Patrol
   ↓
Chase
   ↓
Last Seen Position
   ↓
Return to Patrol
\`\`\`**Konsep utama yang harus dipahami:**  
>**Game AI tidak harus dimulai dari algoritma yang kompleks. Sistem sederhana yang mampu mengamati lingkungan, mengambil keputusan berdasarkan parameter, dan memberikan respons yang jelas sudah merupakan fondasi penting dari intelligent game agent.**`,Nc=`# MODUL PRAKTIKUM 10  
# PROCEDURAL DUNGEON GENERATION DENGAN BSP DI UNITY 6

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Tools:** Unity 6 + C#  
**Materi:** PCG for Level & Dungeon Generation  
**Metode Utama:** Binary Space Partitioning (BSP)  
**Jenis Game:** 3D Top-Down Procedural Dungeon  

---

# 1. Tujuan Praktikum

Pada praktikum ini mahasiswa akan membangun sebuah **procedural dungeon generator** menggunakan teknik **Binary Space Partitioning (BSP)**.

Dungeon tidak dibuat secara manual satu per satu. Program akan:

1. membuat representasi level berbasis grid,
2. membagi area dungeon menggunakan BSP,
3. membuat room pada setiap area hasil pembagian,
4. menghubungkan room menggunakan corridor,
5. menentukan Start,
6. menentukan Goal,
7. menempatkan Enemy,
8. menempatkan Item,
9. membangun dungeon menjadi GameObject Unity,
10. menggunakan seed agar level dapat direproduksi,
11. melakukan validasi dasar dungeon.

Output akhirnya berupa dungeon 3D yang berbeda ketika seed berbeda tetapi menghasilkan dungeon yang sama ketika menggunakan seed yang sama.

---

# 2. Capaian Pembelajaran Praktikum

Setelah menyelesaikan praktikum, mahasiswa diharapkan mampu:

- memahami representasi level menggunakan grid,
- menjelaskan hubungan koordinat grid dan koordinat dunia Unity,
- memahami konsep procedural generation,
- memahami penggunaan random seed,
- menjelaskan Binary Space Partitioning,
- membuat pembagian ruang secara rekursif,
- membuat room pada leaf BSP,
- membuat corridor antar-room,
- menggunakan array dua dimensi sebagai representasi level,
- mengubah data dungeon menjadi GameObject,
- menggunakan Prefab untuk membangun level,
- menggunakan \`Instantiate()\`,
- menggunakan parent object untuk mengorganisasi object hasil generation,
- menentukan Start dan Goal,
- melakukan procedural enemy dan item placement,
- memahami konsep validasi dungeon,
- memahami hubungan PCG dengan Pathfinding dan Game AI.

---

# 3. Hasil Akhir Praktikum

Secara konseptual dungeon akan mempunyai struktur seperti:

\`\`\`text
#################################
####......######........#########
####......######........#########
####......######........#########
####.........###........#########
###########..###........#########
###########..########.###########
######.......########.###########
######.......##.............#####
######......................#####
######.......##.............#####
#################################
\`\`\`

Keterangan:

\`\`\`text
# = Wall
. = Floor
S = Start
G = Goal
E = Enemy
I = Item
\`\`\`

Di Unity, data tersebut kemudian divisualisasikan sebagai dungeon 3D menggunakan:

\`\`\`text
Floor Prefab
Wall Prefab
Player
Goal
Enemy Prefab
Item Prefab
\`\`\`

---

# 4. Mengapa Menggunakan BSP?

Materi Pertemuan 10 membahas beberapa metode:

- Grid-Based Generation
- Random Walk
- Binary Space Partitioning
- Cellular Automata
- Constraint-Based Generation

Untuk praktikum utama, metode yang direkomendasikan adalah:

## Binary Space Partitioning / BSP

BSP sangat sesuai untuk membuat:

\`\`\`text
Room → Corridor → Room → Corridor → Room
\`\`\`

dibandingkan Random Walk yang cenderung menghasilkan bentuk lebih organik.

Keuntungan BSP untuk praktikum:

1. konsep pembagian ruang terlihat jelas,
2. hasil dungeon relatif terstruktur,
3. setiap area dapat memiliki satu room,
4. room dapat dihubungkan dengan corridor,
5. konektivitas lebih mudah dikontrol,
6. Start dan Goal mudah ditempatkan,
7. mudah ditambahkan Enemy,
8. mudah ditambahkan Item,
9. kompatibel dengan konsep NavMesh,
10. mudah dikembangkan menjadi dungeon crawler.

---

# 5. Gambaran Arsitektur Sistem

Dungeon dibuat melalui pipeline:

\`\`\`text
Seed
  ↓
Initialize Grid
  ↓
Create BSP Root
  ↓
Split Area Recursively
  ↓
Find Leaf Nodes
  ↓
Generate Rooms
  ↓
Generate Corridors
  ↓
Write Rooms to Grid
  ↓
Place Start & Goal
  ↓
Build 3D Level
  ↓
Place Player
  ↓
Place Enemy & Item
  ↓
Validation
\`\`\`

Penting untuk memahami bahwa:

> PCG tidak berarti sekadar membuat sesuatu secara random.

Level tetap harus memiliki aturan agar dapat dimainkan.

---

# 6. Istilah Teknis yang Digunakan

## 6.1 GameObject

\`GameObject\` adalah objek dasar yang terdapat di dalam Scene Unity.

Contoh:

\`\`\`text
Player
Enemy
Floor
Wall
Main Camera
DungeonGenerator
\`\`\`

GameObject sendiri merupakan wadah yang dapat memiliki berbagai **Component**.

---

# 7. Component

Component memberikan fungsi pada GameObject.

Contoh:

\`\`\`text
Transform
MeshRenderer
Collider
Rigidbody
MonoBehaviour Script
NavMeshAgent
\`\`\`

Contoh sebuah Enemy:

\`\`\`text
Enemy
├── Transform
├── MeshFilter
├── MeshRenderer
├── CapsuleCollider
├── NavMeshAgent
└── EnemyAI.cs
\`\`\`

---

# 8. Transform

Setiap GameObject memiliki \`Transform\`.

Transform menyimpan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

Contoh:

\`\`\`csharp
transform.position
\`\`\`

menunjukkan posisi GameObject pada dunia Unity.

Dalam praktikum ini, posisi tile dihitung dari koordinat grid.

---

# 9. Scene

Scene adalah ruang tempat GameObject sebuah level berada.

Scene praktikum:

\`\`\`text
PCG_Dungeon_Scene
\`\`\`

Di dalam Scene terdapat:

\`\`\`text
DungeonGenerator
LevelRoot
Player
Main Camera
Directional Light
\`\`\`

---

# 10. Prefab

**Prefab** adalah template GameObject yang dapat digunakan berulang kali.

Pada praktikum ini kita membutuhkan:

\`\`\`text
FloorPrefab
WallPrefab
GoalPrefab
EnemyPrefab
ItemPrefab
\`\`\`

Keuntungan Prefab:

- bentuk objek dibuat sekali,
- dapat digunakan berkali-kali,
- dapat di-spawn dari C#,
- perubahan Prefab dapat diterapkan secara konsisten.

---

# 11. Instantiate

\`Instantiate()\` digunakan untuk membuat salinan Object/Prefab saat program berjalan.

Contoh:

\`\`\`csharp
Instantiate(floorPrefab, position, Quaternion.identity);
\`\`\`

Artinya:

\`\`\`text
buat sebuah FloorPrefab
pada position
tanpa rotasi tambahan
\`\`\`

---

# 12. Quaternion.identity

Unity menggunakan \`Quaternion\` untuk menyimpan rotasi.

\`\`\`csharp
Quaternion.identity
\`\`\`

berarti:

\`\`\`text
tidak ada rotasi tambahan
\`\`\`

Untuk praktikum ini Floor dan Wall tidak memerlukan rotasi khusus.

---

# 13. Parent dan Child

GameObject dapat mempunyai hubungan hierarchy:

\`\`\`text
LevelRoot
├── Floors
├── Walls
├── Enemies
└── Items
\`\`\`

Dengan parent, hasil procedural generation tidak memenuhi root Scene dengan ratusan GameObject yang tidak terorganisasi.

---

# 14. SerializeField

Contoh:

\`\`\`csharp
[SerializeField] private int mapWidth = 60;
\`\`\`

\`SerializeField\` memungkinkan field \`private\` tetap ditampilkan di Inspector.

Dengan demikian mahasiswa dapat mengubah parameter:

\`\`\`text
Map Width
Map Height
Minimum Leaf Size
Minimum Room Size
\`\`\`

tanpa mengubah source code.

---

# 15. Array Dua Dimensi

Dungeon disimpan menggunakan:

\`\`\`csharp
TileType[,] grid;
\`\`\`

Ini disebut **two-dimensional array**.

Bayangkan seperti tabel:

\`\`\`text
grid[x,y]
\`\`\`

Misalnya:

\`\`\`text
grid[10,15]
\`\`\`

menunjukkan tile pada koordinat:

\`\`\`text
x = 10
y = 15
\`\`\`

---

# 16. Enum

Enum digunakan ketika suatu data mempunyai sejumlah pilihan yang jelas.

Contoh:

\`\`\`csharp
public enum TileType
{
    Wall,
    Floor,
    Start,
    Goal
}
\`\`\`

Daripada menggunakan:

\`\`\`text
0
1
2
3
\`\`\`

kode menjadi jauh lebih mudah dibaca.

---

# 17. Vector2Int

\`Vector2Int\` menyimpan dua angka integer.

Contoh:

\`\`\`csharp
Vector2Int position = new Vector2Int(10, 20);
\`\`\`

Sangat cocok untuk koordinat grid karena:

\`\`\`text
x dan y berupa bilangan bulat
\`\`\`

---

# 18. Vector3

Posisi GameObject Unity 3D menggunakan:

\`\`\`csharp
Vector3
\`\`\`

yang memiliki:

\`\`\`text
x
y
z
\`\`\`

Dungeon kita menggunakan:

\`\`\`text
X = horizontal
Y = tinggi
Z = grid Y
\`\`\`

Sehingga:

\`\`\`text
grid (x,y)
\`\`\`

diubah menjadi:

\`\`\`text
world (x,0,z)
\`\`\`

---

# 19. Seed

Seed adalah nilai awal generator angka pseudo-random.

Contoh:

\`\`\`text
seed = 12345
\`\`\`

Seed memungkinkan procedural generation menjadi **reproducible**:

\`\`\`text
Seed sama
→ urutan random sama
→ dungeon sama
\`\`\`

Sedangkan:

\`\`\`text
Seed berbeda
→ dungeon berbeda
\`\`\`

Ini sangat penting untuk debugging PCG.

---

# 20. Recursive Function

BSP menggunakan **rekursi**.

Recursive function adalah fungsi yang memanggil dirinya sendiri.

Contoh konsep:

\`\`\`text
Split(area)
    ↓
Split(area kiri)
    ↓
Split(area kiri lagi)
\`\`\`

Rekursi berhenti setelah ukuran area terlalu kecil untuk dibagi kembali.

---

# 21. Leaf pada BSP

BSP membentuk tree.

Contoh:

\`\`\`text
Root
├── Left
│   ├── Leaf
│   └── Leaf
└── Right
    ├── Leaf
    └── Leaf
\`\`\`

\`Leaf\` berarti node yang tidak dibagi lagi.

Room akan dibuat pada setiap Leaf.

---

# 22. Corridor

Corridor adalah jalur yang menghubungkan dua room.

Praktikum menggunakan:

\`\`\`text
L-Shaped Corridor
\`\`\`

Contoh:

\`\`\`text
Room A ──────┐
             │
             │
             └──── Room B
\`\`\`

---

# 23. Persiapan Project Unity

Gunakan:

\`\`\`text
Unity 6
\`\`\`

Buat project:

\`\`\`text
Universal 3D
\`\`\`

atau template 3D lain yang tersedia.

---

# 24. Nama Project yang Direkomendasikan

## Nama utama

\`\`\`text
PCG_BSP_Dungeon
\`\`\`

Nama ini paling direkomendasikan karena singkat dan menjelaskan:

\`\`\`text
PCG = Procedural Content Generation
BSP = algoritma yang digunakan
Dungeon = hasil project
\`\`\`

Alternatif:

\`\`\`text
ProceduralDungeon_BSP
SmartDungeonGenerator
PCG_DungeonLab
ProceduralDungeonAI
GameCerdas_PCGDungeon
\`\`\`

Untuk konsistensi praktikum kuliah saya merekomendasikan:

\`\`\`text
GameCerdas_P10_PCG_BSP_Dungeon
\`\`\`

---

# 25. Membuat Project

Di Unity Hub:

\`\`\`text
New Project
\`\`\`

Pilih:

\`\`\`text
Universal 3D
\`\`\`

Project Name:

\`\`\`text
GameCerdas_P10_PCG_BSP_Dungeon
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 26. Struktur Folder Project

Buat struktur:

\`\`\`text
Assets/
│
├── Scenes/
│   └── PCG_Dungeon_Scene.unity
│
├── Scripts/
│   ├── PCG/
│   │   ├── TileType.cs
│   │   ├── DungeonRoom.cs
│   │   ├── BSPNode.cs
│   │   └── DungeonGenerator.cs
│   │
│   ├── Player/
│   │   └── SimplePlayerController.cs
│   │
│   └── Enemy/
│       └── SimpleEnemy.cs
│
├── Prefabs/
│   ├── Dungeon/
│   │   ├── FloorPrefab.prefab
│   │   ├── WallPrefab.prefab
│   │   └── GoalPrefab.prefab
│   │
│   ├── Characters/
│   │   ├── PlayerPrefab.prefab
│   │   └── EnemyPrefab.prefab
│   │
│   └── Items/
│       └── ItemPrefab.prefab
│
└── Materials/
    ├── FloorMaterial.mat
    ├── WallMaterial.mat
    ├── GoalMaterial.mat
    ├── EnemyMaterial.mat
    └── ItemMaterial.mat
\`\`\`

---

# 27. Membuat Scene

Simpan Scene sebagai:

\`\`\`text
PCG_Dungeon_Scene
\`\`\`

Hierarchy awal:

\`\`\`text
PCG_Dungeon_Scene
├── Main Camera
├── Directional Light
├── DungeonGenerator
└── LevelRoot
\`\`\`

---

# 28. Membuat Floor Prefab

Pilih:

\`\`\`text
GameObject
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
FloorPrefab
\`\`\`

Transform:

\`\`\`text
Position = (0, 0, 0)
Scale    = (1, 0.1, 1)
\`\`\`

Cube sudah memiliki BoxCollider sehingga nantinya dapat diinjak Player.

Tambahkan material Floor.

Drag ke:

\`\`\`text
Assets/Prefabs/Dungeon/
\`\`\`

Setelah menjadi Prefab, hapus instance dari Scene.

---

# 29. Membuat Wall Prefab

Buat Cube.

Rename:

\`\`\`text
WallPrefab
\`\`\`

Transform:

\`\`\`text
Scale = (1, 1, 1)
\`\`\`

Material:

\`\`\`text
WallMaterial
\`\`\`

Drag ke:

\`\`\`text
Assets/Prefabs/Dungeon/
\`\`\`

---

# 30. Membuat Goal Prefab

Buat:

\`\`\`text
3D Object → Cylinder
\`\`\`

Rename:

\`\`\`text
GoalPrefab
\`\`\`

Gunakan scale kecil, misalnya:

\`\`\`text
(0.4, 0.1, 0.4)
\`\`\`

Letakkan visual Goal nantinya sedikit di atas Floor.

Gunakan material berbeda agar mudah terlihat.

---

# 31. Membuat TileType.cs

Buat:

\`\`\`text
Assets/Scripts/PCG/TileType.cs
\`\`\`

Isi:

\`\`\`csharp
public enum TileType
{
    Wall,
    Floor,
    Start,
    Goal
}
\`\`\`

## Penjelasan

\`Wall\`

\`\`\`text
cell tidak dapat dilewati
\`\`\`

\`Floor\`

\`\`\`text
cell dapat dilewati
\`\`\`

\`Start\`

\`\`\`text
posisi awal player
\`\`\`

\`Goal\`

\`\`\`text
tujuan dungeon
\`\`\`

---

# 32. Membuat DungeonRoom.cs

Buat:

\`\`\`text
DungeonRoom.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

[System.Serializable]
public class DungeonRoom
{
    public int x;
    public int y;
    public int width;
    public int height;

    public DungeonRoom(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public Vector2Int Center
    {
        get
        {
            return new Vector2Int(
                x + width / 2,
                y + height / 2
            );
        }
    }
}
\`\`\`

---

# 33. Penjelasan DungeonRoom

Class ini menyimpan sebuah rectangle Room.

Properti:

\`\`\`text
x
y
width
height
\`\`\`

Contoh:

\`\`\`text
x      = 10
y      = 5
width  = 8
height = 6
\`\`\`

berarti room berada dari:

\`\`\`text
x = 10 sampai 17
y = 5 sampai 10
\`\`\`

---

# 34. Property Center

Bagian:

\`\`\`csharp
public Vector2Int Center
\`\`\`

digunakan untuk mendapatkan titik tengah room.

Titik tengah sangat penting untuk:

- corridor,
- Start,
- Goal,
- enemy placement,
- item placement.

---

# 35. Membuat BSPNode.cs

Buat:

\`\`\`text
BSPNode.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

public class BSPNode
{
    public int x;
    public int y;
    public int width;
    public int height;

    public BSPNode left;
    public BSPNode right;

    public DungeonRoom room;

    public BSPNode(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public bool IsLeaf
    {
        get
        {
            return left == null && right == null;
        }
    }
}
\`\`\`

---

# 36. Struktur BSP

Satu \`BSPNode\` mewakili area rectangular.

Misalnya:

\`\`\`text
Root
x = 1
y = 1
width = 58
height = 38
\`\`\`

Kemudian Root dibagi:

\`\`\`text
Root
├── left
└── right
\`\`\`

Masing-masing dapat dibagi lagi.

---

# 37. Membuat DungeonGenerator.cs

Buat:

\`\`\`text
DungeonGenerator.cs
\`\`\`

Script ini menjadi controller utama procedural dungeon.

---

# 38. Header Script DungeonGenerator

Masukkan:

\`\`\`csharp
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
\`\`\`

---

# 39. Parameter Dungeon

Tambahkan:

\`\`\`csharp
[Header("Dungeon Size")]
[SerializeField] private int mapWidth = 60;
[SerializeField] private int mapHeight = 40;
[SerializeField] private float tileSize = 1f;

[Header("BSP Settings")]
[SerializeField] private int minLeafSize = 12;
[SerializeField] private int maxLeafSize = 20;

[Header("Room Settings")]
[SerializeField] private int minRoomSize = 5;
[SerializeField] private int roomMargin = 2;

[Header("Generation")]
[SerializeField] private int seed = 12345;
[SerializeField] private bool randomSeed = false;
\`\`\`

---

# 40. Penjelasan Parameter

## mapWidth

Lebar dungeon.

\`\`\`text
60 cell
\`\`\`

## mapHeight

Tinggi dungeon.

\`\`\`text
40 cell
\`\`\`

## tileSize

Ukuran setiap cell di dunia Unity.

\`\`\`text
1 unit
\`\`\`

## minLeafSize

Ukuran minimum sebuah area BSP sebelum tidak boleh dibagi.

## maxLeafSize

Jika area lebih besar dari nilai ini, area sebaiknya dibagi kembali.

## minRoomSize

Ukuran minimum room.

## roomMargin

Jarak antara room dan batas Leaf.

---

# 41. Referensi Prefab

Tambahkan:

\`\`\`csharp
[Header("Prefabs")]
[SerializeField] private GameObject floorPrefab;
[SerializeField] private GameObject wallPrefab;
[SerializeField] private GameObject goalPrefab;
[SerializeField] private GameObject playerPrefab;
[SerializeField] private GameObject enemyPrefab;
[SerializeField] private GameObject itemPrefab;
\`\`\`

---

# 42. Spawn Settings

Tambahkan:

\`\`\`csharp
[Header("Spawn Settings")]
[SerializeField] private int enemyCount = 8;
[SerializeField] private int itemCount = 5;
[SerializeField] private int safeRoomCount = 1;
\`\`\`

\`safeRoomCount\` digunakan agar room awal tidak langsung berisi Enemy.

---

# 43. Internal Data

Tambahkan:

\`\`\`csharp
private TileType[,] grid;

private BSPNode rootNode;

private List<BSPNode> leafNodes = new List<BSPNode>();
private List<DungeonRoom> rooms = new List<DungeonRoom>();

private Transform levelRoot;

private Vector2Int startPosition;
private Vector2Int goalPosition;
\`\`\`

---

# 44. Fungsi Start

Tambahkan:

\`\`\`csharp
private void Start()
{
    GenerateDungeon();
}
\`\`\`

Dengan demikian dungeon langsung dihasilkan ketika tombol Play ditekan.

---

# 45. Fungsi GenerateDungeon

Tambahkan:

\`\`\`csharp
public void GenerateDungeon()
{
    ClearDungeon();

    SetupSeed();

    InitializeGrid();

    CreateBSP();

    CreateRooms();

    CreateCorridors();

    PlaceStartAndGoal();

    BuildDungeon();

    SpawnGameplayObjects();

    Debug.Log("Dungeon generated with seed: " + seed);
}
\`\`\`

Ini merupakan pipeline utama generator.

---

# 46. Setup Seed

Tambahkan:

\`\`\`csharp
private void SetupSeed()
{
    if (randomSeed)
    {
        seed = System.DateTime.Now.GetHashCode();
    }

    Random.InitState(seed);
}
\`\`\`

Jika:

\`\`\`text
randomSeed = false
\`\`\`

maka nilai Inspector dipakai.

Jika:

\`\`\`text
randomSeed = true
\`\`\`

program membuat seed baru.

---

# 47. Initialize Grid

Tambahkan:

\`\`\`csharp
private void InitializeGrid()
{
    grid = new TileType[mapWidth, mapHeight];

    for (int x = 0; x < mapWidth; x++)
    {
        for (int y = 0; y < mapHeight; y++)
        {
            grid[x, y] = TileType.Wall;
        }
    }
}
\`\`\`

Dungeon menggunakan pendekatan:

\`\`\`text
Wall-First
\`\`\`

Awalnya seluruh map adalah Wall.

Kemudian algoritma "mengukir" Floor.

---

# 48. Create BSP

Tambahkan:

\`\`\`csharp
private void CreateBSP()
{
    rootNode = new BSPNode(
        1,
        1,
        mapWidth - 2,
        mapHeight - 2
    );

    leafNodes.Clear();

    SplitNode(rootNode);
}
\`\`\`

Area dungeon tidak dimulai tepat pada koordinat 0 karena kita menyisakan boundary Wall.

---

# 49. SplitNode

Tambahkan:

\`\`\`csharp
private void SplitNode(BSPNode node)
{
    if (node.width <= maxLeafSize &&
        node.height <= maxLeafSize)
    {
        leafNodes.Add(node);
        return;
    }

    bool splitHorizontal;

    if (node.width > node.height * 1.25f)
    {
        splitHorizontal = false;
    }
    else if (node.height > node.width * 1.25f)
    {
        splitHorizontal = true;
    }
    else
    {
        splitHorizontal = Random.value > 0.5f;
    }

    if (splitHorizontal)
    {
        SplitHorizontal(node);
    }
    else
    {
        SplitVertical(node);
    }
}
\`\`\`

---

# 50. Mengapa Menggunakan Rasio?

Jika area terlalu lebar:

\`\`\`text
████████████████████
\`\`\`

lebih baik dipotong vertical.

Jika area terlalu tinggi:

\`\`\`text
████
████
████
████
████
\`\`\`

lebih baik dipotong horizontal.

Hal ini mencegah terbentuknya Leaf yang terlalu panjang.

---

# 51. Split Horizontal

Tambahkan:

\`\`\`csharp
private void SplitHorizontal(BSPNode node)
{
    if (node.height < minLeafSize * 2)
    {
        leafNodes.Add(node);
        return;
    }

    int split = Random.Range(
        minLeafSize,
        node.height - minLeafSize
    );

    node.left = new BSPNode(
        node.x,
        node.y,
        node.width,
        split
    );

    node.right = new BSPNode(
        node.x,
        node.y + split,
        node.width,
        node.height - split
    );

    SplitNode(node.left);
    SplitNode(node.right);
}
\`\`\`

---

# 52. Split Vertical

Tambahkan:

\`\`\`csharp
private void SplitVertical(BSPNode node)
{
    if (node.width < minLeafSize * 2)
    {
        leafNodes.Add(node);
        return;
    }

    int split = Random.Range(
        minLeafSize,
        node.width - minLeafSize
    );

    node.left = new BSPNode(
        node.x,
        node.y,
        split,
        node.height
    );

    node.right = new BSPNode(
        node.x + split,
        node.y,
        node.width - split,
        node.height
    );

    SplitNode(node.left);
    SplitNode(node.right);
}
\`\`\`

---

# 53. Visualisasi BSP

Misalnya area awal:

\`\`\`text
+--------------------------------+
|                                |
|                                |
|                                |
|                                |
+--------------------------------+
\`\`\`

Split:

\`\`\`text
+---------------+----------------+
|               |                |
|               |                |
|               |                |
|               |                |
+---------------+----------------+
\`\`\`

Split lagi:

\`\`\`text
+-------+-------+----------------+
|       |       |                |
|       |       +---------+------+
|       |       |         |      |
|       |       |         |      |
+-------+-------+---------+------+
\`\`\`

Setiap bagian akhir adalah Leaf.

---

# 54. CreateRooms

Tambahkan:

\`\`\`csharp
private void CreateRooms()
{
    rooms.Clear();

    foreach (BSPNode leaf in leafNodes)
    {
        int maxWidth = leaf.width - roomMargin * 2;
        int maxHeight = leaf.height - roomMargin * 2;

        if (maxWidth < minRoomSize ||
            maxHeight < minRoomSize)
        {
            continue;
        }

        int roomWidth = Random.Range(
            minRoomSize,
            maxWidth + 1
        );

        int roomHeight = Random.Range(
            minRoomSize,
            maxHeight + 1
        );

        int maxXOffset =
            leaf.width - roomWidth - roomMargin;

        int maxYOffset =
            leaf.height - roomHeight - roomMargin;

        int roomX = leaf.x +
                    Random.Range(
                        roomMargin,
                        maxXOffset + 1
                    );

        int roomY = leaf.y +
                    Random.Range(
                        roomMargin,
                        maxYOffset + 1
                    );

        DungeonRoom room =
            new DungeonRoom(
                roomX,
                roomY,
                roomWidth,
                roomHeight
            );

        leaf.room = room;

        rooms.Add(room);

        CarveRoom(room);
    }
}
\`\`\`

---

# 55. CarveRoom

Tambahkan:

\`\`\`csharp
private void CarveRoom(DungeonRoom room)
{
    for (int x = room.x;
         x < room.x + room.width;
         x++)
    {
        for (int y = room.y;
             y < room.y + room.height;
             y++)
        {
            if (IsInsideGrid(x, y))
            {
                grid[x, y] = TileType.Floor;
            }
        }
    }
}
\`\`\`

Room mengubah:

\`\`\`text
Wall
\`\`\`

menjadi:

\`\`\`text
Floor
\`\`\`

---

# 56. IsInsideGrid

Tambahkan utility:

\`\`\`csharp
private bool IsInsideGrid(int x, int y)
{
    return x >= 0 &&
           x < mapWidth &&
           y >= 0 &&
           y < mapHeight;
}
\`\`\`

Fungsi ini penting untuk menghindari:

\`\`\`text
IndexOutOfRangeException
\`\`\`

---

# 57. Membuat Corridor

Setelah room selesai dibuat, room masih terpisah.

Contoh:

\`\`\`text
Room A


              Room B


      Room C
\`\`\`

Kita harus menghubungkannya.

Untuk praktikum dasar kita menghubungkan room berdasarkan urutan list.

---

# 58. CreateCorridors

Tambahkan:

\`\`\`csharp
private void CreateCorridors()
{
    if (rooms.Count < 2)
        return;

    for (int i = 0; i < rooms.Count - 1; i++)
    {
        Vector2Int from = rooms[i].Center;
        Vector2Int to = rooms[i + 1].Center;

        CreateLCorridor(from, to);
    }
}
\`\`\`

---

# 59. Create L Corridor

Tambahkan:

\`\`\`csharp
private void CreateLCorridor(
    Vector2Int from,
    Vector2Int to)
{
    bool horizontalFirst =
        Random.value > 0.5f;

    if (horizontalFirst)
    {
        CarveHorizontal(
            from.x,
            to.x,
            from.y
        );

        CarveVertical(
            from.y,
            to.y,
            to.x
        );
    }
    else
    {
        CarveVertical(
            from.y,
            to.y,
            from.x
        );

        CarveHorizontal(
            from.x,
            to.x,
            to.y
        );
    }
}
\`\`\`

---

# 60. Corridor Horizontal

Tambahkan:

\`\`\`csharp
private void CarveHorizontal(
    int x1,
    int x2,
    int y)
{
    int start = Mathf.Min(x1, x2);
    int end = Mathf.Max(x1, x2);

    for (int x = start; x <= end; x++)
    {
        if (IsInsideGrid(x, y))
        {
            grid[x, y] = TileType.Floor;
        }
    }
}
\`\`\`

---

# 61. Corridor Vertical

Tambahkan:

\`\`\`csharp
private void CarveVertical(
    int y1,
    int y2,
    int x)
{
    int start = Mathf.Min(y1, y2);
    int end = Mathf.Max(y1, y2);

    for (int y = start; y <= end; y++)
    {
        if (IsInsideGrid(x, y))
        {
            grid[x, y] = TileType.Floor;
        }
    }
}
\`\`\`

---

# 62. Mengapa Corridor Berbentuk L?

Misalnya:

\`\`\`text
A = (5,5)
B = (12,10)
\`\`\`

Jalur:

\`\`\`text
(5,5)
→ (12,5)
→ (12,10)
\`\`\`

membentuk:

\`\`\`text
A -------+
         |
         |
         B
\`\`\`

Keuntungannya:

- mudah dibuat,
- selalu menghubungkan dua titik,
- tetap mengikuti grid,
- mudah dipahami mahasiswa.

---

# 63. Start dan Goal

Untuk versi awal:

\`\`\`text
Start = center room pertama
Goal = center room terakhir
\`\`\`

Tambahkan:

\`\`\`csharp
private void PlaceStartAndGoal()
{
    if (rooms.Count == 0)
        return;

    startPosition = rooms[0].Center;
    goalPosition = rooms[rooms.Count - 1].Center;

    grid[startPosition.x, startPosition.y]
        = TileType.Start;

    grid[goalPosition.x, goalPosition.y]
        = TileType.Goal;
}
\`\`\`

---

# 64. Versi Start–Goal yang Lebih Baik

Room terakhir belum tentu room terjauh.

Versi yang lebih baik adalah mencari:

\`\`\`text
room yang paling jauh dari Start
\`\`\`

Tambahkan:

\`\`\`csharp
private DungeonRoom FindFarthestRoom(
    Vector2Int position)
{
    DungeonRoom farthest = rooms[0];
    float farthestDistance = 0f;

    foreach (DungeonRoom room in rooms)
    {
        float distance =
            Vector2Int.Distance(
                position,
                room.Center
            );

        if (distance > farthestDistance)
        {
            farthestDistance = distance;
            farthest = room;
        }
    }

    return farthest;
}
\`\`\`

Lalu ubah:

\`\`\`csharp
private void PlaceStartAndGoal()
{
    if (rooms.Count == 0)
        return;

    startPosition = rooms[0].Center;

    DungeonRoom farthestRoom =
        FindFarthestRoom(startPosition);

    goalPosition = farthestRoom.Center;

    grid[startPosition.x, startPosition.y]
        = TileType.Start;

    grid[goalPosition.x, goalPosition.y]
        = TileType.Goal;
}
\`\`\`

Ini lebih direkomendasikan.

---

# 65. Mengubah Grid menjadi Scene Unity

Sekarang data seperti:

\`\`\`text
Wall
Floor
Floor
Wall
Goal
\`\`\`

belum mempunyai visual.

Kita perlu:

\`\`\`text
Grid Data
↓
GameObject
\`\`\`

---

# 66. Membuat LevelRoot

Tambahkan:

\`\`\`csharp
private void CreateLevelRoot()
{
    GameObject root =
        new GameObject("GeneratedLevel");

    levelRoot = root.transform;
}
\`\`\`

---

# 67. BuildDungeon

Tambahkan:

\`\`\`csharp
private void BuildDungeon()
{
    CreateLevelRoot();

    for (int x = 0; x < mapWidth; x++)
    {
        for (int y = 0; y < mapHeight; y++)
        {
            Vector3 worldPosition =
                GridToWorld(x, y);

            switch (grid[x, y])
            {
                case TileType.Floor:
                case TileType.Start:
                case TileType.Goal:

                    Instantiate(
                        floorPrefab,
                        worldPosition,
                        Quaternion.identity,
                        levelRoot
                    );

                    break;
            }

            if (grid[x, y] == TileType.Wall &&
                IsWallVisible(x, y))
            {
                Instantiate(
                    wallPrefab,
                    worldPosition +
                    Vector3.up * 0.5f,
                    Quaternion.identity,
                    levelRoot
                );
            }

            if (grid[x, y] == TileType.Goal)
            {
                Instantiate(
                    goalPrefab,
                    worldPosition +
                    Vector3.up * 0.15f,
                    Quaternion.identity,
                    levelRoot
                );
            }
        }
    }
}
\`\`\`

---

# 68. GridToWorld

Tambahkan:

\`\`\`csharp
private Vector3 GridToWorld(int x, int y)
{
    return new Vector3(
        x * tileSize,
        0f,
        y * tileSize
    );
}
\`\`\`

Ini merupakan salah satu konsep Unity terpenting dalam praktikum.

Grid:

\`\`\`text
(x,y)
\`\`\`

diubah ke World:

\`\`\`text
(x,0,z)
\`\`\`

---

# 69. Optimasi Wall

Tidak semua Wall perlu dibuat sebagai GameObject.

Wall yang sangat jauh dari Floor tidak akan terlihat.

Tambahkan:

\`\`\`csharp
private bool IsWallVisible(int x, int y)
{
    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (!IsInsideGrid(nx, ny))
                continue;

            if (grid[nx, ny] != TileType.Wall)
                return true;
        }
    }

    return false;
}
\`\`\`

Dengan ini hanya Wall yang berbatasan dengan area dungeon yang di-render.

---

# 70. Mengapa Optimasi Ini Penting?

Misalnya map:

\`\`\`text
60 × 40 = 2400 cell
\`\`\`

Jika semua Wall dibuat sebagai Cube, jumlah GameObject bisa menjadi sangat banyak.

Padahal Wall di area tertutup tidak terlihat dan tidak memiliki fungsi gameplay.

Prinsip penting PCG:

> Jangan hanya memikirkan generation algorithm. Perhatikan juga biaya visualisasi hasil generation.

---

# 71. Spawn Gameplay Objects

Tambahkan:

\`\`\`csharp
private void SpawnGameplayObjects()
{
    SpawnPlayer();
    SpawnEnemies();
    SpawnItems();
}
\`\`\`

---

# 72. Membuat Player

Buat:

\`\`\`text
GameObject → 3D Object → Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Tambahkan:

\`\`\`text
CharacterController
\`\`\`

Buat Prefab:

\`\`\`text
PlayerPrefab
\`\`\`

---

# 73. SimplePlayerController

Buat:

\`\`\`text
SimplePlayerController.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 move =
            new Vector3(
                horizontal,
                0f,
                vertical
            ).normalized;

        controller.Move(
            move *
            moveSpeed *
            Time.deltaTime
        );

        if (controller.isGrounded &&
            velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );
    }
}
\`\`\`

---

# 74. Catatan Input Unity

Jika project menggunakan konfigurasi Input lama / compatibility input, script tersebut dapat menggunakan:

\`\`\`text
Horizontal
Vertical
\`\`\`

dengan kontrol default:

\`\`\`text
W / S
A / D
Arrow Keys
\`\`\`

Apabila project hanya menggunakan **Input System** baru, implementasi input perlu disesuaikan.

Untuk fokus Praktikum PCG, penggunaan input sederhana sudah mencukupi.

---

# 75. Spawn Player

Kembali ke \`DungeonGenerator.cs\`.

Tambahkan:

\`\`\`csharp
private void SpawnPlayer()
{
    if (playerPrefab == null)
        return;

    Vector3 position =
        GridToWorld(
            startPosition.x,
            startPosition.y
        );

    Instantiate(
        playerPrefab,
        position + Vector3.up * 1f,
        Quaternion.identity
    );
}
\`\`\`

Player muncul pada Start Room.

---

# 76. Membuat Enemy Prefab

Buat:

\`\`\`text
Capsule
\`\`\`

Rename:

\`\`\`text
EnemyPrefab
\`\`\`

Gunakan material berbeda.

Untuk Praktikum 10, Enemy belum harus memiliki AI kompleks.

Tujuan utama:

\`\`\`text
procedural placement
\`\`\`

---

# 77. SimpleEnemy

Opsional, buat:

\`\`\`csharp
using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 60f;

    private void Update()
    {
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime
        );
    }
}
\`\`\`

Ini hanya agar Enemy mudah dikenali.

Mahasiswa dapat menggantinya dengan NPC AI dari praktikum sebelumnya.

---

# 78. Random Floor Position

Enemy harus spawn pada:

\`\`\`text
Floor
\`\`\`

bukan Wall.

Tambahkan:

\`\`\`csharp
private Vector2Int GetRandomFloorPosition()
{
    for (int attempt = 0;
         attempt < 100;
         attempt++)
    {
        int x = Random.Range(1, mapWidth - 1);
        int y = Random.Range(1, mapHeight - 1);

        if (grid[x, y] == TileType.Floor)
        {
            return new Vector2Int(x, y);
        }
    }

    return startPosition;
}
\`\`\`

---

# 79. Spawn Enemy

Tambahkan:

\`\`\`csharp
private void SpawnEnemies()
{
    if (enemyPrefab == null)
        return;

    for (int i = 0; i < enemyCount; i++)
    {
        Vector2Int gridPosition =
            GetRandomFloorPosition();

        float distanceFromStart =
            Vector2Int.Distance(
                gridPosition,
                startPosition
            );

        if (distanceFromStart < 8f)
        {
            i--;
            continue;
        }

        Vector3 worldPosition =
            GridToWorld(
                gridPosition.x,
                gridPosition.y
            );

        GameObject enemy =
            Instantiate(
                enemyPrefab,
                worldPosition +
                Vector3.up * 1f,
                Quaternion.identity,
                levelRoot
            );

        enemy.name = "Enemy_" + i;
    }
}
\`\`\`

---

# 80. Constraint pada Enemy

Perhatikan:

\`\`\`csharp
if (distanceFromStart < 8f)
\`\`\`

Ini merupakan contoh sederhana:

## Constraint-Based PCG

Aturannya:

\`\`\`text
Enemy tidak boleh terlalu dekat dengan Start.
\`\`\`

Random menentukan lokasi kandidat.

Constraint menentukan:

\`\`\`text
lokasi tersebut boleh dipakai atau tidak.
\`\`\`

---

# 81. Membuat Item Prefab

Buat:

\`\`\`text
3D Object → Sphere
\`\`\`

Rename:

\`\`\`text
ItemPrefab
\`\`\`

Scale:

\`\`\`text
0.3
0.3
0.3
\`\`\`

Tambahkan material.

Buat Prefab.

---

# 82. Spawn Item

Tambahkan:

\`\`\`csharp
private void SpawnItems()
{
    if (itemPrefab == null)
        return;

    for (int i = 0; i < itemCount; i++)
    {
        Vector2Int gridPosition =
            GetRandomFloorPosition();

        Vector3 worldPosition =
            GridToWorld(
                gridPosition.x,
                gridPosition.y
            );

        GameObject item =
            Instantiate(
                itemPrefab,
                worldPosition +
                Vector3.up * 0.5f,
                Quaternion.identity,
                levelRoot
            );

        item.name = "Item_" + i;
    }
}
\`\`\`

---

# 83. Clear Dungeon

Agar generator dapat dijalankan kembali, dungeon lama perlu dihapus.

Tambahkan:

\`\`\`csharp
private void ClearDungeon()
{
    GameObject existing =
        GameObject.Find("GeneratedLevel");

    if (existing != null)
    {
        Destroy(existing);
    }

    GameObject oldPlayer =
        GameObject.FindWithTag("Player");

    if (oldPlayer != null)
    {
        Destroy(oldPlayer);
    }
}
\`\`\`

PlayerPrefab sebaiknya diberi:

\`\`\`text
Tag = Player
\`\`\`

---

# 84. Menyiapkan DungeonGenerator di Inspector

Pilih:

\`\`\`text
DungeonGenerator
\`\`\`

Tambahkan component:

\`\`\`text
DungeonGenerator.cs
\`\`\`

Isi Inspector:

\`\`\`text
Dungeon Size
    Map Width       = 60
    Map Height      = 40
    Tile Size       = 1

BSP Settings
    Min Leaf Size   = 12
    Max Leaf Size   = 20

Room Settings
    Min Room Size   = 5
    Room Margin     = 2

Generation
    Seed            = 12345
    Random Seed     = false

Spawn Settings
    Enemy Count     = 8
    Item Count      = 5
\`\`\`

Drag Prefab:

\`\`\`text
Floor Prefab → Floor Prefab
Wall Prefab  → Wall Prefab
Goal Prefab  → Goal Prefab
Player       → Player Prefab
Enemy        → Enemy Prefab
Item         → Item Prefab
\`\`\`

---

# 85. Mengatur Kamera

Untuk top-down view, letakkan Main Camera kira-kira:

\`\`\`text
Position:
X = mapWidth / 2
Y = 45
Z = mapHeight / 2
\`\`\`

Rotation:

\`\`\`text
X = 90
Y = 0
Z = 0
\`\`\`

Jika dungeon terlalu besar, tingkatkan nilai Y.

---

# 86. Pengujian Pertama

Tekan:

\`\`\`text
Play
\`\`\`

Dungeon seharusnya menghasilkan:

\`\`\`text
beberapa room
+
corridor
+
wall
+
player
+
goal
+
enemy
+
item
\`\`\`

Periksa Console.

Output:

\`\`\`text
Dungeon generated with seed: 12345
\`\`\`

---

# 87. Pengujian Seed

Gunakan:

\`\`\`text
Random Seed = false
Seed = 12345
\`\`\`

Play.

Catat bentuk dungeon.

Stop.

Play lagi.

Dungeon seharusnya menghasilkan konfigurasi prosedural yang konsisten selama seluruh random sequence dan parameter generator juga sama.

Kemudian ubah:

\`\`\`text
Seed = 999
\`\`\`

Dungeon akan berubah.

---

# 88. Mengapa Seed Sangat Penting?

Bayangkan mahasiswa menemukan bug:

\`\`\`text
Player terjebak
\`\`\`

Jika tidak menggunakan seed:

\`\`\`text
Run berikutnya dungeon sudah berubah.
\`\`\`

Bug sulit direproduksi.

Dengan seed:

\`\`\`text
Bug ditemukan pada Seed 64521.
\`\`\`

Developer dapat menggunakan:

\`\`\`text
Seed 64521
\`\`\`

dan melihat layout yang sama kembali.

---

# 89. Debug Visualization dengan Gizmos

Tambahkan ke \`DungeonGenerator.cs\`:

\`\`\`csharp
private void OnDrawGizmos()
{
    if (rooms == null)
        return;

    Gizmos.color = Color.yellow;

    foreach (DungeonRoom room in rooms)
    {
        Vector3 center =
            new Vector3(
                (room.x + room.width / 2f) * tileSize,
                0.2f,
                (room.y + room.height / 2f) * tileSize
            );

        Vector3 size =
            new Vector3(
                room.width * tileSize,
                0.2f,
                room.height * tileSize
            );

        Gizmos.DrawWireCube(
            center,
            size
        );
    }
}
\`\`\`

---

# 90. Apa Itu Gizmos?

Gizmos adalah visual debugging di Unity Scene View.

Gizmos tidak menjadi bagian utama visual game.

Contoh kegunaan:

- melihat detection radius,
- melihat waypoint,
- melihat room boundary,
- melihat corridor,
- melihat sensor AI,
- melihat spawn point.

Pada praktikum ini:

\`\`\`text
garis kuning = batas Room BSP
\`\`\`

---

# 91. Validasi Dungeon

Dungeon procedural sebaiknya tidak langsung dianggap valid hanya karena terlihat benar.

Beberapa **hard constraint**:

\`\`\`text
Room minimal satu.
Start tersedia.
Goal tersedia.
Start berada pada floor.
Goal berada pada floor.
Start dan Goal terhubung.
\`\`\`

Beberapa **soft constraint**:

\`\`\`text
Start dan Goal cukup jauh.
Jumlah Enemy tidak terlalu banyak.
Item tersebar.
Room tidak terlalu kecil.
Dungeon tidak terlalu linear.
\`\`\`

---

# 92. Validasi Sederhana

Tambahkan:

\`\`\`csharp
private bool ValidateDungeon()
{
    if (rooms == null || rooms.Count < 2)
    {
        Debug.LogWarning(
            "Dungeon invalid: jumlah room terlalu sedikit."
        );

        return false;
    }

    if (!IsInsideGrid(
            startPosition.x,
            startPosition.y))
    {
        return false;
    }

    if (!IsInsideGrid(
            goalPosition.x,
            goalPosition.y))
    {
        return false;
    }

    return true;
}
\`\`\`

---

# 93. Validasi Konektivitas dengan BFS

Versi lebih baik menggunakan:

## Breadth-First Search

Tujuan:

\`\`\`text
Mulai dari Start
↓
jelajahi seluruh Floor yang terhubung
↓
apakah Goal ditemukan?
\`\`\`

Tambahkan:

\`\`\`csharp
private bool HasPathToGoal()
{
    bool[,] visited =
        new bool[mapWidth, mapHeight];

    Queue<Vector2Int> queue =
        new Queue<Vector2Int>();

    queue.Enqueue(startPosition);

    visited[
        startPosition.x,
        startPosition.y
    ] = true;

    Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    while (queue.Count > 0)
    {
        Vector2Int current =
            queue.Dequeue();

        if (current == goalPosition)
        {
            return true;
        }

        foreach (Vector2Int direction
                 in directions)
        {
            Vector2Int next =
                current + direction;

            if (!IsInsideGrid(
                    next.x,
                    next.y))
            {
                continue;
            }

            if (visited[
                    next.x,
                    next.y])
            {
                continue;
            }

            if (grid[
                    next.x,
                    next.y]
                == TileType.Wall)
            {
                continue;
            }

            visited[
                next.x,
                next.y
            ] = true;

            queue.Enqueue(next);
        }
    }

    return false;
}
\`\`\`

---

# 94. Mengintegrasikan Validasi

Setelah:

\`\`\`csharp
PlaceStartAndGoal();
\`\`\`

tambahkan:

\`\`\`csharp
if (!HasPathToGoal())
{
    Debug.LogWarning(
        "Dungeon tidak valid: Goal tidak dapat dicapai."
    );

    return;
}
\`\`\`

Secara ideal pipeline menjadi:

\`\`\`text
Generate
↓
Validate
↓
Build
\`\`\`

bukan:

\`\`\`text
Generate
↓
Build
↓
baru tahu salah
\`\`\`

---

# 95. Hubungan dengan Materi Pathfinding

BFS pada praktikum ini bukan dipakai agar NPC bergerak.

BFS dipakai sebagai:

\`\`\`text
PCG Validation Tool
\`\`\`

Ini menunjukkan bahwa algoritma pathfinding dapat memiliki fungsi lain.

Materi pathfinding sebelumnya digunakan untuk:

\`\`\`text
NPC mencari jalan
\`\`\`

Sekarang digunakan untuk:

\`\`\`text
Generator memeriksa apakah level dapat dimainkan
\`\`\`

---

# 96. Constraint-Based Generation

Perhatikan struktur praktikum:

\`\`\`text
BSP
↓
Generate Candidate
↓
Start / Goal
↓
Check Connectivity
↓
Check Enemy Distance
↓
Accept
\`\`\`

Artinya BSP dapat digabungkan dengan:

\`\`\`text
Constraint-Based Generation
\`\`\`

BSP bertugas:

\`\`\`text
menghasilkan layout
\`\`\`

Constraint bertugas:

\`\`\`text
menentukan apakah layout layak digunakan
\`\`\`

---

# 97. Menambahkan Regeneration

Tambahkan:

\`\`\`csharp
private void Update()
{
    if (Input.GetKeyDown(KeyCode.R))
    {
        seed++;
        GenerateDungeon();
    }
}
\`\`\`

Ketika:

\`\`\`text
R
\`\`\`

ditekan:

\`\`\`text
seed bertambah
↓
dungeon baru dibuat
\`\`\`

Ini sangat berguna untuk demonstrasi di kelas.

---

# 98. Catatan Penting ClearDungeon

\`Destroy()\` tidak langsung menghancurkan Object pada baris kode tersebut; penghancuran dilakukan oleh Unity setelah siklus frame terkait.

Karena itu, jika fungsi regenerate dikembangkan lebih lanjut, mahasiswa sebaiknya memahami bahwa pengelolaan object runtime dan hierarchy perlu dilakukan dengan hati-hati.

Untuk praktikum dasar, pendekatan ini masih cukup.

---

# 99. Pengembangan: NavMesh

Dungeon ini dapat dikembangkan menggunakan:

\`\`\`text
AI Navigation
\`\`\`

Alur:

\`\`\`text
Generate Dungeon
↓
Build NavMesh
↓
Spawn Enemy
↓
Enemy menggunakan NavMeshAgent
\`\`\`

Secara arsitektur:

\`\`\`text
Dungeon Generator
        │
        ↓
Generated Geometry
        │
        ↓
NavMeshSurface
        │
        ↓
NavMesh
        │
        ↓
Enemy NavMeshAgent
\`\`\`

Bagian ini direkomendasikan sebagai **pengembangan lanjutan**, bukan keharusan minimum Praktikum 10.

---

# 100. Mengapa NavMesh Cocok untuk BSP?

Dungeon BSP menghasilkan:

\`\`\`text
room rectangular
+
corridor
\`\`\`

Sehingga area walkable relatif jelas.

Ini membuat BSP sangat mudah dikembangkan menjadi environment untuk:

\`\`\`text
NPC Guard
FSM Enemy
Behavior Tree
Utility AI
Tactical AI
\`\`\`

yang telah dipelajari pada pertemuan sebelumnya.

---

# 101. Hierarchy Akhir

Saat Play:

\`\`\`text
PCG_Dungeon_Scene
│
├── Main Camera
├── Directional Light
├── DungeonGenerator
│
├── GeneratedLevel
│   ├── FloorPrefab(Clone)
│   ├── FloorPrefab(Clone)
│   ├── ...
│   ├── WallPrefab(Clone)
│   ├── ...
│   ├── GoalPrefab(Clone)
│   ├── Enemy_0
│   ├── Enemy_1
│   ├── ...
│   └── Item_0
│
└── PlayerPrefab(Clone)
\`\`\`

---

# 102. Eksperimen 1 — Seed

Uji:

\`\`\`text
Seed 100
Seed 200
Seed 300
Seed 400
Seed 500
\`\`\`

Catat:

- jumlah room,
- bentuk room,
- panjang corridor,
- posisi Goal.

Pertanyaan:

> Mengapa hanya mengubah satu angka dapat menghasilkan dungeon yang berbeda?

---

# 103. Eksperimen 2 — Min Leaf Size

Uji:

\`\`\`text
minLeafSize = 8
minLeafSize = 12
minLeafSize = 16
\`\`\`

Amati:

\`\`\`text
jumlah area BSP
jumlah room
ukuran room
\`\`\`

Hipotesis:

\`\`\`text
minLeafSize kecil
→ lebih banyak kemungkinan area kecil

minLeafSize besar
→ lebih sedikit area tetapi lebih besar
\`\`\`

---

# 104. Eksperimen 3 — Room Margin

Bandingkan:

\`\`\`text
roomMargin = 1
roomMargin = 2
roomMargin = 4
\`\`\`

Perhatikan jarak room terhadap batas Leaf.

---

# 105. Eksperimen 4 — Ukuran Dungeon

Bandingkan:

\`\`\`text
40 × 30
60 × 40
80 × 60
\`\`\`

Perhatikan:

- jumlah tile,
- jumlah GameObject,
- waktu generation,
- ukuran dungeon.

---

# 106. Eksperimen 5 — Enemy Density

Bandingkan:

\`\`\`text
Enemy Count = 5
Enemy Count = 10
Enemy Count = 20
\`\`\`

Diskusikan:

> Apakah jumlah Enemy seharusnya menjadi angka absolut atau disesuaikan dengan luas Floor?

---

# 107. Pengembangan Enemy Density

Versi lebih baik:

\`\`\`text
Enemy density =
jumlah enemy / jumlah floor
\`\`\`

Contoh:

\`\`\`text
1 enemy setiap 50 floor tile
\`\`\`

Dengan demikian:

\`\`\`text
Dungeon kecil → Enemy sedikit
Dungeon besar → Enemy lebih banyak
\`\`\`

---

# 108. Eksperimen 6 — Start/Goal

Bandingkan:

## Strategi A

\`\`\`text
First Room
→ Last Room
\`\`\`

## Strategi B

\`\`\`text
First Room
→ Geometrically Farthest Room
\`\`\`

## Strategi C

\`\`\`text
Start
→ Room dengan shortest-path distance terpanjang
\`\`\`

Strategi C paling baik tetapi membutuhkan perhitungan graph/grid distance.

---

# 109. Kesalahan Umum 1

## Dungeon tidak muncul

Periksa:

\`\`\`text
DungeonGenerator.cs sudah dipasang?
Prefab sudah dimasukkan Inspector?
Ada error di Console?
\`\`\`

---

# 110. Kesalahan Umum 2

## NullReferenceException

Kemungkinan:

\`\`\`text
floorPrefab kosong
wallPrefab kosong
playerPrefab kosong
\`\`\`

Periksa Inspector.

---

# 111. Kesalahan Umum 3

## IndexOutOfRangeException

Penyebab umum:

\`\`\`text
mengakses grid[x,y]
\`\`\`

dengan:

\`\`\`text
x < 0
atau
x >= mapWidth
\`\`\`

Gunakan:

\`\`\`csharp
IsInsideGrid()
\`\`\`

sebelum mengakses cell ketika koordinat berpotensi keluar batas.

---

# 112. Kesalahan Umum 4

## Room terlalu sedikit

Periksa:

\`\`\`text
Map terlalu kecil
Min Leaf Size terlalu besar
Max Leaf Size terlalu besar
Room Margin terlalu besar
\`\`\`

---

# 113. Kesalahan Umum 5

## Dungeon terlalu padat

Kurangi:

\`\`\`text
minRoomSize
\`\`\`

atau tambah:

\`\`\`text
roomMargin
\`\`\`

---

# 114. Kesalahan Umum 6

## Corridor terlalu sempit

Versi kita menggunakan:

\`\`\`text
corridorWidth = 1
\`\`\`

Sebagai pengembangan, mahasiswa dapat membuat:

\`\`\`text
corridorWidth = 2
\`\`\`

atau:

\`\`\`text
3
\`\`\`

---

# 115. Corridor Width 2

Contoh modifikasi horizontal:

\`\`\`csharp
private void CarveHorizontalWide(
    int x1,
    int x2,
    int y,
    int width)
{
    int start = Mathf.Min(x1, x2);
    int end = Mathf.Max(x1, x2);

    for (int x = start; x <= end; x++)
    {
        for (int offset = 0;
             offset < width;
             offset++)
        {
            int targetY = y + offset;

            if (IsInsideGrid(x, targetY))
            {
                grid[x, targetY]
                    = TileType.Floor;
            }
        }
    }
}
\`\`\`

---

# 116. Pengembangan Boss Room

Goal Room dapat diubah menjadi:

\`\`\`text
Boss Room
\`\`\`

Strategi:

\`\`\`text
Start Room
↓
hitung room terjauh
↓
Goal Room
↓
Boss Spawn
\`\`\`

Constraint:

\`\`\`text
Boss hanya boleh spawn di Goal Room.
\`\`\`

---

# 117. Pengembangan Treasure

Treasure sebaiknya tidak sepenuhnya random.

Contoh rule:

\`\`\`text
Treasure berada di room
yang tidak memiliki Start atau Goal.
\`\`\`

Pengembangan lebih lanjut:

\`\`\`text
Treasure ditempatkan pada dead-end.
\`\`\`

Ini membuat exploration mempunyai reward.

---

# 118. Hard Constraint dan Soft Constraint

## Hard Constraint

Wajib benar.

Contoh:

\`\`\`text
Start tersedia.
Goal tersedia.
Start–Goal memiliki path.
Player tidak spawn di Wall.
Enemy tidak spawn di Wall.
\`\`\`

Jika gagal:

\`\`\`text
Dungeon invalid.
\`\`\`

## Soft Constraint

Lebih baik terpenuhi.

Contoh:

\`\`\`text
Goal jauh dari Start.
Enemy tersebar merata.
Treasure ada di room samping.
Dungeon tidak terlalu linear.
\`\`\`

---

# 119. Generate-and-Test

Dungeon generator profesional sering menggunakan pola:

\`\`\`text
Generate
↓
Test
↓
Valid?
├── YES → Use
└── NO  → Regenerate
\`\`\`

Untuk praktikum dapat dibuat:

\`\`\`text
maxGenerationAttempts = 10
\`\`\`

sehingga generator tidak mencoba tanpa batas.

---

# 120. Metrics Dungeon

Untuk tugas pengembangan, mahasiswa dapat mencatat:

\`\`\`text
Room Count
Floor Count
Wall Count
Corridor Length
Start–Goal Distance
Enemy Count
Item Count
Floor Percentage
\`\`\`

Contoh:

\`\`\`text
Seed           : 12450
Rooms          : 8
Floor Tiles    : 620
StartGoalDist  : 47
Enemies        : 8
Items          : 5
\`\`\`

---

# 121. Mengapa Metrics Penting?

Tanpa metrics mahasiswa hanya mengatakan:

\`\`\`text
"Dungeon ini terlihat bagus."
\`\`\`

Dengan metrics:

\`\`\`text
Seed A
Path Length = 30

Seed B
Path Length = 65
\`\`\`

Kita mempunyai data yang dapat dibandingkan.

PCG tidak hanya dapat dievaluasi secara visual tetapi juga secara kuantitatif.

---

# 122. Hubungan dengan Dynamic Difficulty Adjustment

Praktikum ini merupakan fondasi untuk materi berikutnya.

PCG dapat menerima parameter seperti:

\`\`\`text
Enemy Count
Item Count
Dungeon Size
Room Count
Path Length
\`\`\`

Kemudian DDA dapat mengubahnya.

Contoh:

\`\`\`text
Player terlalu kuat

↓ 

enemyCount += 3
itemCount -= 1
dungeonSize meningkat
\`\`\`

Atau:

\`\`\`text
Player kesulitan

↓

enemyCount berkurang
itemCount bertambah
path lebih pendek
\`\`\`

---

# 123. Perbandingan dengan Random Walk

## BSP

Hasil:

\`\`\`text
Room
│
Corridor
│
Room
\`\`\`

Karakter:

\`\`\`text
Terstruktur
Rectangular
Mudah digunakan untuk combat room
\`\`\`

## Random Walk

Hasil:

\`\`\`text
jalur organik dan berliku
\`\`\`

Karakter:

\`\`\`text
Natural
Tidak terlalu terstruktur
Cocok untuk cave
\`\`\`

---

# 124. Perbandingan dengan Cellular Automata

## BSP

Cocok:

\`\`\`text
Dungeon
Building
Facility
Laboratory
RPG Room
\`\`\`

## Cellular Automata

Cocok:

\`\`\`text
Cave
Natural cavern
Underground environment
Alien nest
\`\`\`

Cellular Automata membutuhkan perhatian lebih besar terhadap region yang terputus.

---

# 125. Rekomendasi Tingkat Praktikum

## Wajib

Mahasiswa harus menyelesaikan:

\`\`\`text
Grid
Seed
BSP
Room
Corridor
Start
Goal
Floor
Wall
Player
Enemy Placement
Item Placement
\`\`\`

## Pengembangan

Minimal satu:

\`\`\`text
BFS Validation
NavMesh
Boss Room
Treasure Room
Runtime Regeneration
Dungeon Metrics
Improved Goal Placement
\`\`\`

---

# 126. Tugas Eksperimen

Mahasiswa melakukan minimal **5 generation** menggunakan seed berbeda.

Contoh tabel:

| Seed | Rooms | Enemy | Item | Start–Goal Distance | Valid |
|---|---:|---:|---:|---:|---|
| 101 | ... | ... | ... | ... | Yes/No |
| 202 | ... | ... | ... | ... | Yes/No |
| 303 | ... | ... | ... | ... | Yes/No |
| 404 | ... | ... | ... | ... | Yes/No |
| 505 | ... | ... | ... | ... | Yes/No |

Kemudian mahasiswa menjelaskan:

1. pengaruh seed,
2. pengaruh parameter BSP,
3. kualitas struktur dungeon,
4. konektivitas,
5. variasi antar-level.

---

# 127. Pertanyaan Analisis

Jawab pada laporan:

1. Apa perbedaan random dan procedural generation?

2. Mengapa level procedural tidak cukup hanya menggunakan random?

3. Mengapa diperlukan seed?

4. Apa fungsi grid pada dungeon generator?

5. Mengapa BSP cocok untuk room-based dungeon?

6. Apa yang dimaksud Leaf dalam BSP?

7. Mengapa Room tidak dibuat memenuhi seluruh Leaf?

8. Apa fungsi corridor?

9. Mengapa corridor berbentuk L mudah digunakan?

10. Apa yang dimaksud hard constraint?

11. Apa yang dimaksud soft constraint?

12. Mengapa Enemy tidak boleh spawn dekat Start?

13. Mengapa level perlu divalidasi?

14. Bagaimana BFS dapat memvalidasi dungeon?

15. Apa keuntungan menggabungkan PCG dengan NavMesh dan AI?

---

# 128. Checklist Keberhasilan Praktikum

Dungeon dinyatakan berhasil jika:

- [ ] Project Unity berhasil berjalan.
- [ ] Dungeon dibuat secara procedural.
- [ ] Dungeon menggunakan grid.
- [ ] BSP membagi area dungeon.
- [ ] Room berhasil dibuat.
- [ ] Corridor berhasil menghubungkan room.
- [ ] Floor berhasil dibuat dari grid.
- [ ] Wall berhasil dibuat.
- [ ] Player muncul pada Start.
- [ ] Goal muncul pada lokasi valid.
- [ ] Enemy hanya muncul pada Floor.
- [ ] Enemy tidak terlalu dekat Start.
- [ ] Item muncul pada Floor.
- [ ] Seed dapat diubah.
- [ ] Seed berbeda menghasilkan variasi dungeon.
- [ ] Seed yang sama dapat digunakan untuk mereproduksi generation.
- [ ] Tidak muncul error pada Console.
- [ ] Dungeon dapat dimainkan.

---

# 129. Rekomendasi Pengembangan Terbaik

Setelah versi dasar berhasil, urutan pengembangan yang saya rekomendasikan:

\`\`\`text
Tahap 1
BSP Dungeon
        ↓
Tahap 2
BFS Validation
        ↓
Tahap 3
Farthest Goal Placement
        ↓
Tahap 4
Enemy + Item Constraint
        ↓
Tahap 5
Runtime NavMesh
        ↓
Tahap 6
Enemy AI
        ↓
Tahap 7
Boss / Treasure Room
        ↓
Tahap 8
Dungeon Metrics
\`\`\`

Urutan ini menjaga agar kompleksitas bertambah secara bertahap.

---

# 130. Rekomendasi Praktikum Utama

Dari teknik yang dibahas pada Pertemuan 10:

\`\`\`text
Grid Random Fill
Random Walk
BSP
Cellular Automata
Constraint-Based Generation
\`\`\`

pilihan yang paling sesuai untuk praktikum utama adalah:

# BSP ROOM–CORRIDOR DUNGEON + CONSTRAINT VALIDATION

Alasannya:

1. merepresentasikan materi PCG dengan jelas,
2. memperlihatkan hubungan data dan visual Unity,
3. hasil mudah diamati mahasiswa,
4. procedural tetapi tetap terstruktur,
5. mudah menjamin konektivitas,
6. mudah dikembangkan,
7. cocok untuk NavMesh,
8. cocok untuk NPC AI,
9. dapat menggunakan materi Pathfinding sebelumnya,
10. cocok menjadi dasar mini-project dungeon crawler.

---

# 131. Alternatif Praktikum yang Lebih Mudah

Jika waktu praktikum terbatas, gunakan:

# RANDOM WALK DUNGEON

Pipeline:

\`\`\`text
Create Grid
↓
Fill Wall
↓
Start dari tengah
↓
Random Walk
↓
Carve Floor
↓
Pilih Floor terjauh sebagai Goal
↓
Spawn Enemy
↓
Build Level
\`\`\`

Random Walk lebih mudah diimplementasikan tetapi tidak memberikan struktur room–corridor sejelas BSP.

---

# 132. Alternatif Praktikum Lanjutan

Untuk mahasiswa yang ingin tantangan lebih tinggi:

# HYBRID PROCEDURAL DUNGEON

Gunakan:

\`\`\`text
BSP
↓
Room Generation
↓
L-Corridor
↓
Constraint Validation
↓
BFS Distance
↓
Boss Room
↓
Treasure Room
↓
Enemy Distribution
↓
Runtime NavMesh
↓
Enemy AI
\`\`\`

Pendekatan ini sangat cocok dijadikan basis pengembangan project lebih besar.

---

# 133. Nama Project Final

## Rekomendasi utama

\`\`\`text
GameCerdas_P10_PCG_BSP_Dungeon
\`\`\`

## Nama Scene

\`\`\`text
PCG_Dungeon_Scene
\`\`\`

## Nama generator

\`\`\`text
DungeonGenerator
\`\`\`

## Nama root runtime

\`\`\`text
GeneratedLevel
\`\`\`

Nama project ini jelas menunjukkan:

\`\`\`text
Game Cerdas
Pertemuan 10
Procedural Content Generation
Binary Space Partitioning
Dungeon
\`\`\`

---

# 134. Kesimpulan

Pada praktikum ini mahasiswa tidak sekadar membuat dungeon acak.

Mahasiswa membangun sebuah pipeline:

\`\`\`text
Representasi Data
        ↓
Procedural Algorithm
        ↓
Random Seed
        ↓
BSP Partitioning
        ↓
Room Generation
        ↓
Corridor Generation
        ↓
Constraint
        ↓
Validation
        ↓
Unity Scene Building
        ↓
Gameplay Object Placement
\`\`\`

Konsep paling penting adalah:

\`\`\`text
PCG ≠ Random
\`\`\`

melainkan:

\`\`\`text
PCG
=
Randomness
+
Algorithm
+
Rule
+
Constraint
+
Validation
+
Design Goal
\`\`\`

Dungeon yang baik bukan dungeon yang sekadar berbeda setiap kali dibuat.

Dungeon yang baik harus:

\`\`\`text
bervariasi
+
terhubung
+
dapat dimainkan
+
mempunyai tujuan
+
memiliki tantangan
+
dapat divalidasi
\`\`\`

Dengan demikian, praktikum ini menjadi penghubung antara:

\`\`\`text
PCG Fundamentals
Pathfinding
Game AI
Procedural Level Design
\`\`\`

dan materi berikutnya:

\`\`\`text
Dynamic Difficulty Adjustment
\`\`\``,Dc=`# MODUL PRAKTIKUM 11  
# DYNAMIC DIFFICULTY ADJUSTMENT (DDA) DI UNITY 6

## Adaptive Enemy Difficulty — Arena Survival

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Engine:** Unity 6  
**Bahasa Pemrograman:** C#  
**Topik:** Dynamic Difficulty Adjustment  
**Nama Project Unity yang direkomendasikan:**

\`\`\`text
GC11_AdaptiveArenaDDA
\`\`\`

Alternatif:

\`\`\`text
AdaptiveEnemyArena
DynamicDifficultyArena
SmartDifficultySurvival
DDAEnemyChallenge
AdaptiveCombatArena
\`\`\`

### Rekomendasi utama

\`\`\`text
GC11_AdaptiveArenaDDA
\`\`\`

Nama ini direkomendasikan karena:

- \`GC11\` menunjukkan Game Cerdas Praktikum/Pertemuan 11.
- \`AdaptiveArena\` menggambarkan jenis gameplay.
- \`DDA\` langsung menunjukkan teknologi utama yang dipelajari.
- mudah dibedakan dari project praktikum sebelumnya.

---

# 1. Tujuan Praktikum

Setelah menyelesaikan praktikum ini, mahasiswa diharapkan mampu:

1. Memahami prinsip **Dynamic Difficulty Adjustment (DDA)**.
2. Mengukur performa player berdasarkan data gameplay.
3. Melakukan normalisasi beberapa metrik performa.
4. Menggabungkan beberapa metrik menjadi \`skillScore\`.
5. Membuat \`DifficultyManager\`.
6. Mengubah \`difficultyMultiplier\` berdasarkan performa player.
7. Membatasi tingkat difficulty minimum dan maksimum.
8. Menggunakan evaluation window untuk mencegah perubahan difficulty terlalu cepat.
9. Mengadaptasi atribut enemy berdasarkan difficulty.
10. Mengadaptasi interval spawn musuh.
11. Membuat Debug UI untuk mengamati proses DDA.
12. Menguji apakah sistem DDA bekerja secara stabil.
13. Menjelaskan perbedaan antara difficulty statis dan difficulty adaptif.

---

# 2. Hasil Akhir Praktikum

Mahasiswa akan membuat game sederhana berbentuk:

# Adaptive Arena Survival

Player berada di dalam arena dan menghadapi enemy yang muncul secara berkala.

Player dapat:

- bergerak menggunakan WASD atau Arrow Key,
- mengarahkan karakter menggunakan mouse,
- menembak enemy,
- menerima damage,
- membunuh enemy.

Enemy dapat:

- mengejar player,
- menyerang ketika cukup dekat,
- menerima damage,
- mati,
- muncul secara berkala dari beberapa spawn point.

Sistem DDA kemudian akan:

\`\`\`text
mengamati performa player
            ↓
menghitung skill score
            ↓
menentukan kondisi player
            ↓
mengubah difficulty multiplier
            ↓
mengubah karakteristik enemy/spawner
\`\`\`

Contohnya:

\`\`\`text
Player bermain sangat baik
        ↓
Skill Score tinggi
        ↓
Difficulty meningkat
        ↓
Enemy lebih kuat
Enemy muncul lebih cepat
\`\`\`

Sedangkan:

\`\`\`text
Player kesulitan
        ↓
Skill Score rendah
        ↓
Difficulty berkurang
        ↓
Enemy sedikit lebih lemah
Enemy muncul lebih lambat
\`\`\`

---

# 3. Konsep Dasar Dynamic Difficulty Adjustment

## 3.1 Apa itu DDA?

**Dynamic Difficulty Adjustment** adalah teknik yang membuat game mengubah tingkat kesulitannya secara otomatis berdasarkan performa atau keadaan player.

Pada difficulty tradisional:

\`\`\`text
Easy
Normal
Hard
\`\`\`

player biasanya memilih difficulty sebelum permainan dimulai.

Pada DDA:

\`\`\`text
Difficulty dapat berubah ketika game sedang dimainkan.
\`\`\`

Contoh:

\`\`\`text
Player terlalu mudah mengalahkan enemy
                ↓
Game meningkatkan tantangan
\`\`\`

atau:

\`\`\`text
Player berkali-kali menerima banyak damage
                ↓
Game mengurangi tantangan
\`\`\`

---

# 4. Static Difficulty vs Dynamic Difficulty

## Static Difficulty

Contoh:

\`\`\`text
Easy
Enemy HP = 70

Normal
Enemy HP = 100

Hard
Enemy HP = 150
\`\`\`

Nilai tersebut relatif tetap selama permainan.

## Dynamic Difficulty

Contoh:

\`\`\`text
Enemy HP =
baseHealth × difficultyMultiplier
\`\`\`

Misalnya:

\`\`\`text
difficultyMultiplier = 0.75
Enemy HP = 75
\`\`\`

atau:

\`\`\`text
difficultyMultiplier = 1.25
Enemy HP = 125
\`\`\`

Difficulty berubah mengikuti performa player.

---

# 5. Konsep Sistem yang Akan Dibuat

Arsitektur praktikum:

\`\`\`text
Player
  │
  │ gameplay metrics
  ↓
PlayerPerformanceTracker
  │
  │ Skill Score
  ↓
DifficultyManager
  │
  ├───────────────→ EnemySpawner
  │                    │
  │                    └── Spawn Interval
  │
  └───────────────→ Enemy
                       ├── Health
                       ├── Damage
                       └── Speed
\`\`\`

Debug UI membaca:

\`\`\`text
PlayerPerformanceTracker
DifficultyManager
EnemySpawner
PlayerHealth
\`\`\`

dan menampilkannya ke layar.

---

# 6. Rekomendasi Desain DDA untuk Praktikum

Pada materi terdapat banyak parameter yang dapat diadaptasi:

- enemy health,
- enemy damage,
- movement speed,
- attack cooldown,
- detection radius,
- spawn interval,
- jumlah enemy,
- health drop,
- loot,
- dan sebagainya.

Namun untuk praktikum pertama tentang DDA, **jangan mengubah semua parameter sekaligus**.

## Pilihan terbaik

Gunakan tiga parameter utama:

\`\`\`text
1. Enemy Health
2. Enemy Damage
3. Enemy Spawn Interval
\`\`\`

Enemy speed dapat ikut berubah tetapi dengan pengaruh yang lebih kecil.

Alasannya:

- efek DDA mudah diamati,
- implementasinya sederhana,
- hasil eksperimen mudah dibandingkan,
- balancing tidak terlalu kompleks.

---

# 7. Model Difficulty yang Digunakan

Gunakan difficulty kontinu:

\`\`\`text
difficultyMultiplier
\`\`\`

Nilai awal:

\`\`\`text
1.0
\`\`\`

Batas:

\`\`\`text
Minimum = 0.75
Maximum = 1.50
\`\`\`

Langkah perubahan:

\`\`\`text
0.05
\`\`\`

Contoh:

\`\`\`text
1.00
1.05
1.10
1.15
1.20
...
1.50
\`\`\`

atau turun:

\`\`\`text
1.00
0.95
0.90
0.85
0.80
0.75
\`\`\`

---

# 8. Evaluation Window

Difficulty **tidak dievaluasi setiap frame**.

Gunakan:

\`\`\`text
evaluationInterval = 30 detik
\`\`\`

Alurnya:

\`\`\`text
30 detik gameplay
      ↓
Kumpulkan data
      ↓
Hitung Skill Score
      ↓
Evaluasi Difficulty
      ↓
Reset data window tertentu
      ↓
30 detik berikutnya
\`\`\`

Ini disebut:

**Evaluation Window**

Tujuannya:

- mengurangi perubahan difficulty berlebihan,
- menghindari oscillation,
- mengurangi efek satu kejadian ekstrem,
- menghasilkan difficulty yang lebih stabil.

---

# 9. Performance Metrics

Pada praktikum ini kita menggunakan:

\`\`\`text
Health
Kills
Damage Taken
Accuracy
\`\`\`

## 9.1 Health Score

\`\`\`text
healthScore =
currentHealth / maxHealth
\`\`\`

Contoh:

\`\`\`text
currentHealth = 80
maxHealth = 100

healthScore = 0.8
\`\`\`

---

## 9.2 Kill Score

\`\`\`text
killScore =
killsInWindow / expectedKills
\`\`\`

Nilai kemudian dibatasi:

\`\`\`text
0 sampai 1
\`\`\`

Misalnya target:

\`\`\`text
expectedKills = 5
\`\`\`

Player membunuh 4 enemy:

\`\`\`text
killScore = 4 / 5
          = 0.8
\`\`\`

---

## 9.3 Damage Score

Semakin banyak damage diterima, semakin kecil score.

\`\`\`text
damageScore =
1 - damageTaken / maxExpectedDamage
\`\`\`

Nilai dibatasi antara:

\`\`\`text
0 sampai 1
\`\`\`

---

## 9.4 Accuracy Score

\`\`\`text
accuracyScore =
hitCount / shotCount
\`\`\`

Contoh:

\`\`\`text
10 tembakan
7 mengenai enemy

accuracyScore = 0.7
\`\`\`

---

# 10. Skill Score

Semua metrik digabungkan.

Pada praktikum ini kita gunakan:

\`\`\`text
Skill Score =
0.35 × healthScore
+
0.30 × killScore
+
0.20 × damageScore
+
0.15 × accuracyScore
\`\`\`

Total bobot:

\`\`\`text
0.35 + 0.30 + 0.20 + 0.15 = 1.0
\`\`\`

Dengan demikian:

\`\`\`text
0.0 <= SkillScore <= 1.0
\`\`\`

Interpretasi:

| Skill Score | Kondisi |
|---:|---|
| < 0.35 | Player kesulitan |
| 0.35–0.75 | Kondisi relatif seimbang |
| > 0.75 | Player dominan |

---

# 11. Mengapa Accuracy Digunakan?

Materi kuliah juga memberikan contoh \`survivalScore\`.

Untuk praktikum ini, accuracy digunakan sebagai pengganti salah satu komponen temporal karena evaluation dilakukan secara periodik setiap 30 detik.

Accuracy memberi variasi data yang lebih mudah diamati ketika mahasiswa sedang bermain.

Mahasiswa tetap dapat melakukan eksperimen dengan mengganti accuracy menjadi:

\`\`\`text
survivalScore
\`\`\`

pada bagian tugas pengembangan.

---

# 12. Rule DDA

Gunakan aturan:

\`\`\`text
Jika skillScore > 0.75
    difficulty += 0.05

Jika skillScore < 0.35
    difficulty -= 0.05

Selain itu
    difficulty tetap
\`\`\`

Kemudian:

\`\`\`text
difficulty =
Clamp(difficulty, 0.75, 1.50)
\`\`\`

Contoh:

\`\`\`text
Skill Score = 0.82

Difficulty:
1.00 → 1.05
\`\`\`

30 detik kemudian:

\`\`\`text
Skill Score = 0.79

Difficulty:
1.05 → 1.10
\`\`\`

---

# 13. Struktur Folder Project

Buat folder:

\`\`\`text
Assets/
│
├── Materials/
│
├── Prefabs/
│   ├── Player.prefab
│   └── Enemy.prefab
│
├── Scenes/
│   └── MainArena.unity
│
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   ├── PlayerHealth.cs
│   │   └── PlayerShooter.cs
│   │
│   ├── Enemy/
│   │   ├── EnemyAI.cs
│   │   └── EnemyHealth.cs
│   │
│   ├── DDA/
│   │   ├── PlayerPerformanceTracker.cs
│   │   └── DifficultyManager.cs
│   │
│   ├── Gameplay/
│   │   └── EnemySpawner.cs
│   │
│   └── UI/
│       └── DDADebugUI.cs
│
└── UI/
\`\`\`

---

# 14. Istilah Teknis Unity

Sebelum implementasi, pahami beberapa istilah berikut.

## GameObject

Objek dasar di dalam Scene Unity.

Contoh:

\`\`\`text
Player
Enemy
Main Camera
Directional Light
GameManager
\`\`\`

GameObject sendiri hanyalah container.

Fungsionalitasnya berasal dari **Component**.

---

## Component

Bagian yang memberikan kemampuan tertentu kepada GameObject.

Contoh:

\`\`\`text
Transform
CharacterController
Collider
MeshRenderer
Script
\`\`\`

---

## Transform

Setiap GameObject memiliki Transform.

Transform menyimpan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

---

## Scene

Scene merupakan sebuah level atau lingkungan permainan.

Dalam praktikum:

\`\`\`text
MainArena.unity
\`\`\`

berisi seluruh objek arena.

---

## Prefab

Prefab adalah template GameObject yang dapat digunakan berulang kali.

Enemy dibuat sebagai Prefab karena:

\`\`\`text
EnemySpawner
\`\`\`

akan membuat banyak enemy selama permainan.

---

## Inspector

Panel Unity untuk:

- melihat Component,
- mengatur parameter,
- memasukkan reference,
- mengubah nilai public atau \`[SerializeField]\`.

---

## SerializeField

Contoh:

\`\`\`csharp
[SerializeField] private float moveSpeed = 5f;
\`\`\`

Variabel tetap \`private\`, tetapi nilainya muncul di Inspector.

Keuntungan:

- enkapsulasi tetap terjaga,
- designer dapat mengatur nilai tanpa mengubah kode.

---

## CharacterController

Component Unity untuk menggerakkan karakter tanpa menggunakan simulasi fisika Rigidbody penuh.

Pada praktikum:

\`\`\`text
Player
\`\`\`

menggunakan \`CharacterController\`.

---

## Collider

Collider menentukan area tabrakan.

Contoh:

\`\`\`text
BoxCollider
CapsuleCollider
SphereCollider
\`\`\`

---

## Coroutine

Coroutine memungkinkan proses berjalan bertahap berdasarkan waktu.

Contoh:

\`\`\`csharp
IEnumerator SpawnRoutine()
\`\`\`

dapat melakukan:

\`\`\`text
spawn
wait
spawn
wait
...
\`\`\`

---

## Time.deltaTime

Waktu antar-frame.

Digunakan agar movement tidak tergantung frame rate.

\`\`\`csharp
position += direction * speed * Time.deltaTime;
\`\`\`

---

## Mathf.Clamp

Membatasi nilai.

Contoh:

\`\`\`csharp
difficultyMultiplier =
    Mathf.Clamp(
        difficultyMultiplier,
        0.75f,
        1.5f
    );
\`\`\`

---

## Mathf.Clamp01

Membatasi nilai antara:

\`\`\`text
0 dan 1
\`\`\`

Sangat berguna untuk normalisasi.

---

## Instantiate

Membuat GameObject baru berdasarkan Prefab.

Contoh:

\`\`\`csharp
Instantiate(enemyPrefab, position, rotation);
\`\`\`

---

## Destroy

Menghapus GameObject.

\`\`\`csharp
Destroy(gameObject);
\`\`\`

---

# 15. Membuat Project

Buka Unity Hub.

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D Core
\`\`\`

Untuk praktikum ini keduanya dapat digunakan.

Nama project:

\`\`\`text
GC11_AdaptiveArenaDDA
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 16. Pengaturan Input

Praktikum menggunakan input sederhana:

\`\`\`text
Input.GetAxisRaw()
Input.GetMouseButtonDown()
\`\`\`

Jika project menggunakan Input System baru, buka:

\`\`\`text
Edit
→ Project Settings
→ Player
→ Active Input Handling
\`\`\`

pilih:

\`\`\`text
Both
\`\`\`

Dengan demikian input klasik tetap dapat digunakan.

---

# 17. Membuat Arena

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
ArenaFloor
\`\`\`

Scale misalnya:

\`\`\`text
X = 3
Y = 1
Z = 3
\`\`\`

Tambahkan beberapa Cube sebagai dinding jika diperlukan.

Contoh hierarchy:

\`\`\`text
MainArena
├── ArenaFloor
├── WallNorth
├── WallSouth
├── WallEast
└── WallWest
\`\`\`

---

# 18. Membuat Player

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Posisi:

\`\`\`text
X = 0
Y = 1
Z = 0
\`\`\`

Tambahkan:

\`\`\`text
CharacterController
\`\`\`

Hapus \`CapsuleCollider\` bawaan jika menggunakan CharacterController agar tidak ada collider ganda.

---

# 19. Script PlayerController.cs

Buat:

\`\`\`text
Assets/Scripts/Player/PlayerController.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical).normalized;

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );
    }
}
\`\`\`

Pasang ke:

\`\`\`text
Player
\`\`\`

---

# 20. Penjelasan PlayerController

\`\`\`csharp
[RequireComponent(typeof(CharacterController))]
\`\`\`

memastikan GameObject memiliki \`CharacterController\`.

---

\`\`\`csharp
Input.GetAxisRaw("Horizontal")
\`\`\`

membaca:

\`\`\`text
A / D
Left Arrow / Right Arrow
\`\`\`

---

\`\`\`csharp
Input.GetAxisRaw("Vertical")
\`\`\`

membaca:

\`\`\`text
W / S
Up Arrow / Down Arrow
\`\`\`

---

\`\`\`csharp
.normalized
\`\`\`

mencegah movement diagonal menjadi lebih cepat.

---

# 21. Membuat PlayerHealth

Buat:

\`\`\`text
PlayerHealth.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;
        currentHealth =
            Mathf.Clamp(currentHealth, 0f, maxHealth);

        PlayerPerformanceTracker tracker =
            FindFirstObjectByType<PlayerPerformanceTracker>();

        if (tracker != null)
        {
            tracker.RegisterDamageTaken(damage);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );
    }

    private void Die()
    {
        Debug.Log("Player died.");

        // Untuk praktikum sederhana:
        // isi kembali HP agar pengujian DDA dapat dilanjutkan.
        currentHealth = maxHealth;
    }
}
\`\`\`

Pasang ke:

\`\`\`text
Player
\`\`\`

---

# 22. Catatan tentang FindFirstObjectByType

Kode:

\`\`\`csharp
FindFirstObjectByType<PlayerPerformanceTracker>()
\`\`\`

mencari object tertentu di Scene.

Untuk project praktikum kecil, pendekatan ini cukup sederhana.

Untuk game produksi besar lebih baik menggunakan:

- reference melalui Inspector,
- event,
- dependency injection,
- GameManager,
- atau sistem komunikasi lain.

---

# 23. Membuat PlayerPerformanceTracker

Buat:

\`\`\`text
PlayerPerformanceTracker.cs
\`\`\`

Script ini merupakan salah satu komponen paling penting.

Tugasnya:

\`\`\`text
mencatat kills
mencatat damage
mencatat jumlah tembakan
mencatat hit
menghitung skill score
\`\`\`

Gunakan:

\`\`\`csharp
using UnityEngine;

public class PlayerPerformanceTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Normalization")]
    [SerializeField] private float expectedKills = 5f;
    [SerializeField] private float maxExpectedDamage = 60f;

    private int killsInWindow;
    private float damageTakenInWindow;

    private int shotsInWindow;
    private int hitsInWindow;

    private float lastSkillScore = 0.5f;

    public int KillsInWindow => killsInWindow;
    public float DamageTakenInWindow => damageTakenInWindow;

    public int ShotsInWindow => shotsInWindow;
    public int HitsInWindow => hitsInWindow;

    public float LastSkillScore => lastSkillScore;

    public void RegisterKill()
    {
        killsInWindow++;
    }

    public void RegisterDamageTaken(float damage)
    {
        damageTakenInWindow += damage;
    }

    public void RegisterShot()
    {
        shotsInWindow++;
    }

    public void RegisterHit()
    {
        hitsInWindow++;
    }

    public float CalculateSkillScore()
    {
        if (playerHealth == null)
            return 0.5f;

        float healthScore =
            playerHealth.CurrentHealth /
            playerHealth.MaxHealth;

        float killScore =
            Mathf.Clamp01(
                killsInWindow / expectedKills
            );

        float damageScore =
            1f -
            Mathf.Clamp01(
                damageTakenInWindow /
                maxExpectedDamage
            );

        float accuracyScore = 0.5f;

        if (shotsInWindow > 0)
        {
            accuracyScore =
                Mathf.Clamp01(
                    (float)hitsInWindow /
                    shotsInWindow
                );
        }

        lastSkillScore =
            0.35f * healthScore +
            0.30f * killScore +
            0.20f * damageScore +
            0.15f * accuracyScore;

        return Mathf.Clamp01(lastSkillScore);
    }

    public void ResetWindow()
    {
        killsInWindow = 0;
        damageTakenInWindow = 0f;

        shotsInWindow = 0;
        hitsInWindow = 0;
    }
}
\`\`\`

---

# 24. Mengatur PlayerPerformanceTracker

Buat Empty GameObject:

\`\`\`text
GameObject
→ Create Empty
\`\`\`

Rename:

\`\`\`text
PlayerPerformanceTracker
\`\`\`

Tambahkan script:

\`\`\`text
PlayerPerformanceTracker
\`\`\`

Pada Inspector:

\`\`\`text
Player Health
\`\`\`

drag object:

\`\`\`text
Player
\`\`\`

ke field tersebut.

Atur:

\`\`\`text
Expected Kills = 5
Max Expected Damage = 60
\`\`\`

---

# 25. Membuat PlayerShooter

Buat child Player:

\`\`\`text
Player
└── ShootOrigin
\`\`\`

Posisikan di depan Player.

Buat:

\`\`\`text
PlayerShooter.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private float weaponDamage = 35f;
    [SerializeField] private float shootingRange = 25f;

    [SerializeField]
    private PlayerPerformanceTracker performanceTracker;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void AimAtMouse()
    {
        if (mainCamera == null)
            return;

        Ray ray =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        Plane groundPlane =
            new Plane(
                Vector3.up,
                Vector3.zero
            );

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint =
                ray.GetPoint(distance);

            Vector3 direction =
                targetPoint - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.forward =
                    direction.normalized;
            }
        }
    }

    private void Shoot()
    {
        performanceTracker?.RegisterShot();

        RaycastHit hit;

        if (Physics.Raycast(
            shootOrigin.position,
            shootOrigin.forward,
            out hit,
            shootingRange))
        {
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(weaponDamage);

                performanceTracker?.RegisterHit();
            }
        }

        Debug.DrawRay(
            shootOrigin.position,
            shootOrigin.forward * shootingRange,
            Color.red,
            0.3f
        );
    }
}
\`\`\`

Pasang script ke:

\`\`\`text
Player
\`\`\`

Assign:

\`\`\`text
Shoot Origin
Performance Tracker
\`\`\`

melalui Inspector.

---

# 26. Camera Arena

Posisikan Main Camera misalnya:

\`\`\`text
Position:
X = 0
Y = 18
Z = -10

Rotation:
X = 55
Y = 0
Z = 0
\`\`\`

Pastikan seluruh arena terlihat.

---

# 27. Membuat Enemy

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Enemy
\`\`\`

Tambahkan material berbeda agar mudah dikenali.

Pastikan terdapat Collider.

---

# 28. EnemyHealth.cs

Buat:

\`\`\`csharp
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private float maxHealth;
    private float currentHealth;

    private PlayerPerformanceTracker tracker;

    public void Initialize(
        float health,
        PlayerPerformanceTracker performanceTracker)
    {
        maxHealth = health;
        currentHealth = maxHealth;

        tracker = performanceTracker;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (tracker != null)
        {
            tracker.RegisterKill();
        }

        Destroy(gameObject);
    }
}
\`\`\`

Pasang ke:

\`\`\`text
Enemy
\`\`\`

---

# 29. EnemyAI.cs

Enemy akan melakukan AI sederhana:

\`\`\`text
Find Player
     ↓
Move toward Player
     ↓
Jika dekat
     ↓
Attack
\`\`\`

Buat:

\`\`\`csharp
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private PlayerHealth playerHealth;

    private float moveSpeed;
    private float damage;

    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    private float nextAttackTime;

    public void Initialize(
        Transform target,
        float speed,
        float attackDamage)
    {
        player = target;

        playerHealth =
            target.GetComponent<PlayerHealth>();

        moveSpeed = speed;
        damage = attackDamage;
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            AttackPlayer();
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.forward =
                direction.normalized;

            transform.position +=
                direction.normalized *
                moveSpeed *
                Time.deltaTime;
        }
    }

    private void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime =
            Time.time + attackCooldown;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}
\`\`\`

---

# 30. Catatan Enemy Movement

Pada praktikum ini enemy bergerak langsung menggunakan:

\`\`\`csharp
transform.position
\`\`\`

Tujuannya agar fokus tetap pada DDA.

Untuk pengembangan lebih lanjut dapat diganti dengan:

\`\`\`text
NavMesh
NavMeshAgent
FSM
Behavior Tree
Utility AI
\`\`\`

Dengan demikian materi DDA dapat dihubungkan dengan praktikum AI sebelumnya.

---

# 31. Membuat DifficultyManager

Ini merupakan **pusat DDA**.

Buat:

\`\`\`text
DifficultyManager.cs
\`\`\`

\`\`\`csharp
using System.Collections;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerPerformanceTracker performanceTracker;

    [Header("Difficulty")]
    [SerializeField]
    private float difficultyMultiplier = 1f;

    [SerializeField]
    private float minDifficulty = 0.75f;

    [SerializeField]
    private float maxDifficulty = 1.50f;

    [SerializeField]
    private float adjustmentStep = 0.05f;

    [Header("Skill Threshold")]
    [SerializeField]
    private float lowerThreshold = 0.35f;

    [SerializeField]
    private float upperThreshold = 0.75f;

    [Header("Evaluation")]
    [SerializeField]
    private float evaluationInterval = 30f;

    private string lastAdjustment = "Stable";

    public float DifficultyMultiplier =>
        difficultyMultiplier;

    public string LastAdjustment =>
        lastAdjustment;

    public float EvaluationInterval =>
        evaluationInterval;

    private void Start()
    {
        StartCoroutine(EvaluationRoutine());
    }

    private IEnumerator EvaluationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                evaluationInterval
            );

            EvaluateDifficulty();
        }
    }

    private void EvaluateDifficulty()
    {
        if (performanceTracker == null)
            return;

        float skillScore =
            performanceTracker.CalculateSkillScore();

        if (skillScore > upperThreshold)
        {
            difficultyMultiplier +=
                adjustmentStep;

            lastAdjustment = "Increasing";
        }
        else if (skillScore < lowerThreshold)
        {
            difficultyMultiplier -=
                adjustmentStep;

            lastAdjustment = "Decreasing";
        }
        else
        {
            lastAdjustment = "Stable";
        }

        difficultyMultiplier =
            Mathf.Clamp(
                difficultyMultiplier,
                minDifficulty,
                maxDifficulty
            );

        Debug.Log(
            $"Skill Score: {skillScore:F2} | " +
            $"Difficulty: {difficultyMultiplier:F2} | " +
            $"Adjustment: {lastAdjustment}"
        );

        performanceTracker.ResetWindow();
    }

    public float GetEnemyHealth(float baseHealth)
    {
        return baseHealth *
               difficultyMultiplier;
    }

    public float GetEnemyDamage(float baseDamage)
    {
        float damageMultiplier =
            Mathf.Lerp(
                1f,
                difficultyMultiplier,
                0.5f
            );

        return baseDamage *
               damageMultiplier;
    }

    public float GetEnemySpeed(float baseSpeed)
    {
        float speedMultiplier =
            Mathf.Lerp(
                1f,
                difficultyMultiplier,
                0.25f
            );

        return baseSpeed *
               speedMultiplier;
    }

    public float GetSpawnInterval(
        float baseInterval)
    {
        return baseInterval /
               difficultyMultiplier;
    }
}
\`\`\`

---

# 32. Memahami DifficultyManager

Bagian:

\`\`\`csharp
difficultyMultiplier = 1f;
\`\`\`

adalah difficulty awal.

---

Batas:

\`\`\`csharp
minDifficulty = 0.75f;
maxDifficulty = 1.50f;
\`\`\`

mencegah sistem menghasilkan difficulty ekstrem.

---

Langkah:

\`\`\`csharp
adjustmentStep = 0.05f;
\`\`\`

mencegah perubahan drastis.

---

Evaluation:

\`\`\`csharp
evaluationInterval = 30f;
\`\`\`

membuat DDA tidak mengevaluasi setiap frame.

---

# 33. Mengapa Damage dan Speed Tidak Dikalikan Penuh?

Health menggunakan:

\`\`\`csharp
baseHealth * difficultyMultiplier
\`\`\`

Namun damage menggunakan pengaruh yang lebih lembut:

\`\`\`csharp
Mathf.Lerp(
    1f,
    difficultyMultiplier,
    0.5f
)
\`\`\`

Speed lebih lembut lagi:

\`\`\`csharp
Mathf.Lerp(
    1f,
    difficultyMultiplier,
    0.25f
)
\`\`\`

Ini merupakan pilihan desain praktikum agar difficulty tidak meningkat terlalu agresif.

Jika seluruh parameter langsung dikalikan \`1.5\`, enemy dapat sekaligus:

\`\`\`text
50% lebih kuat
50% lebih sakit
50% lebih cepat
50% lebih sering muncul
\`\`\`

dan peningkatan sebenarnya akan terasa jauh lebih besar daripada sekadar 50%.

---

# 34. Membuat GameManager

Buat Empty GameObject:

\`\`\`text
GameManager
\`\`\`

Tambahkan:

\`\`\`text
DifficultyManager
\`\`\`

Assign:

\`\`\`text
Performance Tracker
→ PlayerPerformanceTracker
\`\`\`

Atur:

\`\`\`text
Difficulty Multiplier = 1.00

Min Difficulty = 0.75
Max Difficulty = 1.50

Adjustment Step = 0.05

Lower Threshold = 0.35
Upper Threshold = 0.75

Evaluation Interval = 30
\`\`\`

---

# 35. Membuat EnemySpawner

Buat:

\`\`\`text
EnemySpawner.cs
\`\`\`

\`\`\`csharp
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform player;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private DifficultyManager difficultyManager;

    [SerializeField]
    private PlayerPerformanceTracker performanceTracker;

    [Header("Base Enemy Settings")]
    [SerializeField]
    private float baseEnemyHealth = 100f;

    [SerializeField]
    private float baseEnemyDamage = 10f;

    [SerializeField]
    private float baseEnemySpeed = 3f;

    [Header("Spawner")]
    [SerializeField]
    private float baseSpawnInterval = 4.5f;

    [SerializeField]
    private int baseMaxEnemies = 5;

    public float CurrentSpawnInterval =>
        difficultyManager != null
        ? difficultyManager.GetSpawnInterval(
            baseSpawnInterval)
        : baseSpawnInterval;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float interval =
                CurrentSpawnInterval;

            yield return new WaitForSeconds(
                interval
            );

            TrySpawnEnemy();
        }
    }

    private void TrySpawnEnemy()
    {
        if (enemyPrefab == null ||
            player == null ||
            spawnPoints.Length == 0)
        {
            return;
        }

        int enemyCount =
            GameObject.FindGameObjectsWithTag(
                "Enemy"
            ).Length;

        int maxEnemies =
            Mathf.RoundToInt(
                baseMaxEnemies *
                difficultyManager
                    .DifficultyMultiplier
            );

        if (enemyCount >= maxEnemies)
            return;

        int index =
            Random.Range(
                0,
                spawnPoints.Length
            );

        Transform spawnPoint =
            spawnPoints[index];

        GameObject enemy =
            Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        EnemyHealth health =
            enemy.GetComponent<EnemyHealth>();

        EnemyAI ai =
            enemy.GetComponent<EnemyAI>();

        float healthValue =
            difficultyManager
                .GetEnemyHealth(
                    baseEnemyHealth
                );

        float damageValue =
            difficultyManager
                .GetEnemyDamage(
                    baseEnemyDamage
                );

        float speedValue =
            difficultyManager
                .GetEnemySpeed(
                    baseEnemySpeed
                );

        health.Initialize(
            healthValue,
            performanceTracker
        );

        ai.Initialize(
            player,
            speedValue,
            damageValue
        );
    }
}
\`\`\`

---

# 36. Tag Enemy

Spawner menggunakan:

\`\`\`csharp
FindGameObjectsWithTag("Enemy")
\`\`\`

Karena itu buat Tag:

\`\`\`text
Enemy
\`\`\`

Caranya:

1. pilih Enemy,
2. pada Inspector cari \`Tag\`,
3. pilih \`Add Tag\`,
4. tambahkan:

\`\`\`text
Enemy
\`\`\`

5. kembali ke Enemy,
6. pilih Tag:

\`\`\`text
Enemy
\`\`\`

---

# 37. Fungsi Tag

**Tag** digunakan untuk memberi identitas logis pada GameObject.

Contohnya:

\`\`\`text
Player
Enemy
Projectile
Pickup
Boss
\`\`\`

Pada praktikum:

\`\`\`text
Enemy
\`\`\`

digunakan untuk menghitung jumlah musuh aktif.

Tag berbeda dengan nama GameObject.

Nama boleh:

\`\`\`text
Enemy(Clone)
Enemy(Clone)
Enemy(Clone)
\`\`\`

tetapi semuanya tetap memiliki:

\`\`\`text
Tag = Enemy
\`\`\`

---

# 38. Membuat Enemy Prefab

Setelah Enemy selesai:

drag:

\`\`\`text
Enemy
\`\`\`

dari Hierarchy menuju:

\`\`\`text
Assets/Prefabs/
\`\`\`

Unity akan membuat:

\`\`\`text
Enemy.prefab
\`\`\`

Kemudian hapus Enemy dari Scene.

Spawner sekarang yang bertanggung jawab membuat enemy.

---

# 39. Membuat Spawn Points

Buat Empty GameObject:

\`\`\`text
SpawnPoints
\`\`\`

Child:

\`\`\`text
SpawnPoints
├── SpawnPoint1
├── SpawnPoint2
├── SpawnPoint3
└── SpawnPoint4
\`\`\`

Letakkan di empat sisi arena.

Contoh:

\`\`\`text
SpawnPoint1 = (-10, 1, 10)
SpawnPoint2 = (10, 1, 10)
SpawnPoint3 = (-10, 1, -10)
SpawnPoint4 = (10, 1, -10)
\`\`\`

---

# 40. Membuat EnemySpawner Object

Buat:

\`\`\`text
EnemySpawner
\`\`\`

Tambahkan:

\`\`\`text
EnemySpawner.cs
\`\`\`

Assign:

\`\`\`text
Enemy Prefab
→ Enemy.prefab

Player
→ Player

Difficulty Manager
→ GameManager

Performance Tracker
→ PlayerPerformanceTracker
\`\`\`

Atur Spawn Points menjadi:

\`\`\`text
Size = 4
\`\`\`

masukkan keempat SpawnPoint.

Parameter awal:

\`\`\`text
Base Enemy Health = 100

Base Enemy Damage = 10

Base Enemy Speed = 3

Base Spawn Interval = 4.5

Base Max Enemies = 5
\`\`\`

---

# 41. Hierarchy Sementara

Scene sekarang kira-kira:

\`\`\`text
MainArena
│
├── Main Camera
├── Directional Light
│
├── ArenaFloor
│
├── Player
│   └── ShootOrigin
│
├── GameManager
│
├── PlayerPerformanceTracker
│
├── EnemySpawner
│
└── SpawnPoints
    ├── SpawnPoint1
    ├── SpawnPoint2
    ├── SpawnPoint3
    └── SpawnPoint4
\`\`\`

---

# 42. Pengujian Tahap Pertama

Tekan:

\`\`\`text
Play
\`\`\`

Periksa:

- player dapat bergerak,
- player dapat diarahkan dengan mouse,
- klik kiri menghasilkan tembakan,
- enemy muncul,
- enemy mengejar player,
- enemy dapat memberikan damage,
- player dapat menembak enemy,
- enemy dapat mati.

Jangan melanjutkan ke UI jika gameplay dasar belum berfungsi.

---

# 43. Membuat Debug UI

Debug UI sangat penting dalam praktikum DDA.

Tanpa Debug UI mahasiswa hanya melihat bahwa enemy berubah, tetapi sulit mengetahui:

\`\`\`text
Mengapa difficulty berubah?
Skill score berapa?
Difficulty sekarang berapa?
\`\`\`

---

# 44. Membuat Canvas

Pilih:

\`\`\`text
GameObject
→ UI
→ Canvas
\`\`\`

Rename:

\`\`\`text
DebugCanvas
\`\`\`

Tambahkan:

\`\`\`text
UI
→ Text - TextMeshPro
\`\`\`

Jika diminta:

\`\`\`text
Import TMP Essentials
\`\`\`

pilih:

\`\`\`text
Import
\`\`\`

Rename Text:

\`\`\`text
DDADebugText
\`\`\`

Posisikan di kiri atas.

---

# 45. TextMeshPro

**TextMeshPro** adalah sistem rendering text Unity yang memberikan kualitas dan kontrol lebih baik daripada komponen text lama.

TextMeshPro dapat mengatur:

- font,
- ukuran,
- alignment,
- spacing,
- material,
- rich text.

---

# 46. DDADebugUI.cs

Buat:

\`\`\`csharp
using TMPro;
using UnityEngine;

public class DDADebugUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private PlayerPerformanceTracker tracker;

    [SerializeField]
    private DifficultyManager difficultyManager;

    [SerializeField]
    private EnemySpawner enemySpawner;

    private void Update()
    {
        if (debugText == null ||
            playerHealth == null ||
            tracker == null ||
            difficultyManager == null ||
            enemySpawner == null)
        {
            return;
        }

        debugText.text =
            $"DDA DEBUG\\n" +
            $"------------------------\\n" +
            $"Health: " +
            $"{playerHealth.CurrentHealth:F0}/" +
            $"{playerHealth.MaxHealth:F0}\\n" +

            $"Kills Window: " +
            $"{tracker.KillsInWindow}\\n" +

            $"Damage Window: " +
            $"{tracker.DamageTakenInWindow:F0}\\n" +

            $"Shots: " +
            $"{tracker.ShotsInWindow}\\n" +

            $"Hits: " +
            $"{tracker.HitsInWindow}\\n" +

            $"Skill Score: " +
            $"{tracker.LastSkillScore:F2}\\n" +

            $"Difficulty: " +
            $"{difficultyManager.DifficultyMultiplier:F2}\\n" +

            $"Spawn Interval: " +
            $"{enemySpawner.CurrentSpawnInterval:F2}s\\n" +

            $"Adjustment: " +
            $"{difficultyManager.LastAdjustment}";
    }
}
\`\`\`

---

# 47. Memasang DDADebugUI

Pasang script pada:

\`\`\`text
DebugCanvas
\`\`\`

Assign:

\`\`\`text
Debug Text
→ DDADebugText

Player Health
→ Player

Tracker
→ PlayerPerformanceTracker

Difficulty Manager
→ GameManager

Enemy Spawner
→ EnemySpawner
\`\`\`

---

# 48. Contoh Tampilan

Saat game berjalan:

\`\`\`text
DDA DEBUG
------------------------
Health: 82/100
Kills Window: 4
Damage Window: 18
Shots: 12
Hits: 9
Skill Score: 0.72
Difficulty: 1.10
Spawn Interval: 4.09s
Adjustment: Increasing
\`\`\`

---

# 49. Masalah LastSkillScore

Perhatikan bahwa:

\`\`\`text
LastSkillScore
\`\`\`

baru dihitung ketika evaluation berlangsung.

Jadi pada 30 detik pertama dapat terlihat:

\`\`\`text
Skill Score = 0.50
\`\`\`

Ini normal karena nilai awal didefinisikan:

\`\`\`csharp
0.5f
\`\`\`

---

# 50. Alur Lengkap Sistem

Game dimulai:

\`\`\`text
difficulty = 1.0
\`\`\`

EnemySpawner membaca difficulty:

\`\`\`text
HP = 100
Damage = 10
Spawn Interval = 4.5 s
\`\`\`

Player bermain selama 30 detik.

Tracker mencatat:

\`\`\`text
Health
Kills
Damage
Shots
Hits
\`\`\`

Setelah 30 detik:

\`\`\`text
DifficultyManager
       ↓
CalculateSkillScore()
\`\`\`

Misalnya:

\`\`\`text
Skill Score = 0.81
\`\`\`

Karena:

\`\`\`text
0.81 > 0.75
\`\`\`

maka:

\`\`\`text
Difficulty:
1.00 → 1.05
\`\`\`

Enemy baru selanjutnya menggunakan:

\`\`\`text
HP = 105
Damage ≈ 10.25
Speed sedikit meningkat
Spawn interval ≈ 4.29s
\`\`\`

---

# 51. Contoh Difficulty Meningkat

Misalnya:

\`\`\`text
Difficulty = 1.25
\`\`\`

Base enemy:

\`\`\`text
Health = 100
Damage = 10
Speed = 3
Spawn Interval = 4.5
\`\`\`

## Health

\`\`\`text
100 × 1.25
= 125
\`\`\`

## Damage

Damage menggunakan scaling lebih lembut:

\`\`\`text
Multiplier kira-kira 1.125
\`\`\`

sehingga:

\`\`\`text
Damage ≈ 11.25
\`\`\`

## Speed

Pengaruh speed hanya 25%.

Enemy tidak langsung menjadi terlalu cepat.

## Spawn interval

\`\`\`text
4.5 / 1.25
= 3.6 detik
\`\`\`

---

# 52. Contoh Difficulty Menurun

Misalnya:

\`\`\`text
Difficulty = 0.80
\`\`\`

Enemy menjadi lebih mudah.

Health:

\`\`\`text
100 × 0.80 = 80
\`\`\`

Spawn interval:

\`\`\`text
4.5 / 0.80
= 5.625 detik
\`\`\`

Enemy muncul lebih jarang.

---

# 53. Menguji DDA dengan Benar

Jangan hanya bermain secara normal.

Lakukan tiga skenario.

## Test A — Player Dominan

Lakukan:

- hindari damage,
- tembak enemy dengan akurat,
- bunuh enemy sebanyak mungkin.

Harapan:

\`\`\`text
Skill Score > 0.75
Difficulty naik
\`\`\`

---

## Test B — Player Kesulitan

Lakukan:

- biarkan enemy menyerang,
- sedikit membunuh enemy,
- sengaja banyak miss.

Harapan:

\`\`\`text
Skill Score < 0.35
Difficulty turun
\`\`\`

---

## Test C — Player Normal

Bermain biasa.

Harapan:

\`\`\`text
0.35 <= Skill Score <= 0.75
Difficulty relatif tetap
\`\`\`

---

# 54. Mempercepat Pengujian

30 detik cukup baik untuk gameplay, tetapi agak lama untuk debugging.

Saat pengembangan, sementara ubah:

\`\`\`text
Evaluation Interval = 10
\`\`\`

Setelah selesai debugging, kembalikan:

\`\`\`text
30
\`\`\`

---

# 55. DDA Oscillation

Perhatikan kemungkinan:

\`\`\`text
Skill tinggi
↓
Difficulty naik
↓
Player mulai kesulitan
↓
Difficulty turun
↓
Player kembali dominan
↓
Difficulty naik
\`\`\`

Jika terjadi terlalu sering, disebut:

**Oscillation**

---

# 56. Cara Mengurangi Oscillation

Praktikum sudah menggunakan beberapa mekanisme:

### 1. Evaluation Window

\`\`\`text
30 detik
\`\`\`

### 2. Tolerance Range

\`\`\`text
0.35 – 0.75
\`\`\`

tidak menyebabkan perubahan.

### 3. Adjustment Step

\`\`\`text
0.05
\`\`\`

perubahan sangat kecil.

### 4. Min/Max

\`\`\`text
0.75 – 1.50
\`\`\`

---

# 57. Tolerance atau Dead Zone

Rentang:

\`\`\`text
0.35 <= SkillScore <= 0.75
\`\`\`

dapat disebut:

\`\`\`text
Tolerance Range
\`\`\`

atau secara konsep kontrol:

\`\`\`text
Dead Zone
\`\`\`

Selama performance masih berada di zona ini:

\`\`\`text
Difficulty tidak berubah.
\`\`\`

Ini penting untuk menjaga kestabilan.

---

# 58. Smoothing

**Smoothing** berarti mencegah perubahan parameter terlalu mendadak.

Dalam praktikum, smoothing dilakukan secara sederhana melalui:

\`\`\`text
evaluation interval
+
adjustment step kecil
+
tolerance
\`\`\`

Sistem lebih lanjut dapat menggunakan:

\`\`\`text
Moving Average
Exponential Moving Average
Rolling Window
\`\`\`

---

# 59. Mengapa Tidak Mengevaluasi Setiap Frame?

Misalnya player terkena damage satu kali.

Jika DDA dievaluasi setiap frame:

\`\`\`text
Damage terjadi
 ↓
Score turun
 ↓
Difficulty turun
\`\`\`

beberapa frame berikutnya:

\`\`\`text
Player membunuh enemy
 ↓
Score naik
 ↓
Difficulty naik
\`\`\`

Hasilnya:

\`\`\`text
difficulty tidak stabil
\`\`\`

Karena itu DDA lebih baik bekerja pada:

\`\`\`text
wave
room
checkpoint
atau time window
\`\`\`

---

# 60. Parameter Adaptation

Praktikum sekarang mempunyai:

\`\`\`text
DifficultyManager
   │
   ├── Enemy Health
   ├── Enemy Damage
   ├── Enemy Speed
   └── Spawn Interval
\`\`\`

Inilah contoh:

**Parameter Adaptation**

Artinya sistem tidak perlu mengganti seluruh AI.

Cukup mengubah beberapa parameter gameplay.

---

# 61. Invisible Adaptation

Pada project saat ini, sebagian besar DDA bersifat:

\`\`\`text
Invisible Adaptation
\`\`\`

Player tidak diberitahu langsung bahwa:

\`\`\`text
Enemy HP berubah dari 100 menjadi 105.
\`\`\`

Namun Debug UI menampilkan data karena project ini digunakan untuk pembelajaran.

Pada game final Debug UI biasanya disembunyikan.

---

# 62. Diegetic Adaptation

Pengembangan lebih lanjut dapat membuat DDA terlihat lebih natural.

Contoh difficulty meningkat:

\`\`\`text
"Enemy reinforcement incoming!"
\`\`\`

Enemy tambahan masuk ke arena.

Difficulty turun:

\`\`\`text
"Supply drone incoming!"
\`\`\`

health pack muncul.

Dengan demikian adaptation memiliki alasan di dalam dunia game.

Ini disebut:

**Diegetic Adaptation**

---

# 63. Rubber Banding

Rubber banding adalah bentuk adaptasi yang membantu pihak yang tertinggal agar kompetisi tetap menarik.

Dalam arena survival konsep serupa dapat dibuat:

\`\`\`text
Player health < 25%
        ↓
Health pickup chance meningkat
\`\`\`

atau:

\`\`\`text
Player kesulitan berat
        ↓
Enemy spawn sedikit diperlambat
\`\`\`

Rubber banding harus digunakan secara hati-hati.

Jika terlalu kuat, player dapat merasa game memanipulasi hasil.

---

# 64. Rekomendasi Terbaik untuk Praktikum Ini

Untuk Praktikum 11, gunakan desain:

# Multi-Metric DDA + Continuous Difficulty Multiplier + Evaluation Window

Konfigurasi:

\`\`\`text
Metrics:
Health
Kills
Damage Taken
Accuracy
\`\`\`

Difficulty:

\`\`\`text
Continuous multiplier
0.75 – 1.50
\`\`\`

Adjustment:

\`\`\`text
±0.05
\`\`\`

Evaluation:

\`\`\`text
setiap 30 detik
\`\`\`

Parameter adaptation:

\`\`\`text
Enemy Health
Enemy Damage
Enemy Speed ringan
Spawn Interval
\`\`\`

Debug:

\`\`\`text
wajib menggunakan Debug UI
\`\`\`

---

# 65. Mengapa Pilihan Ini Direkomendasikan?

## Dibanding hanya Health

Jika hanya:

\`\`\`text
if health < 30%
    difficulty turun
\`\`\`

sistem terlalu sederhana.

Player ahli yang sengaja menggunakan gaya bermain berisiko bisa selalu dianggap buruk.

---

## Dibanding terlalu banyak metrics

Jika mahasiswa langsung menggunakan:

\`\`\`text
health
kills
accuracy
death
resource
combo
dodge
time
distance
progress
ammo
\`\`\`

sistem menjadi sulit di-debug.

Empat metrics sudah cukup untuk menjelaskan konsep.

---

## Dibanding Difficulty Level 1–5

Difficulty diskrit lebih mudah.

Namun multiplier kontinu:

\`\`\`text
1.00
1.05
1.10
\`\`\`

lebih baik untuk menunjukkan **parameter adaptation bertahap**.

---

# 66. Eksperimen 1 — Static vs Dynamic Difficulty

Nonaktifkan sementara:

\`\`\`text
DifficultyManager
\`\`\`

Gunakan:

\`\`\`text
difficulty = 1.0
\`\`\`

Mainkan game.

Kemudian aktifkan DDA.

Bandingkan:

\`\`\`text
Static Difficulty
vs
Dynamic Difficulty
\`\`\`

Diskusikan perubahan pengalaman bermain.

---

# 67. Eksperimen 2 — Adjustment Step

Uji:

\`\`\`text
0.05
\`\`\`

kemudian:

\`\`\`text
0.20
\`\`\`

Amati.

Pertanyaan:

- mana yang terasa lebih halus?
- mana yang lebih mudah mengalami perubahan ekstrem?

Harapan:

\`\`\`text
0.05
\`\`\`

lebih stabil.

---

# 68. Eksperimen 3 — Evaluation Interval

Bandingkan:

\`\`\`text
5 detik
10 detik
30 detik
60 detik
\`\`\`

Pertanyaan:

- apakah 5 detik terlalu reaktif?
- apakah 60 detik terlalu lambat?
- berapa interval yang paling cocok?

---

# 69. Eksperimen 4 — Single Metric

Ubah sementara skill score menjadi:

\`\`\`csharp
lastSkillScore = healthScore;
\`\`\`

Bandingkan dengan multi-metric.

Pertanyaan:

\`\`\`text
Apakah health saja cukup
menggambarkan kemampuan player?
\`\`\`

---

# 70. Eksperimen 5 — Enemy Health Saja

Nonaktifkan adaptasi:

\`\`\`text
damage
speed
spawn interval
\`\`\`

Adaptasikan hanya:

\`\`\`text
Enemy Health
\`\`\`

Bandingkan gameplay.

---

# 71. Eksperimen 6 — Spawn Interval Saja

Gunakan:

\`\`\`text
Enemy stats tetap
\`\`\`

tetapi:

\`\`\`text
spawnInterval =
baseSpawnInterval /
difficultyMultiplier
\`\`\`

Amati apakah perubahan difficulty masih terasa.

---

# 72. Eksperimen 7 — Difficulty Range

Bandingkan:

\`\`\`text
0.75 – 1.50
\`\`\`

dengan:

\`\`\`text
0.25 – 3.00
\`\`\`

Range kedua sengaja ekstrem.

Diskusikan mengapa DDA memerlukan:

\`\`\`text
minimum
maximum
\`\`\`

---

# 73. Eksperimen 8 — Threshold

Bandingkan:

\`\`\`text
Lower = 0.35
Upper = 0.75
\`\`\`

dengan:

\`\`\`text
Lower = 0.49
Upper = 0.51
\`\`\`

Rentang kedua sangat sempit.

Harapan:

\`\`\`text
difficulty lebih sering berubah
\`\`\`

sehingga risiko oscillation meningkat.

---

# 74. Tugas Pengembangan — Health Pickup Adaptif

Tambahkan:

\`\`\`text
HealthPickup
\`\`\`

Aturan:

\`\`\`text
Difficulty rendah
        ↓
Health pickup lebih sering
\`\`\`

atau:

\`\`\`text
Player health rendah
        ↓
drop chance meningkat
\`\`\`

Contoh desain:

\`\`\`text
Base health drop chance = 10%

Difficulty 0.75
→ 20%

Difficulty 1.00
→ 10%

Difficulty 1.50
→ 5%
\`\`\`

---

# 75. Tugas Pengembangan — Moving Average

Simpan beberapa \`skillScore\` terakhir:

\`\`\`text
0.60
0.72
0.81
0.70
\`\`\`

Kemudian hitung rata-rata.

\`\`\`text
Average =
(0.60 + 0.72 + 0.81 + 0.70) / 4
\`\`\`

Difficulty menggunakan rata-rata tersebut.

Bandingkan stabilitasnya dengan sistem awal.

---

# 76. Tugas Pengembangan — Threat Level

Tambahkan UI:

\`\`\`text
Threat Level
\`\`\`

Contoh:

\`\`\`text
0.75 – 0.90
LOW

0.90 – 1.10
NORMAL

1.10 – 1.30
HIGH

1.30 – 1.50
EXTREME
\`\`\`

---

# 77. Tugas Pengembangan — Visual Feedback

Gunakan warna pada Threat Level.

Contoh konsep:

\`\`\`text
LOW       → hijau
NORMAL    → kuning
HIGH      → oranye
EXTREME   → merah
\`\`\`

Tujuannya bukan hanya estetika, tetapi membantu mahasiswa melihat perubahan sistem.

---

# 78. Tugas Pengembangan — Wave-Based DDA

Saat ini DDA dievaluasi:

\`\`\`text
setiap 30 detik
\`\`\`

Modifikasi menjadi:

\`\`\`text
Wave selesai
      ↓
Evaluate Performance
      ↓
Adjust Difficulty
      ↓
Start next wave
\`\`\`

Metode ini sering lebih natural untuk game wave survival.

---

# 79. Tugas Pengembangan — Integrasi FSM

Jika menggunakan materi FSM sebelumnya:

\`\`\`text
Enemy FSM
├── Chase
├── Attack
└── Retreat
\`\`\`

DDA dapat mengubah:

\`\`\`text
attackRange
attackCooldown
chaseSpeed
\`\`\`

Difficulty tinggi:

\`\`\`text
Attack lebih sering
Chase lebih cepat
\`\`\`

---

# 80. Tugas Pengembangan — Behavior Tree

Jika enemy menggunakan Behavior Tree:

DDA dapat mengubah:

\`\`\`text
Attack Cooldown
Condition Threshold
Action Parameter
\`\`\`

Contoh:

\`\`\`text
Difficulty rendah:
attackCooldown = 2.0

Normal:
attackCooldown = 1.5

Difficulty tinggi:
attackCooldown = 1.0
\`\`\`

---

# 81. Tugas Pengembangan — Utility AI

Jika menggunakan Utility AI:

Difficulty dapat memengaruhi:

\`\`\`text
attackWeight
fleeWeight
coverWeight
\`\`\`

Difficulty tinggi:

\`\`\`text
attackWeight meningkat
fleeWeight menurun
\`\`\`

Enemy menjadi lebih agresif tanpa harus sekadar meningkatkan HP.

---

# 82. Tugas Pengembangan — Integrasi PCG

Hubungkan dengan Praktikum PCG.

Contoh:

\`\`\`text
Difficulty tinggi
        ↓
Enemy count lebih banyak
Trap lebih banyak
Loot lebih sedikit
\`\`\`

Difficulty rendah:

\`\`\`text
Enemy lebih sedikit
Health item lebih banyak
\`\`\`

Dengan demikian:

\`\`\`text
DDA
↓
mengontrol
↓
PCG
\`\`\`

---

# 83. Kesalahan Umum

## Error 1 — Enemy tidak muncul

Periksa:

\`\`\`text
Enemy Prefab sudah diassign?
Spawn Point tersedia?
Player sudah diassign?
DifficultyManager sudah diassign?
\`\`\`

---

## Error 2 — NullReferenceException

Biasanya terdapat field Inspector yang belum diisi.

Klik pesan error pada Console dan periksa object terkait.

---

## Error 3 — Enemy tidak bisa ditembak

Periksa:

\`\`\`text
Enemy memiliki Collider?
EnemyHealth terpasang?
ShootOrigin menghadap ke depan?
\`\`\`

---

## Error 4 — Kill tidak bertambah

Pastikan:

\`\`\`text
EnemyHealth.Initialize()
\`\`\`

menerima:

\`\`\`text
PlayerPerformanceTracker
\`\`\`

---

## Error 5 — Damage tidak tercatat

Pastikan enemy menyerang melalui:

\`\`\`csharp
PlayerHealth.TakeDamage()
\`\`\`

karena fungsi tersebut meneruskan informasi ke tracker.

---

## Error 6 — Difficulty tidak pernah berubah

Periksa:

\`\`\`text
Skill score
Threshold
Evaluation interval
DifficultyManager aktif
PerformanceTracker reference
\`\`\`

Sementara gunakan:

\`\`\`text
Evaluation Interval = 10
\`\`\`

untuk mempercepat pengujian.

---

## Error 7 — Enemy terlalu sulit

Jangan meningkatkan semua parameter terlalu agresif.

Kurangi:

\`\`\`text
Damage scaling
Speed scaling
Base Max Enemies
\`\`\`

---

## Error 8 — Difficulty selalu naik

Periksa normalization.

Jika:

\`\`\`text
expectedKills
\`\`\`

terlalu kecil, kill score akan selalu mendekati 1.

---

## Error 9 — Difficulty selalu turun

Mungkin:

\`\`\`text
maxExpectedDamage
\`\`\`

terlalu kecil.

Damage score akan cepat menjadi:

\`\`\`text
0
\`\`\`

---

# 84. Debugging dengan Console

\`Debug.Log()\` pada DifficultyManager menghasilkan:

\`\`\`text
Skill Score: 0.78 |
Difficulty: 1.10 |
Adjustment: Increasing
\`\`\`

Gunakan:

\`\`\`text
Window
→ General
→ Console
\`\`\`

untuk melihat log.

---

# 85. Inspector sebagai Balancing Tool

Jangan hard-code seluruh nilai.

Gunakan:

\`\`\`csharp
[SerializeField]
\`\`\`

untuk parameter:

\`\`\`text
Base Enemy Health
Base Damage
Base Speed
Spawn Interval
Min Difficulty
Max Difficulty
Adjustment Step
Threshold
Evaluation Interval
\`\`\`

Dengan demikian mahasiswa dapat melakukan eksperimen tanpa mengubah script.

---

# 86. Checklist Implementasi

- [ ] Project \`GC11_AdaptiveArenaDDA\` dibuat.
- [ ] Scene \`MainArena\` dibuat.
- [ ] Arena floor dibuat.
- [ ] Player dibuat.
- [ ] CharacterController terpasang.
- [ ] PlayerController berfungsi.
- [ ] Player dapat dikontrol WASD/Arrow.
- [ ] Player dapat diarahkan dengan mouse.
- [ ] Player dapat menembak.
- [ ] PlayerHealth berfungsi.
- [ ] Enemy prefab dibuat.
- [ ] EnemyAI berfungsi.
- [ ] EnemyHealth berfungsi.
- [ ] Enemy dapat menyerang player.
- [ ] Enemy dapat mati.
- [ ] Enemy memiliki Tag \`Enemy\`.
- [ ] Spawn points dibuat.
- [ ] EnemySpawner berfungsi.
- [ ] PlayerPerformanceTracker berfungsi.
- [ ] Kill tercatat.
- [ ] Damage tercatat.
- [ ] Shots tercatat.
- [ ] Hits tercatat.
- [ ] Skill score dapat dihitung.
- [ ] DifficultyManager dibuat.
- [ ] Difficulty memiliki min/max.
- [ ] Difficulty berubah bertahap.
- [ ] Evaluation window berfungsi.
- [ ] Enemy health adaptif.
- [ ] Enemy damage adaptif.
- [ ] Enemy speed adaptif secara ringan.
- [ ] Spawn interval adaptif.
- [ ] Debug UI dibuat.
- [ ] Skill score tampil.
- [ ] Difficulty multiplier tampil.
- [ ] Adjustment status tampil.
- [ ] Kondisi dominant player diuji.
- [ ] Kondisi struggling player diuji.
- [ ] Kondisi balanced diuji.

---

# 87. Pertanyaan Analisis Praktikum

Jawab setelah melakukan eksperimen.

1. Apa perbedaan static difficulty dan dynamic difficulty?
2. Mengapa performa player tidak sebaiknya diukur hanya dari health?
3. Mengapa metrik perlu dinormalisasi?
4. Apa fungsi \`skillScore\`?
5. Apa fungsi \`difficultyMultiplier\`?
6. Mengapa difficulty tidak boleh dievaluasi setiap frame?
7. Apa fungsi \`evaluationInterval\`?
8. Apa fungsi \`adjustmentStep\`?
9. Mengapa perlu \`minDifficulty\` dan \`maxDifficulty\`?
10. Apa yang dimaksud oscillation dalam DDA?
11. Bagaimana tolerance range mengurangi oscillation?
12. Mengapa seluruh parameter enemy sebaiknya tidak dinaikkan secara agresif sekaligus?
13. Mengapa Debug UI penting saat mengembangkan DDA?
14. Apa keuntungan menggunakan multi-metric dibanding single metric?
15. Apakah player yang sangat ahli seharusnya selalu diberi enemy yang semakin kuat?
16. Kapan DDA dapat dianggap tidak adil?
17. Apa hubungan DDA dengan rubber banding?
18. Bagaimana DDA dapat dikombinasikan dengan Behavior Tree?
19. Bagaimana DDA dapat dikombinasikan dengan Utility AI?
20. Bagaimana DDA dapat mengontrol PCG?

---

# 88. Tugas Eksperimen Mahasiswa

Lakukan minimal tiga konfigurasi.

## Konfigurasi A

\`\`\`text
Evaluation = 10s
Step = 0.10
Range = 0.75–1.50
\`\`\`

## Konfigurasi B

\`\`\`text
Evaluation = 30s
Step = 0.05
Range = 0.75–1.50
\`\`\`

## Konfigurasi C

\`\`\`text
Evaluation = 60s
Step = 0.05
Range = 0.75–1.50
\`\`\`

Catat:

\`\`\`text
Skill Score
Difficulty
Enemy Spawn Interval
Pengalaman gameplay
\`\`\`

Bandingkan kestabilannya.

---

# 89. Format Tabel Pengamatan

| Waktu | Skill Score | Difficulty | Spawn Interval | Adjustment |
|---|---:|---:|---:|---|
| 30 s | | | | |
| 60 s | | | | |
| 90 s | | | | |
| 120 s | | | | |
| 150 s | | | | |

Mahasiswa dapat menggunakan tabel ini sebagai data analisis.

---

# 90. Analisis Fairness

Setelah memainkan game, jawab:

\`\`\`text
Apakah kenaikan difficulty terasa natural?
\`\`\`

\`\`\`text
Apakah player seperti dihukum karena bermain baik?
\`\`\`

\`\`\`text
Apakah penurunan difficulty terlalu mudah diketahui?
\`\`\`

\`\`\`text
Apakah kemenangan masih terasa berasal dari kemampuan player?
\`\`\`

Ini penting karena DDA bukan hanya persoalan pemrograman.

DDA juga merupakan persoalan:

\`\`\`text
Game Design
Player Experience
Fairness
Player Agency
\`\`\`

---

# 91. Rekomendasi Parameter Awal Final

Gunakan konfigurasi berikut sebagai baseline:

\`\`\`text
PLAYER
Max Health              = 100
Weapon Damage           = 35
Movement Speed          = 6

ENEMY
Base Health             = 100
Base Damage             = 10
Base Speed              = 3

SPAWNER
Base Spawn Interval     = 4.5
Base Max Enemies        = 5

PERFORMANCE
Expected Kills          = 5
Max Expected Damage     = 60

DDA
Initial Difficulty      = 1.00
Min Difficulty          = 0.75
Max Difficulty          = 1.50
Adjustment Step         = 0.05

SKILL THRESHOLD
Lower Threshold         = 0.35
Upper Threshold         = 0.75

EVALUATION
Evaluation Interval     = 30 seconds
\`\`\`

Parameter ini bukan angka absolut yang harus selalu digunakan.

Mahasiswa diperbolehkan melakukan balancing berdasarkan hasil eksperimen.

---

# 92. Rekomendasi Arsitektur

Pembagian tanggung jawab script:

## PlayerPerformanceTracker

\`\`\`text
Mengumpulkan data.
Tidak mengubah difficulty.
\`\`\`

## DifficultyManager

\`\`\`text
Menganalisis performance.
Mengelola difficulty.
Tidak melakukan spawning.
\`\`\`

## EnemySpawner

\`\`\`text
Membuat enemy.
Menggunakan difficulty.
Tidak menghitung skill score.
\`\`\`

## EnemyAI

\`\`\`text
Mengontrol perilaku enemy.
\`\`\`

## DDADebugUI

\`\`\`text
Menampilkan data.
Tidak mengubah gameplay.
\`\`\`

Pemisahan ini penting karena menerapkan konsep:

**Separation of Concerns**

Setiap class mempunyai tanggung jawab yang jelas.

---

# 93. Mengapa Arsitektur Modular Penting?

Hindari membuat satu script:

\`\`\`text
GameManager.cs
\`\`\`

yang sekaligus:

\`\`\`text
menggerakkan player
spawn enemy
menghitung HP
menghitung kill
menghitung difficulty
mengendalikan enemy
mengupdate UI
\`\`\`

Walaupun mungkin berjalan, kode akan:

- sulit dibaca,
- sulit diuji,
- sulit dikembangkan,
- sulit di-debug.

Arsitektur modular membuat praktikum lebih dekat dengan praktik pengembangan game yang baik.

---

# 94. Konsep Feedback Loop

DDA merupakan sebuah:

**Feedback Loop**

\`\`\`text
PLAYER ACTION
      ↓
GAMEPLAY DATA
      ↓
PERFORMANCE ANALYSIS
      ↓
DIFFICULTY ADJUSTMENT
      ↓
GAMEPLAY CHANGES
      ↓
PLAYER ACTION
      ↓
...
\`\`\`

Ini adalah konsep penting dari adaptive game intelligence.

---

# 95. Hubungan dengan Pertemuan Sebelumnya

Praktikum sebelumnya membahas AI yang berfokus pada:

\`\`\`text
NPC Intelligence
\`\`\`

seperti:

\`\`\`text
Perception
Movement
Pathfinding
FSM
Behavior Tree
Utility AI
Tactical AI
\`\`\`

Kemudian PCG berfokus pada:

\`\`\`text
Content Intelligence
\`\`\`

Praktikum 11 mulai masuk ke:

\`\`\`text
Adaptive Game Intelligence
\`\`\`

Game tidak hanya membuat NPC cerdas.

Game juga:

\`\`\`text
mengamati player
menganalisis player
menyesuaikan pengalaman bermain
\`\`\`

---

# 96. Hubungan dengan Pertemuan 12

Praktikum 11 menggunakan data player untuk menjawab:

\`\`\`text
"Bagaimana performa player sekarang?"
\`\`\`

Pertemuan berikutnya tentang:

\`\`\`text
Player Modeling & Adaptive Game AI
\`\`\`

akan memperluas pertanyaan menjadi:

\`\`\`text
"Player seperti apa yang sedang bermain?"
\`\`\`

Misalnya:

\`\`\`text
Beginner
Expert
Aggressive
Defensive
Explorer
Risk-taker
\`\`\`

Dengan demikian:

\`\`\`text
Performance Tracking
        ↓
DDA
        ↓
Player Modeling
        ↓
Adaptive Game AI
\`\`\`

---

# 97. Ringkasan

Pada praktikum ini mahasiswa telah membangun:

\`\`\`text
Adaptive Arena Survival
\`\`\`

dengan sistem:

\`\`\`text
Player
↓
Performance Tracking
↓
Normalization
↓
Skill Score
↓
Difficulty Manager
↓
Difficulty Multiplier
↓
Enemy Scaling
↓
Adaptive Spawn
↓
Gameplay Feedback
\`\`\`

Komponen utama:

\`\`\`text
PlayerPerformanceTracker
DifficultyManager
EnemySpawner
EnemyHealth
EnemyAI
DDADebugUI
\`\`\`

Konsep utama:

\`\`\`text
Player Performance
Multi-Metric Evaluation
Normalization
Skill Score
Difficulty Model
Adaptive Gameplay
Parameter Adaptation
Evaluation Window
Smoothing
Tolerance
Difficulty Bounds
Rubber Banding
Fairness
Player Agency
\`\`\`

---

# 98. Kesimpulan

Dynamic Difficulty Adjustment bukan sekadar:

\`\`\`text
"Jika player hebat, tambah HP musuh."
\`\`\`

DDA merupakan sistem adaptif yang terdiri dari:

\`\`\`text
Observe
   ↓
Measure
   ↓
Analyze
   ↓
Adapt
   ↓
Observe Again
\`\`\`

Sistem yang baik harus:

- membaca performa player secara masuk akal,
- menggunakan metrik yang relevan,
- menormalisasi data,
- melakukan perubahan secara bertahap,
- memiliki minimum dan maksimum difficulty,
- tidak berubah terlalu sering,
- menjaga fairness,
- mempertahankan player agency,
- serta tetap membuat game terasa menantang dan menyenangkan.

Prinsip paling penting:

> **DDA yang baik tidak bertujuan membuat player selalu menang atau selalu kesulitan. DDA bertujuan menjaga tingkat tantangan tetap sesuai sehingga gameplay tetap menarik, stabil, dan terasa adil.**`,Mc=`# MODUL PRAKTIKUM 12  
# Gameplay Metrics, Player Modeling & Adaptive Game AI

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Tools:** Unity 6 + C#  
**Pertemuan:** 12  
**Topik:** Player Modeling & Adaptive Game AI

---

# 1. Judul Praktikum

## Gameplay Metrics & Simple Player Model

Pada praktikum ini mahasiswa membuat sebuah game arena sederhana yang mampu:

1. merekam aktivitas player,
2. mengubah aktivitas tersebut menjadi gameplay metrics,
3. memperkirakan skill player,
4. mengenali kecenderungan play style,
5. membuat player profile,
6. menampilkan profile secara real-time,
7. dan menggunakan player profile untuk melakukan adaptasi gameplay sederhana.

---

# 2. Nama Project Unity yang Direkomendasikan

Nama project yang paling direkomendasikan:

\`\`\`text
GC12_AdaptivePlayerModeling
\`\`\`

Alternatif:

\`\`\`text
PlayerModelingArena
AdaptiveGameAI
AdaptivePlayerArena
GameplayTelemetryLab
SmartPlayerProfile
\`\`\`

## Rekomendasi Utama

\`\`\`text
GC12_AdaptivePlayerModeling
\`\`\`

Alasannya:

- \`GC12\` menunjukkan Game Cerdas Praktikum 12.
- \`AdaptivePlayerModeling\` menjelaskan inti sistem.
- nama cukup pendek.
- mudah dibedakan dari project DDA pada Praktikum 11.
- masih relevan jika project dikembangkan lebih lanjut.

---

# 3. Hubungan dengan Praktikum 11

Pada Praktikum 11 mahasiswa mempelajari:

\`\`\`text
Player Performance
       ↓
Dynamic Difficulty Adjustment
       ↓
Difficulty berubah
\`\`\`

Pada Praktikum 12 sistem diperluas menjadi:

\`\`\`text
Gameplay Event
      ↓
Telemetry
      ↓
Gameplay Metrics
      ↓
Player Model
      ↓
Skill + Play Style
      ↓
Adaptation Policy
      ↓
Adaptive Game AI
\`\`\`

Perbedaan utamanya:

| Praktikum 11 | Praktikum 12 |
|---|---|
| Fokus pada difficulty | Fokus memahami karakteristik player |
| Mengukur apakah game terlalu sulit/mudah | Mengukur skill dan gaya bermain |
| Output berupa difficulty | Output berupa player profile |
| Adaptasi terutama difficulty | Adaptasi dapat memengaruhi AI, resource, level, reward |
| Model relatif sederhana | Model memiliki beberapa metrics |

---

# 4. Rekomendasi Bentuk Praktikum

## Pilihan terbaik: Adaptive Arena Survival

Mahasiswa membuat sebuah arena kecil dengan:

- Player.
- beberapa Enemy.
- Health Pickup.
- beberapa area eksplorasi.
- shooting sederhana.
- telemetry system.
- player modeling.
- debug UI.
- adaptive enemy parameter.

### Kenapa Arena Survival?

Arena Survival sangat cocok karena memberikan cukup banyak data:

\`\`\`text
Combat
→ shooting
→ hit
→ kill
→ damage dealt

Survival
→ health
→ damage taken
→ death

Exploration
→ area visited
→ item collected

Progress
→ survival time
→ objective
\`\`\`

Dengan satu scene sederhana mahasiswa sudah dapat mempelajari hampir seluruh konsep penting player modeling.

---

# 5. Scope yang Direkomendasikan

Jangan langsung membuat sistem terlalu kompleks.

Untuk praktikum ini cukup gunakan empat kelompok data:

\`\`\`text
Combat Metric
Survival Metric
Exploration Metric
Progress Metric
\`\`\`

Kemudian model player menghasilkan:

\`\`\`text
Skill Score
Skill Level

Aggressive Score
Defensive Score
Explorer Score

Dominant Style
\`\`\`

Adaptasi cukup menggunakan:

\`\`\`text
Skill → Enemy Damage / Spawn Rate

Play Style → Enemy Speed / Bonus Reward
\`\`\`

Ini merupakan kombinasi terbaik antara:

- konsep akademik,
- kemudahan implementasi,
- waktu praktikum,
- kemampuan debugging,
- dan hasil yang mudah diamati.

---

# 6. Tujuan Praktikum

Setelah praktikum mahasiswa diharapkan mampu:

1. memahami konsep player telemetry;
2. membedakan telemetry dan gameplay metrics;
3. menangkap event gameplay melalui script Unity;
4. menghitung metrics dari data permainan;
5. melakukan normalisasi nilai;
6. menghitung skill score;
7. menentukan skill level;
8. menghitung play style score;
9. menentukan dominant play style;
10. membuat player profile;
11. membuat adaptation policy sederhana;
12. mengubah parameter game berdasarkan player model;
13. menampilkan telemetry dan player profile melalui UI;
14. menguji kestabilan player model.

---

# 7. Konsep Utama

## 7.1 Player Telemetry

**Player Telemetry** adalah data yang direkam dari aktivitas player selama permainan.

Contoh:

\`\`\`text
Player menembak
Player mengenai enemy
Player menerima damage
Enemy mati
Player mengambil item
Player masuk area tertentu
Player mati
\`\`\`

Telemetry masih merupakan **data mentah**.

Contoh:

\`\`\`text
Shots Fired = 20
Shots Hit = 12
Enemies Killed = 3
Damage Taken = 45
\`\`\`

---

# 8. Telemetry vs Gameplay Metrics

Telemetry:

\`\`\`text
Shots Fired = 20
Shots Hit = 12
\`\`\`

Gameplay metric:

\`\`\`text
Accuracy
= Shots Hit / Shots Fired
= 12 / 20
= 0.60
\`\`\`

Jadi:

\`\`\`text
Telemetry
    ↓
Calculation
    ↓
Metrics
\`\`\`

Telemetry adalah **apa yang terjadi**.

Metrics adalah **informasi yang diperoleh dari apa yang terjadi tersebut**.

---

# 9. Player Modeling

Player Modeling merupakan proses membuat representasi tentang player berdasarkan data gameplay.

Contoh hasil:

\`\`\`text
PLAYER PROFILE

Skill Score      : 0.67
Skill Level      : Medium

Aggressive Score : 0.73
Defensive Score  : 0.42
Explorer Score   : 0.31

Dominant Style   : Aggressive
\`\`\`

Sistem tidak menyatakan bahwa player "pasti aggressive".

Sistem hanya melakukan **estimasi berdasarkan data yang tersedia**.

---

# 10. Skill dan Play Style Harus Dipisahkan

Ini merupakan konsep penting.

## Skill

Menjawab:

\`\`\`text
Seberapa baik kemampuan player?
\`\`\`

Contoh:

\`\`\`text
Low
Medium
High
\`\`\`

## Play Style

Menjawab:

\`\`\`text
Bagaimana cara player bermain?
\`\`\`

Contoh:

\`\`\`text
Aggressive
Defensive
Explorer
\`\`\`

Player dapat mempunyai kombinasi:

\`\`\`text
High Skill + Aggressive

High Skill + Defensive

Medium Skill + Explorer

Low Skill + Aggressive
\`\`\`

Karena itu damage received tinggi **tidak otomatis berarti player tidak terampil**.

Bisa saja player memang memilih bermain agresif.

---

# 11. Istilah Teknis Unity yang Digunakan

## GameObject

\`GameObject\` adalah objek dasar dalam Unity.

Contoh:

\`\`\`text
Player
Enemy
Main Camera
GameManager
Canvas
HealthPickup
\`\`\`

GameObject dapat memiliki beberapa Component.

---

# 12. Component

Component memberikan fungsi tertentu kepada GameObject.

Contoh:

\`\`\`text
Player
├── Transform
├── CharacterController
├── PlayerMovement
├── PlayerHealth
└── PlayerShooter
\`\`\`

Script C# yang dipasang pada GameObject juga merupakan Component.

---

# 13. MonoBehaviour

Sebagian besar script Unity diturunkan dari:

\`\`\`csharp
MonoBehaviour
\`\`\`

Contoh:

\`\`\`csharp
public class PlayerHealth : MonoBehaviour
{
}
\`\`\`

Dengan \`MonoBehaviour\`, script dapat menggunakan fungsi Unity seperti:

\`\`\`text
Awake()
Start()
Update()
OnTriggerEnter()
OnCollisionEnter()
\`\`\`

---

# 14. Transform

Setiap GameObject memiliki \`Transform\`.

Transform menyimpan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

Contoh:

\`\`\`csharp
transform.position
\`\`\`

mengembalikan posisi GameObject.

---

# 15. CharacterController

\`CharacterController\` digunakan untuk menggerakkan karakter tanpa harus menggunakan simulasi fisika penuh Rigidbody.

Pada praktikum ini digunakan untuk movement sederhana Player.

Keuntungannya:

- mudah digunakan;
- gerakan stabil;
- tidak memerlukan konfigurasi Rigidbody;
- cocok untuk prototype.

---

# 16. Collider

Collider mendefinisikan area fisik sebuah GameObject.

Contoh:

\`\`\`text
Box Collider
Sphere Collider
Capsule Collider
\`\`\`

Collider digunakan untuk:

- collision,
- trigger,
- raycast detection.

---

# 17. Trigger

Collider dapat menggunakan:

\`\`\`text
Is Trigger = true
\`\`\`

Trigger tidak menghalangi movement tetapi dapat mendeteksi object yang masuk.

Digunakan untuk:

\`\`\`text
Health Pickup
Exploration Zone
\`\`\`

Callback:

\`\`\`csharp
OnTriggerEnter(Collider other)
\`\`\`

---

# 18. Raycast

**Raycast** adalah garis tak terlihat yang dikirim dari suatu posisi ke suatu arah untuk mendeteksi objek.

Dalam praktikum:

\`\`\`text
Player
   ↓
Raycast
   ↓
Enemy
\`\`\`

Raycast digunakan untuk shooting sederhana.

---

# 19. LayerMask

LayerMask digunakan untuk membatasi object apa saja yang boleh dideteksi oleh Raycast.

Misalnya:

\`\`\`text
Layer:
Enemy
\`\`\`

Raycast Player hanya akan memeriksa layer \`Enemy\`.

Ini membuat deteksi lebih efisien dan lebih terkontrol.

---

# 20. Prefab

Prefab adalah template GameObject yang dapat digunakan berulang kali.

Contoh:

\`\`\`text
Enemy.prefab
HealthPickup.prefab
\`\`\`

Prefab sangat penting untuk sistem:

\`\`\`text
EnemySpawner
\`\`\`

karena enemy dapat dibuat melalui:

\`\`\`csharp
Instantiate()
\`\`\`

---

# 21. Singleton

Pada praktikum digunakan pola sederhana Singleton untuk:

\`\`\`text
TelemetryManager
PlayerModelManager
AdaptationManager
\`\`\`

Tujuannya agar script lain mudah mengakses manager.

Contoh:

\`\`\`csharp
TelemetryManager.Instance.RecordKill();
\`\`\`

Untuk project produksi besar dapat digunakan sistem dependency atau event yang lebih terstruktur.

Untuk praktikum, Singleton cukup sederhana dan mudah dipahami.

---

# 22. Mathf.Clamp01()

Fungsi:

\`\`\`csharp
Mathf.Clamp01(value)
\`\`\`

membatasi nilai menjadi:

\`\`\`text
0.0 sampai 1.0
\`\`\`

Contoh:

\`\`\`csharp
float score = Mathf.Clamp01(killRate / expectedKillRate);
\`\`\`

Sangat penting dalam proses **normalisasi metric**.

---

# 23. TextMeshPro

TextMeshPro digunakan untuk menampilkan:

\`\`\`text
Telemetry
Gameplay Metrics
Player Profile
Adaptation
\`\`\`

Class yang digunakan:

\`\`\`csharp
TMP_Text
\`\`\`

Namespace:

\`\`\`csharp
using TMPro;
\`\`\`

---

# 24. Struktur Folder Project

Buat struktur:

\`\`\`text
Assets/
│
├── Scenes/
│   └── PlayerModelingDemo.unity
│
├── Scripts/
│   ├── Core/
│   ├── Player/
│   ├── Enemy/
│   ├── Telemetry/
│   ├── PlayerModel/
│   ├── Adaptation/
│   ├── Items/
│   └── UI/
│
├── Prefabs/
│   ├── Enemy.prefab
│   └── HealthPickup.prefab
│
└── Materials/
\`\`\`

---

# 25. Struktur Scene

Hierarchy yang direkomendasikan:

\`\`\`text
PlayerModelingDemo
│
├── Systems
│   ├── GameManager
│   ├── TelemetryManager
│   ├── PlayerModelManager
│   ├── AdaptationManager
│   └── EnemySpawner
│
├── Player
│
├── Arena
│   ├── Floor
│   ├── Wall_North
│   ├── Wall_South
│   ├── Wall_East
│   └── Wall_West
│
├── ExplorationZones
│   ├── Zone_A
│   ├── Zone_B
│   ├── Zone_C
│   └── Zone_D
│
├── SpawnPoints
│
├── BonusReward
│
├── Main Camera
│
└── Canvas
    └── DebugPanel
\`\`\`

---

# 26. Langkah 1 — Membuat Project

Buka Unity Hub.

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D
\`\`\`

Nama:

\`\`\`text
GC12_AdaptivePlayerModeling
\`\`\`

Kemudian klik:

\`\`\`text
Create Project
\`\`\`

---

# 27. Langkah 2 — Membuat Scene

Simpan scene sebagai:

\`\`\`text
Assets/Scenes/PlayerModelingDemo.unity
\`\`\`

Buat Floor:

\`\`\`text
Hierarchy
→ Create
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
Floor
\`\`\`

Scale misalnya:

\`\`\`text
X = 3
Y = 1
Z = 3
\`\`\`

---

# 28. Langkah 3 — Membuat Player

Buat:

\`\`\`text
3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Position:

\`\`\`text
X = 0
Y = 1
Z = 0
\`\`\`

Tambahkan:

\`\`\`text
CharacterController
\`\`\`

Tag:

\`\`\`text
Player
\`\`\`

---

# 29. PlayerMovement.cs

Buat:

\`\`\`text
Assets/Scripts/Player/PlayerMovement.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;

        if (Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical).normalized;

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }
}
\`\`\`

Pasang pada Player.

Player sekarang dapat dikontrol menggunakan:

\`\`\`text
W A S D
\`\`\`

atau:

\`\`\`text
Arrow Keys
\`\`\`

---

# 30. Penjelasan Time.deltaTime

\`Time.deltaTime\` adalah waktu antara frame sekarang dengan frame sebelumnya.

Digunakan agar movement:

\`\`\`text
tidak bergantung pada FPS.
\`\`\`

Contoh:

\`\`\`csharp
moveSpeed * Time.deltaTime
\`\`\`

Tanpa \`Time.deltaTime\`, komputer dengan FPS tinggi dapat membuat Player bergerak lebih cepat.

---

# 31. Langkah 4 — TelemetryManager

Buat:

\`\`\`text
TelemetryManager.cs
\`\`\`

\`\`\`csharp
using System.Collections.Generic;
using UnityEngine;

public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }

    [Header("Combat")]
    public int shotsFired;
    public int shotsHit;
    public int enemiesKilled;
    public float damageDealt;

    [Header("Survival")]
    public float damageTaken;
    public int deathCount;

    [Header("Exploration")]
    public int itemsCollected;
    public int totalZones = 4;

    [Header("Progress")]
    public float playTime;
    public bool objectiveCompleted;

    private HashSet<string> visitedZones =
        new HashSet<string>();

    public int VisitedZoneCount => visitedZones.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        playTime += Time.deltaTime;
    }

    public void RecordShot()
    {
        shotsFired++;
    }

    public void RecordHit()
    {
        shotsHit++;
    }

    public void RecordDamageDealt(float value)
    {
        damageDealt += value;
    }

    public void RecordDamageTaken(float value)
    {
        damageTaken += value;
    }

    public void RecordKill()
    {
        enemiesKilled++;
    }

    public void RecordDeath()
    {
        deathCount++;
    }

    public void RecordItem()
    {
        itemsCollected++;
    }

    public void RecordZone(string zoneID)
    {
        visitedZones.Add(zoneID);
    }

    public void CompleteObjective()
    {
        objectiveCompleted = true;
    }
}
\`\`\`

Pasang script pada:

\`\`\`text
Systems/TelemetryManager
\`\`\`

---

# 32. Mengapa Menggunakan HashSet?

Digunakan:

\`\`\`csharp
HashSet<string>
\`\`\`

untuk menyimpan area yang sudah pernah dikunjungi.

Contoh:

\`\`\`text
Player masuk Zone_A 10 kali
\`\`\`

tetap dihitung:

\`\`\`text
1 unique zone
\`\`\`

Berbeda dengan \`List\`, HashSet tidak menyimpan nilai duplikat.

---

# 33. Langkah 5 — PlayerHealth

Buat:

\`\`\`text
PlayerHealth.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public float HealthRatio =>
        currentHealth / maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth =
            Mathf.Clamp(currentHealth, 0f, maxHealth);

        TelemetryManager.Instance
            .RecordDamageTaken(amount);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;

        currentHealth =
            Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Die()
    {
        TelemetryManager.Instance.RecordDeath();

        Debug.Log("Player died");

        currentHealth = maxHealth;
        transform.position = Vector3.zero;
    }
}
\`\`\`

Pasang pada Player.

---

# 34. Health Ratio

Health Ratio dihitung:

\`\`\`text
Current Health
--------------
Maximum Health
\`\`\`

Contoh:

\`\`\`text
Health = 75
Max = 100

HealthRatio = 0.75
\`\`\`

Normalisasi otomatis menghasilkan rentang:

\`\`\`text
0 – 1
\`\`\`

---

# 35. Langkah 6 — Enemy

Buat Capsule.

Rename:

\`\`\`text
Enemy
\`\`\`

Tambahkan:

\`\`\`text
Capsule Collider
EnemyHealth
EnemyAI
\`\`\`

Buat Layer:

\`\`\`text
Enemy
\`\`\`

Assign Enemy ke Layer tersebut.

---

# 36. EnemyHealth.cs

\`\`\`csharp
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 50f;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        float actualDamage =
            Mathf.Min(amount, currentHealth);

        currentHealth -= actualDamage;

        TelemetryManager.Instance
            .RecordDamageDealt(actualDamage);

        if (currentHealth <= 0f)
        {
            TelemetryManager.Instance.RecordKill();

            Destroy(gameObject);
        }
    }
}
\`\`\`

---

# 37. Langkah 7 — Player Shooting

Buat:

\`\`\`text
PlayerShooter.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    public float damage = 20f;
    public float range = 15f;
    public float fireCooldown = 0.35f;

    public LayerMask enemyLayer;

    private float nextFireTime;

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space) &&
            Time.time >= nextFireTime)
        {
            Shoot();

            nextFireTime =
                Time.time + fireCooldown;
        }
    }

    private void Shoot()
    {
        TelemetryManager.Instance.RecordShot();

        Vector3 origin =
            transform.position + Vector3.up * 0.8f;

        Vector3 direction =
            transform.forward;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            range,
            enemyLayer))
        {
            EnemyHealth enemy =
                hit.collider.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                TelemetryManager.Instance.RecordHit();

                enemy.TakeDamage(damage);
            }
        }

        Debug.DrawRay(
            origin,
            direction * range,
            Color.red,
            0.2f
        );
    }
}
\`\`\`

Pasang pada Player.

Assign:

\`\`\`text
Enemy Layer
\`\`\`

ke field:

\`\`\`text
Enemy Layer
\`\`\`

---

# 38. Input Shooting

Kontrol praktikum:

\`\`\`text
WASD / Arrow → Movement

SPACE → Attack
\`\`\`

Tujuan penggunaan Space adalah agar praktikum fokus pada:

\`\`\`text
AI + telemetry
\`\`\`

dan tidak terlalu banyak waktu digunakan untuk membuat FPS controller.

---

# 39. Langkah 8 — Enemy AI

Buat:

\`\`\`text
EnemyAI.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float baseMoveSpeed = 2f;
    public float baseDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    private Transform player;
    private PlayerHealth playerHealth;

    private float nextAttackTime;

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;

            playerHealth =
                playerObject.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            Attack();
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        float multiplier = 1f;

        if (AdaptationManager.Instance != null)
        {
            multiplier =
                AdaptationManager.Instance.enemySpeedMultiplier;
        }

        transform.position +=
            direction.normalized
            * baseMoveSpeed
            * multiplier
            * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.forward =
                direction.normalized;
        }
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        float multiplier = 1f;

        if (AdaptationManager.Instance != null)
        {
            multiplier =
                AdaptationManager.Instance.enemyDamageMultiplier;
        }

        playerHealth.TakeDamage(
            baseDamage * multiplier
        );

        nextAttackTime =
            Time.time + attackCooldown;
    }
}
\`\`\`

---

# 40. Apa yang Dilakukan Enemy?

Enemy hanya memiliki behavior sederhana:

\`\`\`text
Player jauh
   ↓
Move toward Player

Player dekat
   ↓
Attack
\`\`\`

AI sengaja sederhana karena fokus Praktikum 12 bukan membuat AI dasar lagi.

AI ini berfungsi sebagai **target adaptasi**.

---

# 41. Langkah 9 — Membuat Enemy Prefab

Drag GameObject:

\`\`\`text
Enemy
\`\`\`

dari Hierarchy ke:

\`\`\`text
Assets/Prefabs/
\`\`\`

Sekarang terdapat:

\`\`\`text
Enemy.prefab
\`\`\`

Hapus Enemy dari Scene jika ingin semua enemy dibuat melalui spawner.

---

# 42. Langkah 10 — EnemySpawner

Buat beberapa Empty GameObject:

\`\`\`text
SpawnPoint_A
SpawnPoint_B
SpawnPoint_C
SpawnPoint_D
\`\`\`

Letakkan mengelilingi arena.

Buat:

\`\`\`text
EnemySpawner.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;

    public float baseSpawnInterval = 5f;
    public int maxEnemies = 8;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        float intervalMultiplier = 1f;

        if (AdaptationManager.Instance != null)
        {
            intervalMultiplier =
                AdaptationManager.Instance.spawnIntervalMultiplier;
        }

        float currentInterval =
            baseSpawnInterval * intervalMultiplier;

        if (timer >= currentInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        int enemyCount =
            GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (enemyCount >= maxEnemies)
            return;

        if (spawnPoints.Length == 0)
            return;

        Transform point =
            spawnPoints[
                Random.Range(0, spawnPoints.Length)
            ];

        Instantiate(
            enemyPrefab,
            point.position,
            point.rotation
        );
    }
}
\`\`\`

---

# 43. Tag Enemy

Jangan lupa memberikan:

\`\`\`text
Tag = Enemy
Layer = Enemy
\`\`\`

kepada Enemy Prefab.

Perbedaannya:

## Tag

Digunakan untuk identifikasi logis.

\`\`\`csharp
FindGameObjectsWithTag("Enemy")
\`\`\`

## Layer

Digunakan untuk filtering sistem seperti:

\`\`\`text
Raycast
Collision
Camera
Physics
\`\`\`

Dalam praktikum ini:

\`\`\`text
Tag Enemy → menghitung enemy aktif

Layer Enemy → Raycast shooting
\`\`\`

---

# 44. Langkah 11 — Exploration Zone

Buat Empty Object:

\`\`\`text
Zone_A
\`\`\`

Tambahkan:

\`\`\`text
Box Collider
\`\`\`

Centang:

\`\`\`text
Is Trigger
\`\`\`

Buat:

\`\`\`text
ExplorationZone.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class ExplorationZone : MonoBehaviour
{
    public string zoneID;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        TelemetryManager.Instance
            .RecordZone(zoneID);
    }
}
\`\`\`

Isi:

\`\`\`text
Zone_A → zoneID = Zone_A
Zone_B → zoneID = Zone_B
Zone_C → zoneID = Zone_C
Zone_D → zoneID = Zone_D
\`\`\`

---

# 45. Mengapa Zone Visit Berguna?

Zone visit memberikan telemetry untuk mengenali Player Explorer.

Misalnya:

\`\`\`text
Total Zone = 4

Player mengunjungi:
Zone A
Zone B
Zone C

Exploration Ratio
= 3 / 4
= 0.75
\`\`\`

Semakin tinggi nilai ini, semakin besar indikasi bahwa Player suka melakukan eksplorasi.

---

# 46. Langkah 12 — Health Pickup

Buat object misalnya:

\`\`\`text
Sphere
\`\`\`

Rename:

\`\`\`text
HealthPickup
\`\`\`

Centang Collider:

\`\`\`text
Is Trigger
\`\`\`

Script:

\`\`\`csharp
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public float healAmount = 25f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth health =
            other.GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.Heal(healAmount);

            TelemetryManager.Instance
                .RecordItem();

            Destroy(gameObject);
        }
    }
}
\`\`\`

---

# 47. GameplayMetrics.cs

Sekarang data mentah akan diolah menjadi metrics.

Buat:

\`\`\`text
GameplayMetrics.cs
\`\`\`

\`\`\`csharp
using System;
using UnityEngine;

[Serializable]
public class GameplayMetrics
{
    public float accuracy;
    public float killRate;

    public float normalizedKillRate;
    public float damageAvoidance;

    public float explorationRatio;
    public float itemCollectionScore;

    public float attackFrequency;
    public float normalizedAttackFrequency;

    public float progressScore;

    public float combatScore;
    public float survivalScore;
    public float resourceScore;

    public void Calculate(
        TelemetryManager telemetry,
        PlayerHealth playerHealth)
    {
        // ==========================
        // COMBAT
        // ==========================

        if (telemetry.shotsFired > 0)
        {
            accuracy =
                (float)telemetry.shotsHit
                / telemetry.shotsFired;
        }
        else
        {
            accuracy = 0f;
        }

        float minutes =
            Mathf.Max(
                telemetry.playTime / 60f,
                0.1f
            );

        killRate =
            telemetry.enemiesKilled / minutes;

        float expectedKillRate = 4f;

        normalizedKillRate =
            Mathf.Clamp01(
                killRate / expectedKillRate
            );

        attackFrequency =
            telemetry.shotsFired / minutes;

        float expectedAttackFrequency = 20f;

        normalizedAttackFrequency =
            Mathf.Clamp01(
                attackFrequency
                / expectedAttackFrequency
            );

        combatScore =
            0.6f * accuracy
            +
            0.4f * normalizedKillRate;

        // ==========================
        // SURVIVAL
        // ==========================

        float expectedDamage = 150f;

        float normalizedDamage =
            Mathf.Clamp01(
                telemetry.damageTaken
                / expectedDamage
            );

        damageAvoidance =
            1f - normalizedDamage;

        float deathPenalty =
            1f -
            Mathf.Clamp01(
                telemetry.deathCount / 3f
            );

        survivalScore =
            0.5f * playerHealth.HealthRatio
            +
            0.3f * damageAvoidance
            +
            0.2f * deathPenalty;

        // ==========================
        // EXPLORATION
        // ==========================

        if (telemetry.totalZones > 0)
        {
            explorationRatio =
                (float)telemetry.VisitedZoneCount
                / telemetry.totalZones;
        }

        // ==========================
        // RESOURCE
        // ==========================

        float expectedItems = 4f;

        itemCollectionScore =
            Mathf.Clamp01(
                telemetry.itemsCollected
                / expectedItems
            );

        resourceScore = itemCollectionScore;

        // ==========================
        // PROGRESS
        // ==========================

        float targetSurvivalTime = 120f;

        progressScore =
            Mathf.Clamp01(
                telemetry.playTime
                / targetSurvivalTime
            );

        if (telemetry.objectiveCompleted)
        {
            progressScore = 1f;
        }
    }
}
\`\`\`

---

# 48. Apa Itu Normalisasi?

Metrics memiliki skala berbeda.

Misalnya:

\`\`\`text
Accuracy      = 0.75
Kills/minute  = 4
Damage Taken  = 120
Shots/minute  = 25
\`\`\`

Nilai tersebut tidak dapat langsung digabungkan.

Karena itu digunakan normalisasi:

\`\`\`text
0.0 – 1.0
\`\`\`

Contoh:

\`\`\`text
Expected Kill Rate = 4

Player Kill Rate = 2

Normalized Kill Rate
= 2 / 4
= 0.5
\`\`\`

---

# 49. Mengapa Expected Value Diperlukan?

Contoh:

\`\`\`csharp
float expectedKillRate = 4f;
\`\`\`

Nilai tersebut berfungsi sebagai **baseline**.

Artinya:

\`\`\`text
4 kill/menit dianggap performa penuh pada metric tersebut.
\`\`\`

Nilai ini bukan nilai universal.

Game berbeda membutuhkan baseline berbeda.

Mahasiswa justru dianjurkan melakukan eksperimen terhadap nilai ini.

---

# 50. Skill Estimation

Gunakan rumus:

\`\`\`text
SkillScore =
0.35 Combat
+
0.30 Survival
+
0.20 Progress
+
0.15 Resource
\`\`\`

Sesuai konsep player modeling, skill tidak ditentukan hanya dari satu metric.

---

# 51. PlayerModel.cs

\`\`\`csharp
using System;

public enum SkillLevel
{
    Low,
    Medium,
    High
}

public enum PlayStyle
{
    Aggressive,
    Defensive,
    Explorer
}

[Serializable]
public class PlayerModel
{
    public float skillScore;
    public SkillLevel skillLevel;

    public float aggressiveScore;
    public float defensiveScore;
    public float explorerScore;

    public PlayStyle dominantStyle;
}
\`\`\`

---

# 52. Mengapa Menggunakan Enum?

Daripada menggunakan:

\`\`\`csharp
string skillLevel = "High";
\`\`\`

lebih baik:

\`\`\`csharp
SkillLevel.High
\`\`\`

\`enum\` mengurangi risiko typo.

Contoh string:

\`\`\`text
"High"
"HIGH"
"Hig"
\`\`\`

dapat menyebabkan error logika.

Enum lebih aman dan terstruktur.

---

# 53. Langkah 13 — PlayerModelManager

Buat:

\`\`\`text
PlayerModelManager.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerModelManager : MonoBehaviour
{
    public static PlayerModelManager Instance
    {
        get;
        private set;
    }

    public GameplayMetrics metrics =
        new GameplayMetrics();

    public PlayerModel model =
        new PlayerModel();

    public PlayerHealth playerHealth;

    public float evaluationInterval = 10f;

    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= evaluationInterval)
        {
            timer = 0f;

            EvaluatePlayer();
        }
    }

    public void EvaluatePlayer()
    {
        if (TelemetryManager.Instance == null ||
            playerHealth == null)
            return;

        metrics.Calculate(
            TelemetryManager.Instance,
            playerHealth
        );

        CalculateSkill();
        CalculatePlayStyle();
    }

    private void CalculateSkill()
    {
        model.skillScore =
            0.35f * metrics.combatScore
            +
            0.30f * metrics.survivalScore
            +
            0.20f * metrics.progressScore
            +
            0.15f * metrics.resourceScore;

        model.skillScore =
            Mathf.Clamp01(model.skillScore);

        if (model.skillScore < 0.4f)
        {
            model.skillLevel =
                SkillLevel.Low;
        }
        else if (model.skillScore < 0.7f)
        {
            model.skillLevel =
                SkillLevel.Medium;
        }
        else
        {
            model.skillLevel =
                SkillLevel.High;
        }
    }

    private void CalculatePlayStyle()
    {
        TelemetryManager telemetry =
            TelemetryManager.Instance;

        // Aggressive
        float damageScore =
            Mathf.Clamp01(
                telemetry.damageDealt / 300f
            );

        model.aggressiveScore =
            0.6f
            * metrics.normalizedAttackFrequency
            +
            0.4f
            * damageScore;

        // Defensive
        model.defensiveScore =
            0.6f
            * metrics.damageAvoidance
            +
            0.4f
            * playerHealth.HealthRatio;

        // Explorer
        model.explorerScore =
            0.7f
            * metrics.explorationRatio
            +
            0.3f
            * metrics.itemCollectionScore;

        DetermineDominantStyle();
    }

    private void DetermineDominantStyle()
    {
        float highest =
            model.aggressiveScore;

        model.dominantStyle =
            PlayStyle.Aggressive;

        if (model.defensiveScore > highest)
        {
            highest =
                model.defensiveScore;

            model.dominantStyle =
                PlayStyle.Defensive;
        }

        if (model.explorerScore > highest)
        {
            model.dominantStyle =
                PlayStyle.Explorer;
        }
    }
}
\`\`\`

Pasang pada:

\`\`\`text
PlayerModelManager
\`\`\`

Assign:

\`\`\`text
Player Health
\`\`\`

dengan Player pada Inspector.

---

# 54. Evaluation Window

Perhatikan:

\`\`\`csharp
evaluationInterval = 10f;
\`\`\`

Player model tidak dihitung setiap frame.

Model diperbarui:

\`\`\`text
setiap 10 detik
\`\`\`

Ini merupakan bentuk sederhana dari:

**Evaluation Window**

Tujuannya agar model tidak berubah berdasarkan satu kejadian singkat.

Contoh buruk:

\`\`\`text
Player menembak terus selama 2 detik

→ langsung dianggap aggressive
\`\`\`

Dengan window, sistem mengamati pola yang lebih panjang.

---

# 55. Skill Level

Gunakan threshold:

\`\`\`text
0.00 – 0.39
Low

0.40 – 0.69
Medium

0.70 – 1.00
High
\`\`\`

Kode:

\`\`\`csharp
if (skillScore < 0.4f)
    Low;
else if (skillScore < 0.7f)
    Medium;
else
    High;
\`\`\`

Nilai threshold merupakan parameter desain.

Tidak ada nilai universal untuk seluruh game.

---

# 56. Play Style Classification

Praktikum menggunakan tiga style.

## Aggressive

Dipengaruhi oleh:

\`\`\`text
Attack Frequency
Damage Dealt
\`\`\`

## Defensive

Dipengaruhi oleh:

\`\`\`text
Damage Avoidance
Remaining Health
\`\`\`

## Explorer

Dipengaruhi oleh:

\`\`\`text
Exploration Ratio
Item Collection
\`\`\`

---

# 57. Catatan Penting tentang Model

Rumus praktikum merupakan:

\`\`\`text
heuristic model
\`\`\`

bukan model ilmiah universal.

Misalnya:

\`\`\`text
Defensive Score tinggi
\`\`\`

tidak membuktikan secara mutlak bahwa player adalah pemain defensif.

Sistem hanya mengatakan:

> Berdasarkan metrics yang dipilih, perilaku player lebih mirip kategori Defensive dibanding kategori lainnya.

Konsep ini penting agar mahasiswa memahami keterbatasan rule-based player modeling.

---

# 58. Langkah 14 — Adaptation Policy

Sekarang game akan merespons hasil player model.

Buat:

\`\`\`text
AdaptationManager.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class AdaptationManager : MonoBehaviour
{
    public static AdaptationManager Instance
    {
        get;
        private set;
    }

    public float enemyDamageMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float spawnIntervalMultiplier = 1f;

    public GameObject explorerBonus;

    public string currentAdaptation =
        "Collecting player data";

    private float timer;

    public float adaptationInterval = 10f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (explorerBonus != null)
        {
            explorerBonus.SetActive(false);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= adaptationInterval)
        {
            timer = 0f;

            ApplyAdaptation();
        }
    }

    private void ApplyAdaptation()
    {
        if (PlayerModelManager.Instance == null)
            return;

        PlayerModel model =
            PlayerModelManager.Instance.model;

        ResetParameters();

        ApplySkillAdaptation(model);
        ApplyStyleAdaptation(model);
    }

    private void ResetParameters()
    {
        enemyDamageMultiplier = 1f;
        enemySpeedMultiplier = 1f;
        spawnIntervalMultiplier = 1f;

        if (explorerBonus != null)
        {
            explorerBonus.SetActive(false);
        }
    }

    private void ApplySkillAdaptation(
        PlayerModel model)
    {
        switch (model.skillLevel)
        {
            case SkillLevel.Low:

                enemyDamageMultiplier = 0.75f;

                spawnIntervalMultiplier = 1.2f;

                currentAdaptation =
                    "Assist: lower enemy damage";

                break;

            case SkillLevel.Medium:

                currentAdaptation =
                    "Normal difficulty";

                break;

            case SkillLevel.High:

                enemyDamageMultiplier = 1.15f;

                spawnIntervalMultiplier = 0.85f;

                currentAdaptation =
                    "Challenge: stronger and faster spawning enemies";

                break;
        }
    }

    private void ApplyStyleAdaptation(
        PlayerModel model)
    {
        if (model.dominantStyle ==
            PlayStyle.Aggressive)
        {
            enemySpeedMultiplier *= 1.15f;

            currentAdaptation +=
                " | Aggressive response";
        }

        if (model.dominantStyle ==
            PlayStyle.Explorer)
        {
            if (explorerBonus != null)
            {
                explorerBonus.SetActive(true);
            }

            currentAdaptation +=
                " | Explorer bonus enabled";
        }

        if (model.dominantStyle ==
            PlayStyle.Defensive)
        {
            enemySpeedMultiplier *= 1.10f;

            currentAdaptation +=
                " | Defensive response";
        }
    }
}
\`\`\`

---

# 59. Penjelasan Adaptation Policy

## Low Skill

\`\`\`text
Enemy Damage ↓
Spawn Interval ↑
\`\`\`

Enemy muncul lebih lambat dan damage lebih rendah.

---

## Medium Skill

\`\`\`text
Normal
\`\`\`

---

## High Skill

\`\`\`text
Enemy Damage ↑
Spawn Interval ↓
\`\`\`

Tantangan sedikit meningkat.

---

## Aggressive

\`\`\`text
Enemy Speed ↑
\`\`\`

Musuh lebih aktif merespons player agresif.

---

## Explorer

\`\`\`text
Bonus Reward aktif
\`\`\`

Game mendukung kecenderungan eksplorasi player.

---

# 60. Mengapa Adaptasi Tidak Dibuat Sangat Kuat?

Contoh yang tidak direkomendasikan:

\`\`\`text
Low Skill:
enemy damage = 10%

High Skill:
enemy damage = 300%
\`\`\`

Perubahan seperti ini mudah dirasakan sebagai:

\`\`\`text
cheating
\`\`\`

atau:

\`\`\`text
punishment
\`\`\`

Adaptasi sebaiknya relatif kecil dan halus.

---

# 61. Player Agency

**Player Agency** adalah kemampuan player untuk merasa bahwa:

\`\`\`text
keputusan dan tindakannya tetap bermakna.
\`\`\`

Adaptive Game AI sebaiknya:

\`\`\`text
mendukung player
\`\`\`

bukan:

\`\`\`text
memaksa player mengikuti model.
\`\`\`

Misalnya Player Explorer sebaiknya memperoleh kesempatan menemukan bonus.

Jangan langsung:

\`\`\`text
memaksa seluruh objective menjadi exploration.
\`\`\`

---

# 62. Langkah 15 — Explorer Bonus

Buat object:

\`\`\`text
BonusReward
\`\`\`

Letakkan di salah satu area samping arena.

Buat inactive:

\`\`\`text
Inspector
→ uncheck GameObject
\`\`\`

Drag ke:

\`\`\`text
AdaptationManager
→ Explorer Bonus
\`\`\`

Jika sistem mendeteksi:

\`\`\`text
Dominant Style = Explorer
\`\`\`

maka:

\`\`\`text
BonusReward.SetActive(true)
\`\`\`

---

# 63. Langkah 16 — Membuat Debug UI

Buat:

\`\`\`text
Hierarchy
→ UI
→ Canvas
\`\`\`

Kemudian:

\`\`\`text
UI
→ Text - TextMeshPro
\`\`\`

Jika Unity meminta:

\`\`\`text
Import TMP Essentials
\`\`\`

pilih:

\`\`\`text
Import
\`\`\`

Rename Text menjadi:

\`\`\`text
PlayerModelDebugText
\`\`\`

Letakkan di kiri atas.

---

# 64. PlayerModelDebugUI.cs

\`\`\`csharp
using TMPro;
using UnityEngine;

public class PlayerModelDebugUI : MonoBehaviour
{
    public TMP_Text debugText;

    private void Update()
    {
        if (TelemetryManager.Instance == null ||
            PlayerModelManager.Instance == null)
            return;

        TelemetryManager t =
            TelemetryManager.Instance;

        GameplayMetrics m =
            PlayerModelManager.Instance.metrics;

        PlayerModel p =
            PlayerModelManager.Instance.model;

        string adaptation = "None";

        if (AdaptationManager.Instance != null)
        {
            adaptation =
                AdaptationManager.Instance
                .currentAdaptation;
        }

        debugText.text =
            "<b>TELEMETRY</b>\\n" +
            $"Play Time: {t.playTime:F1}s\\n" +
            $"Shots Fired: {t.shotsFired}\\n" +
            $"Shots Hit: {t.shotsHit}\\n" +
            $"Kills: {t.enemiesKilled}\\n" +
            $"Damage Dealt: {t.damageDealt:F0}\\n" +
            $"Damage Taken: {t.damageTaken:F0}\\n" +
            $"Items: {t.itemsCollected}\\n" +
            $"Zones: {t.VisitedZoneCount}/{t.totalZones}\\n\\n" +

            "<b>METRICS</b>\\n" +
            $"Accuracy: {m.accuracy:F2}\\n" +
            $"Kill Rate: {m.killRate:F2}/min\\n" +
            $"Combat: {m.combatScore:F2}\\n" +
            $"Survival: {m.survivalScore:F2}\\n" +
            $"Exploration: {m.explorationRatio:F2}\\n" +
            $"Progress: {m.progressScore:F2}\\n\\n" +

            "<b>PLAYER MODEL</b>\\n" +
            $"Skill: {p.skillScore:F2}\\n" +
            $"Skill Level: {p.skillLevel}\\n" +
            $"Aggressive: {p.aggressiveScore:F2}\\n" +
            $"Defensive: {p.defensiveScore:F2}\\n" +
            $"Explorer: {p.explorerScore:F2}\\n" +
            $"Dominant: {p.dominantStyle}\\n\\n" +

            "<b>ADAPTATION</b>\\n" +
            adaptation;
    }
}
\`\`\`

Pasang pada DebugPanel atau Canvas.

Assign:

\`\`\`text
Debug Text
\`\`\`

ke komponen TMP_Text.

---

# 65. Mengapa Debug UI Sangat Penting?

Player modeling sulit diuji jika mahasiswa hanya melihat gameplay.

Debug UI memperlihatkan:

\`\`\`text
Input data
     ↓
Metrics
     ↓
Player model
     ↓
Adaptation
\`\`\`

Mahasiswa dapat melihat langsung:

\`\`\`text
Shots naik
→ Attack Frequency naik
→ Aggressive Score naik
→ Dominant Style berubah
→ Adaptation berubah
\`\`\`

Ini sangat membantu pemahaman hubungan sebab-akibat.

---

# 66. Data Flow Keseluruhan

Arsitektur akhir:

\`\`\`text
PLAYER ACTION
     │
     ▼
TelemetryManager
     │
     ▼
GameplayMetrics
     │
     ▼
PlayerModelManager
     │
     ├── Skill Estimation
     │
     └── Play Style Classification
     │
     ▼
PlayerModel
     │
     ▼
AdaptationManager
     │
     ├── Enemy Damage
     ├── Enemy Speed
     ├── Spawn Rate
     └── Bonus Reward
     │
     ▼
ADAPTIVE GAMEPLAY
\`\`\`

---

# 67. Pembagian Tanggung Jawab Script

## TelemetryManager

Menjawab:

\`\`\`text
Apa yang dilakukan Player?
\`\`\`

---

## GameplayMetrics

Menjawab:

\`\`\`text
Apa arti statistik dari data tersebut?
\`\`\`

---

## PlayerModelManager

Menjawab:

\`\`\`text
Seberapa terampil Player?

Bagaimana gaya bermain Player?
\`\`\`

---

## PlayerModel

Menyimpan:

\`\`\`text
representasi Player.
\`\`\`

---

## AdaptationManager

Menjawab:

\`\`\`text
Apa yang harus dilakukan game berdasarkan Player Model?
\`\`\`

Pemisahan tanggung jawab ini penting untuk menghasilkan arsitektur program yang bersih.

---

# 68. Menguji Sistem — Test 1 Aggressive Player

Bermain dengan cara:

\`\`\`text
sering menembak
aktif mengejar enemy
banyak melakukan damage
tidak banyak eksplorasi
\`\`\`

Perhatikan:

\`\`\`text
normalizedAttackFrequency ↑
damageDealt ↑

Aggressive Score ↑
\`\`\`

Target hasil:

\`\`\`text
Dominant Style:
Aggressive
\`\`\`

---

# 69. Test 2 — Explorer Player

Bermain dengan:

\`\`\`text
mengunjungi Zone A
mengunjungi Zone B
mengunjungi Zone C
mengunjungi Zone D

mengambil item
tidak terlalu fokus combat
\`\`\`

Perhatikan:

\`\`\`text
Exploration Ratio ↑
Item Collection ↑
Explorer Score ↑
\`\`\`

Target:

\`\`\`text
Dominant Style:
Explorer
\`\`\`

Explorer bonus seharusnya aktif.

---

# 70. Test 3 — Defensive Player

Bermain:

\`\`\`text
menjaga jarak
menghindari enemy
mengurangi damage received
mempertahankan health tinggi
\`\`\`

Expected:

\`\`\`text
Damage Avoidance ↑
Health Ratio ↑
Defensive Score ↑
\`\`\`

Target:

\`\`\`text
Dominant Style:
Defensive
\`\`\`

---

# 71. Test 4 — Low Skill

Biarkan:

\`\`\`text
sering terkena enemy
accuracy rendah
kill sedikit
\`\`\`

Hasil yang diharapkan:

\`\`\`text
Combat Score rendah
Survival Score turun
Skill Score turun
\`\`\`

Kemudian:

\`\`\`text
Skill Level = Low
\`\`\`

Adaptasi:

\`\`\`text
Enemy Damage ↓
Spawn lebih lambat
\`\`\`

---

# 72. Test 5 — High Skill

Bermain dengan:

\`\`\`text
accuracy tinggi
kill rate tinggi
health tetap tinggi
damage received rendah
\`\`\`

Hasil:

\`\`\`text
Skill Score ↑
\`\`\`

Jika:

\`\`\`text
Skill Score >= 0.70
\`\`\`

maka:

\`\`\`text
Skill Level = High
\`\`\`

Adaptasi:

\`\`\`text
Enemy Damage ↑ sedikit
Spawn lebih cepat
\`\`\`

---

# 73. Cold Start Problem

Pada awal permainan hanya tersedia sedikit data.

Misalnya setelah:

\`\`\`text
3 detik
\`\`\`

data mungkin hanya:

\`\`\`text
Shots = 2
Kills = 0
Zones = 0
\`\`\`

Jika sistem langsung membuat kesimpulan, hasilnya tidak stabil.

Inilah:

## Cold Start Problem

---

# 74. Solusi Cold Start Sederhana

Tambahkan batas minimum waktu.

Pada:

\`\`\`text
PlayerModelManager.cs
\`\`\`

ubah awal \`EvaluatePlayer()\`:

\`\`\`csharp
public void EvaluatePlayer()
{
    if (TelemetryManager.Instance == null ||
        playerHealth == null)
        return;

    if (TelemetryManager.Instance.playTime < 20f)
        return;

    metrics.Calculate(
        TelemetryManager.Instance,
        playerHealth
    );

    CalculateSkill();
    CalculatePlayStyle();
}
\`\`\`

Dengan demikian:

\`\`\`text
0 – 20 detik
\`\`\`

digunakan sebagai fase pengumpulan data.

Adaptasi baru mulai setelah cukup data tersedia.

Ini **direkomendasikan**.

---

# 75. Confidence

Versi praktikum dasar tidak wajib menggunakan confidence.

Namun secara konsep dapat dibuat:

\`\`\`text
Confidence
= Play Time / Minimum Reliable Time
\`\`\`

Contoh:

\`\`\`csharp
float confidence =
    Mathf.Clamp01(
        playTime / 60f
    );
\`\`\`

Interpretasi:

\`\`\`text
10 detik → confidence rendah

60 detik → confidence tinggi
\`\`\`

---

# 76. Masalah Player Model Berubah Terlalu Cepat

Contoh:

\`\`\`text
10 detik pertama:
Aggressive

10 detik berikutnya:
Defensive

10 detik berikutnya:
Aggressive
\`\`\`

Profile menjadi tidak stabil.

Fenomena ini dapat disebut:

\`\`\`text
oscillation
\`\`\`

---

# 77. Solusi — Moving Average

Pengembangan lebih lanjut dapat menggunakan:

\`\`\`text
New Score =
Old Score × 0.7
+
Measured Score × 0.3
\`\`\`

Contoh:

\`\`\`csharp
model.aggressiveScore =
    Mathf.Lerp(
        model.aggressiveScore,
        measuredAggressiveScore,
        0.3f
    );
\`\`\`

\`Mathf.Lerp()\` dapat digunakan untuk membuat perubahan profile lebih halus.

Ini sangat direkomendasikan sebagai eksperimen lanjutan.

---

# 78. Adaptation Stability

Adaptasi sebaiknya:

\`\`\`text
tidak berubah setiap frame.
\`\`\`

Pada project ini digunakan:

\`\`\`text
Adaptation Interval = 10 detik
\`\`\`

Pilihan lain:

\`\`\`text
setiap wave
setiap room
setelah objective
setelah player mati
\`\`\`

Untuk game arena, interval waktu cukup sesuai.

---

# 79. Kesalahan Umum 1 — NullReferenceException

Contoh:

\`\`\`text
NullReferenceException
PlayerModelManager
\`\`\`

Periksa:

\`\`\`text
Player Health
\`\`\`

pada Inspector.

Pastikan Player sudah di-drag ke:

\`\`\`text
PlayerModelManager
→ Player Health
\`\`\`

---

# 80. Kesalahan Umum 2 — Shooting Tidak Mengenai Enemy

Periksa:

\`\`\`text
Enemy mempunyai Collider?
Enemy mempunyai Layer = Enemy?
PlayerShooter EnemyLayer sudah di-assign?
Player menghadap Enemy?
Range cukup?
\`\`\`

Gunakan:

\`\`\`csharp
Debug.DrawRay()
\`\`\`

dan lihat pada Scene View.

---

# 81. Kesalahan Umum 3 — Zone Tidak Terdeteksi

Periksa:

\`\`\`text
Zone mempunyai Collider?
Is Trigger aktif?
Player Tag = Player?
\`\`\`

Jika trigger tidak bekerja karena konfigurasi physics tertentu, tambahkan Rigidbody kinematic pada trigger object:

\`\`\`text
Rigidbody
Use Gravity = false
Is Kinematic = true
\`\`\`

---

# 82. Kesalahan Umum 4 — Enemy Tidak Spawn

Periksa:

\`\`\`text
Enemy Prefab sudah di-assign?
Spawn Points sudah masuk array?
Enemy memiliki Tag Enemy?
Maximum enemy belum tercapai?
\`\`\`

---

# 83. Kesalahan Umum 5 — Skill Selalu Rendah

Periksa nilai baseline:

\`\`\`text
expectedKillRate
expectedDamage
expectedItems
targetSurvivalTime
\`\`\`

Jika baseline tidak sesuai dengan gameplay, hasil metric juga tidak sesuai.

Ini menunjukkan bahwa player modeling memerlukan:

\`\`\`text
calibration
\`\`\`

---

# 84. Calibration

Calibration adalah proses menyesuaikan parameter model agar cocok dengan gameplay.

Contoh:

\`\`\`text
Expected Kill Rate = 4
\`\`\`

tetapi pada game sebenarnya Player normal hanya dapat:

\`\`\`text
1 kill/menit
\`\`\`

Maka hampir seluruh Player akan terlihat buruk.

Parameter seharusnya dikalibrasi berdasarkan hasil pengujian.

---

# 85. Eksperimen Mahasiswa 1

Ubah:

\`\`\`csharp
expectedKillRate
\`\`\`

dari:

\`\`\`text
4
\`\`\`

menjadi:

\`\`\`text
2
\`\`\`

Bandingkan:

\`\`\`text
Normalized Kill Rate
Combat Score
Skill Score
\`\`\`

Tuliskan pengaruhnya.

---

# 86. Eksperimen Mahasiswa 2

Ubah rumus:

\`\`\`text
Combat Score
\`\`\`

dari:

\`\`\`text
60% Accuracy
40% Kill Rate
\`\`\`

menjadi:

\`\`\`text
30% Accuracy
70% Kill Rate
\`\`\`

Pertanyaan:

> Player seperti apa yang sekarang lebih mudah memperoleh Combat Score tinggi?

---

# 87. Eksperimen Mahasiswa 3

Ubah interval:

\`\`\`text
10 detik
\`\`\`

menjadi:

\`\`\`text
2 detik
\`\`\`

Kemudian bandingkan kestabilan Player Model.

Pertanyaan:

> Apakah model menjadi lebih responsif atau justru terlalu mudah berubah?

---

# 88. Eksperimen Mahasiswa 4

Tambahkan style:

\`\`\`text
Risk-Taker
\`\`\`

Data yang dapat digunakan:

\`\`\`text
Damage Taken
Health Ratio
Attack Frequency
\`\`\`

Contoh rule:

\`\`\`text
Attack tinggi
+
Damage Taken tinggi
+
Health rendah

→ Risk Score tinggi
\`\`\`

---

# 89. Eksperimen Mahasiswa 5

Tambahkan:

\`\`\`text
Player Profile JSON
\`\`\`

Tujuannya:

\`\`\`text
menyimpan hasil profile setelah sesi permainan.
\`\`\`

Contoh informasi:

\`\`\`json
{
  "skillScore": 0.68,
  "skillLevel": "Medium",
  "aggressiveScore": 0.74,
  "defensiveScore": 0.31,
  "explorerScore": 0.54,
  "dominantStyle": "Aggressive"
}
\`\`\`

Ini dapat menjadi pengembangan untuk profile jangka panjang.

---

# 90. Praktikum Minimum yang Wajib Berhasil

Mahasiswa minimal harus menghasilkan:

- [ ] Player dapat bergerak.
- [ ] Player dapat menyerang.
- [ ] Enemy dapat mengejar Player.
- [ ] Enemy dapat menerima damage.
- [ ] Enemy dapat mati.
- [ ] Shots Fired tercatat.
- [ ] Shots Hit tercatat.
- [ ] Kill tercatat.
- [ ] Damage Taken tercatat.
- [ ] Zone visited tercatat.
- [ ] Item collected tercatat.
- [ ] Accuracy dihitung.
- [ ] Combat Score dihitung.
- [ ] Survival Score dihitung.
- [ ] Exploration Ratio dihitung.
- [ ] Skill Score dihitung.
- [ ] Skill Level ditentukan.
- [ ] Aggressive Score dihitung.
- [ ] Defensive Score dihitung.
- [ ] Explorer Score dihitung.
- [ ] Dominant Style ditentukan.
- [ ] Player Profile tampil di UI.
- [ ] Minimal satu adaptasi gameplay bekerja.

---

# 91. Challenge Tambahan

Untuk mahasiswa yang menyelesaikan bagian wajib lebih cepat:

### Challenge 1

Tambahkan:

\`\`\`text
Risk Score
\`\`\`

### Challenge 2

Tambahkan:

\`\`\`text
Moving Average
\`\`\`

untuk menstabilkan model.

### Challenge 3

Simpan profile:

\`\`\`text
JSON
\`\`\`

### Challenge 4

Buat grafik perubahan:

\`\`\`text
Skill Score terhadap waktu
\`\`\`

### Challenge 5

Integrasikan dengan DDA Praktikum 11.

### Challenge 6

Hubungkan player profile dengan Utility AI dari Praktikum 6.

---

# 92. Hubungan dengan Utility AI

Misalnya:

\`\`\`text
Dominant Style = Aggressive
\`\`\`

maka enemy dapat mengubah:

\`\`\`text
TakeCover Weight ↑
MaintainDistance Weight ↑
\`\`\`

Jika:

\`\`\`text
Dominant Style = Defensive
\`\`\`

maka:

\`\`\`text
Flank Weight ↑
Approach Weight ↑
\`\`\`

Dengan demikian Player Model menjadi input bagi AI decision system.

---

# 93. Hubungan dengan PCG

Player model juga dapat digunakan oleh Procedural Content Generation.

Contoh:

\`\`\`text
Explorer
→ Optional Room ↑
→ Collectible ↑

Aggressive
→ Combat Room ↑

Low Skill
→ Trap ↓
→ Health Item ↑

High Skill
→ Elite Enemy ↑
\`\`\`

Ini menghubungkan praktikum Player Modeling dengan Praktikum 9 dan Praktikum 10.

---

# 94. Hubungan dengan DDA

DDA dapat dianggap sebagai salah satu bentuk adaptation policy.

\`\`\`text
Telemetry
     ↓
Player Model
     ↓
Skill Estimation
     ↓
DDA
\`\`\`

Contoh:

\`\`\`text
Skill = Low
→ Difficulty ↓

Skill = High
→ Difficulty ↑
\`\`\`

Tetapi Player Modeling lebih luas karena juga menyimpan:

\`\`\`text
Play Style
Exploration Preference
Risk Preference
\`\`\`

---

# 95. Rekomendasi Implementasi Terbaik

Untuk Praktikum 12, implementasi yang paling disarankan adalah:

## Core

\`\`\`text
TelemetryManager
GameplayMetrics
PlayerModel
PlayerModelManager
Debug UI
\`\`\`

## Metrics

Gunakan empat kelompok:

\`\`\`text
Combat
Survival
Exploration
Progress
\`\`\`

## Player Model

Gunakan:

\`\`\`text
Skill:
Low / Medium / High

Style:
Aggressive
Defensive
Explorer
\`\`\`

## Adaptation

Gunakan maksimal dua mekanisme utama:

\`\`\`text
Skill
→ enemy damage / spawn rate

Play Style
→ enemy response / explorer reward
\`\`\`

Model ini cukup sederhana untuk dipahami mahasiswa tetapi tetap menunjukkan siklus Adaptive Game AI secara lengkap.

---

# 96. Mengapa Tidak Langsung Menggunakan Machine Learning?

Pada praktikum ini metode:

\`\`\`text
Rule-Based
+
Score-Based
\`\`\`

lebih direkomendasikan.

Alasannya:

1. setiap keputusan dapat dijelaskan;
2. mahasiswa dapat melihat hubungan metric dengan profile;
3. debugging jauh lebih mudah;
4. tidak membutuhkan dataset;
5. tidak membutuhkan training;
6. sesuai untuk memahami fondasi Player Modeling.

Machine Learning akan lebih tepat diperkenalkan setelah mahasiswa memahami:

\`\`\`text
State
Data
Features
Metrics
Classification
Decision
\`\`\`

yang menjadi fondasi menuju materi Pertemuan 13.

---

# 97. Pertanyaan Evaluasi Praktikum

1. Apa perbedaan telemetry dan metrics?
2. Mengapa telemetry merupakan data mentah?
3. Mengapa metric harus dinormalisasi?
4. Apa fungsi \`Mathf.Clamp01()\`?
5. Apa perbedaan skill dan play style?
6. Mengapa Player agresif belum tentu mempunyai skill rendah?
7. Mengapa Player Model tidak sebaiknya diperbarui setiap frame?
8. Apa fungsi evaluation window?
9. Apa itu cold start?
10. Mengapa adaptation policy perlu dipisahkan dari Player Model?
11. Apa fungsi \`TelemetryManager\`?
12. Apa fungsi \`GameplayMetrics\`?
13. Apa fungsi \`PlayerModelManager\`?
14. Apa fungsi \`AdaptationManager\`?
15. Mengapa debug UI penting?
16. Mengapa baseline metric perlu dikalibrasi?
17. Apa masalah jika adaptasi terlalu kuat?
18. Apa yang dimaksud player agency?
19. Bagaimana Player Model dapat digunakan oleh Utility AI?
20. Bagaimana Player Model dapat digunakan oleh PCG?

---

# 98. Tugas Analisis

Lakukan tiga sesi permainan:

\`\`\`text
Session A
Bermain Aggressive

Session B
Bermain Defensive

Session C
Bermain Explorer
\`\`\`

Catat:

| Metric | Aggressive | Defensive | Explorer |
|---|---:|---:|---:|
| Accuracy | | | |
| Kill Rate | | | |
| Damage Taken | | | |
| Exploration Ratio | | | |
| Item Score | | | |
| Skill Score | | | |
| Aggressive Score | | | |
| Defensive Score | | | |
| Explorer Score | | | |
| Dominant Style | | | |

Kemudian analisis:

1. Apakah classification sesuai dengan cara bermain?
2. Metric mana yang paling memengaruhi classification?
3. Apakah skill level sesuai pengamatan?
4. Apakah terdapat classification yang tidak masuk akal?
5. Parameter apa yang perlu diperbaiki?

---

# 99. Tugas Pengembangan

Pilih minimal satu:

\`\`\`text
A. Menambahkan Risk-Taker Score

B. Menambahkan Moving Average

C. Menyimpan Player Profile ke JSON

D. Menambahkan adaptasi berdasarkan Explorer

E. Menghubungkan Skill Score dengan DDA

F. Menambahkan grafik Player Model

G. Menambahkan confidence score
\`\`\`

---

# 100. Kesimpulan Praktikum

Pada praktikum ini dibangun sistem:

\`\`\`text
Player
   ↓
Gameplay Activity
   ↓
Telemetry
   ↓
Metrics
   ↓
Skill Estimation
   ↓
Play Style Classification
   ↓
Player Profile
   ↓
Adaptation Policy
   ↓
Adaptive Game AI
\`\`\`

Mahasiswa tidak hanya membuat game yang bereaksi terhadap kondisi dunia game.

Game mulai mempunyai kemampuan untuk:

> **mengamati pola perilaku player, membentuk representasi sederhana tentang player, dan menyesuaikan gameplay berdasarkan representasi tersebut.**

Inilah konsep inti:

# Player Modeling & Adaptive Game AI

---

# 101. Catatan Penting untuk Pengajar

Praktikum ini sebaiknya tidak dinilai berdasarkan apakah classifier selalu menghasilkan kategori yang "benar".

Justru diskusi terpenting berada pada:

\`\`\`text
Mengapa sistem membuat kesimpulan tersebut?
\`\`\`

Mahasiswa perlu memahami bahwa:

\`\`\`text
Player Model
≠ fakta absolut tentang Player
\`\`\`

melainkan:

\`\`\`text
Player Model
=
estimasi berdasarkan telemetry,
metrics,
baseline,
formula,
dan aturan yang dirancang developer.
\`\`\`

Karena itu kualitas Player Modeling sangat bergantung pada:

- pemilihan telemetry;
- relevansi metrics;
- normalisasi;
- weighting;
- evaluation window;
- calibration;
- stability;
- dan adaptation policy.

---

# 102. Rekomendasi Final Praktikum 12

## Nama Project

\`\`\`text
GC12_AdaptivePlayerModeling
\`\`\`

## Nama Scene

\`\`\`text
PlayerModelingDemo
\`\`\`

## Game

\`\`\`text
Adaptive Arena Survival
\`\`\`

## Model

\`\`\`text
Rule-Based + Score-Based Player Modeling
\`\`\`

## Skill

\`\`\`text
Low
Medium
High
\`\`\`

## Play Style

\`\`\`text
Aggressive
Defensive
Explorer
\`\`\`

## Metrics Minimum

\`\`\`text
Accuracy
Kill Rate
Damage Taken
Health Ratio
Exploration Ratio
Item Collection
Attack Frequency
Progress
\`\`\`

## Adaptasi Utama

\`\`\`text
Skill
→ Enemy Damage + Spawn Rate

Play Style
→ Enemy Response + Explorer Reward
\`\`\`

### Pilihan ini paling direkomendasikan

karena mencakup seluruh siklus Player Modeling tetapi masih cukup sederhana untuk diimplementasikan dan di-debug oleh mahasiswa dalam Unity 6.

---

# 103. Hasil Akhir yang Diharapkan

Saat game dijalankan, UI kira-kira menampilkan:

\`\`\`text
TELEMETRY
Play Time       : 87.5 s
Shots Fired     : 42
Shots Hit       : 29
Kills           : 7
Damage Dealt    : 315
Damage Taken    : 63
Items           : 2
Zones           : 3 / 4

METRICS
Accuracy        : 0.69
Kill Rate       : 4.80/min
Combat          : 0.81
Survival        : 0.68
Exploration     : 0.75
Progress        : 0.73

PLAYER MODEL
Skill Score     : 0.72
Skill Level     : High

Aggressive      : 0.78
Defensive       : 0.53
Explorer        : 0.66

Dominant Style  : Aggressive

ADAPTATION
Challenge: stronger and faster spawning enemies
| Aggressive response
\`\`\`

Dengan demikian mahasiswa dapat mengamati secara langsung:

\`\`\`text
cara bermain
      ↓
mengubah telemetry
      ↓
mengubah metrics
      ↓
mengubah Player Model
      ↓
mengubah gameplay
\`\`\`

Itulah tujuan utama Praktikum 12.`,Tc=`# MODUL PRAKTIKUM 13  
# MACHINE LEARNING FOR GAMES  
## Implementasi Q-Learning Grid Agent pada Unity 6

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Tools:** Unity 6 + C#  
**Nama Project:** \`GameAI_13_QLearningGrid\`  
**Nama Scene:** \`QLearningGridScene\`

---

# 1. Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat sebuah **Grid World** sederhana di Unity yang berisi:

- Agent,
- Start,
- Goal,
- Cell normal,
- Obstacle,
- sistem reward,
- Q-table,
- algoritma Q-Learning,
- epsilon-greedy,
- training episode,
- visualisasi proses belajar,
- dan mode inference setelah training selesai.

Agent pada awalnya bergerak relatif acak. Setelah menjalani banyak episode, nilai pada Q-table berubah dan agent secara bertahap mempelajari kebijakan atau **policy** untuk mencapai Goal.

---

# 2. Capaian Pembelajaran Praktikum

Setelah menyelesaikan praktikum, mahasiswa diharapkan mampu:

1. Menjelaskan konsep **Reinforcement Learning**.
2. Menjelaskan hubungan antara **Agent** dan **Environment**.
3. Menentukan **state** suatu permasalahan sederhana.
4. Menentukan **action space** agent.
5. Merancang sistem **reward dan penalty**.
6. Mengimplementasikan **Q-table** menggunakan C#.
7. Mengimplementasikan algoritma **Q-Learning**.
8. Mengimplementasikan metode **epsilon-greedy**.
9. Menjelaskan perbedaan **exploration** dan **exploitation**.
10. Menjalankan training berbasis episode.
11. Mengamati perubahan perilaku agent selama training.
12. Menggunakan hasil training sebagai policy pada mode inference.
13. Menganalisis pengaruh parameter Q-Learning.
14. Membandingkan Q-Learning dengan algoritma pathfinding seperti A*.

---

# 3. Gambaran Praktikum

Environment yang dibuat berupa grid sederhana, misalnya:

\`\`\`text
S . . . .
. # . # .
. # . . .
. . # . .
. . . . G
\`\`\`

Keterangan:

\`\`\`text
S = Start
G = Goal
# = Obstacle
. = Cell normal
\`\`\`

Agent memiliki empat action:

\`\`\`text
0 = Up
1 = Down
2 = Left
3 = Right
\`\`\`

Reward yang digunakan:

\`\`\`text
Goal             = +1.0
Menabrak obstacle = -1.0
Keluar grid       = -1.0
Setiap langkah    = -0.01
\`\`\`

Dengan reward tersebut, agent diharapkan belajar:

\`\`\`text
Hindari obstacle
        +
Hindari langkah tidak perlu
        +
Temukan Goal
        ↓
Jalur relatif pendek menuju Goal
\`\`\`

---

# 4. Mengapa Memilih Q-Learning Grid World?

Untuk Pertemuan 13, **Q-Learning Grid World adalah pilihan praktikum terbaik**.

Alasannya:

- implementasinya relatif sederhana,
- tidak membutuhkan Python,
- tidak membutuhkan training framework eksternal,
- seluruh proses dapat dilakukan menggunakan C#,
- Q-table dapat dilihat secara langsung,
- state dan action mudah dipahami,
- perubahan policy mudah divisualisasikan,
- reward design dapat dieksperimenkan,
- sangat cocok untuk pengantar Reinforcement Learning.

Unity ML-Agents tetap diperkenalkan pada materi, tetapi implementasi ML-Agents akan lebih tepat digunakan pada praktikum Pertemuan 14.

---

# 5. Konsep Dasar Sebelum Praktikum

## 5.1 Reinforcement Learning

**Reinforcement Learning (RL)** adalah metode pembelajaran di mana Agent belajar melalui interaksi dengan Environment.

Alurnya:

\`\`\`text
Agent mengamati State
        ↓
Agent memilih Action
        ↓
Environment berubah
        ↓
Agent menerima Reward
        ↓
Agent mengamati State berikutnya
        ↓
Agent memperbarui pengetahuan
\`\`\`

Secara sederhana:

\`\`\`text
State → Action → Reward → Next State
\`\`\`

---

# 6. Istilah Penting Reinforcement Learning

## 6.1 Agent

**Agent** adalah objek yang belajar mengambil keputusan.

Pada praktikum:

\`\`\`text
Agent = GameObject berbentuk Capsule/Cube
\`\`\`

Agent akan berpindah dari satu cell menuju cell lainnya.

---

## 6.2 Environment

**Environment** adalah dunia tempat agent melakukan interaksi.

Pada praktikum:

\`\`\`text
Environment = Grid World
\`\`\`

Environment menentukan:

- cell yang tersedia,
- obstacle,
- posisi start,
- posisi goal,
- reward,
- hasil action.

---

## 6.3 State

**State** adalah representasi keadaan agent.

Dalam praktikum ini:

\`\`\`text
State = posisi cell agent
\`\`\`

Jika grid berukuran:

\`\`\`text
5 × 5
\`\`\`

maka terdapat maksimal:

\`\`\`text
25 state
\`\`\`

Misalnya:

\`\`\`text
(0,0) → State 0
(1,0) → State 1
(2,0) → State 2
...
\`\`\`

---

## 6.4 Action

**Action** adalah keputusan yang dapat dilakukan agent.

Action:

\`\`\`text
Up
Down
Left
Right
\`\`\`

Jumlah action:

\`\`\`text
4
\`\`\`

---

## 6.5 Reward

Reward adalah feedback numerik kepada agent.

Contoh:

\`\`\`text
Goal              +1.00
Obstacle          -1.00
Keluar grid       -1.00
Langkah biasa     -0.01
\`\`\`

Reward positif menunjukkan hasil yang diinginkan.

Penalty atau reward negatif menunjukkan hasil yang tidak diinginkan.

---

## 6.6 Episode

**Episode** adalah satu percobaan lengkap.

Contoh:

\`\`\`text
Episode dimulai
↓
Agent ditempatkan di Start
↓
Agent bergerak
↓
Agent mencapai Goal / gagal / step maksimum tercapai
↓
Episode selesai
↓
Environment di-reset
↓
Episode berikutnya dimulai
\`\`\`

---

## 6.7 Policy

**Policy** adalah strategi agent dalam memilih action.

Setelah Q-Learning selesai, policy sederhana adalah:

\`\`\`text
Pada state tertentu:
pilih action dengan Q-value terbesar
\`\`\`

---

# 7. Q-Table

Q-Learning menyimpan:

\`\`\`text
Q(State, Action)
\`\`\`

Misalnya:

| State | Up | Down | Left | Right |
|---|---:|---:|---:|---:|
| S0 | 0.20 | -0.10 | -0.30 | 0.60 |
| S1 | 0.10 | 0.30 | 0.00 | 0.70 |
| S2 | -0.10 | 0.40 | 0.20 | 0.10 |

Jika berada pada \`S0\`:

\`\`\`text
Up    = 0.20
Down  = -0.10
Left  = -0.30
Right = 0.60
\`\`\`

Maka action terbaik:

\`\`\`text
Right
\`\`\`

---

# 8. Formula Q-Learning

Formula dasarnya:

\`\`\`text
Q(s,a) ← Q(s,a) + α [r + γ max Q(s',a') - Q(s,a)]
\`\`\`

Keterangan:

\`\`\`text
s  = state sekarang
a  = action
r  = reward
s' = state berikutnya
α  = learning rate
γ  = discount factor
\`\`\`

---

# 9. Makna Formula Secara Intuitif

Tidak perlu memandang formula Q-Learning hanya sebagai rumus matematika.

Intinya adalah:

\`\`\`text
Nilai lama
+
koreksi berdasarkan
reward sekarang
+
kemungkinan reward masa depan
\`\`\`

Jika suatu action memberikan hasil yang bagus:

\`\`\`text
Q-value meningkat
\`\`\`

Jika hasilnya buruk:

\`\`\`text
Q-value menurun
\`\`\`

Setelah dilakukan berulang kali:

\`\`\`text
Q-table
    ↓
menyimpan pengalaman agent
    ↓
membentuk policy
\`\`\`

---

# 10. Learning Rate

Parameter:

\`\`\`text
alpha / α
\`\`\`

Menentukan seberapa besar informasi baru memengaruhi nilai lama.

Contoh:

\`\`\`text
learningRate = 0.2
\`\`\`

Interpretasi:

- nilai kecil → belajar lambat tetapi stabil,
- nilai besar → belajar cepat tetapi dapat berubah drastis.

Rekomendasi praktikum:

\`\`\`text
0.1 – 0.5
\`\`\`

Gunakan:

\`\`\`text
0.2
\`\`\`

sebagai nilai awal.

---

# 11. Discount Factor

Parameter:

\`\`\`text
gamma / γ
\`\`\`

Mengatur pentingnya reward masa depan.

Gunakan:

\`\`\`text
discountFactor = 0.9
\`\`\`

Nilai tinggi berarti agent mempertimbangkan konsekuensi jangka panjang.

---

# 12. Exploration dan Exploitation

## Exploration

Agent mencoba action yang belum tentu terbaik.

Tujuan:

\`\`\`text
mencari pengalaman baru
\`\`\`

---

## Exploitation

Agent menggunakan pengetahuan yang sudah dipelajari.

Tujuan:

\`\`\`text
memilih action dengan Q-value terbesar
\`\`\`

Kedua proses perlu diseimbangkan.

---

# 13. Epsilon-Greedy

Kita menggunakan strategi:

\`\`\`text
epsilon-greedy
\`\`\`

Misalnya:

\`\`\`text
epsilon = 0.30
\`\`\`

Artinya:

\`\`\`text
30% → memilih action random
70% → memilih action terbaik
\`\`\`

Pada training:

\`\`\`text
epsilon
↓
secara bertahap dikurangi
\`\`\`

Contoh:

\`\`\`text
0.80
0.79
0.78
...
0.10
\`\`\`

Agent menjadi semakin sedikit melakukan eksplorasi setelah memiliki cukup pengalaman.

---

# 14. Struktur Project Unity

Buat project:

\`\`\`text
GameAI_13_QLearningGrid
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D Core
\`\`\`

Keduanya dapat digunakan.

Untuk praktikum sederhana, **3D Core sudah mencukupi**.

---

# 15. Struktur Folder

Pada \`Assets\`, buat:

\`\`\`text
Assets/
│
├── Scenes/
│   └── QLearningGridScene.unity
│
├── Scripts/
│   ├── GridCell.cs
│   ├── GridWorld.cs
│   ├── QTable.cs
│   ├── QLearningAgent.cs
│   ├── TrainingManager.cs
│   └── QLearningDebugUI.cs
│
├── Materials/
│   ├── NormalCell.mat
│   ├── StartCell.mat
│   ├── GoalCell.mat
│   ├── ObstacleCell.mat
│   └── Agent.mat
│
└── Prefabs/
    └── GridCell.prefab
\`\`\`

---

# 16. Istilah Teknis Unity yang Digunakan

## 16.1 Scene

**Scene** adalah satu dunia atau level Unity.

Dalam praktikum:

\`\`\`text
QLearningGridScene
\`\`\`

menyimpan semua GameObject.

---

## 16.2 GameObject

GameObject adalah objek dasar di Unity.

Contoh:

\`\`\`text
Agent
GridWorld
Goal
Main Camera
Directional Light
Canvas
\`\`\`

---

## 16.3 Component

Component memberi fungsi kepada GameObject.

Contoh:

\`\`\`text
Transform
Mesh Renderer
Collider
Script
Camera
\`\`\`

---

## 16.4 Transform

\`Transform\` menentukan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

Semua GameObject Unity memiliki Transform.

Dalam praktikum, Transform terutama digunakan untuk memindahkan agent ke cell tertentu.

---

## 16.5 MonoBehaviour

Script Unity biasanya diturunkan dari:

\`\`\`csharp
MonoBehaviour
\`\`\`

Contoh:

\`\`\`csharp
public class GridWorld : MonoBehaviour
\`\`\`

Dengan MonoBehaviour, script dapat:

- dipasang sebagai Component,
- menggunakan \`Start()\`,
- menggunakan \`Update()\`,
- menjalankan Coroutine,
- mengakses Inspector.

---

## 16.6 SerializeField

Contoh:

\`\`\`csharp
[SerializeField] private int width = 5;
\`\`\`

\`SerializeField\` membuat private field tetap dapat muncul di Inspector.

Keuntungannya:

\`\`\`text
encapsulation tetap terjaga
+
parameter dapat diubah dari Inspector
\`\`\`

---

## 16.7 Inspector

Inspector adalah panel Unity untuk melihat dan mengubah Component serta parameter suatu GameObject.

Parameter Q-Learning akan dibuat agar dapat diubah melalui Inspector.

---

## 16.8 Prefab

Prefab adalah template GameObject yang dapat digunakan berulang.

Kita dapat membuat:

\`\`\`text
GridCell.prefab
\`\`\`

kemudian menghasilkan seluruh grid dari prefab yang sama.

---

## 16.9 Coroutine

Coroutine memungkinkan fungsi berjalan secara bertahap selama beberapa frame.

Contoh:

\`\`\`csharp
IEnumerator TrainingLoop()
\`\`\`

Coroutine cocok digunakan agar pergerakan agent dapat divisualisasikan tanpa membuat Unity Editor membeku.

---

## 16.10 IEnumerator

Method Coroutine menggunakan tipe:

\`\`\`csharp
IEnumerator
\`\`\`

dan dapat berhenti sementara melalui:

\`\`\`csharp
yield return new WaitForSeconds(...)
\`\`\`

---

## 16.11 Vector2Int

\`Vector2Int\` menyimpan dua bilangan integer.

Contoh:

\`\`\`csharp
Vector2Int position = new Vector2Int(2, 3);
\`\`\`

Sangat cocok untuk koordinat grid:

\`\`\`text
x = 2
y = 3
\`\`\`

---

## 16.12 Dictionary

\`Dictionary<TKey,TValue>\` menyimpan pasangan:

\`\`\`text
key → value
\`\`\`

Kita dapat menggunakannya untuk mencari cell berdasarkan koordinat.

---

## 16.13 Random.value

Unity menyediakan:

\`\`\`csharp
Random.value
\`\`\`

yang menghasilkan angka antara:

\`\`\`text
0 sampai 1
\`\`\`

Digunakan untuk epsilon-greedy.

---

# 17. Membuat Scene

Buat scene baru:

\`\`\`text
File
→ New Scene
\`\`\`

Simpan:

\`\`\`text
Assets/Scenes/QLearningGridScene.unity
\`\`\`

---

# 18. Membuat GridCell

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
GridCell
\`\`\`

Set Scale:

\`\`\`text
X = 0.95
Y = 0.10
Z = 0.95
\`\`\`

Tujuannya agar terdapat sedikit jarak antar-cell.

Drag ke folder:

\`\`\`text
Assets/Prefabs/
\`\`\`

untuk membuat:

\`\`\`text
GridCell.prefab
\`\`\`

Kemudian hapus instance GridCell dari scene.

---

# 19. Script GridCell.cs

Buat:

\`\`\`text
Assets/Scripts/GridCell.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public enum CellType
    {
        Normal,
        Start,
        Goal,
        Obstacle
    }

    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private CellType cellType;

    public Vector2Int GridPosition => gridPosition;
    public CellType Type => cellType;

    public void Initialize(Vector2Int position, CellType type)
    {
        gridPosition = position;
        cellType = type;
        name = $"Cell_{position.x}_{position.y}_{type}";
    }

    public void SetType(CellType newType)
    {
        cellType = newType;
    }

    public bool IsObstacle()
    {
        return cellType == CellType.Obstacle;
    }

    public bool IsGoal()
    {
        return cellType == CellType.Goal;
    }
}
\`\`\`

---

# 20. Penjelasan GridCell

## enum

\`\`\`csharp
public enum CellType
\`\`\`

\`enum\` menyediakan daftar nilai terbatas.

Dalam kasus ini:

\`\`\`text
Normal
Start
Goal
Obstacle
\`\`\`

Lebih aman dibanding menggunakan string seperti:

\`\`\`text
"normal"
"goal"
"obstacle"
\`\`\`

karena risiko typo lebih kecil.

---

# 21. Membuat GridWorld

Buat Empty GameObject:

\`\`\`text
GameObject
→ Create Empty
\`\`\`

Rename:

\`\`\`text
GridWorld
\`\`\`

Reset Transform:

\`\`\`text
Position = 0,0,0
Rotation = 0,0,0
Scale    = 1,1,1
\`\`\`

---

# 22. Script GridWorld.cs

Buat:

\`\`\`text
Assets/Scripts/GridWorld.cs
\`\`\`

Isi:

\`\`\`csharp
using System.Collections.Generic;
using UnityEngine;

public class GridWorld : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 5;
    [SerializeField] private float cellSize = 1.2f;

    [Header("References")]
    [SerializeField] private GridCell cellPrefab;

    [Header("Special Positions")]
    [SerializeField] private Vector2Int startPosition = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalPosition = new Vector2Int(4, 4);

    [Header("Obstacles")]
    [SerializeField] private List<Vector2Int> obstaclePositions =
        new List<Vector2Int>();

    private Dictionary<Vector2Int, GridCell> cells =
        new Dictionary<Vector2Int, GridCell>();

    public int Width => width;
    public int Height => height;
    public Vector2Int StartPosition => startPosition;
    public Vector2Int GoalPosition => goalPosition;

    private void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        cells.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);

                GridCell.CellType type = GridCell.CellType.Normal;

                if (gridPosition == startPosition)
                {
                    type = GridCell.CellType.Start;
                }
                else if (gridPosition == goalPosition)
                {
                    type = GridCell.CellType.Goal;
                }
                else if (obstaclePositions.Contains(gridPosition))
                {
                    type = GridCell.CellType.Obstacle;
                }

                Vector3 worldPosition = GridToWorld(gridPosition);

                GridCell cell = Instantiate(
                    cellPrefab,
                    worldPosition,
                    Quaternion.identity,
                    transform
                );

                cell.Initialize(gridPosition, type);
                cells.Add(gridPosition, cell);
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int position)
    {
        return new Vector3(
            position.x * cellSize,
            0f,
            position.y * cellSize
        );
    }

    public bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < width &&
               position.y >= 0 &&
               position.y < height;
    }

    public bool IsObstacle(Vector2Int position)
    {
        if (!cells.ContainsKey(position))
            return false;

        return cells[position].IsObstacle();
    }

    public bool IsGoal(Vector2Int position)
    {
        return position == goalPosition;
    }

    public int PositionToState(Vector2Int position)
    {
        return position.y * width + position.x;
    }

    public int StateCount()
    {
        return width * height;
    }
}
\`\`\`

---

# 23. Mengatur GridWorld di Inspector

Pasang:

\`\`\`text
GridWorld.cs
\`\`\`

pada GameObject \`GridWorld\`.

Set:

\`\`\`text
Width       = 5
Height      = 5
Cell Size   = 1.2

Start Position
X = 0
Y = 0

Goal Position
X = 4
Y = 4
\`\`\`

Drag:

\`\`\`text
GridCell.prefab
\`\`\`

ke:

\`\`\`text
Cell Prefab
\`\`\`

---

# 24. Menambahkan Obstacle

Pada Inspector:

\`\`\`text
Obstacle Positions
Size = 4
\`\`\`

Contoh:

\`\`\`text
Element 0 = (1,1)
Element 1 = (1,2)
Element 2 = (2,3)
Element 3 = (3,1)
\`\`\`

Pastikan:

\`\`\`text
Start != Obstacle
Goal  != Obstacle
\`\`\`

---

# 25. Memberi Warna Cell

Untuk tahap pertama, semua cell dapat menggunakan material yang sama.

Setelah algoritma berfungsi, material dapat diperluas menjadi:

\`\`\`text
Normal   = abu-abu
Start    = biru
Goal     = hijau
Obstacle = merah/hitam
\`\`\`

Visualisasi bukan bagian inti Q-Learning, tetapi sangat membantu proses belajar.

---

# 26. Membuat QTable.cs

Buat:

\`\`\`text
Assets/Scripts/QTable.cs
\`\`\`

Script ini **tidak perlu** diturunkan dari MonoBehaviour karena hanya berfungsi sebagai class data.

\`\`\`csharp
using UnityEngine;

public class QTable
{
    private float[,] values;

    private int stateCount;
    private int actionCount;

    public QTable(int states, int actions)
    {
        stateCount = states;
        actionCount = actions;

        values = new float[stateCount, actionCount];
    }

    public float GetValue(int state, int action)
    {
        return values[state, action];
    }

    public void SetValue(int state, int action, float value)
    {
        values[state, action] = value;
    }

    public float GetMaxValue(int state)
    {
        float maxValue = values[state, 0];

        for (int action = 1; action < actionCount; action++)
        {
            if (values[state, action] > maxValue)
            {
                maxValue = values[state, action];
            }
        }

        return maxValue;
    }

    public int GetBestAction(int state)
    {
        int bestAction = 0;
        float bestValue = values[state, 0];

        for (int action = 1; action < actionCount; action++)
        {
            if (values[state, action] > bestValue)
            {
                bestValue = values[state, action];
                bestAction = action;
            }
        }

        return bestAction;
    }

    public void Reset()
    {
        values = new float[stateCount, actionCount];
    }

    public void PrintState(int state)
    {
        string output = $"State {state}: ";

        for (int action = 0; action < actionCount; action++)
        {
            output += $"A{action}={values[state, action]:F3} ";
        }

        Debug.Log(output);
    }
}
\`\`\`

---

# 27. Mengapa QTable Bukan MonoBehaviour?

Class:

\`\`\`csharp
QTable
\`\`\`

tidak membutuhkan:

- GameObject,
- Transform,
- Update,
- Inspector,
- Coroutine.

Karena itu lebih tepat dibuat sebagai class C# biasa.

Ini juga memperkenalkan prinsip:

\`\`\`text
Logic tidak harus selalu menjadi MonoBehaviour.
\`\`\`

---

# 28. Membuat Agent

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Agent
\`\`\`

Set Scale, misalnya:

\`\`\`text
X = 0.45
Y = 0.45
Z = 0.45
\`\`\`

Posisi sementara:

\`\`\`text
0, 0.5, 0
\`\`\`

Tambahkan material agar mudah terlihat.

---

# 29. Mendefinisikan Action

Kita gunakan enum:

\`\`\`csharp
public enum AgentAction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}
\`\`\`

Mapping:

\`\`\`text
Up    → z + 1
Down  → z - 1
Left  → x - 1
Right → x + 1
\`\`\`

---

# 30. Script QLearningAgent.cs

Buat:

\`\`\`text
Assets/Scripts/QLearningAgent.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

public class QLearningAgent : MonoBehaviour
{
    public enum AgentAction
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    [Header("References")]
    [SerializeField] private GridWorld gridWorld;

    [Header("Visual")]
    [SerializeField] private float agentHeight = 0.6f;

    private Vector2Int currentPosition;

    public Vector2Int CurrentPosition => currentPosition;

    public void ResetAgent()
    {
        currentPosition = gridWorld.StartPosition;
        UpdateWorldPosition();
    }

    public Vector2Int GetNextPosition(AgentAction action)
    {
        Vector2Int direction = Vector2Int.zero;

        switch (action)
        {
            case AgentAction.Up:
                direction = Vector2Int.up;
                break;

            case AgentAction.Down:
                direction = Vector2Int.down;
                break;

            case AgentAction.Left:
                direction = Vector2Int.left;
                break;

            case AgentAction.Right:
                direction = Vector2Int.right;
                break;
        }

        return currentPosition + direction;
    }

    public void MoveTo(Vector2Int position)
    {
        currentPosition = position;
        UpdateWorldPosition();
    }

    private void UpdateWorldPosition()
    {
        Vector3 worldPosition =
            gridWorld.GridToWorld(currentPosition);

        worldPosition.y = agentHeight;

        transform.position = worldPosition;
    }
}
\`\`\`

---

# 31. Fungsi QLearningAgent

Script ini mempunyai tanggung jawab sederhana:

\`\`\`text
menyimpan posisi agent
menghitung posisi berdasarkan action
memindahkan agent
reset ke Start
\`\`\`

Perhatikan bahwa script ini **belum melakukan learning**.

Learning nantinya ditempatkan pada:

\`\`\`text
TrainingManager
\`\`\`

Pembagian tanggung jawab seperti ini membuat kode lebih mudah dipahami.

---

# 32. Menghubungkan GridWorld

Pada GameObject \`Agent\`:

\`\`\`text
Add Component
→ QLearningAgent
\`\`\`

Drag:

\`\`\`text
GridWorld
\`\`\`

ke field:

\`\`\`text
Grid World
\`\`\`

Set:

\`\`\`text
Agent Height = 0.6
\`\`\`

---

# 33. Membuat TrainingManager

Buat Empty GameObject:

\`\`\`text
GameObject
→ Create Empty
\`\`\`

Rename:

\`\`\`text
TrainingManager
\`\`\`

---

# 34. Script TrainingManager.cs

Buat:

\`\`\`text
Assets/Scripts/TrainingManager.cs
\`\`\`

Isi:

\`\`\`csharp
using System.Collections;
using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private QLearningAgent agent;

    [Header("Q-Learning Parameters")]
    [Range(0f, 1f)]
    [SerializeField] private float learningRate = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float discountFactor = 0.9f;

    [Range(0f, 1f)]
    [SerializeField] private float epsilon = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float epsilonDecay = 0.995f;

    [Range(0f, 1f)]
    [SerializeField] private float minEpsilon = 0.05f;

    [Header("Reward")]
    [SerializeField] private float goalReward = 1.0f;
    [SerializeField] private float obstaclePenalty = -1.0f;
    [SerializeField] private float outOfBoundsPenalty = -1.0f;
    [SerializeField] private float stepPenalty = -0.01f;

    [Header("Training")]
    [SerializeField] private int totalEpisodes = 500;
    [SerializeField] private int maxStepsPerEpisode = 100;
    [SerializeField] private float stepDelay = 0.02f;
    [SerializeField] private bool trainOnStart = true;

    private const int ACTION_COUNT = 4;

    private QTable qTable;

    private int currentEpisode;
    private int currentStep;
    private float episodeReward;

    public int CurrentEpisode => currentEpisode;
    public int CurrentStep => currentStep;
    public float EpisodeReward => episodeReward;
    public float CurrentEpsilon => epsilon;

    private void Start()
    {
        qTable = new QTable(
            gridWorld.StateCount(),
            ACTION_COUNT
        );

        agent.ResetAgent();

        if (trainOnStart)
        {
            StartCoroutine(TrainingLoop());
        }
    }

    private IEnumerator TrainingLoop()
    {
        for (currentEpisode = 1;
             currentEpisode <= totalEpisodes;
             currentEpisode++)
        {
            yield return StartCoroutine(RunEpisode());

            epsilon = Mathf.Max(
                minEpsilon,
                epsilon * epsilonDecay
            );
        }

        Debug.Log("Training selesai.");

        yield return StartCoroutine(RunInferenceEpisode());
    }

    private IEnumerator RunEpisode()
    {
        agent.ResetAgent();

        currentStep = 0;
        episodeReward = 0f;

        while (currentStep < maxStepsPerEpisode)
        {
            currentStep++;

            int currentState =
                gridWorld.PositionToState(
                    agent.CurrentPosition
                );

            int action = ChooseAction(currentState);

            QLearningAgent.AgentAction selectedAction =
                (QLearningAgent.AgentAction)action;

            Vector2Int nextPosition =
                agent.GetNextPosition(selectedAction);

            float reward;
            bool episodeDone;

            EvaluateAction(
                nextPosition,
                out reward,
                out episodeDone
            );

            int nextState = currentState;

            if (gridWorld.IsInsideGrid(nextPosition) &&
                !gridWorld.IsObstacle(nextPosition))
            {
                agent.MoveTo(nextPosition);

                nextState =
                    gridWorld.PositionToState(
                        agent.CurrentPosition
                    );
            }

            UpdateQValue(
                currentState,
                action,
                reward,
                nextState,
                episodeDone
            );

            episodeReward += reward;

            if (stepDelay > 0f)
            {
                yield return new WaitForSeconds(stepDelay);
            }
            else
            {
                yield return null;
            }

            if (episodeDone)
            {
                break;
            }
        }

        Debug.Log(
            $"Episode {currentEpisode} | " +
            $"Steps: {currentStep} | " +
            $"Reward: {episodeReward:F3} | " +
            $"Epsilon: {epsilon:F3}"
        );
    }

    private int ChooseAction(int state)
    {
        if (Random.value < epsilon)
        {
            return Random.Range(0, ACTION_COUNT);
        }

        return qTable.GetBestAction(state);
    }

    private void EvaluateAction(
        Vector2Int nextPosition,
        out float reward,
        out bool done)
    {
        done = false;

        if (!gridWorld.IsInsideGrid(nextPosition))
        {
            reward = outOfBoundsPenalty;
            done = true;
            return;
        }

        if (gridWorld.IsObstacle(nextPosition))
        {
            reward = obstaclePenalty;
            done = true;
            return;
        }

        if (gridWorld.IsGoal(nextPosition))
        {
            reward = goalReward;
            done = true;
            return;
        }

        reward = stepPenalty;
    }

    private void UpdateQValue(
        int state,
        int action,
        float reward,
        int nextState,
        bool terminal)
    {
        float oldQ =
            qTable.GetValue(state, action);

        float futureQ =
            terminal
                ? 0f
                : qTable.GetMaxValue(nextState);

        float target =
            reward +
            discountFactor * futureQ;

        float newQ =
            oldQ +
            learningRate * (target - oldQ);

        qTable.SetValue(
            state,
            action,
            newQ
        );
    }

    private IEnumerator RunInferenceEpisode()
    {
        Debug.Log("Memulai inference...");

        agent.ResetAgent();

        for (int step = 0;
             step < maxStepsPerEpisode;
             step++)
        {
            int state =
                gridWorld.PositionToState(
                    agent.CurrentPosition
                );

            int action =
                qTable.GetBestAction(state);

            QLearningAgent.AgentAction selectedAction =
                (QLearningAgent.AgentAction)action;

            Vector2Int nextPosition =
                agent.GetNextPosition(selectedAction);

            if (!gridWorld.IsInsideGrid(nextPosition) ||
                gridWorld.IsObstacle(nextPosition))
            {
                Debug.LogWarning(
                    "Inference memilih posisi tidak valid."
                );

                yield break;
            }

            agent.MoveTo(nextPosition);

            yield return new WaitForSeconds(0.25f);

            if (gridWorld.IsGoal(nextPosition))
            {
                Debug.Log(
                    $"Inference mencapai goal dalam {step + 1} langkah."
                );

                yield break;
            }
        }

        Debug.LogWarning(
            "Inference tidak mencapai goal."
        );
    }

    public void PrintCurrentStateQValues()
    {
        int state =
            gridWorld.PositionToState(
                agent.CurrentPosition
            );

        qTable.PrintState(state);
    }
}
\`\`\`

---

# 35. Mengatur TrainingManager di Inspector

Pasang script:

\`\`\`text
TrainingManager.cs
\`\`\`

pada GameObject:

\`\`\`text
TrainingManager
\`\`\`

Set references:

\`\`\`text
Grid World → GridWorld
Agent      → Agent
\`\`\`

Parameter awal yang direkomendasikan:

\`\`\`text
Learning Rate          = 0.20
Discount Factor        = 0.90

Epsilon                = 0.80
Epsilon Decay          = 0.995
Min Epsilon            = 0.05

Goal Reward            = 1.00
Obstacle Penalty       = -1.00
Out Of Bounds Penalty  = -1.00
Step Penalty           = -0.01

Total Episodes         = 500
Max Steps Per Episode  = 100
Step Delay             = 0.02
Train On Start         = true
\`\`\`

---

# 36. Memahami ChooseAction()

Bagian:

\`\`\`csharp
if (Random.value < epsilon)
{
    return Random.Range(0, ACTION_COUNT);
}
\`\`\`

berarti:

\`\`\`text
Exploration
\`\`\`

Sedangkan:

\`\`\`csharp
return qTable.GetBestAction(state);
\`\`\`

berarti:

\`\`\`text
Exploitation
\`\`\`

Inilah implementasi epsilon-greedy.

---

# 37. Memahami UpdateQValue()

Kode:

\`\`\`csharp
float target =
    reward +
    discountFactor * futureQ;
\`\`\`

merepresentasikan:

\`\`\`text
r + γ max Q(s',a')
\`\`\`

Kemudian:

\`\`\`csharp
float newQ =
    oldQ +
    learningRate * (target - oldQ);
\`\`\`

merepresentasikan:

\`\`\`text
Q(s,a) + α[target - Q(s,a)]
\`\`\`

Dengan demikian kode tersebut merupakan implementasi langsung formula Q-Learning.

---

# 38. Terminal State

Jika:

\`\`\`text
Goal
Obstacle
Out of Bounds
\`\`\`

terjadi, episode selesai.

Pada terminal state:

\`\`\`csharp
futureQ = 0f;
\`\`\`

karena tidak ada lagi reward masa depan setelah episode selesai.

---

# 39. Menempatkan Kamera

Contoh untuk grid 5×5:

\`\`\`text
Main Camera Position:
X = 2.4
Y = 8
Z = -3
\`\`\`

Rotation:

\`\`\`text
X = 55
Y = 0
Z = 0
\`\`\`

Atur hingga seluruh grid terlihat.

Alternatif yang lebih mudah adalah kamera top-down.

Contoh:

\`\`\`text
Position:
X = 2.4
Y = 9
Z = 2.4

Rotation:
X = 90
Y = 0
Z = 0
\`\`\`

---

# 40. Menjalankan Praktikum Pertama Kali

Tekan:

\`\`\`text
Play
\`\`\`

Yang seharusnya terjadi:

\`\`\`text
Episode 1
Agent bergerak cukup acak
↓
Episode reset
↓
Episode 2
↓
...
↓
Q-value berubah
↓
Epsilon semakin kecil
↓
Training selesai
↓
Inference dijalankan
\`\`\`

---

# 41. Apa yang Perlu Diamati?

Pada awal training:

\`\`\`text
Agent bergerak acak.
Agent sering salah.
Agent menabrak obstacle.
Agent keluar grid.
\`\`\`

Setelah banyak episode:

\`\`\`text
Agent lebih sering memilih arah menuju Goal.
Kesalahan berkurang.
Policy menjadi lebih stabil.
\`\`\`

---

# 42. Console Unity

Buka:

\`\`\`text
Window
→ General
→ Console
\`\`\`

Contoh output:

\`\`\`text
Episode 1 | Steps: 4 | Reward: -1.030 | Epsilon: 0.800

Episode 20 | Steps: 18 | Reward: 0.830 | Epsilon: 0.727

Episode 100 | Steps: 9 | Reward: 0.920 | Epsilon: 0.487

Episode 500 | Steps: 8 | Reward: 0.930 | Epsilon: 0.066

Training selesai.

Inference mencapai goal dalam 8 langkah.
\`\`\`

Nilainya dapat berbeda karena proses Q-Learning bersifat stokastik.

---

# 43. Mengapa Hasil Setiap Training Bisa Berbeda?

Karena terdapat:

\`\`\`text
Random action
\`\`\`

pada proses exploration.

Akibatnya:

\`\`\`text
urutan pengalaman
dan
nilai Q
\`\`\`

dapat sedikit berbeda setiap training.

Ini normal pada Reinforcement Learning.

---

# 44. Membuat Debug UI

Agar praktikum lebih mudah diamati, tambahkan UI.

Buat:

\`\`\`text
GameObject
→ UI
→ Canvas
\`\`\`

Kemudian tambahkan TextMeshPro Text.

Jika Unity meminta:

\`\`\`text
Import TMP Essentials
\`\`\`

pilih:

\`\`\`text
Import TMP Essentials
\`\`\`

Rename Text:

\`\`\`text
TrainingInfoText
\`\`\`

---

# 45. Istilah TextMeshPro

**TextMeshPro/TMP** adalah sistem teks Unity dengan kualitas rendering dan pengaturan yang lebih baik dibanding sistem teks lama.

Namespace yang digunakan:

\`\`\`csharp
using TMPro;
\`\`\`

---

# 46. Script QLearningDebugUI.cs

Buat:

\`\`\`text
Assets/Scripts/QLearningDebugUI.cs
\`\`\`

Isi:

\`\`\`csharp
using TMPro;
using UnityEngine;

public class QLearningDebugUI : MonoBehaviour
{
    [SerializeField] private TrainingManager trainingManager;
    [SerializeField] private QLearningAgent agent;
    [SerializeField] private TMP_Text infoText;

    private void Update()
    {
        if (trainingManager == null ||
            agent == null ||
            infoText == null)
        {
            return;
        }

        infoText.text =
            $"Q-LEARNING TRAINING\\n" +
            $"Episode : {trainingManager.CurrentEpisode}\\n" +
            $"Step    : {trainingManager.CurrentStep}\\n" +
            $"Reward  : {trainingManager.EpisodeReward:F3}\\n" +
            $"Epsilon : {trainingManager.CurrentEpsilon:F3}\\n" +
            $"Agent   : {agent.CurrentPosition}";
    }
}
\`\`\`

---

# 47. Menyiapkan Debug UI

Pasang:

\`\`\`text
QLearningDebugUI.cs
\`\`\`

ke Canvas atau Empty GameObject:

\`\`\`text
DebugUI
\`\`\`

Drag references:

\`\`\`text
Training Manager
Agent
Info Text
\`\`\`

Sekarang informasi training akan terlihat langsung di Game View.

---

# 48. Hasil Debug UI

Contoh:

\`\`\`text
Q-LEARNING TRAINING

Episode : 128
Step    : 7
Reward  : -0.060
Epsilon : 0.422
Agent   : (3, 2)
\`\`\`

Visualisasi semacam ini sangat membantu dalam praktikum Reinforcement Learning.

---

# 49. Training Cepat vs Training Visual

Ada dua kebutuhan berbeda.

## Training Visual

Gunakan:

\`\`\`text
Step Delay = 0.05
\`\`\`

Tujuan:

\`\`\`text
mahasiswa dapat melihat agent bergerak
\`\`\`

---

## Training Cepat

Gunakan:

\`\`\`text
Step Delay = 0
\`\`\`

Training berlangsung jauh lebih cepat.

Namun script saat ini tetap menjalankan:

\`\`\`csharp
yield return null;
\`\`\`

sehingga training tidak langsung mengunci Editor dalam satu frame.

---

# 50. Mode Training dan Inference

## Training

Pada training:

\`\`\`text
epsilon > 0
Q-table diperbarui
Agent exploration + exploitation
\`\`\`

---

## Inference

Pada inference:

\`\`\`text
epsilon tidak digunakan
Q-table tidak diperbarui
Agent selalu memilih best action
\`\`\`

Kode:

\`\`\`csharp
qTable.GetBestAction(state);
\`\`\`

digunakan untuk memilih policy yang telah dipelajari.

---

# 51. Perbedaan Training dan Inference

| Aspek | Training | Inference |
|---|---|---|
| Tujuan | Belajar | Menggunakan hasil belajar |
| Exploration | Ya | Tidak |
| Q-table diubah | Ya | Tidak |
| Agent dapat acak | Ya | Tidak |
| Digunakan saat belajar | Ya | Tidak |
| Digunakan pada behavior final | Tidak selalu | Ya |

---

# 52. Eksperimen 1 — Mengubah Epsilon

Coba:

\`\`\`text
epsilon = 0.0
\`\`\`

Apa yang terjadi?

Agent hampir tidak melakukan exploration.

Masalah:

\`\`\`text
semua Q-value awal = 0
\`\`\`

Agent dapat terus memilih action pertama jika tidak memperoleh pengalaman yang cukup.

---

Kemudian coba:

\`\`\`text
epsilon = 1.0
\`\`\`

Agent selalu melakukan exploration di awal.

Bandingkan hasilnya.

---

# 53. Eksperimen 2 — Epsilon Decay

Coba:

\`\`\`text
epsilonDecay = 1.0
\`\`\`

Artinya epsilon tidak berkurang.

Bandingkan dengan:

\`\`\`text
epsilonDecay = 0.99
\`\`\`

atau:

\`\`\`text
0.995
\`\`\`

Pertanyaan:

> Mengapa exploration sebaiknya dikurangi setelah agent memiliki cukup pengalaman?

---

# 54. Eksperimen 3 — Step Penalty

Coba:

\`\`\`text
stepPenalty = 0
\`\`\`

Kemudian:

\`\`\`text
stepPenalty = -0.01
\`\`\`

Lalu:

\`\`\`text
stepPenalty = -0.1
\`\`\`

Analisis pengaruhnya terhadap panjang jalur.

Step penalty kecil mendorong agent mencapai goal menggunakan lebih sedikit langkah.

---

# 55. Eksperimen 4 — Learning Rate

Bandingkan:

\`\`\`text
0.05
0.2
0.5
0.9
\`\`\`

Perhatikan:

- kecepatan perubahan Q-value,
- stabilitas policy,
- jumlah episode hingga agent terlihat konsisten.

---

# 56. Eksperimen 5 — Discount Factor

Bandingkan:

\`\`\`text
gamma = 0.1
gamma = 0.5
gamma = 0.9
gamma = 0.99
\`\`\`

Pada Goal yang membutuhkan beberapa langkah, nilai gamma tinggi umumnya membuat reward Goal lebih efektif menyebar ke state-state sebelumnya.

---

# 57. Eksperimen 6 — Mengubah Obstacle

Tambahkan obstacle baru.

Contoh:

\`\`\`text
(1,0)
(1,1)
(1,2)
(3,2)
(3,3)
\`\`\`

Pastikan Goal masih memiliki jalur yang dapat dicapai.

Jalankan training kembali.

Perhatikan apakah policy berubah.

---

# 58. Eksperimen 7 — Grid Lebih Besar

Ubah:

\`\`\`text
Width  = 10
Height = 10
\`\`\`

Goal:

\`\`\`text
(9,9)
\`\`\`

Jumlah state menjadi:

\`\`\`text
10 × 10 = 100 state
\`\`\`

Bandingkan dengan grid 5×5:

\`\`\`text
25 state
\`\`\`

Pertanyaan:

> Apakah jumlah episode yang sama masih cukup?

Eksperimen ini menunjukkan awal dari **state-space problem**.

---

# 59. State-Space Problem

Jika jumlah state bertambah besar:

\`\`\`text
jumlah isi Q-table juga bertambah
\`\`\`

Misalnya:

\`\`\`text
Grid 5 × 5
25 state × 4 action
= 100 Q-value
\`\`\`

Sedangkan:

\`\`\`text
Grid 100 × 100
10.000 state × 4 action
= 40.000 Q-value
\`\`\`

Jika state juga mencakup:

\`\`\`text
health
enemy position
ammo
distance
status
\`\`\`

jumlah kemungkinan state dapat meningkat sangat besar.

Inilah salah satu alasan Q-table klasik kurang cocok untuk game yang kompleks.

---

# 60. Discretization

Q-Learning klasik biasanya membutuhkan state diskrit.

Misalnya distance sebenarnya:

\`\`\`text
2.1
2.2
2.3
...
\`\`\`

Dapat dikategorikan menjadi:

\`\`\`text
Near
Medium
Far
\`\`\`

Contoh:

\`\`\`csharp
if (distance < 3f)
    state = Near;
else if (distance < 8f)
    state = Medium;
else
    state = Far;
\`\`\`

Proses tersebut disebut:

\`\`\`text
Discretization
\`\`\`

---

# 61. Reward Design

Reward merupakan bagian kritis RL.

Reward praktikum:

\`\`\`text
Goal            +1.0
Obstacle        -1.0
Out of Bounds   -1.0
Step            -0.01
\`\`\`

Interpretasinya:

\`\`\`text
Goal
→ sangat baik

Obstacle
→ sangat buruk

Keluar grid
→ sangat buruk

Langkah
→ sedikit buruk
\`\`\`

Akibatnya agent didorong untuk:

\`\`\`text
mencapai goal
+
menghindari kesalahan
+
mengurangi jumlah langkah
\`\`\`

---

# 62. Reward Hacking

**Reward hacking** terjadi jika agent menemukan cara memperoleh reward yang secara matematis bagus tetapi tidak sesuai tujuan designer.

Contoh buruk:

\`\`\`text
+0.1 setiap kali berada dekat Goal
\`\`\`

tetapi:

\`\`\`text
Goal hanya memberi +0.5
\`\`\`

Agent mungkin belajar:

\`\`\`text
bolak-balik dekat Goal
\`\`\`

karena mendapatkan lebih banyak reward daripada menyelesaikan episode.

Pelajaran:

> Reward mendefinisikan tujuan agent. Kesalahan reward design dapat menghasilkan behavior yang tidak diinginkan.

---

# 63. Penambahan Maksimum Step

Parameter:

\`\`\`text
maxStepsPerEpisode
\`\`\`

penting untuk mencegah episode berlangsung selamanya.

Misalnya agent:

\`\`\`text
berputar-putar
atau
tidak pernah mencapai goal
\`\`\`

Jika:

\`\`\`text
maxStepsPerEpisode = 100
\`\`\`

episode otomatis berakhir setelah 100 step.

---

# 64. Troubleshooting — Grid Tidak Muncul

Periksa:

1. \`GridCell.prefab\` sudah dibuat.
2. \`Cell Prefab\` pada GridWorld sudah terisi.
3. Script \`GridWorld.cs\` terpasang.
4. Tidak ada error merah di Console.
5. Camera menghadap ke posisi grid.

---

# 65. Troubleshooting — Agent Tidak Terlihat

Periksa:

\`\`\`text
Agent Height
Camera
Scale Agent
Material Agent
GridWorld reference
\`\`\`

Pastikan Agent berada sedikit di atas permukaan cell.

Contoh:

\`\`\`text
Agent Height = 0.6
\`\`\`

---

# 66. Troubleshooting — NullReferenceException

Contoh error:

\`\`\`text
NullReferenceException
\`\`\`

Biasanya berarti reference Inspector belum diisi.

Periksa:

\`\`\`text
TrainingManager
├── Grid World
└── Agent

QLearningAgent
└── Grid World

QLearningDebugUI
├── Training Manager
├── Agent
└── Info Text
\`\`\`

---

# 67. Troubleshooting — Agent Tidak Pernah Sampai Goal

Periksa:

1. Goal tidak tertutup seluruhnya oleh obstacle.
2. Start bukan obstacle.
3. Goal bukan obstacle.
4. Koordinat obstacle benar.
5. Total episode cukup.
6. Epsilon tidak terlalu rendah dari awal.
7. Step maksimum tidak terlalu kecil.
8. Reward goal cukup positif.

Coba:

\`\`\`text
Total Episodes = 1000
\`\`\`

jika environment lebih sulit.

---

# 68. Troubleshooting — Agent Tetap Acak Setelah Training

Periksa:

\`\`\`text
epsilonDecay
learningRate
reward design
jumlah episode
\`\`\`

Pastikan epsilon turun.

Contoh:

\`\`\`text
Initial epsilon = 0.8
Minimum epsilon = 0.05
\`\`\`

---

# 69. Troubleshooting — Agent Selalu Memilih Up

Jika seluruh Q-value masih:

\`\`\`text
0
\`\`\`

fungsi:

\`\`\`csharp
GetBestAction()
\`\`\`

akan mengembalikan action pertama.

Karena itu exploration melalui epsilon sangat penting pada awal training.

---

# 70. Pengembangan Opsional — Tombol Start Training

Mahasiswa yang sudah menyelesaikan bagian dasar dapat menambahkan:

\`\`\`text
Button Start Training
Button Reset Q-Table
Button Run Inference
Button Print Q-Table
\`\`\`

Dengan demikian training tidak harus otomatis berjalan melalui:

\`\`\`text
trainOnStart
\`\`\`

---

# 71. Pengembangan Opsional — Policy Arrow

Setiap cell dapat menampilkan:

\`\`\`text
↑
↓
←
→
\`\`\`

sesuai action dengan Q-value terbesar.

Misalnya setelah training:

\`\`\`text
→ → ↓ ↓ ↓
↑ # → # ↓
↑ # → → ↓
↑ ← # → ↓
→ → → → G
\`\`\`

Visualisasi ini disebut:

\`\`\`text
policy visualization
\`\`\`

dan sangat direkomendasikan untuk demonstrasi kelas.

---

# 72. Pengembangan Opsional — Warna Berdasarkan Q-Value

Cell juga dapat diberi intensitas berdasarkan Q-value terbaik.

Contoh:

\`\`\`text
nilai tinggi
→ cell semakin terang

nilai rendah
→ cell semakin gelap
\`\`\`

Hal ini membantu mahasiswa memahami bagaimana reward Goal menyebar ke state-state sebelumnya.

---

# 73. Pengembangan Opsional — Grafik Reward

Simpan total reward setiap episode:

\`\`\`text
Episode
Reward
\`\`\`

Kemudian buat grafik:

\`\`\`text
Reward
  ↑
  │              ______
  │          ___/
  │      ___/
  │_____/______________→ Episode
\`\`\`

Jika training berhasil, reward rata-rata biasanya menunjukkan kecenderungan membaik.

---

# 74. Q-Learning vs A*

Q-Learning bukan pengganti A*.

| Aspek | A* | Q-Learning |
|---|---|---|
| Kategori | Pathfinding | Reinforcement Learning |
| Training | Tidak | Ya |
| Input utama | Graph + heuristic | Reward + pengalaman |
| Output | Path | Policy |
| Trial-and-error | Tidak | Ya |
| Deterministik | Lebih mudah | Tidak selalu |
| Cocok untuk pathfinding murni | Sangat cocok | Tidak selalu |
| Tujuan pembelajaran RL | Tidak | Sangat cocok |

---

# 75. Mengapa Tidak Menggunakan A* pada Praktikum Ini?

Karena tujuan praktikum bukan:

\`\`\`text
menemukan algoritma tercepat menuju Goal
\`\`\`

melainkan:

\`\`\`text
memahami bagaimana agent belajar melalui reward
\`\`\`

A* mengetahui struktur graph dan mencari jalur.

Q-Learning memperoleh policy melalui pengalaman.

---

# 76. Hubungan dengan Praktikum Sebelumnya

Praktikum sebelumnya banyak menggunakan:

\`\`\`text
developer menentukan aturan
\`\`\`

Contoh:

\`\`\`text
FSM
Behavior Tree
Utility AI
Pathfinding
DDA
Player Modeling
\`\`\`

Pada praktikum ini mulai diperkenalkan:

\`\`\`text
Learning-Based AI
\`\`\`

di mana keputusan diperoleh melalui pengalaman.

---

# 77. Hubungan dengan Pertemuan 12

Pertemuan 12 memperkenalkan:

\`\`\`text
Gameplay Data
↓
Metrics
↓
Player Model
↓
Adaptation
\`\`\`

Pertemuan 13 memperluas konsep data-driven system ke:

\`\`\`text
Agent
↓
Interaction
↓
Reward
↓
Learning
↓
Policy
\`\`\`

Keduanya menunjukkan bahwa Game AI tidak selalu harus ditulis sepenuhnya sebagai aturan eksplisit.

---

# 78. Hubungan dengan Pertemuan 14

Praktikum ini menjadi dasar memahami:

\`\`\`text
Agent
Observation
Action
Reward
Episode
Policy
Training
Inference
\`\`\`

Konsep yang sama akan digunakan kembali pada:

\`\`\`text
Unity ML-Agents
\`\`\`

pada pertemuan berikutnya.

Perbedaannya:

\`\`\`text
Q-Learning sekarang:
Q-table sederhana

ML-Agents berikutnya:
policy dapat dipelajari menggunakan
algoritma dan model yang lebih kompleks
\`\`\`

---

# 79. Rekomendasi Alur Pelaksanaan di Laboratorium

## Tahap A — Konsep

Durasi awal praktikum:

\`\`\`text
State
Action
Reward
Episode
Q-table
Epsilon
\`\`\`

---

## Tahap B — Environment

Mahasiswa membuat:

\`\`\`text
GridWorld
GridCell
Start
Goal
Obstacle
\`\`\`

---

## Tahap C — Agent

Mahasiswa membuat:

\`\`\`text
QLearningAgent
\`\`\`

dan memeriksa pergerakan antar-cell.

---

## Tahap D — Q-Table

Mahasiswa mengimplementasikan:

\`\`\`text
GetValue
SetValue
GetMaxValue
GetBestAction
\`\`\`

---

## Tahap E — Training

Implementasikan:

\`\`\`text
Episode
Epsilon-Greedy
Reward
Q Update
\`\`\`

---

## Tahap F — Observasi

Amati:

\`\`\`text
Episode
Step
Reward
Epsilon
\`\`\`

---

## Tahap G — Inference

Setelah training:

\`\`\`text
agent menggunakan best action
\`\`\`

---

## Tahap H — Eksperimen

Mahasiswa mengubah:

\`\`\`text
epsilon
alpha
gamma
reward
obstacle
\`\`\`

kemudian menganalisis hasil.

---

# 80. Parameter Rekomendasi Final

Untuk praktikum standar:

\`\`\`text
Grid:
5 × 5

Learning Rate:
0.20

Discount Factor:
0.90

Initial Epsilon:
0.80

Epsilon Decay:
0.995

Minimum Epsilon:
0.05

Goal Reward:
+1.00

Obstacle Penalty:
-1.00

Out-of-Bounds Penalty:
-1.00

Step Penalty:
-0.01

Episodes:
500–1000

Maximum Steps:
100
\`\`\`

Konfigurasi tersebut merupakan titik awal, bukan satu-satunya konfigurasi benar.

---

# 81. Eksperimen Wajib Mahasiswa

Lakukan minimal tiga eksperimen.

## Eksperimen A

\`\`\`text
epsilon:
0.1
0.5
0.9
\`\`\`

Analisis exploration.

---

## Eksperimen B

\`\`\`text
learningRate:
0.1
0.5
0.9
\`\`\`

Analisis kecepatan pembelajaran.

---

## Eksperimen C

\`\`\`text
stepPenalty:
0
-0.01
-0.1
\`\`\`

Analisis panjang jalur.

---

# 82. Data yang Dicatat

Untuk setiap eksperimen, catat:

| Parameter | Episode | Step Akhir | Reward | Epsilon Akhir | Berhasil? |
|---|---:|---:|---:|---:|---|
| Konfigurasi 1 | | | | | |
| Konfigurasi 2 | | | | | |
| Konfigurasi 3 | | | | | |

Mahasiswa kemudian membandingkan hasilnya.

---

# 83. Pertanyaan Analisis

Jawab pertanyaan berikut:

1. Apa yang dimaksud dengan state dalam praktikum?
2. Mengapa posisi agent digunakan sebagai state?
3. Berapa jumlah state pada grid 5×5?
4. Apa saja action yang tersedia?
5. Mengapa Goal diberi reward positif?
6. Mengapa obstacle diberi penalty?
7. Apa fungsi step penalty?
8. Apa fungsi learning rate?
9. Apa fungsi discount factor?
10. Apa fungsi epsilon?
11. Mengapa epsilon dikurangi selama training?
12. Apa perbedaan exploration dan exploitation?
13. Apa isi Q-table?
14. Bagaimana agent memilih action saat inference?
15. Mengapa Q-Learning membutuhkan episode berulang?
16. Mengapa hasil setiap training dapat sedikit berbeda?
17. Apa yang terjadi jika epsilon selalu nol?
18. Apa yang terjadi jika epsilon selalu satu?
19. Mengapa Q-Learning kurang cocok untuk state yang sangat besar?
20. Apa perbedaan Q-Learning dengan A*?

---

# 84. Tugas Pengembangan

Pilih minimal satu:

### Pilihan A
Tambahkan Policy Arrow.

### Pilihan B
Tambahkan tombol:

\`\`\`text
Start Training
Reset
Run Inference
\`\`\`

### Pilihan C
Tambahkan jumlah Goal yang berhasil dicapai.

### Pilihan D
Tambahkan total reward rata-rata per 10 episode.

### Pilihan E
Ubah grid menjadi:

\`\`\`text
8 × 8
\`\`\`

dan analisis kebutuhan episode.

### Pilihan F
Tambahkan dua jenis obstacle dengan penalty berbeda.

---

# 85. Checklist Keberhasilan Praktikum

- [ ] Project Unity dapat dibuka tanpa compilation error.
- [ ] Scene \`QLearningGridScene\` berhasil dibuat.
- [ ] Grid World muncul.
- [ ] Start dan Goal memiliki posisi berbeda.
- [ ] Obstacle berhasil ditempatkan.
- [ ] Agent muncul di posisi Start.
- [ ] Agent dapat melakukan empat action.
- [ ] Q-table berhasil dibuat.
- [ ] Epsilon-greedy berhasil dijalankan.
- [ ] Reward Goal diberikan.
- [ ] Penalty obstacle diberikan.
- [ ] Step penalty bekerja.
- [ ] Episode berhasil di-reset.
- [ ] Q-value diperbarui.
- [ ] Epsilon berkurang selama training.
- [ ] Informasi training terlihat di Console/UI.
- [ ] Agent menunjukkan perubahan perilaku setelah training.
- [ ] Mode inference dapat dijalankan.
- [ ] Agent dapat mencapai Goal menggunakan policy hasil training.
- [ ] Mahasiswa melakukan eksperimen parameter.
- [ ] Mahasiswa dapat menjelaskan hasil eksperimen.

---

# 86. Kesimpulan Praktikum

Pada praktikum ini telah dibuat sistem:

\`\`\`text
Grid World
    ↓
Agent
    ↓
State
    ↓
Action
    ↓
Reward
    ↓
Q-Learning
    ↓
Q-Table
    ↓
Policy
    ↓
Inference
\`\`\`

Hal penting yang harus dipahami adalah bahwa Agent **tidak diberikan jalur menuju Goal secara langsung**.

Agent belajar melalui:

\`\`\`text
trial
↓
error
↓
reward
↓
update Q-value
↓
pengalaman berulang
\`\`\`

Dengan demikian, praktikum ini memperlihatkan perbedaan mendasar antara:

\`\`\`text
Rule-Based AI
\`\`\`

dan:

\`\`\`text
Learning-Based AI
\`\`\`

---

# 87. Rekomendasi Akhir Praktikum Pertemuan 13

## Praktikum Utama

**Q-Learning Grid Agent**

Pilihan ini paling direkomendasikan karena:

- langsung berhubungan dengan materi kuliah,
- mudah dijalankan di Unity 6,
- tidak membutuhkan Python,
- tidak bergantung pada package ML tambahan,
- mudah di-debug,
- mudah dipresentasikan di kelas,
- memperlihatkan Q-table secara nyata,
- memperlihatkan exploration dan exploitation,
- sangat baik sebagai dasar menuju Unity ML-Agents.

---

# 88. Nama Project yang Direkomendasikan

## Pilihan utama

\`\`\`text
GameAI_13_QLearningGrid
\`\`\`

Nama ini paling saya rekomendasikan karena:

\`\`\`text
GameAI
\`\`\`

menunjukkan domain mata kuliah,

\`\`\`text
13
\`\`\`

menunjukkan nomor praktikum/pertemuan,

dan:

\`\`\`text
QLearningGrid
\`\`\`

menunjukkan implementasi utama project.

Alternatif nama:

\`\`\`text
GameAI_QLearningGridWorld
SmartGame_QLearningAgent
RL_GridWorld_Unity
GameAI13_RLGridAgent
LearningAgent_GridWorld
\`\`\`

Untuk konsistensi dengan project praktikum satu semester, gunakan:

\`\`\`text
GameAI_13_QLearningGrid
\`\`\`

---

# 89. Struktur Akhir Hierarchy

Hierarchy yang direkomendasikan:

\`\`\`text
QLearningGridScene
│
├── Main Camera
├── Directional Light
│
├── GridWorld
│   ├── Cell_0_0_Start
│   ├── Cell_1_0_Normal
│   ├── ...
│   └── Cell_4_4_Goal
│
├── Agent
│
├── TrainingManager
│
└── Canvas
    ├── Panel
    └── TrainingInfoText
\`\`\`

Cell akan menjadi child \`GridWorld\` secara otomatis karena dibuat melalui:

\`\`\`csharp
Instantiate(
    cellPrefab,
    worldPosition,
    Quaternion.identity,
    transform
);
\`\`\`

Parameter:

\`\`\`text
transform
\`\`\`

di sini berarti GameObject tempat script GridWorld dipasang menjadi parent object baru.

---

# 90. Struktur Script Akhir

\`\`\`text
Scripts/
│
├── GridCell.cs
│   └── Menyimpan informasi masing-masing cell
│
├── GridWorld.cs
│   └── Membuat dan mengelola environment grid
│
├── QTable.cs
│   └── Menyimpan nilai Q(state, action)
│
├── QLearningAgent.cs
│   └── Mengelola posisi dan movement agent
│
├── TrainingManager.cs
│   └── Menjalankan algoritma Q-Learning
│
└── QLearningDebugUI.cs
    └── Menampilkan informasi proses training
\`\`\`

Arsitektur tersebut sengaja memisahkan:

\`\`\`text
Environment
Agent
Learning Data
Training Logic
Visualization
\`\`\`

agar setiap tanggung jawab mudah dipahami mahasiswa.

---

# 91. Konsep yang Harus Dikuasai Setelah Praktikum

Mahasiswa tidak cukup hanya mendapatkan agent yang bisa mencapai Goal.

Mahasiswa harus dapat menjelaskan:

\`\`\`text
Mengapa agent belajar?
\`\`\`

Jawabannya bukan:

\`\`\`text
karena Unity otomatis mencari jalur
\`\`\`

melainkan:

\`\`\`text
karena setiap interaksi menghasilkan reward,
reward digunakan untuk memperbarui Q-value,
Q-value menyimpan estimasi kualitas action,
dan policy akhirnya memilih action dengan
Q-value terbesar.
\`\`\`

Inilah inti Praktikum 13.

---

# 92. Persiapan Menuju Praktikum 14

Setelah memahami Q-Learning sederhana, mahasiswa telah mengenal konsep:

\`\`\`text
Agent
Environment
Observation/State
Action
Reward
Episode
Policy
Training
Inference
Exploration
\`\`\`

Pada Praktikum 14, konsep tersebut dapat diterapkan menggunakan:

\`\`\`text
Unity ML-Agents
\`\`\`

untuk melatih agent pada environment Unity yang lebih kompleks.

Dengan demikian urutan pembelajaran menjadi:

\`\`\`text
Pertemuan 13
Q-Learning sederhana
        ↓
memahami konsep RL
        ↓
Pertemuan 14
Unity ML-Agents
        ↓
training agent yang lebih kompleks
\`\`\`

Ini merupakan urutan praktikum yang paling direkomendasikan agar mahasiswa memahami **konsep terlebih dahulu sebelum menggunakan framework Machine Learning**.`,xc=`# MODUL PRAKTIKUM 14  
# Reinforcement Learning dengan Unity ML-Agents

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Tools:** Unity 6, Unity ML-Agents, C#, Python  
**Algoritma Training:** PPO — Proximal Policy Optimization  

---

# 1. Judul Praktikum

## Training Agent Menggunakan Unity ML-Agents

Pada praktikum ini mahasiswa akan membuat sebuah **learning agent** yang belajar bergerak menuju sebuah target pada arena 3D.

Agent tidak diberi aturan seperti:

\`\`\`text
IF target di kanan
THEN bergerak ke kanan
\`\`\`

Sebaliknya, agent hanya diberikan:

\`\`\`text
Observation
    ↓
Policy
    ↓
Action
    ↓
Environment berubah
    ↓
Reward
    ↓
Training
\`\`\`

Melalui ribuan percobaan, agent akan belajar sebuah **policy** untuk mencapai target dengan efektif.

Praktikum ini menerapkan langsung materi Pertemuan 14 mengenai Agent, Environment, Observation, Action, Reward, Episode, Policy, PPO, Behavior Parameters, Decision Requester, training, dan inference.

---

# 2. Rekomendasi Nama Project Unity

## Nama yang direkomendasikan

\`\`\`text
GC14_MLAgents_MoveToTarget_PPO
\`\`\`

### Alasan

- \`GC14\` → Game Cerdas Praktikum/Pertemuan 14.
- \`MLAgents\` → menggunakan Unity ML-Agents.
- \`MoveToTarget\` → menggambarkan tugas utama agent.
- \`PPO\` → algoritma Reinforcement Learning yang digunakan.

Alternatif nama yang lebih sederhana:

\`\`\`text
GC14_MLAgents_MoveToTarget
\`\`\`

atau:

\`\`\`text
IntelligentAgentTraining
\`\`\`

### Rekomendasi utama

Gunakan:

\`\`\`text
GC14_MLAgents_MoveToTarget_PPO
\`\`\`

karena paling jelas ketika seluruh project praktikum Game Cerdas dikumpulkan dalam satu semester.

---

# 3. Tujuan Praktikum

Setelah menyelesaikan praktikum ini mahasiswa diharapkan mampu:

1. Membuat environment Reinforcement Learning di Unity.
2. Membuat GameObject yang berfungsi sebagai ML-Agent.
3. Membuat class yang mewarisi \`Agent\`.
4. Mendefinisikan observation untuk agent.
5. Mendefinisikan continuous action.
6. Mendesain reward dan penalty.
7. Membuat mekanisme episode.
8. Melakukan randomisasi posisi agent dan target.
9. Menggunakan \`Behavior Parameters\`.
10. Menggunakan \`Decision Requester\`.
11. Menguji agent menggunakan \`Heuristic()\`.
12. Membuat konfigurasi PPO.
13. Menjalankan training menggunakan \`mlagents-learn\`.
14. Mengamati perkembangan cumulative reward.
15. Menghasilkan model hasil training.
16. Menggunakan model tersebut dalam mode inference.
17. Menjelaskan hubungan observation, action, reward, dan policy.

---

# 4. Gambaran Praktikum

Agent berada di sebuah platform.

\`\`\`text
+--------------------------------------+
|                                      |
|              TARGET                  |
|                ●                     |
|                                      |
|                                      |
|       AGENT                          |
|         ●                            |
|                                      |
+--------------------------------------+
\`\`\`

Tugas agent:

> Bergerak menuju target secepat mungkin tanpa jatuh dari arena.

Setiap episode:

\`\`\`text
Agent ditempatkan random
        ↓
Target ditempatkan random
        ↓
Agent mengamati target
        ↓
Agent memilih arah bergerak
        ↓
Mencapai target?
   ├── Ya → Reward +1 → EndEpisode
   └── Tidak
           ↓
       Jatuh?
       ├── Ya → Reward -1 → EndEpisode
       └── Tidak → lanjut
\`\`\`

---

# 5. Konsep Reinforcement Learning yang Digunakan

## 5.1 Agent

**Agent** adalah entitas yang belajar mengambil keputusan.

Pada praktikum:

\`\`\`text
Agent = bola/capsule yang bergerak di arena
\`\`\`

Di Unity, GameObject Agent akan memiliki:

- \`Collider\`
- \`Rigidbody\`
- \`MoveToTargetAgent.cs\`
- \`Behavior Parameters\`
- \`Decision Requester\`

Class agent dibuat dengan:

\`\`\`csharp
public class MoveToTargetAgent : Agent
\`\`\`

Artinya class tersebut mewarisi kemampuan dasar dari class \`Agent\` milik Unity ML-Agents.

---

# 6. Environment

Environment adalah dunia tempat agent belajar.

Environment praktikum terdiri dari:

\`\`\`text
TrainingArea
├── Ground
├── Agent
├── Target
└── Camera
\`\`\`

Environment menentukan:

- tempat agent bergerak;
- posisi target;
- batas arena;
- kondisi berhasil;
- kondisi gagal;
- kapan episode di-reset.

---

# 7. Observation

**Observation** adalah informasi yang diketahui agent.

Pertanyaan pentingnya adalah:

> Informasi apa yang dibutuhkan agent agar mampu mengambil keputusan?

Dalam praktikum ini agent memperoleh:

\`\`\`text
1. arah relatif menuju target pada sumbu X
2. arah relatif menuju target pada sumbu Z
3. jarak menuju target
4. velocity agent pada X
5. velocity agent pada Z
\`\`\`

Total:

\`\`\`text
5 observation
\`\`\`

---

# 8. Mengapa Menggunakan Posisi Relatif?

Kita tidak memberikan:

\`\`\`text
Agent world position
Target world position
\`\`\`

sebagai observation utama.

Sebaliknya:

\`\`\`csharp
Vector3 toTarget =
    target.localPosition - transform.localPosition;
\`\`\`

Dengan demikian agent belajar:

\`\`\`text
"target berada di arah mana relatif terhadap saya?"
\`\`\`

bukan:

\`\`\`text
"target selalu berada pada koordinat tertentu"
\`\`\`

Pendekatan ini membantu agent melakukan generalisasi ketika posisi awal dan target berubah.

---

# 9. Action

Action adalah hal yang dapat dilakukan agent.

Praktikum menggunakan **Continuous Action**.

Terdapat dua output:

\`\`\`text
Action 0 → moveX
Action 1 → moveZ
\`\`\`

Nilainya berada di sekitar rentang:

\`\`\`text
-1 sampai +1
\`\`\`

Contoh:

\`\`\`text
moveX = +1
moveZ = 0
\`\`\`

berarti bergerak ke kanan.

Sedangkan:

\`\`\`text
moveX = 0
moveZ = +1
\`\`\`

berarti bergerak maju.

Unity ML-Agents PPO mendukung continuous actions, dan dokumentasi ML-Agents menyatakan output continuous PPO dibatasi ke rentang \`[-1,1]\`.

---

# 10. Reward

Reward adalah feedback yang digunakan agent untuk mengetahui apakah perilakunya baik atau buruk.

Reward yang digunakan:

| Kondisi | Reward |
|---|---:|
| Mencapai target | \`+1.0\` |
| Jatuh dari arena | \`-1.0\` |
| Setiap decision step | \`-0.001\` |

Tujuan step penalty:

\`\`\`text
Mendorong agent menyelesaikan tugas lebih cepat.
\`\`\`

Agent yang berputar-putar terlalu lama akan mengumpulkan penalty lebih banyak.

Unity merekomendasikan reward per keputusan berada pada skala yang wajar dan umumnya sekitar \`[-1,1]\` agar training lebih stabil.

---

# 11. Episode

Episode adalah satu percobaan agent.

Contoh:

\`\`\`text
EPISODE 1

Agent spawn
↓
bergerak
↓
jatuh
↓
reward -1
↓
EndEpisode()
\`\`\`

Kemudian:

\`\`\`text
EPISODE 2

Agent spawn kembali
↓
target pindah
↓
agent mencoba strategi lain
↓
target tercapai
↓
reward +1
↓
EndEpisode()
\`\`\`

Proses tersebut terjadi berkali-kali selama training.

---

# 12. Policy

**Policy** dapat dipahami sebagai:

> Strategi agent dalam memilih action berdasarkan observation.

Secara sederhana:

\`\`\`text
Observation
       ↓
Neural Network
       ↓
Action
\`\`\`

Contoh policy yang akhirnya mungkin dipelajari:

\`\`\`text
Target kanan → dorong ke kanan
Target depan → dorong ke depan
Terlalu cepat → kurangi gaya
\`\`\`

Kita tidak menulis aturan tersebut secara manual.

Agent mempelajarinya melalui training.

---

# 13. PPO

PPO adalah singkatan dari:

## Proximal Policy Optimization

PPO merupakan algoritma Reinforcement Learning yang dapat digunakan oleh trainer ML-Agents.

Dalam praktikum ini mahasiswa **tidak perlu mengimplementasikan matematika PPO**.

Secara konseptual:

\`\`\`text
Agent melakukan banyak percobaan
        ↓
Menghasilkan pengalaman
        ↓
Observation + Action + Reward
        ↓
PPO mengevaluasi policy
        ↓
Bobot neural network diperbaiki
        ↓
Policy baru
        ↓
Agent mencoba lagi
\`\`\`

Tujuan PPO:

\`\`\`text
Mencari policy yang menghasilkan
cumulative reward sebesar mungkin.
\`\`\`

---

# 14. Software yang Diperlukan

Gunakan:

\`\`\`text
Unity 6
Unity ML-Agents
Python
ML-Agents Python Trainer
Code Editor
\`\`\`

Dokumentasi resmi ML-Agents saat ini mensyaratkan Unity \`6000.0\` atau lebih baru dan merekomendasikan Python \`3.10.12\`. Untuk menghindari konflik library Python, virtual environment juga direkomendasikan.

---

# 15. LANGKAH 1 — Membuat Project Unity

Buka:

\`\`\`text
Unity Hub
\`\`\`

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D Core
\`\`\`

Nama project:

\`\`\`text
GC14_MLAgents_MoveToTarget_PPO
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 16. LANGKAH 2 — Membuat Struktur Folder

Pada jendela \`Project\`, buat struktur:

\`\`\`text
Assets
├── Scenes
├── Scripts
├── Materials
├── Prefabs
├── Models
└── MLConfigs
\`\`\`

### Fungsi folder

**Scenes**

Menyimpan scene Unity.

**Scripts**

Menyimpan script C#.

**Materials**

Menyimpan material visual.

**Prefabs**

Menyimpan template GameObject.

**Models**

Menyimpan model ML hasil training.

**MLConfigs**

Menyimpan konfigurasi training \`.yaml\`.

---

# 17. LANGKAH 3 — Menyimpan Scene

Simpan scene sebagai:

\`\`\`text
Assets/Scenes/MLAgents_MoveToTarget.unity
\`\`\`

Gunakan:

\`\`\`text
File → Save As
\`\`\`

---

# 18. LANGKAH 4 — Instalasi Unity ML-Agents

Buka:

\`\`\`text
Window
→ Package Manager
\`\`\`

Cari package:

\`\`\`text
ML Agents
\`\`\`

Kemudian install package:

\`\`\`text
com.unity.ml-agents
\`\`\`

Unity mendistribusikan ML-Agents C# SDK sebagai package \`com.unity.ml-agents\`; dokumentasi resmi juga menyediakan opsi instalasi package lokal dari repository apabila package tidak tampil di registry.

> Catatan: jangan memaksakan nomor versi package dari modul apabila versi Unity yang dipakai laboratorium berbeda. Gunakan versi ML-Agents yang kompatibel dengan editor Unity 6 yang digunakan.

---

# 19. LANGKAH 5 — Menyiapkan Python Environment

## Pilihan yang direkomendasikan

Gunakan virtual environment.

Contoh menggunakan Conda:

\`\`\`bash
conda create -n mlagents python=3.10.12
\`\`\`

Aktifkan:

\`\`\`bash
conda activate mlagents
\`\`\`

Dokumentasi instalasi ML-Agents resmi merekomendasikan Python \`3.10.12\` serta Conda/Mamba atau virtual environment lain untuk mengisolasi dependency.

---

# 20. Instalasi Python ML-Agents

Cara yang paling aman untuk lingkungan praktikum adalah mengikuti package Python yang cocok dengan release ML-Agents yang digunakan.

Dokumentasi resmi saat ini merekomendasikan clone release stable:

\`\`\`bash
git clone --branch release_22 https://github.com/Unity-Technologies/ml-agents.git
\`\`\`

Masuk ke directory:

\`\`\`bash
cd ml-agents
\`\`\`

Kemudian:

\`\`\`bash
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents
\`\`\`

Validasi:

\`\`\`bash
mlagents-learn --help
\`\`\`

Jika help tampil, trainer telah terpasang.

---

# 21. LANGKAH 6 — Membuat TrainingArea

Pada Hierarchy:

\`\`\`text
Create Empty
\`\`\`

Rename:

\`\`\`text
TrainingArea
\`\`\`

Transform:

\`\`\`text
Position = (0, 0, 0)
Rotation = (0, 0, 0)
Scale    = (1, 1, 1)
\`\`\`

TrainingArea berfungsi sebagai parent environment.

---

# 22. Mengapa TrainingArea Penting?

Nanti seluruh posisi agent dan target menggunakan:

\`\`\`text
localPosition
\`\`\`

Dengan demikian TrainingArea dapat diduplikasi:

\`\`\`text
TrainingArea_01
TrainingArea_02
TrainingArea_03
TrainingArea_04
...
\`\`\`

tanpa merusak perhitungan posisi agent.

Hal ini sangat berguna untuk **parallel training**.

---

# 23. LANGKAH 7 — Membuat Ground

Klik kanan:

\`\`\`text
TrainingArea
→ 3D Object
→ Cube
\`\`\`

Nama:

\`\`\`text
Ground
\`\`\`

Transform:

\`\`\`text
Position = (0, 0, 0)
Scale    = (10, 0.5, 10)
\`\`\`

Dengan demikian arena kira-kira berukuran:

\`\`\`text
10 × 10
\`\`\`

Ground harus memiliki:

\`\`\`text
Box Collider
\`\`\`

yang otomatis dibuat oleh Unity.

---

# 24. Collider

**Collider** merupakan komponen yang menentukan bentuk fisik sebuah GameObject untuk collision detection.

Contohnya:

\`\`\`text
Box Collider
Sphere Collider
Capsule Collider
Mesh Collider
\`\`\`

Ground menggunakan:

\`\`\`text
Box Collider
\`\`\`

agar agent tidak jatuh menembus lantai.

---

# 25. LANGKAH 8 — Membuat Agent

Klik:

\`\`\`text
TrainingArea
→ 3D Object
→ Sphere
\`\`\`

atau Capsule.

Rename:

\`\`\`text
Agent
\`\`\`

Contoh Transform:

\`\`\`text
Position = (0, 1, 0)
Scale    = (1, 1, 1)
\`\`\`

Tambahkan komponen:

\`\`\`text
Rigidbody
\`\`\`

---

# 26. Rigidbody

\`Rigidbody\` membuat GameObject ikut dalam simulasi physics Unity.

Dengan Rigidbody, object dapat:

- menerima gaya;
- mengalami gravitasi;
- memiliki velocity;
- bertabrakan;
- bergerak melalui physics engine.

Pada agent kita akan menggunakan:

\`\`\`csharp
rb.AddForce(...)
\`\`\`

untuk menggerakkannya.

---

# 27. Konfigurasi Rigidbody Agent

Gunakan kira-kira:

\`\`\`text
Mass             = 1
Linear Damping   = 0.5
Angular Damping  = 0.5
Use Gravity      = ON
\`\`\`

Untuk agent berbentuk bola, rotation tidak menjadi masalah.

Jika menggunakan capsule/cube dan tidak ingin agent terguling, gunakan:

\`\`\`text
Constraints
Freeze Rotation X
Freeze Rotation Z
\`\`\`

---

# 28. LANGKAH 9 — Membuat Target

Klik:

\`\`\`text
TrainingArea
→ 3D Object
→ Cylinder
\`\`\`

Rename:

\`\`\`text
Target
\`\`\`

Transform contoh:

\`\`\`text
Position = (3, 0.75, 3)
Scale    = (0.8, 0.25, 0.8)
\`\`\`

Pastikan target mempunyai Collider.

Centang:

\`\`\`text
Is Trigger = ON
\`\`\`

---

# 29. Trigger

Collider normal menghasilkan collision fisik.

Sedangkan Collider dengan:

\`\`\`text
Is Trigger = true
\`\`\`

tidak digunakan sebagai penghalang fisik.

Collider tersebut menjadi area pendeteksi.

Kita dapat mendeteksinya melalui:

\`\`\`csharp
OnTriggerEnter(Collider other)
\`\`\`

---

# 30. LANGKAH 10 — Membuat Tag Target

Pilih GameObject:

\`\`\`text
Target
\`\`\`

Pada Inspector:

\`\`\`text
Tag
→ Add Tag...
\`\`\`

Buat tag:

\`\`\`text
Target
\`\`\`

Kembali ke GameObject Target.

Set:

\`\`\`text
Tag = Target
\`\`\`

---

# 31. Fungsi Tag

**Tag** adalah label untuk mengidentifikasi jenis GameObject.

Contoh:

\`\`\`text
Player
Enemy
Target
Obstacle
Collectible
\`\`\`

Script dapat mengeceknya:

\`\`\`csharp
other.CompareTag("Target")
\`\`\`

Ini lebih baik daripada membandingkan nama GameObject.

---

# 32. LANGKAH 11 — Membuat Script Agent

Buat:

\`\`\`text
Assets/Scripts/MoveToTargetAgent.cs
\`\`\`

Gunakan script berikut:

\`\`\`csharp
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class MoveToTargetAgent : Agent
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRange = 4f;

    [Header("Reward")]
    [SerializeField] private float targetReward = 1f;
    [SerializeField] private float fallPenalty = -1f;
    [SerializeField] private float stepPenalty = -0.001f;

    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Hentikan gerakan dari episode sebelumnya
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Random posisi Agent
        transform.localPosition = new Vector3(
            Random.Range(-spawnRange, spawnRange),
            1f,
            Random.Range(-spawnRange, spawnRange)
        );

        // Random posisi Target
        target.localPosition = new Vector3(
            Random.Range(-spawnRange, spawnRange),
            0.75f,
            Random.Range(-spawnRange, spawnRange)
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toTarget =
            target.localPosition - transform.localPosition;

        float maxDistance = spawnRange * 2f * 1.414f;

        // Observation 1-2:
        // arah target pada bidang XZ
        Vector3 direction = toTarget.normalized;

        sensor.AddObservation(direction.x);
        sensor.AddObservation(direction.z);

        // Observation 3:
        // jarak target yang dinormalisasi
        sensor.AddObservation(
            Mathf.Clamp01(toTarget.magnitude / maxDistance)
        );

        // Observation 4-5:
        // kecepatan Agent
        sensor.AddObservation(
            Mathf.Clamp(rb.linearVelocity.x / maxSpeed, -1f, 1f)
        );

        sensor.AddObservation(
            Mathf.Clamp(rb.linearVelocity.z / maxSpeed, -1f, 1f)
        );
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX =
            Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);

        float moveZ =
            Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector3 movement =
            new Vector3(moveX, 0f, moveZ);

        rb.AddForce(
            movement * moveForce,
            ForceMode.Force
        );

        // Batasi kecepatan horizontal
        Vector3 horizontalVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity =
                horizontalVelocity.normalized * maxSpeed;

            rb.linearVelocity =
                new Vector3(
                    horizontalVelocity.x,
                    rb.linearVelocity.y,
                    horizontalVelocity.z
                );
        }

        // Penalty kecil agar Agent menyelesaikan tugas
        // secepat mungkin
        AddReward(stepPenalty);

        // Gagal jika jatuh dari platform
        if (transform.localPosition.y < -1f)
        {
            AddReward(fallPenalty);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            AddReward(targetReward);
            EndEpisode();
        }
    }

    public override void Heuristic(
        in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions =
            actionsOut.ContinuousActions;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal = -1f;
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal = 1f;
            }

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
            {
                vertical = -1f;
            }

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
            {
                vertical = 1f;
            }
        }

        actions[0] = horizontal;
        actions[1] = vertical;
    }
}
\`\`\`

---

# 33. Penjelasan Namespace

## UnityEngine

\`\`\`csharp
using UnityEngine;
\`\`\`

Menyediakan class dasar Unity seperti:

\`\`\`text
Vector3
Transform
Rigidbody
Collider
Mathf
Random
\`\`\`

---

## Unity.MLAgents

\`\`\`csharp
using Unity.MLAgents;
\`\`\`

Menyediakan:

\`\`\`text
Agent
AddReward()
EndEpisode()
\`\`\`

dan komponen utama ML-Agents.

---

## Unity.MLAgents.Sensors

\`\`\`csharp
using Unity.MLAgents.Sensors;
\`\`\`

Digunakan untuk:

\`\`\`text
VectorSensor
\`\`\`

yang menerima observation.

---

## Unity.MLAgents.Actuators

\`\`\`csharp
using Unity.MLAgents.Actuators;
\`\`\`

Digunakan untuk:

\`\`\`text
ActionBuffers
ActionSegment
\`\`\`

yang menyimpan action dari policy.

---

## UnityEngine.InputSystem

\`\`\`csharp
using UnityEngine.InputSystem;
\`\`\`

Digunakan hanya pada fungsi:

\`\`\`text
Heuristic()
\`\`\`

agar agent dapat dikendalikan manual dengan keyboard.

Jika package Input System belum tersedia:

\`\`\`text
Window
→ Package Manager
→ Input System
→ Install
\`\`\`

---

# 34. Initialize()

\`\`\`csharp
public override void Initialize()
{
    rb = GetComponent<Rigidbody>();
}
\`\`\`

Dipanggil saat ML-Agent diinisialisasi.

Tujuannya mengambil komponen:

\`\`\`text
Rigidbody
\`\`\`

dan menyimpannya pada:

\`\`\`csharp
rb
\`\`\`

---

# 35. OnEpisodeBegin()

Method:

\`\`\`csharp
public override void OnEpisodeBegin()
\`\`\`

dipanggil ketika episode baru dimulai.

Dokumentasi ML-Agents mendefinisikan \`OnEpisodeBegin()\` sebagai method yang dipanggil pada awal episode, sedangkan \`CollectObservations()\` dan \`OnActionReceived()\` menjalankan observation/action loop agent.

Dalam praktikum fungsi ini melakukan:

\`\`\`text
Reset velocity
↓
Random posisi Agent
↓
Random posisi Target
\`\`\`

---

# 36. Mengapa Velocity Harus Direset?

Jika tidak:

\`\`\`text
Episode 1:
Agent bergerak cepat ke kanan
↓
EndEpisode
↓
Episode 2:
Agent masih membawa momentum
\`\`\`

Hal tersebut membuat initial state setiap episode tidak konsisten.

Karena itu:

\`\`\`csharp
rb.linearVelocity = Vector3.zero;
rb.angularVelocity = Vector3.zero;
\`\`\`

---

# 37. Randomisasi

Kode:

\`\`\`csharp
Random.Range(-spawnRange, spawnRange)
\`\`\`

membuat posisi berubah setiap episode.

Tujuannya mencegah agent menghafal:

\`\`\`text
"target selalu ada di kanan atas"
\`\`\`

dan mendorong policy yang lebih general.

---

# 38. CollectObservations()

Method:

\`\`\`csharp
public override void CollectObservations(
    VectorSensor sensor)
\`\`\`

berfungsi menyediakan state informasi kepada policy.

Dokumentasi ML-Agents merekomendasikan \`CollectObservations()\` untuk data numerik/nonvisual dan mengharuskan jumlah serta struktur observation tetap konsisten.

---

# 39. Observation Praktikum

Observation yang kita kirim:

\`\`\`text
direction.x
direction.z
distance
velocity.x
velocity.z
\`\`\`

Sehingga:

\`\`\`text
Vector Observation Space Size = 5
\`\`\`

Ini penting.

Jika Inspector diset:

\`\`\`text
Space Size = 6
\`\`\`

sedangkan script menghasilkan:

\`\`\`text
5
\`\`\`

konfigurasi agent menjadi tidak sesuai.

---

# 40. Normalisasi Observation

Contoh:

\`\`\`csharp
rb.linearVelocity.x / maxSpeed
\`\`\`

bertujuan mengubah nilai ke rentang yang lebih terkontrol.

Alih-alih memberikan:

\`\`\`text
velocity = 37.54
\`\`\`

kita berusaha memberikan sekitar:

\`\`\`text
-1 sampai +1
\`\`\`

Observation yang memiliki skala nilai konsisten biasanya lebih mudah dipelajari oleh neural network.

---

# 41. OnActionReceived()

Method:

\`\`\`csharp
public override void OnActionReceived(
    ActionBuffers actions)
\`\`\`

dipanggil ketika policy menghasilkan action.

Kita membaca:

\`\`\`csharp
actions.ContinuousActions[0]
actions.ContinuousActions[1]
\`\`\`

sebagai:

\`\`\`text
moveX
moveZ
\`\`\`

---

# 42. Rigidbody.AddForce()

Kode:

\`\`\`csharp
rb.AddForce(
    movement * moveForce,
    ForceMode.Force
);
\`\`\`

memberikan gaya pada Rigidbody.

Ini berbeda dengan:

\`\`\`csharp
transform.position += ...
\`\`\`

karena \`AddForce()\` bekerja melalui physics engine.

Akibatnya agent memiliki:

- acceleration;
- momentum;
- velocity;
- collision response.

Hal ini membuat problem RL sedikit lebih menarik karena agent harus belajar mengendalikan momentum.

---

# 43. AddReward()

\`\`\`csharp
AddReward(stepPenalty);
\`\`\`

menambahkan reward pada agent.

Jika:

\`\`\`text
stepPenalty = -0.001
\`\`\`

maka agent mendapat sedikit penalty selama tugas belum selesai.

Jika target tercapai:

\`\`\`csharp
AddReward(1f);
\`\`\`

---

# 44. EndEpisode()

\`\`\`csharp
EndEpisode();
\`\`\`

mengakhiri episode sekarang.

Setelah episode selesai, ML-Agents akan memulai episode baru dan memanggil kembali:

\`\`\`csharp
OnEpisodeBegin()
\`\`\`

---

# 45. Heuristic()

\`Heuristic()\` memungkinkan manusia menggantikan policy ML sementara.

Gunanya adalah untuk memastikan:

\`\`\`text
Apakah action mapping benar?
Apakah Agent bisa bergerak?
Apakah collision bekerja?
Apakah Target terdeteksi?
Apakah environment memang dapat diselesaikan?
\`\`\`

Dokumentasi resmi menyatakan \`Heuristic Only\` membuat Agent menggunakan method \`Heuristic()\` sebagai sumber keputusannya.

---

# 46. LANGKAH 12 — Memasang Script Agent

Pilih:

\`\`\`text
Agent
\`\`\`

Drag:

\`\`\`text
MoveToTargetAgent.cs
\`\`\`

ke Inspector.

Pada field:

\`\`\`text
Target
\`\`\`

drag GameObject:

\`\`\`text
Target
\`\`\`

ke field tersebut.

Set:

\`\`\`text
Move Force     = 10
Max Speed      = 5
Spawn Range    = 4
Target Reward  = 1
Fall Penalty   = -1
Step Penalty   = -0.001
\`\`\`

---

# 47. LANGKAH 13 — Tambahkan Behavior Parameters

Pilih:

\`\`\`text
Agent
\`\`\`

Klik:

\`\`\`text
Add Component
\`\`\`

Cari:

\`\`\`text
Behavior Parameters
\`\`\`

Tambahkan.

Behavior Parameters menentukan policy, observation space, action space, model inference, dan Behavior Type agent.

---

# 48. Konfigurasi Behavior Parameters

Gunakan:

\`\`\`text
Behavior Name:
MoveToTarget
\`\`\`

### Vector Observation

\`\`\`text
Space Size      = 5
Stacked Vectors = 1
\`\`\`

### Actions

\`\`\`text
Continuous Actions = 2
Discrete Branches  = 0
\`\`\`

### Model

\`\`\`text
None
\`\`\`

selama training.

### Behavior Type

\`\`\`text
Default
\`\`\`

---

# 49. Behavior Name

Gunakan tepat:

\`\`\`text
MoveToTarget
\`\`\`

Nama ini nanti juga digunakan pada file:

\`\`\`text
MoveToTarget.yaml
\`\`\`

Jika Behavior Name pada Unity berbeda dengan konfigurasi trainer, trainer tidak akan menggunakan konfigurasi yang kita harapkan.

---

# 50. Behavior Type

Terdapat tiga mode penting.

## Default

\`\`\`text
Training process tersedia
→ gunakan trainer

Training tidak tersedia + model tersedia
→ inference
\`\`\`

Digunakan untuk training.

## Heuristic Only

\`\`\`text
Gunakan Heuristic()
\`\`\`

Digunakan untuk testing manual.

## Inference Only

\`\`\`text
Gunakan trained model
\`\`\`

Digunakan setelah training.

Behavior tersebut sesuai definisi resmi \`Behavior Parameters\`.

---

# 51. LANGKAH 14 — Tambahkan Decision Requester

Pada Agent:

\`\`\`text
Add Component
→ Decision Requester
\`\`\`

Set awal:

\`\`\`text
Decision Period = 5
Take Actions Between Decisions = ON
\`\`\`

---

# 52. Decision Requester

Agent tidak harus membuat keputusan pada setiap frame rendering.

\`Decision Requester\` menentukan kapan agent meminta keputusan dari policy. ML-Agents merekomendasikan mekanisme keputusan periodik terutama untuk simulasi berbasis physics.

Contoh:

\`\`\`text
Decision Period = 5
\`\`\`

berarti policy baru diminta secara periodik, sedangkan action dapat terus diterapkan di antara dua decision.

---

# 53. LANGKAH 15 — Mengatur Max Step

Pada komponen Agent/script ML-Agents biasanya terdapat parameter:

\`\`\`text
Max Step
\`\`\`

Set:

\`\`\`text
Max Step = 500
\`\`\`

Artinya episode memiliki batas maksimum.

Jika agent tidak berhasil dalam periode tersebut:

\`\`\`text
episode selesai
↓
environment reset
\`\`\`

Max Step mencegah episode berjalan tanpa batas.

---

# 54. LANGKAH 16 — Testing Heuristic

Sebelum melakukan machine learning, **wajib uji environment secara manual**.

Set:

\`\`\`text
Behavior Type = Heuristic Only
\`\`\`

Klik:

\`\`\`text
Play
\`\`\`

Kontrol Agent:

\`\`\`text
W / ↑ = maju
S / ↓ = mundur
A / ← = kiri
D / → = kanan
\`\`\`

Pastikan:

- agent dapat bergerak;
- agent tidak menembus ground;
- agent bisa jatuh;
- agent di-reset ketika jatuh;
- agent di-reset ketika mencapai target;
- posisi target berubah;
- posisi agent berubah.

---

# 55. Mengapa Heuristic Testing Penting?

Jangan langsung menyalahkan PPO jika agent tidak belajar.

Kesalahan mungkin berasal dari:

\`\`\`text
Action salah
Collision salah
Target tidak memiliki Tag
Target bukan Trigger
Rigidbody salah
Observation salah
Episode tidak selesai
\`\`\`

Jika manusia menggunakan action yang sama saja tidak dapat menyelesaikan environment, model RL juga akan kesulitan.

---

# 56. LANGKAH 17 — Kembalikan Behavior Type

Setelah testing berhasil:

\`\`\`text
Behavior Type = Default
\`\`\`

Jangan biarkan:

\`\`\`text
Heuristic Only
\`\`\`

ketika training.

---

# 57. LANGKAH 18 — Membuat Konfigurasi PPO

Buat file:

\`\`\`text
MoveToTarget.yaml
\`\`\`

Direkomendasikan ditempatkan pada folder yang mudah diakses trainer, misalnya:

\`\`\`text
config/MoveToTarget.yaml
\`\`\`

Isi awal:

\`\`\`yaml
behaviors:
  MoveToTarget:
    trainer_type: ppo

    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
      learning_rate_schedule: linear

    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2

    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0

    max_steps: 500000
    time_horizon: 64
    summary_freq: 10000
\`\`\`

Ini merupakan konfigurasi awal untuk praktikum, bukan parameter universal terbaik untuk semua environment.

ML-Agents menggunakan file YAML untuk menentukan trainer dan hyperparameter; \`mlagents-learn\` adalah entry point resmi untuk training.

---

# 58. Penjelasan Trainer Type

\`\`\`yaml
trainer_type: ppo
\`\`\`

menentukan algoritma yang digunakan:

\`\`\`text
Proximal Policy Optimization
\`\`\`

---

# 59. Batch Size

\`\`\`yaml
batch_size: 1024
\`\`\`

Batch size adalah jumlah pengalaman yang diproses bersama dalam satu proses update.

Secara konseptual:

\`\`\`text
pengalaman Agent
↓
dikumpulkan
↓
dibagi ke batch
↓
digunakan memperbarui network
\`\`\`

---

# 60. Buffer Size

\`\`\`yaml
buffer_size: 10240
\`\`\`

Buffer adalah kumpulan pengalaman sebelum proses learning update.

Experience secara sederhana mengandung:

\`\`\`text
Observation
Action
Reward
Next state / progression
\`\`\`

---

# 61. Learning Rate

\`\`\`yaml
learning_rate: 3.0e-4
\`\`\`

Learning rate menentukan seberapa besar perubahan bobot neural network setiap proses pembelajaran.

Terlalu tinggi:

\`\`\`text
Training dapat tidak stabil
\`\`\`

Terlalu rendah:

\`\`\`text
Learning sangat lambat
\`\`\`

---

# 62. Hidden Units

\`\`\`yaml
hidden_units: 128
\`\`\`

Menentukan jumlah neuron pada hidden layer neural network.

---

# 63. Num Layers

\`\`\`yaml
num_layers: 2
\`\`\`

Menentukan jumlah hidden layer.

Untuk problem MoveToTarget sederhana:

\`\`\`text
2 layer × 128 neuron
\`\`\`

sudah menjadi starting point yang masuk akal.

---

# 64. Gamma

\`\`\`yaml
gamma: 0.99
\`\`\`

Gamma berkaitan dengan seberapa penting future reward.

Secara konsep:

\`\`\`text
Gamma tinggi
→ Agent lebih mempertimbangkan reward masa depan
\`\`\`

---

# 65. Max Steps Training

\`\`\`yaml
max_steps: 500000
\`\`\`

Jangan tertukar dengan:

\`\`\`text
Agent Max Step = 500
\`\`\`

Keduanya berbeda.

### Agent Max Step

Membatasi:

\`\`\`text
panjang satu episode
\`\`\`

### Trainer max_steps

Membatasi:

\`\`\`text
keseluruhan proses training behavior
\`\`\`

---

# 66. LANGKAH 19 — Memulai Training

Aktifkan environment Python:

\`\`\`bash
conda activate mlagents
\`\`\`

Kemudian:

\`\`\`bash
mlagents-learn config/MoveToTarget.yaml --run-id=MoveToTarget_Run01
\`\`\`

Jika training dilakukan langsung melalui Unity Editor, parameter \`--env\` tidak perlu diberikan. Dokumentasi resmi menyebutkan bahwa trainer akan meminta pengguna menekan Play di Unity ketika training melalui editor.

Terminal akan menampilkan pesan yang meminta Unity dijalankan.

---

# 67. LANGKAH 20 — Menjalankan Unity

Setelah trainer siap:

1. kembali ke Unity;
2. buka scene \`MLAgents_MoveToTarget\`;
3. pastikan \`Behavior Type = Default\`;
4. tekan:

\`\`\`text
Play
\`\`\`

Unity akan terhubung ke Python trainer.

---

# 68. Apa yang Terjadi Selama Training?

Secara berulang:

\`\`\`text
CollectObservations()
        ↓
PPO policy
        ↓
OnActionReceived()
        ↓
Agent bergerak
        ↓
Reward
        ↓
Observation berikutnya
        ↓
...
\`\`\`

Python mengumpulkan pengalaman dari agent.

Kemudian PPO memperbarui neural network.

---

# 69. Perilaku Agent pada Awal Training

Pada awal training biasanya agent akan:

- bergerak tidak terarah;
- jatuh;
- berputar;
- menjauh dari target;
- gagal berkali-kali.

Itu **normal**.

Agent belum mengetahui hubungan:

\`\`\`text
Observation → Action → Reward
\`\`\`

---

# 70. Perilaku Setelah Belajar

Jika training berjalan baik, perlahan agent mulai:

\`\`\`text
bergerak ke arah target
↓
mengurangi gerakan acak
↓
mencapai target lebih sering
↓
menyelesaikan episode lebih cepat
\`\`\`

---

# 71. Cumulative Reward

Salah satu indikator utama training:

\`\`\`text
Cumulative Reward
\`\`\`

Cumulative reward adalah total reward yang diperoleh agent dalam suatu episode/periode evaluasi.

Harapan umum:

\`\`\`text
Awal:
reward rendah

↓ training

Tengah:
reward mulai meningkat

↓ training

Akhir:
reward relatif tinggi dan stabil
\`\`\`

Namun reward tinggi **harus tetap diperiksa secara visual**.

Jangan hanya melihat angka.

---

# 72. Reward Hacking

Contoh:

Kita memberi reward:

\`\`\`text
+0.01 setiap kali bergerak mendekati target
\`\`\`

Tetapi tidak mendesainnya dengan baik.

Agent mungkin belajar:

\`\`\`text
mendekat
↓
menjauh
↓
mendekat lagi
↓
mendapat reward berulang
\`\`\`

tanpa menyelesaikan goal.

Itulah salah satu contoh:

## Reward Hacking

Karena itu modul dasar ini menggunakan reward sederhana:

\`\`\`text
+1 goal
-1 jatuh
-0.001 waktu
\`\`\`

---

# 73. LANGKAH 21 — Menghentikan Training

Di terminal gunakan:

\`\`\`text
Ctrl + C
\`\`\`

satu kali.

ML-Agents akan menyimpan hasil training.

Dokumentasi resmi menyatakan model, summary training, dan timer disimpan dalam:

\`\`\`text
results/<run-id>
\`\`\`

dan model final dihasilkan dalam format \`.onnx\`.

---

# 74. Hasil Training

Contoh:

\`\`\`text
results/
└── MoveToTarget_Run01/
    ├── MoveToTarget.onnx
    ├── checkpoint...
    └── run_logs/
\`\`\`

File paling penting untuk Unity:

\`\`\`text
MoveToTarget.onnx
\`\`\`

---

# 75. Apa Itu ONNX?

ONNX adalah format model neural network yang memungkinkan model hasil training digunakan kembali untuk inference.

Dalam praktikum:

\`\`\`text
Training Python
        ↓
MoveToTarget.onnx
        ↓
Unity
        ↓
Behavior Parameters
        ↓
Inference
\`\`\`

---

# 76. LANGKAH 22 — Import Model ke Unity

Copy:

\`\`\`text
MoveToTarget.onnx
\`\`\`

ke:

\`\`\`text
Assets/Models/
\`\`\`

Unity akan meng-import model tersebut.

---

# 77. LANGKAH 23 — Memasang Model

Pilih:

\`\`\`text
Agent
\`\`\`

Pada:

\`\`\`text
Behavior Parameters
\`\`\`

drag model:

\`\`\`text
MoveToTarget.onnx
\`\`\`

ke field:

\`\`\`text
Model
\`\`\`

Kemudian ubah:

\`\`\`text
Behavior Type
\`\`\`

menjadi:

\`\`\`text
Inference Only
\`\`\`

---

# 78. LANGKAH 24 — Testing Inference

Tekan:

\`\`\`text
Play
\`\`\`

Sekarang:

\`\`\`text
Python trainer tidak diperlukan.
\`\`\`

Flow berubah menjadi:

\`\`\`text
Observation
      ↓
ONNX Policy Model
      ↓
Action
      ↓
Agent bergerak
\`\`\`

Reward tidak diperlukan untuk memperbarui model pada inference. Dokumentasi ML-Agents menyatakan reward digunakan untuk RL training dan tidak digunakan oleh trained model untuk belajar ketika inference.

---

# 79. Training vs Inference

| Aspek | Training | Inference |
|---|---|---|
| Tujuan | belajar | menggunakan hasil belajar |
| Python trainer | diperlukan | tidak |
| Model berubah | ya | tidak |
| Reward digunakan belajar | ya | tidak |
| PPO | aktif training | tidak melakukan training |
| ONNX | dihasilkan | digunakan |
| Behavior Type | Default | Inference Only |

---

# 80. LANGKAH 25 — Membuat Parallel Training

Setelah satu arena bekerja sempurna, \`TrainingArea\` dapat dibuat sebagai Prefab.

Drag:

\`\`\`text
TrainingArea
\`\`\`

ke:

\`\`\`text
Assets/Prefabs
\`\`\`

Kemudian buat beberapa instance.

Contoh:

\`\`\`text
TrainingArea_01
TrainingArea_02
TrainingArea_03
TrainingArea_04
TrainingArea_05
TrainingArea_06
TrainingArea_07
TrainingArea_08
\`\`\`

Pisahkan secara fisik:

\`\`\`text
Area 1 → (0,0,0)
Area 2 → (15,0,0)
Area 3 → (30,0,0)
Area 4 → (45,0,0)
...
\`\`\`

---

# 81. Mengapa Parallel Training?

Semua agent dapat menggunakan:

\`\`\`text
Behavior Name = MoveToTarget
\`\`\`

Artinya pengalaman dari banyak agent digunakan untuk mempelajari policy yang sama.

Contoh:

\`\`\`text
Agent 1 ─┐
Agent 2 ─┤
Agent 3 ─┼→ PPO → MoveToTarget Policy
Agent 4 ─┤
Agent 5 ─┘
\`\`\`

Manfaatnya:

- pengalaman terkumpul lebih cepat;
- variasi episode lebih banyak;
- training lebih efisien.

---

# 82. Mengapa localPosition Digunakan?

Bayangkan:

\`\`\`text
TrainingArea_01 position = (0,0,0)

TrainingArea_02 position = (20,0,0)
\`\`\`

Jika script menggunakan:

\`\`\`text
transform.position
\`\`\`

agent pada area kedua memiliki world coordinate sekitar:

\`\`\`text
X = 20
\`\`\`

padahal secara lokal posisinya mungkin:

\`\`\`text
X = 0
\`\`\`

Dengan:

\`\`\`text
localPosition
\`\`\`

semua environment mempunyai sistem koordinat lokal yang seragam.

Ini penting untuk parallel environment.

---

# 83. Eksperimen 1 — Menghilangkan Step Penalty

Ubah:

\`\`\`text
Step Penalty = 0
\`\`\`

Train ulang:

\`\`\`bash
mlagents-learn config/MoveToTarget.yaml --run-id=NoStepPenalty
\`\`\`

Amati:

- apakah agent tetap mencapai target?
- apakah lebih lambat?
- apakah episode lebih panjang?

---

# 84. Eksperimen 2 — Penalty Terlalu Besar

Ubah:

\`\`\`text
Step Penalty = -0.05
\`\`\`

Pertanyaan:

> Apakah agent masih dapat belajar?

Diskusikan mengapa excessive negative reward dapat menghambat proses learning.

---

# 85. Eksperimen 3 — Observation Dikurangi

Hilangkan velocity observation.

Observation menjadi:

\`\`\`text
direction.x
direction.z
distance
\`\`\`

Maka:

\`\`\`text
Space Size = 3
\`\`\`

Train ulang.

Bandingkan dengan:

\`\`\`text
Space Size = 5
\`\`\`

Pertanyaan:

> Apakah velocity membantu agent mengendalikan momentum?

---

# 86. Eksperimen 4 — Decision Period

Bandingkan:

\`\`\`text
Decision Period = 1
\`\`\`

dengan:

\`\`\`text
Decision Period = 5
\`\`\`

dan:

\`\`\`text
Decision Period = 10
\`\`\`

Analisis:

- responsivitas agent;
- kestabilan movement;
- kecepatan training;
- kemampuan mencapai target.

---

# 87. Eksperimen 5 — Fixed Target

Nonaktifkan randomisasi Target.

Gunakan posisi:

\`\`\`text
Target = (3, 0.75, 3)
\`\`\`

Training.

Kemudian pindahkan target setelah model selesai.

Pertanyaan:

> Apakah agent mampu mencapai posisi target baru?

Eksperimen ini menunjukkan pentingnya:

\`\`\`text
generalization
\`\`\`

dan:

\`\`\`text
environment randomization
\`\`\`

---

# 88. Pengembangan Lanjutan — Obstacle

Setelah praktikum dasar berhasil, tambahkan:

\`\`\`text
Cube
\`\`\`

sebagai:

\`\`\`text
Obstacle
\`\`\`

Agent sekarang harus:

\`\`\`text
mencapai target
+
menghindari obstacle
\`\`\`

Namun observation sekarang mungkin belum cukup.

Agent mengetahui target tetapi:

\`\`\`text
tidak mengetahui obstacle
\`\`\`

Solusinya adalah menambahkan sensor.

---

# 89. Ray Perception Sensor

Salah satu pengembangan yang sesuai materi adalah:

\`\`\`text
Ray Perception Sensor
\`\`\`

Sensor tersebut memungkinkan agent mendeteksi:

- wall;
- obstacle;
- target;
- jarak objek.

Struktur:

\`\`\`text
Agent
├── MoveToTargetAgent
├── Behavior Parameters
├── Decision Requester
└── Ray Perception Sensor
\`\`\`

Untuk praktikum utama saya **tidak merekomendasikan langsung memakai Ray Sensor**, karena mahasiswa sebaiknya terlebih dahulu memahami vector observation.

---

# 90. Vector Observation vs Ray Observation

| Vector | Ray |
|---|---|
| angka eksplisit | perception berbasis ray |
| sederhana | lebih kompleks |
| training relatif mudah | observation lebih banyak |
| ideal untuk praktikum awal | ideal pengembangan |
| posisi/jarak/velocity | obstacle/wall/target |

---

# 91. Troubleshooting — Agent Tidak Bergerak

Periksa:

\`\`\`text
Rigidbody terpasang?
\`\`\`

Kemudian:

\`\`\`text
Move Force > 0?
\`\`\`

Periksa:

\`\`\`text
Continuous Actions = 2?
\`\`\`

Periksa:

\`\`\`text
Decision Requester ada?
\`\`\`

Periksa juga:

\`\`\`text
Behavior Type
\`\`\`

Jika inference:

\`\`\`text
Model sudah dipasang?
\`\`\`

---

# 92. Troubleshooting — IndexOutOfRangeException

Jika terjadi pada:

\`\`\`csharp
actions.ContinuousActions[1]
\`\`\`

kemungkinan:

\`\`\`text
Continuous Actions bukan 2.
\`\`\`

Set:

\`\`\`text
Behavior Parameters
→ Actions
→ Continuous Actions = 2
\`\`\`

---

# 93. Troubleshooting — Observation Tidak Cocok

Script menghasilkan:

\`\`\`text
5 observation
\`\`\`

Maka Inspector harus:

\`\`\`text
Space Size = 5
\`\`\`

Hitung:

\`\`\`text
direction.x = 1
direction.z = 1
distance    = 1
velocity.x  = 1
velocity.z  = 1
----------------
TOTAL       = 5
\`\`\`

---

# 94. Troubleshooting — Target Tidak Terdeteksi

Periksa target:

\`\`\`text
Collider ada?
Is Trigger ON?
Tag = Target?
\`\`\`

Periksa Agent:

\`\`\`text
Collider ada?
Rigidbody ada?
\`\`\`

Pastikan penulisan:

\`\`\`csharp
CompareTag("Target")
\`\`\`

sama persis.

---

# 95. Troubleshooting — Agent Tidak Reset Ketika Jatuh

Periksa:

\`\`\`csharp
if (transform.localPosition.y < -1f)
\`\`\`

Jika platform terlalu rendah, sesuaikan threshold.

Contoh:

\`\`\`csharp
if (transform.localPosition.y < -2f)
\`\`\`

---

# 96. Troubleshooting — mlagents-learn Tidak Dikenali

Pastikan virtual environment aktif:

\`\`\`bash
conda activate mlagents
\`\`\`

Lalu:

\`\`\`bash
mlagents-learn --help
\`\`\`

Jika tidak ditemukan, periksa kembali instalasi package Python ML-Agents.

---

# 97. Troubleshooting — Unity Tidak Terhubung ke Trainer

Urutan harus:

\`\`\`text
1. Jalankan mlagents-learn
2. Tunggu pesan trainer
3. Buka Unity
4. Tekan Play
\`\`\`

Pastikan:

\`\`\`text
Behavior Type = Default
\`\`\`

bukan:

\`\`\`text
Heuristic Only
\`\`\`

atau:

\`\`\`text
Inference Only
\`\`\`

---

# 98. Troubleshooting — Agent Bergerak Acak Terus

Kemungkinan:

1. Training masih terlalu awal.
2. Reward terlalu lemah.
3. Observation salah.
4. Action mapping salah.
5. Target sulit dicapai.
6. Agent terlalu cepat.
7. Episode terlalu pendek.
8. PPO belum memiliki cukup pengalaman.

Pertama-tama uji:

\`\`\`text
Heuristic Only
\`\`\`

Jika manual control berhasil, environment kemungkinan sudah benar.

---

# 99. Troubleshooting — Reward Naik tetapi Agent Aneh

Jangan langsung menyimpulkan model bagus hanya karena:

\`\`\`text
Cumulative Reward ↑
\`\`\`

Lihat agent secara visual.

Mungkin terjadi:

\`\`\`text
Reward Hacking
\`\`\`

Pertanyaan utama:

> Apakah agent benar-benar menyelesaikan tujuan yang kita inginkan?

---

# 100. Rekomendasi Praktikum Terbaik

Untuk Praktikum 14, saya merekomendasikan konfigurasi utama berikut:

\`\`\`text
Environment:
Arena 3D sederhana

Agent:
Sphere dengan Rigidbody

Observation:
5 vector observations

Action:
2 continuous actions

Reward:
+1 target
-1 jatuh
-0.001 per decision

Episode:
Random Agent
Random Target
Max Step 500

Algorithm:
PPO

Decision Period:
5

Training:
500.000 max trainer steps
\`\`\`

---

# 101. Mengapa Pilihan Ini Direkomendasikan?

## Pertama — Konsepnya lengkap

Dalam satu project mahasiswa mempraktikkan:

\`\`\`text
Agent
Environment
Observation
Action
Reward
Episode
Policy
PPO
Training
Inference
\`\`\`

## Kedua — Visual dan mudah diamati

Keberhasilan model dapat terlihat langsung:

\`\`\`text
Agent bergerak menuju Target.
\`\`\`

## Ketiga — Tidak terlalu kompleks

Kita belum membutuhkan:

- visual observation;
- multi-agent competition;
- self-play;
- curriculum;
- imitation learning;
- complex ray sensors.

## Keempat — Mudah dikembangkan

Setelah dasar berhasil dapat ditambah:

\`\`\`text
Obstacle
Ray Sensor
Moving Target
Multiple Target
Parallel Arena
Curriculum
\`\`\`

## Kelima — Cocok untuk membandingkan AI klasik dan learning AI

Mahasiswa dapat membandingkan:

\`\`\`text
NavMeshAgent
vs
ML-Agent
\`\`\`

Pada NavMesh:

\`\`\`text
developer menentukan mekanisme navigasi.
\`\`\`

Pada ML-Agent:

\`\`\`text
developer mendesain observation,
action dan reward,
kemudian agent belajar policy.
\`\`\`

---

# 102. Hal yang Tidak Direkomendasikan untuk Praktikum Pertama

Jangan langsung memulai dengan:

\`\`\`text
Visual Observation
+
Camera Sensor
+
10 obstacle
+
moving target
+
multiple agents
+
self-play
\`\`\`

Problem yang terlalu kompleks membuat mahasiswa sulit menentukan apakah kegagalan berasal dari:

\`\`\`text
Observation?
Reward?
Action?
Environment?
Hyperparameter?
Sensor?
Physics?
\`\`\`

Gunakan prinsip:

\`\`\`text
Simple Environment
↓
Training berhasil
↓
Tambah kompleksitas bertahap
\`\`\`

---

# 103. Tahapan Pengembangan yang Direkomendasikan

## Level 1 — Basic

\`\`\`text
MoveToTarget
tanpa obstacle
\`\`\`

## Level 2 — Randomization

\`\`\`text
Random Agent
Random Target
\`\`\`

## Level 3 — Obstacle

\`\`\`text
Tambahkan obstacle
\`\`\`

## Level 4 — Perception

\`\`\`text
Ray Perception Sensor
\`\`\`

## Level 5 — Parallel Training

\`\`\`text
8–16 TrainingArea
\`\`\`

## Level 6 — Advanced Experiment

\`\`\`text
Moving Target
Curriculum
Reward Shaping
\`\`\`

Dengan tahapan tersebut mahasiswa dapat memahami efek setiap perubahan.

---

# 104. Tugas Praktikum Mahasiswa

Mahasiswa wajib menghasilkan:

1. Project Unity yang dapat dijalankan.
2. TrainingArea.
3. Agent berbasis ML-Agents.
4. Random target.
5. Continuous action.
6. Vector observation.
7. Reward dan penalty.
8. Episode reset.
9. PPO configuration.
10. Minimal satu hasil training.
11. File \`.onnx\`.
12. Agent inference yang berhasil menuju target.

---

# 105. Eksperimen Wajib

Lakukan minimal dua training:

## Training A

\`\`\`text
stepPenalty = -0.001
Decision Period = 5
\`\`\`

## Training B

Ubah salah satu:

\`\`\`text
stepPenalty
Decision Period
Observation
Max Step
\`\`\`

Bandingkan hasilnya.

---

# 106. Data yang Dicatat

Buat tabel:

| Run | Observation | Reward | Decision Period | Hasil |
|---|---|---|---:|---|
| Run01 | Direction + Distance + Velocity | +1/-1/-0.001 | 5 | ... |
| Run02 | ... | ... | ... | ... |

Tambahkan:

\`\`\`text
Waktu training:
Cumulative reward:
Perilaku:
Success rate:
Catatan:
\`\`\`

---

# 107. Pertanyaan Analisis

Jawab pertanyaan berikut.

### 1.
Apa fungsi \`Agent\` dalam Unity ML-Agents?

### 2.
Apa perbedaan Agent dan Environment?

### 3.
Mengapa target diberikan sebagai posisi relatif?

### 4.
Mengapa velocity diberikan sebagai observation?

### 5.
Mengapa jumlah observation harus konsisten?

### 6.
Mengapa praktikum menggunakan Continuous Action?

### 7.
Apa fungsi \`AddReward()\`?

### 8.
Apa fungsi \`EndEpisode()\`?

### 9.
Apa akibatnya jika reward target terlalu kecil?

### 10.
Apa akibatnya jika step penalty terlalu besar?

### 11.
Mengapa posisi target di-random?

### 12.
Apa fungsi \`Behavior Parameters\`?

### 13.
Apa fungsi \`Decision Requester\`?

### 14.
Apa perbedaan \`Default\`, \`Heuristic Only\`, dan \`Inference Only\`?

### 15.
Apa fungsi \`Heuristic()\`?

### 16.
Apa yang dimaksud Policy?

### 17.
Apa fungsi PPO?

### 18.
Apa perbedaan training dan inference?

### 19.
Mengapa reward tinggi belum tentu berarti behavior agent benar?

### 20.
Apa yang dimaksud reward hacking?

---

# 108. Tantangan Tambahan

Bagi mahasiswa yang telah menyelesaikan praktikum dasar:

## Challenge 1

Tambahkan obstacle.

## Challenge 2

Tambahkan Ray Perception Sensor.

## Challenge 3

Berikan penalty ketika menabrak obstacle.

## Challenge 4

Buat moving target.

## Challenge 5

Buat 8 parallel training areas.

## Challenge 6

Bandingkan:

\`\`\`text
Sparse Reward
vs
Reward Shaping
\`\`\`

## Challenge 7

Uji model pada arena yang berbeda dari arena training.

---

# 109. Checklist Keberhasilan

- [ ] Project Unity dapat dibuka tanpa error.
- [ ] Package ML-Agents terpasang.
- [ ] Python ML-Agents trainer dapat dijalankan.
- [ ] TrainingArea dibuat.
- [ ] Ground mempunyai collider.
- [ ] Agent mempunyai Rigidbody dan Collider.
- [ ] Target mempunyai Collider \`Is Trigger\`.
- [ ] Tag \`Target\` sudah benar.
- [ ] \`MoveToTargetAgent.cs\` terpasang.
- [ ] Target reference sudah diisi.
- [ ] \`Behavior Parameters\` terpasang.
- [ ] Behavior Name adalah \`MoveToTarget\`.
- [ ] Vector Observation Space Size adalah \`5\`.
- [ ] Continuous Actions adalah \`2\`.
- [ ] \`Decision Requester\` terpasang.
- [ ] Max Step diatur.
- [ ] Heuristic dapat dikendalikan WASD.
- [ ] Agent reset ketika jatuh.
- [ ] Agent reset ketika mencapai target.
- [ ] Target di-random setiap episode.
- [ ] File PPO YAML tersedia.
- [ ] \`mlagents-learn\` berhasil tersambung ke Unity.
- [ ] Training berhasil berjalan.
- [ ] Model \`.onnx\` berhasil dihasilkan.
- [ ] Model dapat dimasukkan ke Unity.
- [ ] Behavior Type dapat dijalankan sebagai \`Inference Only\`.
- [ ] Trained agent mampu mencapai target.

---

# 110. Kesimpulan Praktikum

Praktikum ini memperlihatkan perbedaan mendasar antara AI berbasis aturan dengan learning-based AI.

Pada AI klasik developer dapat menulis:

\`\`\`text
IF target berada di kanan
THEN bergerak ke kanan
\`\`\`

Pada Unity ML-Agents developer tidak menentukan rule tersebut secara langsung.

Developer menentukan:

\`\`\`text
Environment
+
Observation
+
Action
+
Reward
+
Episode
\`\`\`

Kemudian:

\`\`\`text
Agent
   ↓
mengalami banyak episode
   ↓
mengumpulkan reward
   ↓
PPO memperbaiki policy
   ↓
Agent belajar behavior
\`\`\`

Konsep terpenting dari praktikum ini bukan sekadar:

\`\`\`text
menjalankan mlagents-learn
\`\`\`

melainkan memahami bahwa:

> **Kualitas perilaku agent sangat ditentukan oleh bagaimana developer mendesain observation, action, reward, dan episode.**

Jika observation tidak cukup:

\`\`\`text
Agent tidak mengetahui informasi
yang diperlukan untuk memecahkan masalah.
\`\`\`

Jika action salah:

\`\`\`text
Agent tidak memiliki kontrol yang tepat.
\`\`\`

Jika reward salah:

\`\`\`text
Agent dapat mempelajari perilaku
yang berbeda dari tujuan developer.
\`\`\`

Jika episode salah:

\`\`\`text
Training menjadi tidak konsisten.
\`\`\`

Karena itu workflow yang direkomendasikan adalah:

\`\`\`text
Buat environment sederhana
        ↓
Validasi physics
        ↓
Validasi dengan Heuristic
        ↓
Validasi Observation
        ↓
Validasi Action
        ↓
Validasi Reward
        ↓
Training PPO
        ↓
Evaluasi behavior
        ↓
Export model
        ↓
Inference
        ↓
Tambah kompleksitas
\`\`\`

---

# 111. Ringkasan Teknis

\`\`\`text
PROJECT
GC14_MLAgents_MoveToTarget_PPO

SCENE
MLAgents_MoveToTarget

BEHAVIOR
MoveToTarget

AGENT
MoveToTargetAgent.cs

OBSERVATION
5 vector values

ACTION
2 continuous actions

REWARD
+1.0 target
-1.0 fall
-0.001 step

EPISODE
Goal / Fall / Max Step

MAX STEP
500

DECISION PERIOD
5

TRAINER
PPO

TRAINER MAX STEPS
500000

MODEL OUTPUT
MoveToTarget.onnx

TRAINING MODE
Default

DEBUG MODE
Heuristic Only

FINAL MODE
Inference Only
\`\`\`

---

# 112. Alur Akhir Praktikum

\`\`\`text
UNITY ENVIRONMENT
       │
       ├── Agent
       ├── Target
       └── Ground
             │
             ▼
     CollectObservations()
             │
             ▼
       PPO POLICY
             │
             ▼
      Continuous Action
        moveX / moveZ
             │
             ▼
      Rigidbody Movement
             │
             ▼
          REWARD
       ┌─────┴─────┐
       │           │
    Goal +1     Fall -1
       │           │
       └─────┬─────┘
             ▼
        EndEpisode()
             │
             ▼
        Random Reset
             │
             ▼
          TRAINING
             │
             ▼
       POLICY MODEL
             │
             ▼
    MoveToTarget.onnx
             │
             ▼
        INFERENCE
\`\`\`

---

# Catatan Pengajar

Untuk pelaksanaan kuliah, urutan yang paling efektif adalah:

\`\`\`text
1. Demonstrasikan agent dengan Heuristic.
2. Tunjukkan 5 observation yang digunakan.
3. Tunjukkan 2 continuous action.
4. Jelaskan reward +1, -1 dan -0.001.
5. Jalankan training.
6. Biarkan mahasiswa melihat agent bergerak acak.
7. Tunjukkan perkembangan behavior.
8. Pasang model ONNX.
9. Jalankan inference tanpa Python.
10. Baru lakukan eksperimen reward/observation.
\`\`\`

Jangan memulai praktikum dengan obstacle atau Ray Sensor.

Target utama Praktikum 14 adalah memastikan mahasiswa benar-benar memahami hubungan:

\`\`\`text
Observation
    ↓
Policy
    ↓
Action
    ↓
Reward
    ↓
Learning
\`\`\`

Setelah hubungan tersebut dipahami, obstacle, Ray Perception Sensor, curriculum learning, dan parallel training dapat diberikan sebagai pengembangan.`,Ic=`# MODUL PRAKTIKUM 15
# Advanced Game AI & Final Project Integration

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Tools:** Unity 6 + C#  
**Topik:** Advanced Game AI & Final Project Development  
**Praktikum:** Integrasi AI dalam Proyek Akhir

---

# 1. Judul Praktikum

## Adaptive Arena Director — Integrasi Advanced Game AI

Pada praktikum ini mahasiswa membangun sebuah **mini game arena survival** yang mengintegrasikan beberapa sistem Game AI yang telah dipelajari selama satu semester.

Sistem yang diintegrasikan:

\`\`\`text
Player
   ↓
Game State / Player Performance
   ↓
AI Director
   ↓
Adaptive Difficulty / Intensity
   ↓
Enemy Spawner
   ↓
Enemy AI
   ├── Perception
   ├── Finite State Machine
   ├── NavMesh Navigation
   └── Combat
   ↓
Gameplay Result
   ↓
Performance Tracker
   └──────────────→ kembali ke AI Director
\`\`\`

Game tidak hanya memiliki enemy yang mengejar player. Sistem global **AI Director** mengamati kondisi permainan dan secara dinamis menentukan kapan tekanan terhadap player perlu dinaikkan atau diturunkan.

---

# 2. Rekomendasi Nama Project Unity

## Nama utama yang direkomendasikan

\`\`\`text
GC15_AdaptiveArenaDirector
\`\`\`

Keterangan:

- \`GC15\` = Game Cerdas Praktikum/Pertemuan 15.
- \`Adaptive\` = gameplay dapat berubah berdasarkan kondisi player.
- \`Arena\` = scope permainan dibuat kecil dan realistis.
- \`Director\` = sistem utama menggunakan AI Director.

### Alternatif nama

\`\`\`text
GC15_IntelligentArena
GC15_AdaptiveCombatArena
GC15_AIDirectorDemo
GC15_FinalAIIntegration
GC15_SmartSurvivalArena
\`\`\`

### Rekomendasi terbaik

\`\`\`text
GC15_AdaptiveArenaDirector
\`\`\`

Nama tersebut paling jelas menggambarkan fitur teknis utama praktikum.

---

# 3. Konsep Game

Game yang dibuat adalah **3D Survival Arena** sederhana.

Player berada di sebuah arena dan harus bertahan hidup dari enemy yang datang secara bertahap.

Namun jumlah dan intensitas enemy tidak sepenuhnya tetap.

AI Director memperhatikan:

\`\`\`text
Player Health
Kill Count
Enemy Count
Kill Rate
Time Since Last Spawn
Current Intensity
\`\`\`

Kemudian menentukan:

\`\`\`text
Spawn Enemy
Increase Pressure
Maintain Pressure
Decrease Pressure
Give Recovery Time
\`\`\`

Contoh:

\`\`\`text
Player health tinggi
+
player membunuh enemy dengan cepat
+
jumlah enemy sedikit
        ↓
AI Director meningkatkan intensity
        ↓
spawn enemy lebih cepat
\`\`\`

Sebaliknya:

\`\`\`text
Player health rendah
+
enemy masih banyak
        ↓
AI Director menurunkan pressure
        ↓
spawn ditunda
\`\`\`

---

# 4. Mengapa Praktikum Ini Dipilih?

Praktikum ini direkomendasikan karena dapat menggabungkan beberapa materi sebelumnya sekaligus:

| Materi | Implementasi Praktikum |
|---|---|
| Perception | Enemy mendeteksi player |
| Memory/State | Enemy menyimpan state perilaku |
| Movement AI | Enemy bergerak menuju target |
| Navigation | NavMeshAgent |
| FSM | Patrol, Chase, Attack |
| Tactical/Integration | Beberapa enemy aktif bersamaan |
| DDA | Intensity menyesuaikan performa |
| Player Modeling sederhana | Kill rate dan health |
| AI Director | Mengatur tekanan global |
| Debugging AI | State, target, intensity, path |
| Evaluasi AI | Correctness, fairness, responsiveness |

Dengan demikian praktikum bukan pengulangan satu algoritma, melainkan latihan **integrasi sistem AI**.

---

# 5. Tujuan Praktikum

Setelah menyelesaikan praktikum mahasiswa mampu:

1. Mengintegrasikan beberapa komponen Game AI.
2. Membuat enemy AI menggunakan FSM.
3. Menggunakan NavMesh untuk navigasi NPC.
4. Mengimplementasikan perception sederhana.
5. Membuat sistem AI Director.
6. Membuat adaptive gameplay.
7. Mengukur performa player.
8. Mengatur intensitas gameplay.
9. Membuat Enemy Spawner adaptif.
10. Membuat AI Debug Mode.
11. Menampilkan internal state AI.
12. Melakukan pengujian AI.
13. Mengevaluasi correctness dan fairness.
14. Menjelaskan arsitektur AI.
15. Menggunakan desain modular untuk proyek akhir.

---

# 6. Hasil Akhir Praktikum

Setelah selesai, gameplay yang diharapkan:

\`\`\`text
START
  ↓
Player masuk arena
  ↓
Enemy mulai muncul
  ↓
Enemy Patrol
  ↓
Player terlihat?
 ├── Tidak → Patrol
 └── Ya
       ↓
     Chase
       ↓
Dalam attack range?
 ├── Tidak → Chase
 └── Ya → Attack
\`\`\`

Pada saat bersamaan:

\`\`\`text
AI DIRECTOR
     ↓
Membaca kondisi game
     ↓
Menghitung intensity
     ↓
Player terlalu dominan?
     ↓ YA
Tambah tekanan
     ↓
Spawn lebih cepat / enemy lebih banyak
\`\`\`

atau:

\`\`\`text
Player tertekan?
     ↓ YA
Kurangi tekanan
     ↓
Delay spawning
\`\`\`

---

# 7. Gameplay Loop

Gameplay loop utama:

\`\`\`text
Move
 ↓
Find Enemy
 ↓
Fight Enemy
 ↓
Kill Enemy
 ↓
Director Evaluates Performance
 ↓
Difficulty / Intensity Changes
 ↓
New Enemy Encounter
 ↓
Survive
\`\`\`

Kondisi menang:

\`\`\`text
Survive selama 180 detik
\`\`\`

Kondisi kalah:

\`\`\`text
Player Health <= 0
\`\`\`

Nilai tersebut dapat diubah sesuai kebutuhan.

---

# 8. Arsitektur Sistem

Gunakan struktur:

\`\`\`text
GameManager
│
├── Player
│   ├── PlayerController
│   ├── PlayerHealth
│   └── PlayerCombat
│
├── AI Systems
│   ├── AIDirector
│   ├── EnemySpawner
│   └── AIDebugHUD
│
├── Enemies
│   └── Enemy
│       ├── EnemyAI
│       ├── EnemyHealth
│       └── NavMeshAgent
│
└── Environment
    ├── Ground
    ├── Obstacles
    ├── SpawnPoints
    └── PatrolPoints
\`\`\`

---

# 9. Istilah Teknis Unity yang Digunakan

## 9.1 GameObject

GameObject merupakan objek dasar dalam sebuah scene Unity.

Contoh:

\`\`\`text
Player
Enemy
Main Camera
Ground
GameManager
SpawnPoint
\`\`\`

GameObject pada dasarnya menjadi tempat berbagai **Component** dipasang.

---

# 9.2 Component

Component memberikan kemampuan tertentu kepada GameObject.

Contoh:

\`\`\`text
Transform
Collider
Rigidbody
NavMeshAgent
Camera
Script C#
\`\`\`

Contoh:

\`\`\`text
Enemy
├── Transform
├── Capsule Collider
├── NavMeshAgent
├── EnemyAI
└── EnemyHealth
\`\`\`

---

# 9.3 Transform

Semua GameObject memiliki Transform.

Transform menyimpan:

\`\`\`text
Position
Rotation
Scale
\`\`\`

Contoh mengakses posisi:

\`\`\`csharp
transform.position
\`\`\`

---

# 9.4 Prefab

Prefab adalah template GameObject yang dapat digunakan berulang kali.

Enemy akan dibuat sebagai:

\`\`\`text
Enemy.prefab
\`\`\`

EnemySpawner kemudian dapat membuat beberapa instance dari prefab tersebut.

Keuntungan prefab:

- konfigurasi enemy cukup dilakukan sekali;
- dapat digunakan berulang;
- perubahan prefab dapat diterapkan pada banyak instance;
- cocok untuk spawning.

---

# 9.5 Tag

Tag digunakan untuk memberi identitas logis pada GameObject.

Pada praktikum:

\`\`\`text
Player
\`\`\`

Player diberi Tag:

\`\`\`text
Player
\`\`\`

Contohnya:

\`\`\`csharp
GameObject.FindGameObjectWithTag("Player");
\`\`\`

---

# 9.6 Layer

Layer digunakan untuk mengelompokkan GameObject terutama untuk:

\`\`\`text
Physics
Raycast
Collision
Camera Culling
LayerMask
\`\`\`

Buat layer:

\`\`\`text
Player
Enemy
Obstacle
\`\`\`

Perception enemy nantinya dapat menggunakan LayerMask agar pemeriksaan objek lebih terkontrol.

---

# 9.7 Collider

Collider menentukan area fisik sebuah GameObject.

Contoh:

\`\`\`text
CapsuleCollider
BoxCollider
SphereCollider
\`\`\`

Collider digunakan oleh sistem physics untuk mendeteksi tabrakan dan raycast.

---

# 9.8 CharacterController

CharacterController dapat digunakan untuk menggerakkan player tanpa harus membuat simulasi Rigidbody yang kompleks.

Pada praktikum ini CharacterController digunakan pada Player.

---

# 9.9 NavMesh

NavMesh adalah representasi area yang dapat dilalui agent.

Enemy menggunakan NavMesh agar dapat mencari jalur menuju player tanpa menembus obstacle.

---

# 9.10 NavMeshAgent

NavMeshAgent adalah Component untuk menggerakkan GameObject pada NavMesh.

Parameter penting:

\`\`\`text
Speed
Angular Speed
Acceleration
Stopping Distance
Radius
Height
\`\`\`

---

# 9.11 NavMeshSurface

NavMeshSurface digunakan untuk menentukan area scene yang akan dibangun menjadi data navigasi.

Setelah environment selesai:

\`\`\`text
Bake NavMesh
\`\`\`

agar enemy mengetahui area yang dapat dilewati.

---

# 9.12 LayerMask

LayerMask memungkinkan script memilih layer tertentu.

Contoh:

\`\`\`csharp
[SerializeField] private LayerMask obstacleMask;
\`\`\`

Raycast kemudian hanya memeriksa obstacle.

---

# 9.13 Raycast

Raycast adalah pemeriksaan berbentuk garis dari suatu titik menuju arah tertentu.

Dalam perception:

\`\`\`text
Enemy
  ───────────────→ Player
        ray
\`\`\`

Jika garis terhalang wall:

\`\`\`text
Enemy → Wall → Player
\`\`\`

maka player dianggap tidak terlihat.

---

# 9.14 SerializeField

Contoh:

\`\`\`csharp
[SerializeField] private float viewDistance = 12f;
\`\`\`

\`SerializeField\` membuat variabel private tetap dapat diedit melalui Inspector.

Ini berguna untuk parameter AI karena mahasiswa dapat melakukan tuning tanpa mengubah source code.

---

# 9.15 Inspector

Inspector adalah panel Unity untuk mengatur component dan parameter GameObject.

Contoh:

\`\`\`text
Enemy AI
View Distance: 12
Attack Range: 2
Attack Damage: 10
Attack Cooldown: 1
\`\`\`

---

# 9.16 Gizmos

Gizmos merupakan visualisasi bantu di Scene View.

Dalam praktikum digunakan untuk menampilkan:

\`\`\`text
Detection Radius
Attack Range
Target Line
Spawn Radius
\`\`\`

Gizmos sangat bermanfaat karena kondisi internal AI dapat dilihat secara visual.

---

# 9.17 Coroutine

Coroutine memungkinkan proses berlangsung selama beberapa frame.

Contoh:

\`\`\`csharp
IEnumerator Example()
{
    yield return new WaitForSeconds(1f);
}
\`\`\`

Namun pada praktikum utama director menggunakan timer berbasis \`Time.deltaTime\` agar aliran logika lebih mudah diamati mahasiswa.

---

# 9.18 Time.deltaTime

\`Time.deltaTime\` adalah waktu yang berlalu sejak frame sebelumnya.

Contoh:

\`\`\`csharp
timer += Time.deltaTime;
\`\`\`

Dengan demikian perhitungan waktu tidak bergantung langsung pada frame rate.

---

# 9.19 Singleton

Singleton adalah pola sederhana untuk menyediakan satu instance global sebuah manager.

Contoh:

\`\`\`csharp
GameManager.Instance
AIDirector.Instance
\`\`\`

Gunakan secara terbatas.

Untuk praktikum kecil pola ini mempermudah integrasi, tetapi pada proyek besar dependency injection atau reference yang eksplisit dapat menghasilkan struktur yang lebih terkontrol.

---

# 10. Persiapan Project

Buat project baru:

\`\`\`text
GC15_AdaptiveArenaDirector
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau template 3D lain yang telah digunakan selama praktikum sebelumnya.

---

# 11. Struktur Folder

Buat folder:

\`\`\`text
Assets
├── _Project
│   ├── Scenes
│   ├── Scripts
│   │   ├── Player
│   │   ├── Enemy
│   │   ├── AI
│   │   └── Managers
│   ├── Prefabs
│   │   ├── Characters
│   │   └── Environment
│   ├── Materials
│   ├── Models
│   ├── UI
│   └── Audio
\`\`\`

Keuntungan struktur tersebut adalah script dan asset final project tidak bercampur dengan package atau asset eksternal.

---

# 12. Membuat Scene

Simpan scene:

\`\`\`text
Assets/_Project/Scenes/Arena.unity
\`\`\`

Hierarchy dasar:

\`\`\`text
Arena
├── Environment
│   ├── Ground
│   ├── Wall_North
│   ├── Wall_South
│   ├── Wall_East
│   ├── Wall_West
│   └── Obstacles
│
├── Player
├── Main Camera
│
├── Navigation
│
├── SpawnPoints
│   ├── SpawnPoint_01
│   ├── SpawnPoint_02
│   ├── SpawnPoint_03
│   └── SpawnPoint_04
│
├── PatrolPoints
│
└── Systems
    ├── GameManager
    ├── EnemySpawner
    ├── AIDirector
    └── AIDebugHUD
\`\`\`

---

# 13. Membuat Arena

Buat Ground:

\`\`\`text
GameObject
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
Ground
\`\`\`

Scale misalnya:

\`\`\`text
X = 4
Y = 1
Z = 4
\`\`\`

Tambahkan beberapa cube sebagai obstacle.

Contoh:

\`\`\`text
Obstacle01
Obstacle02
Obstacle03
Obstacle04
\`\`\`

Tujuan obstacle adalah agar pathfinding dan line-of-sight enemy dapat terlihat saat pengujian.

---

# 14. Menyiapkan Navigation

Pastikan sistem AI Navigation tersedia pada project.

Tambahkan NavMeshSurface pada GameObject:

\`\`\`text
Navigation
\`\`\`

Kemudian lakukan:

\`\`\`text
Bake
\`\`\`

Pastikan Ground menjadi area walkable.

Obstacle harus membentuk area yang tidak dapat dilewati.

Setelah bake berhasil, area navigasi akan terlihat pada Scene View saat visualisasi Navigation aktif.

---

# 15. Membuat Player

Buat:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Tag:

\`\`\`text
Player
\`\`\`

Tambahkan:

\`\`\`text
CharacterController
PlayerController
PlayerHealth
PlayerCombat
\`\`\`

Hapus CapsuleCollider bawaan jika menggunakan CharacterController agar tidak terdapat collider karakter yang tidak diperlukan.

---

# 16. Script PlayerController.cs

Buat:

\`\`\`text
Assets/_Project/Scripts/Player/PlayerController.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Rotation")]
    [SerializeField] private Camera mainCamera;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        Move();
        RotateTowardMouse();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical).normalized;

        controller.Move(direction * moveSpeed * Time.deltaTime);
    }

    private void RotateTowardMouse()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 target = ray.GetPoint(distance);
            Vector3 direction = target - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.forward = direction.normalized;
            }
        }
    }
}
\`\`\`

---

# 17. Penjelasan PlayerController

## RequireComponent

\`\`\`csharp
[RequireComponent(typeof(CharacterController))]
\`\`\`

Memastikan GameObject memiliki CharacterController.

---

## Input.GetAxisRaw

\`\`\`csharp
Input.GetAxisRaw("Horizontal")
Input.GetAxisRaw("Vertical")
\`\`\`

Digunakan untuk membaca input movement tradisional:

\`\`\`text
W / S
A / D
Arrow keys
\`\`\`

Jika project menggunakan Input System baru, bagian input dapat diganti dengan sistem input yang telah digunakan pada praktikum sebelumnya.

---

## normalized

\`\`\`csharp
direction.normalized
\`\`\`

Mencegah gerakan diagonal menjadi lebih cepat dibandingkan gerakan horizontal/vertikal.

---

# 18. PlayerHealth.cs

Buat:

\`\`\`text
PlayerHealth.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    public float MaxHealth => maxHealth;

    public float HealthNormalized =>
        maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
            return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        if (CurrentHealth <= 0f)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerDied();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
    }
}
\`\`\`

---

# 19. Normalized Value

Bagian:

\`\`\`csharp
CurrentHealth / maxHealth
\`\`\`

mengubah health menjadi rentang:

\`\`\`text
0.0 – 1.0
\`\`\`

Contoh:

\`\`\`text
100 / 100 = 1.00
75 / 100  = 0.75
30 / 100  = 0.30
\`\`\`

Normalized value sangat berguna untuk sistem adaptive karena tidak bergantung pada skala absolut.

---

# 20. Membuat PlayerCombat.cs

Player menggunakan raycast sederhana untuk menyerang enemy.

\`\`\`csharp
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private LayerMask hitMask;

    private float nextFireTime;

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
            return;

        if (Input.GetMouseButton(0) &&
            Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireCooldown;
        }
    }

    private void Shoot()
    {
        Vector3 origin =
            transform.position + Vector3.up * 0.8f;

        Vector3 direction = transform.forward;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            attackRange,
            hitMask))
        {
            EnemyHealth enemy =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.8f,
            transform.position +
            Vector3.up * 0.8f +
            transform.forward * attackRange
        );
    }
}
\`\`\`

Set \`hitMask\` agar mencakup:

\`\`\`text
Enemy
Obstacle
\`\`\`

Dengan demikian tembakan tidak menembus wall.

---

# 21. Membuat Enemy

Buat Capsule baru:

\`\`\`text
Enemy
\`\`\`

Tambahkan:

\`\`\`text
NavMeshAgent
EnemyAI
EnemyHealth
\`\`\`

Layer:

\`\`\`text
Enemy
\`\`\`

Parameter NavMeshAgent awal:

\`\`\`text
Speed             = 3.5
Angular Speed     = 360
Acceleration      = 8
Stopping Distance = 1.5
\`\`\`

Kemudian jadikan prefab:

\`\`\`text
Assets/_Project/Prefabs/Characters/Enemy.prefab
\`\`\`

Hapus instance dari scene setelah prefab selesai.

---

# 22. Finite State Machine Enemy

Enemy menggunakan tiga state utama:

\`\`\`text
PATROL
CHASE
ATTACK
\`\`\`

Alurnya:

\`\`\`text
          Player Seen
PATROL ───────────────→ CHASE
                          │
                          │ in attack range
                          ↓
                       ATTACK
                          │
                          │ player too far
                          ↓
                        CHASE

CHASE
  │
  │ player lost
  ↓
PATROL
\`\`\`

---

# 23. Membuat EnemyAI.cs

\`\`\`csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Perception")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Patrol")]
    [SerializeField] private float patrolRadius = 7f;
    [SerializeField] private float patrolWaitTime = 2f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;

    private AIState currentState;

    private float nextAttackTime;
    private float patrolTimer;

    private Vector3 patrolDestination;

    public AIState CurrentState => currentState;

    public Transform CurrentTarget => player;

    public NavMeshAgent Agent => agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth =
                playerObject.GetComponent<PlayerHealth>();
        }

        ChangeState(AIState.Patrol);

        ChooseNewPatrolPoint();
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
        {
            agent.isStopped = true;
            return;
        }

        bool playerVisible = CanSeePlayer();

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol(playerVisible);
                break;

            case AIState.Chase:
                UpdateChase(playerVisible);
                break;

            case AIState.Attack:
                UpdateAttack(playerVisible);
                break;
        }
    }

    private void UpdatePatrol(bool playerVisible)
    {
        agent.isStopped = false;

        if (playerVisible)
        {
            ChangeState(AIState.Chase);
            return;
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                ChooseNewPatrolPoint();
                patrolTimer = 0f;
            }
        }
    }

    private void UpdateChase(bool playerVisible)
    {
        if (player == null)
            return;

        agent.isStopped = false;

        float distance =
            Vector3.Distance(transform.position, player.position);

        if (!playerVisible)
        {
            ChangeState(AIState.Patrol);
            ChooseNewPatrolPoint();
            return;
        }

        if (distance <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        agent.SetDestination(player.position);
    }

    private void UpdateAttack(bool playerVisible)
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(transform.position, player.position);

        if (!playerVisible || distance > attackRange)
        {
            ChangeState(AIState.Chase);
            return;
        }

        agent.isStopped = true;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.forward = direction.normalized;
        }

        if (Time.time >= nextAttackTime)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            nextAttackTime =
                Time.time + attackCooldown;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin =
            transform.position + Vector3.up * 0.8f;

        Vector3 playerPosition =
            player.position + Vector3.up * 0.8f;

        Vector3 toPlayer =
            playerPosition - origin;

        float distance = toPlayer.magnitude;

        if (distance > viewDistance)
            return false;

        float angle =
            Vector3.Angle(transform.forward, toPlayer);

        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(
            origin,
            toPlayer.normalized,
            distance,
            obstacleMask))
        {
            return false;
        }

        return true;
    }

    private void ChooseNewPatrolPoint()
    {
        Vector3 randomDirection =
            Random.insideUnitSphere * patrolRadius;

        randomDirection.y = 0f;

        Vector3 candidate =
            transform.position + randomDirection;

        if (NavMesh.SamplePosition(
            candidate,
            out NavMeshHit hit,
            patrolRadius,
            NavMesh.AllAreas))
        {
            patrolDestination = hit.position;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(patrolDestination);
            }
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        AIState previousState = currentState;

        currentState = newState;

        if (AIDebugHUD.DebugEnabled)
        {
            Debug.Log(
                $"[{name}] State: " +
                $"{previousState} -> {newState}"
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            viewDistance
        );

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Gizmos.DrawSphere(
            patrolDestination,
            0.2f
        );

        if (player != null)
        {
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.8f,
                player.position + Vector3.up * 0.8f
            );
        }
    }
}
\`\`\`

---

# 24. Analisis Enemy Perception

Enemy hanya melihat player ketika seluruh syarat berikut terpenuhi:

\`\`\`text
Distance valid
       ↓
Field of View valid
       ↓
Line of Sight tidak terhalang
       ↓
PLAYER VISIBLE
\`\`\`

## Distance

\`\`\`csharp
if (distance > viewDistance)
    return false;
\`\`\`

Enemy tidak dapat mendeteksi objek yang terlalu jauh.

---

## Field of View

\`\`\`csharp
Vector3.Angle(transform.forward, toPlayer)
\`\`\`

menghitung sudut antara arah pandang enemy dan posisi player.

Misalnya:

\`\`\`text
viewAngle = 120°
\`\`\`

berarti:

\`\`\`text
60° kiri
+
60° kanan
\`\`\`

---

## Line of Sight

\`\`\`csharp
Physics.Raycast(...)
\`\`\`

digunakan untuk memastikan tidak ada obstacle antara enemy dan player.

Inilah yang mencegah enemy seolah-olah dapat melihat menembus dinding.

---

# 25. EnemyHealth.cs

\`\`\`csharp
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;

    private float currentHealth;
    private bool dead;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        dead = false;
    }

    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        dead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill();
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.NotifyEnemyKilled(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
\`\`\`

---

# 26. Membuat Spawn Points

Di Hierarchy:

\`\`\`text
SpawnPoints
├── SpawnPoint_01
├── SpawnPoint_02
├── SpawnPoint_03
├── SpawnPoint_04
└── SpawnPoint_05
\`\`\`

Gunakan Empty GameObject.

Letakkan di beberapa sisi arena.

Prinsip penting:

\`\`\`text
Jangan menempatkan spawn point tepat di dekat Player.
\`\`\`

Spawn yang terlalu dekat dapat membuat AI terasa curang.

---

# 27. EnemySpawner.cs

\`\`\`csharp
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private int hardEnemyLimit = 12;

    [SerializeField] private float minimumSpawnDistanceFromPlayer = 8f;

    private readonly List<GameObject> activeEnemies =
        new List<GameObject>();

    private Transform player;

    public int ActiveEnemyCount
    {
        get
        {
            CleanupList();
            return activeEnemies.Count;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }

    public bool SpawnEnemy()
    {
        CleanupList();

        if (enemyPrefab == null)
            return false;

        if (activeEnemies.Count >= hardEnemyLimit)
            return false;

        Transform spawnPoint = FindValidSpawnPoint();

        if (spawnPoint == null)
            return false;

        GameObject enemy =
            Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        activeEnemies.Add(enemy);

        return true;
    }

    private Transform FindValidSpawnPoint()
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
            return null;

        int startIndex =
            Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int index =
                (startIndex + i) % spawnPoints.Length;

            Transform candidate = spawnPoints[index];

            if (candidate == null)
                continue;

            if (player == null)
                return candidate;

            float distance =
                Vector3.Distance(
                    candidate.position,
                    player.position
                );

            if (distance >= minimumSpawnDistanceFromPlayer)
                return candidate;
        }

        return null;
    }

    public void NotifyEnemyKilled(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        Destroy(enemy);
    }

    private void CleanupList()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }
}
\`\`\`

---

# 28. Mengapa Ada Hard Enemy Limit?

\`\`\`csharp
hardEnemyLimit = 12;
\`\`\`

AI Director sebaiknya tidak memiliki kebebasan tanpa batas.

Tanpa batasan dapat terjadi:

\`\`\`text
Director ingin menambah pressure
        ↓
spawn terus
        ↓
20 enemy
        ↓
30 enemy
        ↓
performance turun
        ↓
game tidak fair
\`\`\`

Hard limit adalah bentuk **constraint**.

Prinsip Advanced Game AI:

\`\`\`text
Adaptive
≠
Unlimited
\`\`\`

---

# 29. GameManager.cs

GameManager menyimpan state gameplay dan statistik player.

\`\`\`csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float survivalDuration = 180f;

    private float gameTime;
    private int killCount;

    public bool IsGameOver { get; private set; }

    public bool PlayerWon { get; private set; }

    public float GameTime => gameTime;

    public float RemainingTime =>
        Mathf.Max(0f, survivalDuration - gameTime);

    public int KillCount => killCount;

    public float KillsPerMinute
    {
        get
        {
            if (gameTime <= 1f)
                return 0f;

            return killCount / (gameTime / 60f);
        }
    }

    private void Awake()
    {
        Instance = this;

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (IsGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                RestartScene();

            return;
        }

        gameTime += Time.deltaTime;

        if (gameTime >= survivalDuration)
        {
            WinGame();
        }
    }

    public void RegisterKill()
    {
        if (IsGameOver)
            return;

        killCount++;
    }

    public void PlayerDied()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        PlayerWon = false;
    }

    private void WinGame()
    {
        IsGameOver = true;
        PlayerWon = true;
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
\`\`\`

---

# 30. Player Performance Metrics

AI Director membutuhkan informasi mengenai player.

Pada praktikum digunakan:

\`\`\`text
HealthNormalized
KillsPerMinute
EnemyCount
\`\`\`

Contoh interpretation:

\`\`\`text
Health = 90%
Kills/minute = tinggi
Enemy count = rendah

→ player relatif dominan
\`\`\`

Sebaliknya:

\`\`\`text
Health = 20%
Enemy count = tinggi

→ player sedang tertekan
\`\`\`

---

# 31. Konsep Intensity

Gunakan nilai intensity:

\`\`\`text
0.0 ───────────────────── 1.0

Low                        High
\`\`\`

Contoh:

\`\`\`text
0.20 = Low
0.50 = Medium
0.85 = High
\`\`\`

Intensity menjadi representasi internal mengenai besarnya tekanan gameplay yang ingin diberikan Director.

---

# 32. Membuat AIDirector.cs

\`\`\`csharp
using UnityEngine;

public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Director Timing")]
    [SerializeField] private float evaluationInterval = 2f;

    [Header("Spawn Timing")]
    [SerializeField] private float minimumSpawnInterval = 1.5f;
    [SerializeField] private float maximumSpawnInterval = 6f;

    [Header("Intensity")]
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.3f;

    [SerializeField] private float intensityChangeSpeed = 0.1f;

    [Header("Target Enemy Count")]
    [SerializeField] private int minimumEnemies = 2;
    [SerializeField] private int maximumEnemies = 8;

    private float evaluationTimer;
    private float spawnTimer;

    private string lastDecision = "Initializing";

    public float Intensity => intensity;

    public string LastDecision => lastDecision;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerHealth =
                    player.GetComponent<PlayerHealth>();
            }
        }

        if (enemySpawner == null)
            enemySpawner = EnemySpawner.Instance;
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.IsGameOver)
            return;

        evaluationTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (evaluationTimer >= evaluationInterval)
        {
            EvaluateGameState();

            evaluationTimer = 0f;
        }

        TrySpawn();
    }

    private void EvaluateGameState()
    {
        if (playerHealth == null ||
            enemySpawner == null ||
            GameManager.Instance == null)
            return;

        float health =
            playerHealth.HealthNormalized;

        float killRate =
            GameManager.Instance.KillsPerMinute;

        int enemyCount =
            enemySpawner.ActiveEnemyCount;

        // PLAYER SANGAT TERTEKAN
        if (health < 0.30f)
        {
            intensity -= intensityChangeSpeed;

            lastDecision =
                "Reduce pressure: player health low";
        }

        // PLAYER DOMINAN
        else if (
            health > 0.70f &&
            killRate >= 4f &&
            enemyCount <= 4)
        {
            intensity += intensityChangeSpeed;

            lastDecision =
                "Increase pressure: player dominant";
        }

        // ARENA TERLALU SEPI
        else if (
            health > 0.50f &&
            enemyCount < minimumEnemies)
        {
            intensity += intensityChangeSpeed * 0.5f;

            lastDecision =
                "Increase pressure: arena too quiet";
        }

        // ENEMY TERLALU BANYAK
        else if (enemyCount >= maximumEnemies)
        {
            intensity -= intensityChangeSpeed;

            lastDecision =
                "Reduce pressure: too many enemies";
        }

        else
        {
            intensity =
                Mathf.MoveTowards(
                    intensity,
                    0.5f,
                    intensityChangeSpeed * 0.25f
                );

            lastDecision =
                "Maintain balanced intensity";
        }

        intensity = Mathf.Clamp01(intensity);

        if (AIDebugHUD.DebugEnabled)
        {
            Debug.Log(
                $"[Director] {lastDecision} | " +
                $"Health={health:F2}, " +
                $"KPM={killRate:F2}, " +
                $"Enemies={enemyCount}, " +
                $"Intensity={intensity:F2}"
            );
        }
    }

    private void TrySpawn()
    {
        if (enemySpawner == null)
            return;

        int targetEnemyCount =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    minimumEnemies,
                    maximumEnemies,
                    intensity
                )
            );

        if (enemySpawner.ActiveEnemyCount >= targetEnemyCount)
            return;

        float spawnInterval =
            Mathf.Lerp(
                maximumSpawnInterval,
                minimumSpawnInterval,
                intensity
            );

        if (spawnTimer >= spawnInterval)
        {
            bool spawned =
                enemySpawner.SpawnEnemy();

            if (spawned)
            {
                spawnTimer = 0f;
            }
        }
    }
}
\`\`\`

---

# 33. Bagaimana AI Director Bekerja?

Pipeline:

\`\`\`text
Collect Game State
       ↓
Health
Kill Rate
Enemy Count
       ↓
Evaluate
       ↓
Calculate Intensity
       ↓
Determine Target Enemy Count
       ↓
Determine Spawn Interval
       ↓
EnemySpawner
       ↓
Observe Result
\`\`\`

---

# 34. Adaptive Rule 1 — Player Low Health

\`\`\`csharp
if (health < 0.30f)
{
    intensity -= intensityChangeSpeed;
}
\`\`\`

Interpretasi:

\`\`\`text
Player Health < 30%
       ↓
Director menganggap player tertekan
       ↓
Intensity turun
       ↓
Target enemy berkurang
       ↓
Spawn interval menjadi lebih lama
\`\`\`

Ini memberi player kesempatan recovery.

---

# 35. Adaptive Rule 2 — Player Dominan

\`\`\`csharp
health > 0.70f
killRate >= 4f
enemyCount <= 4
\`\`\`

Interpretasinya:

\`\`\`text
Health tinggi
+
kill rate tinggi
+
enemy sedikit
       ↓
Player terlalu nyaman
       ↓
Director menaikkan intensity
\`\`\`

---

# 36. Mengapa Tidak Langsung Mengubah Difficulty?

Contoh yang buruk:

\`\`\`text
Player kill satu enemy
       ↓
Difficulty langsung +50%
\`\`\`

Akibatnya perubahan terlalu mendadak.

Pada implementasi ini digunakan:

\`\`\`csharp
intensity += intensityChangeSpeed;
\`\`\`

sehingga perubahan lebih bertahap.

Konsep ini disebut **smoothing** atau gradual adjustment.

---

# 37. Mathf.Clamp01

\`\`\`csharp
Mathf.Clamp01(intensity)
\`\`\`

menjamin nilai selalu berada pada:

\`\`\`text
0 <= intensity <= 1
\`\`\`

Tanpa clamp dapat terjadi:

\`\`\`text
Intensity = 1.4
Intensity = -0.2
\`\`\`

yang dapat menyebabkan parameter adaptasi tidak sesuai desain.

---

# 38. Mathf.Lerp untuk Adaptive Parameter

Kode:

\`\`\`csharp
Mathf.Lerp(
    maximumSpawnInterval,
    minimumSpawnInterval,
    intensity
);
\`\`\`

Misalnya:

\`\`\`text
maximumSpawnInterval = 6
minimumSpawnInterval = 1.5
\`\`\`

Jika:

\`\`\`text
Intensity = 0
\`\`\`

maka interval mendekati:

\`\`\`text
6 detik
\`\`\`

Jika:

\`\`\`text
Intensity = 1
\`\`\`

maka interval mendekati:

\`\`\`text
1.5 detik
\`\`\`

Artinya:

\`\`\`text
Intensity naik
     ↓
Spawn lebih sering
\`\`\`

---

# 39. Target Enemy Count

Director juga menghitung:

\`\`\`csharp
Mathf.Lerp(
    minimumEnemies,
    maximumEnemies,
    intensity
);
\`\`\`

Contoh:

\`\`\`text
minimumEnemies = 2
maximumEnemies = 8
\`\`\`

Maka kira-kira:

\`\`\`text
Intensity 0.0 → target 2
Intensity 0.5 → target 5
Intensity 1.0 → target 8
\`\`\`

Dengan demikian adaptasi tidak hanya mengubah spawn speed tetapi juga jumlah tekanan aktif di arena.

---

# 40. Mengapa AI Director Lebih Baik daripada Spawner Biasa?

Spawner biasa:

\`\`\`text
Setiap 3 detik
     ↓
Spawn enemy
\`\`\`

Tidak peduli:

\`\`\`text
Player hampir mati
Player sedang menang
Enemy masih banyak
Arena sedang kosong
\`\`\`

AI Director:

\`\`\`text
Observe
   ↓
Evaluate
   ↓
Decide
   ↓
Adapt
\`\`\`

Game menjadi lebih responsif terhadap kondisi gameplay.

---

# 41. Membuat AI Debug Mode

Salah satu requirement penting praktikum adalah AI dapat dijelaskan.

Buat:

\`\`\`text
AIDebugHUD.cs
\`\`\`

---

# 42. AIDebugHUD.cs

\`\`\`csharp
using UnityEngine;

public class AIDebugHUD : MonoBehaviour
{
    public static bool DebugEnabled { get; private set; }

    [SerializeField] private PlayerHealth playerHealth;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;

    private void Start()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null && playerHealth == null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();
        }

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        textStyle = new GUIStyle();
        textStyle.fontSize = 16;
        textStyle.normal.textColor = Color.white;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugEnabled = !DebugEnabled;
        }
    }

    private void OnGUI()
    {
        if (!DebugEnabled)
            return;

        GUI.Box(
            new Rect(10, 10, 430, 250),
            ""
        );

        GUI.Label(
            new Rect(25, 20, 350, 30),
            "AI DEBUG MODE",
            titleStyle
        );

        float health =
            playerHealth != null
                ? playerHealth.CurrentHealth
                : 0f;

        float intensity =
            AIDirector.Instance != null
                ? AIDirector.Instance.Intensity
                : 0f;

        string decision =
            AIDirector.Instance != null
                ? AIDirector.Instance.LastDecision
                : "-";

        int enemies =
            EnemySpawner.Instance != null
                ? EnemySpawner.Instance.ActiveEnemyCount
                : 0;

        int kills =
            GameManager.Instance != null
                ? GameManager.Instance.KillCount
                : 0;

        float kpm =
            GameManager.Instance != null
                ? GameManager.Instance.KillsPerMinute
                : 0f;

        string info =
            $"Player Health : {health:F0}\\n" +
            $"Enemy Count   : {enemies}\\n" +
            $"Kills         : {kills}\\n" +
            $"Kills/Minute  : {kpm:F2}\\n" +
            $"Intensity     : {intensity:F2}\\n" +
            $"Director      : {decision}\\n\\n" +
            "F1 = Toggle Debug";

        GUI.Label(
            new Rect(25, 55, 390, 190),
            info,
            textStyle
        );
    }
}
\`\`\`

---

# 43. Informasi Debug yang Ditampilkan

Saat F1 ditekan:

\`\`\`text
AI DEBUG MODE

Player Health : 80
Enemy Count   : 4
Kills         : 7
Kills/Minute  : 5.20
Intensity     : 0.70

Director:
Increase pressure: player dominant
\`\`\`

Data tersebut menjawab:

\`\`\`text
Apa yang dilihat AI Director?
Apa keputusan Director?
Mengapa gameplay berubah?
\`\`\`

Ini merupakan aspek penting **explainability**.

---

# 44. Membuat Game Status UI Sederhana

Tambahkan script berikut pada GameManager jika ingin HUD pengujian cepat:

\`\`\`csharp
private void OnGUI()
{
    GUI.Box(
        new Rect(
            Screen.width - 230,
            10,
            220,
            100
        ),
        ""
    );

    GUI.Label(
        new Rect(
            Screen.width - 215,
            25,
            200,
            25
        ),
        $"Time: {RemainingTime:F0}"
    );

    GUI.Label(
        new Rect(
            Screen.width - 215,
            50,
            200,
            25
        ),
        $"Kills: {killCount}"
    );

    if (IsGameOver)
    {
        string message =
            PlayerWon
                ? "YOU SURVIVED!"
                : "GAME OVER";

        GUI.Box(
            new Rect(
                Screen.width / 2 - 150,
                Screen.height / 2 - 70,
                300,
                140
            ),
            message + "\\n\\nPress R to Restart"
        );
    }
}
\`\`\`

Untuk final project sebenarnya, HUD player sebaiknya dibuat menggunakan Canvas/UI system agar visual lebih baik.

\`OnGUI\` di sini digunakan agar fokus praktikum tetap pada integrasi AI.

---

# 45. Menambahkan Debug State pada Enemy

Tambahkan script:

\`\`\`text
EnemyDebugLabel.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class EnemyDebugLabel : MonoBehaviour
{
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    private void OnGUI()
    {
        if (!AIDebugHUD.DebugEnabled ||
            enemyAI == null)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 screenPosition =
            cam.WorldToScreenPoint(
                transform.position +
                Vector3.up * 2.2f
            );

        if (screenPosition.z <= 0)
            return;

        Rect rect =
            new Rect(
                screenPosition.x - 60,
                Screen.height -
                screenPosition.y - 15,
                120,
                25
            );

        GUI.Label(
            rect,
            enemyAI.CurrentState.ToString()
        );
    }
}
\`\`\`

Tambahkan pada prefab Enemy.

Saat debug aktif mahasiswa dapat melihat:

\`\`\`text
Enemy01: PATROL
Enemy02: CHASE
Enemy03: ATTACK
\`\`\`

secara langsung di Game View.

---

# 46. Kamera

Untuk praktikum ini gunakan kamera top-down/isometric sederhana.

Contoh posisi:

\`\`\`text
Position:
X = 0
Y = 18
Z = -12

Rotation:
X = 55
Y = 0
Z = 0
\`\`\`

Agar kamera mengikuti player, buat:

\`\`\`text
CameraFollow.cs
\`\`\`

\`\`\`csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset =
        new Vector3(0f, 18f, -12f);

    [SerializeField] private float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition =
            target.position + offset;

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
    }
}
\`\`\`

Assign Player ke:

\`\`\`text
Target
\`\`\`

---

# 47. Menghubungkan Semua Component

## Player

\`\`\`text
Player
├── CharacterController
├── PlayerController
├── PlayerHealth
└── PlayerCombat
\`\`\`

Tag:

\`\`\`text
Player
\`\`\`

---

## Enemy Prefab

\`\`\`text
Enemy
├── NavMeshAgent
├── EnemyAI
├── EnemyHealth
└── EnemyDebugLabel
\`\`\`

Layer:

\`\`\`text
Enemy
\`\`\`

---

## EnemySpawner

Inspector:

\`\`\`text
Enemy Prefab:
Enemy.prefab

Spawn Points:
SpawnPoint_01
SpawnPoint_02
SpawnPoint_03
SpawnPoint_04

Hard Enemy Limit:
12

Minimum Spawn Distance:
8
\`\`\`

---

## AIDirector

Assign:

\`\`\`text
Player Health → Player
Enemy Spawner → EnemySpawner
\`\`\`

Parameter awal:

\`\`\`text
Evaluation Interval     = 2
Minimum Spawn Interval  = 1.5
Maximum Spawn Interval  = 6
Intensity               = 0.3
Intensity Change Speed  = 0.1
Minimum Enemies         = 2
Maximum Enemies         = 8
\`\`\`

---

# 48. Layer Setup

Buat layer:

\`\`\`text
Player
Enemy
Obstacle
\`\`\`

Assign:

\`\`\`text
Player → Player
Enemy → Enemy
Walls → Obstacle
Obstacle objects → Obstacle
\`\`\`

Pada EnemyAI:

\`\`\`text
Obstacle Mask = Obstacle
\`\`\`

Pada PlayerCombat:

\`\`\`text
Hit Mask =
Enemy + Obstacle
\`\`\`

---

# 49. Urutan Pengujian yang Benar

Jangan langsung menguji seluruh sistem sekaligus.

Gunakan urutan berikut.

## Test 1 — Player

Pastikan:

\`\`\`text
WASD bergerak
Mouse mengubah arah
\`\`\`

---

## Test 2 — Navigation

Masukkan satu Enemy secara manual.

Pastikan:

\`\`\`text
Enemy berada pada NavMesh
Enemy dapat berjalan
Enemy tidak menembus obstacle
\`\`\`

---

## Test 3 — Perception

Posisikan player:

\`\`\`text
di depan enemy
di belakang enemy
di balik wall
di luar detection range
\`\`\`

Perhatikan perubahan perilaku.

---

## Test 4 — FSM

Aktifkan F1.

Pastikan urutan:

\`\`\`text
PATROL
   ↓
CHASE
   ↓
ATTACK
\`\`\`

berfungsi.

---

## Test 5 — Combat

Tembak enemy.

Pastikan:

\`\`\`text
Enemy health turun
Enemy mati
Kill count bertambah
\`\`\`

---

## Test 6 — Spawner

Jalankan:

\`\`\`text
EnemySpawner.SpawnEnemy()
\`\`\`

atau jalankan game bersama Director.

Pastikan enemy muncul dari spawn point.

---

## Test 7 — AI Director

Perhatikan:

\`\`\`text
Intensity
Enemy Count
Kills/Minute
Director Decision
\`\`\`

---

# 50. Skenario Pengujian AI Director

## Skenario A — Player Dominan

Usahakan:

\`\`\`text
Health > 70%
Kill Rate > 4/min
Enemy <= 4
\`\`\`

Hasil yang diharapkan:

\`\`\`text
Director:
Increase pressure: player dominant

Intensity naik
Spawn interval turun
Enemy target naik
\`\`\`

---

# 51. Skenario B — Player Low Health

Biarkan enemy menyerang sampai:

\`\`\`text
Health < 30%
\`\`\`

Hasil:

\`\`\`text
Director:
Reduce pressure: player health low
\`\`\`

Intensity turun.

---

# 52. Skenario C — Terlalu Banyak Enemy

Biarkan:

\`\`\`text
Enemy Count >= Maximum Enemies
\`\`\`

Director seharusnya tidak terus menambah tekanan.

---

# 53. Skenario D — Arena Sepi

Bunuh enemy hingga:

\`\`\`text
Enemy Count < Minimum Enemies
\`\`\`

dengan health player masih cukup tinggi.

Director secara perlahan menaikkan tekanan.

---

# 54. Konsep Feedback Loop

Sistem sekarang mempunyai feedback loop:

\`\`\`text
Player Action
      ↓
Player Performance
      ↓
Performance Metrics
      ↓
AI Director
      ↓
Adaptation
      ↓
Enemy Pressure
      ↓
Player Action
\`\`\`

Ini adalah karakteristik penting sebuah adaptive game system.

---

# 55. AI Director versus Enemy AI

## Enemy AI

Berpikir secara lokal:

\`\`\`text
Apakah saya melihat player?
Apakah player dekat?
Haruskah saya menyerang?
\`\`\`

## AI Director

Berpikir secara global:

\`\`\`text
Apakah player terlalu nyaman?
Apakah permainan terlalu sulit?
Berapa banyak enemy yang seharusnya aktif?
Apakah perlu encounter berikutnya?
\`\`\`

Dengan demikian:

\`\`\`text
Enemy AI = local intelligence
AI Director = global gameplay intelligence
\`\`\`

---

# 56. Menambahkan Emergent Behavior — Opsional

Untuk pengembangan lanjut, tambahkan beberapa enemy sekaligus.

Masing-masing hanya mempunyai aturan sederhana:

\`\`\`text
Chase player
Avoid obstacle
NavMesh local avoidance
Attack ketika dekat
\`\`\`

Interaksi beberapa agent dapat menghasilkan pola gameplay yang tidak sepenuhnya di-script.

Contohnya enemy dapat:

\`\`\`text
datang dari arah berbeda,
memotong jalur player,
memaksa player berpindah posisi.
\`\`\`

Namun jangan mengklaim setiap perilaku kelompok otomatis merupakan emergence yang baik.

Evaluasilah apakah pola tersebut benar-benar muncul dari interaksi rule dan memberi pengaruh pada gameplay.

---

# 57. Pengembangan Advanced — Recovery Event

AI Director dapat dikembangkan untuk memberi bantuan.

Contoh:

\`\`\`text
Health < 20%
Enemy <= 3
        ↓
Spawn Health Pickup
\`\`\`

Director tidak hanya mengatur enemy, tetapi dapat menjadi:

\`\`\`text
Encounter Manager
Resource Manager
Tension Manager
\`\`\`

---

# 58. Pengembangan Advanced — Enemy Aggression

Director juga dapat mengubah parameter global:

\`\`\`text
Intensity 0.0–0.3
→ enemy aggression rendah

Intensity 0.3–0.7
→ normal

Intensity > 0.7
→ aggression tinggi
\`\`\`

Contohnya:

\`\`\`text
Attack cooldown
Movement speed
Detection range
\`\`\`

Namun perubahan sebaiknya memiliki batas aman.

---

# 59. Hal yang Tidak Direkomendasikan

Jangan membuat adaptasi:

\`\`\`text
Intensity naik
→ enemy damage menjadi 10x
\`\`\`

atau:

\`\`\`text
player bermain bagus
→ enemy muncul tepat di belakang player
\`\`\`

Hal tersebut berisiko menghasilkan game yang terasa menghukum player.

Adaptive gameplay sebaiknya mempertahankan:

\`\`\`text
Challenge
+
Fairness
+
Counterplay
\`\`\`

---

# 60. Debugging AI

Jika Enemy tidak bergerak, periksa:

\`\`\`text
[ ] NavMesh sudah di-bake
[ ] Enemy berada pada NavMesh
[ ] NavMeshAgent aktif
[ ] Destination berada pada NavMesh
[ ] Agent tidak isStopped
[ ] Obstacle tidak menutup semua jalan
\`\`\`

---

# 61. Enemy Tidak Melihat Player

Periksa:

\`\`\`text
[ ] Player menggunakan Tag Player
[ ] View Distance cukup besar
[ ] View Angle sesuai
[ ] Obstacle Mask benar
[ ] Enemy menghadap player
[ ] Wall tidak salah layer
\`\`\`

---

# 62. Enemy Melihat Menembus Wall

Kemungkinan:

\`\`\`text
Wall tidak berada pada Layer Obstacle
\`\`\`

Pastikan:

\`\`\`text
Wall
Layer = Obstacle
\`\`\`

dan:

\`\`\`text
EnemyAI
Obstacle Mask = Obstacle
\`\`\`

---

# 63. Enemy Tidak Menyerang

Periksa:

\`\`\`text
Attack Range
PlayerHealth
player reference
attack cooldown
distance
\`\`\`

Aktifkan:

\`\`\`text
F1
\`\`\`

dan lihat apakah state berubah menjadi:

\`\`\`text
ATTACK
\`\`\`

---

# 64. Director Tidak Berubah

Periksa AI Debug Mode.

Lihat:

\`\`\`text
Health
Kill Rate
Enemy Count
Intensity
Last Decision
\`\`\`

Misalnya kill rate tidak pernah mencapai threshold:

\`\`\`text
4 kills/minute
\`\`\`

maka rule Player Dominant memang tidak akan aktif.

---

# 65. Director Spawn Terlalu Cepat

Atur:

\`\`\`text
Minimum Spawn Interval
Maximum Spawn Interval
Maximum Enemies
Intensity Change Speed
\`\`\`

Contoh lebih aman:

\`\`\`text
Min Interval = 2
Max Interval = 7
Max Enemies  = 6
\`\`\`

---

# 66. Debug Logging

Contoh log:

\`\`\`text
[Enemy01] State: Patrol -> Chase

[Enemy01] State: Chase -> Attack

[Director]
Increase pressure: player dominant
Health=0.88
KPM=5.20
Enemies=3
Intensity=0.70
\`\`\`

Log sangat membantu untuk mencari sebab suatu keputusan.

Namun hindari:

\`\`\`csharp
Debug.Log(...)
\`\`\`

setiap frame.

Lebih baik log hanya ketika terjadi perubahan state atau evaluasi berkala.

---

# 67. Parameter Tuning

Parameter awal bukan nilai mutlak.

Mahasiswa harus melakukan tuning.

Contoh:

| Parameter | Awal |
|---|---:|
| Enemy speed | 3.5 |
| View distance | 12 |
| View angle | 120 |
| Attack range | 2 |
| Attack cooldown | 1 |
| Player health | 100 |
| Enemy health | 50 |
| Player damage | 25 |
| Min enemies | 2 |
| Max enemies | 8 |
| Min spawn interval | 1.5 |
| Max spawn interval | 6 |
| Initial intensity | 0.3 |

Tujuan tuning adalah menemukan gameplay yang:

\`\`\`text
tidak terlalu mudah
dan
tidak terlalu sulit
\`\`\`

---

# 68. Evaluasi Correctness

Pertanyaan:

\`\`\`text
Apakah AI bekerja sesuai spesifikasi?
\`\`\`

Checklist:

\`\`\`text
[ ] Enemy patrol
[ ] Enemy mendeteksi player
[ ] Enemy chase
[ ] Enemy attack
[ ] Enemy tidak melihat melalui wall
[ ] NavMesh bekerja
[ ] Enemy dapat mati
[ ] Kill tercatat
[ ] Director membaca metrics
[ ] Director mengubah intensity
[ ] Spawner mengikuti intensity
\`\`\`

---

# 69. Evaluasi Robustness

Uji:

\`\`\`text
Player berada di sudut arena
Player bersembunyi
Banyak enemy aktif
Enemy spawn dari berbagai titik
Player mati
Timer selesai
Obstacle berada di antara enemy dan player
\`\`\`

AI harus tetap berada dalam state yang masuk akal.

---

# 70. Evaluasi Believability

Tanyakan:

\`\`\`text
Apakah enemy terlihat mengetahui terlalu banyak?
\`\`\`

Contoh AI lebih believable:

\`\`\`text
Enemy hanya chase setelah player terlihat.
\`\`\`

Contoh kurang believable:

\`\`\`text
Enemy selalu mengetahui posisi player
meskipun player berada di balik dinding.
\`\`\`

---

# 71. Evaluasi Fairness

Periksa:

\`\`\`text
Apakah enemy spawn terlalu dekat?
Apakah player memiliki waktu bereaksi?
Apakah attack cooldown masuk akal?
Apakah pressure turun saat player kesulitan?
Apakah Director menaikkan difficulty secara bertahap?
\`\`\`

---

# 72. Evaluasi Responsiveness

Catat waktu respons AI.

Contoh:

\`\`\`text
Player terlihat
→ Enemy Chase

Player mendekat
→ Enemy Attack

Health player turun
→ Director menurunkan intensity
\`\`\`

Director tidak perlu melakukan evaluasi setiap frame.

Pada praktikum:

\`\`\`text
Evaluation Interval = 2 detik
\`\`\`

cukup untuk memperlihatkan sistem adaptif tanpa membuat keputusan terlalu sering.

---

# 73. Evaluasi Explainability

Mahasiswa harus dapat menjelaskan contoh:

\`\`\`text
Mengapa intensity sekarang 0.7?
\`\`\`

Jawaban yang diharapkan:

\`\`\`text
Health player 85%.
Kill rate 5.2 per minute.
Enemy yang aktif hanya tiga.

Karena kondisi Player Dominant terpenuhi,
AI Director meningkatkan intensity dari
0.6 menjadi 0.7.
\`\`\`

Bukan:

\`\`\`text
"Karena script-nya seperti itu."
\`\`\`

---

# 74. Evaluasi Player Experience

Minta beberapa mahasiswa lain mencoba game.

Catat:

\`\`\`text
Apakah game terlalu mudah?
Apakah game terlalu sulit?
Apakah perubahan difficulty terasa?
Apakah spawn terasa fair?
Apakah enemy terlalu cepat?
Apakah arena terlalu sempit?
\`\`\`

---

# 75. Tabel Hasil Pengujian

Gunakan tabel seperti berikut:

| Test | Kondisi | Expected | Actual | Status |
|---|---|---|---|---|
| T01 | Player masuk FOV | Enemy Chase | Chase | Pass |
| T02 | Player di balik wall | Tidak terdeteksi | Tidak terdeteksi | Pass |
| T03 | Distance < attack range | Attack | Attack | Pass |
| T04 | Health < 30% | Intensity turun | 0.6 → 0.5 | Pass |
| T05 | Player dominan | Intensity naik | 0.5 → 0.6 | Pass |
| T06 | Enemy >= limit | Tidak spawn | Tidak spawn | Pass |

---

# 76. Pengujian Adaptive System

Lakukan minimal tiga sesi.

## Sesi 1 — Sengaja bermain buruk

Tujuan:

\`\`\`text
Health rendah
Kill rate rendah
\`\`\`

Catat:

\`\`\`text
Average Intensity
Max Enemy Count
Result
\`\`\`

---

## Sesi 2 — Bermain normal

Catat parameter yang sama.

---

## Sesi 3 — Bermain agresif

Usahakan kill rate tinggi.

Bandingkan.

Contoh hasil:

| Mode Bermain | Avg Intensity | Max Enemy | Hasil |
|---|---:|---:|---|
| Kesulitan | 0.32 | 4 | Survive |
| Normal | 0.51 | 5 | Survive |
| Dominan | 0.75 | 7 | Survive |

Jika nilai benar-benar berubah sesuai performa, adaptive system dapat diamati secara kuantitatif.

---

# 77. Diagram Arsitektur untuk Laporan

Gunakan diagram:

\`\`\`text
                    ┌──────────────────┐
                    │      Player      │
                    └────────┬─────────┘
                             │
                             ↓
                    ┌──────────────────┐
                    │ Player Metrics   │
                    │ Health           │
                    │ Kill Rate        │
                    └────────┬─────────┘
                             │
                             ↓
                    ┌──────────────────┐
                    │   AI Director    │
                    │ Intensity Model  │
                    └────────┬─────────┘
                             │
               ┌─────────────┴────────────┐
               ↓                          ↓
      ┌─────────────────┐       ┌─────────────────┐
      │ Target Enemy    │       │ Spawn Interval  │
      │ Count           │       │                 │
      └────────┬────────┘       └────────┬────────┘
               └─────────────┬────────────┘
                             ↓
                    ┌──────────────────┐
                    │  Enemy Spawner   │
                    └────────┬─────────┘
                             ↓
                    ┌──────────────────┐
                    │     Enemy AI     │
                    │ Patrol           │
                    │ Chase            │
                    │ Attack           │
                    └────────┬─────────┘
                             ↓
                    ┌──────────────────┐
                    │    Gameplay      │
                    └──────────────────┘
\`\`\`

---

# 78. Diagram Enemy AI

\`\`\`text
              ┌─────────────┐
              │   PATROL    │
              └──────┬──────┘
                     │
                player seen
                     │
                     ↓
              ┌─────────────┐
        ┌────→│    CHASE    │←────┐
        │     └──────┬──────┘     │
        │            │            │
 player │       attack range      │ target
 lost   │            │            │ too far
        │            ↓            │
        │     ┌─────────────┐     │
        └─────│   ATTACK    │─────┘
              └─────────────┘
\`\`\`

---

# 79. AI Debug Requirement

Saat F1 ditekan minimal tampilkan:

\`\`\`text
GLOBAL AI
-------------------------
Health
Kill Count
Kill Rate
Enemy Count
Intensity
Director Decision
\`\`\`

dan untuk enemy:

\`\`\`text
PATROL
CHASE
ATTACK
\`\`\`

Jika dikembangkan menjadi final project, tambahkan:

\`\`\`text
Target
Path
FOV
Last Known Position
Utility Score
Difficulty Multiplier
PCG Seed
\`\`\`

sesuai teknik AI yang digunakan kelompok.

---

# 80. Optimasi untuk Final Project

Jika jumlah enemy semakin banyak, hindari perhitungan mahal setiap frame.

Contoh yang dapat dikembangkan:

\`\`\`text
Perception interval
Object pooling
Path update interval
Cached component references
Maximum active agent
Spatial filtering
\`\`\`

Jangan melakukan optimasi kompleks sebelum terdapat masalah yang dapat diukur.

Prioritas:

\`\`\`text
Correctness
→ Stability
→ Profiling
→ Optimization
\`\`\`

---

# 81. Object Pooling sebagai Pengembangan

Pada contoh dasar digunakan:

\`\`\`csharp
Instantiate()
Destroy()
\`\`\`

karena lebih mudah dipahami.

Pada final project dengan spawn enemy sering, mahasiswa dapat mengubah sistem menjadi:

\`\`\`text
Object Pool

Get Enemy
   ↓
Activate
   ↓
Enemy mati
   ↓
Reset
   ↓
Return to Pool
\`\`\`

Sehingga GameObject dapat digunakan kembali.

---

# 82. Rekomendasi Polishing

Setelah AI stabil, mahasiswa dapat menambahkan:

\`\`\`text
Character models
Animations
Particle effects
Hit effects
Sound effects
Background music
Health bar
Crosshair
Objective UI
Environment assets
Lighting
Post-processing
\`\`\`

Tetapi jangan mendahulukan polish jika AI utama belum berjalan.

Urutan prioritas:

\`\`\`text
1. Playable
2. AI working
3. AI integration
4. Debugging
5. Evaluation
6. Polish
\`\`\`

---

# 83. Pengembangan ke Final Project

Praktikum ini dapat dijadikan kerangka final project.

Contohnya:

## Stealth Infiltration

Ganti arena combat menjadi:

\`\`\`text
Guard Patrol
FOV
Memory
Search
Shared Alert
AI Director Alarm Level
\`\`\`

---

## Tactical Shooter

Tambahkan:

\`\`\`text
Cover
Flanking
Utility AI
Squad roles
AI Director
\`\`\`

---

## Procedural Dungeon

Tambahkan:

\`\`\`text
PCG
Seed
Dungeon validation
Adaptive spawning
Adaptive loot
Enemy AI
\`\`\`

---

## Robot Arena

Tambahkan:

\`\`\`text
Utility AI
Take Cover
Attack
Flee
Player Modeling
Adaptive Difficulty
AI Director
\`\`\`

---

# 84. Minimum Requirement untuk Tugas Praktikum

Mahasiswa harus mengumpulkan project yang memiliki:

\`\`\`text
[ ] Player playable
[ ] Arena navigable
[ ] Minimal 3 enemy aktif
[ ] Perception
[ ] FSM
[ ] NavMesh
[ ] Combat
[ ] EnemySpawner
[ ] AI Director
[ ] Adaptive intensity
[ ] Win condition
[ ] Lose condition
[ ] AI Debug Mode
[ ] Minimal 5 skenario pengujian
[ ] Dokumentasi arsitektur AI
\`\`\`

---

# 85. Requirement Tambahan yang Direkomendasikan

Untuk nilai lebih tinggi:

\`\`\`text
[ ] Health pickup adaptif
[ ] Enemy type berbeda
[ ] Tactical positioning
[ ] Player model
[ ] Object pooling
[ ] World-space AI state
[ ] Path visualization
[ ] Better UI
[ ] Animation
[ ] Audio/VFX
\`\`\`

---

# 86. Tugas Analisis

Mahasiswa harus menjawab:

1. Apa perbedaan Enemy AI dan AI Director pada project?
2. Apa input AI Director?
3. Apa output AI Director?
4. Mengapa player health dinormalisasi?
5. Mengapa intensity dibatasi 0–1?
6. Apa fungsi evaluation interval?
7. Mengapa spawn point diberi minimum distance?
8. Apa fungsi hard enemy limit?
9. Bagaimana sistem menjaga fairness?
10. Bagaimana membuktikan adaptive system benar-benar bekerja?
11. Apa fungsi AI Debug Mode?
12. Apa kelemahan AI Director berbasis rule ini?
13. Apa yang akan terjadi jika threshold terlalu sensitif?
14. Parameter apa yang paling memengaruhi gameplay?
15. Bagaimana sistem ini dikembangkan untuk final project?

---

# 87. Tantangan Pengembangan 1 — Adaptive Health Pickup

Tambahkan:

\`\`\`text
Health < 25%
        ↓
Director mengecek recovery cooldown
        ↓
Spawn health pickup
\`\`\`

Namun beri cooldown agar health item tidak muncul terus-menerus.

---

# 88. Tantangan Pengembangan 2 — Enemy Type

Tambahkan:

\`\`\`text
MeleeEnemy
FastEnemy
TankEnemy
\`\`\`

Director menentukan komposisi berdasarkan intensity.

Contoh:

\`\`\`text
Low:
80% melee
20% fast

Medium:
60% melee
30% fast
10% tank

High:
40% melee
40% fast
20% tank
\`\`\`

Ini membuat intensity memengaruhi **jenis encounter**, bukan hanya jumlah enemy.

---

# 89. Tantangan Pengembangan 3 — Tension Curve

Tambahkan konsep target tension:

\`\`\`text
0–30 sec  → Low
30–60 sec → Medium
60–90 sec → High
90–110    → Recovery
110–150   → High
150–180   → Final Peak
\`\`\`

Lalu adaptive system mengombinasikan:

\`\`\`text
Target Tension
+
Player Performance
\`\`\`

Ini lebih dekat dengan konsep AI Director yang mengatur pacing.

---

# 90. Tantangan Pengembangan 4 — Last Known Position

Enemy saat kehilangan player jangan langsung Patrol.

Tambahkan:

\`\`\`text
PATROL
CHASE
ATTACK
SEARCH
\`\`\`

Alur:

\`\`\`text
Player terlihat
→ simpan lastKnownPosition

Player hilang
→ Search lastKnownPosition

Tidak menemukan player
→ Patrol
\`\`\`

Ini mengintegrasikan materi perception + memory.

---

# 91. Tantangan Pengembangan 5 — Tactical Enemy

Tambahkan state:

\`\`\`text
TakeCover
Flank
Retreat
\`\`\`

Gunakan:

\`\`\`text
Health
distance
cover availability
director intensity
\`\`\`

untuk menentukan aksi.

Dapat menggunakan FSM, Behavior Tree, atau Utility AI sesuai final project.

---

# 92. Tantangan Pengembangan 6 — Player Modeling

Tambahkan statistik:

\`\`\`text
Aggression
Accuracy
Kill Rate
Damage Taken
Movement Activity
\`\`\`

Kemudian klasifikasikan:

\`\`\`text
Aggressive
Defensive
Struggling
Balanced
\`\`\`

AI Director dapat memilih encounter berdasarkan profile.

---

# 93. Rubrik Penilaian Praktikum

| Komponen | Bobot |
|---|---:|
| Scene dan gameplay playable | 10% |
| Enemy Perception | 10% |
| FSM Enemy AI | 15% |
| Navigation/NavMesh | 10% |
| EnemySpawner | 10% |
| AI Director | 20% |
| Adaptive behavior | 10% |
| AI Debug Mode | 10% |
| Pengujian/evaluasi | 5% |
| **Total** | **100%** |

---

# 94. Kriteria Penilaian AI Director

### Sangat Baik

\`\`\`text
Director membaca beberapa metrics,
adaptasi terlihat,
perubahan gradual,
fair,
debug jelas,
dan berdampak pada gameplay.
\`\`\`

### Baik

\`\`\`text
Director bekerja dan adaptive spawning terlihat,
tetapi evaluasi atau tuning masih sederhana.
\`\`\`

### Cukup

\`\`\`text
Director hanya mengubah satu parameter
dan pengaruh gameplay kurang jelas.
\`\`\`

### Kurang

\`\`\`text
Spawner sebenarnya statis
atau keputusan Director tidak dapat dibuktikan.
\`\`\`

---

# 95. Checklist Sebelum Praktikum Dinyatakan Selesai

## Player

\`\`\`text
[ ] Bisa bergerak
[ ] Bisa berputar
[ ] Bisa menyerang
[ ] Memiliki health
[ ] Bisa mati
\`\`\`

## Enemy

\`\`\`text
[ ] Patrol
[ ] Detect player
[ ] Chase
[ ] Attack
[ ] Bisa menerima damage
[ ] Bisa mati
\`\`\`

## Navigation

\`\`\`text
[ ] NavMesh baked
[ ] Enemy tidak menembus wall
[ ] Enemy dapat mencapai player
\`\`\`

## Director

\`\`\`text
[ ] Membaca player health
[ ] Membaca kill rate
[ ] Membaca enemy count
[ ] Mengubah intensity
[ ] Mengubah target enemy
[ ] Mengubah spawn interval
\`\`\`

## Game

\`\`\`text
[ ] Timer berjalan
[ ] Kill count berjalan
[ ] Win condition
[ ] Lose condition
[ ] Restart
\`\`\`

## Debug

\`\`\`text
[ ] F1 aktif/nonaktif
[ ] Intensity terlihat
[ ] Director decision terlihat
[ ] Enemy state terlihat
[ ] Log perubahan state tersedia
\`\`\`

---

# 96. Kesalahan Umum

## Kesalahan 1

\`\`\`text
Enemy tidak bergerak.
\`\`\`

Penyebab umum:

\`\`\`text
NavMesh belum bake.
\`\`\`

---

## Kesalahan 2

\`\`\`text
Player tidak ditemukan.
\`\`\`

Periksa:

\`\`\`text
Tag Player.
\`\`\`

---

## Kesalahan 3

\`\`\`text
Enemy melihat melalui wall.
\`\`\`

Periksa:

\`\`\`text
Obstacle Layer
Obstacle Mask
\`\`\`

---

## Kesalahan 4

\`\`\`text
Director selalu intensity tinggi.
\`\`\`

Periksa threshold dan \`intensityChangeSpeed\`.

---

## Kesalahan 5

\`\`\`text
Enemy spawn terus.
\`\`\`

Periksa:

\`\`\`text
targetEnemyCount
hardEnemyLimit
spawnTimer
\`\`\`

---

## Kesalahan 6

\`\`\`text
Game terlalu sulit.
\`\`\`

Jangan langsung mengubah banyak parameter.

Ubah satu per satu:

\`\`\`text
Enemy speed
→ spawn interval
→ enemy count
→ attack damage
\`\`\`

kemudian uji kembali.

---

# 97. Prinsip Penting Praktikum

## Prinsip 1

\`\`\`text
AI yang kompleks tidak selalu lebih baik.
\`\`\`

AI sederhana tetapi terintegrasi dengan gameplay dapat menghasilkan sistem yang lebih baik daripada algoritma kompleks yang tidak selesai.

---

## Prinsip 2

\`\`\`text
AI harus observable.
\`\`\`

Mahasiswa harus dapat melihat:

\`\`\`text
State
Input
Decision
Output
\`\`\`

---

## Prinsip 3

\`\`\`text
AI harus explainable.
\`\`\`

Mahasiswa harus dapat menjawab:

\`\`\`text
Mengapa AI melakukan tindakan tersebut?
\`\`\`

---

## Prinsip 4

\`\`\`text
Adaptation harus memiliki tujuan.
\`\`\`

Bukan:

\`\`\`text
Player bagus → hukum player.
\`\`\`

Tetapi:

\`\`\`text
Player dominan
→ tingkatkan challenge secara wajar
→ pertahankan engagement.
\`\`\`

---

## Prinsip 5

\`\`\`text
Constraint tetap diperlukan.
\`\`\`

Adaptive AI harus memiliki:

\`\`\`text
Minimum
Maximum
Cooldown
Threshold
Hard Limit
Fallback
\`\`\`

---

# 98. Rekomendasi Implementasi Terbaik

Untuk Praktikum 15, implementasi yang paling direkomendasikan adalah:

\`\`\`text
3D Survival Arena
+
Enemy FSM
+
Perception
+
NavMesh
+
AI Director
+
Adaptive Enemy Spawning
+
AI Debug Mode
\`\`\`

### Alasannya

**Pertama — scope realistis.**

Arena kecil dapat selesai dalam satu sesi pengembangan dibandingkan membuat dungeon besar atau stealth game lengkap.

**Kedua — seluruh AI terlihat.**

Dosen dan mahasiswa dapat melihat:

\`\`\`text
Patrol
Chase
Attack
Spawning
Intensity
Adaptive decision
\`\`\`

secara langsung.

**Ketiga — mudah diuji.**

Kondisi player dapat dimanipulasi untuk menunjukkan keputusan AI Director.

**Keempat — mudah dikembangkan.**

Struktur project dapat dikembangkan menjadi:

\`\`\`text
Utility AI
Behavior Tree
PCG
DDA
Player Modeling
ML-Agents
Tactical AI
\`\`\`

**Kelima — sangat sesuai untuk persiapan final project.**

Praktikum mengajarkan bahwa sistem final bukan sekadar kumpulan script, melainkan:

\`\`\`text
Input
↓
AI Decision
↓
Gameplay Action
↓
Feedback
↓
Evaluation
\`\`\`

---

# 99. Scope yang Direkomendasikan

Untuk praktikum:

\`\`\`text
1 arena
1 player
1 enemy prefab
3 enemy states
4–6 spawn points
1 AI Director
1 adaptive parameter model
1 debug mode
1 win condition
1 lose condition
\`\`\`

Jangan menambah terlalu banyak fitur sebelum sistem tersebut stabil.

---

# 100. Pengembangan Menuju UAS

Setelah praktikum ini berhasil, kelompok dapat menggunakan kerangka yang sama untuk final project.

Target akhir:

\`\`\`text
Playable Vertical Slice
        +
AI Integrated
        +
Advanced Feature
        +
Debug Mode
        +
Evaluation
        +
Polish
\`\`\`

Contoh final architecture:

\`\`\`text
                    GAME
                      │
          ┌───────────┴───────────┐
          ↓                       ↓
       Player                  AI World
                                  │
                 ┌────────────────┼───────────────┐
                 ↓                ↓               ↓
             Enemy AI        AI Director       PCG/DDA
                 │                │               │
           ┌─────┼─────┐          │               │
           ↓     ↓     ↓          ↓               ↓
      Perception FSM NavMesh   Intensity       Content
           │     │     │          │               │
           └─────┴─────┴──────────┴───────────────┘
                              │
                              ↓
                         GAMEPLAY
                              │
                              ↓
                         EVALUATION
\`\`\`

---

# 101. Kesimpulan

Praktikum 15 menunjukkan perubahan cara berpikir dari:

\`\`\`text
"Membuat script AI"
\`\`\`

menjadi:

\`\`\`text
"Membuat sistem Game AI."
\`\`\`

Sistem yang dibuat mengintegrasikan:

\`\`\`text
Perception
+
Decision Making
+
Navigation
+
Combat
+
Performance Tracking
+
Adaptive System
+
AI Director
+
Debugging
+
Evaluation
\`\`\`

Enemy AI menangani keputusan pada tingkat individu, sedangkan AI Director menangani kondisi gameplay secara global.

Adaptive system menggunakan feedback dari performa player untuk menyesuaikan intensitas permainan.

AI Debug Mode memungkinkan mahasiswa melihat dan menjelaskan kondisi internal AI.

Dengan demikian project tidak hanya:

\`\`\`text
AI berjalan
\`\`\`

tetapi dapat menjawab:

\`\`\`text
Apa yang AI lihat?
Apa yang AI putuskan?
Mengapa keputusan itu dibuat?
Apa pengaruhnya terhadap gameplay?
Apakah keputusan tersebut fair?
Bagaimana sistem tersebut diuji?
\`\`\`

---

# 102. Rekomendasi Akhir

## Nama Project

\`\`\`text
GC15_AdaptiveArenaDirector
\`\`\`

## Judul Praktikum

\`\`\`text
Adaptive Arena Director:
Integrasi Advanced Game AI untuk Final Project
\`\`\`

## Teknik Utama

\`\`\`text
Enemy FSM
Perception
NavMesh Navigation
Adaptive Enemy Spawning
AI Director
AI Debug Mode
\`\`\`

## Fitur Advanced Utama

\`\`\`text
AI Director berbasis Player Performance
\`\`\`

## Alasan Pemilihan

Kombinasi tersebut memberikan keseimbangan terbaik antara:

\`\`\`text
kompleksitas implementasi
+
cakupan materi
+
keterlihatan AI saat demo
+
kemudahan debugging
+
kemudahan evaluasi
+
potensi pengembangan Final Project
\`\`\`

Pesan utama praktikum:

> **Game AI yang baik bukan AI dengan algoritma paling banyak, melainkan AI yang terintegrasi dengan gameplay, dapat diamati, dapat dijelaskan, dapat diuji, dan memberikan pengalaman bermain yang lebih baik.**`,_c=`# MODUL PRAKTIKUM 2  
## NPC Guard: Sensor + Memory + Decision

**Mata Kuliah:** Game Cerdas  
**Program:** S1 Teknik Informatika – Semester 7  
**Game Engine:** Unity 6  
**Bahasa Pemrograman:** C#  

---

# 1. Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat **NPC Guard** sederhana yang dapat:

1. Berjalan patroli mengikuti beberapa waypoint.
2. Mendeteksi Player berdasarkan:
   - jarak,
   - Field of View,
   - Line of Sight.
3. Mengejar Player ketika Player terlihat.
4. Mengingat posisi terakhir Player.
5. Menuju posisi terakhir tersebut ketika Player menghilang.
6. Mencari Player selama beberapa detik.
7. Kembali patroli jika Player tidak ditemukan.
8. Kembali mengejar jika Player muncul lagi.
9. Menampilkan visual debugging melalui Gizmos.
10. Mengubah parameter AI melalui Inspector.

Arsitektur yang digunakan mengikuti konsep Pertemuan 2:

\`\`\`text
Environment
     ↓
Perception
     ↓
Memory
     ↓
Decision
     ↓
Action
     ↓
Environment
\`\`\`

Target perilaku akhir:

\`\`\`text
PATROL
   │
   │ Player terlihat
   ▼
CHASE
   │
   │ Player menghilang
   ▼
SEARCH
   │
   ├──────── Player ditemukan ────────→ CHASE
   │
   │ Tidak ditemukan
   ▼
PATROL
\`\`\`

Materi ini mengimplementasikan secara langsung konsep **Sensor, Memory, Decision, Action, parameter AI, dan debugging Gizmos** dari Pertemuan 2.

---

# 2. Hasil Akhir Praktikum

Scene akhir akan memiliki struktur seperti berikut:

\`\`\`text
Scene
│
├── Ground
├── Navigation
│   └── NavMesh Surface
│
├── Player
│   └── PlayerController
│
├── NPC_Guard
│   ├── NavMeshAgent
│   ├── NPCSensor
│   ├── NPCBrain
│   └── DirectionMarker
│
├── Wall
│
└── PatrolPoints
    ├── Point1
    ├── Point2
    ├── Point3
    └── Point4
\`\`\`

Player dapat dikontrol menggunakan:

\`\`\`text
W / ↑ = maju
S / ↓ = mundur
A / ← = kiri
D / → = kanan
\`\`\`

NPC kemudian bereaksi terhadap pergerakan Player.

---

# 3. Membuat Project Unity 6

Buka **Unity Hub**.

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D
\`\`\`

Nama project:

\`\`\`text
GameCerdas_Praktikum02
\`\`\`

Klik:

\`\`\`text
Create Project
\`\`\`

---

# 4. Membuat Struktur Folder

Di dalam jendela Project, buat folder:

\`\`\`text
Assets
│
├── Materials
├── Prefabs
├── Scenes
└── Scripts
\`\`\`

Tujuannya agar asset project lebih terorganisir.

---

# 5. Menyimpan Scene

Pilih:

\`\`\`text
File
→ Save As
\`\`\`

Simpan di:

\`\`\`text
Assets/Scenes
\`\`\`

dengan nama:

\`\`\`text
Praktikum02_NPCGuard
\`\`\`

---

# 6. Membuat Ground

Pada Hierarchy pilih:

\`\`\`text
GameObject
→ 3D Object
→ Plane
\`\`\`

Rename:

\`\`\`text
Ground
\`\`\`

Atur Transform:

\`\`\`text
Position
X = 0
Y = 0
Z = 0

Scale
X = 3
Y = 1
Z = 3
\`\`\`

Arena sekarang memiliki ukuran yang cukup luas untuk pengujian.

---

# 7. Membuat Material Ground

Masuk:

\`\`\`text
Assets/Materials
\`\`\`

Klik kanan:

\`\`\`text
Create
→ Material
\`\`\`

Nama:

\`\`\`text
GroundMaterial
\`\`\`

Pilih warna yang mudah dibedakan.

Drag material tersebut ke object:

\`\`\`text
Ground
\`\`\`

---

# 8. Membuat Player

Pada Hierarchy:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Atur:

\`\`\`text
Position
X = 0
Y = 1
Z = 0
\`\`\`

Capsule akan menjadi karakter Player sederhana.

---

# 9. Memberikan Material Player

Buat material:

\`\`\`text
PlayerMaterial
\`\`\`

Misalnya gunakan warna biru.

Drag material tersebut ke:

\`\`\`text
Player
\`\`\`

Tujuannya agar Player mudah dibedakan dari NPC.

---

# 10. Membuat Tag Player

Pilih object:

\`\`\`text
Player
\`\`\`

Pada Inspector:

\`\`\`text
Tag
→ Add Tag
\`\`\`

Tambahkan:

\`\`\`text
Player
\`\`\`

Kembali ke object Player.

Set:

\`\`\`text
Tag = Player
\`\`\`

---

# 11. Membuat Layer Player dan Obstacle

Pilih:

\`\`\`text
Layer
→ Add Layer
\`\`\`

Tambahkan dua Layer:

\`\`\`text
Player
Obstacle
\`\`\`

Kemudian pilih Player.

Set:

\`\`\`text
Layer = Player
\`\`\`

Layer Obstacle akan digunakan untuk Wall.

---

# 12. Membuat Player Controller

Sekarang Player akan dibuat dapat bergerak menggunakan:

\`\`\`text
WASD
\`\`\`

dan:

\`\`\`text
Arrow Key
\`\`\`

Masuk ke:

\`\`\`text
Assets/Scripts
\`\`\`

Klik kanan:

\`\`\`text
Create
→ Scripting
→ MonoBehaviour Script
\`\`\`

Nama:

\`\`\`text
PlayerController
\`\`\`

Buka script tersebut.

Hapus seluruh isinya dan gunakan kode berikut.

\`\`\`csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(
            horizontal,
            0f,
            vertical
        ).normalized;

        // Gerakkan Player
        transform.position +=
            movement *
            moveSpeed *
            Time.deltaTime;

        // Putar Player mengikuti arah gerakan
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }
    }
}
\`\`\`

---

# 13. Memahami Player Controller

Bagian:

\`\`\`csharp
Input.GetAxisRaw("Horizontal");
\`\`\`

membaca:

\`\`\`text
A / D
← / →
\`\`\`

Sedangkan:

\`\`\`csharp
Input.GetAxisRaw("Vertical");
\`\`\`

membaca:

\`\`\`text
W / S
↑ / ↓
\`\`\`

Arah gerakan:

\`\`\`text
                 +Z
                  ↑
                W / ↑
                  │
                  │

      A / ← ─── PLAYER ─── D / →

                  │
                  │
                S / ↓
                  ↓
                 -Z
\`\`\`

---

# 14. Fungsi normalized

Perhatikan:

\`\`\`csharp
Vector3 movement =
    new Vector3(
        horizontal,
        0f,
        vertical
    ).normalized;
\`\`\`

\`.normalized\` digunakan agar kecepatan diagonal tidak lebih tinggi.

Tanpa normalisasi:

\`\`\`text
W + D
\`\`\`

dapat menghasilkan kecepatan lebih tinggi dibanding hanya:

\`\`\`text
W
\`\`\`

Dengan \`.normalized\`, kecepatan tetap konsisten.

---

# 15. Memasang PlayerController

Pilih:

\`\`\`text
Player
\`\`\`

Drag:

\`\`\`text
PlayerController.cs
\`\`\`

ke Inspector Player.

Atau:

\`\`\`text
Add Component
→ PlayerController
\`\`\`

Set:

\`\`\`text
Move Speed = 5
Rotation Speed = 10
\`\`\`

---

# 16. Konfigurasi Input Unity

Kode praktikum menggunakan:

\`\`\`csharp
Input.GetAxisRaw()
\`\`\`

Jika Unity menampilkan error bahwa project hanya menggunakan Input System baru, buka:

\`\`\`text
Edit
→ Project Settings
→ Player
\`\`\`

Cari:

\`\`\`text
Active Input Handling
\`\`\`

Pilih:

\`\`\`text
Both
\`\`\`

Jika Unity meminta restart Editor, lakukan restart.

---

# 17. Testing Player

Tekan:

\`\`\`text
Play
\`\`\`

Klik jendela Game.

Coba:

\`\`\`text
W
A
S
D
\`\`\`

Kemudian:

\`\`\`text
↑
←
↓
→
\`\`\`

Player harus dapat bergerak.

Player juga harus berputar mengikuti arah pergerakan.

Stop Play Mode setelah pengujian selesai.

---

# 18. Membuat Wall

Pada Hierarchy:

\`\`\`text
GameObject
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
Wall
\`\`\`

Contoh Transform:

\`\`\`text
Position
X = 0
Y = 1.5
Z = 3

Scale
X = 6
Y = 3
Z = 0.5
\`\`\`

Set Layer:

\`\`\`text
Obstacle
\`\`\`

Wall akan digunakan untuk menghalangi pandangan NPC.

---

# 19. Membuat Material Wall

Buat:

\`\`\`text
WallMaterial
\`\`\`

Misalnya gunakan warna abu-abu.

Drag ke object:

\`\`\`text
Wall
\`\`\`

---

# 20. Membuat NPC Guard

Pilih:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
NPC_Guard
\`\`\`

Contoh posisi:

\`\`\`text
X = -6
Y = 1
Z = -6
\`\`\`

---

# 21. Membuat Material NPC

Buat material:

\`\`\`text
NPCMaterial
\`\`\`

Misalnya gunakan warna merah.

Drag ke:

\`\`\`text
NPC_Guard
\`\`\`

Sekarang:

\`\`\`text
Player = biru
NPC = merah
\`\`\`

---

# 22. Membuat Direction Marker

Capsule sulit menunjukkan arah hadap.

Kita akan membuat penanda arah.

Klik kanan:

\`\`\`text
NPC_Guard
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
DirectionMarker
\`\`\`

Atur local position:

\`\`\`text
X = 0
Y = 0.5
Z = 0.65
\`\`\`

Scale:

\`\`\`text
X = 0.2
Y = 0.2
Z = 0.5
\`\`\`

DirectionMarker menunjukkan arah:

\`\`\`csharp
transform.forward
\`\`\`

NPC.

---

# 23. Instalasi AI Navigation

Buka:

\`\`\`text
Window
→ Package Manager
\`\`\`

Cari package:

\`\`\`text
AI Navigation
\`\`\`

Install package tersebut.

Package ini menyediakan:

\`\`\`text
NavMesh Surface
NavMesh Link
NavMesh Modifier
\`\`\`

yang diperlukan untuk navigasi NPC.

---

# 24. Membuat Navigation Object

Pada Hierarchy:

\`\`\`text
GameObject
→ Create Empty
\`\`\`

Rename:

\`\`\`text
Navigation
\`\`\`

Pilih:

\`\`\`text
Add Component
\`\`\`

Cari:

\`\`\`text
NavMesh Surface
\`\`\`

Tambahkan component tersebut.

---

# 25. Bake NavMesh

Pilih object:

\`\`\`text
Navigation
\`\`\`

Pada komponen:

\`\`\`text
NavMesh Surface
\`\`\`

lakukan Bake NavMesh menggunakan kontrol baking pada komponen tersebut.

Setelah proses selesai, Ground akan menjadi area navigasi NPC.

Pastikan NPC berada di atas area NavMesh.

---

# 26. Menambahkan NavMeshAgent

Pilih:

\`\`\`text
NPC_Guard
\`\`\`

Klik:

\`\`\`text
Add Component
\`\`\`

Cari:

\`\`\`text
NavMesh Agent
\`\`\`

Gunakan parameter awal:

\`\`\`text
Speed = 3
Angular Speed = 360
Acceleration = 8
Stopping Distance = 0.3
\`\`\`

NPC sekarang dapat digerakkan menggunakan:

\`\`\`csharp
agent.SetDestination(...)
\`\`\`

---

# 27. Membuat Patrol Points

Buat Empty GameObject:

\`\`\`text
PatrolPoints
\`\`\`

Kemudian buat empat child Empty GameObject:

\`\`\`text
PatrolPoints
│
├── Point1
├── Point2
├── Point3
└── Point4
\`\`\`

Contoh posisi:

\`\`\`text
Point1
X = -8
Y = 0
Z = -8

Point2
X = 8
Y = 0
Z = -8

Point3
X = 8
Y = 0
Z = 8

Point4
X = -8
Y = 0
Z = 8
\`\`\`

Flow patrol:

\`\`\`text
Point1
   ↓
Point2
   ↓
Point3
   ↓
Point4
   ↓
Point1
\`\`\`

---

# 28. Arsitektur Script NPC

Pada praktikum ini digunakan dua script utama.

\`\`\`text
NPCSensor.cs
\`\`\`

bertanggung jawab terhadap:

\`\`\`text
PERCEPTION
\`\`\`

Sedangkan:

\`\`\`text
NPCBrain.cs
\`\`\`

menangani:

\`\`\`text
MEMORY
DECISION
ACTION
\`\`\`

Secara konseptual:

\`\`\`text
GAME WORLD
    │
    ▼
NPCSensor
    │
    ▼
Perception
    │
    ▼
NPCBrain
    │
    ├── Memory
    ├── Decision
    └── Action
          │
          ▼
     NavMeshAgent
\`\`\`

---

# 29. Membuat NPCSensor.cs

Masuk:

\`\`\`text
Assets/Scripts
\`\`\`

Buat script:

\`\`\`text
NPCSensor.cs
\`\`\`

Gunakan kode berikut.

\`\`\`csharp
using UnityEngine;

public class NPCSensor : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Vision Settings")]
    [SerializeField] private float viewRadius = 8f;

    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [SerializeField] private LayerMask obstacleMask;

    [Header("Eye Settings")]
    [SerializeField] private float eyeHeight = 1.2f;

    public bool CanSeePlayer { get; private set; }

    public Transform Player => player;

    private void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer()
    {
        CanSeePlayer = false;

        if (player == null)
            return;

        Vector3 directionToPlayer =
            player.position - transform.position;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        // =============================
        // STEP 1 : DISTANCE CHECK
        // =============================

        if (distanceToPlayer > viewRadius)
            return;

        Vector3 normalizedDirection =
            directionToPlayer.normalized;

        // =============================
        // STEP 2 : FIELD OF VIEW
        // =============================

        float angleToPlayer =
            Vector3.Angle(
                transform.forward,
                normalizedDirection
            );

        if (angleToPlayer > viewAngle / 2f)
            return;

        // =============================
        // STEP 3 : LINE OF SIGHT
        // =============================

        Vector3 eyePosition =
            transform.position +
            Vector3.up * eyeHeight;

        Vector3 targetPosition =
            player.position +
            Vector3.up * 0.5f;

        Vector3 rayDirection =
            targetPosition - eyePosition;

        float rayDistance =
            rayDirection.magnitude;

        if (Physics.Raycast(
            eyePosition,
            rayDirection.normalized,
            rayDistance,
            obstacleMask))
        {
            return;
        }

        // Semua pemeriksaan berhasil
        CanSeePlayer = true;
    }

    private void OnDrawGizmosSelected()
    {
        // =============================
        // VIEW RADIUS
        // =============================

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            viewRadius
        );

        // =============================
        // FIELD OF VIEW
        // =============================

        Vector3 leftBoundary =
            DirectionFromAngle(
                -viewAngle / 2f
            );

        Vector3 rightBoundary =
            DirectionFromAngle(
                viewAngle / 2f
            );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            leftBoundary * viewRadius
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            rightBoundary * viewRadius
        );

        // =============================
        // PLAYER VISIBLE
        // =============================

        if (player != null &&
            CanSeePlayer)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                transform.position +
                Vector3.up * eyeHeight,
                player.position +
                Vector3.up * 0.5f
            );
        }
    }

    private Vector3 DirectionFromAngle(
        float angle
    )
    {
        float finalAngle =
            transform.eulerAngles.y +
            angle;

        return new Vector3(
            Mathf.Sin(
                finalAngle *
                Mathf.Deg2Rad
            ),
            0f,
            Mathf.Cos(
                finalAngle *
                Mathf.Deg2Rad
            )
        );
    }
}
\`\`\`

---

# 30. Cara Kerja NPC Sensor

NPC melakukan tiga pemeriksaan.

## Pemeriksaan 1 — Radius

\`\`\`text
Apakah Player cukup dekat?
\`\`\`

Kode:

\`\`\`csharp
if (distanceToPlayer > viewRadius)
    return;
\`\`\`

Contoh:

\`\`\`text
View Radius = 8 meter
\`\`\`

Player pada jarak 12 meter:

\`\`\`text
Tidak terlihat
\`\`\`

---

# 31. Pemeriksaan 2 — Field of View

NPC kemudian memeriksa sudut.

Contoh:

\`\`\`text
View Angle = 90°
\`\`\`

maka area pandangan kira-kira:

\`\`\`text
             +45°
               /
              /
             /
NPC ───────→
             \\
              \\
               \\
             -45°
\`\`\`

Player di belakang NPC tidak terlihat meskipun jaraknya dekat.

---

# 32. Pemeriksaan 3 — Line of Sight

Terakhir NPC menggunakan:

\`\`\`csharp
Physics.Raycast()
\`\`\`

Situasi:

\`\`\`text
NPC ───────────── PLAYER
\`\`\`

Player terlihat.

Tetapi:

\`\`\`text
NPC ─── WALL ─── PLAYER
\`\`\`

Raycast mengenai Wall.

Player dianggap tidak terlihat.

---

# 33. Memasang NPCSensor

Pilih:

\`\`\`text
NPC_Guard
\`\`\`

Tambahkan:

\`\`\`text
NPCSensor
\`\`\`

Pada Inspector:

\`\`\`text
Player
→ drag GameObject Player

View Radius
→ 8

View Angle
→ 90

Obstacle Mask
→ Obstacle

Eye Height
→ 1.2
\`\`\`

---

# 34. Testing Sensor

Tekan:

\`\`\`text
Play
\`\`\`

Pilih NPC_Guard di Scene View.

Pastikan tombol:

\`\`\`text
Gizmos
\`\`\`

aktif.

Anda harus melihat:

\`\`\`text
Lingkaran kuning
= Detection Radius

Dua garis kuning
= Field of View

Garis merah
= Player terlihat
\`\`\`

---

# 35. Test Sensor 1

Gerakkan Player ke depan NPC.

Expected:

\`\`\`text
Player:
dalam radius
+
dalam FOV
+
tidak terhalang
\`\`\`

hasil:

\`\`\`text
CanSeePlayer = TRUE
\`\`\`

---

# 36. Test Sensor 2

Gerakkan Player ke belakang NPC.

Expected:

\`\`\`text
CanSeePlayer = FALSE
\`\`\`

meskipun Player masih dekat.

---

# 37. Test Sensor 3

Gerakkan Player ke belakang Wall.

\`\`\`text
NPC ─── WALL │ PLAYER
\`\`\`

Expected:

\`\`\`text
CanSeePlayer = FALSE
\`\`\`

---

# 38. Membuat NPCBrain.cs

Sekarang buat script:

\`\`\`text
NPCBrain.cs
\`\`\`

Gunakan kode berikut.

\`\`\`csharp
using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState
    {
        Patrol,
        Chase,
        Search
    }

    [Header("References")]
    [SerializeField]
    private NPCSensor sensor;

    [SerializeField]
    private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [SerializeField]
    private Transform[] patrolPoints;

    [SerializeField]
    private float waypointTolerance = 0.7f;

    [SerializeField]
    private float patrolSpeed = 2f;

    [Header("Chase Settings")]
    [SerializeField]
    private float chaseSpeed = 4f;

    [Header("Search Settings")]
    [SerializeField]
    private float searchDuration = 4f;

    [SerializeField]
    private float searchTolerance = 0.8f;

    [Header("Debug")]
    [SerializeField]
    private NPCState currentState;

    private NPCState previousState;

    private int patrolIndex = 0;

    // =============================
    // MEMORY
    // =============================

    private Vector3 lastKnownPosition;

    private bool hasLastKnownPosition;

    private float searchTimer;

    private void Start()
    {
        currentState = NPCState.Patrol;
        previousState = currentState;

        GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        UpdateMemory();

        MakeDecision();

        ExecuteCurrentState();
    }

    // ======================================
    // MEMORY
    // ======================================

    private void UpdateMemory()
    {
        if (sensor.CanSeePlayer)
        {
            lastKnownPosition =
                sensor.Player.position;

            hasLastKnownPosition = true;
        }
    }

    // ======================================
    // DECISION
    // ======================================

    private void MakeDecision()
    {
        // PRIORITAS 1
        // PLAYER TERLIHAT
        if (sensor.CanSeePlayer)
        {
            ChangeState(
                NPCState.Chase
            );

            return;
        }

        // PRIORITAS 2
        // PLAYER BARU HILANG
        if (currentState ==
                NPCState.Chase &&
            hasLastKnownPosition)
        {
            searchTimer =
                searchDuration;

            ChangeState(
                NPCState.Search
            );

            return;
        }

        // PRIORITAS 3
        // SEARCH SELESAI
        if (currentState ==
                NPCState.Search &&
            searchTimer <= 0f)
        {
            hasLastKnownPosition =
                false;

            ChangeState(
                NPCState.Patrol
            );
        }
    }

    // ======================================
    // ACTION
    // ======================================

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:

                Patrol();
                break;

            case NPCState.Chase:

                Chase();
                break;

            case NPCState.Search:

                Search();
                break;
        }
    }

    // ======================================
    // PATROL
    // ======================================

    private void Patrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        if (!agent.pathPending &&
            agent.remainingDistance <=
            waypointTolerance)
        {
            patrolIndex++;

            if (patrolIndex >=
                patrolPoints.Length)
            {
                patrolIndex = 0;
            }

            GoToCurrentPatrolPoint();
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        agent.SetDestination(
            patrolPoints[
                patrolIndex
            ].position
        );
    }

    // ======================================
    // CHASE
    // ======================================

    private void Chase()
    {
        agent.speed =
            chaseSpeed;

        if (sensor.Player == null)
            return;

        agent.SetDestination(
            sensor.Player.position
        );
    }

    // ======================================
    // SEARCH
    // ======================================

    private void Search()
    {
        agent.speed =
            patrolSpeed;

        agent.SetDestination(
            lastKnownPosition
        );

        if (!agent.pathPending &&
            agent.remainingDistance <=
            searchTolerance)
        {
            searchTimer -=
                Time.deltaTime;

            agent.ResetPath();
        }
    }

    // ======================================
    // STATE TRANSITION
    // ======================================

    private void ChangeState(
        NPCState newState
    )
    {
        if (currentState ==
            newState)
        {
            return;
        }

        previousState =
            currentState;

        currentState =
            newState;

        Debug.Log(
            gameObject.name +
            ": " +
            previousState +
            " -> " +
            currentState
        );

        if (currentState ==
            NPCState.Patrol)
        {
            GoToCurrentPatrolPoint();
        }
    }

    // ======================================
    // DEBUG GIZMOS
    // ======================================

    private void OnDrawGizmosSelected()
    {
        switch (currentState)
        {
            case NPCState.Patrol:

                Gizmos.color =
                    Color.green;
                break;

            case NPCState.Chase:

                Gizmos.color =
                    Color.red;
                break;

            case NPCState.Search:

                Gizmos.color =
                    Color.blue;
                break;
        }

        Gizmos.DrawWireSphere(
            transform.position,
            0.8f
        );

        if (hasLastKnownPosition)
        {
            Gizmos.color =
                Color.magenta;

            Gizmos.DrawSphere(
                lastKnownPosition,
                0.3f
            );

            Gizmos.DrawLine(
                transform.position,
                lastKnownPosition
            );
        }
    }
}
\`\`\`

---

# 39. Memasang NPCBrain

Pilih:

\`\`\`text
NPC_Guard
\`\`\`

Tambahkan:

\`\`\`text
NPCBrain
\`\`\`

Isi References:

\`\`\`text
Sensor
→ drag NPC_Guard

Agent
→ drag NPC_Guard
\`\`\`

Unity akan mengambil komponen:

\`\`\`text
NPCSensor
NavMeshAgent
\`\`\`

dari object tersebut.

---

# 40. Memasukkan Patrol Points

Pada:

\`\`\`text
NPCBrain
→ Patrol Points
\`\`\`

set:

\`\`\`text
Size = 4
\`\`\`

Kemudian:

\`\`\`text
Element 0 → Point1
Element 1 → Point2
Element 2 → Point3
Element 3 → Point4
\`\`\`

---

# 41. Parameter NPC

Gunakan nilai awal berikut.

## NPCSensor

\`\`\`text
View Radius = 8
View Angle = 90
Eye Height = 1.2
\`\`\`

## NPCBrain

\`\`\`text
Waypoint Tolerance = 0.7

Patrol Speed = 2

Chase Speed = 4

Search Duration = 4

Search Tolerance = 0.8
\`\`\`

---

# 42. Memahami State NPC

NPC memiliki tiga state utama.

\`\`\`text
PATROL

CHASE

SEARCH
\`\`\`

---

# 43. State PATROL

Dalam PATROL:

\`\`\`text
NPC berjalan:
Point1
   ↓
Point2
   ↓
Point3
   ↓
Point4
   ↓
Point1
\`\`\`

Kecepatan:

\`\`\`text
Patrol Speed
\`\`\`

---

# 44. State CHASE

NPC masuk CHASE ketika:

\`\`\`text
sensor.CanSeePlayer == true
\`\`\`

NPC kemudian menjalankan:

\`\`\`csharp
agent.SetDestination(
    sensor.Player.position
);
\`\`\`

Tujuan NPC selalu diperbarui mengikuti posisi Player.

---

# 45. State SEARCH

Ketika Player menghilang:

\`\`\`text
CHASE
  ↓
SEARCH
\`\`\`

NPC tidak mengetahui posisi Player terbaru.

Tetapi NPC masih menyimpan:

\`\`\`text
lastKnownPosition
\`\`\`

NPC kemudian menuju lokasi tersebut.

---

# 46. Konsep Memory

Perhatikan:

\`\`\`csharp
lastKnownPosition =
    sensor.Player.position;
\`\`\`

Kode hanya dijalankan jika Player terlihat.

Contoh:

\`\`\`text
t0

NPC ───────── PLAYER
\`\`\`

Memory NPC:

\`\`\`text
lastKnownPosition = posisi Player
\`\`\`

Kemudian:

\`\`\`text
t1

NPC ─── WALL │ PLAYER
\`\`\`

NPC tidak melihat Player lagi.

Tetapi memory masih menyimpan:

\`\`\`text
posisi Player saat terakhir terlihat
\`\`\`

Inilah bentuk sederhana dari:

\`\`\`text
Belief State
\`\`\`

sesuai konsep pada materi Pertemuan 2.

---

# 47. Konsep Decision

Decision rule praktikum:

\`\`\`text
IF Player terlihat
    CHASE

ELSE IF Player baru saja hilang
    SEARCH

ELSE IF Search selesai
    PATROL
\`\`\`

Flow:

\`\`\`text
Player terlihat?
       │
   ┌───┴───┐
  YES      NO
   │        │
   ▼        ▼
 CHASE   Baru hilang?
            │
        ┌───┴───┐
       YES      NO
        │        │
        ▼        ▼
      SEARCH   PATROL
\`\`\`

---

# 48. Konsep Action

Decision menentukan:

\`\`\`text
Apa yang harus dilakukan?
\`\`\`

Action menerjemahkan keputusan tersebut ke Unity.

PATROL:

\`\`\`csharp
agent.SetDestination(
    patrolPoint.position
);
\`\`\`

CHASE:

\`\`\`csharp
agent.SetDestination(
    player.position
);
\`\`\`

SEARCH:

\`\`\`csharp
agent.SetDestination(
    lastKnownPosition
);
\`\`\`

Sehingga:

\`\`\`text
Decision
    ↓
CHASE
    ↓
Action
    ↓
NavMeshAgent.SetDestination()
\`\`\`

---

# 49. Testing Lengkap

Sekarang tekan:

\`\`\`text
Play
\`\`\`

Klik jendela Game.

Gunakan:

\`\`\`text
WASD
\`\`\`

atau:

\`\`\`text
Arrow Key
\`\`\`

untuk menggerakkan Player.

---

# 50. Test 1 — Patrol

Tempatkan Player jauh dari NPC.

Expected:

\`\`\`text
NPC State = Patrol
\`\`\`

NPC berjalan melalui waypoint.

Pastikan:

\`\`\`text
Point1 → Point2 → Point3 → Point4
\`\`\`

berjalan dengan benar.

---

# 51. Test 2 — Player di Belakang NPC

Gerakkan Player mendekati NPC dari belakang.

\`\`\`text
PLAYER → NPC →
\`\`\`

Walaupun dekat:

\`\`\`text
Player tidak berada dalam FOV
\`\`\`

Expected:

\`\`\`text
NPC tetap PATROL
\`\`\`

---

# 52. Test 3 — Player Masuk FOV

Gerakkan Player ke depan NPC.

\`\`\`text
NPC ─────→ PLAYER
\`\`\`

Jika:

\`\`\`text
Distance < View Radius

dan

Angle < View Angle / 2

dan

tidak ada obstacle
\`\`\`

Expected:

\`\`\`text
PATROL
   ↓
CHASE
\`\`\`

Console:

\`\`\`text
NPC_Guard: Patrol -> Chase
\`\`\`

---

# 53. Test 4 — NPC Mengejar Player

Gunakan:

\`\`\`text
WASD
\`\`\`

untuk menjauh.

NPC harus mengikuti Player.

Contoh:

\`\`\`text
NPC → → → PLAYER
\`\`\`

Perhatikan:

\`\`\`text
Patrol Speed = 2

Chase Speed = 4
\`\`\`

NPC bergerak lebih cepat ketika CHASE.

---

# 54. Test 5 — Player Bersembunyi

Ketika NPC mengejar, arahkan Player ke belakang Wall.

\`\`\`text
NPC → → WALL │ PLAYER
\`\`\`

Raycast mengenai Wall.

Expected:

\`\`\`text
CanSeePlayer = false
\`\`\`

State:

\`\`\`text
CHASE
   ↓
SEARCH
\`\`\`

Console:

\`\`\`text
NPC_Guard: Chase -> Search
\`\`\`

---

# 55. Last Known Position

Saat Player hilang, NPC bergerak menuju:

\`\`\`text
lastKnownPosition
\`\`\`

Pada Scene View akan terlihat:

\`\`\`text
Sphere magenta
\`\`\`

yang menunjukkan posisi terakhir Player terlihat.

---

# 56. Test 6 — Player Tetap Bersembunyi

Biarkan Player tetap berada di belakang Wall.

NPC akan:

\`\`\`text
menuju lastKnownPosition
        ↓
sampai di lokasi
        ↓
menunggu
        ↓
Search Duration habis
        ↓
PATROL
\`\`\`

Expected:

\`\`\`text
SEARCH
   ↓
PATROL
\`\`\`

Console:

\`\`\`text
NPC_Guard: Search -> Patrol
\`\`\`

---

# 57. Test 7 — Player Muncul Saat Search

Ulangi:

\`\`\`text
CHASE
  ↓
Player bersembunyi
  ↓
SEARCH
\`\`\`

Tetapi sebelum Search Duration habis, keluarkan Player dari balik Wall.

Jika NPC melihat Player:

\`\`\`text
SEARCH
   ↓
CHASE
\`\`\`

Ini menunjukkan perception terbaru mempunyai prioritas tinggi.

---

# 58. Debugging dengan Gizmos

Pilih:

\`\`\`text
NPC_Guard
\`\`\`

Pastikan:

\`\`\`text
Gizmos = ON
\`\`\`

Interpretasi warna:

\`\`\`text
Lingkaran kuning
= View Radius

Garis kuning
= batas FOV

Garis merah ke Player
= Player terlihat

Sphere magenta
= Last Known Position
\`\`\`

Warna state NPC:

\`\`\`text
Hijau
= PATROL

Merah
= CHASE

Biru
= SEARCH
\`\`\`

Debug visual sangat penting karena masalah AI sering berasal dari:

\`\`\`text
FOV salah
Layer salah
Raycast salah
Waypoint salah
NavMesh salah
Memory tidak tersimpan
State tidak berubah
\`\`\`

---

# 59. Debugging Console

Setiap perpindahan state menghasilkan log.

Contoh:

\`\`\`text
NPC_Guard: Patrol -> Chase

NPC_Guard: Chase -> Search

NPC_Guard: Search -> Patrol
\`\`\`

Gunakan:

\`\`\`text
Window
→ General
→ Console
\`\`\`

untuk melihat log.

---

# 60. Eksperimen 1 — View Radius

Coba:

\`\`\`text
View Radius = 4
\`\`\`

kemudian:

\`\`\`text
View Radius = 8
\`\`\`

dan:

\`\`\`text
View Radius = 15
\`\`\`

Amati perubahan jarak deteksi.

Pertanyaan:

> Apakah radius yang sangat besar selalu menghasilkan NPC yang lebih baik?

---

# 61. Eksperimen 2 — View Angle

Coba:

\`\`\`text
45°
90°
180°
360°
\`\`\`

Bandingkan perilakunya.

Dengan:

\`\`\`text
360°
\`\`\`

NPC dapat mendeteksi Player dari hampir seluruh arah.

Diskusikan apakah perilaku tersebut terasa adil bagi pemain.

---

# 62. Eksperimen 3 — Player Speed dan Chase Speed

Gunakan:

\`\`\`text
Player Move Speed = 5
\`\`\`

Bandingkan:

| Player | Patrol | Chase | Efek |
|---:|---:|---:|---|
| 5 | 2 | 3 | Player mudah kabur |
| 5 | 2 | 4 | Cukup seimbang |
| 5 | 2 | 5 | Hampir sama |
| 5 | 2 | 7 | NPC sangat agresif |

Mahasiswa harus memahami bahwa Game AI tidak hanya mengenai algoritma, tetapi juga:

\`\`\`text
parameter tuning
\`\`\`

---

# 63. Eksperimen 4 — Search Duration

Coba:

\`\`\`text
Search Duration = 1
\`\`\`

kemudian:

\`\`\`text
4
\`\`\`

dan:

\`\`\`text
10
\`\`\`

Amati perbedaannya.

Search Duration terlalu kecil:

\`\`\`text
NPC terlalu cepat lupa
\`\`\`

Search Duration terlalu besar:

\`\`\`text
NPC terlalu lama mencari
\`\`\`

---

# 64. Eksperimen 5 — Obstacle

Tambahkan beberapa Cube baru.

Set semuanya:

\`\`\`text
Layer = Obstacle
\`\`\`

Susun menjadi lorong sederhana.

Contoh:

\`\`\`text
┌───────────────┐
│               │
│   WALL        │
│        WALL   │
│               │
│ NPC    PLAYER │
└───────────────┘
\`\`\`

Amati bagaimana obstacle memengaruhi:

\`\`\`text
Line of Sight
\`\`\`

dan:

\`\`\`text
NavMesh navigation
\`\`\`

---

# 65. Checklist Pengujian

Mahasiswa wajib menguji minimal kondisi berikut:

| No | Kondisi | Expected |
|---|---|---|
| 1 | Player jauh | PATROL |
| 2 | Player dekat tetapi di belakang NPC | PATROL |
| 3 | Player di depan NPC | CHASE |
| 4 | Player di balik Wall sebelum terdeteksi | PATROL |
| 5 | Player hilang ketika sedang dikejar | SEARCH |
| 6 | Player muncul kembali saat SEARCH | CHASE |
| 7 | Player tidak ditemukan | PATROL |

---

# 66. Troubleshooting — Player Tidak Bergerak

Pastikan:

\`\`\`text
PlayerController
\`\`\`

sudah terpasang pada:

\`\`\`text
Player
\`\`\`

Pastikan juga jendela:

\`\`\`text
Game
\`\`\`

aktif ketika menekan keyboard.

Jika terdapat error Input:

\`\`\`text
Edit
→ Project Settings
→ Player
→ Active Input Handling
→ Both
\`\`\`

---

# 67. Troubleshooting — NPC Tidak Bergerak

Periksa:

\`\`\`text
NavMeshAgent
\`\`\`

sudah terpasang.

Pastikan:

\`\`\`text
NavMesh sudah di-Bake
\`\`\`

dan NPC berada tepat di atas NavMesh.

---

# 68. Troubleshooting — NPC Tidak Patrol

Periksa:

\`\`\`text
Patrol Points
Size = 4
\`\`\`

Pastikan:

\`\`\`text
Element 0 = Point1
Element 1 = Point2
Element 2 = Point3
Element 3 = Point4
\`\`\`

Pastikan waypoint berada di atas NavMesh.

---

# 69. Troubleshooting — NPC Tidak Melihat Player

Periksa field:

\`\`\`text
NPCSensor
→ Player
\`\`\`

Harus berisi:

\`\`\`text
Player
\`\`\`

Kemudian periksa:

\`\`\`text
View Radius
View Angle
\`\`\`

Coba terlebih dahulu:

\`\`\`text
View Radius = 10
View Angle = 120
\`\`\`

untuk debugging.

---

# 70. Troubleshooting — NPC Melihat Melalui Wall

Pastikan:

\`\`\`text
Wall
→ Layer = Obstacle
\`\`\`

dan:

\`\`\`text
NPCSensor
→ Obstacle Mask
→ Obstacle
\`\`\`

Pastikan Wall mempunyai:

\`\`\`text
Collider
\`\`\`

Cube otomatis memiliki Box Collider.

---

# 71. Troubleshooting — NPC Langsung Kembali Patrol

Periksa:

\`\`\`text
Search Duration
\`\`\`

Pastikan bukan:

\`\`\`text
0
\`\`\`

Gunakan:

\`\`\`text
4
\`\`\`

untuk pengujian awal.

---

# 72. Troubleshooting — NPC Tidak Mengejar Player

Periksa:

\`\`\`text
CanSeePlayer
\`\`\`

melalui Inspector saat Play Mode.

Jika:

\`\`\`text
false
\`\`\`

berarti masalah berada pada Sensor.

Jika:

\`\`\`text
true
\`\`\`

tetapi NPC tidak bergerak, periksa:

\`\`\`text
NavMeshAgent
NavMesh
NPCBrain
\`\`\`

---

# 73. Troubleshooting — NPC Bergerak Aneh

Pastikan NPC tidak memiliki:

\`\`\`text
Rigidbody
\`\`\`

yang menggunakan physics bebas bersamaan dengan NavMeshAgent.

Untuk praktikum ini cukup gunakan:

\`\`\`text
Capsule Collider
+
NavMeshAgent
\`\`\`

---

# 74. Struktur Script Akhir

Folder Scripts:

\`\`\`text
Assets
└── Scripts
    ├── PlayerController.cs
    ├── NPCSensor.cs
    └── NPCBrain.cs
\`\`\`

---

# 75. Struktur NPC Akhir

\`\`\`text
NPC_Guard
│
├── Transform
├── Mesh Renderer
├── Capsule Collider
├── NavMesh Agent
├── NPC Sensor
├── NPC Brain
│
└── DirectionMarker
\`\`\`

---

# 76. Struktur Player Akhir

\`\`\`text
Player
│
├── Transform
├── Mesh Renderer
├── Capsule Collider
└── Player Controller
\`\`\`

---

# 77. Struktur Scene Akhir

\`\`\`text
Praktikum02_NPCGuard
│
├── Main Camera
├── Directional Light
│
├── Ground
│
├── Navigation
│   └── NavMesh Surface
│
├── Player
│   └── PlayerController
│
├── NPC_Guard
│   ├── NavMeshAgent
│   ├── NPCSensor
│   ├── NPCBrain
│   └── DirectionMarker
│
├── Wall
│
└── PatrolPoints
    ├── Point1
    ├── Point2
    ├── Point3
    └── Point4
\`\`\`

---

# 78. Hubungan dengan Game Agent Architecture

Setelah praktikum selesai, identifikasi komponen berikut.

## Environment

\`\`\`text
Ground
Wall
Player
Waypoint
\`\`\`

## Perception

\`\`\`text
Distance
Field of View
Raycast
\`\`\`

## Memory

\`\`\`text
lastKnownPosition

hasLastKnownPosition

searchTimer
\`\`\`

## Decision

\`\`\`text
PATROL

CHASE

SEARCH
\`\`\`

## Action

\`\`\`text
NavMeshAgent.SetDestination()
\`\`\`

Keseluruhan:

\`\`\`text
Player bergerak
      ↓
Environment berubah
      ↓
NPC melakukan sensing
      ↓
Memory diperbarui
      ↓
Decision dipilih
      ↓
NPC melakukan Action
      ↓
Environment kembali berubah
\`\`\`

Ini merupakan implementasi konkret dari arsitektur agen yang dibahas dalam materi Pertemuan 2.

---

# 79. Diagram Final NPC Guard

\`\`\`text
                 GAME WORLD
                     │
                     ▼
              ┌─────────────┐
              │ NPC SENSOR  │
              │             │
              │ Distance    │
              │ FOV         │
              │ Raycast     │
              └──────┬──────┘
                     │
                     ▼
              Can See Player?
                     │
            ┌────────┴────────┐
           YES                NO
            │                  │
            ▼                  ▼
      Save Position      Recently Seen?
            │                  │
            ▼            ┌─────┴─────┐
          CHASE          YES          NO
                           │           │
                           ▼           ▼
                        SEARCH      PATROL
                           │
                           ▼
                  Player Found Again?
                       │          │
                      YES        NO
                       │          │
                       ▼          ▼
                     CHASE    Search Timeout
                                   │
                                   ▼
                                PATROL
\`\`\`

---

# 80. Pertanyaan Analisis

Jawab pertanyaan berikut.

### Pertanyaan 1

Mengapa NPC tidak cukup hanya menggunakan:

\`\`\`text
distance
\`\`\`

untuk mendeteksi Player?

---

### Pertanyaan 2

Apa fungsi:

\`\`\`text
Field of View
\`\`\`

dalam perception?

---

### Pertanyaan 3

Apa fungsi:

\`\`\`text
Raycast
\`\`\`

dalam praktikum ini?

---

### Pertanyaan 4

Mengapa Player yang berada di belakang Wall tidak boleh dianggap terlihat?

---

### Pertanyaan 5

Apa perbedaan antara:

\`\`\`text
Perception
\`\`\`

dan:

\`\`\`text
Memory
\`\`\`

?

---

### Pertanyaan 6

Jelaskan fungsi:

\`\`\`text
lastKnownPosition
\`\`\`

---

### Pertanyaan 7

Mengapa NPC tidak langsung kembali PATROL setelah kehilangan Player?

---

### Pertanyaan 8

Apa yang terjadi jika:

\`\`\`text
Search Duration = 0
\`\`\`

?

---

### Pertanyaan 9

Apa yang terjadi jika:

\`\`\`text
View Angle = 360
\`\`\`

?

---

### Pertanyaan 10

Mengapa parameter seperti:

\`\`\`text
viewRadius
viewAngle
patrolSpeed
chaseSpeed
searchDuration
\`\`\`

lebih baik dibuat:

\`\`\`csharp
[SerializeField]
\`\`\`

daripada ditulis permanen di dalam kode?

---

### Pertanyaan 11

Jelaskan bagaimana alur:

\`\`\`text
Perception
→ Memory
→ Decision
→ Action
\`\`\`

terjadi pada NPC Guard.

---

### Pertanyaan 12

Jika NPC berfungsi tetapi mahasiswa tidak dapat menjelaskan mengapa NPC masuk state tertentu, apakah implementasinya dapat dianggap sebagai AI yang dirancang dengan baik? Jelaskan.

---

# 81. Challenge 1 — NPC Berhenti di Waypoint

Modifikasi PATROL:

\`\`\`text
berjalan
   ↓
sampai waypoint
   ↓
diam 2 detik
   ↓
lanjut waypoint berikutnya
\`\`\`

Tujuannya memahami tambahan behavior sederhana.

---

# 82. Challenge 2 — Search Rotation

Saat NPC sampai di:

\`\`\`text
lastKnownPosition
\`\`\`

buat NPC:

\`\`\`text
lihat kiri
   ↓
lihat kanan
   ↓
menunggu
   ↓
kembali patrol
\`\`\`

NPC akan tampak lebih seperti sedang mencari.

---

# 83. Challenge 3 — Alert Indicator

Tambahkan object:

\`\`\`text
!
\`\`\`

di atas kepala NPC ketika:

\`\`\`text
CHASE
\`\`\`

dan:

\`\`\`text
?
\`\`\`

ketika:

\`\`\`text
SEARCH
\`\`\`

---

# 84. Challenge 4 — Hearing

Tambahkan konsep:

\`\`\`text
Player menghasilkan suara
        ↓
NPC mendengar
        ↓
NPC menuju sumber suara
\`\`\`

Ini memperluas perception dari:

\`\`\`text
Visual Sensor
\`\`\`

menjadi:

\`\`\`text
Visual + Auditory
\`\`\`

---

# 85. Challenge 5 — Multiple Guards

Duplikat:

\`\`\`text
NPC_Guard
\`\`\`

menjadi:

\`\`\`text
NPC_Guard_A

NPC_Guard_B

NPC_Guard_C
\`\`\`

Berikan waypoint berbeda.

Amati apakah setiap agent dapat mengambil keputusan secara mandiri.

---

# 86. Checklist Praktikum

Sebelum mengumpulkan project, pastikan semua berikut berhasil.

## Player

- [ ] Player dapat bergerak dengan W.
- [ ] Player dapat bergerak dengan A.
- [ ] Player dapat bergerak dengan S.
- [ ] Player dapat bergerak dengan D.
- [ ] Player dapat bergerak dengan Arrow Key.
- [ ] Player berputar mengikuti arah gerak.
- [ ] Move Speed dapat diubah di Inspector.

## Navigation

- [ ] AI Navigation sudah terpasang.
- [ ] Ground memiliki NavMesh.
- [ ] NPC memiliki NavMeshAgent.
- [ ] NPC dapat bergerak di arena.

## Patrol

- [ ] Terdapat minimal empat waypoint.
- [ ] NPC bergerak antar-waypoint.
- [ ] Setelah waypoint terakhir NPC kembali ke waypoint pertama.

## Perception

- [ ] NPC memiliki View Radius.
- [ ] NPC memiliki Field of View.
- [ ] NPC menggunakan Raycast.
- [ ] Player di belakang NPC tidak terdeteksi.
- [ ] Wall menghalangi penglihatan NPC.

## Memory

- [ ] NPC menyimpan lastKnownPosition.
- [ ] Last Known Position terlihat di Gizmos.
- [ ] Memory diperbarui ketika Player terlihat.

## Decision

- [ ] NPC mempunyai state PATROL.
- [ ] NPC mempunyai state CHASE.
- [ ] NPC mempunyai state SEARCH.
- [ ] PATROL berubah menjadi CHASE.
- [ ] CHASE berubah menjadi SEARCH.
- [ ] SEARCH dapat berubah kembali menjadi CHASE.
- [ ] SEARCH berubah menjadi PATROL setelah timeout.

## Debugging

- [ ] Detection Radius terlihat.
- [ ] FOV terlihat.
- [ ] Garis ke Player terlihat ketika terdeteksi.
- [ ] Last Known Position terlihat.
- [ ] Transisi state muncul di Console.

---

# 87. Skenario Demo Akhir

Video demo sebaiknya memperlihatkan seluruh siklus berikut.

## Adegan 1

NPC berjalan patroli.

\`\`\`text
PATROL
\`\`\`

---

## Adegan 2

Player digerakkan menggunakan:

\`\`\`text
WASD
\`\`\`

masuk ke FOV NPC.

\`\`\`text
PATROL
   ↓
CHASE
\`\`\`

---

## Adegan 3

NPC mengejar Player.

\`\`\`text
NPC → → → PLAYER
\`\`\`

---

## Adegan 4

Player bersembunyi di balik Wall.

\`\`\`text
NPC → WALL │ PLAYER
\`\`\`

State:

\`\`\`text
CHASE
   ↓
SEARCH
\`\`\`

---

## Adegan 5

NPC menuju:

\`\`\`text
lastKnownPosition
\`\`\`

---

## Adegan 6

Player tetap bersembunyi.

Setelah:

\`\`\`text
Search Duration
\`\`\`

state menjadi:

\`\`\`text
SEARCH
   ↓
PATROL
\`\`\`

---

## Adegan 7

Ulangi demonstrasi, tetapi kali ini Player keluar dari Wall ketika SEARCH.

Expected:

\`\`\`text
SEARCH
   ↓
CHASE
\`\`\`

---

# 88. Deliverable Praktikum

Mahasiswa mengumpulkan:

### A. Unity Project

Project Unity lengkap atau repository Git.

---

### B. Screenshot Scene

Harus menunjukkan:

\`\`\`text
Player
NPC
Wall
Waypoint
Navigation
\`\`\`

---

### C. Screenshot Gizmos

Minimal menunjukkan:

\`\`\`text
View Radius

Field of View

Line of Sight

Last Known Position
\`\`\`

---

### D. Screenshot State

Minimal:

\`\`\`text
PATROL

CHASE

SEARCH
\`\`\`

---

### E. Video Demo

Durasi yang disarankan:

\`\`\`text
1–3 menit
\`\`\`

Harus menunjukkan:

\`\`\`text
PATROL
   ↓
CHASE
   ↓
SEARCH
   ↓
PATROL
\`\`\`

serta satu contoh:

\`\`\`text
SEARCH
   ↓
CHASE
\`\`\`

---

### F. Laporan Singkat

Struktur:

\`\`\`text
1. Tujuan Praktikum

2. Desain Scene

3. Arsitektur NPC

4. Implementasi Player Controller

5. Implementasi Sensor

6. Implementasi Memory

7. Implementasi Decision

8. Implementasi Action

9. Hasil Pengujian

10. Eksperimen Parameter

11. Analisis

12. Kesimpulan
\`\`\`

---

# 89. Rubrik Penilaian

| Komponen | Bobot |
|---|---:|
| Sensor bekerja | 25% |
| Memory & State | 20% |
| Decision Rule | 20% |
| Debug Gizmos | 15% |
| Parameter Tuning | 10% |
| Penjelasan / Analisis | 10% |
| **Total** | **100%** |

---

# 90. Kriteria Keberhasilan Praktikum

NPC tidak cukup hanya:

> bergerak dan mengejar Player.

Mahasiswa harus mampu menjelaskan:

\`\`\`text
Apa yang dilihat NPC?

Bagaimana NPC mengetahui Player ada?

Bagaimana NPC mengetahui Player terhalang?

Informasi apa yang disimpan?

Mengapa NPC memilih CHASE?

Mengapa NPC memilih SEARCH?

Mengapa NPC kembali PATROL?
\`\`\`

Dengan demikian fokus praktikum bukan hanya coding, tetapi memahami bagaimana sebuah **Game Agent** bekerja.

---

# 91. Ringkasan

Pada praktikum ini kita telah membangun:

\`\`\`text
PLAYER
WASD / Arrow Key
       │
       ▼
   GAME WORLD
       │
       ▼
   PERCEPTION
       │
       ├── Radius
       ├── FOV
       └── Raycast
       │
       ▼
     MEMORY
       │
       └── lastKnownPosition
       │
       ▼
    DECISION
       │
       ├── PATROL
       ├── CHASE
       └── SEARCH
       │
       ▼
     ACTION
       │
       └── NavMeshAgent
       │
       ▼
   GAME WORLD
\`\`\`

Perilaku akhir:

\`\`\`text
NPC Patrol
    ↓
Player terlihat
    ↓
NPC Chase
    ↓
Player bersembunyi
    ↓
NPC kehilangan target
    ↓
NPC menuju posisi terakhir
    ↓
NPC Search
    ↓
Player tidak ditemukan
    ↓
NPC Patrol kembali
\`\`\`

atau:

\`\`\`text
SEARCH
   ↓
Player muncul kembali
   ↓
CHASE
\`\`\`

Praktikum ini menjadi dasar untuk pengembangan AI berikutnya seperti:

\`\`\`text
Finite State Machine
Steering Behavior
Behavior Tree
Utility AI
Hearing System
Combat AI
Group AI
\`\`\`

---

# SELESAI

## Praktikum 2 — NPC Guard: Sensor + Memory + Decision

**Game Cerdas – S1 Teknik Informatika**

**Unity 6 + C#**

**Konsep utama:**

\`\`\`text
PERCEPTION → MEMORY → DECISION → ACTION
\`\`\``,Rc=`# MODUL PRAKTIKUM 3  
# Autonomous Steering Agent — Movement AI & Steering Behaviors

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Game Engine:** Unity 6  
**Bahasa Pemrograman:** C#  
**Materi:** Movement AI & Steering Behaviors

---

# 1. Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat sebuah **NPC Autonomous Steering Agent** yang mampu bergerak secara mandiri di dalam lingkungan game.

NPC akan memiliki kemampuan:

1. Bergerak menuju target.
2. Melambat ketika mendekati target.
3. Berhenti pada jarak tertentu dari target.
4. Menghadap ke arah gerak.
5. Bergerak secara acak menggunakan **Wander** ketika tidak memiliki target.
6. Mendeteksi obstacle menggunakan sensor fisika.
7. Menghindari obstacle menggunakan **Obstacle Avoidance**.
8. Menggabungkan beberapa steering behavior.
9. Melakukan tuning parameter AI melalui Inspector.
10. Melakukan debugging menggunakan Gizmos.

Behavior utama yang digunakan:

\`\`\`text
                    TARGET ADA?
                       │
             ┌─────────┴─────────┐
             │                   │
            YA                 TIDAK
             │                   │
          ARRIVE               WANDER
             │                   │
             └─────────┬─────────┘
                       │
              OBSTACLE AVOIDANCE
                       │
                       ▼
                DESIRED VELOCITY
                       │
                       ▼
                 ACCELERATION
                       │
                       ▼
                    VELOCITY
                       │
                       ▼
                    MOVEMENT
                       │
                       ▼
                   ROTATION
\`\`\`

---

# 2. Hubungan dengan Praktikum Sebelumnya

Pada praktikum sebelumnya fokus AI berada pada:

\`\`\`text
Perception
    ↓
Memory
    ↓
Decision
\`\`\`

Pada Praktikum 3 fokus berpindah ke:

\`\`\`text
Decision
    ↓
Movement / Action
\`\`\`

Contohnya:

\`\`\`text
Decision:
"Kejar Player"

       ↓

Movement AI:
"Bagaimana NPC bergerak menuju Player?"
\`\`\`

Movement AI tidak menentukan **apa keputusan NPC**, tetapi menentukan **bagaimana keputusan tersebut diterjemahkan menjadi gerakan**.

---

# 3. Konsep yang Digunakan

Praktikum ini menggunakan beberapa konsep utama.

## 3.1 Position

Posisi GameObject di world space.

\`\`\`csharp
transform.position
\`\`\`

Contoh:

\`\`\`csharp
Vector3 npcPosition = transform.position;
\`\`\`

---

## 3.2 Direction

Direction adalah arah dari suatu posisi menuju posisi lainnya.

\`\`\`text
Direction = Target Position - Agent Position
\`\`\`

Dalam Unity:

\`\`\`csharp
Vector3 direction =
    target.position - transform.position;
\`\`\`

---

## 3.3 Magnitude

Magnitude menunjukkan panjang sebuah vector.

\`\`\`csharp
float distance = direction.magnitude;
\`\`\`

Dalam konteks steering:

\`\`\`text
Magnitude direction
        =
jarak agent ke target
\`\`\`

---

## 3.4 Normalized Vector

Jika:

\`\`\`csharp
Vector3 direction =
    target.position - transform.position;
\`\`\`

besar vector tersebut masih dipengaruhi jarak.

Agar hanya mendapatkan arah:

\`\`\`csharp
direction.Normalize();
\`\`\`

atau:

\`\`\`csharp
direction = direction.normalized;
\`\`\`

Vector hasil normalisasi memiliki panjang:

\`\`\`text
1
\`\`\`

Sehingga dapat digunakan sebagai:

\`\`\`csharp
velocity = direction * speed;
\`\`\`

---

# 4. Model Movement yang Digunakan

Materi membedakan movement menjadi:

\`\`\`text
Kinematic Movement
Dynamic Movement
\`\`\`

Untuk praktikum ini digunakan pendekatan **semi-dynamic steering**.

Agent memiliki velocity internal:

\`\`\`csharp
private Vector3 velocity;
\`\`\`

Kemudian velocity bergerak menuju desired velocity secara bertahap:

\`\`\`csharp
velocity = Vector3.MoveTowards(
    velocity,
    desiredVelocity,
    maxAcceleration * Time.deltaTime
);
\`\`\`

Setelah itu:

\`\`\`csharp
transform.position +=
    velocity * Time.deltaTime;
\`\`\`

Dengan demikian:

\`\`\`text
Desired Velocity
       ↓
Acceleration
       ↓
Velocity
       ↓
Position
\`\`\`

Pendekatan ini masih cukup sederhana untuk pembelajaran tetapi menghasilkan gerakan yang lebih halus daripada mengubah posisi dengan kecepatan maksimum secara langsung.

---

# 5. Hasil Akhir Praktikum

Scene akhir kurang lebih memiliki struktur:

\`\`\`text
Praktikum03
│
├── Main Camera
├── Directional Light
├── Ground
│
├── Player
│
├── NPC
│   ├── SteeringAgent
│   └── SteeringSensor
│
└── Obstacles
    ├── Obstacle01
    ├── Obstacle02
    ├── Obstacle03
    └── Obstacle04
\`\`\`

NPC akan:

\`\`\`text
Target tersedia
        ↓
Menuju Player
        ↓
Melambat ketika mendekat
        ↓
Berhenti dekat Player
\`\`\`

Jika target tidak digunakan:

\`\`\`text
Tidak ada Target
        ↓
Wander
        ↓
Berjalan secara autonomous
\`\`\`

Pada kedua kondisi tersebut:

\`\`\`text
Obstacle terdeteksi
        ↓
Obstacle Avoidance
        ↓
NPC membelok menghindari obstacle
\`\`\`

---

# 6. Membuat Project

Buka Unity Hub.

Pilih:

\`\`\`text
New Project
\`\`\`

Gunakan template:

\`\`\`text
Universal 3D
\`\`\`

atau:

\`\`\`text
3D
\`\`\`

Nama project yang disarankan:

\`\`\`text
GameCerdas_Praktikum03_Steering
\`\`\`

Kemudian klik:

\`\`\`text
Create Project
\`\`\`

---

# 7. Membuat Struktur Folder

Pada jendela **Project**, buka:

\`\`\`text
Assets
\`\`\`

Buat folder:

\`\`\`text
Assets/
├── Scenes/
├── Scripts/
├── Materials/
└── Prefabs/
\`\`\`

Tujuannya agar asset project lebih terorganisasi.

---

# 8. Membuat Scene Praktikum

Simpan scene menggunakan:

\`\`\`text
File
→ Save As
\`\`\`

Simpan di:

\`\`\`text
Assets/Scenes/
\`\`\`

dengan nama:

\`\`\`text
Praktikum03_Steering
\`\`\`

---

# 9. Membuat Ground

Pilih:

\`\`\`text
GameObject
→ 3D Object
→ Plane
\`\`\`

Rename menjadi:

\`\`\`text
Ground
\`\`\`

Atur Transform:

\`\`\`text
Position
X = 0
Y = 0
Z = 0
\`\`\`

Scale:

\`\`\`text
X = 3
Y = 1
Z = 3
\`\`\`

Ground akan menjadi area pergerakan NPC dan Player.

---

# 10. Membuat Player

Pilih:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
Player
\`\`\`

Atur posisi awal misalnya:

\`\`\`text
X = 6
Y = 1
Z = 5
\`\`\`

Capsule secara default telah memiliki:

\`\`\`text
Capsule Collider
\`\`\`

---

# 11. Membuat Layer Player

Pilih GameObject:

\`\`\`text
Player
\`\`\`

Pada Inspector:

\`\`\`text
Layer
→ Add Layer...
\`\`\`

Tambahkan:

\`\`\`text
Player
\`\`\`

Kembali ke GameObject Player.

Set:

\`\`\`text
Layer = Player
\`\`\`

---

# 12. Membuat Player Controller

Agar Player dapat digerakkan menggunakan:

\`\`\`text
WASD
\`\`\`

atau:

\`\`\`text
Arrow Keys
\`\`\`

kita membuat controller sederhana.

Tambahkan komponen:

\`\`\`text
Character Controller
\`\`\`

ke Player melalui:

\`\`\`text
Inspector
→ Add Component
→ Character Controller
\`\`\`

---

# 13. Script SimplePlayerController.cs

Masuk ke:

\`\`\`text
Assets/Scripts
\`\`\`

Buat script:

\`\`\`text
SimplePlayerController.cs
\`\`\`

Isi dengan:

\`\`\`csharp
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float turnSpeed = 10f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical);

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
        }
    }
}
\`\`\`

Pasang script tersebut ke:

\`\`\`text
Player
\`\`\`

---

# 14. Penjelasan SimplePlayerController

Bagian:

\`\`\`csharp
Input.GetAxisRaw("Horizontal")
\`\`\`

membaca input horizontal.

Secara default:

\`\`\`text
A / Left Arrow  → -1
D / Right Arrow → +1
\`\`\`

Sedangkan:

\`\`\`csharp
Input.GetAxisRaw("Vertical")
\`\`\`

membaca:

\`\`\`text
S / Down Arrow → -1
W / Up Arrow   → +1
\`\`\`

Kemudian dibuat direction:

\`\`\`csharp
Vector3 direction =
    new Vector3(horizontal, 0f, vertical);
\`\`\`

Nilai Y dibuat:

\`\`\`text
0
\`\`\`

karena Player hanya bergerak pada bidang XZ.

---

# 15. Catatan Input System Unity 6

Jika muncul error yang berhubungan dengan:

\`\`\`text
Input.GetAxisRaw
\`\`\`

periksa:

\`\`\`text
Edit
→ Project Settings
→ Player
\`\`\`

Cari:

\`\`\`text
Active Input Handling
\`\`\`

Gunakan:

\`\`\`text
Both
\`\`\`

atau:

\`\`\`text
Input Manager (Old)
\`\`\`

untuk praktikum ini.

Hal ini dilakukan agar script pembelajaran tetap sederhana.

---

# 16. Membuat NPC

Buat Capsule baru:

\`\`\`text
GameObject
→ 3D Object
→ Capsule
\`\`\`

Rename:

\`\`\`text
NPC
\`\`\`

Posisi awal misalnya:

\`\`\`text
X = -6
Y = 1
Z = -5
\`\`\`

NPC ini akan menjadi:

\`\`\`text
Autonomous Steering Agent
\`\`\`

---

# 17. Memberikan Material Berbeda

Agar mudah membedakan Player dan NPC, buat dua material.

Contoh:

\`\`\`text
Materials/
├── PlayerMaterial
└── NPCMaterial
\`\`\`

Berikan warna yang berbeda sesuai kebutuhan.

Hal ini tidak memengaruhi algoritma AI tetapi membantu visualisasi.

---

# 18. Membuat Obstacle

Buat beberapa Cube.

\`\`\`text
GameObject
→ 3D Object
→ Cube
\`\`\`

Rename:

\`\`\`text
Obstacle01
\`\`\`

Contoh Transform:

\`\`\`text
Position:
X = 0
Y = 1
Z = 0

Scale:
X = 3
Y = 2
Z = 1
\`\`\`

Duplicate beberapa kali menggunakan:

\`\`\`text
Ctrl + D
\`\`\`

Buat konfigurasi misalnya:

\`\`\`text
Obstacle01
Obstacle02
Obstacle03
Obstacle04
\`\`\`

Susun obstacle sehingga NPC harus membelok untuk menuju Player.

---

# 19. Membuat Layer Obstacle

Buka:

\`\`\`text
Layer
→ Add Layer
\`\`\`

Tambahkan:

\`\`\`text
Obstacle
\`\`\`

Kemudian set setiap obstacle menjadi:

\`\`\`text
Layer = Obstacle
\`\`\`

Jika muncul:

\`\`\`text
Change children too?
\`\`\`

pilih sesuai struktur objek.

---

# 20. Mengapa Menggunakan Layer?

NPC menggunakan sensor:

\`\`\`text
Physics.SphereCast()
\`\`\`

Tanpa LayerMask, sensor berpotensi mendeteksi:

\`\`\`text
Ground
Player
NPC
Obstacle
objek lain
\`\`\`

Padahal yang ingin dideteksi hanya:

\`\`\`text
Obstacle
\`\`\`

Karena itu digunakan:

\`\`\`csharp
LayerMask obstacleMask;
\`\`\`

Alur:

\`\`\`text
SphereCast
    ↓
LayerMask
    ↓
Hanya mendeteksi Obstacle
\`\`\`

---

# 21. Membuat SteeringSensor.cs

Buat:

\`\`\`text
Assets/Scripts/SteeringSensor.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

public class SteeringSensor : MonoBehaviour
{
    [Header("Obstacle Sensor")]

    [SerializeField]
    private float sensorDistance = 3f;

    [SerializeField]
    private float sensorRadius = 0.5f;

    [SerializeField]
    private float sensorHeight = 0.5f;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Avoidance")]

    [SerializeField]
    private float forwardBias = 0.5f;

    private bool obstacleDetected;
    private RaycastHit lastHit;

    public bool ObstacleDetected => obstacleDetected;

    public RaycastHit LastHit => lastHit;

    public Vector3 GetAvoidanceDirection(
        Vector3 movementDirection)
    {
        obstacleDetected = false;

        if (movementDirection.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        movementDirection.Normalize();

        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        if (Physics.SphereCast(
            origin,
            sensorRadius,
            movementDirection,
            out lastHit,
            sensorDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            obstacleDetected = true;

            Vector3 avoidDirection =
                Vector3.ProjectOnPlane(
                    lastHit.normal,
                    Vector3.up
                );

            avoidDirection.y = 0f;

            if (avoidDirection.sqrMagnitude > 0.001f)
            {
                avoidDirection.Normalize();
            }

            avoidDirection +=
                movementDirection * forwardBias;

            return avoidDirection.normalized;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        Vector3 direction =
            transform.forward;

        Gizmos.DrawWireSphere(
            origin,
            sensorRadius
        );

        Gizmos.DrawLine(
            origin,
            origin + direction * sensorDistance
        );

        Gizmos.DrawWireSphere(
            origin + direction * sensorDistance,
            sensorRadius
        );
    }
}
\`\`\`

Pasang script ke:

\`\`\`text
NPC
\`\`\`

---

# 22. Memahami SphereCast

Pada Raycast:

\`\`\`text
NPC ● ─────────────→
\`\`\`

sensor hanya berupa garis.

Pada SphereCast:

\`\`\`text
NPC ● (========)→
\`\`\`

sensor memiliki radius.

NPC mempunyai ukuran fisik sehingga **SphereCast lebih representatif** daripada Raycast tunggal.

Sintaks utama:

\`\`\`csharp
Physics.SphereCast(
    origin,
    radius,
    direction,
    out hit,
    distance,
    layerMask
);
\`\`\`

---

# 23. RaycastHit

Jika obstacle ditemukan, Unity mengisi:

\`\`\`csharp
RaycastHit lastHit;
\`\`\`

Informasi penting di dalam \`RaycastHit\`:

\`\`\`text
hit.point
hit.normal
hit.distance
hit.collider
\`\`\`

Dalam praktikum ini yang penting adalah:

\`\`\`csharp
hit.normal
\`\`\`

---

# 24. Apa Itu Surface Normal?

Misalkan NPC menuju tembok:

\`\`\`text
NPC ─────→ █ WALL
           ← normal
\`\`\`

\`hit.normal\` adalah vector yang mengarah keluar dari permukaan obstacle.

Vector tersebut dapat dimanfaatkan sebagai dasar arah menghindar.

---

# 25. Vector3.ProjectOnPlane

Kode:

\`\`\`csharp
Vector3.ProjectOnPlane(
    lastHit.normal,
    Vector3.up
);
\`\`\`

digunakan untuk membuang komponen vertikal dari surface normal.

Dengan demikian NPC tetap bergerak pada:

\`\`\`text
bidang XZ
\`\`\`

dan tidak mencoba bergerak ke atas atau ke bawah.

---

# 26. Membuat SteeringAgent.cs

Sekarang kita membuat komponen utama AI.

Buat:

\`\`\`text
Assets/Scripts/SteeringAgent.cs
\`\`\`

Isi dengan kode berikut.

\`\`\`csharp
using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform target;

    [SerializeField]
    private bool useTarget = true;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Arrive")]

    [SerializeField]
    private float slowRadius = 4f;

    [SerializeField]
    private float stopRadius = 1.5f;

    [Header("Wander")]

    [SerializeField]
    private float wanderSpeed = 2.5f;

    [SerializeField]
    private float wanderChangeInterval = 1.5f;

    [SerializeField]
    private float wanderAngleChange = 45f;

    [Header("Obstacle Avoidance")]

    [SerializeField]
    private SteeringSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    public Vector3 Velocity => velocity;

    private void Start()
    {
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;
    }

    private void Update()
    {
        Vector3 desiredVelocity;

        if (useTarget && target != null)
        {
            desiredVelocity = CalculateArrive();
        }
        else
        {
            desiredVelocity = CalculateWander();
        }

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        velocity =
            Vector3.ClampMagnitude(
                velocity,
                maxSpeed
            );

        ApplyMovement();

        UpdateRotation();
    }

    private Vector3 CalculateArrive()
    {
        Vector3 toTarget =
            target.position - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return Vector3.zero;
        }

        float desiredSpeed = maxSpeed;

        if (distance < slowRadius)
        {
            float range =
                Mathf.Max(
                    slowRadius - stopRadius,
                    0.001f
                );

            float normalizedDistance =
                (distance - stopRadius) / range;

            desiredSpeed =
                maxSpeed *
                Mathf.Clamp01(normalizedDistance);
        }

        return toTarget.normalized * desiredSpeed;
    }

    private Vector3 CalculateWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            float randomAngle =
                Random.Range(
                    -wanderAngleChange,
                    wanderAngleChange
                );

            wanderDirection =
                Quaternion.Euler(
                    0f,
                    randomAngle,
                    0f
                ) * transform.forward;

            wanderDirection.y = 0f;
            wanderDirection.Normalize();

            wanderTimer = wanderChangeInterval;
        }

        return wanderDirection * wanderSpeed;
    }

    private Vector3 ApplyObstacleAvoidance(
        Vector3 desiredVelocity)
    {
        if (sensor == null)
        {
            return desiredVelocity;
        }

        Vector3 checkDirection =
            desiredVelocity.sqrMagnitude > 0.001f
                ? desiredVelocity.normalized
                : transform.forward;

        Vector3 avoidanceDirection =
            sensor.GetAvoidanceDirection(
                checkDirection
            );

        if (avoidanceDirection.sqrMagnitude > 0.001f)
        {
            Vector3 combinedDirection =
                checkDirection +
                avoidanceDirection *
                avoidanceWeight;

            combinedDirection.y = 0f;

            if (combinedDirection.sqrMagnitude > 0.001f)
            {
                combinedDirection.Normalize();
            }

            float desiredSpeed =
                Mathf.Max(
                    desiredVelocity.magnitude,
                    wanderSpeed
                );

            return combinedDirection * desiredSpeed;
        }

        return desiredVelocity;
    }

    private void ApplyMovement()
    {
        transform.position +=
            velocity * Time.deltaTime;
    }

    private void UpdateRotation()
    {
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                horizontalVelocity.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            stopRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            slowRadius
        );

        if (target != null)
        {
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }
    }
}
\`\`\`

---

# 27. Memasang SteeringAgent

Pilih:

\`\`\`text
NPC
\`\`\`

Tambahkan:

\`\`\`text
SteeringAgent
\`\`\`

Sekarang Inspector NPC memiliki parameter kurang lebih:

\`\`\`text
Target
├── Target
└── Use Target

Movement
├── Max Speed
├── Max Acceleration
└── Turn Speed

Arrive
├── Slow Radius
└── Stop Radius

Wander
├── Wander Speed
├── Wander Change Interval
└── Wander Angle Change

Obstacle Avoidance
├── Sensor
└── Avoidance Weight
\`\`\`

---

# 28. Menghubungkan Target

Drag:

\`\`\`text
Player
\`\`\`

dari Hierarchy ke field:

\`\`\`text
Target
\`\`\`

pada komponen SteeringAgent.

Sehingga:

\`\`\`text
NPC SteeringAgent
        │
        └── Target → Player
\`\`\`

Pastikan:

\`\`\`text
Use Target = true
\`\`\`

---

# 29. Menghubungkan SteeringSensor

Pada field:

\`\`\`text
Sensor
\`\`\`

drag komponen:

\`\`\`text
NPC
\`\`\`

atau drag langsung komponen SteeringSensor dari Inspector.

Tujuannya agar:

\`\`\`text
SteeringAgent
       ↓
SteeringSensor
       ↓
SphereCast
       ↓
Obstacle
\`\`\`

---

# 30. Mengatur Obstacle Mask

Pada komponen:

\`\`\`text
SteeringSensor
\`\`\`

set:

\`\`\`text
Obstacle Mask = Obstacle
\`\`\`

Jangan gunakan:

\`\`\`text
Everything
\`\`\`

karena sensor berpotensi mendeteksi objek yang tidak diperlukan.

---

# 31. Parameter Awal yang Disarankan

## SteeringAgent

\`\`\`text
Use Target             = true

Max Speed              = 4
Max Acceleration       = 8
Turn Speed             = 8

Slow Radius            = 4
Stop Radius            = 1.5

Wander Speed           = 2.5
Wander Change Interval = 1.5
Wander Angle Change    = 45

Avoidance Weight       = 2.5
\`\`\`

## SteeringSensor

\`\`\`text
Sensor Distance        = 3
Sensor Radius          = 0.5
Sensor Height          = 0.5
Forward Bias           = 0.5

Obstacle Mask          = Obstacle
\`\`\`

Parameter tersebut hanya titik awal.

Mahasiswa dianjurkan melakukan tuning sendiri.

---

# 32. Uji Tahap 1 — Arrive Tanpa Obstacle

Untuk memahami behavior secara bertahap, sementara letakkan Player dan NPC pada area tanpa obstacle.

Klik:

\`\`\`text
Play
\`\`\`

Gerakkan Player menggunakan:

\`\`\`text
W A S D
\`\`\`

atau:

\`\`\`text
Arrow Keys
\`\`\`

Yang seharusnya terjadi:

\`\`\`text
NPC
 ↓
bergerak menuju Player
 ↓
semakin dekat
 ↓
melambat
 ↓
berhenti
\`\`\`

---

# 33. Mengapa Menggunakan Arrive, Bukan Seek Murni?

Seek sederhana:

\`\`\`csharp
desiredVelocity =
    direction * maxSpeed;
\`\`\`

Agent selalu menggunakan kecepatan maksimum.

Saat mendekati target:

\`\`\`text
NPC ─────────→ Target
\`\`\`

agent berpotensi:

\`\`\`text
melewati target
→ berbalik
→ melewati lagi
→ berbalik lagi
\`\`\`

Akibatnya muncul:

\`\`\`text
overshoot
jitter
bolak-balik
\`\`\`

Arrive memperbaiki masalah tersebut.

---

# 34. Cara Kerja Arrive

NPC dibagi menjadi tiga kondisi.

\`\`\`text
                   TARGET
                     ●

                STOP RADIUS
               ─────────────

                SLOW RADIUS
          ─────────────────────

             FULL SPEED AREA
\`\`\`

## Jauh dari target

\`\`\`text
distance >= slowRadius
\`\`\`

Kecepatan:

\`\`\`text
maxSpeed
\`\`\`

## Masuk slow radius

\`\`\`text
stopRadius < distance < slowRadius
\`\`\`

Kecepatan berkurang secara bertahap.

## Masuk stop radius

\`\`\`text
distance <= stopRadius
\`\`\`

Desired velocity:

\`\`\`csharp
Vector3.zero
\`\`\`

---

# 35. Perbaikan Formula Arrive

Versi sederhana pada materi dapat menggunakan:

\`\`\`csharp
desiredSpeed =
    maxSpeed * (distance / slowRadius);
\`\`\`

Pada praktikum ini digunakan formula sedikit diperbaiki:

\`\`\`csharp
float normalizedDistance =
    (distance - stopRadius) /
    (slowRadius - stopRadius);
\`\`\`

Kemudian:

\`\`\`csharp
desiredSpeed =
    maxSpeed *
    Mathf.Clamp01(normalizedDistance);
\`\`\`

Alasannya:

ketika agent mencapai:

\`\`\`text
stopRadius
\`\`\`

kecepatan target benar-benar mendekati:

\`\`\`text
0
\`\`\`

bukan masih memiliki sebagian kecepatan maksimum.

---

# 36. Uji Parameter Arrive

Saat Play Mode, coba:

\`\`\`text
Slow Radius = 8
\`\`\`

Perhatikan NPC mulai melambat lebih jauh.

Kemudian coba:

\`\`\`text
Slow Radius = 2
\`\`\`

NPC akan mempertahankan kecepatan tinggi lebih lama.

Eksperimen juga dengan:

\`\`\`text
Stop Radius = 0.5
Stop Radius = 2
Stop Radius = 4
\`\`\`

---

# 37. Max Speed

Parameter:

\`\`\`text
Max Speed
\`\`\`

menentukan batas maksimum kecepatan agent.

Contoh:

\`\`\`text
2 → lambat
4 → sedang
8 → cepat
\`\`\`

Dalam model Unity sederhana ini dapat dianggap sebagai:

\`\`\`text
Unity units / second
\`\`\`

---

# 38. Max Acceleration

Parameter:

\`\`\`text
Max Acceleration
\`\`\`

mengatur seberapa cepat velocity berubah.

Kode:

\`\`\`csharp
Vector3.MoveTowards(
    velocity,
    desiredVelocity,
    maxAcceleration * Time.deltaTime
);
\`\`\`

Jika:

\`\`\`text
Max Acceleration tinggi
\`\`\`

NPC cepat mencapai kecepatan yang diinginkan.

Jika:

\`\`\`text
Max Acceleration rendah
\`\`\`

NPC membutuhkan waktu lebih lama untuk mempercepat atau memperlambat.

Hal ini membuat movement mempunyai kesan:

\`\`\`text
massa
momentum
inersia sederhana
\`\`\`

---

# 39. Turn Speed

Parameter:

\`\`\`text
Turn Speed
\`\`\`

menentukan seberapa cepat NPC menghadap ke arah velocity.

Rotasi menggunakan:

\`\`\`csharp
Quaternion.Slerp()
\`\`\`

Jika terlalu rendah:

\`\`\`text
NPC berbelok lambat
\`\`\`

Jika terlalu tinggi:

\`\`\`text
NPC berputar hampir seketika
\`\`\`

---

# 40. Uji Tahap 2 — Obstacle Avoidance

Sekarang letakkan obstacle di antara:

\`\`\`text
NPC
\`\`\`

dan:

\`\`\`text
Player
\`\`\`

Contoh:

\`\`\`text
NPC ●

      █████
      █████
      █████

                  ● Player
\`\`\`

Klik:

\`\`\`text
Play
\`\`\`

NPC seharusnya mendeteksi obstacle dan mencoba membelok.

---

# 41. Alur Obstacle Avoidance

Secara konseptual:

\`\`\`text
Desired Movement
       ↓
SphereCast
       ↓
Obstacle?
       │
   ┌───┴────┐
   │        │
  NO       YES
   │        │
   │      hit.normal
   │        │
   │   Avoid Direction
   │        │
   └────┬───┘
        ↓
Combined Direction
        ↓
Movement
\`\`\`

---

# 42. Weighted Blending

Pada script:

\`\`\`csharp
Vector3 combinedDirection =
    checkDirection +
    avoidanceDirection *
    avoidanceWeight;
\`\`\`

Secara konseptual:

\`\`\`text
Final Steering
=
Movement Steering
+
Obstacle Avoidance × Weight
\`\`\`

Jika:

\`\`\`text
Avoidance Weight = 0
\`\`\`

NPC hampir mengabaikan obstacle.

Jika:

\`\`\`text
Avoidance Weight = 1
\`\`\`

keduanya mempunyai pengaruh serupa.

Jika:

\`\`\`text
Avoidance Weight = 3
\`\`\`

avoidance mempunyai pengaruh lebih kuat.

---

# 43. Mengapa Avoidance Weight Biasanya Lebih Tinggi?

Tujuan utama:

\`\`\`text
Seek / Arrive
\`\`\`

adalah mencapai target.

Tetapi:

\`\`\`text
Obstacle Avoidance
\`\`\`

berhubungan dengan keselamatan movement lokal.

Jika bobotnya terlalu kecil:

\`\`\`text
Arrive →→→→→ Player
       ↓
   obstacle
       ↓
avoidance terlalu lemah
       ↓
NPC menabrak
\`\`\`

Karena itu obstacle avoidance umumnya memperoleh prioritas atau bobot lebih tinggi.

---

# 44. Uji Sensor Distance

Coba:

\`\`\`text
Sensor Distance = 1
\`\`\`

NPC baru mengetahui obstacle ketika sudah dekat.

Akibatnya:

\`\`\`text
reaksi terlambat
\`\`\`

Kemudian coba:

\`\`\`text
Sensor Distance = 5
\`\`\`

NPC mendeteksi obstacle lebih awal.

Tetapi terlalu panjang juga dapat menyebabkan:

\`\`\`text
NPC terlalu cepat bereaksi
\`\`\`

atau:

\`\`\`text
memilih menghindar walaupun obstacle masih jauh
\`\`\`

---

# 45. Uji Sensor Radius

Coba:

\`\`\`text
Sensor Radius = 0.1
\`\`\`

SphereCast hampir seperti Raycast.

Kemudian:

\`\`\`text
Sensor Radius = 1
\`\`\`

sensor menjadi lebih lebar.

Jika radius terlalu kecil:

\`\`\`text
NPC dapat menyerempet obstacle
\`\`\`

Jika terlalu besar:

\`\`\`text
NPC merasa obstacle lebih besar daripada sebenarnya
\`\`\`

---

# 46. Uji Tahap 3 — Wander

Pada Play Mode atau sebelum Play, ubah:

\`\`\`text
Use Target = false
\`\`\`

Sekarang NPC tidak menggunakan Player sebagai target.

NPC seharusnya masuk behavior:

\`\`\`text
Wander
\`\`\`

dan bergerak secara autonomous.

---

# 47. Cara Kerja Wander

Implementasi praktikum sengaja dibuat relatif sederhana.

Agent memiliki:

\`\`\`text
wanderDirection
\`\`\`

Setelah waktu:

\`\`\`text
wanderChangeInterval
\`\`\`

terpenuhi, arah berubah sedikit secara random.

Contoh:

\`\`\`text
        ↗
      ↗
NPC ● → → →
        ↘
\`\`\`

Bukan memilih arah global sepenuhnya random setiap frame.

Hal ini penting.

Jika direction dibuat random setiap frame:

\`\`\`text
← ↑ ↘ → ↓ ↖ ↑ →
\`\`\`

movement akan terlihat:

\`\`\`text
bergetar
tidak natural
\`\`\`

Dengan perubahan berkala:

\`\`\`text
→ → → ↗ ↗ ↗ ↑ ↑
\`\`\`

gerakan terlihat lebih halus.

---

# 48. Wander Angle Change

Parameter:

\`\`\`text
Wander Angle Change
\`\`\`

mengatur seberapa besar perubahan arah.

Contoh:

\`\`\`text
15°
\`\`\`

gerakan cenderung lurus.

\`\`\`text
45°
\`\`\`

gerakan cukup bervariasi.

\`\`\`text
120°
\`\`\`

NPC dapat berubah arah secara sangat tajam.

---

# 49. Wander Change Interval

Contoh:

\`\`\`text
0.2
\`\`\`

arah berubah sangat sering.

\`\`\`text
1.5
\`\`\`

lebih stabil.

\`\`\`text
5
\`\`\`

NPC berjalan relatif lama ke arah yang sama.

Tujuannya adalah melihat hubungan antara:

\`\`\`text
parameter
        ↓
karakter movement
\`\`\`

---

# 50. Wander + Obstacle Avoidance

Walaupun berada dalam Wander:

\`\`\`text
Wander
   ↓
Desired Velocity
   ↓
Obstacle Sensor
   ↓
Obstacle?
   ↓
Avoidance
\`\`\`

Dengan demikian NPC tetap mencoba menghindari obstacle.

Ini menunjukkan bahwa beberapa behavior dapat digunakan bersama.

---

# 51. Menambahkan SteeringDebug.cs

Untuk visual debugging tambahan, buat:

\`\`\`text
Assets/Scripts/SteeringDebug.cs
\`\`\`

Isi:

\`\`\`csharp
using UnityEngine;

public class SteeringDebug : MonoBehaviour
{
    [SerializeField]
    private SteeringAgent agent;

    [SerializeField]
    private float velocityScale = 1f;

    private void Reset()
    {
        agent = GetComponent<SteeringAgent>();
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null)
        {
            return;
        }

        Vector3 start =
            transform.position + Vector3.up;

        Vector3 end =
            start +
            agent.Velocity * velocityScale;

        Gizmos.DrawLine(start, end);

        Gizmos.DrawWireSphere(
            end,
            0.15f
        );
    }
}
\`\`\`

Pasang script pada:

\`\`\`text
NPC
\`\`\`

---

# 52. Mengapa Gizmos Penting?

Game AI memiliki banyak nilai internal yang tidak terlihat.

Contoh:

\`\`\`text
velocity
sensor radius
slow radius
stop radius
detection direction
target
\`\`\`

Tanpa visual debugging, kita hanya melihat:

\`\`\`text
NPC bergerak aneh
\`\`\`

tetapi tidak mengetahui penyebabnya.

Dengan Gizmos, kita dapat melihat:

\`\`\`text
internal state
      ↓
visual representation
      ↓
lebih mudah debugging
\`\`\`

---

# 53. Melihat Gizmos

Klik GameObject:

\`\`\`text
NPC
\`\`\`

pada Scene View.

Pastikan:

\`\`\`text
Gizmos
\`\`\`

aktif.

Saat NPC dipilih akan terlihat visualisasi:

\`\`\`text
Slow Radius
Stop Radius
Sensor
Velocity
Target Line
\`\`\`

---

# 54. Eksperimen Parameter

Setelah semua behavior bekerja, lakukan eksperimen.

## Eksperimen A

\`\`\`text
Max Speed = 8
Max Acceleration = 2
\`\`\`

Pertanyaan:

- Apakah NPC lambat mencapai kecepatan maksimum?
- Apakah NPC lebih mudah overshoot?

---

## Eksperimen B

\`\`\`text
Max Speed = 4
Max Acceleration = 20
\`\`\`

Pertanyaan:

- Apakah respons NPC menjadi lebih cepat?
- Apakah gerak terasa lebih kaku?

---

## Eksperimen C

\`\`\`text
Slow Radius = 8
Stop Radius = 1
\`\`\`

Amati karakter Arrive.

---

## Eksperimen D

\`\`\`text
Sensor Distance = 1
\`\`\`

Kemudian:

\`\`\`text
Sensor Distance = 5
\`\`\`

Bandingkan kemampuan obstacle avoidance.

---

## Eksperimen E

\`\`\`text
Avoidance Weight = 0.5
\`\`\`

Kemudian:

\`\`\`text
Avoidance Weight = 4
\`\`\`

Amati perubahan jalur NPC.

---

# 55. Menambahkan Beberapa NPC

Setelah satu NPC bekerja, duplicate:

\`\`\`text
NPC
\`\`\`

menggunakan:

\`\`\`text
Ctrl + D
\`\`\`

Buat misalnya:

\`\`\`text
NPC
NPC (1)
NPC (2)
NPC (3)
\`\`\`

Semua dapat menggunakan Player yang sama sebagai Target.

Kemungkinan akan terlihat:

\`\`\`text
NPC
  \\
NPC → Player
  /
NPC
\`\`\`

Tetapi NPC dapat saling bertumpuk.

Ini merupakan motivasi untuk behavior:

\`\`\`text
Separation
\`\`\`

yang dipelajari secara konsep pada materi.

---

# 56. Bonus — Separation Sederhana

Bagian ini bersifat **pengembangan tambahan**, bukan requirement utama Praktikum 3.

Tambahkan pada \`SteeringAgent.cs\`:

\`\`\`csharp
[Header("Separation")]

[SerializeField]
private LayerMask agentMask;

[SerializeField]
private float separationRadius = 1.5f;

[SerializeField]
private float separationWeight = 1.5f;
\`\`\`

Tambahkan fungsi:

\`\`\`csharp
private Vector3 CalculateSeparation()
{
    Collider[] neighbors =
        Physics.OverlapSphere(
            transform.position,
            separationRadius,
            agentMask
        );

    Vector3 separation =
        Vector3.zero;

    int count = 0;

    foreach (Collider neighbor in neighbors)
    {
        if (neighbor.transform == transform)
        {
            continue;
        }

        Vector3 away =
            transform.position -
            neighbor.transform.position;

        away.y = 0f;

        float sqrDistance =
            away.sqrMagnitude;

        if (sqrDistance > 0.001f)
        {
            separation +=
                away.normalized /
                Mathf.Max(sqrDistance, 0.01f);

            count++;
        }
    }

    if (count > 0)
    {
        separation /= count;
    }

    return separation;
}
\`\`\`

Kemudian behavior tersebut dapat digabung dengan desired velocity.

Secara konseptual:

\`\`\`text
Arrive
+
Obstacle Avoidance
+
Separation
\`\`\`

---

# 57. Konsep Emergent Behavior

Jika beberapa agent diberikan aturan sederhana seperti:

\`\`\`text
jangan terlalu dekat
\`\`\`

maka tanpa instruksi:

\`\`\`text
"buat formasi tertentu"
\`\`\`

dapat muncul pola kelompok.

Inilah yang disebut:

\`\`\`text
Emergent Behavior
\`\`\`

Konsep ini menjadi dasar algoritma seperti:

\`\`\`text
Boids
├── Separation
├── Alignment
└── Cohesion
\`\`\`

Flocking lengkap tidak menjadi requirement utama praktikum ini.

---

# 58. Perbedaan Steering dan Pathfinding

Praktikum ini tidak menggunakan:

\`\`\`text
NavMeshAgent
\`\`\`

karena tujuan utamanya adalah memahami algoritma movement.

Steering menjawab:

\`\`\`text
Bagaimana saya bergerak sekarang?
\`\`\`

Sedangkan pathfinding menjawab:

\`\`\`text
Jalur mana yang harus saya gunakan
untuk mencapai tujuan?
\`\`\`

Contoh:

\`\`\`text
               ███████████
               █         █
NPC ●          █         █        ● Target
               █         █
               ███████████
\`\`\`

Obstacle avoidance lokal belum tentu mampu mengetahui bahwa NPC harus berjalan jauh mengelilingi gedung.

Untuk masalah seperti itu diperlukan:

\`\`\`text
Pathfinding
\`\`\`

yang akan dipelajari pada pertemuan berikutnya.

---

# 59. Keterbatasan Obstacle Avoidance Praktikum

SphereCast sederhana adalah:

\`\`\`text
Local Obstacle Avoidance
\`\`\`

bukan:

\`\`\`text
Global Navigation
\`\`\`

Artinya NPC hanya mengetahui:

\`\`\`text
"ada obstacle di depan"
\`\`\`

NPC tidak mengetahui:

\`\`\`text
"jalan terbaik menuju target berada di sebelah kanan gedung"
\`\`\`

Akibatnya NPC masih dapat mengalami:

\`\`\`text
terjebak di sudut
berputar dekat tembok
gagal melewati obstacle berbentuk U
\`\`\`

Hal tersebut bukan selalu bug implementasi.

Itu adalah keterbatasan algoritma lokal.

---

# 60. Kesalahan Umum 1 — NPC Tidak Bergerak

Periksa:

\`\`\`text
SteeringAgent aktif?
\`\`\`

Kemudian:

\`\`\`text
Target sudah diisi?
\`\`\`

Pastikan:

\`\`\`text
Use Target = true
\`\`\`

Jika menggunakan Wander:

\`\`\`text
Use Target = false
\`\`\`

---

# 61. Kesalahan Umum 2 — NPC Tidak Mendeteksi Obstacle

Periksa:

\`\`\`text
Obstacle mempunyai Collider?
\`\`\`

Pastikan layer:

\`\`\`text
Obstacle
\`\`\`

dan:

\`\`\`text
SteeringSensor
→ Obstacle Mask
→ Obstacle
\`\`\`

---

# 62. Kesalahan Umum 3 — NPC Masih Menabrak

Kemungkinan:

\`\`\`text
Sensor Distance terlalu kecil
\`\`\`

atau:

\`\`\`text
Avoidance Weight terlalu rendah
\`\`\`

Coba:

\`\`\`text
Sensor Distance = 4
Avoidance Weight = 3
\`\`\`

Jika NPC sangat cepat, sensor juga perlu lebih panjang.

---

# 63. Kesalahan Umum 4 — NPC Bergetar Dekat Player

Periksa:

\`\`\`text
Stop Radius
\`\`\`

Jika terlalu kecil, agent terus melakukan koreksi posisi.

Coba:

\`\`\`text
Stop Radius = 1.5
\`\`\`

dan:

\`\`\`text
Slow Radius = 4
\`\`\`

---

# 64. Kesalahan Umum 5 — NPC Bergerak Sangat Lambat

Periksa:

\`\`\`text
Max Speed
Max Acceleration
\`\`\`

Misalnya:

\`\`\`text
Max Speed = 4
Max Acceleration = 8
\`\`\`

---

# 65. Kesalahan Umum 6 — NPC Tidak Berputar

Pastikan:

\`\`\`text
Turn Speed > 0
\`\`\`

dan NPC mempunyai velocity.

Rotasi hanya dilakukan jika:

\`\`\`csharp
horizontalVelocity.sqrMagnitude > 0.001f
\`\`\`

Hal tersebut mencegah:

\`\`\`csharp
Quaternion.LookRotation(Vector3.zero)
\`\`\`

---

# 66. Kesalahan Umum 7 — NPC Menengadah atau Miring

Pastikan semua direction movement ground dibuat:

\`\`\`csharp
direction.y = 0f;
\`\`\`

Praktikum ini mengasumsikan movement terjadi pada bidang:

\`\`\`text
XZ
\`\`\`

---

# 67. Kesalahan Umum 8 — Movement Berbeda pada FPS Berbeda

Pastikan movement menggunakan:

\`\`\`csharp
Time.deltaTime
\`\`\`

Contoh benar:

\`\`\`csharp
transform.position +=
    velocity * Time.deltaTime;
\`\`\`

Bukan:

\`\`\`csharp
transform.position += velocity;
\`\`\`

---

# 68. Update() dan FixedUpdate()

Praktikum ini memindahkan NPC menggunakan:

\`\`\`csharp
transform.position
\`\`\`

sehingga digunakan:

\`\`\`csharp
Update()
\`\`\`

Jika pada pengembangan berikutnya digunakan:

\`\`\`text
Rigidbody
AddForce
physics movement
\`\`\`

perhitungan movement fisika sebaiknya dilakukan melalui:

\`\`\`csharp
FixedUpdate()
\`\`\`

---

# 69. Jangan Mencampur Transform dengan Rigidbody Dinamis

Jika NPC memiliki Rigidbody dinamis dan juga menggunakan:

\`\`\`csharp
transform.position += ...
\`\`\`

interaksi fisika dapat menjadi tidak konsisten.

Untuk praktikum dasar ini:

\`\`\`text
NPC Steering
→ Transform movement
\`\`\`

Jika ingin menggunakan physics-based movement:

\`\`\`text
NPC
→ Rigidbody
→ MovePosition / AddForce
→ FixedUpdate
\`\`\`

Gunakan satu pendekatan secara konsisten.

---

# 70. Checklist Pengujian

Sebelum praktikum dianggap selesai, pastikan:

- [ ] Scene \`Praktikum03_Steering\` sudah dibuat.
- [ ] Ground tersedia.
- [ ] Player dapat digerakkan.
- [ ] Player menggunakan WASD atau Arrow Keys.
- [ ] NPC mempunyai \`SteeringAgent\`.
- [ ] NPC mempunyai \`SteeringSensor\`.
- [ ] Target NPC menunjuk ke Player.
- [ ] NPC dapat bergerak menuju Player.
- [ ] NPC melambat ketika mendekati Player.
- [ ] NPC berhenti di dalam Stop Radius.
- [ ] NPC menghadap ke arah movement.
- [ ] Obstacle memiliki Collider.
- [ ] Obstacle menggunakan Layer \`Obstacle\`.
- [ ] Obstacle Mask sudah diatur.
- [ ] NPC mendeteksi obstacle.
- [ ] NPC mencoba menghindari obstacle.
- [ ] \`Use Target = false\` menyebabkan Wander.
- [ ] Wander tetap menggunakan obstacle avoidance.
- [ ] Gizmos dapat digunakan untuk debugging.
- [ ] Parameter AI dapat diubah melalui Inspector.

---

# 71. Eksperimen Wajib Mahasiswa

Lakukan minimal tiga eksperimen.

## Eksperimen 1 — Max Speed

Bandingkan:

\`\`\`text
Max Speed = 2
Max Speed = 4
Max Speed = 8
\`\`\`

Catat perubahan movement.

---

## Eksperimen 2 — Slow Radius

Bandingkan:

\`\`\`text
Slow Radius = 2
Slow Radius = 5
Slow Radius = 10
\`\`\`

Amati kapan NPC mulai melambat.

---

## Eksperimen 3 — Obstacle Avoidance

Bandingkan:

\`\`\`text
Avoidance Weight = 0.5
Avoidance Weight = 2
Avoidance Weight = 5
\`\`\`

Amati kemampuan NPC menghindari obstacle.

---

# 72. Pertanyaan Analisis

Jawab pertanyaan berikut.

1. Apa perbedaan **Seek** dan **Arrive**?

2. Mengapa Seek murni dapat menyebabkan agent melewati target?

3. Apa fungsi:

\`\`\`csharp
direction.normalized
\`\`\`

4. Mengapa movement dikalikan dengan:

\`\`\`csharp
Time.deltaTime
\`\`\`

5. Apa perbedaan:

\`\`\`text
Raycast
\`\`\`

dan:

\`\`\`text
SphereCast
\`\`\`

6. Mengapa obstacle menggunakan Layer tersendiri?

7. Apa fungsi:

\`\`\`text
Avoidance Weight
\`\`\`

8. Mengapa NPC masih dapat terjebak pada obstacle tertentu walaupun memiliki obstacle avoidance?

9. Apa perbedaan:

\`\`\`text
Obstacle Avoidance
\`\`\`

dan:

\`\`\`text
Pathfinding
\`\`\`

10. Apa akibat \`Max Acceleration\` terlalu kecil?

11. Apa akibat \`Slow Radius\` terlalu besar?

12. Mengapa visual debugging penting dalam pengembangan Game AI?

---

# 73. Tugas Pengembangan

Setelah menyelesaikan praktikum dasar, pilih salah satu pengembangan berikut.

### Level 1

Tambahkan perubahan warna NPC:

\`\`\`text
Arrive → merah
Wander → biru
Avoiding → kuning
\`\`\`

### Level 2

Tambahkan:

\`\`\`text
Flee
\`\`\`

ketika Player terlalu dekat.

### Level 3

Tambahkan:

\`\`\`text
Separation
\`\`\`

agar beberapa NPC tidak bertumpuk.

### Level 4

Tambahkan behavior:

\`\`\`text
Pursue
\`\`\`

menggunakan predicted player position.

---

# 74. Tantangan — Animal NPC

Kembangkan NPC menjadi animal agent.

Behavior:

\`\`\`text
Player jauh
    ↓
Wander

Player dekat
    ↓
Flee

Semua kondisi
    ↓
Obstacle Avoidance
\`\`\`

Struktur decision:

\`\`\`text
distanceToPlayer < panicRadius?
           │
       ┌───┴───┐
       │       │
      YES      NO
       │       │
      Flee   Wander
       │       │
       └───┬───┘
           ↓
    Obstacle Avoidance
           ↓
        Movement
\`\`\`

---

# 75. Catatan Teknis dan Perbaikan Modul

## Catatan 1 — Praktikum sebaiknya menggunakan Arrive sebagai behavior utama

Walaupun Seek merupakan behavior dasar yang sangat penting, penggunaan:

\`\`\`text
Seek saja
\`\`\`

kurang ideal sebagai produk akhir praktikum karena NPC mudah overshoot.

Urutan pembelajaran yang disarankan tetap:

\`\`\`text
Seek
 ↓
lihat kelemahannya
 ↓
Arrive
\`\`\`

Sedangkan implementasi final menggunakan:

\`\`\`text
Arrive
\`\`\`

---

## Catatan 2 — Gunakan SphereCast untuk NPC

Materi memperkenalkan:

\`\`\`text
Raycast
SphereCast
\`\`\`

Untuk obstacle avoidance karakter, SphereCast lebih sesuai karena agent memiliki lebar.

Raycast tetap bagus untuk menjelaskan konsep sensor.

---

## Catatan 3 — Hindari obstacle dengan steering, bukan teleport

Jangan menggunakan solusi:

\`\`\`csharp
transform.position = posisiAman;
\`\`\`

karena tujuan praktikum adalah memahami **steering**.

Agent seharusnya:

\`\`\`text
mendeteksi obstacle
        ↓
mengubah desired direction
        ↓
membelok
\`\`\`

---

## Catatan 4 — Movement AI tidak menggantikan pathfinding

Mahasiswa perlu memahami bahwa:

\`\`\`text
SphereCast + Avoidance
\`\`\`

bukan pengganti:

\`\`\`text
A*
NavMesh
NavMeshAgent
\`\`\`

Praktikum ini sengaja menggunakan masalah movement lokal sebelum masuk ke navigation pada pertemuan berikutnya.

---

## Catatan 5 — Hindari membuat terlalu banyak behavior sekaligus

Untuk Praktikum 3 wajib, cukup gunakan:

\`\`\`text
Arrive
Wander
Obstacle Avoidance
\`\`\`

Sedangkan:

\`\`\`text
Pursue
Evade
Separation
Alignment
Cohesion
\`\`\`

lebih baik digunakan sebagai eksperimen atau tugas pengembangan.

Tujuannya agar mahasiswa terlebih dahulu benar-benar memahami pipeline:

\`\`\`text
Steering
→ Desired Velocity
→ Velocity
→ Movement
\`\`\`

---

## Catatan 6 — Gunakan parameter Inspector

Jangan hard-code seluruh parameter.

Contoh yang kurang baik:

\`\`\`csharp
float speed = 4f;
\`\`\`

lebih baik:

\`\`\`csharp
[SerializeField]
private float maxSpeed = 4f;
\`\`\`

Karena Game AI sangat bergantung pada:

\`\`\`text
Parameter Tuning
\`\`\`

---

# 76. Hubungan dengan Materi Berikutnya

Pada Praktikum 3:

\`\`\`text
Target
   ↓
Steering
   ↓
Local Movement
\`\`\`

Pada pertemuan berikutnya akan dipelajari:

\`\`\`text
Target
   ↓
Pathfinding
   ↓
Path
   ↓
Waypoint
   ↓
Steering
   ↓
Movement
\`\`\`

Sehingga:

\`\`\`text
Pathfinding
\`\`\`

menentukan:

> Ke mana agent harus lewat?

Sedangkan:

\`\`\`text
Steering
\`\`\`

menentukan:

> Bagaimana agent bergerak mengikuti tujuan tersebut?

---

# 77. Ringkasan

Pada praktikum ini telah dibuat:

\`\`\`text
Autonomous Steering Agent
│
├── Player Controller
│
├── Arrive
│   ├── Max Speed
│   ├── Slow Radius
│   └── Stop Radius
│
├── Dynamic Velocity
│   └── Max Acceleration
│
├── Smooth Rotation
│   └── Turn Speed
│
├── Wander
│
├── Obstacle Sensor
│   ├── SphereCast
│   ├── LayerMask
│   └── RaycastHit
│
├── Obstacle Avoidance
│
└── Gizmos Debugging
\`\`\`

Konsep terpenting:

\`\`\`text
Target / Environment
        ↓
Steering Behavior
        ↓
Desired Velocity
        ↓
Acceleration
        ↓
Velocity
        ↓
Position
        ↓
Rotation
\`\`\`

Dengan kombinasi aturan sederhana tersebut, NPC mulai menunjukkan **autonomous movement** yang terlihat lebih natural dibanding movement langsung menuju target.

---

# 78. Kesimpulan

Praktikum ini menunjukkan bahwa movement NPC yang terlihat cerdas tidak selalu memerlukan algoritma yang sangat kompleks.

Behavior sederhana seperti:

\`\`\`text
Arrive
+
Wander
+
Obstacle Avoidance
\`\`\`

sudah dapat menghasilkan agent yang:

\`\`\`text
bergerak mandiri
mengejar target
melambat secara natural
menghindari obstacle
menjelajah lingkungan
\`\`\`

Konsep tersebut menjadi fondasi untuk materi berikutnya:

\`\`\`text
Pathfinding
Navigation
NavMesh
Flocking
Behavior Tree
dan Game AI yang lebih kompleks
\`\`\``,Ec=`# MODUL PRAKTIKUM 4 — PATHFINDING & NAVIGATION
## Game Cerdas — S1 Teknik Informatika
### Unity 6 + C#

---

#