# ZombieUnity map in Game Cerdas

Open **Assets/Scenes/UTS.unity**. The current map is under **ZombieUnity - Migrated Map**.

Reusable prefab: **Assets/RobloxMigration/Prefabs/ZombieUnity_Map.prefab**.

## Imported

- 11,628 original hierarchy nodes, 10,585 visible source parts, 321 Unity materials, 39 lights, 7 labels, 8,508 colliders.
- Native OBJ geometry and UVs, exported diffuse/normal/specular/emission image maps. Source names, model pivots and placements preserved; coordinate conversion .28 meters/stud with Unity Z reflected.
- Supplemental MissingObjects.obj supplied Truck003 and Tire Stack; both imported at their original transforms.
- URP glass alpha, vegetation cutouts and lighting adapted after inspecting five rendered views.

## Verification

- No missing materials or unsupported shaders.
- Max position error 0.0000112 meters, rotation error 0 degrees.
- 20 direct doorway sweeps clear; 22 rampart stair treads supported; player spawn has no overlapping map colliders.
- Live Roblox source check: all 10,603 part transforms and sizes unchanged.

## Source limitations

Three source meshes already report Failure in Roblox and are absent from the exports: JunkFirebird (703128376), JunkFairlane (2596054139), pick up (1666172407). Their original hierarchy/transform metadata is retained; no substitute vehicle meshes were created.

Roblox scripts and gameplay do not translate with OBJ. Existing Unity gameplay objects remain. NPC navigation has not been rebaked for this map.

The old Unity Environment is retained inactive to prevent overlapping geometry. Backup: **Assets/RobloxMigration/Backups/UTS_BeforeRoblox_20261020.unity**. Only the player height was raised 0.086 m so its capsule starts above the new surface.

See Reports/ImportReport.json, Reports/Audit.json, and Reports/DisplayAdaptation.json. Original snapshot and both converted geometry packs are in Source. Do not regenerate over manual changes; importer guards against a second map root.
