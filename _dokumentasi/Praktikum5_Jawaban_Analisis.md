# Panduan Jawaban Pertanyaan Analisis Modul 05: Finite State Machine (FSM)

Dokumen ini disusun sebagai referensi resmi untuk menjawab ke-10 pertanyaan analisis pada **Modul 05 (Enemy AI FSM)** dalam mata kuliah *Game Cerdas*.

---

### 1. Apa perbedaan antara State, Condition, dan Transition?
- **State (Keadaan):** Mode atau perilaku aktif yang sedang dijalankan oleh agen pada suatu waktu tertentu (contoh: `Patrol`, `Chase`, `Attack`, `Flee`, `Dead`). Dalam suatu state, agen menjalankan logika spesifik (*Update loop*, *Enter*, *Exit*).
- **Condition (Kondisi/Pemicu):** Logika evaluasi berbasis kondisi lingkungan atau status internal (bernilai `true`/`false`) yang menentukan apakah agen harus berpindah state (contoh: `CanSeePlayer == true`, `distance <= attackRange`, `CurrentHealth <= 30`, `CurrentHealth <= 0`).
- **Transition (Transisi):** Proses peralihan status dari satu state ke state lainnya ketika condition terpenuhi. Transisi melibatkan eksekusi pembersihan state lama (*Exit hook*), pengubahan state aktif, dan inisialisasi state baru (*Enter hook*).

---

### 2. Mengapa `NavMeshAgent` bukan merupakan FSM?
`NavMeshAgent` adalah komponen **Pathfinding & Motor Actuator (Sistem Navigasi Fisik)**, bukan sistem *Decision Making* (pengambil keputusan). 
- `NavMeshAgent` hanya bertugas menghitung rute geometris terpendek (*A\* pathfinding* pada NavMesh polygon) dan menggerakkan GameObject menuju koordinat tujuan (`SetDestination`).
- `NavMeshAgent` tidak memiliki kesadaran kapan ia harus mengejar musuh, kapan harus berhenti untuk menyerang, atau kapan harus kabur karena sekarat. Logika keputusan tersebut sepenuhnya diatur oleh **FSM (`EnemyFSM`)** yang kemudian memberi perintah tujuan/kecepatan kepada `NavMeshAgent`.

---

### 3. Apa fungsi `EnemyPerception`?
`EnemyPerception` berfungsi sebagai **Sistem Sensorik (Perception Layer)** agen cerdas yang bertugas mengumpulkan informasi sensorik dari lingkungan game. Fungsi utamanya adalah:
1. Memeriksa jarak fisik target (*Vision Range*).
2. Memeriksa sudut kerucut pandang mata (*Field of View / FOV* $90^\circ$).
3. Memeriksa garis pandang (*Line of Sight*) menggunakan Raycast untuk mendeteksi rintangan dinding atau halangan fisik.
Dengan memisahkan sensor ke dalam `EnemyPerception`, prinsip *Single Responsibility Principle (SRP)* terpenuhi, di mana `EnemyPerception` hanya bertugas mendeteksi (`CanSeePlayer`), sementara `EnemyFSM` bertugas memutuskan aksi.

---

### 4. Mengapa Line of Sight menggunakan Raycast?
Hanya memeriksa jarak Euclidean (`Vector3.Distance`) dan sudut (`Vector3.Angle`) tidak cukup dalam lingkungan 3D karena agen bisa melihat menembus tembok tebal. 
**`Physics.Raycast`** digunakan untuk menembakkan garis sinar matematis dari posisi mata agen menuju target. Jika raycast menabrak collider yang berada pada `obstacleMask` (Layer `Obstacle` atau `Wall`), maka garis pandang terhalang (`blocked = true`), sehingga agen tidak dapat mendeteksi pemain yang sedang bersembunyi di balik dinding.

---

### 5. Apa perbedaan `Tag` dan `Layer`?
- **Tag:** Pengenal string unik yang digunakan untuk mengidentifikasi/mengelompokkan GameObject secara logis (contoh: Tag `"Player"`, `"Enemy"`, `"MainCamera"`). Tag umumnya digunakan pada fungsi pencarian `GameObject.FindWithTag()` atau komparasi collider `other.CompareTag("Player")`.
- **Layer:** Bitmask integer (0–31) yang digunakan oleh mesin Unity untuk **operasi tingkat rendah**, terutama:
  1. **Physics Raycasting:** Menyaring objek mana yang terkena atau diabaikan oleh Raycast (`LayerMask`).
  2. **Camera Culling:** Menentukan layer objek mana yang dirender oleh kamera tertentu.
  3. **Physics Collision Matrix:** Mengatur apakah collider pada layer A dapat bertabrakan fisik dengan layer B.

