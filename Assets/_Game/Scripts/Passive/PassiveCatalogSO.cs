// ──────────────────────────────────────────────
// PassiveCatalogSO.cs
// 패시브 SO 목록을 한 곳에 모아두는 카탈로그
// (기존 시스템 유지용 — 새 시스템 전환 완료 후 삭제 예정)
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveCatalog",
    menuName = "Game/Passive/PassiveCatalog",
    order = 0)]
public class PassiveCatalogSO : ScriptableObject
{
    [Header("=== 패시브 목록 ===")]
    [Tooltip("등록된 패시브 SO 리스트 (8종)")]
    public List<PassiveConfigSO> passives = new List<PassiveConfigSO>();
}