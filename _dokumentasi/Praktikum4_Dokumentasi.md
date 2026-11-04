# Dokumentasi Praktikum 4: Pathfinding & Navigation
**Mata Kuliah:** Game Cerdas  
**Engine & Bahasa:** Unity 6 & C#  
**Fokus Utama:** Implementasi Manual A* Grid Pathfinder (Bagian A) dan Unity NavMesh Navigation (Bagian B).

---

## 1. Pendahuluan & Konsep Inti

Praktikum 4 berfokus pada pemahaman fundamental bagaimana sebuah agen AI mencari jalan (*pathfinding*), mengelola titik-titik simpul rute (*navigation*), dan menggerakkan tubuh fisiknya (*movement*).

### A. Hierarki Tiga Lapisan Navigasi AI
Mahasiswa tidak hanya menggunakan fungsi bawaan engine seperti `SetDestination()`, melainkan memahami keterikatan tiga lapisan:
```
Pathfinding (Menentukan Jalur Global: A* Graph Search / NavMesh Query)
         ↓
Navigation (Mengikuti Jalur & Manajemen Simpul: Path Following & Smart Repathing)
         ↓
Movement (Aktuator Fisik / Eksekusi Gerak: MoveTowards / Rigidbody / NavMesh Motor)
```

### B. Menjawab Masalah Praktikum 3 (Local Minimum)
Pada Praktikum 3 (*Autonomous Steering Agent*), NPC sering terjebak selamanya di sudut tembok atau ruangan buntu berbentuk U (*Local Minimum*). Hal ini terjadi karena *Steering Behaviors* hanya bekerja secara **reaktif lokal** (hanya melihat apa yang ada di depan hidungnya menggunakan `SphereCast`). 
**Praktikum 4 memecahkan masalah ini dengan Global Pathfinding:** agen memetakan seluruh rute dari awal (*Start*) hingga akhir (*Goal*) pada representasi graph sebelum melangkah, sehingga mustahil terjebak di jalan buntu.

---

## 2. Bagian A — A* Manual pada Grid

Sistem A* manual dibangun pada representasi graph seluler 2D/3D top-down di scene `Assets/Scenes/Praktikum4.unity`.

### Komponen Skrip (`Assets/Scripts/AStar/`)
1. **`GridNode.cs` (Data Model Simpul):**
   - Merepresentasikan satu sel di grid.
   - Menyimpan koordinat grid `(x, y)`, posisi fisik `worldPosition`, status `walkable` (bisa dilewati atau rintangan), `gCost`, `hCost`, total evaluasi `FCost = gCost + hCost`, serta pointer `parent` untuk penelusuran balik.
2. **`GridManager.cs` (Graph & Sensor Rintangan):**
   - Membangun array 2D `GridNode[width, height]`.
   - Menggunakan sensor `Physics.CheckBox` dengan `obstacleMask` untuk menentukan apakah ada collider tembok di atas sel tersebut.
   - Mengubah status sel menjadi un-walkable (hitam) jika terkena rintangan, atau walkable (putih) jika kosong.
   - Menyediakan fungsi tetangga 4-arah orthogonal: Atas, Bawah, Kiri, dan Kanan (`GetNeighbors`).
3. **`AStarPathfinder.cs` (Algoritma Pencari Jalur):**
   - **Open Set (`List<GridNode>`):** Kumpulan node kandidat yang telah ditemukan namun belum dievaluasi.
   - **Closed Set (`HashSet<GridNode>`):** Kumpulan node yang telah selesai dievaluasi agar tidak dihitung ulang.
   - **Heuristik Manhattan Distance:**
     $$\text{Manhattan}(A, B) = (|A.x - B.x| + |A.y - B.y|) \times 10$$
     *(Dikalikan 10 agar nilai cost berupa integer presisi, dengan biaya langkah horizontal/vertikal = 10).*
   - **Reconstruct Path:** Menelusuri balik pointer `parent` dari `goalNode` mundur ke `startNode`, lalu membalik urutan list (`path.Reverse()`) untuk menghasilkan urutan jalan maju.
   - **Visualisasi Warna:**
     - 🟡 **Kuning:** Node yang sedang berada di Open Set.
     - 🟠 **Oranye:** Node yang telah diproses di Closed Set.
     - 🔵 **Cyan:** Jalur final terpendek (*Final Path*).
     - 🟢 **Hijau:** Node Start.
     - 🔴 **Merah:** Node Goal.
