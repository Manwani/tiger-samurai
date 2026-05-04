# Project Plan

**Project Name:** TigerSamurai  
**Active Branch:** `main`  
**Last Updated:** `2026-05-04`
**Current Focus:** Testing whether movement-triggered parry circle spawns, first-pass dodge hazards, and movement-triggered safe-tile health drops make the 3x3 board feel faster and more reactive. Circles now have a 60% chance to spawn on the tile the player lands on, health pickups roll on player landings while the player has fewer than 3 lives, and the broader white/red/blue/black difficulty ladder, 3-life fail state, parry feedback, EXP bar, enemy phase, and randomized six-input enemy prompts remain in place.
**Status:** Active prototype development

## Ongoing Instructions For Assistant
- Inspect current repo state before making changes.
- Keep this project plan up to date.
- Maintain the session log.
- Prefer step-by-step help and readable code.
- Explain changes in plain English.
- Call out readability versus maintenance tradeoffs when there are multiple valid approaches.
- Do not revert unrelated existing changes unless explicitly asked.

## Vision
TigerSamurai is a learn-as-we-build Unity prototype focused on clear, readable systems and fast iteration. The current core loop is tile-based movement on a 3x3 board, timing-based parries, and simple enemy encounters, with future work aimed at turning those pieces into a cohesive combat experience.

## High-Level Requirements
- Keep movement tile-based on the 3x3 gameplay grid with responsive jump movement and simple controls.
- Keep the code beginner-friendly, readable, and easy to explain in plain English.
- Preserve existing scene wiring and working systems instead of rewriting stable parts from scratch.
- Build combat feedback around parry timing, enemy prompts, audio, particles, and readable screen feedback.
- Maintain a durable in-repo source of truth so future sessions can recover context quickly if chat history fails.

## Project Plan
- Stabilize the current vertical slice: movement, shrinking circles, parry scoring, enemy prompts, and feedback.
- Remove temporary or debug-only behavior once the real gameplay replacement is ready, especially the current circle-spawner debug targeting.
- Continue small polish passes that improve feel and clarity without adding heavy architecture too early.
- Keep this document synchronized with actual repo state whenever priorities, instructions, or scope change.

## Outstanding Tasks
### Next Session Notes
- [ ] Prioritize damage feedback so players clearly understand when they lose health from missed parries or dodge hazard wall hits.
- [ ] Add a dodge hazard close sound and smoke particle effect when the lines collapse.
- [ ] Make losing health more visible with stronger feedback, such as flash, shake, hit pause, or life counter punch.
- [ ] Polish health pickup expiration feedback so the turkey leg visibly fades, blinks, or pulses as time runs out.
- [ ] Add smoke particle effects when the character dashes.
- [ ] Add directional dash animations for forward and back movement.
- [ ] Figure out the proper frames for the parry animation.
- [ ] Add a triangle enemy variant that requires holding the parry button.
- [ ] Make the parry button cancel and immediately restart the parry animation when pressed, so parry input feels more responsive.
- [ ] When a circle parry enemy is fully defeated, spawn particles that travel to the EXP bar and increase it for a stronger reward effect.

### Current Priority
- [ ] Play-test the new movement-triggered circle rhythm: every landing rolls a 60% chance to spawn one circle on the landed tile, using the existing white/red/blue/black rules.
- [ ] Play-test the first-pass dodge hazard mechanic: hazards should spawn after landing on non-circle tiles, randomly choose horizontal or vertical lines, damage the player for standing still until collapse, and damage movement through the unsafe line direction.
- [ ] Play-test movement-triggered turkey-leg health drops: while below 3 lives, each landing should roll a 10% chance, place at most one pickup on another random safe empty tile, restore 1 life on collection, and disappear after a short lifetime.
- [ ] Play-test the new lives counter and confirm whether game over at 0 lives feels fair.
- [ ] Play-test the first-pass white/red parry encounter flow and tune whether red circles feel fair.
- [x] Bring `TileCircleSpawner` out of debug mode so circles spawn in the intended gameplay flow instead of always using the starting player tile.
- [ ] Play-test the combined parry sound, particle, shake, flash, and movement lock feedback together and tune anything that feels too strong or distracting.
- [ ] Play-test the new streak-based parry zoom toward the player and tune `zoomAmount`, `zoomInDuration`, `zoomOutDuration`, `maxZoomStreak`, and `zoomFocusStrength`.
- [ ] Play-test the basic EXP bar fill amount and enemy-spawn transition, then decide whether the bar should reset, trigger a reward, or gain animation polish when full.
- [ ] Tune the basic EXP bar fill animation and sparkle values so the effect reads clearly without distracting from parry timing.
- [ ] Play-test whether six randomized enemy inputs feels readable and fair after the EXP bar fills.

