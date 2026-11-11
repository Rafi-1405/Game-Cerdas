# Praktikum 10: Procedural Dungeon / Level Generation di Unity

Proyek ini mengimplementasikan generator dungeon prosedural berbasis grid di Unity menggunakan teknik **Binary Space Partitioning (BSP)** dan validasi keterjangkauan jalur menggunakan algoritma **Breadth-First Search (BFS)**.

---

## Fitur Utama

1. **Layout Dungeon Berbasis Grid**:
   - Struktur grid 2D diwujudkan menjadi scene Unity 3D dengan modul modular: *Room Floor*, *Corridor Floor*, *Wall Blocks* (dengan collider), dan *Doors*.
   - Menggunakan material Universal Render Pipeline (URP) Lit dengan palet warna modern.

2. **Pengaturan Seed & Parameter Generator**:
   - **Seed Input & Display**: Menampilkan seed aktif di layar dan memungkinkan pengujian seed kustom atau acak.
   - **Ukuran Peta**: Slider lebar dan panjang grid (24 hingga 72 cells).
   - **Parameter BSP**: Ukuran minimum/maksimum ruangan, margin padding, dan kedalaman partisi rekursif.
   - **Lebar Koridor**: Pilihan koridor 1 hingga 3 tile.
   - **Penjelasan Parameter**: Panel panduan interaktif yang menjelaskan dampak setiap parameter terhadap geometri level.

3. **Penempatan Entitas & Validasi Zona Aman**:
   - **Start Point**: Titik awal spawn pemain dengan penanda platform emerald.
   - **Zona Aman (Safe Zone)**: Radius perlindungan di sekitar Start agar musuh tidak dapat spawn di dekat pemain.
   - **Goal Shrine**: Monolit kristal bercahaya emas dengan trigger kondisi selesai (*win condition*).
   - **Enemies**: Entitas patroli otonom pada lantai valid di luar zona aman.
   - **Items**: Kristal kolektibel yang mengambang dan berputar dengan animasi bobbing.

4. **Validasi & Perbaikan Jalur (BFS / Flood Fill)**:
   - Memeriksa konektivitas langsung dari Start menuju Goal dan seluruh item.
   - Menghitung jalur terpendek (*shortest path*) dan persentase cakupan lantai (*connectivity percentage*).
   - Melakukan perbaikan koridor otomatis (*repair*) atau regenerasi berulang jika validasi gagal (hingga batas batas retry).

5. **Dapat Dimainkan (Playable)**:
   - Karakter pemain bergerak halus dengan input WASD / Tombol Panah.
   - Sistem kamera multi-mode dengan tombol **C** (*Isometric Top-Down*, *Third-Person Follow*, dan *Overview*).
   - Radar Minimap 2D interaktif dengan tombol **M**.
   - Notifikasi kemenangan saat mencapai Goal.

---

## Panduan Pengujian & Video Demo

| Skenario Pengujian | Langkah Pengujian | Hasil yang Diharapkan |
| :--- | :--- | :--- |
| **1. Konsistensi Seed yang Sama** | Masukkan angka seed (misal `77123`), lalu klik tombol **Regenerate Same Seed**. | Layout dungeon, posisi ruangan, dan entitas terbentuk 100% identik. |
| **2. Variasi Seed Berbeda** | Klik tombol **Random** pada panel HUD untuk menghasilkan seed baru. | Layout dungeon berubah total dengan susunan ruangan dan koridor yang berbeda. |
| **3. Perubahan Parameter Generator** | Ubah slider **Min/Max Room Size** atau **Corridor Width**, lalu generate ulang. | Ukuran ruangan membesar/mengecil dan koridor melebar secara proporsional. |
| **4. Validasi Konektivitas & Spawn** | Perhatikan kotak status **Connectivity & Validation** di HUD serta Scene Gizmos / Minimap. | Status menunjukkan `PASS (Fully Reachable)`, panjang path ditampilkan, dan musuh tidak ada di zona aman. |
| **5. Gameplay Start hingga Goal** | Gerakkan pemain menggunakan tombol **WASD** dari platform Start menuju Goal Shrine. | Pemain dapat mengumpulkan item dan memicu banner kemenangan: *"★ GOAL REACHED! DUNGEON CLEARED! ★"*. |

---

## Kontrol Input

- **W, A, S, D** / **Panah**: Menggerakkan pemain
- **C**: Mengganti mode kamera (*Isometric Top-Down* ➔ *Third-Person* ➔ *Overview*)
- **M**: Menampilkan / menyembunyikan Radar Minimap 2D
- **F1**: Menampilkan / menyembunyikan Panel Kontrol HUD
- **Mouse Scroll**: Zoom in / zoom out kamera
