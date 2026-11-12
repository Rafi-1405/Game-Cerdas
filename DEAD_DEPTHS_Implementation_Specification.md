# DEAD DEPTHS

## Game Design and Implementation Specification for Antigravity

**Engine:** Unity\
**Game format:** 3D, isometric-view, semi-open-world zombie dungeon
crawler\
**Core systems:** Procedural Content Generation (PCG), Dynamic
Difficulty Adjustment (DDA), FSM-based Game AI\
**Document purpose:** Implementation specification. Read this document
before modifying the Unity project. Treat it as the current source of
truth for the game design described here.

------------------------------------------------------------------------

# 1. Project Overview

## 1.1 High concept

DEAD DEPTHS is a 3D isometric zombie-survival dungeon crawler presented
as a continuous, semi-open-world map.

Each level is a procedurally generated outdoor environment. The playable
area is surrounded by impassable tree boundaries and other natural
terrain constraints, giving the impression of an open world while
retaining a finite, dungeon-like level structure.

The player's objective is to collect supplies from abandoned structures
and reach a designated extraction safe zone. A helicopter is located
inside the safe zone. The level is completed only when the player enters
the safe zone and then enters the helicopter.

After extraction, display a simple completion screen:

> Supplies Collection X Cleared

`X` is the current level number.

The completion screen must contain no additional statistics, inventory,
buttons, or extra UI. It has only one action: **Continue**. Selecting
Continue proceeds to the next level.

## 1.2 Core gameplay loop

1.  Start a level.
2.  Generate the semi-open-world map using PCG and the current DDA
    configuration.
3.  Spawn the player in the designated starting area.
4.  Explore the map and search abandoned structures.
5.  Open supplies crates to obtain weapons, healing items, and
    resources.
6.  Avoid or fight Walker, Spitter, and Charger zombies.
7.  Navigate around or break blocking objects.
8.  Reach the extraction safe zone.
9.  Enter the helicopter.
10. Complete the level and show `Supplies Collection X Cleared`.
11. The player selects **Continue**.
12. Generate the next level using the next level number and updated
    player-performance data.

The safe zone and helicopter are the level objective. Do not add a
separate mission-complete condition based solely on collecting a certain
number of crates unless the existing project explicitly requires it.

## 1.3 Design principles

-   The world is 3D and viewed from an isometric camera.
-   The map is continuous within a level; it is not composed of discrete
    dungeon rooms.
-   Each level is procedurally generated.
-   The safe zone is a fixed objective location within each generated
    level.
-   Abandoned structures are the primary locations for supplies crates
    and can contain a small number of zombies.
-   Supplies crates are the primary source of weapons, healing items,
    and resources.
-   DDA changes only the explicitly permitted world and encounter
    parameters in Section 6.
-   Zombie archetype behavior and timing remain fixed; DDA must not
    modify their mechanics.
-   Keep the HUD minimal. Do not add a conventional inventory panel or
    crate-opening menu.

------------------------------------------------------------------------

# 2. World and Map Design

## 2.1 Map composition

The generated map consists of:

  -----------------------------------------------------------------------
  Element                 Description             Gameplay role
  ----------------------- ----------------------- -----------------------
  Playable terrain        Main ground surface     Supports exploration
                                                  and combat

  Tree boundary           Dense trees around the  Defines the impassable
                          perimeter               world border

  Muddy patches           Mud areas distributed   Slow player and zombies
                          through the map         

  Open clearings          Relatively unobstructed Combat and traversal
                          areas                   space

  Abandoned structures    Buildings, sheds, or    Contain supplies crates
                          ruined facilities       and a few zombies

  Supplies crates         Interactable containers Main source of weapons,
                          placed in structures    healing items, and
                                                  resources

  Blocking objects        Objects that obstruct   Can be breakable or
                          movement or projectiles unbreakable

  Zombie spawn areas      Valid enemy spawn       Populate the world with
                          locations               encounters

  Starting area           Designated player spawn Safe initial spawn
                          location                

  Extraction safe zone    Designated area         Level completion
                          containing a helicopter objective
  -----------------------------------------------------------------------

Do not generate separate loot locations across the map. Loot is obtained
primarily by opening supplies crates, and crates are placed inside
abandoned structures.

Do not add environmental hazard systems such as toxic puddles, fire
zones, or damaging terrain as part of the current design. Muddy patches
are terrain modifiers, not damage hazards.

## 2.2 World boundaries

The outer map boundary is made from trees or other appropriate natural
boundary objects.

Requirements:

-   Boundary objects must prevent the player from leaving the playable
    map.
-   Boundary objects must prevent zombies from leaving the playable map.
-   Boundary placement must not leave gaps that allow actors to escape.
-   Boundary generation is separate from ordinary obstacle placement.
-   DDA must never remove or weaken the world boundary.
-   The player should perceive the boundary as part of the environment,
    not as an invisible wall.

## 2.3 Muddy patches

Muddy patches are terrain areas that reduce movement speed for both the
player and zombies.

**Movement-speed modifier:**

-   Normal terrain: `1.0` movement multiplier.
-   Muddy terrain: `0.7` movement multiplier.
-   This is a 30% reduction from the actor's normal movement speed.

Apply the modifier consistently to: - Player movement. - Walker
movement. - Spitter movement. - Charger normal walking movement.

The mud modifier should not alter the fixed timing of the Spitter's
attack sequence or the Charger's telegraph, wind-up, stun, or other
timed attack phases.

Implementation guidance:

-   Represent mud as a terrain layer, tagged collider, trigger volume,
    or equivalent terrain classification.