### Near-Term Cleanup
- [ ] If the movement-triggered circle spawn rhythm stays, simplify `TileCircleSpawner` around that loop by removing or archiving the old timed two-circle wave mode, replacing the active encounter list with one active circle, and keeping health pickups disconnected until they are redesigned.
- [ ] Clean up Inspector ergonomics for `TileCircleSpawner`, `HealthPickupSpawner`, and `DodgeMechanic` so dependency reference fields do not feel redundant when all components live on `GameplayTiles`.
- [ ] Confirm whether the test `ParryBurst` scene object should stay in `Main.unity` or be removed now that the prefab is spawned by code.
- [ ] Decide how to show remaining parries on multi-hit circles so red encounters read clearly at a glance.
- [ ] Replace the temporary `P` prompt above the player with final parry-ready art or UI.
- [ ] Decide when circle spawning should resume after the EXP-spawned enemy encounter ends.
- [ ] Keep this file updated at the end of meaningful work sessions.
- [ ] Re-check the enemy encounter flow after current combat polish and decide the next improvement.

### Future Architecture Ideas
- [ ] Consider moving future enemy/encounter setup toward a simple recipe-and-spawner model:
  - **Encounter definitions are recipes:** what type it is, what it looks like, how hard it is, and what reward it gives.
  - **Spawners place recipes onto the board:** pick a tile, create the object, and hand it the needed references.
  - **Behavior scripts play the actual rules:** circle parry, triangle hold-parry, enemy input sequence, boss pattern, etc.
- [ ] Avoid making one giant `EnemyConstructor` that knows every enemy rule. If a new enemy type plays differently, give it its own focused behavior script.
- [ ] When circle, triangle, enemy, and boss patterns start sharing spawn needs, consider a more general `EncounterSpawner` or `BoardEncounterSpawner`.

## Session Log
### 2026-04-17 - Repo State Recovery And Tracker Setup
- **Goal:** Re-establish shared project memory after recovered chat loss and create a durable source of truth inside the repo.
- **Key Decisions:** Use one repo-tracked root file named `PROJECT_PLAN.md`; keep the log in the same file; use concise session summaries instead of transcript copies.
- **What Changed:** Audited the current movement, parry, and enemy systems; confirmed the active branch is `main`; created this tracker with standing assistant instructions and seeded project context.
- **Next Likely Step:** Use this file as the first reference point in future sessions and keep it updated whenever work meaningfully changes.

### 2026-04-17 - Parry Feedback Polish
- **Goal:** Make successful parries feel more satisfying and readable.
- **Key Decisions:** Keep parry pitch variation subtle (`0.9` to `1.1`); use the user-authored `ParryBurst` prefab for the parry VFX; use a simple camera-based shake plus white flash for screen feedback.
- **What Changed:** Randomized the parry success sound pitch, hooked the `ParryBurst` prefab to successful parries, and added `ParryScreenFeedback` on the main camera to drive shake and flash.
- **Next Likely Step:** Play-test the full parry feedback package, then decide whether to continue combat polish or return the circle spawner from debug mode.

### 2026-04-18 - First Pass Multi-Stage Parry Encounters
- **Goal:** Turn circle parries into clearer challenge types with room for difficulty scaling.
- **Key Decisions:** Add a dedicated `ParryCircleEncounter` class instead of overloading `ShrinkingCircle`; make white circles the simple one-parry baseline; make red circles require three parries, tighten their timing each stage, and lock the player in place once engaged; keep only one active circle on the board at a time; make circles wait at full size until the player actually lands on their tile.
- **What Changed:** Reworked `TileCircleSpawner` to pick white, red, blue, or black encounter settings, avoid spawning on the player tile, enemy tiles, or an already-occupied tile, default back to random gameplay spawning, keep red as the most common special case for testing, and wait for the current encounter to finish before spawning another; updated `ParryPointTracker` to track encounter objects, create a temporary `P` prompt above the player during the valid parry window, and treat missed parry presses as a fast-fail instead of allowing spam retries; changed player movement locking to use a small lock count so enemy and parry locks can safely overlap; temporarily disabled enemy auto-spawning in `EnemySpawner` to keep the current test loop focused on circle feel.
- **Next Likely Step:** Test whether the one-at-a-time idle spawn pacing feels readable, then decide whether the next polish pass should be a remaining-parries visual, a fail-state effect, or further tuning on spawn pacing.

