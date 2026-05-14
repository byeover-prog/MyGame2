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
| 4 | Adds official equipment gacha state for pity tracking. |

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

`MetaWalletService2D` is the official runtime service for both `nyang` and `soul`. UI code that stages or spends these currencies should go through that service or a domain service that uses it.

### Equipment Gacha

```text
MetaProfileSaveData2D.equipmentGacha.pullsSinceEpic
```

This field stores the official gacha pity counter. It is versioned because pity affects paid player progression and must not be reset silently when the gacha UI is connected.

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

Decision:

The active equipped talisman slot count is now 6.

Reason:

The confirmed game rule is 6 equipped talismans. Keeping 8 active slots would keep gameplay, UI, and validators in conflict.

Compatibility rule:

- `CharacterEquipmentSaveData.MaxSlots` is 6.
- Declare `CharacterEquipmentSaveData.TargetTalismanSlots = 6`.
- Old 8-slot saves are normalized during `EnsureDefaults()`.
- Items found in legacy slots 6 and 7 are unequipped, not deleted.
- If an overflow slot contains an item ID that is not represented in owned items, the migration preserves at least one owned count for that item.

Rejected options:

| Option | Behavior | Risk |
|---|---|---|
| Keep hidden legacy slots | Save keeps 8, gameplay reads first 6 only. | Avoids immediate loss, but keeps tools and designers confused. |
| Delete extras | Drop slots 6 and 7. | Not acceptable without explicit user-facing reset policy. |

The current policy is the safe version of "move extras to inventory" using the existing owned item list until the Equipment/Talisman ownership is fully unified.

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
- `SCV041` and `SCV042` should stop failing because the active save slot count is now 6 and legacy overflow slots are normalized.
- `SCV050` should still fail until PlayerPrefs currency is migrated or retired.
