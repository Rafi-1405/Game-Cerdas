# Dead Depths (`DeadDepths`)

**3D Isometric Zombie-Survival Dungeon Crawler** dengan **Multi-Cluster Procedural Content Generation (PCG)**, **5-Metric Dynamic Difficulty Adjustment (DDA)**, dan **Multi-Archetype FSM Game AI** menggunakan **Unity 6 (`6000.6.0f1`) + Universal Render Pipeline (URP)**.

---

## Ringkasan Fitur Utama

1. **Multi-Cluster Procedural World (`WorldGenerator.cs` + `WorldValidator.cs`):**
   - Menghasilkan peta *semi-open-world* `64 × 64` sel (`CellSize = 2.0m`, total `128 × 128m`) dengan **4–6 cluster organik**.
   - Terhubung menggunakan **Prim's Minimum Spanning Tree (MST)** + koridor loop opsional.
   - Cluster `Start` dan `Evacuation` ditempatkan pada jarak terjauh, disertai *abandoned structures*, *supplies crates*, *muddy patches*, *breakable/unbreakable blockers*, dan bingkai hutan prosedural.
   - Divalidasi oleh **BFS Connectivity Validator** (`WorldValidator`) sebelum runtime **NavMesh Baking** (`NavMeshRuntimeBaker`).

2. **3 Archetype Zombie AI + Shared FSM (`ZombieController.cs`):**
   - **Walker** (`HP 80`, `Speed 3.5 m/s`): *Melee* dengan deteksi LOS & jarak serang `2.0m`.
   - **Spitter** (`HP 60`, `Speed 6.2 m/s`): Serangan jarak jauh `10.0m` dengan fase *Preparation (`1.0s`)*, proyektil `SpitterProjectile` (`18 m/s`, `15 dmg`, dapat menghancurkan barikade *breakable*), dan *Recovery (`3.0s`)*.
   - **Charger** (`HP 160`, `Speed 2.4 m/s`, skala `1.3x`): Aktivasi `9.0m`, fase *TelegraphTracking (`3.0s`)* dengan garis merah `ChargerTelegraph`, *StepBack (`0.5s`)*, *Charging (`15 m/s`)* via `Physics.SphereCastAll`, dan *Stunned (`5.0s`)* jika menabrak tembok/pilar/pemain.
   - **Shared FSM:** `IdlePatrol` $\leftrightarrow$ `Chase` $\leftrightarrow$ `Attack` $\rightarrow$ `Search` (menuju `LastKnownPlayerPos`) $\rightarrow$ `IdlePatrol` / `Dead`. *(Catatan: `ZombieState.Investigate` dicadangkan sebagai reserved enum — lihat [FINAL_PROJECT_DOCUMENTATION.md](./FINAL_PROJECT_DOCUMENTATION.md)).*

3. **Dynamic Difficulty Adjustment (`PerformanceEvaluator.cs` + `DifficultyController.cs`):**
   - Mengevaluasi performa pemain pada akhir sektor menggunakan **5 metrik berbobot**:
     `Health (30%) + Kills (20%) + Time (15%) + Damage Avoidance (20%) + Resource Efficiency (15%)`.
   - Mengklasifikasikan tingkat kesulitan sektor berikutnya ke `Struggling` ($\le 0.39$), `Balanced` ($\le 0.69$), atau `PerformingWell` ($> 0.69$) untuk mengatur jumlah zombie, komposisi `W/S/C`, kualitas loot, suplai medis, dan jumlah *blocking objects*.

