# Phase 2 Tracker

## Purpose

This document tracks Phase 2 work after the Phase 1 validator baseline.
Phase 2 is runtime proof, workflow simplification, and release-readiness work.

## Starting Point

Phase 1 closed with all validators passing 0 errors, 0 warnings, and 0 info findings.

The project now has static/editor coverage for:

- Build scene baseline
- Story and Casual flow signals
- Continue checkpoint ownership
- RunSetup ownership
- Character and squad contracts
- Skill pool integrity
- Talisman/gacha/economy structure
- Save compatibility
- Asset integrity
- Debug object safety
- Document-code consistency

## Phase 2 Work Queue

| Order | Work Item | Goal | Exit Criteria | Status |
|---:|---|---|---|---|
| 1 | Game scene stabilization | Make `Scene_Game` easier to reason about before broader flow/UI work. | Game-scene runtime owners are not duplicated, test leftovers are inactive, StageCatalog is assigned, and `Game Scene Validator` passes. | In Progress |
| 2 | Minimum runtime smoke pass | Prove the highest-risk Story/Continue/Casual paths without a long QA session. | `Phase2_RuntimeQAProtocol.md` Q-01 through Q-06 are checked. | Ready |
| 3 | Story flow bug fixes | Fix actual Story New Game, death, clear, Continue, and Story Lobby failures. | All Story QA rows pass. | Waiting |
| 4 | Casual flow completion | Finalize difficulty/map popup and Casual RunSetup behavior. | Casual QA rows pass and no Story checkpoint corruption. | Waiting |
| 5 | Human workflow docs | Make adding characters, exclusive skills, talismans, and gacha items simple for collaborators. | New workflow docs exist and are validator-backed. | Waiting |
| 6 | Weapon save policy | Decide whether `weapon_save.json` is official or legacy. | Policy is documented and validator behavior matches it. | Waiting |
| 7 | Legacy Shop retirement | Remove or quarantine inactive shop path only after evidence. | No active UI/runtime dependency exists. | Waiting |
| 8 | Ultimate/passive upgrade validator | Cover 10/20/30/40 ultimate choices and level 50 passive choice. | Validator catches missing or invalid upgrade choices. | Waiting |
| 9 | Card grade design | Add future-proof card rarity/grade schema. | Data has safe defaults and validator rules. | Waiting |
| 10 | Runtime smoke tests | Automate the highest-risk manual checks where practical. | Smoke test or editor tool proves save/load and RunSetup basics. | Waiting |
| 11 | Update loop cleanup | Reduce unnecessary polling without breaking combat. | Stable UI/debug polling is event-driven or intentionally ticked. | Waiting |

## Current Focus

Current focus: Work Item 1, Game scene stabilization.

Do not start broad refactors until `Scene_Game` has one clear runtime owner per system.

## Decision Rules

- If runtime QA fails because wiring is missing, fix wiring narrowly.
- If runtime QA fails because target behavior is unclear, mark it `Needs Design`.
- If runtime QA passes but the workflow is hard for humans, document and simplify the workflow next.
- If a fix changes a Phase 1 contract, update the matching validator in the same work item.
- If legacy code is unused but harmless, do not delete it until dependency evidence is recorded.

## Evidence Log

Add entries here as Phase 2 tests are performed.

| Date | Area | Evidence | Decision |
|---|---|---|---|
| 2026-05-16 | Phase 2 start | Runtime QA protocol created. | Begin manual Unity Play Mode pass. |
| 2026-05-16 | Scene_Game | Common skill runtime owner references were unified, duplicate trial scaler and stray EnemyHealth test object were disabled, and `Game Scene Validator` was added. | Continue with stage catalog/clear-rule ownership next. |
| 2026-05-17 | Scene_Game | Stage 0/1 data assets and StageCatalog were added. `StageManager2D` now drives `EnemySpawner2D`, and the legacy direct timeline spawner is inactive. | Run Game Scene Validator in Unity and then test a short play session. |
