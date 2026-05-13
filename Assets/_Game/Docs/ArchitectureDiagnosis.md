# Architecture Diagnosis

## Purpose

This is the Phase 1 diagnosis baseline for `MyGame2`.

It records the current structural state against the confirmed target rules. It is not a refactor plan and it is not permission to delete code. The current goal is to make failure points visible before migration starts.

## Confirmed Target Rules

- Title must branch into Story Mode, Casual Mode, Settings, and Quit.
- Story Mode must expose Continue and New Game.
- Story New Game must flow through Opening Story, Stage 0, Stage 0 checkpoint, Stage 0 clear story, Stage 1, Stage 1 checkpoint, Stage 1 clear, and Story Lobby checkpoint.
- Continue points are save points.
- Story Lobby buttons are talisman shop, talisman management, team management, and next stage.
- Casual Lobby buttons are difficulty, talisman shop, talisman management, team management, map select, and start popup.
- A character starts with 1 basic skill, 2 exclusive level-up skills, 1 ultimate, and 1 unique passive.
- Main character uses R ultimate. Support 1 and Support 2 use T support ultimate behavior.
- Support attributes and exclusive skills must affect the main run.
- Talismans have 6 equip slots and apply to the main character.
- Gacha contains 44 items, price must be data-configurable, and Nyang is the gacha currency.
- Soul is shared Story/Casual meta currency for player/character growth, ultimate upgrades, and passive upgrade choices.
- A run must eventually start from one validated `RunSetup` snapshot.

## Current Architecture Read

The project has working gameplay pieces, but many ownership boundaries are still prototype-shaped.

The most important structural issue is not script count. The issue is that several gameplay truths are spread across scenes, static runtime bridges, save data, local inspector arrays, and legacy systems.

## Primary Ownership Gaps

| Domain | Current Owner Shape | Target Owner Shape | Risk |
|---|---|---|---|
| Game flow | Scene buttons and direct scene names | `GameFlow` route/state owner | Story/Casual/Continue behavior cannot be verified end to end. |
| Run start | `RunConfigHolder`, `StageSelectBridge`, `SquadLoadoutRuntime`, save reads | One `RunSetup` snapshot | Battle scene does not know which choices were validated. |
| Continue | Stage progress cleared/max reached fields | Explicit checkpoint data | Continue cannot restore Stage 0 start, Stage 1 start, or Story Lobby by meaning. |
| Character contract | `CharacterDefinitionSO` plus loose skill assets and scene arrays | Character-centered contract with validator | Adding characters requires searching multiple systems. |
| Exclusive card pool | `LevelUpCardGenerator.characterSkillSets` scene array | Data-owned character skill sets from `RunSetup` | Duplicate character entries can hide later skills. |
| Talisman/gacha | 44 equipment assets, no database asset, legacy shop save path | One talisman/equipment catalog plus gacha service | Items can exist without being drawable, saved, or applied. |
| Currency | Meta Nyang/Soul plus legacy PlayerPrefs Nyang/Spirit and runtime Spirit | One wallet/economy owner | Story/Casual shared currency can drift until legacy paths are retired. |
| Save migration | Root version and first migration skeleton | Versioned migration functions | The baseline exists, but 8-slot to 6-slot and legacy currency migration still need implementation. |
| Debug/release safety | Debug scripts and historical scene refs | Validator-gated release checks | Debug tools can return to build scenes without a guard. |

## Highest Risks

1. Continue is not a first-class saved checkpoint.
2. Run start has no single validated input model.
3. Exclusive skill card pool is scene-owned and can silently ignore skills.
4. Talisman/gacha data exists but is not wired as the combat/save owner.
5. Save compatibility now has a baseline, but 8-slot to 6-slot and legacy currency migration are still unresolved.

## Migration Posture

Do not start by deleting scripts or moving folders.

The safe migration order is:

1. Keep validators report-only.
2. Create missing human-readable docs.
3. Decide official owners for flow, run setup, character contract, skill pool, talisman/gacha, and save.
4. Add target contracts as read models or adapters.
5. Move one runtime owner at a time.
6. Only remove legacy paths after validators and play tests prove replacement behavior.
