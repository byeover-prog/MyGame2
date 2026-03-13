// ──────────────────────────────────────────────
// PassiveCatalogSO.cs
// ★ SkillDefinitionSO를 직접 받도록 변경
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Skills;

[CreateAssetMenu(
    fileName = "PassiveCatalog",
    menuName = "Game/Passive/PassiveCatalog",
    order = 0)]
public class PassiveCatalogSO : ScriptableObject
{
    [Header("=== 패시브 목록 ===")]
    [Tooltip("등록된 패시브 SkillDefinitionSO 리스트 (8종)")]
    public List<SkillDefinitionSO> passives = new List<SkillDefinitionSO>();
}