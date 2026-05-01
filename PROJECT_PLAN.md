# Project Plan

**Project Name:** TigerSamurai  
**Active Branch:** `main`  
**Last Updated:** `2026-05-01`
**Current Focus:** First-pass parry encounter progression with a broader white/red/blue/black difficulty ladder, one-at-a-time idle pacing, clearer parry timing feedback, stricter anti-spam parry rules, streak-based parry zoom toward the player, a basic `ExpBarController` that fills and sparkles on parries, starts the enemy phase, and randomized six-input enemy defeat prompts.
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
- [ ] Add smoke particle effects when the character dashes.
- [ ] Add directional dash animations for forward and back movement.
- [ ] Figure out the proper frames for the parry animation.
- [ ] Add a triangle enemy variant that requires holding the parry button.
- [ ] Make the parry button cancel and immediately restart the parry animation when pressed, so parry input feels more responsive.
- [ ] When a circle parry enemy is fully defeated, spawn particles that travel to the EXP bar and increase it for a stronger reward effect.

### Current Priority
- [ ] Play-test the first-pass white/red parry encounter flow and tune whether red circles feel fair.
- [x] Bring `TileCircleSpawner` out of debug mode so circles spawn in the intended gameplay flow instead of always using the starting player tile.
- [ ] Play-test the combined parry sound, particle, shake, flash, and movement lock feedback together and tune anything that feels too strong or distracting.
- [ ] Play-test the new streak-based parry zoom toward the player and tune `zoomAmount`, `zoomInDuration`, `zoomOutDuration`, `maxZoomStreak`, and `zoomFocusStrength`.
- [ ] Play-test the basic EXP bar fill amount and enemy-spawn transition, then decide whether the bar should reset, trigger a reward, or gain animation polish when full.
- [ ] Tune the basic EXP bar fill animation and sparkle values so the effect reads clearly without distracting from parry timing.
- [ ] Play-test whether six randomized enemy inputs feels readable and fair after the EXP bar fills.

### Near-Term Cleanup
- [ ] Confirm whether the test `ParryBurst` scene object should stay in `Main.unity` or be removed now that the prefab is spawned by code.
- [ ] Decide how to show remaining parries on multi-hit circles so red encounters read clearly at a glance.
- [ ] Replace the temporary `P` prompt above the player with final parry-ready art or UI.
- [ ] Decide when circle spawning should resume after the EXP-spawned enemy encounter ends.
- [ ] Keep this file updated at the end of meaningful work sessions.
- [ ] Re-check the enemy encounter flow after current combat polish and decide the next improvement.

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
