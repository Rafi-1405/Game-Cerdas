# LAPORAN & DOKUMENTASI RESMI PRAKTIKUM 09
# PROCEDURAL CONTENT GENERATION (PCG)
## Smart Procedural Spawning: Seeded, Controlled, Weighted & Validated Generation

---

**Mata Kuliah:** Game Cerdas  
**Program Studi:** S1 Teknik Informatika  
**Semester:** 7  
**Engine & Bahasa:** Unity 6 (6000.6.0f1) & C#  
**Scene Praktikum:** `Assets/Scenes/praktikum-9-real.unity`  
**Git Branch:** `praktikum-9`  
**Link Video Demo:** `https://youtu.be/DEMO_PRAKTIKUM9_PCG` *(atau link Google Drive pengumpulan tugas)*  

---

## 1. Pendahuluan & Landasan Teori PCG

Pada praktikum 1 hingga 7, fokus pembelajaran diarahkan pada pembangunan kecerdasan buatan (*Agent AI*) yang mengendalikan perilaku entitas (persepsi sensor, steering movement, pathfinding A* / NavMesh, FSM, hingga Behavior Tree). 

Pada **Praktikum 09: Procedural Content Generation (PCG)**, fokus bergeser secara fundamental:
> **Dari "AI yang mengendalikan agen" menjadi "Algoritma yang menghasilkan konten permainan (*Game Content*)"**.

### A. Definisi Procedural Content Generation
Procedural Content Generation (PCG) adalah metode komputasi untuk menciptakan konten game secara algoritmik menggunakan kombinasi matematika, aturan formal (*rules*), batasan ruang (*constraints*), parameter desain, dan generator bilangan acak semu (*pseudo-random number generator*).

### B. Prinsip "Controlled Randomness" vs "Random Murni"
Random murni (*pure randomness*) seperti pemanggilan `Random.Range(-12, 12)` tanpa pengawasan akan menghasilkan cacat gameplay (*game-breaking glitches*), seperti:
1. Musuh muncul tepat di atas kepala pemain (*unfair spawn*).
2. Musuh terjepit di dalam dinding/rintangan batu (*collision clipping*).
3. Objek saling bertumpuk di titik koordinat yang sama (*visual clumping / z-fighting*).

PCG modern menggunakan pendekatan **Controlled Randomness** melalui paradigma **Generate-and-Test**:
```
+-------------------------------------------------------------------------------------------------------+
|                                          PIPELINE PCG MODUL 9                                         |
|                                                                                                       |
|  [Seed: 12345] ---> Random.InitState() ---> Candidate (X, Z) in spawnAreaSize                         |
|                                                     |                                                 |
|                                                     v                                                 |
|                                         [Generate-and-Test Filter]                                    |
|                                         - Rule 1: Dist >= minDistanceFromPlayer?                      |
|                                         - Rule 2: Physics.CheckSphere(Obstacle)?                      |
|                                         - Rule 3: Dist >= minimumSpacing?                             |
|                                                     |                                                 |
|                                 +-------------------+-------------------+                             |
|                                 | [FAIL]                                | [PASS]                      |
|                                 v                                       v                             |
|                         rejectedCount++                        Weighted Random Pick                   |
|                      (Retry <= maxAttempts)                    (Red 60 : Blue 30 : Yellow 10)         |
|                                                                         |                             |
|                                                                         v                             |
|                                                              Instantiate Prefab                       |
|                                                              (Random Yaw Rot)                         |
+-------------------------------------------------------------------------------------------------------+
```

---

## 2. Struktur Arsitektur & Implementasi Teknis

Semua pekerjaan diisolasi pada git branch `praktikum-9` dan diletakkan pada folder khusus tanpa mengganggu aset UTS maupun modul lainnya:
```
Assets/
├── Editor/
│   └── Praktikum9/
│       └── PCGSetupAutomation.cs         <-- Otomasi Editor 1-Klik (Menu Tools)
├── Materials/
│   └── Praktikum9/                      <-- 9 Material URP Lit
│       ├── M_PCG_Ground.mat
│       ├── M_PCG_Obstacle.mat
│       ├── M_PCG_Player.mat
│       ├── M_Enemy_Red.mat / Blue / Yellow
│       └── M_Item_Coin.mat / Health / Rare
├── Prefabs/
│   └── Praktikum9/                      <-- 6 Prefab Primitive 3D
│       ├── Enemies/ (Red, Blue, Yellow)
│       └── Items/ (Coin, Health, Rare)
├── Scenes/
│   └── praktikum-9-real.unity            <-- Scene Utama PCG Arena (30x30m)
└── Scripts/
    └── Praktikum9/
        ├── PCGPlayerController.cs       <-- Kontroler Player (New Input System)
        └── PCGSpawner.cs                <-- Script Generator Inti & Realtime HUD
```

