using System;
using UnityEngine;

/// <summary>
/// 데미지 관련 공용 이벤트 허브.
/// 전투와 UI/VFX를 분리하기 위한 이벤트 중계 클래스.
/// </summary>
public static class DamageEvents2D
{
    // ═══════════════════════════════════════════════════════
    //  1. 데미지 팝업 요청 (기존)
    // ═══════════════════════════════════════════════════════

    public readonly struct DamagePopupRequest
    {
        public readonly Vector3 WorldPos;
        public readonly int Amount;
        public readonly DamageElement2D Element;

        public DamagePopupRequest(Vector3 worldPos, int amount, DamageElement2D element)
        {
            WorldPos = worldPos;
            Amount = amount;
            Element = element;
        }
    }

    /// <summary>데미지 팝업 UI에서 구독.</summary>
    public static event Action<DamagePopupRequest> OnDamagePopupRequested;

    public static void RaiseDamagePopup(Vector3 worldPos, int amount, DamageElement2D element)
    {
        OnDamagePopupRequested?.Invoke(new DamagePopupRequest(worldPos, amount, element));
    }

    // ═══════════════════════════════════════════════════════
    //  2. 속성 피격 이펙트 요청 (신규 — ElementVFXObserver2D가 구독)
    // ═══════════════════════════════════════════════════════

    public readonly struct ElementHitRequest
    {
        /// <summary>피격 대상 GameObject (적 루트 또는 콜라이더 소유자)</summary>
        public readonly GameObject Target;
        /// <summary>적용된 데미지 속성</summary>
        public readonly DamageElement2D Element;

        public ElementHitRequest(GameObject target, DamageElement2D element)
        {
            Target = target;
            Element = element;
        }
    }

    /// <summary>ElementVFXObserver2D가 구독하여 속성별 부착 이펙트를 처리.</summary>
    public static event Action<ElementHitRequest> OnElementHitRequested;

    public static void RaiseElementHit(GameObject target, DamageElement2D element)
    {
        OnElementHitRequested?.Invoke(new ElementHitRequest(target, element));
    }

    // ═══════════════════════════════════════════════════════
    //  3. 적 데미지 적용 완료 알림 (신규 — 흡혈/후처리용)
    // ═══════════════════════════════════════════════════════

    public readonly struct EnemyDamageAppliedInfo
    {
        /// <summary>피격 대상 GameObject</summary>
        public readonly GameObject Target;
        /// <summary>실제 적용된 데미지량</summary>
        public readonly int Amount;
        /// <summary>적용된 속성</summary>
        public readonly DamageElement2D Element;

        public EnemyDamageAppliedInfo(GameObject target, int amount, DamageElement2D element)
        {
            Target = target;
            Amount = amount;
            Element = element;
        }
    }

    /// <summary>흡혈, 속성 후처리 등에서 구독. 데미지가 실제로 적용된 뒤 발생.</summary>
    public static event Action<EnemyDamageAppliedInfo> OnEnemyDamageApplied;

    public static void RaiseEnemyDamageApplied(GameObject target, int amount, DamageElement2D element)
    {
        OnEnemyDamageApplied?.Invoke(new EnemyDamageAppliedInfo(target, amount, element));
    }
}