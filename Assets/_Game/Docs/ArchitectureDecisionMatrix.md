# Architecture Decision Matrix

## Purpose

This document defines the rebuilding criteria for MyGame2.

The goal is not to use many design patterns. The goal is to choose the simplest structure that fits each problem, keeps the current behavior stable, and makes future character, skill, talisman, gacha, save, and flow updates easier to add and verify.

This document is a decision baseline. Code changes, deletions, folder moves, and migrations should refer to this file before implementation.

## Non-Negotiable Principles

- Do not use quick temporary fixes to pass the current situation.
- Preserve existing behavior during migration.
- Do not delete or move code without evidence and a migration path.
- Prefer human-friendly ownership, naming, and workflows over clever architecture.
- Keep SOLID principles where they reduce real coupling and confusion.
- Make user choices traceable from lobby selection to battle behavior.
- Make release readiness verifiable by validators, not by manual confidence.
- New characters, skills, talismans, and card rules must become easier to add over time.

## Current Evidence Snapshot

| Area | Current Evidence | Risk |
|---|---|---|
| Build scenes | `ProjectSettings/EditorBuildSettings.asset` currently includes `Scene_Lobby`, `Scene_Boot`, `Scene_Game`. | Target title/story/casual flow is not represented as an explicit build-scene flow. |
| Character data | `CharacterDefinitionSO` exists and owns ID, visuals, basic skill ID/config, ultimate, support buff, stats, upgrade tree. | It does not yet fully express the confirmed long-term character contract: 1 basic skill, 2 exclusive skills, 1 ultimate, 1 innate passive, support effect, future card grade rules. |
| Skill data | `SkillDefinitionSO` exists for skill ID, type, UI, level text, passive stat type, and optional character skill balance data. | Skill execution, targeting, grade, card-pool source, and runtime behavior ownership are not fully separated. |
| Card pool | `LevelUpCardGenerator` builds common, passive, and character exclusive candidates. It reads `SquadLoadoutRuntime.MainId/Support1Id/Support2Id`. | Card rules are embedded in a scene component instead of a reusable rule model. Future grade rules will be hard to verify. |
| Squad data | `SquadLoadoutRuntime` stores static main/support IDs and writes formation to save. | Useful bridge, but it is not a complete run-start snapshot. Battle still needs a single immutable `RunSetup`. |
| Run config | `RunConfigHolder` stores a static `RunConfigSO` and raises `RunConfigChanged`. | This covers part of run configuration, but not user choices, talismans, progression, generated card pool, checkpoint, or mode-specific flow. |
| Gacha config | `GachaConfigSO` owns rates, cost, pity, 10-pull guarantee, duplicate refunds. | Good direction for configurable price, but must be reconciled with 44 talisman/equipment items and legacy shop paths. |
| Update usage | Many weapon, HUD, ultimate, debug, and runtime components use `Update`. | The issue is not `Update` itself. The issue is hidden game responsibility inside scattered per-frame scripts. |

## Decision Matrix

### 1. Game Flow

| Decision Item | Choice |
|---|---|
| Problem | Title, Story New Game, Continue, Stage 0/1, Story Lobby, Casual Lobby, death restart, and save checkpoints need a single understandable owner. |
| Selected Structure | `GameFlow` state machine plus async flow sequence for scene transitions and story/stage steps. |
| Candidate Patterns | FSM, hierarchical state machine, flow sequence. UniTask can be considered later for async scene/story/save sequencing. |
| Why This Fits | The confirmed game flow is stateful and checkpoint-based. A state model makes allowed transitions explicit, while a sequence model keeps story -> save -> stage order readable. |
| Avoid | Do not let buttons directly load arbitrary scenes without flow validation. Do not spread continue logic across title UI, save manager, and stage scripts. |
| Current Evidence | Build Settings only list `Scene_Lobby`, `Scene_Boot`, `Scene_Game`; no explicit target flow contract is visible there. |
| Validation | Story Flow Validator and Continue Checkpoint Validator must check allowed states, checkpoint targets, and scene existence. |
| Migration | First document states and checkpoints. Then add a read-only flow validator. Then route title/lobby buttons through a flow facade before changing scene layout. |

### 2. Run Setup

