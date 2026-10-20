# NPC navigation and character animation update

Scene: Assets/Scenes/UTS.unity

## Why NPCs stopped

The only previous NavMeshSurface was on the inactive Environment root. The live migrated scene had zero active NavMesh triangles. Existing AI scripts require an active NavMeshAgent bound to navigation data, so their update loop returned without moving.

The new Gameplay Navigation object loads TownNavMesh.asset, baked against the current map's physical colliders. The dedicated ZombieUnity Human agent type uses a 0.30 m radius, 1.85 m height, 0.30 m step, and 0.08 m voxels. It includes streets, interiors and the fortress. Roof/ceiling surfaces are marked non-walkable. Existing map transforms are preserved.

## Controls

- WASD / arrow keys: walk (2.3 m/s)
- Hold Shift while moving: run (5.2 m/s, existing stamina system)
- Hold Ctrl: crouch; stationary crouch idle and crouch walk (1.1 m/s)
- Space or left mouse button: punch
- Right mouse button: existing aim/flashlight behavior

Crouch uses a 1.40 m capsule, preserves foot height, and cannot stand under low overhead obstacles. Humanoid IK keeps the supplied crouch walk's feet while aligning its body height with the deeper idle pose. The controller remains responsible for movement; locomotion root trajectories are extracted and not applied by the Animator.

Punch has a 1.03 s cooldown, 1.6 m range, 110-degree frontal arc, 20 damage at 0.32 s, and checks physical occlusion. Zombie attack uses its own clip, 2.7 s cooldown, and 15 damage at 0.65 s with range/visibility rechecks. Zombie sight targets adjust with player crouch height and include migrated Default-layer walls.

## Assets and controllers

The supplied CharAssets FBXs contain animation/skeleton data only, without a character mesh. The existing Y Bot model was copied to Models/Humanoid.fbx and converted to a Humanoid Avatar; original Resources models and other legacy practice scenes are retained.

Player.controller: Idle, Walking, Running, Crouching Idle, Crouched Walking, Punching.
Zombie.controller: zombie idle, zombie walk, zombie run, zombie attack, zombie death.

The horde manager now uses Prefabs/Zombie.prefab. Seven spawn zones follow the current town. Spawns require a complete path to the player's navigation region, avoid elevated surfaces, check body clearance, and exclude the fortress camp. Nonempty custom spawn-zone lists are preserved when edited.

## After editing the map

Select Gameplay Navigation, then use its NavMeshSurface Bake button. Keep agent type ZombieUnity Human and Physics Colliders collection. Update spawn-zone centers if the town is moved. The Animator Controller assets can be edited normally in Unity.

## Backups and verification

Backups/UTS_BeforeCharacterUpgrade_20261020.unity is the saved scene before this update. Backups/OriginalScripts contains the previous runtime scripts as text backups.

Reports contain the navigation paths, live Play Mode checks, and setup/geometry-preservation results. The runtime checks cover real controller input, walk/run/crouch transitions, low-ceiling clearance, punch impact/occlusion, NPC chase, attack damage, and death animation.