-   Avoid applying the 0.7 multiplier repeatedly every frame to an
    already modified speed. Calculate effective speed from the actor's
    base speed and current terrain multiplier.
-   If an actor overlaps multiple terrain types, define a deterministic
    rule. Prefer the terrain under the actor's movement/ground contact
    point.
-   When the actor leaves mud, restore its normal effective speed
    immediately.
-   Ensure the same terrain detection method works for player and AI
    navigation.

## 2.4 Abandoned structures

Abandoned structures are the primary exploration and supply points.

Requirements:

-   Structures are procedurally placed within the playable map.
-   Each structure has valid entrances and accessible interior space.
-   Structures contain one or more supplies crates, subject to
    generation settings.
-   A few zombies may spawn inside or near structures.
-   Structure placement must not overlap the player start or block the
    extraction safe zone.
-   Crates and zombies must not spawn inside walls, props, or
    inaccessible geometry.
-   Interior and exterior navigation must be connected through valid
    entrances.
-   The generator should avoid placing structures so close together that
    their collision geometry overlaps.

The exact structure models may be selected from available project
assets. Do not assume assets exist if they are not present; inspect the
project before choosing prefabs.

## 2.5 Blocking objects and projectile collision

Blocking objects obstruct movement and may also block projectiles. There
are exactly two current categories:

### Breakable blocking objects

-   Can be damaged by eligible attacks/projectiles.
-   Have health or another explicit destruction condition.
-   Are destroyed when their durability reaches zero.
-   Once destroyed, they no longer block movement or projectiles.
-   Their destroyed state should be reflected in collision and
    navigation as appropriate.

### Unbreakable blocking objects

-   Cannot be destroyed by ordinary player attacks.
-   Continue to block movement and projectiles.
-   Must remain intact for the duration of the level.

Implementation requirements:

-   Use explicit object configuration, such as a `BlockingObject`
    component with a breakability setting.
-   Do not infer breakability from object names.
-   Ensure projectile collision checks distinguish breakable from
    unbreakable objects.
-   Ensure enemy charge collision can detect valid blocking surfaces.
-   Avoid allowing a projectile to pass through a blocker merely because
    it is breakable; it should collide first, apply damage if eligible,
    and only pass through after the blocker is destroyed, if the
    projectile's behavior supports continued travel.
-   DDA may adjust the quantity and placement of blocking objects, but
    it must not change an object's breakable/unbreakable classification
    or durability unless explicitly added to the design later.

## 2.6 Isometric obstruction see-through

Because the game uses an isometric camera, foreground objects can
visually obscure the player.

Implement a see-through mechanism whenever an obstacle or environmental
object blocks the camera's view of the player.

Expected behavior:

-   Detect objects between the active camera and the player.
-   Make obstructing objects visually transparent or partially
    transparent while they obstruct the player's view.
-   Restore their normal appearance when they no longer obstruct the
    player.
-   The player must remain visually readable.
-   Avoid making the entire environment transparent.
-   Handle multiple overlapping obstructions.
-   Do not permanently modify an object's original material settings.

Recommended implementation options:

1.  **Preferred if compatible with the project's render pipeline:** Use
    a material property or shader parameter to fade obstructing
    renderers.
2.  **Simpler fallback:** Temporarily change the alpha of a compatible
    material or use a dedicated transparent material variant.
3.  **Alternative:** Use a camera-to-player raycast or sphere cast and
    fade only the renderers hit by the cast.

Technical requirements:

-   First inspect whether the project uses Built-in Render Pipeline,
    URP, or HDRP.
-   Use a method compatible with the actual pipeline and shader.
-   Avoid modifying shared materials in a way that makes every object
    using that material transparent. Use `MaterialPropertyBlock`,
    material instances, or a safe equivalent.
-   Preserve each renderer's original appearance and restore it when no
    longer obstructing the player.
-   Support multiple renderers on a single obstacle.
-   Avoid rapid flickering when the camera or player moves near an
    obstruction. Use a small fade duration or hysteresis.
-   Consider a short fade-in/fade-out transition rather than an
    instantaneous toggle.
-   Do not fade the player itself.
-   Exclude objects that should never fade, such as UI, particles, or
    intentionally transparent effects.

------------------------------------------------------------------------

# 3. Player and Interaction Design

## 3.1 Player

The player uses a 3D character in an isometric view.

Required baseline capabilities:

-   Movement.
-   Health and damage.
-   Weapon use.
-   Ammunition/resource handling.
-   Interaction with supplies crates.
-   Entry into the helicopter to complete the level.

Preserve existing player scripts and project conventions where possible.
Inspect the current implementation before replacing or renaming
anything.

## 3.2 Supplies crates

Supplies crates are the primary source of: - Weapons. - Healing items. -
Resources.

Crates are primarily placed inside abandoned structures.

### Interaction flow

1.  Player enters interaction range of a crate.
2.  Player initiates the crate-opening interaction using the project's
    configured interaction input.
3.  The crate-opening progress indicator appears above the player's
    head.
4.  The white progress bar fills over a transparent rectangular
    background.
5.  The crate opens when progress completes.
6.  The item or items are granted automatically.
7.  Each granted item type produces a small notification above the
    player's head.
8.  The crate becomes opened and cannot grant its contents again.

No separate crate inventory UI, loot-selection window, item list, or
full-screen HUD should appear.

### Crate-opening UI

The only opening UI is a small progress indicator above the player:

-   A small transparent/dark rectangular background.
-   A white progress bar.
-   Positioned above the player's head.
-   Visible only while the crate-opening action is in progress.
-   Hidden when the action completes, is cancelled, or becomes invalid.
-   No percentage text is required.
-   No separate progress panel is required.

The indicator should face the camera or otherwise remain readable in the
isometric view.

### Interaction interruption

Define safe interruption behavior:

-   If the player moves out of interaction range, cancel the opening
    action.
-   If the player takes damage, use a consistent rule. Recommended
    initial behavior: cancel the action.
-   If the player releases the interaction input before completion, use
    the project's chosen hold/toggle interaction convention
    consistently.
-   An opened crate cannot be opened again.
-   Do not grant items until the interaction completes successfully.

Use a single interaction controller or equivalent reusable system rather
than duplicating crate interaction logic across every crate prefab.

## 3.3 Item-gained notifications

When an item is obtained, show a small notification above the player's
head.

Example:

``` text
       +------------------+
       |   Rifle Ammo +20 |
       +------------------+
       |     Medkit +1    |
       +------------------+
       |    Rifle +1      |
       +------------------+
              PLAYER
```

The example is illustrative. The actual UI should be compact and
legible.

Requirements:

-   Display one notification per item type gained.
-   Do not show all acquired items simultaneously in a large inventory
    panel.
-   Notifications appear above the player's head.
-   New notifications are added one at a time.
-   When a new item type is gained, existing notifications shift upward
    to make room.
-   Maximum visible notifications: **3 item types**.
-   If a fourth item type is gained, remove/fade out the oldest
    notification.
-   Notifications should fade out after a configurable display duration.
-   Notifications should not overlap the crate progress bar in a
    visually confusing way.
-   Repeated gains of the same item type should follow one consistent
    policy. Recommended: merge into the existing notification and update
    its quantity rather than creating duplicate rows.
-   Use a queue or notification manager so simultaneous item grants are
    presented sequentially and predictably.

Suggested behavior:

-   New item notification enters at the bottom of the stack.
-   Existing notifications shift upward.
-   The oldest notification fades out when the visible count would
    exceed three.
-   Each notification has a short fade-in and fade-out.
-   The notification stack follows the player while remaining
    screen-readable.

The notification should identify the item and quantity, for example: -
`Rifle Ammo +20` - `Medkit +1` - `Shotgun +1` - `Scrap +5`

Do not invent item categories that are not implemented in the project.
Inspect the existing item definitions and use their actual names.

------------------------------------------------------------------------

# 4. Level Completion and Game Loop

## 4.1 Extraction safe zone

Each generated level contains one designated extraction safe zone with a
helicopter.

Requirements:

-   The safe zone is placed at a valid location within the generated
    map.
-   The safe zone must be reachable from the player starting area.
-   The helicopter is located inside the safe zone.
-   The player must enter the safe-zone area and then enter the
    helicopter interaction/trigger area.
-   Merely entering the safe-zone radius does not complete the level.
-   Merely approaching or seeing the helicopter does not complete the
    level.
-   The level completes only when the player is inside the helicopter's
    designated completion area after entering the safe zone.

Use explicit trigger/collider or interaction logic. Avoid relying on
approximate visual proximity.

## 4.2 Completion screen

When extraction succeeds, stop normal gameplay progression and display:

``` text
Supplies Collection X Cleared

              [ Continue ]
```

Where `X` is the current level number.

UI requirements:

-   Display only the completion text and one Continue button.
-   No score summary.
-   No kill count.
-   No inventory summary.
-   No additional navigation buttons.
-   No extra statistics panel.
-   No unnecessary transition menu.

Gameplay should be paused or otherwise prevented from continuing behind
the completion screen.

## 4.3 Continue behavior

When the player selects Continue:

1.  Close the completion screen.
2.  Advance the level number by one.
3.  Preserve only the player progression/resources that the existing
    game design intends to carry over. Do not invent persistence rules;
    inspect the current project and ask if this is ambiguous.
4.  Generate the next level.
5.  Apply the appropriate DDA configuration for that next level.
6.  Spawn the player at the new starting area.
7.  Resume gameplay.

Prevent multiple Continue clicks from generating multiple levels.

The level number should be managed centrally and used by the completion
text and generation system.

------------------------------------------------------------------------

# 5. Zombie AI

There are three enemy archetypes:

1.  Walker --- standard zombie.
2.  Spitter --- fast-moving ranged toxic attacker.
3.  Charger --- slow-moving heavy enemy with a telegraphed charge.

Keep the shared FSM architecture. Each archetype implements its own
specialized attack behavior while retaining the shared high-level
states.

## 5.1 Shared FSM

The shared high-level states are:

-   Idle/Patrol.
-   Investigate.
-   Chase.
-   Attack.
-   Search.

Conceptual transitions:

-   Idle/Patrol → Investigate when a relevant sound is detected.
-   Idle/Patrol → Chase when the player is detected.
-   Investigate → Chase when the player is detected.
-   Investigate → Idle/Patrol or Search when the sound target expires,
    according to the project's existing state design.
-   Chase → Attack when the archetype's attack activation condition is
    met.
-   Chase → Search when the player is lost.
-   Attack → Chase or Search after the attack sequence finishes,
    depending on target visibility.
-   Search → Chase when the player is rediscovered.
-   Search → Idle/Patrol when the search times out.

Do not replace the FSM with Utility AI or Behavior Trees.

## 5.2 Walker

The Walker is the baseline enemy.

Behavior: - Average movement speed. - Average health. - Approaches the
player. - Performs a melee attack within its attack range. - Uses the
shared FSM for perception and movement decisions.

