# Debug Object Validator Spec

## Purpose

This document defines the release-safety rules for debug-only objects, debug hotkeys, runtime probes, and test reset helpers.

This is a validator specification, not a cleanup task. The validator must report and classify problems first. It must not delete scene objects, edit prefabs, or rewrite code automatically.

## Why This Validator Exists

Debug helpers are useful during development, but they become release risks when they remain active in build scenes or runtime controllers.

Current examples include:

- Runtime debug HUD rendered with `OnGUI`.
- UI raycast and button probes in build scenes.
- F9/F10 cooldown reset hotkeys inside runtime ultimate controllers.
- Runtime state hard reset component in a build scene.
- Developer save/load hotkeys in gameplay systems.

These issues are not just performance problems. They can change gameplay state, expose debug behavior to players, or hide real flow/data bugs during testing.

## Build Scene Scope

Current enabled build scenes from `ProjectSettings/EditorBuildSettings.asset`:

| Scene | Enabled |
|---|---:|
| `Assets/Scenes/Scene_Lobby.unity` | Yes |
| `Assets/Scenes/Scene_Boot.unity` | Yes |
| `Assets/Scenes/Scene_Game.unity` | Yes |

The validator must scan enabled build scenes first. Non-build scenes can be reported separately as warnings, but they must not block release unless they are later added to Build Settings.

## Current Confirmed Findings

Evidence level: Confirmed

| Scene/File | Evidence | Release Risk | Severity |
|---|---|---|---|
| `Assets/Scenes/Scene_Boot.unity:3718` | GameObject named `UIRaycastProbe`. | UI click probe can log/debug raycast state in a build scene. | Error |
| `Assets/Scenes/Scene_Boot.unity:3735` | `m_EditorClassIdentifier: Assembly-CSharp::UIRaycastProbe`. | Debug-only probe component in build scene. | Error |
| `Assets/Scenes/Scene_Boot.unity:4278` | `m_EditorClassIdentifier: Assembly-CSharp::UIButtonPointerProbe`. | Debug-only pointer probe component in build scene. | Error |
| `Assets/Scenes/Scene_Game.unity:13730` | GameObject named `DebugRuntimeHUD`. | Runtime debug HUD remains in build scene. | Error |
| `Assets/Scenes/Scene_Game.unity:13762` | `m_EditorClassIdentifier: Assembly-CSharp::DebugRuntimeHUD`. | `OnGUI` debug overlay can render in release. | Error |
| `Assets/Scenes/Scene_Game.unity:13984` | GameObject named `LevelUpRuntimeHardResetOnPlay`. | Runtime hard reset helper remains in build scene. | Error |
| `Assets/Scenes/Scene_Game.unity:14016` | `m_EditorClassIdentifier: Assembly-CSharp::LevelUpRuntimeHardResetOnPlay`. | Reflection-based runtime reset can mutate play state on Awake. | Error |
| `Assets/_Game/Scripts/Ultimate/Ultimatecontroller2d.cs:85` | `Input.GetKeyDown(KeyCode.F9)` resets main ultimate cooldown. | Release hotkey can change combat balance. | Error unless editor/development gated |
| `Assets/_Game/Scripts/Ultimate/Supportultimatecontroller2d.cs:110` | `Input.GetKeyDown(KeyCode.F10)` resets support ultimate cooldown. | Release hotkey can change combat balance. | Error unless editor/development gated |
| `Assets/_Game/Scripts/Debug/DebugRuntimeHUD.cs:36` | Uses `OnGUI`. | Debug overlay rendering path. | Error if scene-referenced in build |

Evidence level: Needs Verification

| File | Evidence | Risk | Severity |
|---|---|---|---|
| `Assets/_Game/Scripts/Skill/WeaponLoadApplier2D.cs:24` | F5/F9 save/load hotkeys when `enableHotkey` is true. | Developer save/load path may be active in release scenes. | Warning or Error if scene-referenced |
| `Assets/_Game/Scripts/Core/Balance/SkillBalanceBootstrap2D.cs:64` | F5 reload in play when `allowReloadInPlay` is true. | Balance reload can be useful in development but should be gated. | Warning unless release scene has it enabled |
| `Assets/_Game/Scripts/UI/ClearUI/ClearUIController.cs:93` | F1 test clear UI is inside `#if UNITY_EDITOR`. | Editor-only compile guard exists, so lower risk. | Info |
| `Assets/Scenes/Scene_HJO.unity`, `Scene_JGM.unity`, `Scene_UI.unity` | DebugRuntimeHUD and LevelUpRuntimeHardResetOnPlay references found. | Not current build scenes, but can become release risk if added. | Warning |

## Validator Rules

### Rule DBG001 - Build Scene Debug Component

| Field | Spec |
|---|---|
| Checks | Enabled build scenes must not contain release-forbidden debug MonoBehaviours. |
| Targets | `.unity` files listed and enabled in `EditorBuildSettings.asset`. |
| Forbidden Types | `DebugRuntimeHUD`, `UIRaycastProbe`, `UIButtonPointerProbe`, `LevelUpRuntimeHardResetOnPlay`. |
| Failure | Error. |
| Why | These are development probes or reset helpers, not gameplay systems. |
| Suggested Fix | Remove from build scene, move to development-only scene, or gate under an explicit development bootstrap. |