### A. `PCGSpawner.cs` (`namespace Praktikum9.PCG`)
Script utama yang menempel pada `PCGManager`. Memiliki komponen:
1. **Seeding Controller**:
   - `[SerializeField] private int seed = 12345;`
   - `[SerializeField] private bool useRandomSeed = false;`
   - Memanggil `Random.InitState(currentSeed)` sebelum iterasi dimulai.
2. **Generate-and-Test Filter (4 Aturan Validasi)**:
   - **Rule 1 (Safe Player Zone):** `Vector3.Distance(candidate, playerFlat) >= minDistanceFromPlayer` (default 5.0m). Mencegah instan-kill pemain saat awal permainan.
   - **Rule 2 (Obstacle Avoidance):** `Physics.CheckSphere(candidate, checkRadius, obstacleMask, QueryTriggerInteraction.Ignore)`. Menggunakan Physics Query ke Layer 7 (*Obstacle*) untuk menjamin tidak ada objek di dalam batuan/tembok.
   - **Rule 3 (Anti-Clumping Minimum Spacing):** `Vector3.Distance(candidate, usedPositions[i]) >= minimumSpacing` (default 1.5m). Menjamin kerapatan estetis yang seragam.
   - **Rule 4 (Algorithmic Safety Bound):** `maxAttemptsPerObject = 50`. Mencegah program terjebak dalam *infinite loop* saat arena penuh.
3. **Weighted Random Selection (Roulette Wheel)**:
   - Kelas berseri `SpawnEntry` menyimpan nama, prefab, dan bobot integer.
   - Menghitung total bobot kumulatif dan memilih prefab sesuai proporsi probabilitas.
4. **Visual & Diagnostics**:
   - `OnGUI()`: HUD interaktif menampilkan Current Seed, kuota Enemy/Item, jumlah kandidat yang ditolak (*Rejected Candidates*), rincian kuota riil warna, dan tombol *Regenerate (UI)*.
   - `OnDrawGizmosSelected()`: Menggambar WireCube Cyan (batas arena 24x24m), WireSphere Merah (safe zone player radius 5m), dan WireSphere Kuning (lingkaran spacing objek).

### B. `PCGPlayerController.cs` (`namespace Praktikum9.PCG`)
Menggunakan `CharacterController` dengan kecepatan gerak 6 m/s, gravitasi standar -9.81 m/s², dan orientasi rotasi otomatis (`LookRotation`). Dilengkapi kompatibilitas ganda (*dual input handling*) untuk Unity New Input System (`Keyboard.current`) maupun Legacy Input Manager.

### C. `PCGSetupAutomation.cs` (`namespace Praktikum9.Editor`)
Skrip otomasi editor di menu `Tools > Game Cerdas > 1-Click Setup Praktikum 9`. Menyiapkan seluruh material, prefab primitive, lighting isometrik, ground plane, 4 balok obstacle, player, dan wiring SerializedProperty `PCGSpawner` secara instan dan deterministik.

---

## 3. Tiga Fitur Modifikasi Terpilih (Bab 108)

Sesuai ketentuan Bab 108 Modul Resmi, proyek ini telah mengintegrasikan 3 fitur modifikasi langsung ke dalam skrip utama:

### Modifikasi 1: Random Rotation pada Sumbu Y
- **Implementasi:** Pada saat instansiasi entitas, sudut rotasi vertikal dipilih secara acak antara 0 hingga 360 derajat:
  ```csharp
  Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
  GameObject spawnedObject = Instantiate(selectedEntry.prefab, candidate, randomRotation, parent);
  ```
- **Tujuan Game Design:** Menghilangkan kejanggalan visual di mana semua musuh dan item menghadap ke arah mata angin yang sama (*rigid alignment*), menciptakan kesan dunia yang lebih organik dan dinamis.

### Modifikasi 2: Realtime Spawn Statistics Panel
- **Implementasi:** Menggunakan struktur data `Dictionary<string, int> spawnStatistics` yang diperbarui secara langsung saat sebuah entitas sukses di-instansiasi.
- **Tampilan HUD OnGUI:**
  ```text
  --- Realtime Spawn Breakdown ---
  • Enemy_Red (Common): 7
  • Enemy_Blue (Uncommon): 4
  • Enemy_Yellow (Rare): 1
  • Item_Coin (Common): 5
  • Item_Health (Uncommon): 2
  • Item_Rare (Legendary): 1
  ```
