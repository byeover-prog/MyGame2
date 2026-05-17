# Full Script Review - Phase 2

Date: 2026-05-17
Scope: `C:\MyGame\MyGame2`
Purpose: Review the current script structure before deeper refactoring.

This review does not recommend deleting code immediately.
Its purpose is to identify ownership problems, human-unfriendly structure, and release risks.

## Scan Summary

| Area | Script Count | Approx Lines | Notes |
|---|---:|---:|---|
| `Assets/_Game/Scripts` | 374 | 53,913 | Main runtime/editor script area. |
| `Assets/_Game/Editor` | 10 | 1,412 | Additional editor tooling. |
| `Assets/_LSH_Folder` | 15 | 3,408 | Hazard/enemy prototype island. |
| `Assets/JGM_Scenes` | 36 | 8,953 | Boss prototype island. |

Project-wide `.cs` count found by root:

| Root | Count |
|---|---:|
| `Assets/_Game` | 384 |
| `Assets/JGM_Scenes` | 36 |
| `Assets/Sandbox_Legacy` | 36 |
| `Assets/_LSH_Folder` | 15 |
| `Assets/ThirdParty` | 5 |
| `Assets/M Studio` | 2 |
| `Assets/Sirenix` | 1 |

`Scene_Game.unity` currently references 73 direct `Assembly-CSharp` script components and 60 unique script classes.
This number does not include every script inside source prefabs unless Unity serializes that component directly in the scene override.

Build verification:

| Command | Result |
|---|---|
| `dotnet build Assembly-CSharp.csproj` | Passed, 0 warnings, 0 errors |
| `dotnet build Assembly-CSharp-Editor.csproj` | Passed, 0 warnings, 0 errors |

## Static Risk Signals

| Signal | Count | Meaning |
|---|---:|---|
| Files with `Update()` | 121 | Per-frame behavior is spread widely. |
| Files with `FixedUpdate()` | 21 | Physics/runtime movement is also scattered. |
| Files with `LateUpdate()` | 11 | Camera/UI/follower timing exists outside one owner. |
| Files with `Awake()` | 169 | Many scripts self-initialize instead of being coordinated. |
| Files with `Start()` | 42 | Runtime startup order is hard to reason about. |
| Files using object lookup / tag lookup | 56 | Scene dependency discovery is still implicit. |
| Files with static/global patterns | 106 | Some are valid utilities, but several are hidden state owners. |
| Files using direct scene load / input / quit / timescale | 21+ | Flow control is spread across UI, game, story, and debug scripts. |
| Files with `PlayerPrefs` | 3 | Settings is acceptable; progression/currency must stay under save ownership. |
| Files with `OnGUI()` | 3 | Release/debug separation must remain strict. |

## Top Findings

## Critical Code Evidence

These are the first files to inspect before changing the game scene architecture.

| Priority | File | Evidence | Why It Matters |
|---:|---|---|---|
| 1 | `Assets/_Game/Scripts/Core/Gamemanager2d.cs:55` | `GameManager2D` finds `SessionGameManager2D`, `KillCountSource`, and `StageManager2D` in `Awake`. | The top-level game start still depends on scene search instead of an explicit context. |
| 1 | `Assets/_Game/Scripts/Core/Gamemanager2d.cs:63` | `Start()` loads squad runtime state and optionally starts the game. | A scene object can begin the run before other systems are deliberately composed. |
| 1 | `Assets/_Game/Scripts/Core/Gamemanager2d.cs:114` | `StartGame(RunSetup)` sets `RunSetupHolder` and copies setup into `SquadLoadoutRuntime`. | `RunSetup` exists, but the game still mirrors it back into global mutable state. |
| 1 | `Assets/_Game/Scripts/Stage/StageManager.cs:91` | `StageManager2D` finds `SessionGameManager2D` and `EnemySpawner2D`. | Stage ownership is improved, but stage runtime dependencies are still discovered implicitly. |
| 1 | `Assets/_Game/Scripts/Stage/StageManager.cs:152` | `BeginStage(RunSetup)` starts stage, loads map, configures spawner, and saves story checkpoint. | This is a core runtime boundary and should eventually receive a prepared context, not search on its own. |
| 1 | `Assets/_Game/Scripts/Skill/CommonSkill/CommonSkillStartBinder2D.cs:15` | Starting skills still come from a prefab/scene array. | The confirmed rule says starting skill should follow selected main character data. |
| 1 | `Assets/_Game/Scripts/Skill/CommonSkill/CommonSkillStartBinder2D.cs:38` | Start skill applies on `Start()` and also on `RunSignals.StageStarted`. | This double-entry behavior is fragile even with `_appliedThisRun`. |
| 1 | `Assets/_Game/Scripts/Player/Charater/Characterpassivemanager2d.cs:79` | Character passives are registered with hardcoded character IDs and concrete classes. | New character updates still require code edits. |
| 1 | `Assets/_Game/Scripts/Squad/Squadloadout2d.cs:47` | If runtime main ID is empty, default squad is written into `SquadLoadoutRuntime`. | A game scene component can mutate global squad state from defaults. |
| 2 | `Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs:159` | Card generator reads `RunSetupHolder.GetOrCreateFromCurrentState()`. | Better than old direct squad reads, but still pulls global state instead of receiving a run context. |
| 2 | `Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs:397` | Exclusive skills come from `CharacterSkillDatabaseSO`. | This is the right direction and should become the official skill-pool source. |
| 2 | `Assets/_Game/Scripts/Core/Save/SaveManager2D.cs:42` | Save manager increments play time and autosaves from `Update()`. | This is acceptable short-term, but a release save policy should define when autosave is allowed. |
| 2 | `Assets/_Game/Scripts/Run_Scripts/RunSetup.cs:75` | `RunSetupHolder` creates a setup from current state when none exists. | Useful compatibility bridge, but it hides missing setup creation mistakes. |
| 3 | `Assets/_Game/Scripts/Skill/WeaponSaveSystem.cs` | Separate `weapon_save.json` path still exists. | Needs a release/legacy/dev-only decision. |

