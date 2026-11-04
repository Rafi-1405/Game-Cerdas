# Rencana Implementasi Praktikum 3: Autonomous Steering Agent

*Catatan: Dokumen ini saat ini berfungsi sebagai draf perencanaan (Roadmap). Jika Praktikum 3 telah selesai diimplementasikan, dokumen ini akan diubah dan disesuaikan menjadi catatan dokumentasi hasil akhir.*

## Tujuan Utama
Mengembangkan NPC dari sistem pergerakan kaku berbasis murni *NavMesh* menjadi agen otonom (*Autonomous Agent*) yang bergerak secara dinamis menggunakan gaya dorong matematika (*Steering Behaviors*) dan dapat menghindari rintangan secara fisik.

---

## 🚀 Rencana Fitur & Implementasi

### 1. Investigasi Suara Lanjutan (Audio Investigation)
- **Kondisi Saat Ini:** NPC hanya berhenti dan menoleh saat mendengar suara (State *Suspicious*).
- **Rencana Peningkatan:** Memodifikasi reaksi otak NPC. Setelah mendengar suara, NPC akan secara proaktif berjalan menginvestigasi menuju koordinat `LastHeardPosition`.

### 2. Algoritma Steering Behaviors Dasar
Kita akan membuat modul skrip baru (contoh: `SteeringBehaviors.cs`) untuk menghitung vektor pergerakan:
- [ ] **Seek (Mengejar):** Menghitung `Desired Velocity` (Kecepatan yang diinginkan) lurus menuju posisi target, lalu merumuskan `Steering Force` (Gaya dorong) agar NPC berbelok secara halus menuju target.
- [ ] **Arrival (Tiba):** Menambahkan logika pengereman (*Braking Radius*). Jika jarak NPC dengan target sudah dekat, kecepatannya akan dikurangi secara linier hingga berhenti mulus tepat di titik target.
- [ ] **Wander (Berkeliling Acak):** Mengganti/Melengkapi sistem *Patrol* kaku. NPC akan memproyeksikan lingkaran imajiner di depannya dan memilih titik acak (*Jitter*) pada lingkaran tersebut untuk berjalan-jalan secara natural tanpa *waypoints* yang kaku.

### 3. Obstacle Avoidance (Sensor Penghindar Rintangan)
- [ ] **Sensor Fisik:** Memasang tembakan *Raycast* atau *SphereCast* seperti "kumis" di bagian depan NPC (Tengah, Serong Kiri, Serong Kanan).
- [ ] **Logika Menghindar:** Jika tembakan tersebut mendeteksi tembok/objek (*Layer Obstacle*), NPC akan langsung menciptakan vektor gaya dorong yang berlawanan (*Avoidance Force*) untuk menjauhi rintangan tersebut.

### 4. Behavior Blending (Penggabungan Gaya)
- [ ] **Kalkulasi Vektor Akhir:** NPC tidak hanya bergerak berdasarkan satu perintah, tapi menggabungkan beberapa gaya. Contoh: 
  `Total Force = (Seek Force * bobot_seek) + (Avoidance Force * bobot_avoid)`
- [ ] **Aplikasi Pergerakan:** Mengaplikasikan `Total Force` ini ke *NavMeshAgent.velocity* (atau ke *Rigidbody* murni, tergantung spesifikasi tugas lanjutan).

### 5. Tuning & Debugging (Gizmos)
- [ ] **Inspector Tuning:** Semua parameter seperti *Max Speed*, *Max Force*, *Wander Radius*, *Wander Jitter*, dan *Avoidance Distance* wajib bisa diedit melalui Inspector.
- [ ] **Visualisasi Vektor (Gizmos):** Menggambar garis (*Lines*) di Scene View secara *real-time* untuk:
  - Garis Biru: Menunjukkan vektor kecepatan saat ini (*Current Velocity*).
  - Garis Merah: Menunjukkan target yang ingin dicapai (*Desired Velocity*).
  - Garis Kuning: Menunjukkan sensor *Raycast* untuk *Obstacle Avoidance*.
  - Lingkaran Hijau: Menunjukkan proyeksi cincin pencarian target untuk algoritma *Wander*.