| Decision Item | Choice |
|---|---|
| Problem | A battle must start from one clear snapshot of user choices and mode data. |
| Selected Structure | `RunSetup` DTO built by `RunSetupBuilder`. |
| Candidate Patterns | DTO, Builder, immutable snapshot, facade. |
| Why This Fits | Main/support characters, talismans, mode, stage/map, difficulty, progression upgrades, and card-pool inputs must be frozen before battle starts. This makes battle reproducible and debuggable. |
| Avoid | Do not let battle systems independently read live static state, scene objects, and save data whenever they need something. |
| Current Evidence | `SquadLoadoutRuntime` and `RunConfigHolder` exist separately. `LevelUpCardGenerator` reads static squad IDs directly. |
| Validation | RunSetup Validator must verify required fields, IDs, 6 talismans, mode, stage/map, difficulty, and generated card-pool inputs. |
| Migration | Keep existing `SquadLoadoutRuntime` as a bridge. Add `RunSetup` as a read model first. Move battle bootstrapping to consume `RunSetup` after validators pass. |

### 3. Character Definition

| Decision Item | Choice |
|---|---|
| Problem | Future updates will continuously add characters, each with basic skill, 2 exclusive skills, ultimate, innate passive, support effect, and progression choices. |
| Selected Structure | Character-centered `CharacterDefinition` as the primary authoring unit. |
| Candidate Patterns | Data-driven definition, catalog, validator, composition. |
| Why This Fits | Character addition should be a checklist around one definition, not a search across prefabs, generator arrays, scene fields, and hardcoded IDs. |
| Avoid | Do not encode character-exclusive skills only in a card generator array. Do not hardcode innate passives per character without a definition reference. |
| Current Evidence | `CharacterDefinitionSO` already owns basic skill and ultimate data, but not the full confirmed long-term contract. |
| Validation | Character/Squad Validator must check 1 basic skill, 2 exclusive skills, 1 ultimate, 1 innate passive, support effect, icons, resolver prefab, and unique ID. |
| Migration | Extend definition shape carefully after catalog audit. Add validator before requiring every existing character to satisfy the full future contract. |

### 4. Skill Definition And Execution

| Decision Item | Choice |
|---|---|
| Problem | Skills need clear authoring data, card data, runtime execution, targeting, cooldown, and presentation boundaries. |
| Selected Structure | Split skill into definition, runtime instance, cast behavior, targeting policy, and scheduler-managed cooldown where practical. |
| Candidate Patterns | Strategy, scheduler/ticker, data-driven definition, factory, runtime instance. |
| Why This Fits | New skills should not require each weapon to invent its own update loop, targeting, cooldown, and card binding. Strategy keeps behavior replaceable without making a giant skill class. |
| Avoid | Do not jump to full ECS or a huge generic rule engine before the current skill contracts are stable. Do not remove all `Update` methods blindly. |
| Current Evidence | Many weapon scripts own `Update -> cooldown -> Fire`. `SkillDefinitionSO` owns UI/level data but not execution policy. |
| Validation | Skill Pool Validator must check skill IDs, owner/source, max level, prefab mapping, targeting/cast policy, and card eligibility. |
| Migration | First classify existing skills by attack pattern. Then introduce shared scheduler for simple cooldown-fired weapons. Keep special cases such as orbit/area/projectile movement local until stable. |

### 5. Card Pool Rules

| Decision Item | Choice |
|---|---|
| Problem | Main/support choices must change the card pool, and future card grades may be added. |
| Selected Structure | `CardPoolRule` and `CardPoolBuilder` generated from `RunSetup`. |
| Candidate Patterns | Specification, weighted table, rule list, data-driven catalog. |
| Why This Fits | The game needs to answer why a card appeared. Rules make source, grade, unlock, owner, rarity, and max-level filtering explicit. |
| Avoid | Do not keep card-pool truth inside scene component arrays only. Do not introduce a complex rule engine until basic rules are data-backed and validated. |
| Current Evidence | `LevelUpCardGenerator` currently builds candidates using `SkillCatalogSO`, `CommonSkillCatalogSO`, `characterSkillSets`, and `SquadLoadoutRuntime`. |
| Validation | Skill Pool Validator must simulate a RunSetup and verify expected main/support exclusive cards can enter the pool. |
| Migration | Extract candidate collection into a pure service while keeping current generator UI. Add optional grade fields with safe defaults before grade gameplay is implemented. |

### 6. Talisman, Gacha, And Economy

| Decision Item | Choice |
|---|---|
| Problem | Talisman count, gacha price, 44 items, duplicate refund, Nyang/Soul usage, and old shop paths need one standard. |
| Selected Structure | `TalismanDefinition` or equipment-backed talisman catalog, `GachaTable`, and wallet/economy service. |
| Candidate Patterns | Catalog, weighted random table, repository, policy. |
| Why This Fits | 44 gacha items are small enough for a readable weighted table. Prices must be data-configurable. Currency ownership must be explicit. |
| Avoid | Do not hardcode gacha price in UI or button handlers. Do not keep legacy shop and new equipment as equal sources of truth. |
| Current Evidence | `GachaConfigSO` already owns rates and costs. `EquipmentDatabaseSO` exists. Legacy `ShopService` and shop database also exist. |
| Validation | Talisman/Gacha Validator must check exactly 44 gacha entries, 6 equip slots, valid rates, configurable costs, duplicate policy, and Nyang/Soul usage boundaries. |
| Migration | Choose whether talismans are the public name over equipment data or a separate domain. Then adapt legacy shop through a facade or mark it legacy-only. |

