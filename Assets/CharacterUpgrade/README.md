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

Crouch uses a 1.50 m capsule, preserves foot height, and cannot stand under low overhead obstacles. The Crouching blend tree plays the original Crouching Idle.fbx and Crouched Walking.fbx clips with Foot IK enabled. Generated Balanced clips are retained but unused. No CrouchPoseAlignment or procedural pelvis-lowering correction is attached. The controller moves the character; locomotion root trajectories are extracted and not applied by the Animator.

Punch has a 1.03 s cooldown, 1.6 m range, 110-degree frontal arc, 20 damage at 0.32 s, and checks physical occlusion. Zombie attack uses its own clip, 2.7 s cooldown, and 15 damage at 0.65 s with range/visibility rechecks. Zombie sight targets adjust with player crouch height and include migrated Default-layer walls.

## Assets and controllers

The current player uses Models/PlayerChar.fbx, and the scene zombie plus Prefabs/Zombie.prefab use Models/ZombieChar.fbx. Both supplied models use their own valid Humanoid Avatar to retarget the existing animation clips. Their visual height is fitted to 1.85 m with feet aligned to the existing actor collider bottom. Actor transforms, colliders, controller settings, AI, navigation data and map placements are preserved.

Embedded textures are extracted into Textures/PlayerChar and Textures/ZombieChar. Materials in Materials/PlayerChar and Materials/ZombieChar use URP Lit with the supplied diffuse, normal and specular maps. Player glossiness is packed into Ch31_SpecGloss alpha. Hair and torn zombie clothing use alpha clipping. Original Resources models, the previous Humanoid fallback, and legacy practice scenes are retained.

Player.controller: Idle, Walking2, Running, Crouching Idle, Crouched Walking, Punching. Walking2 replaces only the previous standing blend tree walk motion; blend thresholds and transitions are preserved.
Zombie.controller: zombie idle, zombie walk, zombie run, zombie attack, zombie death.

The horde manager now uses Prefabs/Zombie.prefab. Seven spawn zones follow the current town. Spawns require a complete path to the player's navigation region, avoid elevated surfaces, check body clearance, and exclude the fortress camp. Nonempty custom spawn-zone lists are preserved when edited.

## After editing the map

Select Gameplay Navigation, then use its NavMeshSurface Bake button. Keep agent type ZombieUnity Human and Physics Colliders collection. Update spawn-zone centers if the town is moved. The Animator Controller assets can be edited normally in Unity.

## Backups and verification

Backups/UTS_BeforeCharacterUpgrade_20261020.unity is the saved scene before this update. Backups/OriginalScripts contains the previous runtime scripts as text backups.

Reports contain the navigation paths, live Play Mode checks, and setup/geometry-preservation results. The runtime checks cover real controller input, walk/run/crouch transitions, low-ceiling clearance, punch impact/occlusion, NPC chase, attack damage, and death animation.

## Character model replacement

Backups/ModelSwap_* contains the live scene, zombie prefab and player controller before replacing the models. Reports/ModelSwap.json records the current model paths and preservation checks. Walking2 is imported as a looping Humanoid clip with root trajectory extracted; the existing player controller controls movement.

ModelSwapPlayMode.json records the new rigs and Walking2 passing the existing walk/run/crouch, headroom, punch, chase, attack, damage and death checks without runtime errors. All 33 NPCs were on the NavMesh; 32 moved more than 0.3 m in the initial 9-second sample, including the scene Enemy (3.39 m).

## Earlier Foot IK change (historical; incomplete diagnosis)

Standing state now enables Foot IK (iKOnFeet), so both Walking2 and Running use Unity humanoid foot correction for PlayerChar. Clip choices, blend thresholds, model/Avatar, root motion settings and gameplay are unchanged. Before changing it, the current scene and controller were copied into Backups/Locomotion_20261020_163512. Reports/PlayerFootIK.json and PlayerFootIKPlayMode.json record the checks. Across-frame Play Mode captures confirm mesh deformation; rendering multiple editor poses within a single frame can reuse stale GPU skinning.

## Earlier crouch correction (historical)

Reports/PlayerCrouchFix.json, PlayerCrouchCycle.jsonl and PlayerCrouchPlayMode.json record the current fix and actual controller-input verification. Backups/Crouch_20261020_182735 preserves the previous scene, controller and correction script. Avoid the older align-crouch/setup commands: those creation helpers restore the obsolete correction.

## Earlier balanced crouch poses (superseded)

The previous repair generated Crouch Idle Balanced.anim from averaged crouch-walk motion and adjusted bilateral balance, foot alignment and gaze. That artistic change made crouching too high and did not address the Avatar reference problem. Both generated clips are now unused; their reports describe the previous state only.

## Current repair: shared PlayerChar Avatar reference

Original source FBX files are unchanged and byte-identical to the supplied files. The running motion itself does not reverse its knees. Animation-only FBXs had been imported with separate Create From This Model Avatars from posed skeleton references. Their reference knee rotations differed from PlayerChar. The five clips with the same skeleton as PlayerChar (Idle, Running, Crouching Idle, Crouched Walking and Punching) now use Copy From Other with the PlayerChar Avatar. Walking2 has a different skeleton and was not changed in this repair.

The crouch blend tree again uses the original crouch idle and crouch walk. Their authored asymmetry is preserved. The collider is now 1.50 m; original crouch walking has a maximum sampled head height of 1.317 m above the floor. No procedural body correction is used. Animator graph, blend thresholds, transitions, Foot IK and movement speeds are retained.

Reports/AvatarReferenceRepair.json summarizes the repair. RunningSourceComparisonBefore.json and RunningSourceComparisonAfter.json compare 32 phases with a Generic import of the original source motion. Mean knee angle error fell from 31.136 to 0.017 degrees without Foot IK, and from 18.773 to 0.956 degrees with Foot IK. AvatarReferenceRunningCycle.jsonl records the real controller's running transition/cycle; sampled knee bend planes stayed forward. AvatarReferenceCrouchCycle.jsonl records the original crouch motions. AvatarReferencePlayMode.json records all 14 functional checks passing with no runtime errors. These are sampled checks, not a guarantee for every future animation blend or terrain condition.

Backups/MotionRepair_20261020_192344 preserves the scene, controller and prior importer settings. All 23,469 scene Transform blocks match that fresh backup. The current scene is saved and back in Edit mode.

Do not rerun old imports, normalize, setup, align-crouch or CrouchPosePolish creation commands: they may restore obsolete Avatar or crouch settings. Inspect live user changes before any further repair.


## Later survival presentation update

See Assets/SurvivalUpgrade/README.md and Reports/UpgradeSummary.json. Player now uses the existing zombie death motion with a death-only ground contact pass. Living walk/run/crouch animation and the Avatar reference repair above are retained. Items, CC0 SFX and night sky were added in this later update.
