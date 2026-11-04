# Dokumentasi Praktikum 3: Autonomous Steering Agent
**Mata Kuliah:** Game Cerdas
**Fokus Utama:** Implementasi Steering Behaviors (Arrive, Wander, Obstacle Avoidance) dan Integrasi dengan FSM (NPC Brain) tanpa menggunakan NavMesh.

---

## 1. Pemenuhan Syarat Modul Utama
Seluruh arsitektur NPC telah dirombak untuk tidak menggunakan `NavMeshAgent`. Sebagai gantinya, pergerakan murni dikalkulasi secara lokal melalui skrip `SteeringAgent.cs`.
1. **Targeting & Arrive:** NPC dapat bergerak menuju target (Player atau Waypoint) dan akan melambat secara perlahan ketika jaraknya mulai mendekati target (*Slow Radius* & *Stop Radius*).
2. **Wander:** Saat tidak memiliki target (misal saat Player menghilang dari pandangan), NPC akan berjalan acak dan merubah arahnya setiap beberapa detik.
3. **Obstacle Avoidance:** Menggunakan sensor `Physics.SphereCast` untuk mendeteksi tembok di depan. Jika terdeteksi, AI akan menghitung vektor pantul untuk membelokkan NPC tanpa menghentikan kecepatannya.
4. **Frame-Rate Independence & Gizmos:** Semua pergerakan manual telah dikalikan dengan `Time.deltaTime`. Dilengkapi juga dengan visualisasi *Gizmos* di Editor untuk melihat arah vektor dan radius AI.

---

## 2. Implementasi Fitur Tambahan (Challenge / Enhancement)
Selain syarat wajib, kami juga berhasil merakit fitur-fitur kompleks berikut:
1. **Patroli Waypoint Bebas NavMesh:** NPC berpindah dari satu titik ke titik lain murni menggunakan gaya tarik *Arrive*.
2. **Toleh Kiri-Kanan di Waypoint:** Saat tiba di Waypoint, NPC menunggu selama durasi tertentu (*Patrol Pause Time*) dan menjalankan animasi rotasi menoleh dengan fungsi `Mathf.Sin` sebelum lanjut berjalan.
3. **Indikator UI Reaktif:** Memunculkan tanda seru (`!`) saat status *Chase*, dan tanda tanya (`?`) saat *Search* atau *Suspicious*.
4. **Komunikasi Antar Penjaga (Shared Alert):** Jika satu NPC melihat Player, ia akan mengeksekusi `Physics.OverlapSphere` (radius 15m) untuk memberi tahu koordinat Player ke penjaga lain di sekitarnya.
5. **Separation (Anti-Tumpuk):** Memisahkan NPC yang saling berdekatan saat mengejar target yang sama agar badannya tidak menyatu.
6. **Integrasi Animasi:** Mengganti parameter kecepatan animasi (*CharacterModelVisual*) dari yang sebelumnya membaca *NavMesh*, kini membaca `SteeringAgent.Velocity.magnitude`.

---

## 3. Jurnal Permasalahan dan Resolusi Kontekstual

Berikut adalah rincian masalah yang ditemukan selama pengembangan di arena Labirin, dikelompokkan berdasarkan akar masalahnya, beserta solusi yang diimplementasikan:

### Konteks A: Sensor Mata dan Vektor Menghindar (Avoidance)
1. **Masalah "X-Ray Vision":** NPC dapat melihat Player menembus tembok.
   * **Solusi:** Menyesuaikan *Obstacle Mask* pada skrip sensor NPC agar mengkalkulasi Layer `Wall` sebagai penghalang visual.
2. **Masalah NPC Bergetar/Zig-Zag di Tembok:** Sensor hanya membaca jarak pendek, membuat NPC memantul menjauh, kehilangan deteksi tembok, lalu menabrak lagi (berulang-ulang).
   * **Solusi:** Menambahkan interpolasi `Vector3.Lerp` pada variabel `smoothedAvoidance` sehingga pembelokan vektor menjadi mulus (*smooth*) layaknya belokan mobil.