### 2026-04-24 - Basic EXP Bar Fill
- **Goal:** Connect the scene `ExpBar` slider to successful parries.
- **Key Decisions:** Keep the first version simple and code-driven; let `ParryPointTracker` auto-find a slider named `ExpBar`; fill the bar over ten successful parries; use a light yellow fill color for now.
- **What Changed:** Added basic EXP bar setup and updating to `ParryPointTracker`, including slider normalization, non-interactive display behavior, and fill-color assignment.
- **Next Likely Step:** Play-test whether ten parries feels right for a full bar, then decide what should happen when the bar fills.

### 2026-04-24 - EXP Bar Enemy Spawn Gate
- **Goal:** Spawn an enemy once the EXP bar fills and stop circle spawning for the enemy phase.
- **Key Decisions:** Keep the full-bar behavior one-shot for now; let `ParryPointTracker` auto-find `EnemySpawner` and `TileCircleSpawner`; clear any active circle when the enemy phase starts so movement locks do not linger.
- **What Changed:** Added a stop-and-clear method to `TileCircleSpawner`; updated `ParryPointTracker` to spawn an enemy after ten successful parries and shut down circle spawning at that moment.
- **Next Likely Step:** Play-test the transition from circle parries into the enemy encounter, then decide whether circles should resume after the enemy is defeated.

### 2026-04-24 - EXP Bar Controller Split
- **Goal:** Simplify `ParryPointTracker` before adding EXP bar animation and particle polish.
- **Key Decisions:** Move EXP bar state, slider setup, full-bar enemy spawning, and circle-spawn shutdown into a dedicated `ExpBarController`; keep `ParryPointTracker` focused on parry detection, scoring, and parry feedback.
- **What Changed:** Added `ExpBarController.cs`; trimmed EXP slider and reward logic out of `ParryPointTracker`; kept runtime auto-wiring so the scene does not need manual Inspector setup yet.
- **Next Likely Step:** Add a small fill animation and UI sparkle burst inside `ExpBarController` without expanding the parry tracker again.

### 2026-04-24 - Randomized Enemy Defeat Inputs
- **Goal:** Make each enemy encounter use a fresh randomized input sequence.
- **Key Decisions:** Replace the fixed `Up, Up, Down, Up` enemy sequence with a six-step sequence generated from up, down, left, and right; allow repeated directions because each slot rolls independently.
- **What Changed:** Updated `EnemyEncounter` to generate and cache a random sequence per spawned enemy, show that sequence in the existing popup, and keep the same reset-on-wrong-input behavior.
- **Next Likely Step:** Play-test whether six inputs is the right first enemy length or should be shortened while the EXP-to-enemy transition is still new.

### 2026-04-24 - Basic EXP Bar Sparkle Effect
- **Goal:** Make EXP gains feel more visible when a parry lands.
- **Key Decisions:** Use simple UI `Image` sparkles under the `ExpBar` RectTransform instead of Unity's world-space particle system for the first pass; keep the effect prefab-free and easy to tune in `ExpBarController`.
- **What Changed:** Added smooth fill animation plus a small sparkle burst at the EXP bar fill edge each time `AddParryExp` runs.
- **Next Likely Step:** Play-test the values, then decide whether to replace the square sparkles with a custom sprite or add a full-bar pulse.

### 2026-04-24 - Parry Zoom Punch
- **Goal:** Make successful parries feel meatier with a quick camera zoom.
- **Key Decisions:** Add the zoom to `ParryScreenFeedback` so it plays alongside the existing shake and flash; use a small orthographic-size punch with separate zoom-in and zoom-out timings.
- **What Changed:** Added tunable `zoomAmount`, `zoomInDuration`, and `zoomOutDuration` fields plus a coroutine that briefly zooms the camera in and eases back after each successful parry.
- **Next Likely Step:** Play-test the zoom with the existing shake/flash/VFX to tune the total feedback intensity.

