# Phase 1 Closeout

## Status

Phase 1 is complete as a validator-led architecture stabilization pass.
The project has not been fully rebuilt into its final long-term architecture, but the release-facing baseline is now guarded by editor validators.

Closeout date: 2026-05-16

## Confirmed Validator Baseline

All Phase 1 validators passed with 0 errors, 0 warnings, and 0 info findings:

| Validator | Result |
|---|---:|
| Build Scene Validator | Passed 0/0/0 |
| Story Flow Validator | Passed 0/0/0 |
| Continue Checkpoint Validator | Passed 0/0/0 |
| Run Setup Validator | Passed 0/0/0 |
| Character Squad Validator | Passed 0/0/0 |
| Skill Pool Validator | Passed 0/0/0 |
| Talisman Gacha Validator | Passed 0/0/0 |
| Save Compatibility Validator | Passed 0/0/0 |
| Asset Integrity Validator | Passed 0/0/0 |
| Debug Object Validator | Passed 0/0/0 |
| Document Code Consistency Validator | Passed 0/0/0 |

## What Phase 1 Established

| Area | Established Contract |
|---|---|
| Entry and scenes | Enabled build scenes have a stable validated baseline. |
| Story flow | Title, Story/Casual branching, checkpoint ownership, and story clear routing have static validator coverage. |
| Continue | Continue points are explicit save points. Stage start and Story Lobby checkpoints are modeled. |
| Run start | `RunSetup` is the validated run-start snapshot for mode, stage/map, squad, talismans, checkpoint, and config. |
| Squad | Current characters satisfy catalog, basic skill, two exclusive skills, ultimate, and unique passive contracts. |
| Skill pool | Common and exclusive card-pool data validates without duplicate character entries or null cards. |
| Talisman/gacha | 44 equipment-backed talisman items, 6 equip slots, data-driven gacha costs/rates, duplicate refund, and runtime equipment effects are validated. |
| Save | Save versioning, migration baseline, Soul/Nyang persistence, continue checkpoint persistence, and atomic-style JSON writes are guarded. |
| Debug safety | Build scenes are free of known release-risk debug components and unguarded hotkey helpers. |
| Docs | Core architecture docs and validator specs are present and consistent with code-level contracts. |

## Important Implementation Outcomes

- `RunSetup`, `RunSetupHolder`, and `RunSetupFactory` now define one run-start input shape.
- `StageProgressSaveData` owns explicit Story Continue checkpoint data.
- `StoryContinueCheckpointService` and `StoryClearRouteService` separate checkpoint and clear-route duties from ad hoc UI routing.
- `MetaWalletService2D` owns both Nyang and Soul through `SaveManager2D`.
- `CharacterEquipmentSaveData` uses 6 active talisman slots and migrates old overflow slots into inventory ownership.
- Equipment/gacha is now the official talisman path: `EquipmentDefinitionSO`, `EquipmentDatabaseSO`, `GachaConfigSO`, and `EquipmentGachaService`.
- `CharacterMetaResolver2D` applies equipped talisman effects through `EquipmentDefinitionSO`.
- `LevelUpCardGenerator` uses data-owned character skill definitions instead of duplicate scene-local character entries.
- `JsonIO2D` centralizes save writes with temporary and backup file handling.
- Debug and save validators now distinguish active build-scene risk from inactive development-only inventory.

## What Phase 1 Did Not Claim

Phase 1 did not prove that every game flow is fun, balanced, or complete.
It also did not remove every legacy script.

The validators prove that the agreed structural contracts are present and that known release-blocking inconsistencies are not currently detected.
Manual Unity playthrough and gameplay QA are still required.

## Remaining Design Debt

| Debt | Why It Remains | Phase 2 Direction |
|---|---|---|
| Story flow runtime polish | Static routes pass, but complete playthrough timing/content still needs QA. | Play-test title -> story -> Stage 0 -> Stage 1 -> Story Lobby. |
| Casual Lobby UX | Static entry exists, but difficulty/map popup needs final behavior. | Define `CasualRunSetup` rules and validate difficulty/map selections. |
| Weapon secondary save | `WeaponSaveSystem` uses `weapon_save.json`; currently controlled and inactive unless a build scene references its applier. | Decide official vs legacy, then migrate or retire. |
| Legacy Shop scripts | Shop code remains as inactive compatibility inventory. | Remove only after confirming no UI/runtime dependency remains. |
| New character workflow | Current characters pass, but humans need a clear addition checklist. | Write and validate a character-add pipeline document. |
| Future card grades | Card pool is valid, but grade rules are not designed yet. | Add grade fields with defaults, then validator rules. |
| Ultimate/passive upgrades | Base character contract exists, but upgrade tree rules need deeper validation. | Add validator coverage for 10/20/30/40 ultimate choices and level 50 passive choice. |
| Update loop cleanup | UpdateLoopAudit exists, but broad loop refactor was intentionally not mixed into Phase 1. | Convert stable UI/debug polling first; leave combat movement loops until profiling/design requires it. |

## Phase 2 Recommended Order

1. Manual playthrough validation for Story New Game and Continue checkpoints.
2. Casual Lobby RunSetup/difficulty/map selection completion.
3. Human workflow docs: add character, add exclusive skill, add talisman, add gacha item.
4. Weapon save policy decision: official secondary save or legacy removal candidate.
5. Legacy Shop retirement plan after UI migration evidence.
6. Ultimate/passive upgrade tree validator.
7. Card grade schema and validator.
8. Runtime playmode tests or scripted smoke tests for save/load and run setup.
9. Update loop cleanup based on `UpdateLoopAudit.md`.

## Required Rule For Future Work

Any future feature that changes one of these contracts must update the matching validator in the same change:

- Scene entry or Story/Casual flow -> Story Flow Validator or Build Scene Validator.
- Continue point or save schema -> Continue Checkpoint Validator and Save Compatibility Validator.
- Character, squad, passive, ultimate, or exclusive skills -> Character Squad Validator.
- Common/exclusive card-pool rules -> Skill Pool Validator.
- Talisman/gacha/economy -> Talisman Gacha Validator and Save Compatibility Validator.
- Debug helpers or build scene additions -> Debug Object Validator.
- Docs or confirmed design rules -> Document Code Consistency Validator.

## Unity QA Checklist

Before treating Phase 1 as release-safe, run this manually in Unity:

1. Open Unity and let compilation finish.
2. Run every validator from `Tools/Honryeom/Validation`.
3. Start from the title scene.
4. Test Story New Game through Stage 0 start save.
5. Die before Stage 0 clear and confirm restart behavior.
6. Clear Stage 0 and confirm story transition.
7. Start Stage 1 and confirm checkpoint save.
8. Die before Stage 1 clear and confirm restart behavior.
9. Clear Stage 1 and confirm Story Lobby entry save.
10. Restart the app and test Continue from the latest checkpoint.
11. Enter Casual Mode, choose map/difficulty, and confirm RunSetup starts the correct run.
12. Equip 6 talismans and confirm only main character receives their effects.
13. Perform gacha draw and confirm Nyang cost, duplicate refund, and ownership persistence.
