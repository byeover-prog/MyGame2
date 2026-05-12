# Update Loop Audit

## Purpose

This document converts the feedback "there are too many Update methods" into an actionable engineering audit.

The goal is not to remove every `Update`. The goal is to make per-frame ownership explicit:

- Keep `Update`/`FixedUpdate` where per-frame or physics behavior is natural.
- Move polling UI to event-driven updates where practical.
- Move repeated weapon cooldown loops toward scheduler/ticker ownership.
- Gate or remove debug-only runtime input before release.
- Avoid broad refactors until each category has a migration path.

## Current Count

Scope: `Assets/_Game/Scripts`

| Method | Count |
|---|---:|
| `Update` | 73 |
| `FixedUpdate` | 15 |
| `LateUpdate` | 7 |
| Total | 95 |

## Distribution

| Folder / Method | Count | Initial Read |
|---|---:|---|
| `Skill` / `Update` | 35 | Main source of repeated cooldown, projectile, area, and weapon behavior. |
| `Skill` / `FixedUpdate` | 12 | Mostly projectile movement. Often legitimate. |
| `UI` / `Update` | 12 | Strong event-driven migration candidate. |
| `Ultimate` / `Update` | 6 | Input, cooldown, visuals, and debug reset are mixed. |
| `Enemy` / `Update` | 4 | Spawning/despawn/timeline checks. Needs domain review. |
| `Core` / `Update` | 4 | Session clock, save autosave, HUD bridge, balance bootstrap. Mixed. |
| `Stage` / `Update` | 2 | Stage timer/clear checks and stage director. Likely central flow candidate. |
| `Vfx` / `Update` | 2 | Contains a good central ticker pattern plus fallback auto-return. |
| `Player` / `Update` | 2 | Input/movement/dash. Mostly legitimate. |
| Other `LateUpdate`/`FixedUpdate` | 20 | Camera, support follow, combat VFX, enemy/player physics, etc. Usually case-by-case. |

## Audit Categories

| Category | Meaning | Default Action |
|---|---|---|
| Keep | Per-frame behavior is natural and local. | Keep until profiling or domain refactor proves otherwise. |
| Event Candidate | Polls state that should change through explicit events. | Replace with events, observer, MVP presenter, or later R3 if justified. |
| Scheduler Candidate | Repeated cooldown/timer loop exists across many similar scripts. | Move to a domain scheduler/ticker after behavior is classified. |
| Debug Gate | Debug-only input, UI, or logging can affect release builds. | Gate behind editor/development build or validator-block release. |
| Flow Candidate | Per-frame logic is actually checking game/state progression. | Move toward GameFlow, StageFlow, or explicit state events. |
| Needs Verification | Not enough evidence for safe classification. | Inspect scene usage and runtime behavior before changing. |

## High-Risk Findings

### 1. Skill Cooldowns Are Spread Across Weapon Scripts

Classification: Scheduler Candidate

Representative evidence:

| File | Evidence | Risk |
|---|---|---|
| `Assets/_Game/Scripts/Skill/Weapons/BalsiWeapon2D.cs:36` | `Update` decrements `cooldownTimer`, finds nearest target, and calls `Fire`. | Every similar skill owns its own cooldown and targeting timing. |
| `Assets/_Game/Scripts/Skill/Weapons/ArrowShotWeapon2D.cs:14` | Same `Update -> cooldown -> target -> Fire` pattern. | New skills repeat timing boilerplate. |
| `Assets/_Game/Scripts/Skill/Weapons/Thundertalismanweapon2d.cs:69` | Same family of common weapon loop. | Common skill model is not fully centralized. |
| `Assets/_Game/Scripts/Skill/CommonSkill/CommonSkillWeapon2D.cs` | Base class owns common stats and cooldown fields, but derived weapons still tick themselves. | Good base exists, but scheduling responsibility is still distributed. |

Decision:

- Do not remove these `Update` methods immediately.
- First classify current skill attack patterns: straight shot, nearest shot, area tick, orbit, homing, bounce, chain, support/ultimate visual, special-case.
- Then move simple cooldown-fired weapons to a shared `SkillCastScheduler` or `AbilityTicker`.
- Keep projectile movement and special active areas local until behavior is stable.

Target direction:

```text
RunSetup
  -> SkillLoadout
  -> RuntimeSkillInstance
  -> SkillCastScheduler
  -> CastBehavior.Cast(context)
```

### 2. HUD Polling Is Duplicated

Classification: Event Candidate

Representative evidence:

| File | Evidence | Risk |
|---|---|---|
| `Assets/_Game/Scripts/Core/Hudconnector.cs:143` | Every frame calls `UpdateHP`, `UpdateTimer`, `UpdatePassiveSlots`. | HUD bridge owns polling for multiple gameplay domains. |
| `Assets/_Game/Scripts/UI/HUD/HUDController.cs:62` | Every frame updates timer, HP, dash, and XP polling. | Timer/HP/XP responsibility overlaps with other HUD scripts. |
| `Assets/_Game/Scripts/UI/HUD/SkillSlotHUDController.cs:22` | Every frame calls `RefreshSlots`. | Skill slot UI should update when loadout changes. |
| `Assets/_Game/Scripts/UI/HUD/PlayerProfileUIController.cs:55` | Polls player HP/profile state. | Another HP/profile path can drift from HUDController/HudConnector. |
| `Assets/_Game/Scripts/UI/HUD/TimerHUD.cs:9` | Separate timer polling. | Multiple timer owners can disagree. |

Decision:

- Treat HUD polling as the first safe architecture cleanup area after debug gating.
- Convert stable state changes to events first: currency, HP, XP, skill slot changes, formation changes.
- Keep timer as a controlled tick or session event. It does not need three separate timer presenters.
- R3 is optional later. Do not introduce R3 until event ownership is clear.

Target direction:

```text
Gameplay Model / Runtime State
  -> Event or Observable State
  -> HUD Presenter
  -> View
```

### 3. Ultimate Input, Cooldown, Execution, UI, And Debug Are Mixed

Classification: Scheduler Candidate + Debug Gate + Input Routing Candidate

Representative evidence:

| File | Evidence | Risk |
|---|---|---|
| `Assets/_Game/Scripts/Ultimate/Ultimatecontroller2d.cs:80` | `Update` decreases cooldown, listens for F9 debug reset, listens for R activation. | Runtime input, cooldown model, and debug hotkey are mixed. |
| `Assets/_Game/Scripts/Ultimate/Supportultimatecontroller2d.cs:105` | `Update` decreases cooldown, listens for F10 debug reset, listens for T activation. | Same issue for support ultimate. |
| `Assets/_Game/Scripts/UI/HUD/UltimateHUDController.cs:94` | Every frame polls cooldown state and paints R/T slots. | UI reads live controller state every frame. Acceptable short-term, but not ideal ownership. |

Decision:

- Keep R/T behavior stable for now.
- Gate F9/F10 release usage first.
- Later separate:
  - input routing,
  - cooldown model,
  - ultimate execution,
  - HUD presentation.

Target direction:

```text
InputRouter
  -> UltimateCommand
  -> UltimateCooldownModel
  -> UltimateExecutor
  -> UltimateHUDPresenter
```

### 4. Debug Runtime Components Must Be Release-Gated

Classification: Debug Gate

Representative evidence:

| File | Evidence | Risk |
|---|---|---|
| `Assets/_Game/Scripts/Debug/DebugRuntimeHUD.cs:30` | `Update` toggles debug HUD with F10 and `OnGUI` renders debug state. | Debug HUD can remain in release scene. |
| `Assets/_Game/Scripts/UI/Debug/UIRaycastProbe.cs:29` | Mouse click logs UI raycast hits. | Useful for debugging, not for release. |
| `Ultimatecontroller2d.cs` and `Supportultimatecontroller2d.cs` | F9/F10 reset cooldown in runtime `Update`. | Release users could trigger debug state if not gated. |

Decision:

- Do not delete immediately.
- Add validator rule first.
- Gate with editor/development build checks or explicit debug setting.

Release rule:

```text
Release build must fail validation if debug-only MonoBehaviours or debug hotkeys are active in build scenes.
```

### 5. Some Update Loops Are Legitimate And Should Not Be Touched First

Classification: Keep

Representative evidence:

| File | Evidence | Reason |
|---|---|---|
| `Assets/_Game/Scripts/Player/PlayerMover2D.cs:181` | Reads input, facing, dash, animation, flip in `Update`; applies movement in `FixedUpdate`. | Standard Unity input/physics split. |
| `Assets/_Game/Scripts/Camera_Scripts/CameraFollow2D.cs:79` | Uses `LateUpdate`. | Camera follow usually belongs in `LateUpdate`. |
| `Assets/_Game/Scripts/Skill/Weapons/ArrowProjectile2D.cs:76` and similar projectile files | Uses `FixedUpdate` for projectile motion. | Physics/movement updates are expected. |
| `Assets/_Game/Scripts/Vfx/Vfxreturnticker.cs:41` | Central ticker updates active VFX auto-return instances. | This is a good example of reducing many per-object updates. |
| `Assets/_Game/Scripts/Vfx/Vfxautoreturn.cs:84` | Fallback `Update` only ticks itself if no central ticker exists. | Acceptable fallback pattern. |