### 1. `Scene_Game` Is Still the Runtime God Scene

Current `Scene_Game` has many systems self-starting through `Awake`, `Start`, `Update`, static holders, and scene object lookup.

Confirmed direct scene participants include:

- `GameManager2D`
- `StageManager2D`
- `SessionGameManager2D`
- `SaveManager2D`
- `JsonManager2D`
- `SkillBalanceBootstrap2D`
- `SquadRuntimeBattleBootstrap2D`
- `SquadApplier2D`
- `CommonSkillManager2D`
- `CommonSkillStartBinder2D` through prefab/runtime wiring
- `LevelUpCardGenerator`
- `LevelUpRewardApplier`
- `PlayerSkillLoadout`
- `CharacterPassiveManager2D`
- `UltimateController2D`
- `SupportUltimateController2D`
- `EnemySpawner2D`
- HUD/UI/controllers

Risk:

The game can appear to work, but changing one entry path can silently break another.
The inactive `CommonSkillManager2D` issue was a concrete example of this.

Refactor direction:

Create a single game-scene composition path that prepares dependencies before gameplay starts.
The target should be:

`RunSetup -> GameSceneRuntime/Context -> Squad/Meta -> Skills/Passives/Ultimates -> Stage/Spawner -> HUD -> Session Start`

### 2. Runtime Dependency Ownership Is Still Implicit

Many scripts still locate dependencies through:

- `FindFirstObjectByType`
- `FindAnyObjectByType`
- `FindObjectOfType`
- `GameObject.Find`
- `FindGameObjectWithTag`
- `FindWithTag`

Top first-party files by lookup count include:

- `SupportUltimateController2D`
- `PlayerMover2D`
- `PlayerStatRuntimeApplier2D`
- `PurificationOrbProjectile2D`
- `LevelUpRewardApplier`
- `LobbyMenuController`
- `PlayerHealth`
- `BattleBuffController2D`
- `HudConnector`
- `EnemySpawner2D`
- `SquadRuntimeBattleBootstrap2D`

Risk:

This is why scene changes are fragile.
An inactive duplicate, a prefab instance, or a later-loaded object can change behavior without compile errors.

Refactor direction:

Do not try to remove every lookup at once.
Start with `Scene_Game` critical path:

1. `GameManager2D`
2. `StageManager2D`
3. `SquadRuntimeBattleBootstrap2D`
4. `CommonSkillManager2D`
5. `LevelUpRewardApplier`
6. `LevelUpCardGenerator`
7. `CharacterPassiveManager2D`
8. `UltimateController2D`
9. `SupportUltimateController2D`

### 3. `Update()` Count Is a Symptom, Not the Root Cause

There are 121 files with `Update()`.

This does not mean all 121 are wrong.
Projectiles, weapons, movement, effects, and timers naturally need ticking.

The problem is that several unrelated responsibilities use their own polling loops:

- input
- debug hotkeys
- UI visibility
- save autosave
- stage progress
- HUD refresh
- skill firing
- enemy spawning
- reward screens

Risk:

The game becomes hard to pause, test, replay, or validate because time and state advance from too many places.

Refactor direction:

Keep per-object ticking for projectiles and effects.
Move high-level game flow ticking into explicit runtime owners:

- `GameSessionRuntime`
- `StageRuntime`
- `PlayerInputRuntime`
- `HudRuntime`
- `SkillRuntime`

### 4. Skill Architecture Is Split Across Too Many Systems

Current skill-related systems include:

- `CommonSkillConfigSO`
- `CommonSkillCatalogSO`
- `CommonSkillManager2D`
- `CommonSkillStartBinder2D`
- `CharacterSkillDefinitionSO`
- `CharacterSkillDatabaseSO`
- `CharacterSkillSetSO`
- `PlayerSkillLoadout`
- `LevelUpCardGenerator`
- `LevelUpRewardApplier`
- `SkillRunner`
- `WeaponShooterSystem2D`
- `WeaponDefinitionSO`
- `WeaponSkillDeckSO`
- `SkillDefinitionSO`
- `SkillCatalogSO`
- per-skill weapon/projectile scripts

Risk:

Adding a new character or skill can require touching prefab, SO, catalog, database, generator, and scene references.
That matches the user's pain point: "prefab 만들고 SO 2개 만들고 generator에 넣는다."

Refactor direction:

Define one canonical skill contract:

- Character owns:
  - 1 basic skill
  - 2 exclusive level-up skills
  - 1 ultimate
  - 1 unique passive
- RunSetup owns selected squad.
- Skill pool is generated from:
  - common pool
  - main character exclusive skills
  - support character exclusive skills
  - future grade/rule filters
- Scene inspector arrays should not be the official source of the card pool.

### 5. Character Passive Ownership Is Partially Data-Driven, But Runtime Is Still Hardcoded

`CharacterDefinitionSO` now has `uniquePassiveId`.

But `CharacterPassiveManager2D` still registers concrete components directly:

- `YoonseolPassive_Hokhan`
- `HayulPassive_Dosa`
- `HarinPassive_Bongukkembeop`

Risk:

Adding a new character still requires code edits.
This blocks long-term live updates.

Refactor direction:

Move passive resolution toward a registry/factory:

`CharacterDefinitionSO.uniquePassiveId -> CharacterPassiveCatalog -> Runtime Passive Behaviour/Effect`

Do not remove the current hardcoded passives immediately.
Bridge them through a catalog first.

### 6. Save Ownership Is Better Than Before, But Legacy Edges Still Exist

Current official save path is centered around:

- `SaveManager2D`
- `PlayerSaveData2D`
- `MetaProfileSaveData2D`
- `JsonIO2D`

This is much better than the original state.

Remaining review concern:

- `WeaponSaveSystem` still has a separate `weapon_save.json` path.
- It may be legacy or development-only, but it is still a separate persistence owner in code.

Risk:

Multiple persistent files are acceptable only when the ownership is documented and migration policy exists.

Refactor direction:

Decide whether weapon save is:

- release save data
- legacy migration input
- development-only tool
- removable after validation

### 7. Scene Flow Is Still Spread Across UI Scripts

Direct scene loading appears in:

- `SquadFormationController`
- `FormationStartExitButtons`
- `PauseUIController`
- `LobbyMenuController`
- `DefeatUIController2D`
- `ClearUIButtonHandler`
- `GameManager2D`
- `StoryContinueCheckpointService`
- `StoryClearRouteService`
- `FormationToolkitPresenter2D`

Risk:

Story/Casual/Retry/Clear/Continue can drift because buttons own route decisions.

Refactor direction:

Keep UI scripts dumb.
UI should call a flow service:

`Button -> GameFlowService / SceneRouteService -> RunSetup / Checkpoint / Scene`

### 8. First-Party Runtime Code Has Weak Human-Friendly Boundaries

Large portions of `Assets/_Game/Scripts` have no namespace.
Many file names do not match class names exactly because of casing or old naming style.

Examples:

- `Gamemanager2d.cs` -> `GameManager2D`
- `Hudconnector.cs` -> `HudConnector`
- `Attributesynergymanager2d.cs` -> `AttributeSynergyManager2D`
- `Characterpassivemanager2d.cs` -> `CharacterPassiveManager2D`
- `Equipmentdatabaseso.cs` -> `EquipmentDatabaseSO`

Also confirmed:

- `Assets/_Game/Scripts/Skill/﻿CharacterSkill` contains an invisible BOM character in the folder name.
- Character passive files contain spaces in filenames:
  - `Harinpassive bongukkembeop.cs`
  - `Hayulpassive dosa.cs`
  - `Yoonseolpassive hokhan.cs`

Risk:

This does not always break compilation, but it makes collaboration, search, automation, and asset references harder.

Refactor direction:

Do not rename files blindly because Unity `.meta` and script references can break.
Handle naming cleanup as a controlled Unity refactor phase after behavior is covered by validators.