---

### 6. Mengapa Attack membutuhkan cooldown?
Jika serangan tidak memiliki cooldown, fungsi `TakeDamage()` akan terpanggil pada setiap frame di dalam `Update()` (sekitar 60–120 kali per detik). Hal ini akan menyebabkan darah Player/Enemy habis secara instan dalam hitungan milidetik.
Cooldown (`attackCooldown = 1.5s`) memberikan jeda waktu matematis yang adil (*gameplay pacing*), memberi kesempatan bagi pemain untuk bereaksi/menghindar, serta menyelaraskan ritme serangan dengan animasi visual karakter.

---

### 7. Mengapa `attackRange` dan `attackExitRange` dibuat berbeda?
Teknik ini disebut **Hysteresis** (ambang batas ganda / *dual threshold*):
- `attackRange` = $2.0\text{m}$ (jarak untuk **masuk** ke mode Attack dari Chase).
- `attackExitRange` = $2.75\text{m}$ (jarak untuk **keluar** dari mode Attack kembali ke Chase).
Perbedaan jarak sebesar $0.75\text{m}$ ini berfungsi sebagai **zona penyangga (buffer zone)** agar agen tidak mengalami fenomena **State Chatter / Oscillation** (berpindah-pindah state secara liar bolak-balik antara Chase dan Attack setiap frame) saat pemain bergerak sedikit di batas $2.0\text{m}$.

---

### 8. Apa masalah yang terjadi jika keduanya bernilai sama?
Jika `attackRange == attackExitRange` (misal sama-sama $2.0\text{m}$):
Ketika Player berdiri tepat di jarak $2.001\text{m}$ lalu maju ke $1.999\text{m}$, Enemy akan berganti status puluhan kali per detik (Chase $\rightarrow$ Attack $\rightarrow$ Chase $\rightarrow$ Attack). 
Akibatnya:
1. `NavMeshAgent` terus menerus dipanggil `isStopped = true` lalu `isStopped = false`.
2. Animasi karakter berkedip-kedip (*glitchy stutter*).
3. Log console dibanjiri ratusan pesan transisi state dalam 1 detik.
4. AI terlihat kaku, tidak natural, dan boros komputasi.

---

### 9. Mengapa condition Dead diperiksa sebelum Flee?
Dalam arsitektur FSM hierarki prioritas:
`Dead` adalah **Terminal State (Keadaan Akhir/Tertinggi)**. Agen yang sudah mati (HP $\le 0$) tidak boleh lagi melakukan aksi fisik apapun, termasuk berlari atau kabur.
Jika condition `Flee` (HP $\le 30$) diperiksa terlebih dahulu sebelum `Dead` (HP $\le 0$), maka ketika HP agen mencapai $0$ (yang mana $0 \le 30$ adalah benar), agen justru akan mencoba masuk ke mode `Flee` dan terus berlari padahal ia sudah mati.
Oleh karena itu, evaluasi `Dead` **wajib diletakkan di prioritas teratas** sebelum Flee, Chase, maupun Patrol.

---

### 10. Mengapa FSM sebaiknya tidak seluruhnya ditulis dalam satu `if-else` besar?
Menulis FSM di dalam satu rantai `if-else` raksasa di dalam `Update()` (*Spaghetti Code*) memiliki kelemahan fatal:
1. **Sulit Dirawat (*High Complexity*):** Logika evaluasi kondisi bercampur baur dengan logika eksekusi aksi, membuat kode rentan bug saat state bertambah.
2. **Tidak Memiliki Lifecycle Hook:** Sulit mendeteksi kapan agen *mulai memasuki* suatu state (`EnterState`) atau *meninggalkan* suatu state (`ExitState`). Tanpa hook ini, inisialisasi seperti `agent.isStopped = true` atau `agent.speed = fleeSpeed` akan tereksekusi terus-menerus setiap frame (*redundant overhead*).
3. **Pemberian Transisi Tidak Terkontrol:** Sulit memicu event eksternal (seperti pembaruan UI, audio, atau partikel) saat terjadi perpindahan state.
Dengan memisahkan struktur menjadi `Enum`, switch-case, serta lifecycle methods (`EnterState`, `ExitState`, `ChangeState`), kode menjadi modular, bersih (*clean code*), dan mudah diperluas (*extensible*).
