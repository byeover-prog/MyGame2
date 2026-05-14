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
| RunSetup | Baseline added | `RunSetup`, `RunSetupHolder`, and `RunSetupFactory` exist; `GameManager2D.StartGame(RunSetup)` validates before starting; `StageManager2D.BeginStage(RunSetup)` consumes the stage target. | Battle start now has one snapshot owner. Some consumers still read legacy globals until the next migration step. | Confirmed |
| Squad data flow | Partially migrated | `RunSetup` captures `mainId`, `support1Id`, and `support2Id`; `SquadLoadoutRuntime` remains as a compatibility bridge. | User team choices are now snapshotted at run start, but battle bootstrap and card pool still need to consume the snapshot directly. | Confirmed |
| Character catalog | Baseline corrected | `SO_CharacterCatalog.asset` registers yoonseol, hayul, and harin. | Current character assets can be found through the official catalog. | Confirmed |
| Character skill contract | Baseline added | Each current character has 2 `CharacterSkillDefinitionSO` assets and one `CharacterSkillSetSO`. | Adding characters now has a clearer data checklist, but `LevelUpCardGenerator` still needs migration away from scene arrays. | Confirmed |
| Unique passive | Baseline added | `CharacterDefinitionSO.uniquePassiveId` exists and current character assets have unique passive IDs. Runtime activation is still bridged by `CharacterPassiveManager2D`. | The character contract now has a data field, but the runtime manager still needs migration away from hardcoded registration. | Confirmed |
| Exclusive card pool | Baseline corrected, ownership risk remains | `Scene_Game` now has one local `characterSkillSets` entry per current character, each with 2 exclusive skills. `LevelUpCardGenerator` still owns the pool through scene inspector data. | Current duplicate-entry bug is removed, but future characters should migrate to data-owned skill sets instead of scene-local arrays. | Confirmed |
| Common skill card pool | Baseline corrected | `Pool_CommonSkillLevelUpCards.asset` references 8 `CommonSkillCardSO` assets backed by the common skill catalog. | The pool no longer depends on missing card GUIDs, but card weight/balance still needs design validation. | Confirmed |
| Talisman slots | Baseline migrated | `CharacterEquipmentSaveData.MaxSlots = TargetTalismanSlots`; legacy overflow slots are normalized into owned item data. | Active equipped talisman slots now match the confirmed 6-slot rule without deleting old overflow item IDs. | Confirmed |
| Gacha item count | Partially passing | 44 `EquipmentDefinitionSO` assets exist. | Raw item set exists. | Confirmed |
| Equipment database | Baseline wired | `RootBootstrapper` registers `EquipmentDatabaseSO.RuntimeInstance`; `CharacterMetaResolver2D` resolves equipped IDs through `EquipmentDefinitionSO`. | The official release-facing item catalog now has a runtime path; UI wiring still needs follow-up. | Confirmed |
| Gacha runtime wiring | Baseline added | `EquipmentGachaService` consumes `GachaConfigSO`, `EquipmentDatabaseSO`, `MetaWalletService2D`, and save-backed pity state. | Prices, rarity, pity, duplicate refund, and ownership now have a runtime service, but no final shop UI has been migrated yet. | Confirmed |
| Legacy shop coexistence | Risk | `ShopService` still exists beside new `EquipmentGachaService`. | Purchase/equip UI must choose the new equipment path before the old shop path can be retired. | Confirmed |
| Nyang/Soul economy | Baseline migrated | `MetaWalletService2D` owns both `nyang` and `soul`; `CurrencyManager` now stages clear rewards through `SaveManager2D` instead of PlayerPrefs. | Story/Casual shared currency has one save owner. `PlayerSpirit2D` still needs classification as battle-only UI or official Soul UI. | Confirmed |
| Save migration | Baseline added | `PlayerSaveData2D.CurrentVersion` and migration methods exist; `SaveMigrationSpec.md` documents policy. | Future migrations now have a starting point, but legacy stores still need cleanup. | Confirmed |
| Asset integrity | Passing | `AssetIntegrityValidator` passes after `PassiveBalanceTable.asset` repair. | No current missing script assets found by this validator. | Confirmed |
| Debug release safety | Passing with warnings | `DebugObjectValidator`: errors 0, warnings remain in non-build scenes and debug script inventory. | Build scenes are cleaner, but non-build scene debug inventory still exists. | Confirmed |
| Document-code consistency | Failing | `DocumentCodeConsistencyValidator`: missing docs and stale claims. | Collaborators lack a single source of truth. | Confirmed |

## Current Top Release Blockers

1. No explicit Story/Continue checkpoint model.
2. Story flow still needs explicit title/new/continue/lobby routing.
3. Card-pool consumers still need migration from globals to `RunSetup`.
4. Talisman/gacha ownership now has a catalog, but runtime behavior is still split between new equipment and legacy shop systems.
5. Save migration still needs atomic write/backup policy and legacy secondary save classification.

## Non-Blocking But Important

- Non-build scenes still contain debug components.
- `DebugObjectValidatorSpec.md` contains historical findings that should no longer be listed as current confirmed build-scene failures.
- `ArchitectureDecisionMatrix.md` is directionally useful but does not yet fully document the concrete title UI labels.
