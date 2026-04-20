# Project Plan

**Project Name:** TigerSamurai  
**Active Branch:** `main`  
**Last Updated:** `2026-04-18`
**Current Focus:** First-pass parry encounter progression with a broader white/red/blue/black difficulty ladder, one-at-a-time idle pacing, clearer parry timing feedback, stricter anti-spam parry rules, and enemy spawning temporarily paused for circle testing.
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
### Current Priority
- [ ] Play-test the first-pass white/red parry encounter flow and tune whether red circles feel fair.
- [x] Bring `TileCircleSpawner` out of debug mode so circles spawn in the intended gameplay flow instead of always using the starting player tile.
- [ ] Play-test the combined parry sound, particle, shake, flash, and movement lock feedback together and tune anything that feels too strong or distracting.

### Near-Term Cleanup
- [ ] Confirm whether the test `ParryBurst` scene object should stay in `Main.unity` or be removed now that the prefab is spawned by code.
- [ ] Decide how to show remaining parries on multi-hit circles so red encounters read clearly at a glance.
- [ ] Replace the temporary `P` prompt above the player with final parry-ready art or UI.
- [ ] Re-enable enemy spawning after the current circle-feel test pass is done.
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
