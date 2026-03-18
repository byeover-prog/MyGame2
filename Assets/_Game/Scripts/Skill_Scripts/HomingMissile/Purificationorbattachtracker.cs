// UTF-8
// ============================================================================
// PurificationOrbAttachTracker.cs
// 경로: Assets/_Game/Scripts/Combat/PurificationOrb/PurificationOrbAttachTracker.cs
//
// [구현 원리]
// 정화구가 어떤 적에게 몇 개 부착되어 있는지 추적하는 정적 매니저.
//
// [핵심 규칙]
// - 기본: 한 대상당 1개만 부착 가능
// - 유물 확장 시: maxAttachPerTarget 증가 → 동일 대상에 중첩 부착 허용
// - 추가 부착분은 피해량 50% 감소 패널티 (DamageMultiplier로 반환)
//
// [사용 흐름]
// 1. 정화구가 적에게 도달 → CanAttachTo(enemy) 확인
// 2. true이면 → RegisterAttach(enemy)
// 3. 부착 해제 시 → UnregisterAttach(enemy)
// 4. 데미지 계산 시 → GetDamageMultiplier(enemy) 확인
//
// [유물 연동]
// 유물 획득 시: PurificationOrbAttachTracker.MaxAttachPerTarget += 1
// 게임 시작 시: PurificationOrbAttachTracker.ResetAll() 호출
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정화구 부착 상태 전역 추적기.
/// </summary>
public static class PurificationOrbAttachTracker
{
    // ══════════════════════════════════════════════════════════════
    // 설정
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 한 대상에 동시 부착 가능한 최대 수.
    /// 기본값 1. 유물 획득 시 +1 증가.
    /// </summary>
    public static int MaxAttachPerTarget { get; set; } = 1;

    /// <summary>
    /// 추가 부착 시 피해량 배율 (50% 감소 = 0.5).
    /// 첫 번째 부착은 항상 1.0, 두 번째부터 이 값 적용.
    /// </summary>
    public const float EXTRA_ATTACH_DAMAGE_MULTIPLIER = 0.5f;

    // ══════════════════════════════════════════════════════════════
    // 내부 상태
    // ══════════════════════════════════════════════════════════════

    // Key: 적의 InstanceID, Value: 현재 부착된 정화구 수
    private static readonly Dictionary<int, int> _attachCounts = new(32);

    // ══════════════════════════════════════════════════════════════
    // 공개 API
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 해당 적에게 추가 부착이 가능한지 확인합니다.
    /// </summary>
    public static bool CanAttachTo(GameObject enemy)
    {
        if (enemy == null) return false;
        int id = enemy.GetInstanceID();
        _attachCounts.TryGetValue(id, out int count);
        return count < MaxAttachPerTarget;
    }

    /// <summary>
    /// 정화구를 적에게 부착 등록합니다.
    /// 반드시 CanAttachTo 확인 후 호출할 것.
    /// </summary>
    public static void RegisterAttach(GameObject enemy)
    {
        if (enemy == null) return;
        int id = enemy.GetInstanceID();
        _attachCounts.TryGetValue(id, out int count);
        _attachCounts[id] = count + 1;
    }

    /// <summary>
    /// 정화구 부착을 해제합니다 (틱 소진 또는 대상 사망 시).
    /// </summary>
    public static void UnregisterAttach(GameObject enemy)
    {
        if (enemy == null) return;
        int id = enemy.GetInstanceID();
        if (_attachCounts.TryGetValue(id, out int count))
        {
            if (count <= 1)
                _attachCounts.Remove(id);
            else
                _attachCounts[id] = count - 1;
        }
    }

    /// <summary>
    /// 해당 적에 부착된 순서에 따른 데미지 배율을 반환합니다.
    /// 첫 번째 부착 = 1.0, 두 번째부터 = 0.5.
    /// </summary>
    /// <param name="enemy">대상 적</param>
    /// <param name="attachOrder">이 정화구가 해당 적에 몇 번째로 부착되었는지 (0-based)</param>
    public static float GetDamageMultiplier(int attachOrder)
    {
        // 첫 번째(order 0) = 풀 데미지, 이후 = 50% 감소
        return attachOrder <= 0 ? 1f : EXTRA_ATTACH_DAMAGE_MULTIPLIER;
    }

    /// <summary>
    /// 현재 해당 적에 부착된 정화구 수를 반환합니다.
    /// </summary>
    public static int GetAttachCount(GameObject enemy)
    {
        if (enemy == null) return 0;
        _attachCounts.TryGetValue(enemy.GetInstanceID(), out int count);
        return count;
    }

    /// <summary>
    /// 게임 시작 시 또는 스테이지 전환 시 전체 초기화.
    /// MaxAttachPerTarget도 기본값(1)으로 리셋.
    /// </summary>
    public static void ResetAll()
    {
        _attachCounts.Clear();
        MaxAttachPerTarget = 1;
    }

    /// <summary>
    /// 부착 카운트만 초기화 (MaxAttachPerTarget 유지).
    /// 스테이지 내 리스폰 시 사용.
    /// </summary>
    public static void ClearAttachCounts()
    {
        _attachCounts.Clear();
    }
}