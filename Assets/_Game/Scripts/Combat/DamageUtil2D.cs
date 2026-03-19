using UnityEngine;

// [구현 원리 요약]
// - 데미지 적용은 DamageUtil2D에서 통일한다(레거시 별칭 포함).
// - 데미지 적용 성공 시 "데미지 팝업 요청 이벤트"를 발생시킨다(전투와 UI 분리).

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

    // 현재 권장
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
    // Damage Apply (기본=물리)
    // --------------------------------------------

    public static bool TryApplyDamage(Collider2D hit, int damage)
        => TryApplyDamage(hit, damage, DamageElement2D.Physical);

    public static bool TryApplyDamage(GameObject hitGo, int damage)
        => TryApplyDamage(hitGo, damage, DamageElement2D.Physical);

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

        // 1) 권장: 인터페이스 기반
        var dmgable = hitGo.GetComponentInParent<IDamageable2D>();
        if (dmgable != null)
        {
            if (!dmgable.IsDead)
            {
                dmgable.TakeDamage(damage);
                applied = true;
            }
        }
        else
        {
            // 2) 구현체 fallback
            var hp = hitGo.GetComponentInParent<EnemyHealth2D>();
            if (hp != null)
            {
                if (!hp.IsDead)
                {
                    hp.TakeDamage(damage);
                    applied = true;
                }
            }
        }

        if (applied)
        {
            Vector3 pos = GetPopupWorldPos(hitGo, hitCol);
            DamageEvents2D.RaiseDamagePopup(pos, damage, element);

            // ★ 속성 피격 이펙트 요청 (ElementVFXObserver2D가 구독)
            DamageEvents2D.RaiseElementHit(hitGo, element);

            // ★ 적 데미지 적용 완료 알림 (흡혈/후처리용)
            DamageEvents2D.RaiseEnemyDamageApplied(hitGo, damage, element);
        }

        return applied;
    }

    private static Vector3 GetPopupWorldPos(GameObject hitGo, Collider2D hitCol)
    {
        // 기본값
        Vector3 pos = hitGo.transform.position + Vector3.up * 0.5f;

        // 1) 맞은 콜라이더가 있으면 그 bounds 최상단
        if (hitCol != null)
        {
            var b = hitCol.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        // 2) 부모에서 Collider2D 탐색
        var col = hitGo.GetComponentInParent<Collider2D>();
        if (col != null)
        {
            var b = col.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        // 3) 부모에서 Renderer 탐색(SpriteRenderer 포함)
        var r = hitGo.GetComponentInParent<Renderer>();
        if (r != null)
        {
            var b = r.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        return pos;
    }

    // 레거시 별칭: ApplyDamage(리턴 없는 버전) 호환
    public static void ApplyDamage(Collider2D hit, int damage)
    {
        TryApplyDamage(hit, damage, DamageElement2D.Physical);
    }

    public static void ApplyDamage(GameObject hitGo, int damage)
    {
        TryApplyDamage(hitGo, damage, DamageElement2D.Physical);
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