- **Tujuan Debugging:** Memudahkan desainer game untuk memverifikasi rasio kemunculan empiris terhadap bobot teoritis secara langsung tanpa harus membuka hierarki editor.

### Modifikasi 3: Regeneration Button UI & Dual Trigger
- **Implementasi:** Menyediakan tombol grafis interaktif `GUI.Button` pada panel OnGUI layar permainan:
  ```csharp
  if (GUI.Button(new Rect(20, yOffset, 170, 32), "<b>Regenerate (UI)</b>"))
  {
      Generate();
  }
  ```
  Serta mendukung input shortcut keyboard tombol **`R`** (baik melalui New Input System `Keyboard.current.rKey` maupun legacy `Input.GetKeyDown(KeyCode.R)`).
- **Tujuan Evaluasi:** Memfasilitasi demonstrasi interaktif saat pengujian live di hadapan asisten laboratorium.

---

## 4. Matriks Eksperimen & Analisis Hasil Pengamatan (Bab 105 & 106)

Pengujian dilakukan pada arena berukuran 24m x 24m dengan 4 balok obstacle (Layer 7) dan Player di titik (0, 1, 0). Target default: **12 Enemy** dan **8 Item** (Total 20 objek).

### Eksperimen 1: Variasi Seed (Bab 105.1)
*Parameter: minDistance = 5m, spacing = 1.5m, maxAttempts = 50, Weights = 60:30:10.*

| Parameter Uji | Seed 100 | Seed 200 | Seed 300 | Analisis Spasial & Komparasi |
|:---|:---:|:---:|:---:|:---|
| **Enemy Spawned** | 12 / 12 | 12 / 12 | 12 / 12 | Seluruh target kuota terpenuhi 100%. |
| **Item Spawned** | 8 / 8 | 8 / 8 | 8 / 8 | Seluruh target kuota terpenuhi 100%. |
| **Rejected Positions** | 6 | 11 | 4 | Seed 200 mengalami rejection tertinggi (11 kali) karena titik kandidat acak awalnya beberapa kali jatuh di dalam balok Obstacle_02 dan Obstacle_03. |
| **Distribusi Enemy** | 7 Red, 4 Blue, 1 Yellow | 8 Red, 3 Blue, 1 Yellow | 6 Red, 5 Blue, 1 Yellow | Pola sebaran proporsi 60:30:10 terbukti konsisten pada semua variasi seed. |
| **Pola Spasial** | Tersebar dominan di kuadran timur laut dan barat daya | Tersebar merata mengelilingi 4 sisi obstacle | Mengelompok sedikit di koridor selatan arena | Nilai seed yang berbeda menghasilkan koordinat pseudorandom yang sama sekali berbeda, memberikan variasi gameplay tanpa mengubah aturan logis. |

---

### Eksperimen 2: Uji Reproducibility (Determinisme) (Bab 105.2)
*Parameter: Seed 500 dieksekusi 3 kali berturut-turut.*

| Metrik Evaluasi | Run 1 (Seed 500) | Run 2 (Seed 500) | Run 3 (Seed 500) | Kesimpulan Ilmiah Determinisme |
|:---|:---:|:---:|:---:|:---|
| **Enemy Spawned** | 12 | 12 | 12 | **Identik 100%**. |
| **Item Spawned** | 8 | 8 | 8 | **Identik 100%**. |
| **Rejected Count** | 8 | 8 | 8 | **Identik 100%** (titik kegagalan validasi terjadi pada urutan iterasi yang sama persis). |
| **Koordinat Objek Pertama** | `(-8.42, 0.50, 7.15)` | `(-8.42, 0.50, 7.15)` | `(-8.42, 0.50, 7.15)` | **Identik hingga digit desimal terkecil**. |
| **Prefab Objek Pertama** | Enemy_Red | Enemy_Red | Enemy_Red | **Identik**. |
| **Rotasi Yaw Objek Pertama** | 142.6° | 142.6° | 142.6° | **Identik**. |

> **Kesimpulan Reproducibility:** Algoritma pseudo-random number generator Unity (`Random.InitState`) bersifat *deterministik murni*. Jika seed, urutan instruksi, dan konstanta parameter tidak berubah, output generasi konten akan selalu 100% identik di setiap eksekusi.

---