The Walker should not have the special Spitter or Charger attack
mechanics.

## 5.3 Spitter

The Spitter runs quickly toward the player until the player enters its
attack radius.

Exact attack sequence:

1.  **Run toward player:** Use its fast movement behavior while outside
    the attack radius.
2.  **Preparation:** When inside the attack radius, stop moving for
    exactly 1 second.
3.  **Target capture:** At the end of that 1-second preparation, capture
    the player's current world position.
4.  **Spew:** Launch a toxic projectile toward the captured position.
5.  **Recovery:** Remain stationary for 3 seconds after launching the
    projectile.
6.  **Resume:** Return to running toward the player.

Important requirements:

-   Capture the player position at the end of the 1-second preparation,
    not at the beginning.
-   The projectile travels toward the captured position.
-   The projectile must not continuously home toward the player unless a
    future design revision explicitly requests it.
-   The Spitter remains stationary during the preparation and recovery
    phases.
-   The Spitter does not resume running until the 3-second recovery has
    completed.
-   Use a dedicated attack-phase state or sub-state within the shared
    `Attack` state.
-   Timers must be deterministic and must not be affected by DDA.
-   Use a projectile prefab and explicit collision/damage handling.
-   The projectile should interact correctly with breakable and
    unbreakable blockers.

Suggested attack phases:

``` text
CHASE/RUN
    |
    | Player enters attack radius
    v
PREPARATION (1.0 s)
    |
    v
CAPTURE PLAYER POSITION
    |
    v
SPEW PROJECTILE
    |
    v
RECOVERY (3.0 s)
    |
    v
CHASE/RUN
```

Clarification: the 3-second recovery starts after the projectile is
launched. If the projectile has a travel time, the Spitter's recovery
timer should not wait for the projectile to hit.

## 5.4 Charger

The Charger walks slowly toward the player until the player enters its
charge activation range.

Exact attack sequence:

1.  **Walk toward player:** Use slow normal movement.
2.  **Telegraph:** When within the charge activation range, display a
    directional indicator toward the player's current position.
3.  **Tracking telegraph:** For 3 seconds, the indicator continuously
    follows the player's current position.
4.  **Target lock:** At the end of the 3 seconds, lock the last captured
    player position and calculate the charge direction.
5.  **Step back:** Move backward for 500 milliseconds.
6.  **Charge:** Charge rapidly along the locked direction.
7.  **Collision:** Continue charging until hitting a designated blocking
    wall/object.
8.  **Stun:** After a valid collision, remain stunned for 5 seconds.
9.  **Resume:** Return to normal behavior after stun ends.

Important requirements:

-   The directional indicator follows the player throughout the full
    3-second telegraph.
-   The indicator stops following after target lock.
-   The locked direction must not change during the step-back or charge
    phases.
-   The Charger must not steer toward the player's new position during
    the charge.
-   The charge continues until a valid blocking collision occurs.
-   The Charger must not stop simply because the player moves away.
-   A valid wall collision starts the 5-second stun.
-   Do not allow the Charger to become permanently stuck if it reaches
    an unexpected collision or navigation edge. Define and test a safe
    fallback for invalid collision conditions.
-   The charge should use a dedicated movement mode that temporarily
    overrides normal NavMesh/pathfinding movement.
-   Use physics collision detection or a sweep to prevent tunneling
    through walls at high charge speed.

Suggested phases:

``` text
CHASE/WALK
    |
    | Enters charge range
    v
TELEGRAPH (3.0 s, tracks player)
    |
    v
LOCK TARGET DIRECTION
    |
    v
STEP BACK (0.5 s)
    |
    v
CHARGE (fixed direction)
    |
    | Valid wall collision
    v
STUN (5.0 s)
    |
    v
CHASE/WALK
```

The visual indicator should be readable from the isometric camera. It
can be a line, arrow, ground decal, or other clear directional
telegraph. It must be positioned/rotated to communicate the locked
charge direction.

## 5.5 Mud interaction with AI

Muddy patches reduce movement speed by 30% for all three zombie
archetypes during their normal movement.

-   Walker normal movement: apply the mud multiplier.
-   Spitter running/chasing: apply the mud multiplier.
-   Charger normal walking: apply the mud multiplier.
-   Charger charge: define separately whether the charge is affected.
    Recommended initial behavior: the charge uses its dedicated fixed
    charge speed and is not slowed by mud, so its telegraphed attack
    remains consistent.
-   Spitter preparation and recovery timers are unaffected.
-   Charger telegraph, step-back duration, and stun duration are
    unaffected.

Do not let DDA modify these movement or timing rules.

------------------------------------------------------------------------

# 6. Dynamic Difficulty Adjustment (DDA)

## 6.1 DDA responsibilities

DDA modifies only the following:

1.  Total enemy quantity.
2.  Enemy archetype distribution.
3.  Loot quality.
4.  Healing supply quantity.
5.  Environmental hazard quantity.
6.  Blocking-object quantity and placement.

**Current revision:** Environmental hazards have been removed from the
map design. Do not implement damaging environmental hazards as part of
the current game. Keep the DDA architecture extensible, but do not
generate or adjust environmental hazards unless they are explicitly
reintroduced later.

Therefore, the active DDA controls are:

-   Enemy quantity.
-   Enemy archetype distribution.
-   Loot quality.
-   Healing supply quantity.
-   Blocking-object quantity and placement.

DDA must NOT modify:

