# Dokumentasi Praktikum 1: NPC Dasar & Sensor Jarak Visual

Praktikum 1 berfokus pada pengenalan konsep dasar deteksi jarak tanpa melibatkan kecerdasan navigasi (*Brain* atau *Actuator*). NPC murni bertindak sebagai sensor statis.

## 1. Skrip Utama: `EnemyDetector.cs`
Skrip ini mengimplementasikan pendeteksian pemain secara 360 derajat menggunakan perhitungan jarak matematis murni.

### A. Logika Jarak (Distance)
- Menggunakan `Vector3.Distance(transform.position, player.position)` untuk mengkalkulasi seberapa jauh pemain dari NPC secara *real-time*.

### B. Status (Visual State)
Skrip ini belum memiliki *Finite State Machine* untuk pergerakan, melainkan 3 status murni untuk mengubah warna visual (representasi ketertarikan NPC):
1. **Idle (Biru):** Pemain berada di luar jangkauan radius apapun.
2. **Suspicious (Kuning):** Pemain masuk ke dalam `suspiciousRadius` (area peringatan awal).
3. **Alert (Merah):** Pemain masuk ke dalam `alertRadius` (area ancaman dekat).

NPC mengubah warna secara langsung dengan memodifikasi material pada komponen `Renderer` (contoh: `enemyRenderer.material.color = alertColor`).

### C. Visual Debugging (Gizmos)
Menggunakan `OnDrawGizmosSelected` untuk menggambar batasan area menggunakan:
- `Gizmos.DrawWireSphere` (Warna Merah) untuk radius Alert.
- `Gizmos.DrawWireSphere` (Warna Kuning) untuk radius Suspicious.
Kelemahan dari sistem Praktikum 1 ini adalah deteksi menembus dinding dan pandangan tidak berarah (berbentuk bola 360 derajat penuh).