4. **Asset Non-Default, URP See-Through & Audio Feedback:**
   - Menggunakan material PBR `YughuesFreeGroundMaterials` & `YughuesFreeArchitecturalMaterials` (`M_YFGM_Ground01`, `M_YFGM_Dry02`, `M_YFGM_SoilMoss`, `M_YFGM_Clay`, `M_YFAM_BricksWeathered`, `M_YFAM_WoodFlooring`, `M_YFAM_Plywood`, `M_YFAM_GabionWall`, `M_YFAM_Ceiling`), model animasi `PlayerChar.fbx` & `ZombieChar.fbx`, serta `SurvivalSoundLibrary.asset`.
   - Pohon prosedural terbagi **2 submesh** (batang `M_YFGM_Dry02` + daun `M_YFGM_SoilMoss`), helikopter memiliki child `MainRotor` berputar `720°/s`, dan `GreenSmokeCanister` memancarkan partikel asap hijau URP.
   - `ObstructionFadeController` mengonversi material URP Opaque penghalang kamera secara sementara ke Transparent (`_Surface = 1`, `renderQueue = 3000`) saat pemain tertutup tembok/pohon, dan memulihkan material asli saat bersih.
   - `AudioManager` memutar ambient loop `SfxCue.Wind` serta cue aksi (`FootGrass`, `PunchSwing`, `PunchHit`, `ZombieGroan`, `ZombieAttack`, `ZombieDeath`, `Creak`, `Pickup`, `ExtractionSuccess`).

5. **AI & PCG Debug Mode (`[F1]`) (`DeadDepthsDebugMode.cs`):**
   - Menampilkan status hidup & FSM zombie, **Target aktif** (`Tgt:Player`, `Tgt:LKP`, `Tgt:Patrol`, `Tgt:None`), **AI State Change Log** (ring-buffer 10 transisi terakhir), FOV cone, garis LOS, jalur NavMesh, marker `[PATROL DEST]` & `[SEARCH TARGET]`, minimap grid PCG live, serta rincian parameter aktif **DDA Profile**.

---

## Kontrol Pemain

| Input | Aksi |
| :--- | :--- |
| `W / A / S / D` atau `Arrow Keys` | Gerak karakter (orientasi kamera isometrik $45^\circ$) |
| `Left Shift` | Sprint (menguras Stamina) |
| `C` | Toggle Crouch |
| `Left Mouse` / `Space` / `F` | Punch Attack (menyerang zombie & barikade *breakable*) |
| `Hold E` | Membuka `SuppliesCrate` (`2.0s` progress bar) |
| `R` | Retry cepat saat layar *YOU DIED* |
| `F1` | Toggle **AI / PCG / DDA Debug Mode** |
| `G` | Toggle **God Mode** (kecepatan tinggi + kebal + *noclip*, khusus pengujian/demo) |

---

## Cara Menjalankan & Menguji

### 1. Menjalankan Game (Standalone / Unity Editor)
- **Windows Standalone:** Jalankan `Builds/DeadDepths.exe`.
- **Unity Editor (`6000.6.0f1`):** Buka folder proyek `Game-Cerdas/`, buka scene `Assets/Scenes/DeadDepths.unity`, lalu tekan **Play**.
- Untuk meregenerasi scene dari skrip editor: menu **`DeadDepths -> Build Main Scene`**.

### 2. Menjalankan Automated Verification (`100` Assertions)
- Di Unity Editor: menu **`DeadDepths -> Run Automated Verification`**.
- Atau via Unity CLI Batchmode (pastikan Editor GUI sedang ditutup agar `Temp/UnityLockfile` tidak terkunci):
  ```powershell
  & "C:\_MySpace\Unity\6000.6.0f1\Editor\Unity.exe" -batchmode -quit `
    -projectPath "c:\_MySpace\Tugas Tim\Tugas Tim\Semester 7\Game Cerdas\Game-Cerdas" `
    -executeMethod DeadDepths.Editor.DeadDepthsAutomatedTests.RunVerification `
    -logFile "Logs\batch_tests.log"
  ```

### 3. Build Windows Standalone
- Di Unity Editor: menu **`DeadDepths -> Build Windows Standalone`**.

---

## Dokumentasi Lengkap EAS

Seluruh dokumen perancangan, diagram arsitektur (Slide 71), tabel parameter, 5 skenario pengujian formal, evaluasi keterbatasan, pembagian tugas tim, dan panduan urutan demo live tersedia di:
- **[FINAL_PROJECT_DOCUMENTATION.md](./FINAL_PROJECT_DOCUMENTATION.md)**
- Salinan arsip workspace: `../_dokumentasi/EAS_Final_Project_Dokumentasi.md`