4. **`AgentPathFollower.cs` (Path Following Actuator):**
   - Menerima hasil `currentPath` dari pathfinder.
   - Menggerakkan transform agen menyusuri simpul demi simpul menggunakan `Vector3.MoveTowards` dengan orientasi rotasi halus (`Quaternion.LookRotation`).

---

## 3. Bagian B — Unity NavMesh Navigation

Sistem NavMesh 3D dibangun mandiri di scene `Assets/Scenes/Praktikum4_NavMesh.unity` menggunakan paket bawaan Unity 6 `Unity.AI.Navigation`.

### Komponen Skrip Mandiri (`Assets/Scripts/Praktikum4_NavMesh/`)
1. **`NavMeshChaser.cs`:**
   - Mengendalikan NPC menggunakan komponen `UnityEngine.AI.NavMeshAgent`.
   - **Stopping Distance (`1.5m`):** Mencegah NPC menabrak atau mendesak collider target ketika telah sampai di dekatnya.
   - **Smart Repathing Controller:**
     - Menghindari pemanggilan `agent.SetDestination()` tanpa kontrol setiap frame (yang dapat menyebabkan beban CPU berlebih dan stuttering).
     - Repathing hanya dipicu jika **jarak pergeseran target > 0.5m** ATAU **interval timer 0.25 detik** telah terlampaui.
   - Menyediakan diagnostik real-time: `ArrivedAtTarget`, `RemainingDistance`, `HasPath`, dan `IsPathPending`.
2. **`PlayerTargetMovement.cs`:**
   - Kontroler pergerakan target yang responsif menggunakan input WASD / Tombol Panah.
   - Dilengkapi tombol shortcut **`R`** untuk me-reset posisi target ke titik spawn awal, memudahkan demonstrasi berulang.
3. **`NavMeshPathDebugger.cs`:**
   - Mengekstrak titik-titik sudut tikungan `agent.path.corners`.
   - Menampilkan garis rute di **Game View** menggunakan `LineRenderer` oranye terang (dinaikkan sedikit agar bebas *z-fighting* dengan lantai).
   - Menampilkan visualisasi garis dan bola simpul kuning di **Scene View** via `OnDrawGizmos`.
   - Mengkalkulasi total panjang lintasan (*Total Path Length*) secara otomatis.
4. **`DynamicObstacleController.cs`:**
   - Pengendali rintangan dinamis yang dilengkapi komponen `NavMeshObstacle`.
   - Memfasilitasi eksperimen wajib **Carve (ON vs OFF)** secara runtime dengan menekan tombol **`C`** atau **`Space`** pada keyboard.
   - Menghadirkan feedback visual langsung: warna material berubah menjadi **Hijau saat Carve ON** dan **Merah saat Carve OFF**.

---

## 4. Matriks Eksperimen Wajib & Hasil Pengamatan

