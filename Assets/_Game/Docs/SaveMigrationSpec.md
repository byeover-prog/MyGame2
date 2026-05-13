# Save Migration Spec

## Purpose

This document defines the first safe save migration direction for `MyGame2`.

The current goal is not to rewrite every old system immediately. The goal is to make future save changes explicit, versioned, and reversible enough to support release and long-term updates.

## Current Save Root

Official save file:

```text
player_save.json
```

Official root data type:

```text
PlayerSaveData2D
```

Current target version after this migration baseline:

```text
PlayerSaveData2D.CurrentVersion = 3
```

## Version Policy

| Version | Meaning |
|---|---|
| 1 or missing | Legacy/default shape before the current meta profile became stable. |
| 2 | Existing save baseline before Soul and Continue checkpoint fields. |
| 3 | Adds official `soul` currency and `StageProgressSaveData.continueCheckpoint`. |

All loaded saves must pass through:

```text
PlayerSaveData2D.EnsureDefaults()
  -> MigrateToCurrentVersion()
  -> nested EnsureDefaults()
```

No migration may delete player-owned data without an explicit policy written here first.

If a save is from a future version, older code must not downgrade its version number.

## Added In Version 3

### Soul

Field:

```text
MetaProfileSaveData2D.soul
```

Default for old saves:

```text
0
```

Reason:

Soul is the confirmed shared Story/Casual meta currency for growth, ultimate upgrade choices, and passive upgrade choices.

### Story Continue Checkpoint

Field:

```text
StageProgressSaveData.continueCheckpoint
```

Default for old saves:

```text
StoryContinueCheckpointKind.None
stageIndex = -1
sceneName = ""
storyId = ""
```

Reason:

Continue points are save points. The confirmed first-story flow needs separate saved meanings for:

- Stage 0 start
- Stage 1 start
- Story Lobby entry

## 8 Slots To 6 Slots Policy

Confirmed design target:

```text
6 equipped talismans
```

Current serialized capacity:

```text
8 legacy slots
```

Decision for this step:

Do not shrink saved slot data yet.

Reason:

There may already be test or future player saves with items in slots 6 and 7. Removing those slots now would either lose data or require an ownership policy that has not been implemented yet.

Temporary rule:

- Keep `CharacterEquipmentSaveData.MaxSlots` at 8 for save compatibility.
- Declare `CharacterEquipmentSaveData.TargetTalismanSlots = 6`.
- UI and future RunSetup should eventually use the target 6 slots.
- Migration must later decide what happens to items in legacy slots 6 and 7.

Final migration options:

| Option | Behavior | Risk |
|---|---|---|
| Move extras to inventory | Unequip slots 6 and 7, keep owned counts. | Safest for players, requires official inventory owner. |
| Keep hidden legacy slots | Save keeps 8, gameplay reads first 6 only. | Avoids data loss, but can confuse tools. |
| Delete extras | Drop slots 6 and 7. | Not acceptable without explicit user-facing reset policy. |

Recommended future decision:

Move extras to inventory after Equipment/Talisman ownership is unified.

## Known Legacy Save Stores

| Store | Current Owner | Policy |
|---|---|---|
| `player_save.json` | `SaveManager2D` | Official release save root. |
| PlayerPrefs currency | `CurrencyManager` | Legacy/prototype. Must be migrated or retired before release. |
| `weapon_save.json` | `WeaponSaveSystem` | Needs verification. Keep until weapon ownership is decided. |
| Settings PlayerPrefs | `GameSettingsRuntime` | Acceptable separate settings store. |

## Migration Rules

1. Add fields with safe defaults before routing gameplay to them.
2. Never shrink lists in the same step that introduces the migration framework.
3. Do not import PlayerPrefs currency automatically until duplicate currency ownership is decided.
4. Do not delete `weapon_save.json` until weapon ownership is classified.
5. Validators should be allowed to keep failing for known incomplete migrations.

## Expected Validator Impact

After this baseline:

- `SCV011` should stop failing because a migration path exists.
- `SCV022` should stop failing because `soul` exists.
- `SCV031` should stop failing because `continueCheckpoint` exists.
- `SCV041` and `SCV042` should still fail until the 8->6 slot migration is implemented.
- `SCV050` should still fail until PlayerPrefs currency is migrated or retired.
