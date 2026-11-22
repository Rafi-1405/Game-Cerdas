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

4. **Asset Non-Default, URP Atmosphere & Pooled Audio Feedback:**
   - Menggunakan material PBR `YughuesFreeGroundMaterials` & `YughuesFreeArchitecturalMaterials` (`M_YFGM_Ground01`, `M_YFGM_Dry02`, `M_YFGM_SoilMoss`, `M_YFGM_Clay`, `M_YFAM_BricksWeathered`, `M_YFAM_WoodFlooring`, `M_YFAM_Plywood`, `M_YFAM_GabionWall`, `M_YFAM_Ceiling`, `M_YFAM_TilesConcave`), model animasi `PlayerChar.fbx` & `ZombieChar.fbx` (+ aksesori tulang *Spitter*/*Charger*), item 3D `Medkit`/`Bandage`/`FirstAidKit`, serta `SurvivalSoundLibrary.asset`.
   - Langit malam `NightSky.mat` (URP shader), fog atmosferik, moonlight dingin, Global URP `Volume` (`DeadDepthsVolumeProfile.asset`: ACES Tonemapping, Bloom, Vignette, Color Adjustments), serta kamera **SMAA**.
   - Pohon prosedural terbagi **2 submesh** (batang `M_YFGM_Dry02` + daun `M_YFGM_SoilMoss`), helikopter memiliki child `MainRotor` (`720°/s`) & `TailRotor`, helipad beton `H`, dan `GreenSmokeCanister` memancarkan partikel asap hijau URP.
   - `ObstructionFadeController` mengonversi material URP Opaque penghalang kamera secara sementara ke Transparent (`_Surface = 1`, `renderQueue = 3000`) saat pemain tertutup tembok/pohon, dan memulihkan material asli saat bersih.
   - `AudioManager` upgraded ke **24-voice pooled `AudioSource`** + **`AudioLowPassFilter` wall occlusion**, `SfxCue.Tension` crossfade adaptif, `SfxCue.Heartbeat` (`HP <= 35%`), langkah kaki permukaan (`FootGrass`, `FootWood`, `FootConcrete`, pitch lumpur), serta pengaturan volume tersimpan di `PlayerPrefs`.

5. **Publish-Grade Combat Feel, uGUI + TMP Interface & Debug Mode (`[F1]`):**
   - **Project Zomboid Combat Stance (`Hold RMB`):** Membidik bebas ke arah kursor dengan upper-body `PlayerAimIK`, ground reticle, sinar senter `[T]`/`[L]` mengikuti kursor, animasi mundur alami (`AnimSpeedMult = -0.85`), dan *camera look-ahead* ber-deadzone `10%` (maks `3.2m`).
   - **Dual-Action `Shift`:** Ketuk cepat (`< 0.20s` *unscaled*, dapat diatur `0.15–0.25s`) untuk **Dodge** (*Ease-Out* burst, *i-frame*, membatalkan crate & uncrouch), atau tahan (`>= 0.20s`) untuk **Sprint**.
   - **uGUI + TextMesh Pro (`1920×1080`):** Live 3D Main Menu Diorama, Gameplay HUD (ghost-drain HP bar, 3-Slot Quick-Belt `[1][2][3]`, Objective `3/3` Supply Intel + `4.0s` Helipad Hold, tabular timer, collapsible Corner Minimap `[M]`), Pause Menu `[ESC]`, Settings Modal, Ranked Evacuation Scoreboard (`S/A/B/C`), serta mode Debug `[F1]` (`DeadDepthsDebugMode`).

---

## Kontrol Pemain

| Input | Aksi |
| :--- | :--- |
| `W / A / S / D` atau `Arrow Keys` | Gerak karakter (orientasi kamera isometrik $45^\circ$) |
| `Hold Right Mouse` | **Combat Aim Stance** (hadap kursor bebas, IK, reticle, *look-ahead*, mundur alami) |
| `Tap Left Shift` (`< 0.20s`) | **Dodge** (*Ease-Out* dash + *i-frame*; *backstep* bila diam sambil Aim) |
| `Hold Left Shift` (`>= 0.20s`) | **Sprint** (menguras Stamina) |
| `C` | Toggle Crouch Stealth (menurunkan `VisionRange` zombie menjadi `0.65x`) |
| `Left Mouse` / `Space` / `F` | **3-Hit Melee Combo** (+ 15% Crit & hitstop; `LMB`/Aim membidik kursor, `F`/`Space` lurus ke depan) |
| `T` / `L` | Toggle **Tactical Flashlight** (dual-beam; bebas bentrok dengan `F`) |
| `Hold E` | Membuka `SuppliesCrate` (`2.0s`, +`1` Supply Intel & item heal/belt) |
| `1` / `2` / `3` | Quick-Use Belt: **Bandage** (`+20 HP`) / **Medkit** (`+50 HP`) / **Adrenaline** (`+40 Stamina` + speed) |
| `M` | Toggle Corner Tactical Minimap |
| `ESC` | Toggle Pause Menu / Tutup Settings |
| `R` | Retry cepat saat layar *YOU DIED* |
| `F1` | Toggle **AI / PCG / DDA Debug Mode** |
| `G` | Toggle **God Mode** (kecepatan tinggi + kebal + *noclip*, khusus pengujian/demo) |

---

## Cara Menjalankan & Menguji

### 1. Menjalankan Game (Standalone / Unity Editor)
- **Windows Standalone:** Jalankan `Builds/DeadDepths.exe`.
- **Unity Editor (`6000.6.0f1`):** Buka folder proyek `Game-Cerdas/`, buka scene `Assets/Scenes/DeadDepths.unity`, lalu tekan **Play**.
- Untuk meregenerasi scene dari skrip editor: menu **`DeadDepths -> Build Main Scene`**.

### 2. Menjalankan Automated Verification (`146` Assertions)
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