| No | Eksperimen | Skenario & Langkah Uji | Hasil Pengamatan Teknis |
|:---|:---|:---|:---|
| 1 | **Pindahkan Goal A\*** | Geser posisi `GoalMarker` di scene A* | Rute cyan A* langsung menyesuaikan dan membentuk jalur terpendek baru menuju koordinat goal yang baru. |
| 2 | **Tambah Dinding A\*** | Tambahkan box obstacle di koridor lurus A* | Sel grid berubah menjadi warna hitam (`walkable = false`), A* menjelajahi cabang tetangga lain dan menemukan rute memutar yang valid. |
| 3 | **Goal A* Terisolasi** | Kurung `GoalMarker` dengan dinding 4 sisi | Open Set kehabisan node yang valid untuk dievaluasi, pathfinder mengeluarkan log peringatan *"A*: path tidak ditemukan; periksa obstacle dan konektivitas grid"*, dan agen berhenti aman tanpa crash. |
| 4 | **Target NavMesh Bergerak** | Gerakkan target menggunakan WASD melintasi sekat dinding | `NavMeshChaser` mendeteksi pergeseran target melampaui ambang batas 0.5m dan segera memperbarui rute; garis `LineRenderer` meliuk mulus mengikuti pergerakan tanpa freeze. |
| 5 | **NavMeshObstacle Carve = OFF** | Tekan tombol 'C' hingga rintangan berwarna Merah (Carve OFF) | NavMesh dasar di lantai tidak terpotong (lubang tidak ada); garis rute NPC tetap menembus rintangan, dan NPC berusaha jalan lurus namun terhenti/tergeser karena collider fisik. |
| 6 | **NavMeshObstacle Carve = ON** | Tekan tombol 'C' hingga rintangan berwarna Hijau (Carve ON) | NavMesh runtime langsung terpotong (*carved*) membentuk lubang solid; garis rute seketika membelok mengitari rintangan menuju lorong samping secara elegan. |

---

## 5. Panduan Jawaban Pertanyaan Demo & Asisten Lab

Berikut adalah rangkuman jawaban untuk seluruh pertanyaan konseptual yang diwajibkan saat sesi demo:

### 1. Mengapa steering atau Seek saja tidak cukup untuk obstacle besar?
> **Jawaban:** Steering behaviors (seperti *Seek* dan *Obstacle Avoidance*) bekerja secara **lokal dan miopik (jangka pendek)**. Agen hanya bereaksi terhadap gaya tolak vektor sesaat di depannya. Saat bertemu rintangan besar berbentuk cekung (huruf U) atau dinding panjang, gaya dorong maju dan gaya tolak hindaran bertabrakan sempurna, menjebak agen dalam fenomena **Local Minimum** (jalan buntu). Pathfinding global (A* / NavMesh) memetakan topologi ruang secara keseluruhan sebelum bergerak sehingga agen mengetahui jalan memutar.

### 2. Apa fungsi `gCost`, `hCost`, dan `fCost` dalam A*?
> **Jawaban:**
> - **`gCost` (Given Cost):** Biaya riil yang sudah ditempuh dari titik *Start* menuju simpul saat ini.
> - **`hCost` (Heuristic Cost):** Estimasi perkiraan biaya tersisa dari simpul saat ini menuju titik *Goal*.
> - **`fCost` (Final Cost / $f = g + h$):** Total estimasi biaya terbaik. A* selalu memprioritaskan node dengan `fCost` terendah untuk dievaluasi terlebih dahulu, menjamin rute yang ditemukan adalah rute terpendek (*optimal path*).

### 3. Mengapa Manhattan Distance sangat cocok untuk grid 4 arah?
> **Jawaban:** Manhattan Distance dihitung berdasarkan rumus:
> $$D = |x_1 - x_2| + |y_1 - y_2|$$
> Pada grid 4 arah, pergerakan diagonal dilarang dan agen hanya bisa melangkah secara orthogonal (kiri, kanan, atas, bawah). Manhattan Distance secara matematis mencerminkan jumlah langkah grid minimum tanpa hipotenusa diagonal, sehingga sifat heuristiknya bersifat *admissible* (tidak pernah melebih-lebihkan biaya riil) dan menghasilkan pencarian A* yang paling efisien.

### 4. Apa fungsi pointer `parent` dalam merekonstruksi jalur (*reconstruct path*)?
> **Jawaban:** Selama proses eksplorasi A*, setiap kali sebuah tetangga mendapatkan nilai `gCost` yang lebih murah, simpul tersebut menyimpan referensi ke node yang membukanya (`neighbor.parent = currentNode`). Ketika *Goal* berhasil dicapai, algoritma cukup menelusuri balik rantai pointer `parent` dari *Goal* mundur hingga mencapai *Start*, lalu membalik list tersebut menjadi rute maju.