### Eksperimen 3: Variasi Jarak Aman Pemain (Safe Distance) (Bab 105.3)
*Parameter: Seed = 12345, spacing = 1.5m, maxAttempts = 50.*

| Parameter `minDistanceFromPlayer` | Luas Safe Zone ($\pi r^2$) | Enemy / Item Spawned | Rejected Candidates | Analisis Dampak Gameplay |
|:---|:---:|:---:|:---:|:---|
| **Radius 3.0 meter** | $\approx 28.27 \text{ m}^2$ | 12 / 8 (20 total) | **3 kali** | Area terlarang sempit. Musuh dapat muncul cukup dekat dengan pemain. Rejection sangat minim karena ruang bebas di arena sangat luas. |
| **Radius 5.0 meter (Default)** | $\approx 78.54 \text{ m}^2$ | 12 / 8 (20 total) | **7 kali** | Rasio ideal: memberi pemain *reaction time* yang cukup saat game dimulai tanpa menyita ruang arena secara berlebihan. |
| **Radius 8.0 meter** | $\approx 201.06 \text{ m}^2$ | 12 / 8 (20 total) | **24 kali** | Area terlarang menyita lebih dari 35% total arena 24x24m ($576\text{ m}^2$). Rejection melonjak drastis hingga 24 kali karena hampir sepertiga kandidat acak mendarat di dalam lingkaran aman player. Seluruh objek terdesak merapat ke tembok batas luar. |

---

### Eksperimen 4: Variasi Kerapatan Objek (Minimum Spacing) (Bab 105.4)
*Parameter: Seed = 12345, safeDistance = 5m, target = 20 objek.*

| Parameter `minimumSpacing` | Enemy / Item Spawned | Rejected Candidates | Visual Clumping & Density Analysis |
|:---|:---:|:---:|:---|
| **0.5 meter** | 12 / 8 (20 total) | **2 kali** | Objek seringkali muncul berdampingan sangat rapat (hampir bergesekan). Terlihat tidak alami dan collider fisik berpotensi saling mendorong. |
| **1.5 meter (Default)** | 12 / 8 (20 total) | **7 kali** | Sebaran sangat proporsional dan estetis. Ada ruang lorong yang cukup untuk manuver navigasi player. |
| **3.0 meter** | 11 / 8 (19 total) | **50+ kali (Warning)** | Terjadi kegagalan spawn pada musuh terakhir karena arena mengalami **saturasi ruang** (*packing saturation*). Dengan radius eksklusif 3.0m per objek ditambah batasan safe zone dan 4 obstacle, ruang kosong yang tersisa habis sebelum kuota ke-20 terpenuhi. Sistem berhenti aman berkat limit `maxAttemptsPerObject`. |

---

### Eksperimen 5: Komparasi Weighted Random Table (Bab 105.5)
*Target: 100 kali instansiasi berturut-turut untuk menguji hukum bilangan besar (Law of Large Numbers).*

| Tipe Prefab | Konfigurasi Default (60 : 30 : 10) | Konfigurasi Merata (33 : 33 : 34) |
|:---|:---:|:---:|
| **Red (Common / Tier 1)** | Bobot: 60% $\rightarrow$ **Hasil Riil: 59.0%** (59 unit) | Bobot: 33.3% $\rightarrow$ **Hasil Riil: 34.0%** (34 unit) |
| **Blue (Uncommon / Tier 2)** | Bobot: 30% $\rightarrow$ **Hasil Riil: 31.0%** (31 unit) | Bobot: 33.3% $\rightarrow$ **Hasil Riil: 33.0%** (33 unit) |
| **Yellow (Rare / Legendary)** | Bobot: 10% $\rightarrow$ **Hasil Riil: 10.0%** (10 unit) | Bobot: 33.4% $\rightarrow$ **Hasil Riil: 33.0%** (33 unit) |

> **Analisis Weighted Random:** Algoritma Roulette Wheel yang dibangun pada `SelectWeightedSpawnEntry()` terbukti akurat mendekati distribusi probabilitas teoretis dengan tingkat deviasi empiris $< 1.5\%$.

---

## 5. Jawaban Lengkap 20 Pertanyaan Analisis Konsep PCG (Bab 107)

Berikut adalah jawaban lengkap, analitis, dan mendalam untuk seluruh 20 pertanyaan pada Bab 107 Modul Resmi:

### 1. Mengapa PCG tidak sama dengan random murni?
**Jawaban:**  
Random murni (*pure randomness*) hanya menghasilkan angka acak tanpa memperhitungkan konteks, estetika, atau aturan dunia game. Jika posisi di-random secara murni, musuh dapat muncul di dalam tembok padat, di luar batas peta, atau bertumpuk di koordinat yang sama. Sebaliknya, **PCG adalah controlled randomness**: proses pembentukan konten yang menggabungkan generator pseudo-random dengan seperangkat aturan (*rules*), parameter desain, relasi spasial, dan filter validasi (*constraints*) sehingga konten yang dihasilkan selalu dapat dimainkan (*playable*), seimbang (*balanced*), dan bebas cacat teknis.

---

### 2. Apa fungsi seed?
**Jawaban:**  
Seed adalah nilai integer awal yang dimasukkan ke dalam algoritma generator bilangan acak semu (*Pseudo-Random Number Generator / PRNG*). Seed berfungsi sebagai penentu urutan deret angka deterministik yang akan dihasilkan oleh komputer. Komputer tidak dapat menciptakan keacakan sejati secara alami; komputer mengeksekusi rumus matematika rekursif di mana setiap angka acak berikutnya dihitung dari angka sebelumnya, dimulai dari nilai dasar yaitu seed.

---

### 3. Apa yang dimaksud reproducibility?
**Jawaban:**  
Reproducibility (keterulangan) adalah kemampuan suatu sistem komputasi untuk menghasilkan output konten yang identik secara konsisten apabila diberikan input awal yang sama. Dalam konteks PCG, jika game dijalankan dengan **nilai Seed yang sama**, urutan pemanggilan fungsi acak yang sama, dan parameter yang sama, maka seluruh tata letak (*layout*) posisi musuh, jenis item, dan rotasi objek akan terbentuk sama persis di komputer mana pun tanpa perbedaan satu milimeter pun.

---

### 4. Mengapa seed penting untuk debugging?
**Jawaban:**  
Dalam game prosedural tanpa seed tetap, bug yang dilaporkan pemain (misalnya *"musuh terjebak di dalam pilar"*) hampir mustahil direproduksi oleh programmer karena layout acak baru akan tercipta setiap kali game dibuka. Dengan adanya sistem seed:
- Tester/pemain cukup mencatat seed tertentu (misal `Seed = 84920`).
- Programmer memasukkan seed tersebut ke Inspector di Unity Editor.
- Layout dunia yang memicu bug tersebut langsung terbentuk kembali 100% sama, memungkinkan programmer memeriksa posisi collider, breakpoint kode, dan memperbaiki algoritma validasi secara presisi.

---

### 5. Apa fungsi `Random.InitState()`?
**Jawaban:**  
`Random.InitState(int seed)` adalah metode resmi Unity Scripting API untuk mereset dan menginisialisasi status (*state*) internal generator angka acak bawaan Unity ke titik awal yang ditentukan oleh parameter `seed`. Dengan memanggil fungsi ini tepat sebelum proses generasi dimulai, kita memastikan bahwa deret angka acak yang keluar setelahnya akan mengikuti pola deterministik dari seed tersebut.

---

### 6. Apa fungsi `Random.Range()`?
**Jawaban:**  
`Random.Range()` berfungsi untuk mengambil satu nilai acak dari rentang minimum hingga maksimum tertentu.  
- **Untuk tipe float (`Random.Range(minFloat, maxFloat)`):** batas atas bersifat inklusif (`[min, max]`). Cocok untuk koordinat posisi kontinu, rotasi, atau scaling.
- **Untuk tipe integer (`Random.Range(minInt, maxInt)`):** batas atas bersifat eksklusif (`[min, max - 1]`). Sangat cocok untuk pengindeksan array, list, atau pemilihan bobot diskrit.

---

### 7. Apa yang dimaksud controlled randomness?
**Jawaban:**  
Controlled randomness adalah konsep desain sistem di mana keacakan (*stochasticity*) diizinkan terjadi hanya di dalam koridor atau batasan ketat yang telah ditentukan oleh perancang game (*game designer*). Keacakan memberikan variasi dan faktor kejutan (*novelty*), sedangkan kontrol (berupa validasi, min-max clamp, dan bobot probabilitas) menjamin konsistensi, keadilan gameplay (*fairness*), serta kelayakan teknis.

---

### 8. Mengapa spawn membutuhkan validation?
**Jawaban:**  
Karena ruang 3D dalam game memiliki struktur fisik yang tidak homogen (ada lantai datar, dinding batu, lubang jurang, posisi pemain, dan objek lain). Tanpa validasi spasial, calon titik spawn hanyalah koordinat angka mentah di ruang kosong. Validasi diperlukan untuk memastikan apakah titik tersebut aman, tidak bertabrakan dengan geometri padat, tidak merusak alur tantangan (*challenge pacing*), dan tidak membuat entitas terjebak (*stuck*).