-   Zombie movement speed.
-   Zombie health.
-   Zombie damage.
-   Zombie detection range.
-   Zombie attack range.
-   FSM states or transitions.
-   Spitter preparation/recovery durations.
-   Charger telegraph duration.
-   Charger step-back duration.
-   Charger charge behavior or speed.
-   Charger stun duration.
-   Mud movement modifier.
-   World boundary configuration.

## 6.2 Performance indicators

Track player performance using:

  ------------------------------------------------------------------------
  Indicator             Description                                 Weight
  --------------------- --------------------- ----------------------------
  Health remaining      Current health                                 30%
                        divided by maximum    
                        health                

  Kill efficiency       Enemies killed                                 20%
                        divided by enemies    
                        encountered           

  Time efficiency       Comparison between                             15%
                        target and actual     
                        completion time       

  Damage avoidance      Damage received                                20%
                        relative to a defined 
                        threshold             

  Resource efficiency   Resources consumed                             15%
                        relative to a defined 
                        budget                
  ------------------------------------------------------------------------

All indicators must be normalized to a score from 0 to 1.

Example formulas:

``` text
HealthScore = CurrentHealth / MaximumHealth

KillScore = ZombiesKilled / ZombiesEncountered

TimeScore = min(1, TargetTime / ActualTime)

DamageScore = max(0, 1 - DamageReceived / DamageThreshold)

PerformanceScore =
    0.30 * HealthScore +
    0.20 * KillScore +
    0.15 * TimeScore +
    0.20 * DamageScore +
    0.15 * ResourceScore
```

Protect against division by zero and missing data. For example, if no
zombies were encountered, do not divide by zero to calculate kill
efficiency.

The exact resource-efficiency formula should be based on the actual
resource system found in the project.

## 6.3 Difficulty classification

Initial suggested thresholds:

  Performance score   Classification
  ------------------- -----------------
  0.00--0.39          Struggling
  0.40--0.69          Balanced
  0.70--1.00          Performing well

Use smoothing or a moving average to avoid extreme changes based on a
single unusually good or bad level.

## 6.4 Active DDA parameters

  Parameter            Struggling   Balanced   Performing well
  -------------------- ------------ ---------- --------------------
  Total zombies        Lower        Normal     Higher
  Walker proportion    Higher       Normal     Lower
  Spitter proportion   Lower        Normal     Higher
  Charger proportion   Lower        Normal     Higher
  Loot quality         Better       Normal     Rarer distribution
  Healing supplies     More         Normal     Fewer
  Blocking objects     Fewer        Normal     More

These are relative tendencies, not final numerical balance values.

The generator must convert enemy proportions into integer counts while
ensuring that: - The total enemy count is respected. - No negative enemy
counts are produced. - Rounding does not accidentally exceed the enemy
budget. - A minimum/maximum count can be enforced for each archetype. -
The starting area remains safe. - The extraction route remains
reachable.

## 6.5 Loot quality versus healing quantity

Treat loot quality and healing quantity as separate parameters.

Loot quality controls the probability distribution of item rarity or
quality inside supplies crates.

Healing quantity controls how many healing items are available across
generated crates.

Do not automatically interpret better loot quality as more healing
items.

## 6.6 DDA timing

Recommended timing:

-   Evaluate player performance after a level is completed.
-   Store the resulting difficulty configuration.
-   Apply it when generating the next level.
-   Do not change active zombie behavior or spawn/despawn enemies around
    the player during an encounter.
-   Do not regenerate the current level because the DDA score changes.

The current level number and DDA state should be managed centrally.

------------------------------------------------------------------------

# 7. Procedural Content Generation (PCG)

## 7.1 Generation approach

Use a multi-stage procedural world generator.

The map is a continuous outdoor environment, not a collection of
separate room prefabs.

Recommended pipeline:

1.  Read map configuration and random seed.
2.  Generate the playable terrain.
3.  Generate the outer tree boundary.
4.  Generate muddy patches.
5.  Generate open clearings and environmental variation.
6.  Place abandoned structures.
7.  Place blocking objects.
8.  Place supplies crates inside structures.
9.  Place zombie spawn zones and enemy groups.
10. Place the player start location.
11. Place the extraction safe zone and helicopter.
12. Validate connectivity, collision, and spawn positions.
13. Initialize navigation and gameplay.

## 7.2 Grid representation

Use an internal grid or spatial data representation for generation and
validation, even if the visible world uses continuous 3D terrain.

Possible cell classifications:

-   Walkable ground.
-   Mud.
-   Structure footprint.
-   Blocking object.
-   Boundary.
-   Reserved player start.
-   Reserved extraction zone.
-   Zombie spawn candidate.
-   Crate placement candidate.

The grid is a generation and validation tool; it does not require the
game to look tile-based.

## 7.3 Randomness and reproducibility

Use a configurable random seed.

Requirements:

-   The same seed and generation settings should reproduce the same
    generated layout, to the extent supported by the implementation.
-   Different seeds should produce different layouts.
-   Store or log the seed for debugging.
-   Avoid using unrelated global random calls that make generation order
    unpredictable.
-   Prefer a centralized seeded random generator or separate
    deterministic random streams for terrain, structures, obstacles,
    loot, and enemies.

## 7.4 Structure placement

Abandoned structures should be placed with sufficient spacing and
accessible entrances.

Each structure can define: - Footprint. - Entrance locations. - Valid
crate spawn points. - Valid zombie spawn points. - Interior navigation
areas.

Use available prefabs and authored placement points if the project
already contains them. Procedural generation may select and position
structure prefabs; the structures themselves do not need to be generated
vertex-by-vertex.

## 7.5 Safe zone placement

The extraction safe zone must be generated as a reserved region.

