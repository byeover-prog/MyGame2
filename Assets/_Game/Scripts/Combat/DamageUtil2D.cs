using UnityEngine;

public static class DamageUtil2D
{
    // 레이어 마스크 판정
    public static bool IsInLayerMask(int layer, LayerMask mask)
    {
        int bit = 1 << layer;
        return (mask.value & bit) != 0;
    }

    // --------------------------------------------
    // Root Instance Id (동일 적 판정용 키)
    // --------------------------------------------

    public static int GetRootId(GameObject go)
    {
        if (go == null) return 0;
        return go.transform.root.gameObject.GetInstanceID();
    }

    public static int GetRootId(Component c)
    {
        if (c == null) return 0;
        return c.transform.root.gameObject.GetInstanceID();
    }

    public static int GetRootId(Collider2D col)
    {
        if (col == null) return 0;
        return col.transform.root.gameObject.GetInstanceID();
    }

    // 레거시 별칭
    public static int GetRootInstanceId(GameObject go) => GetRootId(go);
    public static int GetRootInstanceId(Component c) => GetRootId(c);
    public static int GetRootInstanceId(Collider2D col) => GetRootId(col);

    // --------------------------------------------
    // Damage Apply (속성 미지정 → 메인 캐릭터 속성 자동 적용)
    // ★ 공통 스킬들이 이 버전을 사용 → 메인 캐릭터 속성으로 자동 변환
    // --------------------------------------------

    public static bool TryApplyDamage(Collider2D hit, int damage)
        => TryApplyDamage(hit, damage, MainElementProvider.Element);

    public static bool TryApplyDamage(GameObject hitGo, int damage)
        => TryApplyDamage(hitGo, damage, MainElementProvider.Element);

    // --------------------------------------------
    // Damage Apply (속성 포함)
    // --------------------------------------------

    public static bool TryApplyDamage(Collider2D hit, int damage, DamageElement2D element)
    {
        if (hit == null) return false;
        return TryApplyDamageInternal(hit.gameObject, hit, damage, element);
    }

    public static bool TryApplyDamage(GameObject hitGo, int damage, DamageElement2D element)
    {
        return TryApplyDamageInternal(hitGo, null, damage, element);
    }

    private static bool TryApplyDamageInternal(GameObject hitGo, Collider2D hitCol, int damage, DamageElement2D element)
    {
        if (hitGo == null) return false;
        if (damage <= 0) return false;

        bool applied = false;
        
        // 대부분의 적은 콜라이더와 EnemyHealth2D가 같은 오브젝트 또는 루트에 있음
        if (hitGo.TryGetComponent<IDamageable2D>(out var dmgable))
        {
            if (!dmgable.IsDead)
            {
                dmgable.TakeDamage(damage);
                applied = true;
            }
        }
        else
        {
            var dmgableParent = hitGo.GetComponentInParent<IDamageable2D>();
            if (dmgableParent != null)
            {
                if (!dmgableParent.IsDead)
                {
                    dmgableParent.TakeDamage(damage);
                    applied = true;
                }
            }
        }

        if (applied)
        {
            
            Vector3 pos = GetPopupWorldPos(hitGo, hitCol);
            DamageEvents2D.RaiseDamagePopup(pos, damage, element);

            // 속성 피격 이펙트 요청 (ElementVFXObserver2D가 구독)
            DamageEvents2D.RaiseElementHit(hitGo, element);

            // 적 데미지 적용 완료 알림 (흡혈/후처리용)
            DamageEvents2D.RaiseEnemyDamageApplied(hitGo, damage, element);
        }

        return applied;
    }
    
    private static Vector3 GetPopupWorldPos(GameObject hitGo, Collider2D hitCol)
    {
        // 1) 맞은 콜라이더가 있으면 바로 사용 (GetComponentInParent 0회)
        if (hitCol != null)
        {
            var b = hitCol.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        // 2) hitCol이 없을 때만 1회 탐색 (TryGetComponent 우선)
        if (hitGo.TryGetComponent<Collider2D>(out var col))
        {
            var b = col.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        // 3) 최후 폴백: 오브젝트 위치 + 오프셋
        return hitGo.transform.position + Vector3.up * 0.5f;
    }

    // 레거시 별칭: ApplyDamage(리턴 없는 버전) 호환
    public static void ApplyDamage(Collider2D hit, int damage)
    {
        TryApplyDamage(hit, damage, MainElementProvider.Element);
    }

    public static void ApplyDamage(GameObject hitGo, int damage)
    {
        TryApplyDamage(hitGo, damage, MainElementProvider.Element);
    }

    // 속성 포함 별칭(새로 쓰면 좋음)
    public static void ApplyDamage(Collider2D hit, int damage, DamageElement2D element)
    {
        TryApplyDamage(hit, damage, element);
    }

    public static void ApplyDamage(GameObject hitGo, int damage, DamageElement2D element)
    {
        TryApplyDamage(hitGo, damage, element);
    }
}