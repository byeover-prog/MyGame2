# Current Failure Matrix

## Purpose

This document records the current Phase 1 architecture status after validator-led cleanup.
It replaces the early failure snapshot. Historical failures are preserved as context in
`ArchitectureDiagnosis.md`, `ArchitectureDecisionMatrix.md`, and validator specs.

Evidence levels:

- Confirmed: Direct code, scene, asset, or validator evidence exists.
- Likely: Strong evidence exists, but runtime behavior still needs Unity play verification.
- Needs Verification: Static validation passed, but manual flow testing is still required.
- Unknown: Not enough evidence.

## Phase 1 Validator Status

All Phase 1 validators passed with 0 errors, 0 warnings, and 0 info findings on 2026-05-16.

| Validator | Current Result | Evidence Level |
|---|---:|---|
| Build Scene Validator | Passed 0/0/0 | Confirmed |
| Story Flow Validator | Passed 0/0/0 | Confirmed |
| Continue Checkpoint Validator | Passed 0/0/0 | Confirmed |
| Run Setup Validator | Passed 0/0/0 | Confirmed |
| Character Squad Validator | Passed 0/0/0 | Confirmed |
| Skill Pool Validator | Passed 0/0/0 | Confirmed |
| Talisman Gacha Validator | Passed 0/0/0 | Confirmed |
| Save Compatibility Validator | Passed 0/0/0 | Confirmed |
| Asset Integrity Validator | Passed 0/0/0 | Confirmed |
| Debug Object Validator | Passed 0/0/0 | Confirmed |
| Document Code Consistency Validator | Passed 0/0/0 | Confirmed |

## Current Matrix

| Area | Phase 1 Status | Current Evidence | Remaining Risk | Evidence Level |
|---|---|---|---|---|
| Build scene order | Resolved | Build scene validator passes against enabled scenes. | Manual build smoke test still required before release. | Confirmed |
| Title flow | Resolved for static contract | Story flow validator passes expected Story/Casual/Settings route signals. | UI/UX polish and full title flow playthrough still required. | Confirmed |
| Story New Game | Resolved for static contract | Story flow and continue checkpoint validators pass. | Opening story content and real scene transition timing still need play verification. | Confirmed |
| Continue checkpoint | Resolved | Stage progress save has explicit continue checkpoint ownership. | Old save files should be tested through migration scenarios. | Confirmed |
| Stage start save | Resolved | Continue checkpoint validator confirms stage start save signal. | Runtime death/retry should be verified in Unity play mode. | Confirmed |
| Story Lobby | Baseline contract present | Story flow validator passes current lobby route requirements. | Final Story Lobby UI layout and button binding need play verification. | Needs Verification |
| Casual Lobby | Baseline contract present | Story flow validator passes current Casual route requirements. | Difficulty/map popup UX remains Phase 2 work. | Needs Verification |
| RunSetup | Resolved | RunSetup captures mode, stage/map, squad IDs, talismans, checkpoint, and run config; validator passes. | Future systems must not reintroduce direct global reads. | Confirmed |
| Squad data flow | Resolved for Phase 1 | Run setup, character squad, and skill pool validators pass. | Future card grades/support rules should keep consuming validated run data. | Confirmed |
| Character catalog | Resolved | Character squad validator passes catalog registration and current character contracts. | New character pipeline still needs a human checklist. | Confirmed |
| Character skill contract | Resolved | Each current character satisfies basic, exclusive, ultimate, and unique passive contract. | Ultimate/passive upgrade trees need future validator coverage. | Confirmed |
| Exclusive card pool | Resolved | Skill pool validator passes common and exclusive card pool data. | Phase 2 should extract more pure card-pool services for easier testing. | Confirmed |
| Common skill card pool | Resolved | Skill pool validator passes common pool integrity. | Balance and card rarity systems remain future design work. | Confirmed |
| Talisman slots | Resolved | CharacterEquipmentSaveData uses 6 active slots and migration-safe normalization. | Existing saves with old 8-slot state need manual compatibility play test. | Confirmed |
| Gacha item count | Resolved | Talisman gacha validator confirms 44 equipment definitions. | Future additions need data generation and validator update policy. | Confirmed |
| Equipment database | Resolved | EquipmentDatabaseSO owns release-facing equipment pool and runtime lookup. | UI must continue using equipment IDs, not legacy shop IDs. | Confirmed |
| Gacha runtime wiring | Resolved | EquipmentGachaService consumes GachaConfigSO, EquipmentDatabaseSO, wallet, and save-backed pity state. | Final shop UI still needs Phase 2 integration and play tests. | Confirmed |
| Legacy shop coexistence | Controlled | Talisman gacha validator no longer treats inactive legacy shop code as active runtime owner. | Legacy shop scripts should be retired only after UI migration evidence. | Confirmed |
| Nyang/Soul economy | Resolved | Save compatibility validator passes official meta wallet and Soul persistence checks. | Reward UI should be play-tested against save reload. | Confirmed |
| Save migration | Resolved for baseline | PlayerSaveData2D has current version and migration methods; JsonIO2D uses backup/atomic-style writes. | More old-save fixture tests are needed before release. | Confirmed |
| Weapon secondary save | Controlled | WeaponSaveSystem now uses JsonIO2D; validator only flags it if active in build scenes. | Decide in Phase 2 whether weapon_save.json is legacy or official. | Confirmed |
| Asset integrity | Resolved | Asset integrity validator passes with no missing scripts or broken serialized assets. | Re-run after any asset generation or scene merge. | Confirmed |
| Debug release safety | Resolved | Debug object validator only checks active build scenes and passes 0/0/0. | Non-build development scenes may still contain debug helpers by design. | Confirmed |
| Document-code consistency | Resolved | Document code consistency validator passes. | Docs must be updated whenever target contracts change. | Confirmed |

## Current Top Follow-Up Items

1. Play-test Story New Game from title through Opening Story, Stage 0, Stage 1, and Story Lobby.
2. Play-test Continue after Stage 0 start, Stage 1 start, and Story Lobby entry.
3. Finalize Casual Lobby difficulty/map popup UX and verify RunSetup creation.
4. Decide whether `weapon_save.json` is legacy or an official secondary save file.
5. Build a human checklist for adding a new character, exclusive skills, ultimate, passive, and future card grades.

## Non-Blocking Notes

- Phase 1 validators are static/editor guardrails. Passing them does not replace Unity playthrough tests.
- Non-build development scenes can keep debug helpers, but adding those scenes to Build Settings must re-run validators.
- Legacy shop scripts still exist. They are compatibility or retirement candidates, not the current official talisman/gacha owner.