Requirements:

-   It must be inside the playable boundary.
-   It must be reachable from the player start.
-   It must have enough clear space for the helicopter and player entry.
-   It must not be blocked by generated structures or blocking objects.
-   It must not be placed inside a muddy patch if that would compromise
    the intended extraction interaction.
-   DDA must not remove or relocate the safe zone after generation.
-   The helicopter must be visible and accessible.

## 7.6 World validation

At minimum, validate:

-   Player start is reachable.
-   Safe zone is reachable from player start.
-   Structures have accessible entrances.
-   Crates are accessible.
-   Zombies spawn on valid ground.
-   Zombies do not spawn directly on the player.
-   Blocking objects do not completely isolate essential areas.
-   Boundary trees prevent escape.
-   No generated object overlaps reserved critical regions.
-   Navigation data is usable after generation.

If generation fails validation, retry with a controlled retry limit or
repair the invalid placement. Avoid infinite generation loops.

------------------------------------------------------------------------

# 8. System Integration Architecture

Use clear separation of responsibilities.

Suggested high-level systems:

  -----------------------------------------------------------------------
  System                              Responsibility
  ----------------------------------- -----------------------------------
  GameManager                         Owns level progression and game
                                      state

  WorldGenerator                      Coordinates world generation

  TerrainGenerator                    Creates terrain and muddy patches

  EnvironmentGenerator                Places trees, structures, and
                                      environmental objects

  BoundaryGenerator                   Creates the outer tree boundary

  SpawnGenerator                      Places zombies and spawn zones

  LootGenerator                       Configures crates and their
                                      contents

  WorldValidator                      Validates accessibility and
                                      generation

  PlayerPerformanceTracker            Records player statistics

  PerformanceEvaluator                Calculates normalized performance

  DifficultyController                Produces the next level's DDA
                                      configuration

  ZombieController                    Coordinates shared zombie behavior

  ZombieStateMachine                  Manages shared FSM states

  WalkerAI                            Walker-specific attack behavior

  SpitterAI                           Spitter attack sequence

  ChargerAI                           Charger attack sequence

  CrateInteraction                    Handles opening supplies crates

  ItemNotificationManager             Queues and displays item-gained
                                      notifications

  ExtractionController                Handles safe-zone and helicopter
                                      completion

  ObstructionFadeController           Handles isometric see-through
                                      behavior
  -----------------------------------------------------------------------

## 8.1 Integration sequence

``` text
LEVEL START
    |
    v
GameManager reads level number and DDA profile
    |
    v
WorldGenerator creates the map
    |
    +--> Terrain and muddy patches
    |
    +--> Tree boundaries and environment
    |
    +--> Abandoned structures
    |
    +--> Blocking objects
    |
    +--> Supplies crates
    |
    +--> Zombie spawn groups
    |
    +--> Safe zone and helicopter
    |
    v
WorldValidator checks the generated world
    |
    v
Player explores and collects supplies
    |
    v
Player fights zombies and reaches extraction
    |
    v
Player enters helicopter
    |
    v
Level completion screen
    |
    v
Player selects Continue
    |
    v
PerformanceEvaluator calculates level performance
    |
    v
DifficultyController prepares next-level settings
    |
    v
Next level is generated
```

Performance may be tracked continuously during gameplay, but the
next-level configuration should be finalized at the level transition.

------------------------------------------------------------------------

# 9. Unity Implementation Guidance

## 9.1 Inspect before editing

Before making changes, inspect the existing Unity project.

Determine:

-   Unity version.
-   Render pipeline (Built-in, URP, or HDRP).
-   Existing player controller and movement implementation.
-   Existing zombie scripts and state machine, if any.
-   Existing human/zombie prefabs and animations.
-   Existing environment assets.
-   Existing input system (old Input Manager or Input System package).
-   Existing navigation implementation.
-   Existing UI conventions.
-   Existing folder and naming conventions.

Do not assume that a class, prefab, package, or asset exists simply
because it is mentioned in this document.

## 9.2 Preserve existing project work

-   Reuse existing working scripts where practical.
-   Do not rewrite entire systems when a small extension is sufficient.
-   Do not rename existing classes, public fields, serialized fields, or
    assets without a compelling reason.
-   Preserve Inspector references and prefab assignments.
-   Avoid introducing unnecessary dependencies or packages.
-   Do not delete assets or scripts unless they are demonstrably unused
    and removal is necessary.
-   If a serialized field or component must change, account for existing
    scene and prefab references.

## 9.3 Configuration

Prefer ScriptableObjects or serializable configuration classes for
tunable values.

Examples: - Map dimensions. - Mud movement multiplier. - Structure spawn
density. - Enemy archetype weights. - Loot rarity weights. - Healing
quantity. - Blocking-object quantity. - Crate interaction duration. -
Item notification duration. - Spitter timing values. - Charger timing
values. - DDA thresholds and weights.

Do not hardcode values across multiple scripts when they should be
configured centrally.

## 9.4 Timing

Use consistent time handling.

-   Use scaled time for gameplay timers if gameplay pause should stop
    them.
-   Ensure UI fade behavior is consistent with the pause state.
-   Prevent multiple coroutines or timers from starting duplicate attack
    sequences.
-   Cancel or clean up active timers when an enemy dies, is disabled, or
    is destroyed.
-   DDA must not alter fixed enemy attack timings.

## 9.5 Performance

The world may contain many environmental objects.