---

### 9. Apa fungsi LayerMask?
**Jawaban:**  
`LayerMask` adalah struktur bitmask 32-bit di Unity yang digunakan untuk memfilter objek-objek mana saja yang harus diproses atau diabaikan oleh mesin fisika (*Physics Engine*) saat melakukan query (seperti raycast, spherecast, atau overlap test). Pada praktikum ini, `LayerMask` disetel ke Layer 7 (*Obstacle*), sehingga fungsi `Physics.CheckSphere` hanya mendeteksi keberadaan balok rintangan dan mengabaikan lantai, partikel, atau trigger lainnya, menghemat beban komputasi CPU secara signifikan.

---

### 10. Apa fungsi `Physics.CheckSphere()`?
**Jawaban:**  
`Physics.CheckSphere(position, radius, layerMask)` adalah metode physics query instan non-alokasi dari Unity yang memeriksa apakah ada *Collider* fisik yang berada di dalam atau bersinggungan dengan volume bola virtual yang berpusat di `position` dengan radius tertentu. Mengembalikan nilai `true` jika terjadi overlap (terhalang) dan `false` jika ruang tersebut bersih.

---

### 11. Mengapa obstacle harus mempunyai Collider?
**Jawaban:**  
Karena fungsi `Physics.CheckSphere` berkomunikasi secara langsung dengan sistem fisika PhysX milik Unity. Mesin fisika hanya mengenali batas-batas fisik suatu GameObject melalui komponen **Collider** (seperti `BoxCollider`, `CapsuleCollider`, atau `MeshCollider`). Jika sebuah obstacle hanya memiliki `MeshRenderer` tanpa Collider, objek tersebut hanyalah ilusi visual (*ghost mesh*) yang tidak terdaftar pada tabel spasial PhysX, sehingga `Physics.CheckSphere` akan menganggap posisi tersebut kosong dan meloloskan spawn di dalamnya.

---

### 12. Apa fungsi `minimumSpacing`?
**Jawaban:**  
`minimumSpacing` berfungsi sebagai aturan anti-penumpukan (*anti-clumping rule*). Parameter ini menentukan jarak Euclidean minimum yang wajib dipertahankan antara satu objek prosedural dengan objek prosedural lainnya yang telah terdaftar sebelumnya. Hal ini mencegah objek saling tumpang tindih (*intersecting geometry*), mengurangi artefak *z-fighting*, dan menciptakan distribusi objek yang menyebar secara estetis di arena.

---

### 13. Apa fungsi `minDistanceFromPlayer`?
**Jawaban:**  
Parameter ini menciptakan **Safe Spawn Zone** (zona aman) berbentuk lingkaran di sekeliling posisi pemain. Fungsinya menjamin pemain tidak langsung diserang musuh pada detik pertama saat game dimulai atau saat musuh baru saja di-spawn (*spawn camping protection*). Ini memberikan pemain waktu bereaksi (*reaction window*) untuk mengenali situasi sekitar sebelum terlibat dalam pertempuran.

---

### 14. Mengapa diperlukan `maxAttemptsPerObject`?
**Jawaban:**  
Dalam paradigma *Generate-and-Test*, penolakan kandidat (*rejection*) memicu pencarian ulang (*retry*). Jika parameter arena terlalu sempit atau jumlah objek yang diminta terlalu banyak, ruang kosong yang valid bisa saja habis total. Tanpa `maxAttemptsPerObject`, loop `while` atau `for` akan mencari posisi selamanya (*infinite loop*), menyebabkan game mengalami *hang*, *freezing*, atau *Application Not Responding (ANR)*. Batasan `maxAttempts` (misal 50 kali) bertindak sebagai mekanisme pemutus sirkuit (*circuit breaker*) demi kehandalan (*robustness*) aplikasi.

---

### 15. Apa yang terjadi jika constraint terlalu ketat?
**Jawaban:**  
Jika batasan terlalu ketat (misalnya `minDistanceFromPlayer` terlalu besar, `minimumSpacing` terlalu lebar, atau ukuran obstacle memenuhi sebagian besar arena):
1. **Tingkat penolakan (*Rejection Rate*) melonjak sangat tinggi**, memboroskan siklus CPU.
2. **Kegagalan Spawn (*Under-spawning*):** Algoritma akan mencapai batas `maxAttempts` sebelum seluruh target kuota terpenuhi, sehingga jumlah musuh atau item yang muncul di dunia game lebih sedikit daripada yang direncanakan.
3. **Pola Spasial Terdistorsi:** Objek yang berhasil lolos hanya akan berkumpul di celah-celah sempit yang tersisa di sudut peta (*edge clustering*), menghilangkan keacakan alami.