3. **Masalah NPC Menabrak Tembok Secara Buta:** NPC mencoba merangsek masuk ke tembok alih-alih menghindar.
   * **Solusi:** Mengoreksi rumus *Forward Bias* yang terlalu besar (`2.0`) kembali ke bentuk aslinya (`0.5`). Nilai yang terlalu besar membatalkan gaya pantul tembok dan membuat vektor dorong masuk menjadi lebih dominan.

### Konteks B: Kegagalan Fisika di Sudut Siku-Siku (Friction & Clipping)
1. **Akar Masalah (Clipping):** Metode pemindahan `transform.position` murni bersifat seperti teleportasi dan mengabaikan benturan fisik (Collision). Dipadukan dengan keterbatasan `SphereCast` yang buta akan tembok kedua di sudut siku-siku, NPC akhirnya menyelip/menembus masuk ke dalam sudut tembok.
2. **Solusi Tahap 1 (Rigidbody Velocity):** Kami menambahkan komponen `Rigidbody` (Non-Kinematic) pada NPC agar mesin fisika Unity mencegah badannya menembus objek solid. Skrip diubah agar mentransfer kalkulasi AI langsung ke `rb.velocity` (menghindari bug desync lambat antara Update dan FixedUpdate).
3. **Masalah Turunan (Tertahan Gaya Gesek / Stuck):** Setelah Rigidbody ditambahkan, NPC selamat dari bug menembus sudut, namun ia malah **stuck (berhenti)** saat mencoba meluncur menyusuri tembok datar.
4. **Solusi Tahap 2 (Slippery Physic Material):** Kebuntuan di atas disebabkan oleh *Gaya Gesek (Friction)* bawaan Rigidbody yang bergesekan dengan tembok. Solusinya, kami membuat *Physic Material* khusus dengan parameter `Dynamic & Static Friction = 0` (Slippery). Material pelicin ini dipasang pada kapsul NPC, sehingga NPC kembali dapat meluncur mulus di tembok tanpa pernah bisa menembusnya lagi.

### Konteks C: Rotasi dan Kekacauan Radius
1. **Masalah Badan Berputar 360 Derajat (Gasing):** Penggunaan `Quaternion.Slerp` menyebabkan badan terpelintir jauh ketika vektor berbalik arah mendadak. Selain itu, fitur *Separation* salah membaca tembok sebagai agen lain.
   * **Solusi:** Menerapkan `Vector3.RotateTowards` yang mengunci batas maksimal putaran, serta memfilter deteksi *Separation* murni hanya untuk sesama `SteeringAgent`.
2. **Masalah Mendorong Player Tanpa Henti:** Nilai *Stop Radius* yang disetel terlalu kecil (`0.2`) membuat NPC mencoba mendesak masuk ke dalam ruang fisik kapsul Player (yang tebalnya `0.5`).
3. **Solusi Dynamic StopRadius:** Kami memodifikasi `NPCBrain.cs` agar *Stop Radius* menyesuaikan secara cerdas dengan targetnya. Saat mengejar Player, *StopRadius* membesar jadi `1.5` agar mengerem dengan aman. Saat menuju *Waypoint* atau titik hilangnya Player, *StopRadius* mengecil ke `0.5` agar NPC bisa mendarat presisi di titik koordinat.

### Konteks D: Jebakan "Local Minimum" pada Mode Search
1. **Masalah Terjebak Selamanya di Ujung Tembok:** Saat Player menghilang berbelok ke balik dinding, NPC berusaha menuju koordinat pastinya. Namun, karena terhalang dinding, vektor maju dan vektor mundur bertabrakan sempurna. NPC terjebak seumur hidup di ujung dinding tanpa bisa memulai *Timer* toleh kiri-kanan.
2. **Solusi "Travel Timeout" (Rencana Cadangan AI):** *Local Steering* memang secara teoritis tidak bisa mendeteksi rute memutar (tanpa bantuan NavMesh). Karenanya, terjebak adalah hal logis. Untuk mengatasi bug *"stuck selamanya"*, kami mengimplementasikan *Search Travel Timer* (5 detik). Jika dalam 5 detik NPC berjuang dan tidak bisa menyentuh titik tersebut (karena terhalang ujung tembok), AI akan menyerah, berasumsi jejaknya hilang, berhenti di tempat, lalu menoleh ke kiri-kanan sebelum kembali berpatroli. Ini merepresentasikan respons penjaga bodoh yang sangat realistis.