### 9. Boss and Hazard Code Are Separate Prototype Islands

`Assets/JGM_Scenes` and `Assets/_LSH_Folder` contain their own boss, enemy, hazard, target, debug, and config scripts.

These systems use their own patterns:

- separate boss target providers
- direct tag lookup
- debug flags/logging
- independent SO/config structures
- Korean text that appears mojibake in some files

Risk:

If these are intended for release, they are not yet integrated into the official stage/enemy/runtime architecture.
If they are not intended for release, they should be quarantined from release scenes and validators.

Refactor direction:

Pick one:

1. Integrate them through `StageDefinitionSO` / `StageCatalogSO`.
2. Mark them as prototype-only and keep them out of build scenes.

### 10. Editor Tooling Exists, But Ownership Is Mixed

There are many useful editor helpers:

- validators
- SO generators
- prefab builders
- skill balance converters
- missing asset builders

Most are guarded with `#if UNITY_EDITOR`, but some are located under runtime-looking folders such as:

- `Assets/_Game/Scripts/Core/Balance/SkillBalanceCsvToJsonEditor.cs`
- `Assets/_Game/Scripts/Core/Balance/SkillBalanceJsonToCsvEditor.cs`
- `Assets/_Game/Scripts/Core/Debuglogreplacer.cs`
- `Assets/_Game/Scripts/Skill/Harin/Jwagyeokyoselevelautofill.cs`

Risk:

Collaborators cannot tell whether a script is runtime or tooling by path.

Refactor direction:

Move editor-only tools into `Assets/_Game/Editor` or `Assets/_Game/Scripts/Editor` in a controlled phase.

## Current Judgment

The script count itself is not the main problem.

The real problem is:

1. One scene owns too many runtime decisions.
2. Many scripts initialize themselves independently.
3. Dependencies are discovered at runtime instead of composed explicitly.
4. Skill/card/passive ownership is split across data, prefab, scene, and code.
5. Some prototype islands are not clearly marked as release or non-release.
6. File/folder naming makes the project harder for humans to navigate.

## Recommended Refactor Order

### Phase 2A - Game Scene Composition

Goal:
Make `Scene_Game` start from one readable runtime composition path.

Work:

1. Create `GameSceneContext`.
2. Create `GameSceneRuntime`.
3. Move dependency discovery into one place.
4. Let `GameManager2D` delegate startup to the runtime.
5. Keep existing behavior while removing scattered startup responsibility.

Success criteria:

- `Scene_Game` has one official startup owner.
- Existing validators still pass.
- Basic skill, exclusive skills, passive, ultimate, stage spawn, clear/defeat still work.

### Phase 2B - Skill Ownership Consolidation

Goal:
Make skill addition data-driven and human-friendly.

Work:

1. Make `CharacterDefinitionSO.BasicSkillId` drive starting skill.
2. Make `CharacterSkillDatabaseSO` the official exclusive skill source.
3. Remove scene-inspector skill-pool ownership from release flow.
4. Keep `CommonSkillManager2D` as runtime executor for now.

Success criteria:

- Adding a character does not require editing `LevelUpCardGenerator`.
- Adding an exclusive skill does not require touching `Scene_Game`.

### Phase 2C - Character Passive Registry

Goal:
Stop hardcoding character passive classes inside `CharacterPassiveManager2D`.

Work:

1. Create passive catalog/registry.
2. Bridge current three passives through registry entries.
3. Let `uniquePassiveId` resolve runtime behavior.

Success criteria:

- Adding a character passive has one documented path.
- `CharacterPassiveManager2D` does not need a new hardcoded line per character.

### Phase 2D - Flow Routing Cleanup

Goal:
Remove direct scene route decisions from button scripts.

Work:

1. Introduce one route service for title/lobby/game/defeat/clear.
2. UI buttons call route commands.
3. Story/Casual/Continue rules live in one place.

Success criteria:

- Clear/defeat/continue routing is testable without reading every UI button.

### Phase 2E - Human-Friendly Cleanup

Goal:
Make the project navigable for collaborators.

Work:

1. Move editor-only scripts into editor folders.
2. Normalize file/class naming.
3. Remove invisible BOM folder name through Unity-safe move.
4. Quarantine or integrate prototype boss/hazard folders.

Success criteria:

- A collaborator can find the correct system owner by folder name.
- Validator/build still pass after each small move.

## Do Not Do Yet

Do not mass-delete scripts.
Do not mass-rename files.
Do not move folders without Unity validation.
Do not replace every `Update()` with UniTask/R3 just to appear advanced.
Do not introduce a large DI framework.

The correct direction is a small explicit composition layer first.
