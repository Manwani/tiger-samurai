# Project Plan

**Project Name:** TigerSamurai  
**Active Branch:** `main`  
**Last Updated:** `2026-04-17`  
**Current Focus:** Establish a persistent shared tracker while continuing combat feel polish around parries, circles, and enemy encounters.  
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
- [ ] Decide the next gameplay feature or polish target after the current parry feedback pass.
- [ ] Bring `TileCircleSpawner` out of debug mode so circles spawn in the intended gameplay flow instead of always using the starting player tile.
- [ ] Play-test the combined parry sound, particle, shake, and flash feedback and tune values if any part feels too strong or distracting.

### Near-Term Cleanup
- [ ] Confirm whether the test `ParryBurst` scene object should stay in `Main.unity` or be removed now that the prefab is spawned by code.
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