### 5. Apa perbedaan fundamental antara Open Set dan Closed Set?
> **Jawaban:**
> - **Open Set:** Daftar simpul yang sudah ditemukan (*discovered*) namun **belum dievaluasi**. Simpul-simpul ini menjadi antrean kandidat untuk diperiksa nilai `fCost`-nya.
> - **Closed Set:** Kumpulan simpul yang **sudah selesai dievaluasi sepenuhnya**. Simpul di Closed Set tidak akan pernah diproses ulang untuk mencegah perulangan tak terbatas (*infinite loops*).

### 6. Apa perbedaan antara Pathfinding, Path Following, dan Navigation?
> **Jawaban:**
> - **Pathfinding:** Algoritma pemecah masalah graph (seperti A* atau Dijkstra) yang bertugas **menghasilkan urutan titik koordinat (jalur global)**.
> - **Path Following:** Sistem pergerakan lokal yang bertugas **mengarahkan tubuh agen menyusuri simpul demi simpul** dalam jalur tersebut.
> - **Navigation:** Arsitektur menyeluruh yang mengintegrasikan pembuatan jalur (*pathfinding*), penelusuran rute (*path following*), deteksi tabrakan dinamis (*avoidance*), dan pembaruan rute (*repathing*).

### 7. Apa fungsi `NavMeshSurface` dan `NavMeshAgent`?
> **Jawaban:**
> - **`NavMeshSurface`:** Komponen level-generator yang bertugas menganalisis geometri 3D scene (kemiringan lereng, tinggi undakan, ukuran rintangan) lalu melakukan proses komputasi (*Baking*) menjadi poligon lintasan (*Navigation Mesh*).
> - **`NavMeshAgent`:** Komponen kecerdasan buatan pada karakter/NPC yang membaca data NavMesh untuk mencari jalan, membelokkan rotasi, menghindari agen lain (*RVO avoidance*), dan menggerakkan fisik objek.

### 8. Apa fungsi parameter `Stopping Distance`?
> **Jawaban:** Menentukan radius jarak toleransi sebelum agen menghentikan lajunya saat mendekati target. Jika `Stopping Distance = 0`, agen akan terus memaksa bergerak hingga titik pusat objeknya bertabrakan dengan pusat target, menyebabkan dorongan fisik (*pushing/clipping*). Nilai seperti `1.5m` membuat NPC berhenti secara natural di depan target.

### 9. Apa fungsi `NavMeshObstacle` dan bagaimana pengaruh fitur `Carve`?
> **Jawaban:**
> - **`NavMeshObstacle`:** Komponen untuk menandai objek bergerak/dinamis sebagai rintangan.
> - **Carve = OFF:** Geometri NavMesh di lantai tidak berubah. Agen memperlakukan rintangan hanya melalui sistem penghindaran tabrakan lokal (RVO). Jika lorong sempit tertutup, agen bisa macet karena mengira jalur masih ada.
> - **Carve = ON:** Objek secara aktif memotong (*cut out*) lubang baru pada data NavMesh secara runtime. Seluruh agen yang menggunakan NavMesh langsung mengetahui bahwa area tersebut tidak dapat dilalui dan menghitung rute memutar baru (*repathing*).

### 10. Mengapa proses repathing tidak boleh dilakukan tanpa kontrol di setiap frame?
> **Jawaban:** Kalkulasi rute NavMesh melibatkan komputasi graph poligon ruang 3D yang mahal (*CPU intensive*). Jika `agent.SetDestination()` dipanggil 60–120 kali per detik di dalam `Update()`:
> 1. Terjadi pemborosan daya pemrosesan CPU (*performance drop / frame drops*).
> 2. Jalur yang belum selesai dikalkulasi (*pathPending*) akan dibatalkan berulang kali, menimbulkan getaran arah (*jitter/stutter*) pada NPC.
> 3. Oleh karena itu, kita menerapkan **Smart Repathing**: repath hanya dipanggil berdasarkan batas interval waktu (misal 0.25s) atau saat target telah berpindah jarak signifikan (> 0.5m).
