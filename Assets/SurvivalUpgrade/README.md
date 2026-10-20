# Survival presentation update

Project: `C:\Users\Desktop\Downloads\Game Cerdas`
Scene: `Assets/Scenes/UTS.unity`

## Items

The three models in the supplied `Downloads/ItemAssets/Items.obj` are split into individual OBJ assets, retaining source UVs/normals and textures. The off-center source coordinates are recentered at the bottom of each model and scaled to meters. The source files remain untouched.

| Pickup | Width | Height | Depth |
|---|---:|---:|---:|
| Bandage | 0.18 m | 0.088 m | 0.088 m |
| First Aid Kit | 0.30 m | 0.243 m | 0.089 m |
| Medkit | 0.38 m | 0.148 m | 0.272 m |

`Assets/SurvivalUpgrade/Items/Prefabs` contains complete pickups with URP Lit materials and trigger colliders. The current ItemSpawner references all three prefabs. Authored model colors/scale are preserved instead of the previous cube tint/scale override. Model pickups keep the original floating/spinning behavior: 60 degrees per second, bob amplitude 0.12 m and frequency 2.5. They use the spawner's original position/itemElevation instead of being snapped to support. Existing item values, weights, pickup rules, counts and spawn region are retained. Legacy scenes without prefab assignments keep their original fallback behavior.

## Player death

The existing non-looping `zombie death.fbx` is retargeted by the player's existing Humanoid Avatar in a new `Death` state. Any State enters it when `Dead` is true; there is no automatic transition out, so the corpse holds its last pose. The retargeted final pose initially put the head mesh 14.1 cm below the support plane. A death-only visual contact pass measures the deformed skin and lifts the entire visual just enough to clear that plane by 6 mm. All joint animation is retained, living animation is untouched, and the correction stops evaluating once the corpse settles. Revival restores the original visual offset. Player movement/aim/punch input stops at zero health. Gravity still grounds the controller. `ResetHealth` clears death and restores Standing; `ResetPosition` continues to restore capsule, position and health. Current walk/run/crouch clips, Avatar reference settings and blend thresholds are retained.

## Sound

`Survival Atmosphere` in the scene references `Audio/SurvivalSoundLibrary.asset`. Its inspector exposes master, effects and ambience volumes. There are 58 clips in 22 cues:

- Concrete, grass/soil, timber and carpet footsteps, driven by actual distance traveled; sprinting is louder and crouching quieter.
- Short air whoosh for the unarmed punch swing, plus a separate flesh impact only on a confirmed damage hit. PunchSwing references fist_air_whoosh_1/2, derived from artisticdude's CC0 recorded air swishes; knifeSlice clips are unused.
- Zombie groans, attack/hurt voices, death and body fall, including spawned zombies.
- Player hurt, death, low-health heartbeat.
- Item pickup and full-backpack feedback; flashlight toggle; pause/resume tap.
- Extraction countdown, objective notification and extraction confirmation.
- Wind and a quiet, threat-reactive tension bed; occasional creaks at existing gate/fence positions.

The SFX pool has 24 voices, with a separate limit of five simultaneous zombie vocalizations. Spatial effects attenuate with distance and use a low-pass filter plus reduced volume when physical geometry obstructs the listener. Clips vary to avoid immediate repeats. Ambience uses streamed audio; short effects preload. Tension layers crossfade at loop boundaries. Audible SFX do not change the existing AI hearing-radius system.

Downloaded audio is CC0 1.0. `Licenses/README.md`, `SourceManifest.json`, `AudioFileManifest.json`, author page snapshots and Kenney's included license texts record the source and license of every clip. Original procedural UI tones/heartbeat are also dedicated to CC0. Supplied item models are separate user assets; no CC0 claim is made about them.

## Night

`Sky/NightSky.mat` uses the project-authored `ZombieUnity/Night Sky` shader, with a dark clouded sky, sparse stars and a moon aligned with the existing directional light. The existing NightEnvironmentController is configured for the same cool 0.20 moonlight in both Edit and Play mode. Low ambient light retains readable silhouettes. Blue distance fog helps the map boundary recede. Existing street/building/fortress lights and player flashlight are retained. All existing scene transforms are preserved.

## Maintenance

`Backups/Survival_*` contains the fresh scene/controller and original versions of the five edited existing runtime scripts. Existing user placements remain authoritative. Do not rerun older map/layout or character setup generators. `SurvivalUpgradeTools.Apply` refuses to regenerate an already installed scene; patch current components instead.

Editor command bridges live under `Library/ZU_Survival`; command queues are cleared after work. Test objects and temporary movement occur in Play Mode and are discarded on exit. See `Reports` for the latest completed checks and preservation report.
