# Phase 1 Validator Spec

## Purpose

This document defines the Phase 1 validator suite for release-oriented architecture diagnosis.

The validators are report-only. They must not delete assets, rewrite scenes, or migrate save data automatically.

## Validator Suite

| Validator | Menu | Purpose | Release Risk Covered |
|---|---|---|---|
| Build Scene Validator | `Tools/Honryeom/Validation/Build Scene Validator` | Checks enabled build scene baseline. | Wrong entry scene or missing core scenes. |
| Debug Object Validator | `Tools/Honryeom/Validation/Debug Object Validator` | Finds release-risk debug objects and hotkeys. | Debug UI, probes, or reset helpers in release. |
| Asset Integrity Validator | `Tools/Honryeom/Validation/Asset Integrity Validator` | Finds missing scripts and broken serialized assets. | Broken SO/scene/prefab references. |
| Story Flow Validator | `Tools/Honryeom/Validation/Story Flow Validator` | Checks title and Story/Casual route signals. | Generic Start flow replacing target mode flow. |
| Continue Checkpoint Validator | `Tools/Honryeom/Validation/Continue Checkpoint Validator` | Checks explicit continue checkpoint ownership. | Continue cannot resume the correct point. |
| Run Setup Validator | `Tools/Honryeom/Validation/Run Setup Validator` | Checks whether battle starts from one validated snapshot. | Battle reads scattered scene/static/save state. |
| Character Squad Validator | `Tools/Honryeom/Validation/Character Squad Validator` | Checks character catalog, squad, basic/ultimate/exclusive skill contract. | New characters are hard to add and easy to miswire. |
| Skill Pool Validator | `Tools/Honryeom/Validation/Skill Pool Validator` | Checks common/exclusive card pool integrity and support skill inclusion. | Cards can be missing, duplicated, or ignored. |
| Talisman Gacha Validator | `Tools/Honryeom/Validation/Talisman Gacha Validator` | Checks 44 items, 6 slots, gacha config, and shop/equipment ownership. | Gacha data exists but is not drawable, saved, or applied. |
| Save Compatibility Validator | `Tools/Honryeom/Validation/Save Compatibility Validator` | Checks save versioning, migration risk, currencies, and multiple stores. | Future migrations can corrupt or silently rewrite saves. |
| Document Code Consistency Validator | `Tools/Honryeom/Validation/Document Code Consistency Validator` | Checks docs against confirmed rules and code evidence. | Docs become stale and collaborators follow the wrong source. |

## Severity Rules

| Severity | Meaning |
|---|---|
| Error | Blocks release or blocks a confirmed Phase 1 target contract. |
| Warning | Risk exists but may be acceptable during migration or non-build scene use. |
| Info | Useful inventory only. |

## Evidence Rules

Validators should prefer direct evidence from:

- `ProjectSettings/EditorBuildSettings.asset`
- build scene YAML
- ScriptableObject assets
- runtime scripts
- save data classes
- Phase 1 docs

Avoid speculative deletion language. If a validator cannot prove impact, report `Warning` or leave the issue to manual review.

## Expected Current State

At the end of Phase 1 diagnosis, many validators are expected to fail. That is acceptable.

The goal is not to make every validator pass immediately. The goal is to make each failure concrete enough to plan a safe migration.

## Migration Rule

Before changing runtime behavior, each migration step must answer:

- Which validator failure does this address?
- What existing behavior must remain unchanged?
- What save data may be affected?
- What Unity scene or SO setup must the user perform?
- What validator result should improve after the change?

## Save Migration Baseline

The first save migration baseline is documented in:

```text
Assets/_Game/Docs/SaveMigrationSpec.md
```

This spec owns the policy for root save versioning, official Soul storage, Story Continue checkpoint fields, and the delayed 8-slot to 6-slot talisman migration.
