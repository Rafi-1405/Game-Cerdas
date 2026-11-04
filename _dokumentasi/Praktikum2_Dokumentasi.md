# Dokumentasi Praktikum 2: Finite State Machine & Multi-Sensor NPC

Praktikum 2 mengembangkan sistem menjadi *Agent* yang cerdas dengan arsitektur penuh: **Perception (Sensor) → Memory → Decision (Brain) → Action (Actuator/NavMesh)**.

## 1. Arsitektur Modular
Kode dipisah menjadi 4 komponen utama:
- `PlayerController`: Penggerak & *Emitter* status (pembuat suara).
- `NPCSensor`: Sistem Mata & Telinga.
- `NPCBrain`: FSM Logika & Pengambil Keputusan.
- `CharacterModelVisual`: Pengatur Animasi & Visualisasi 3D (Hasil kolaborasi tim).

## 2. Mekanik Player (`PlayerController.cs`)
- **Fisika:** Pergerakan telah diperbarui menggunakan `Rigidbody.MovePosition` di dalam `FixedUpdate` agar solid dan tidak menembus dinding.
- **State Emisi Suara:**
  - `Idle`: Diam, 0 suara.
  - `Sneak` (Tahan CTRL): Kecepatan lambat, 0 suara (*Stealth*).
  - `Walk` (Default): Kecepatan normal, radius emisi suara = 5.
  - `Run` (Tahan Shift): Kecepatan tinggi, radius emisi suara = 10.

## 3. Sistem Sensor (`NPCSensor.cs`)
- **Visual Sensor (Mata):** 
  - Menggunakan *Field of View* berbentuk kipas (dibatas oleh `viewAngle` dan `viewRadius`).
  - Menggunakan `Physics.Raycast` berlapis `obstacleMask` (Layer "Wall") untuk menghasilkan *Line of Sight* murni yang tidak tembus pandang.
- **Audio Sensor (Telinga):**
  - Mengecek State pemain. Mengabaikan jika berstatus diam/mengendap.
  - Jika pemain masuk area dengar, titik pusat suara akan disimpan ke `LastHeardPosition`.

## 4. Logika Otak NPC (`NPCBrain.cs`)
NPC memiliki siklus pengambil keputusan yang berjalan setiap *frame* (Prioritas tidak saling bentrok):
1. 🔴 **Chase (Prioritas Tertinggi):** Jika Mata melihat pemain. NPC berlari menggunakan `NavMeshAgent` ke posisi pemain. Menyimpan lokasi pemain ke variabel Memori `lastKnownPosition`.
2. 🔵 **Search (Prioritas Kedua):** Jika kehilangan jejak dari state *Chase*. NPC pergi ke titik memori terakhir. Sesampainya di sana, NPC memutar badan menyapu pandangan (*Look Around*) menggunakan logika `Mathf.Sin`. Otomatis memanggil rute Patroli jika waktu pencarian habis.
3. 🟡 **Suspicious (Prioritas Ketiga):** Dipicu oleh pendengaran *ketika sedang berpatroli*. (State ini diblokir saat NPC sedang mode *Search* agar tidak membatalkan pencarian). NPC akan berhenti berjalan lalu memutar perlahan tubuhnya menghadap pusat suara.
4. 🟢 **Patrol (Prioritas Keempat):** State default. NPC berjalan mengelilingi rangkaian titik *Waypoints* secara berulang.

## 5. Fitur Visualisasi, Animasi, & Debugging
- **Animasi Sinkron (`CharacterModelVisual.cs`):** Kecepatan putar animasi `Walking.fbx` diatur dinamis menyesuaikan nilai `CurrentState` pada pemain, dan menyesuaikan kecepatan gerak `NavMeshAgent` pada NPC.
- **Warna Real-time:** Warna pada *Renderer* animasi secara langsung merespons state otak NPC (Hijau/Kuning/Merah/Biru).
- **Gizmos Kipas FOV:** Menggunakan `UnityEditor.Handles.DrawSolidArc` untuk menciptakan visual potongan kipas berwarna kuning transparan yang sangat rapi untuk visualisasi *Line of Sight* di Editor. Garis Laser merah akan muncul saat mata berhasil terkunci ke tubuh pemain.
- **Gizmos Telinga & Memori:** Lingkaran cyan/biru untuk batas suara berjalan/berlari. Bola Magenta untuk menvisualisasikan titik kordinat yang sedang diingat (*Memory*) oleh NPC.