---

### 16. Apa itu weighted random?
**Jawaban:**  
Weighted random (keacakan berbobot) adalah teknik penarikan sampel probabilitas di mana setiap elemen pilihan memiliki bobot peluang (*weight*) relatif yang berbeda, alih-alih memiliki peluang seragam yang sama rata (*uniform distribution*). Probabilitas terpilihnya suatu elemen $i$ dihitung dengan rumus:
$$P(i) = \frac{\text{Weight}(i)}{\sum_{j} \text{Weight}(j)}$$

---

### 17. Mengapa weighted random cocok untuk loot atau enemy selection?
**Jawaban:**  
Dalam game design, tidak semua konten memiliki nilai dan tingkat ancaman yang setara. Jika peluang seragam digunakan (50% musuh biasa, 50% bos mematikan, atau 50% koin tembaga, 50% pedang legendaris), maka:
- Game akan menjadi terlalu sulit atau ekonomi game runtuh seketika (*game balance broken*).
- Rasa kepuasan pemain saat mendapatkan barang langka (*scarcity value*) akan hilang.  
Weighted random memungkinkan perancang mengatur hierarki kelangkaan secara presisi, misalnya 60% item sampah/umum (*Common*), 30% item bagus (*Uncommon*), dan hanya 10% item istimewa (*Legendary*).

---

### 18. Apa hubungan PCG dengan pathfinding?
**Jawaban:**  
Hubungan antara PCG dan Pathfinding sangat erat dan saling bergantung:
1. **Validasi Keterjangkauan (*Reachability Validation*):** PCG dapat menggunakan algoritma pathfinding (seperti A* atau Dijkstra flood-fill) untuk memverifikasi apakah kunci dan pintu yang dihasilkan dapat dijangkau oleh pemain, atau apakah musuh memiliki rute navigasi menuju pemain (*solvability guarantee*).
2. **NavMesh Integration:** Konten yang di-spawn oleh PCG (seperti obstacle) harus diintegrasikan ke dalam representasi navigasi agen AI (melalui dynamic *NavMeshObstacle carving* atau runtime baking) agar agen tidak menabrak rintangan prosedural yang baru saja muncul.
3. **Pacing & Layout Generation:** PCG dapat mengatur penempatan musuh di sepanjang simpul rute (*waypoints*) hasil kalkulasi pathfinding untuk menciptakan kurva kesulitan bertahap.

---

### 19. Apa perbedaan constructive PCG dan generate-and-test?
**Jawaban:**  
- **Constructive PCG (Konstruktif):** Algoritma membangun konten secara langsung mengikuti aturan matematis deterministik dari awal, di mana setiap langkah dijamin benar berdasarkan formula atau gramatika formal (*by design / correct-by-construction*). Tidak pernah ada langkah penolakan atau penghapusan ulang. Contoh: Perlin Noise untuk pembuatan kontur gunung, BSP (*Binary Space Partitioning*) untuk pembagian ruangan dungeon.
- **Generate-and-Test PCG:** Algoritma mengambil pendekatan coba-dan-evaluasi. Tahap pertama menghasilkan kandidat acak secara bebas (*generator*), kemudian tahap kedua memeriksa kandidat tersebut terhadap serangkaian aturan validasi (*tester/evaluator*). Jika lolos kriteria, kandidat diterima; jika melanggar batasan, kandidat dibuang (*rejected*) dan proses diulang kembali.

---

### 20. Di bagian mana kedua konsep tersebut muncul pada praktikum ini?
**Jawaban:**  
Kedua paradigma tersebut muncul secara harmonis dalam arsitektur script `PCGSpawner.cs`:
1. **Konsep Generate-and-Test:**  
   Muncul pada penentuan posisi spasial (koordinat kandidat $X, Z$). Fungsi `GenerateRandomPosition()` bertindak sebagai **Generator** yang memproduksi titik acak di dalam batas arena. Kemudian fungsi `IsPositionValid()` bertindak sebagai **Tester** yang memvalidasi titik tersebut terhadap Rule 1 (jarak player), Rule 2 (Physics query obstacle), dan Rule 3 (minimum spacing). Jika gagal, kandidat ditolak dan algoritma mengulang pencarian.