Consider: - Object pooling for frequently spawned projectiles and
effects. - Efficient placement checks. - Avoiding expensive scene-wide
searches in Update. - Avoiding unnecessary NavMesh rebuilds. - Batching
or instancing vegetation where suitable. - Limiting the number of active
AI perception checks. - Avoiding repeated allocations in per-frame
loops.

Do not prematurely optimize at the expense of correctness. Profile
before making complex optimization changes.

------------------------------------------------------------------------

# 10. UI Specification

The game intentionally uses minimal UI.

## 10.1 During normal gameplay

Only implement the HUD elements already required by the existing game or
explicitly approved by the project owner.

The following new UI elements are required:

1.  Crate-opening progress bar above the player.
2.  Item-gained notifications above the player.
3.  Completion screen after helicopter extraction.

Do not create: - A crate loot-selection screen. - A crate inventory
panel. - A full-screen item-reward display. - A multi-page completion
summary. - Unrequested objective/quest panels.

## 10.2 Crate progress indicator

Visual requirements:

-   Small transparent rectangular background.
-   White progress fill.
-   Positioned above the player.
-   Billboarded or otherwise readable from the isometric camera.
-   Visible only during crate interaction.

## 10.3 Item notifications

Visual requirements:

-   Positioned above the player.
-   One notification per item type.
-   Maximum three visible types.
-   New notifications shift older notifications upward.
-   Oldest notification fades out when a fourth type arrives.
-   Same item type gains merge and update quantity.
-   Automatic item acquisition; no selection required.

## 10.4 Completion screen

Exact content:

``` text
Supplies Collection X Cleared

              Continue
```

Only one button: Continue.

------------------------------------------------------------------------

# 11. Testing and Acceptance Criteria

Do not consider a system complete merely because the code compiles. Test
its behavior in Play Mode.

## 11.1 World generation

-   [ ] A new level generates a 3D playable environment.
-   [ ] Different seeds produce visibly different layouts.
-   [ ] The same seed reproduces the layout under the same settings.
-   [ ] The map has a continuous playable area.
-   [ ] Tree boundaries prevent escape.
-   [ ] Muddy patches are generated.
-   [ ] Mud slows the player by 30%.
-   [ ] Mud slows all zombie archetypes during normal movement by 30%.
-   [ ] Abandoned structures are generated.
-   [ ] Structures have accessible entrances.
-   [ ] Supplies crates appear inside abandoned structures.
-   [ ] A few zombies can spawn inside or near structures.
-   [ ] No separate random loot locations are generated.
-   [ ] No environmental damage hazards are generated.
-   [ ] The extraction safe zone is reachable.
-   [ ] The helicopter is accessible.
-   [ ] Blocking objects have explicit breakable/unbreakable types.
-   [ ] World validation catches blocked or inaccessible critical areas.

## 11.2 Supplies crates and notifications

-   [ ] The player can interact with a crate.
-   [ ] A small transparent progress rectangle appears above the player.
-   [ ] The white progress bar fills correctly.
-   [ ] The progress indicator disappears when interaction ends.
-   [ ] Moving out of range cancels the interaction.
-   [ ] Items are granted automatically when opening completes.
-   [ ] No loot-selection UI appears.
-   [ ] An item-gained notification appears above the player.
-   [ ] Notifications appear one item type at a time.
-   [ ] New notifications shift existing notifications upward.
-   [ ] At most three item types remain visible.
-   [ ] The oldest notification fades when a fourth type is gained.
-   [ ] Repeated gains of the same item type merge into one
    notification.
-   [ ] An opened crate cannot grant its contents twice.

## 11.3 Walker AI

-   [ ] Walker uses the shared FSM.
-   [ ] Walker can detect and chase the player.
-   [ ] Walker attacks within melee range.
-   [ ] Walker can lose the player and search.
-   [ ] Walker moves 30% slower on mud during normal movement.

## 11.4 Spitter AI

-   [ ] Spitter runs toward the player.
-   [ ] Spitter stops upon entering attack radius.
-   [ ] Spitter remains stationary for 1 second.
-   [ ] Spitter captures the player's position at the end of that
    second.
-   [ ] Spitter launches a projectile toward the captured position.
-   [ ] Projectile does not continuously track the player.
-   [ ] Spitter remains stationary for 3 seconds after launching.
-   [ ] Spitter resumes running after recovery.
-   [ ] Mud slows its normal running movement by 30%.
-   [ ] DDA does not modify its timing.

## 11.5 Charger AI

-   [ ] Charger walks slowly toward the player.
-   [ ] Charger starts its telegraph within its activation range.
-   [ ] Direction indicator follows the player for 3 seconds.
-   [ ] Direction locks after the 3-second telegraph.
-   [ ] Charger steps backward for 500 milliseconds.
-   [ ] Charger charges in the locked direction.
-   [ ] Charger does not track the player during the charge.
-   [ ] Charger continues until valid blocking collision.
-   [ ] Charger becomes stunned for 5 seconds after collision.
-   [ ] Charger resumes normal behavior after stun.
-   [ ] High-speed charge does not tunnel through blocking geometry.
-   [ ] DDA does not modify its attack timings or behavior.

## 11.6 Blocking objects and see-through

-   [ ] Breakable blockers can be destroyed by eligible attacks.
-   [ ] Unbreakable blockers cannot be destroyed by ordinary attacks.
-   [ ] Blockers interact correctly with projectiles.
-   [ ] Blockers interact correctly with Charger collision.
-   [ ] Camera obstruction detection identifies objects between camera
    and player.
-   [ ] Obstructing objects fade or become transparent.
-   [ ] Objects return to their original appearance when no longer
    obstructing.