### 2026-04-24 - Streak-Based Player-Focused Parry Zoom
- **Goal:** Make consecutive successful parries zoom toward the player more strongly.
- **Key Decisions:** Keep the existing zoom-out timing behavior; treat `zoomAmount` as the per-parry zoom step; cap the streak at five successful parries; reset the zoom streak on missed parry attempts.
- **What Changed:** Updated `ParryScreenFeedback` to combine shake and zoom offsets cleanly, move the camera toward the player during the zoom punch, and expose `maxZoomStreak` plus `zoomFocusStrength`; updated `ParryPointTracker` to pass the player position on success and reset the streak on misses.
- **Next Likely Step:** Play-test whether full streak zoom feels readable at five parries or should use a lower `zoomFocusStrength`.

### 2026-05-02 - Two-Circle Board Choice Experiment
- **Goal:** Give the player a simple choice on the 3x3 board instead of only chasing one active parry circle.
- **Key Decisions:** Keep the experiment inside `TileCircleSpawner`; spawn two different-colored circles as a wave, wipe the unchosen circle as soon as the player engages one, then spawn the next wave after the chosen circle resolves; keep the value tunable with a serialized `maxActiveCircles` field.
- **What Changed:** Replaced the single active circle reference with an active encounter list, added wave spawning, prevented duplicate circle colors within the same wave, made the EXP-bar enemy transition clear every active circle, and made unchosen circles clear immediately once a circle is engaged.
- **Next Likely Step:** Play-test whether two simultaneous circles create a fun order/risk choice, then decide whether the unchosen circle should disappear instantly, fade out, or award/deny something.

### 2026-05-02 - Basic Lives And Game Over
- **Goal:** Add a simple fail state so missed circle encounters matter.
- **Key Decisions:** Start the player at 3 lives; decrement lives only when a chosen circle encounter actually fails, not when the unchosen choice circle is cleaned up; show game over when lives reach 0.
- **What Changed:** Added a runtime `LivesController` that creates a top-left `Lives` counter and centered `GAME OVER` text; `ParryCircleEncounter` now reports completed versus failed resolution; `TileCircleSpawner` routes failed circle resolutions to the lives controller.
- **Next Likely Step:** Play-test whether game over at 0 lives feels right and whether the frozen state needs restart or retry controls.

### 2026-05-03 - Turkey Leg Health Pickup
- **Goal:** Make the new turkey leg item restore one life and fold it into the two-circle board-choice experiment.
- **Key Decisions:** Spawn at most one turkey leg per circle wave; only spawn it when the player has fewer than 3 lives and can actually heal; place it on one of the active circle tiles so choosing that tile can recover health; clear the pickup with the wave or if its circle is the unchosen one.
- **What Changed:** Added `HealthPickup.cs`; added `RestoreLife`, `MaxLives`, and `CanRestoreLife` to `LivesController`; updated `TileCircleSpawner` to auto-use `Assets/Art/Items/turkeyLeg.png`, scale and center the sprite on a circle tile, and wire collection through player tile landings.
- **Next Likely Step:** Play-test the heal timing and visual size, then decide whether the turkey leg should always appear below 3 lives or use a spawn chance once the loop feels readable.

### 2026-05-03 - Health Pickup Spawner Split
- **Goal:** Keep `TileCircleSpawner` from taking on too many unrelated responsibilities as powerups are added.
- **Key Decisions:** Keep `TileCircleSpawner` as the circle-wave orchestrator; move turkey-leg sprite loading, pickup placement, visual sizing, life-threshold checks, and pickup cleanup into a dedicated `HealthPickupSpawner`.
- **What Changed:** Added `HealthPickupSpawner.cs`; trimmed pickup-specific fields and helper methods out of `TileCircleSpawner`; kept the same gameplay behavior through small calls such as `TrySpawnForWave`, `ClearActivePickup`, and `ClearIfOnTile`.
- **Next Likely Step:** If the health pickup becomes permanent, consider converting the generated turkey-leg visual into a prefab assigned on `HealthPickupSpawner`.