Decision:

- Do not count these as design failures by default.
- Keep until profiling or a broader domain migration gives a concrete reason to change.

### 6. Stage, Session, Save, And Quest Updates Need Ownership Review

Classification: Flow Candidate / Needs Verification

Representative evidence:

| File | Evidence | Risk |
|---|---|---|
| `Assets/_Game/Scripts/Core/Session/SessionGameManager2D.cs:75` | Session time, first spawn delay, boss event timing. | Central session clock is reasonable, but Story/Casual flow ownership still needs GameFlow boundary. |
| `Assets/_Game/Scripts/Core/Save/SaveManager2D.cs:42` | Tracks total play seconds and autosave timer. | Reasonable central service. Could be kept. |
| `Assets/_Game/Scripts/Stage/StageManager.cs:101` | Tracks stage elapsed time, boss warning, boss spawn, clear condition. | Stage flow is centralized here, but target checkpoint/story flow is broader. |
| `Assets/_Game/Scripts/Meta/Quest/QuestManager.cs:55` | Tracks quest offer interval and survival quest progress. | May be acceptable, but should align with session time and pause state. |

Decision:

- Keep short-term.
- Do not split until `GameFlow`, `RunSetup`, and `CheckpointData` are defined.
- Later decide whether these use `SessionClock` or explicit flow events.

## Initial Classification Table

| Area | Classification | Action |
|---|---|---|
| Player movement and physics | Keep | Preserve. Only refactor with input-router work. |
| Projectile movement | Keep | Preserve unless profiling demands central projectile simulation. |
| Camera follow | Keep | Preserve. |
| VFX return ticker | Keep / Good Pattern | Use as reference for future scheduler design. |
| HUD HP/XP/skill/timer polling | Event Candidate | Convert gradually to events/presenters. |
| Weapon cooldown loops | Scheduler Candidate | Migrate simple cooldown-fired weapons after skill pattern classification. |
| Ultimate R/T cooldown/input | Scheduler + Input Candidate | Keep behavior, later split input/cooldown/execution/HUD. |
| Debug HUD/probes/hotkeys | Debug Gate | Validator first, then gate. |
| Session/Stage/Quest timers | Flow Candidate | Keep until GameFlow/RunSetup/checkpoint design is ready. |
| Save autosave timer | Keep | Central service is acceptable. |

## Recommended Migration Order

1. Add Debug Object Validator rules.
2. Add this Update Loop Audit to the architecture docs as a migration baseline.
3. Gate release-forbidden debug hotkeys and debug HUD/probe components.
4. Consolidate duplicate HUD timer/HP/skill-slot ownership.
5. Add state-change events to `PlayerHealth`, `PlayerExp`, `PlayerSkillLoadout`, or their presenter layer.
6. Classify all active and exclusive skills by attack pattern.
7. Build a minimal `SkillCastScheduler` for simple cooldown-fired weapons only.
8. Move one simple weapon family first, then verify behavior before migrating more.
9. Split Ultimate input/cooldown/execution/HUD after the general input and RunSetup direction is stable.
10. Revisit Stage/Session/Quest timing after `GameFlow` and `CheckpointData` are designed.

## Do Not Do

- Do not remove `Update` just because it exists.
- Do not replace every `Update` with coroutines.
- Do not replace every event with R3.
- Do not centralize everything into one giant manager.
- Do not touch projectile/physics loops before there is profiling or a clear system boundary.
- Do not delete debug scripts before validator and build policy exist.

## Acceptance Standard

An `Update` loop is acceptable when it satisfies all of these:

- Its owner is clear.
- It cannot be better represented as a state-change event.
- It is not release-forbidden debug behavior.
- It does not duplicate another component's responsibility.
- It has a clear reason to run per frame or per physics step.

An `Update` loop becomes a migration candidate when any of these are true:

- It polls UI state that changes only occasionally.
- It repeats the same cooldown/timer pattern as many sibling scripts.
- It reads input that should be routed by mode or game state.
- It performs debug-only behavior in runtime builds.
- It hides game flow or checkpoint decisions inside per-frame checks.

## Next Decision

The next architecture decision should be whether to start with:

1. Debug gating first, because it is release-safety work with low gameplay risk.
2. HUD event migration first, because it is human-friendly and removes duplicated UI ownership.
3. Skill scheduler design first, because it directly reduces the largest `Update` cluster and makes future skill additions easier.

Recommended choice: debug gating first, then HUD event migration, then skill scheduler design.