-   [ ] Multiple obstructions are handled correctly.
-   [ ] Shared materials are not unintentionally modified globally.
-   [ ] No rapid transparency flickering occurs during normal movement.

## 11.7 DDA

-   [ ] Player performance indicators are recorded.
-   [ ] Performance score is normalized.
-   [ ] Division-by-zero cases are handled.
-   [ ] Difficulty classification is correct.
-   [ ] Enemy quantity changes according to difficulty.
-   [ ] Enemy archetype distribution changes according to difficulty.
-   [ ] Loot quality changes according to difficulty.
-   [ ] Healing quantity changes according to difficulty.
-   [ ] Blocking-object quantity/placement changes according to
    difficulty.
-   [ ] DDA does not change zombie stats or attack timings.
-   [ ] DDA does not change muddy terrain's movement modifier.
-   [ ] DDA does not alter world boundaries.
-   [ ] DDA is applied to the next level rather than abruptly changing
    the active encounter.

## 11.8 Level completion

-   [ ] Safe-zone entry alone does not complete the level.
-   [ ] Entering the helicopter after entering the safe zone completes
    the level.
-   [ ] Completion text displays the correct level number.
-   [ ] Completion screen contains only the required text and Continue
    button.
-   [ ] Gameplay is paused or disabled behind the completion screen.
-   [ ] Continue advances the level exactly once.
-   [ ] Next level is generated successfully.
-   [ ] DDA configuration is applied to the next level.

------------------------------------------------------------------------

# 12. Implementation Priorities

Implement in this order. Do not attempt all features in one large
code-generation operation.

### Priority 1 --- Core playable environment

-   Inspect and understand the existing project.
-   Confirm the isometric camera.
-   Confirm player movement and combat.
-   Create a manually authored test map.
-   Implement tree boundaries and blocking-object collision.
-   Implement muddy terrain movement modifiers.

### Priority 2 --- World PCG

-   Create map data representation.
-   Generate terrain.
-   Generate tree boundaries.
-   Generate muddy patches.
-   Generate abandoned structures.
-   Generate blocking objects.
-   Place player start, extraction zone, and helicopter.
-   Validate map accessibility.

### Priority 3 --- Zombie AI

-   Implement or preserve the shared FSM.
-   Complete Walker behavior.
-   Implement Spitter attack phases.
-   Implement Charger attack phases.
-   Verify all timing and movement requirements.

### Priority 4 --- Supplies and interactions

-   Implement supplies crate interaction.
-   Implement progress indicator.
-   Implement automatic item granting.
-   Implement stacked item notifications.
-   Implement breakable and unbreakable blockers.

### Priority 5 --- DDA

-   Implement performance tracking.
-   Implement performance evaluation.
-   Implement difficulty profiles.
-   Connect DDA to enemy composition.
-   Connect DDA to loot quality and healing quantity.
-   Connect DDA to blocking-object generation.

### Priority 6 --- Extraction and progression

-   Implement safe-zone entry.
-   Implement helicopter entry/completion.
-   Implement exact completion screen.
-   Implement Continue.
-   Implement next-level generation.

### Priority 7 --- Isometric visual polish and testing

-   Implement obstruction see-through.
-   Test multiple overlapping obstructions.
-   Test multiple generated seeds.
-   Test DDA scenarios.
-   Test all zombie archetypes.
-   Test the full level-to-level loop.

------------------------------------------------------------------------

# 13. Instructions for Antigravity

You are implementing DEAD DEPTHS in the existing Unity project.

Follow these rules:

1.  **Inspect first.** Explore the existing project, scripts, scenes,
    prefabs, packages, and assets before proposing modifications.
2.  **Do not assume.** Do not assume a specific render pipeline, input
    system, navigation package, prefab, or script exists until verified.
3.  **Preserve working code.** Reuse existing implementations where
    possible. Prefer targeted modifications over unnecessary rewrites.
4.  **Respect this specification.** Treat this document as the current
    design baseline. Do not reintroduce removed features such as
    separate world loot locations or environmental damage hazards.
5.  **Do not change enemy mechanics.** DDA must not alter zombie speed,
    health, damage, detection, attack timings, FSM logic, or the muddy
    terrain modifier.
6.  **Keep the FSM.** Walker, Spitter, and Charger share the high-level
    FSM. Their specialized attack behavior is implemented inside their
    respective attack logic.
7.  **Keep the UI minimal.** Implement only the crate progress
    indicator, item-gained notifications, and the specified completion
    screen as new UI.
8.  **Validate generated worlds.** The player start, abandoned
    structures, supplies crates, and extraction helicopter must be
    accessible.
9.  **Test in Unity.** Do not report a feature as complete just because
    code was written. Verify it in Play Mode where possible.
10. **Report exact changes.** For each implementation phase, report:
    -   Files created.
    -   Files modified.
    -   Components or prefabs requiring Inspector setup.
    -   Scene changes.
    -   Tests performed.
    -   Known limitations or unresolved issues.

## Working method

Work incrementally, one priority at a time.

Before modifying files: - Summarize the relevant existing
architecture. - Identify the exact scripts and assets that will be
reused. - Propose the smallest practical implementation plan.

Then implement the current priority and validate it before proceeding.

Do not generate the entire game in one pass. Do not silently skip
requirements. If a requirement conflicts with the current project
architecture or cannot be implemented safely, explain the conflict and
propose a concrete resolution before making destructive changes.

**First task:** Inspect the existing Unity project and produce an
implementation audit mapping the current scripts, prefabs, scenes, and
assets to this specification. Do not begin a broad rewrite before that
audit is complete.