### 7. Save And Continue

| Decision Item | Choice |
|---|---|
| Problem | Save points are continue points. Story and Casual share meta progress but have different flow needs. |
| Selected Structure | Versioned `SaveData`, `CheckpointData`, repository facade, and migration functions. |
| Candidate Patterns | Repository, data mapper, DTO, versioned save migration. |
| Why This Fits | Continue must restore exact story/stage/lobby point, while shared meta data such as talismans and player level must remain common across modes. |
| Avoid | Do not let stage scripts, UI buttons, and managers invent their own continue semantics. Do not store only loose scene names without checkpoint meaning. |
| Current Evidence | `SaveManager2D` exists and is already used by formation/runtime systems, but target checkpoint model needs explicit validation. |
| Validation | Save Compatibility Validator must load current and older save shapes, ensure defaults, and verify checkpoint meaning. |
| Migration | Add save version and checkpoint fields compatibly. Write migration before removing any legacy fields. |

### 8. HUD And Reactive Updates

| Decision Item | Choice |
|---|---|
| Problem | HUD currently contains multiple polling-style updates for HP, timer, skill slots, ultimate status, quest, and profile data. |
| Selected Structure | Event-driven HUD presenters. R3 can be considered later for high-value observable state. |
| Candidate Patterns | Observer, MVP, reactive streams, event facade. |
| Why This Fits | UI should display state, not own gameplay truth. Event-driven UI reduces hidden polling and makes ownership clearer. |
| Avoid | Do not introduce R3 everywhere before the event ownership is clear. Do not convert true per-frame display such as timer blindly if a simple throttled tick is clearer. |
| Current Evidence | `HUDController`, `Hudconnector`, `TimerHUD`, `SkillSlotHUDController`, and profile HUD code contain per-frame refresh logic. |
| Validation | UI/HUD validator can later check duplicate HUD roots and required event bindings. For Phase 1, Update Loop Audit should classify candidates. |
| Migration | Convert stable state changes first: currency, HP, skill-slot changes, formation, level-up state. Keep timer and cooldown display as controlled tick or model event. |

### 9. Update Loop Ownership

| Decision Item | Choice |
|---|---|
| Problem | Many scripts use `Update`, but not all are wrong. The current risk is scattered responsibility. |
| Selected Structure | Update Loop Audit plus domain tick ownership. |
| Candidate Patterns | Scheduler, domain ticker, event-driven state, object pool lifecycle. |
| Why This Fits | Player movement, projectiles, active areas, camera follow, and some VFX need per-frame or physics ticks. HUD polling, debug keys, and duplicated cooldown loops are stronger migration candidates. |
| Avoid | Do not judge by count alone. Do not remove `Update` from physics/projectiles without a performance and behavior reason. |
| Current Evidence | `_Game/Scripts` contains many frame-loop methods, especially in `Skill` and `UI`. Some systems already use central tick ideas, such as `VfxReturnTicker`. |
| Validation | Debug Object Validator and future Update Loop Audit must classify each `Update` as keep, event candidate, scheduler candidate, debug gate, or needs verification. |
| Migration | Start with debug gating, then HUD polling, then simple weapon cooldown loops. Leave special movement/projectile logic until profiling or skill model migration requires it. |

### 10. Debug And Release Safety

| Decision Item | Choice |
|---|---|
| Problem | Debug objects and debug hotkeys can remain in build scenes and runtime controllers. |
| Selected Structure | Build-time validator and explicit debug gating. |
| Candidate Patterns | Validator pipeline, compile flags, build policy. |
| Why This Fits | Release safety should not rely on manual scene inspection. |
| Avoid | Do not silently delete debug helpers without knowing whether they are used by current test workflows. Gate first, remove later when safe. |
| Current Evidence | Debug runtime HUD, UI raycast probes, and F9/F10-style runtime debug reset patterns were found during audit. |
| Validation | Debug Object Validator must scan build scenes and scripts for known debug components and release-forbidden hotkeys. |
| Migration | Define allowed debug contexts, then gate or remove from release scenes. |

### 11. Validators

