# Current Failure Matrix

## Purpose

This document summarizes Phase 1 validator findings against the confirmed target rules. It is a working diagnosis document, not a cleanup checklist.

Evidence levels:

- Confirmed: Direct code, scene, asset, or validator evidence exists.
- Likely: Strong evidence exists, but runtime behavior still needs Unity verification.
- Needs Verification: Static scan found a risk, but impact depends on scene setup or play flow.
- Unknown: Not enough evidence.

## Failure Matrix

| Area | Status | Evidence | Impact | Evidence Level |
|---|---|---|---|---|
| Build scene order | Passing after correction | `BuildSceneValidator` passes with `Scene_Lobby` first. | Boot baseline is stable enough for validation. | Confirmed |
| Title flow | Failing | `StoryFlowValidator`: missing Story Mode, Casual Mode, Settings; generic Start routes to `Scene_Boot`. | User cannot enter confirmed Story/Casual flow through explicit branches. | Confirmed |
| Story New Game | Failing | `StoryFlowValidator`, `ContinueCheckpointValidator`: no explicit Opening Story -> Stage 0 -> Stage 1 -> Story Lobby route. | First-time user flow cannot be verified. | Confirmed |
| Continue checkpoint | Failing | `ContinueCheckpointValidator`, `SaveCompatibilityValidator`: no explicit checkpoint owner. | Continue cannot mean Stage 0 start, Stage 1 start, or Story Lobby entry. | Confirmed |
| Stage start save | Failing | `ContinueCheckpointValidator`: `StageManager.BeginStage` does not save a checkpoint at stage start. | Death/retry and Continue can diverge from target flow. | Confirmed |
| Story Lobby | Unclear | No target Story Lobby button structure found in build flow. | Lobby duties are not verified. | Needs Verification |
| Casual Lobby | Unclear | No target Casual Lobby map/difficulty flow found in build flow. | Casual mode cannot be validated separately. | Needs Verification |
| RunSetup | Failing | `RunSetupValidator`: no explicit `RunSetup` owner; `GameManager2D.StartGame` starts without one. | Battle input is spread across save/static/scene state. | Confirmed |
| Squad data flow | Risk | `RunSetupValidator`, `CharacterSquadValidator`: battle and card generator read globals. | User team choices are hard to replay or validate. | Confirmed |
| Character catalog | Baseline corrected | `SO_CharacterCatalog.asset` registers yoonseol, hayul, and harin. | Current character assets can be found through the official catalog. | Confirmed |
| Character skill contract | Baseline added | Each current character has 2 `CharacterSkillDefinitionSO` assets and one `CharacterSkillSetSO`. | Adding characters now has a clearer data checklist, but `LevelUpCardGenerator` still needs migration away from scene arrays. | Confirmed |
| Unique passive | Baseline added | `CharacterDefinitionSO.uniquePassiveId` exists and current character assets have unique passive IDs. Runtime activation is still bridged by `CharacterPassiveManager2D`. | The character contract now has a data field, but the runtime manager still needs migration away from hardcoded registration. | Confirmed |
| Exclusive card pool | Baseline corrected, ownership risk remains | `Scene_Game` now has one local `characterSkillSets` entry per current character, each with 2 exclusive skills. `LevelUpCardGenerator` still owns the pool through scene inspector data. | Current duplicate-entry bug is removed, but future characters should migrate to data-owned skill sets instead of scene-local arrays. | Confirmed |
| Common skill card pool | Baseline corrected | `Pool_CommonSkillLevelUpCards.asset` references 8 `CommonSkillCardSO` assets backed by the common skill catalog. | The pool no longer depends on missing card GUIDs, but card weight/balance still needs design validation. | Confirmed |
| Talisman slots | Baseline migrated | `CharacterEquipmentSaveData.MaxSlots = TargetTalismanSlots`; legacy overflow slots are normalized into owned item data. | Active equipped talisman slots now match the confirmed 6-slot rule without deleting old overflow item IDs. | Confirmed |
| Gacha item count | Partially passing | 44 `EquipmentDefinitionSO` assets exist. | Raw item set exists. | Confirmed |
| Equipment database | Baseline added | `Assets/GameData/EquipmentDatabase.asset` references 44 `EquipmentDefinitionSO` assets. | The official release-facing item catalog exists; runtime gacha/equip wiring is still pending. | Confirmed |
| Gacha runtime wiring | Failing | `TalismanGachaValidator`: no runtime service consumes `GachaConfigSO`. | Data-driven prices exist but draw flow is not wired. | Confirmed |
| Legacy shop coexistence | Risk | `TalismanGachaValidator`: `ShopService`/`ShopItemSO` coexist with new equipment. | Equip effects can come from the wrong system. | Confirmed |
| Nyang/Soul economy | Partially migrated | `MetaProfileSaveData2D.soul` exists, but PlayerPrefs/runtime Spirit paths still exist. | Story/Casual shared currency can drift until legacy currency is retired. | Confirmed |
| Save migration | Baseline added | `PlayerSaveData2D.CurrentVersion` and migration methods exist; `SaveMigrationSpec.md` documents policy. | Future migrations now have a starting point, but legacy stores still need cleanup. | Confirmed |
| Asset integrity | Passing | `AssetIntegrityValidator` passes after `PassiveBalanceTable.asset` repair. | No current missing script assets found by this validator. | Confirmed |
| Debug release safety | Passing with warnings | `DebugObjectValidator`: errors 0, warnings remain in non-build scenes and debug script inventory. | Build scenes are cleaner, but non-build scene debug inventory still exists. | Confirmed |
| Document-code consistency | Failing | `DocumentCodeConsistencyValidator`: missing docs and stale claims. | Collaborators lack a single source of truth. | Confirmed |

## Current Top Release Blockers

1. No explicit Story/Continue checkpoint model.
2. No `RunSetup` battle-start snapshot.
3. Character/skill/card-pool data is not authoring-friendly.
4. Talisman/gacha ownership now has a catalog, but runtime behavior is still split between new equipment and legacy shop systems.
5. Save migration still needs legacy currency ownership cleanup.

## Non-Blocking But Important

- Non-build scenes still contain debug components.
- `DebugObjectValidatorSpec.md` contains historical findings that should no longer be listed as current confirmed build-scene failures.
- `ArchitectureDecisionMatrix.md` is directionally useful but does not yet fully document the concrete title UI labels.