### 2026-05-03 - Class Diagram Documentation
- **Goal:** Make script relationships easier to see for planning future systems.
- **Key Decisions:** Use a repo-tracked `CLASS_DIAGRAM.md` with Mermaid diagrams so the architecture can be viewed in Markdown-friendly tools and updated over time.
- **What Changed:** Added a class relationship diagram, a runtime flow chart, responsibility boundaries, and planning notes for likely next refactors such as encounter variants, powerup spawners, and ScriptableObject circle settings.
- **Next Likely Step:** Keep the diagram updated when new gameplay scripts or major responsibilities are added.

### 2026-05-03 - Future Encounter Architecture Note
- **Goal:** Capture the idea of building future enemies and board challenges from reusable setup data without creating another oversized manager script.
- **Key Decisions:** Think in three simple pieces: recipe data defines what the encounter is, a spawner places it, and a focused behavior script runs its rules.
- **What Changed:** Added a `Future Architecture Ideas` section with plain-language notes about encounter definitions, spawners, and separate behavior scripts for circles, triangles, bosses, and enemy input patterns.
- **Next Likely Step:** Revisit this once triangle hold-parry or boss encounters are ready to build.

### 2026-05-04 - First Pass Dodge Hazard Mechanic
- **Goal:** Add fast movement pressure so empty board tiles create decisions too, not just circle tiles.
- **Key Decisions:** Keep the mechanic split into a small `DodgeMechanic` manager and a focused `DodgeHazard` behavior; spawn a hazard immediately when the player lands on a non-circle tile; randomly choose horizontal or vertical line pairs; deal 1 life of damage if the player stays until the lines collapse or moves through the unsafe axis.
- **What Changed:** Added `DodgeMechanic.cs` and `DodgeHazard.cs`; added a `StartedMoveFromTile` event to `PlayerController`; wired `TileCircleSpawner` to auto-create/configure the dodge mechanic alongside lives, circle, and pickup references.
- **Next Likely Step:** Play-test the default warning/collapse timing values, then tune `warningDuration`, `collapseDuration`, `lineWidth`, colors, and whether every landing should always spawn a hazard.

### 2026-05-04 - Movement-Triggered Circle Spawn Experiment
- **Goal:** Make parry circles feel like part of the fast movement rhythm instead of a separate two-choice wave system.
- **Key Decisions:** Roll for a circle every time the player lands on a tile; use a 60% spawn chance for the current test pass; spawn the circle on the landed tile and immediately start its existing white/red/blue/black encounter behavior; keep only one active circle at a time for the first pass.
- **What Changed:** Added a `PlayerLandingChance` trigger mode to `TileCircleSpawner`, added a tuneable `circleSpawnChanceOnLanding`, and made landed-tile circles start immediately through `ParryCircleEncounter.StartIfPlayerIsOnTargetTile`.
- **Next Likely Step:** Play-test whether 60% gives enough parry action without crowding out dodge hazards, then decide whether dodge hazards should still spawn on failed circle-roll landings.

### 2026-05-04 - Movement-Triggered Safe-Tile Health Drops
- **Goal:** Reintroduce health recovery in a way that supports the faster movement loop.
- **Key Decisions:** Only roll health drops when the player has fewer than 3 lives; trigger the roll on player landings so waiting cannot be abused; use a 10% chance per landing; keep only one active pickup; spawn on another random safe empty tile; make pickups expire after a short lifetime.
- **What Changed:** Reworked `HealthPickupSpawner` into a movement-triggered safe-tile spawner, added `pickupLifetimeSeconds` to `HealthPickup`, made `DodgeMechanic` and `TileCircleSpawner` skip active pickup tiles, and kept turkey legs restoring 1 life on landing collection.
- **Next Likely Step:** Play-test `spawnChanceOnLanding` and `pickupLifetimeSeconds` to decide whether the pickup feels helpful without rewarding stalling.

### 2026-05-04 - Health Drop Spawn Toggle Fix
- **Goal:** Fix health pickups not appearing during testing.
- **Key Decisions:** Let the `HealthPickupSpawner` component itself control whether health drops run instead of adding a second enable/disable toggle on `TileCircleSpawner`.
- **What Changed:** Removed the redundant `healthPickupSpawningEnabled` gate from `TileCircleSpawner`; this prevents old scene-serialized values from disabling the tuned `HealthPickupSpawner` component at runtime.
- **Next Likely Step:** Re-test below 3 lives with a high `spawnChanceOnLanding` to confirm pickups appear on safe empty tiles, then return it to the intended 10% value.
