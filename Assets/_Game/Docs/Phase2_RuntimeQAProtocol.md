# Phase 2 Runtime QA Protocol

## Purpose

Phase 1 proved the static structure with validators.
Phase 2 starts by proving that the same structure works when a person actually plays through the game in Unity.

This document is a manual QA protocol. Do not treat a validator pass as a substitute for these tests.

## Minimum Smoke Pass

Use this when there is no time for the full table.
This is the current required Phase 2 starting point.

| Step | Action | Must Confirm | Result | Notes |
|---:|---|---|---|---|
| Q-01 | Press Play from the title scene. | No immediate error spam blocks interaction. |  |  |
| Q-02 | Click Story Mode, then New Game. | The game reaches Stage 0 gameplay through the current story/prototype route. |  |  |
| Q-03 | As soon as Stage 0 starts, stop and restart Play Mode, then click Continue. | Continue resumes Stage 0 instead of doing nothing or starting the wrong mode. |  |  |
| Q-04 | Clear Stage 0 or use the current clear/debug route. | Next route goes to the expected story/Stage 1 path, not a random scene. |  |  |
| Q-05 | Click Casual Mode from title. | Casual starts without overwriting the Story Continue checkpoint. |  |  |
| Q-06 | Start one run with any valid squad. | Level-up cards appear without null/missing-card errors. |  |  |

If any row fails, stop there and fix that failure first.
Do not continue into the full protocol until the minimum smoke pass is stable.

## Ground Rules

- Do not change code while running a QA pass.
- Record the exact scene, button, result, and console message when something fails.
- If a failure appears, stop and classify it before fixing it.
- Do not fix by bypassing `RunSetup`, `StoryContinueCheckpointService`, or `SaveManager2D`.
- Every failed row should become either a bug fix, a validator improvement, or a documented design decision.

## Required Preflight

Before starting a pass:

1. Open Unity and wait until compilation finishes.
2. Run every validator under `Tools/Honryeom/Validation`.
3. Confirm all validators return 0 errors, 0 warnings, and 0 info findings.
4. Start Play Mode from the title scene.
5. Keep the Console visible.

## Test Result Labels

| Label | Meaning |
|---|---|
| Pass | Behavior matches the expected result. |
| Fail | Behavior contradicts the expected result. |
| Blocked | Test cannot be completed because a prior step or missing setup prevents it. |
| Needs Design | The implementation works, but the target behavior is not yet sufficiently defined. |

## Story New Game Flow

| Step | Action | Expected Result | Result | Notes |
|---:|---|---|---|---|
| SNG-01 | Start from title with no valid Continue checkpoint. | Story Mode is available. Continue is disabled or unavailable. New Game is available. |  |  |
| SNG-02 | Click Story Mode. | Story submenu opens or Story options become visible. |  |  |
| SNG-03 | Click New Game. | A Story `RunSetup` is created for Stage 0 and the opening story route begins. |  |  |
| SNG-04 | Let opening story finish or use the current prototype route. | Stage 0 gameplay begins. |  |  |
| SNG-05 | When Stage 0 begins, inspect behavior. | Stage 0 start checkpoint is saved. Continue should now resume Stage 0 start. |  |  |
| SNG-06 | Die before clearing Stage 0. | Retry resumes Stage 0 from its saved start checkpoint. |  |  |
| SNG-07 | Clear Stage 0 and press Next. | Flow routes to the next story scene, not directly to final Story Lobby. |  |  |
| SNG-08 | Let the story scene finish or use the current prototype route. | Stage 1 gameplay begins with a Story `RunSetup`. |  |  |
| SNG-09 | When Stage 1 begins, inspect behavior. | Stage 1 start checkpoint is saved. Continue should now resume Stage 1 start. |  |  |
| SNG-10 | Die before clearing Stage 1. | Retry resumes Stage 1 from its saved start checkpoint. |  |  |
| SNG-11 | Clear Stage 1 and press Next. | Flow routes to Story Lobby and saves a Story Lobby checkpoint. |  |  |
| SNG-12 | Exit Play Mode, start again, click Story Mode. | Continue is enabled. |  |  |
| SNG-13 | Click Continue. | Continue loads the Story Lobby checkpoint. |  |  |

## Continue Checkpoint Matrix

Run these as separate small tests. Each checkpoint must be understandable without reading code.

| Checkpoint | How To Create It | Continue Should Load | Result | Notes |
|---|---|---|---|---|
| None | Fresh save or reset save. | Continue disabled or ignored safely. |  |  |
| Stage 0 Start | Start Story New Game and enter Stage 0. | Stage 0 start. |  |  |
| Stage 1 Start | Clear Stage 0, transition story, enter Stage 1. | Stage 1 start. |  |  |
| Story Lobby | Clear Stage 1 and enter Story Lobby. | Story Lobby. |  |  |

## Casual Flow

| Step | Action | Expected Result | Result | Notes |
|---:|---|---|---|---|
| CAS-01 | Start from title. | Casual Mode is available. |  |  |
| CAS-02 | Click Casual Mode. | A Casual `RunSetup` is created. |  |  |
| CAS-03 | Enter Casual lobby or current prototype route. | Casual flow does not overwrite Story Continue checkpoint. |  |  |
| CAS-04 | Start a Casual run. | Run mode is Casual, stage/map target is valid, squad and talisman snapshot are present. |  |  |
| CAS-05 | Clear or exit Casual run. | Return route is Casual lobby/title route, not Story Stage 0/1 route. |  |  |

## Squad And Card Pool Runtime Checks

| Step | Action | Expected Result | Result | Notes |
|---:|---|---|---|---|
| SQD-01 | Select a main character and two support characters. | Start is blocked if no main character exists. |  |  |
| SQD-02 | Start a run. | `RunSetup` includes main, support1, support2. |  |  |
| SQD-03 | Level up during gameplay. | Main exclusive skills and support exclusive skills can appear according to current card-pool rules. |  |  |
| SQD-04 | Use main ultimate. | Main ultimate uses the main character contract. |  |  |
| SQD-05 | Use support ultimate. | Support ultimate uses support character data. |  |  |

## Talisman And Economy Runtime Checks

| Step | Action | Expected Result | Result | Notes |
|---:|---|---|---|---|
| TAL-01 | Equip 6 talismans on the main character. | Exactly 6 slots are usable. |  |  |
| TAL-02 | Start a run. | Equipped talisman effects apply to the main character only. |  |  |
| TAL-03 | Perform a single gacha pull. | Nyang decreases by `GachaConfigSO.singlePullCost`. |  |  |
| TAL-04 | Perform a ten pull. | Nyang decreases by `GachaConfigSO.tenPullCost`. |  |  |
| TAL-05 | Pull a duplicate item. | Duplicate refund is applied according to rarity. |  |  |
| TAL-06 | Restart Play Mode. | Nyang, Soul, equipment ownership, and equipped talismans persist. |  |  |

## Failure Report Template

Use this format when a step fails:

```text
Test ID:
Expected:
Actual:
Scene:
Button or action:
Console message:
Save state, if relevant:
Screenshot or recording:
Initial guess:
Classification: Bug / Missing wiring / Design gap / Validator gap
```

## Full Pass Criteria For Phase 2 Step 1

Full Phase 2 QA is complete only when:

- Story New Game has been played through Stage 0 and Stage 1.
- Continue has been tested at None, Stage 0, Stage 1, and Story Lobby states.
- Casual Mode has been started without corrupting Story Continue.
- At least one squad/card-pool run has been checked.
- At least one talisman/gacha persistence pass has been checked.
- Any failure has been recorded before code is changed.