| Decision Item | Choice |
|---|---|
| Problem | The project currently depends too much on manual confidence. |
| Selected Structure | Validator pipeline before broad migration. |
| Candidate Patterns | Validator pipeline, specification, asset integrity checks. |
| Why This Fits | The project has scene references, ScriptableObjects, save data, prefabs, and document-code contracts. Validators catch structural mistakes before runtime. |
| Avoid | Do not treat validators as cleanup tools that delete assets. They should report, classify, and block unsafe release states. |
| Current Evidence | Broken or unclear asset references, build scene debug objects, gacha/equipment/shop overlap, and data ownership gaps are all validator-suitable issues. |
| Validation | Validators are the validation layer. Each validator must state target, rule, severity, and suggested owner. |
| Migration | Build validators in this order: Asset Integrity, Debug Object, Talisman/Gacha, Continue Checkpoint, RunSetup, Character/Squad, Skill Pool, Story Flow, Save Compatibility, Document-Code Consistency. |

## Algorithm Selection Rules

| Situation | Preferred Algorithm | Reason |
|---|---|---|
| 44-item gacha table | Simple cumulative weighted random | Easy to inspect, fast enough, designer-friendly. |
| Card selection from small/medium candidate pool | Weighted random with filters and recent-history penalty | Current scale does not justify complex alias tables. |
| Future very large weighted table with frequent pulls | Alias method only if profiling proves need | Faster random sampling but harder to maintain. |
| Nearest enemy targeting with small enemy count | Direct scan | Simplest and reliable. |
| Nearest enemy targeting with many enemies and many skills | Spatial hash/grid | Use only when profiling or enemy count makes direct scan too expensive. |
| Projectile and VFX reuse | Object pool | Reduces allocation and instantiate/destroy churn. |
| Story and scene sequence | Async sequence | Keeps ordered flow readable and failure points explicit. |
| UI state change | Event/observer | Updates only when state changes. |
| Continuous movement/physics | Update/FixedUpdate | Correct for movement, physics, and per-frame visual behavior. |

## Pattern Use Rules

| Pattern | Use When | Do Not Use When |
|---|---|---|
| FSM | The system has finite states and allowed transitions. | A simple one-shot operation would be enough. |
| Strategy | Behavior changes by data or character/skill choice. | There are only one or two fixed cases that will not grow. |
| Builder | A valid object requires combining many inputs. | The object has only one or two trivial fields. |
| DTO | Data must cross boundaries clearly. | The object needs behavior and lifecycle ownership. |
| Repository | Save/load access must be centralized. | The data is local temporary runtime state. |
| Observer/Event | Many listeners need state-change notification. | The change happens every frame and is easier as a tick. |
| Reactive/R3 | State streams become complex enough to justify it. | The team cannot yet trace ownership of events and subscriptions. |
| UniTask | Async sequences must be readable and cancellable. | Normal per-frame gameplay logic is being forced into async. |
| Facade | Old and new systems must coexist during migration. | It hides unclear ownership instead of clarifying it. |
| Adapter | Legacy code must connect to a new contract temporarily. | It becomes the new permanent source of truth. |
| Validator | Mistakes can be detected from assets, scenes, data, or code references. | The rule is subjective and cannot be checked consistently. |

## Migration Guardrails

1. Add documents and validators before destructive refactors.
2. Introduce new target contracts as read-only or bridge layers first.
3. Keep legacy systems working until the replacement is verified.
4. Migrate one domain at a time: flow, run setup, character/squad, skill pool, talisman/gacha, save.
5. Every migration step must define what user behavior is preserved.
6. Every new data definition must have a validator rule before it becomes mandatory.
7. Every adapter must have a planned removal condition.

## First Implementation Candidates

| Priority | Candidate | Reason |
|---|---|---|
| 1 | `UpdateLoopAudit.md` | Converts the instructor feedback into a concrete keep/migrate/debug-gate list. |
| 2 | `RunSetup` design spec | Establishes one battle-start contract without immediately breaking existing systems. |
| 3 | Character contract validator spec | Makes future character addition human-friendly and checkable. |
| 4 | Card pool rule design spec | Prepares for support-character cards and future grade rules. |
| 5 | Talisman/gacha ownership decision | Resolves equipment/shop/gacha naming and data-source conflict. |

## Final Standard

A rebuilt system is acceptable only when a teammate can answer these questions without reading unrelated scripts:

- What owns the data?
- What creates the runtime object?
- What changes during the run?
- What is saved?
- What is validated?
- How do I add one more character, skill, talisman, or card rule?
- What breaks if the data is missing?

If a design cannot answer these questions, it is not ready for migration.