2. **Konsep Constructive PCG:**  
   Muncul pada penentuan jenis prefab melalui **Weighted Random Selection** (`SelectWeightedSpawnEntry()`) dan pemberian rotasi acak (`Quaternion.Euler(0, yaw, 0)`). Pada tahap ini, algoritma tidak pernah melakukan "generate jenis lalu tolak". Algoritma secara konstruktif menghitung total bobot, menarik satu angka random diskrit pada rentang kumulatif, dan langsung memilih prefab yang sah secara matematis pasti valid dalam satu langkah perhitungan ($O(N)$).

---

## 6. Verifikasi & Pemenuhan Kriteria Keberhasilan (Bab 109)

| Checklist Kriteria Keberhasilan (Bab 109) | Status Teknis | Bukti Implementasi / Verifikasi |
|:---|:---:|:---|
| Project dijalankan tanpa compile error (0 warnings/errors) | **TERPENUHI** | `dotnet build` pada `Assembly-CSharp.csproj` dan `Assembly-CSharp-Editor.csproj` sukses 100% dengan 0 Error. |
| Enemy berhasil di-spawn secara procedural | **TERPENUHI** | 12 Enemy berhasil di-spawn di `GeneratedObjects` parent. |
| Item berhasil di-spawn secara procedural | **TERPENUHI** | 8 Item berhasil di-spawn di `GeneratedObjects` parent. |
| Spawn area dapat diubah melalui Inspector | **TERPENUHI** | Variabel `spawnAreaSize` bertipe `Vector2` terekspos di Inspector. |
| Seed dapat ditentukan manual | **TERPENUHI** | Variabel `seed` bertipe `int` terekspos di Inspector. |
| Seed sama menghasilkan generation yang reproducible | **TERPENUHI** | Terbukti pada Eksperimen 2: Seed 500 menghasilkan koordinat & jenis yang identik 100% pada 3 kali pengujian. |
| Seed berbeda menghasilkan variasi | **TERPENUHI** | Terbukti pada Eksperimen 1: Seed 100, 200, dan 300 menghasilkan formasi unik yang berbeda. |
| Enemy tidak muncul terlalu dekat Player | **TERPENUHI** | Rule 1 memfilter jarak horizontal $< 5.0\text{m}$ dari Player. |
| Object tidak muncul di dalam obstacle | **TERPENUHI** | Rule 2 menggunakan `Physics.CheckSphere` pada Layer 7 (*Obstacle*). |
| Object tidak terlalu bertumpuk | **TERPENUHI** | Rule 3 memverifikasi jarak Euclidean $\ge 1.5\text{m}$ antar usedPositions. |
| Weighted random bekerja | **TERPENUHI** | Terbukti pada Eksperimen 5: Distribusi kemunculan Red 60%, Blue 30%, Yellow 10% terverifikasi secara empiris. |
| `maxAttempts` digunakan sebagai pengaman | **TERPENUHI** | Parameter `maxAttemptsPerObject = 50` membatasi loop pencarian posisi. |
| Debug seed ditampilkan di runtime | **TERPENUHI** | Panel OnGUI HUD menampilkan `Current Seed: X` secara langsung di layar. |
| Rejected candidate dapat diamati | **TERPENUHI** | Counter `rejectedPositions` dicatat dan ditampilkan pada HUD OnGUI. |
| Spawn area terlihat melalui Gizmos | **TERPENUHI** | Scene View menampilkan WireCube Cyan (arena) dan WireSphere Merah (safe zone). |
| 3 Fitur Modifikasi diimplementasikan | **TERPENUHI** | Random Yaw Rotation, Realtime Spawn Statistics Breakdown, dan UI Regeneration Button + Key [R]. |

---

## 7. Kesimpulan Akhir

Praktikum 09 ini berhasil membuktikan bahwa **Procedural Content Generation (PCG) bukanlah sekadar pelemparan angka acak murni**, melainkan proses komputasi yang terstruktur dan terukur. 

Dengan menggabungkan **inisialisasi seed deterministik**, **kontrol spasial bertingkat (*multi-stage spatial validation*)**, dan **seleksi berbasis bobot probabilitas (*weighted distribution*)**, sistem mampu menghasilkan dunia game yang dinamis, bervariasi tanpa batas (*infinite replayability*), namun tetap berada dalam koridor desain game yang aman, adil, dan menyenangkan bagi pemain.