### Rule DBG002 - Runtime Debug Hotkey

| Field | Spec |
|---|---|
| Checks | Runtime scripts must not expose release-active debug hotkeys such as F9/F10 reset unless guarded. |
| Targets | Runtime `.cs` files under `Assets/_Game/Scripts`, excluding `Editor` folders. |
| Patterns | `Input.GetKeyDown(KeyCode.F9)`, `Input.GetKeyDown(KeyCode.F10)`, debug reset methods called by runtime input. |
| Allowed If | Wrapped by `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, or guarded by a release-disabled debug settings asset. |
| Failure | Error if unguarded in runtime code. |
| Why | Debug hotkeys can alter combat and progression state in release. |
| Suggested Fix | Move to editor/development guard or explicit debug console available only in development builds. |

### Rule DBG003 - Runtime OnGUI Debug Overlay

| Field | Spec |
|---|---|
| Checks | `OnGUI` debug overlays must not be scene-referenced in enabled build scenes. |
| Targets | Debug MonoBehaviours and build scenes. |
| Failure | Error when scene-referenced in build scene. Warning if script exists but is not scene-referenced. |
| Why | `OnGUI` debug overlays are not release UI and can expose internal state. |
| Suggested Fix | Remove from build scenes or gate object creation by development build. |

### Rule DBG004 - Reflection Runtime Reset Helper

| Field | Spec |
|---|---|
| Checks | Runtime reset helpers that use reflection to clear state must not exist in enabled build scenes. |
| Targets | `LevelUpRuntimeHardResetOnPlay` and similar reset helpers. |
| Failure | Error in build scene. |
| Why | Reflection reset helpers can mutate runtime state silently and hide lifecycle bugs. |
| Suggested Fix | Replace with explicit test setup, editor menu, or development-only bootstrap. |

### Rule DBG005 - Developer Save/Load Hotkeys

| Field | Spec |
|---|---|
| Checks | Components with save/load/reload hotkeys must be disabled or development-gated in release scenes. |
| Targets | `WeaponLoadApplier2D`, `SkillBalanceBootstrap2D`, similar tools. |
| Failure | Warning by default, Error if found active in build scene with hotkey enabled. |
| Why | Save/load or balance reload shortcuts can invalidate player state and test results. |
| Suggested Fix | Disable hotkey fields in release scenes or gate input handling. |

### Rule DBG006 - Non-Build Scene Debug Inventory

| Field | Spec |
|---|---|
| Checks | Non-build scenes with debug components are reported as inventory. |
| Targets | All `.unity` files under `Assets/Scenes`. |
| Failure | Warning. |
| Why | These scenes can become release risks if later added to Build Settings. |
| Suggested Fix | Keep debug scene naming explicit or remove before adding to build. |

## Required Output Format

The future validator should output a deterministic report:

```text
Debug Object Validator
Result: Failed

[ERROR] DBG001 Assets/Scenes/Scene_Game.unity
  Component: DebugRuntimeHUD
  Object: DebugRuntimeHUD
  Reason: Release-forbidden debug HUD exists in enabled build scene.

[ERROR] DBG002 Assets/_Game/Scripts/Ultimate/Ultimatecontroller2d.cs:85
  Pattern: Input.GetKeyDown(KeyCode.F9)
  Reason: Ungated runtime cooldown reset hotkey.

[WARNING] DBG006 Assets/Scenes/Scene_UI.unity
  Component: DebugRuntimeHUD
  Reason: Debug component exists in non-build scene.
```

## Evidence Levels

| Level | Meaning |
|---|---|
| Confirmed | Direct scene, script, or build-settings evidence exists. |
| Likely | Naming and usage strongly indicate debug/test behavior, but scene/build use must be checked. |
| Needs Verification | Script exists or pattern appears, but runtime impact depends on serialized settings or scene placement. |
| Unknown | Insufficient evidence. Do not classify as failure. |

## Implementation Notes

The validator should be implemented as an Editor-only tool first.

Recommended location:

```text
Assets/_Game/Scripts/Editor/Validation/DebugObjectValidator.cs
```

Recommended behavior:

1. Parse `EditorBuildSettings.scenes`.
2. For each enabled scene, scan serialized YAML for forbidden `m_EditorClassIdentifier` values and object names.
3. Scan runtime scripts for forbidden unguarded hotkey patterns.
4. Report errors/warnings in a stable order.
5. Do not mutate files.
6. Exit with a failure result when release-blocking errors exist.

## Release Gate Standard

Release candidate validation fails if any of these are true:

- Enabled build scene contains `DebugRuntimeHUD`.
- Enabled build scene contains `UIRaycastProbe`.
- Enabled build scene contains `UIButtonPointerProbe`.
- Enabled build scene contains `LevelUpRuntimeHardResetOnPlay`.
- Runtime code contains unguarded F9/F10 debug reset hotkeys.
- A developer save/load/reload hotkey component is active in a build scene without development gating.

## Migration Recommendation

Do not delete immediately.

Recommended order:

1. Implement the validator as report-only.
2. Run it and confirm current failures.
3. Add release/development gating for hotkeys.
4. Remove or disable debug scene objects from enabled build scenes.
5. Re-run validator.
6. Only then consider deleting obsolete debug scripts.

This keeps the project safe while preserving current debugging workflows until replacements are agreed.
