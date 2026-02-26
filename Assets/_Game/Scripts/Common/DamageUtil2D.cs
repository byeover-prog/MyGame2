// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 프로젝트 전역에서 "데미지 적용"과 "동일 적 판정용 ID"를 통일한다.
// - 레거시 코드(Targeting2D/Shuriken 등)가 쓰는 함수명(GetRootInstanceId/ApplyDamage 등)을
//   별칭으로 제공해서 연쇄 컴파일 오류를 끊는다.

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

    // 레거시 별칭: Targeting2D/ShurikenProjectile2D가 이 이름을 씀
    public static int GetRootInstanceId(GameObject go) => GetRootId(go);
    public static int GetRootInstanceId(Component c) => GetRootId(c);
    public static int GetRootInstanceId(Collider2D col) => GetRootId(col);

    // --------------------------------------------
    // Damage Apply
    // --------------------------------------------

    // 현재 권장
    public static bool TryApplyDamage(Collider2D hit, int damage)
    {
        if (hit == null) return false;
        return TryApplyDamage(hit.gameObject, damage);
    }

    public static bool TryApplyDamage(GameObject hitGo, int damage)
    {
        if (hitGo == null) return false;
        if (damage <= 0) return false;

        // 1) 권장: 인터페이스 기반
        var dmgable = hitGo.GetComponentInParent<IDamageable2D>();
        if (dmgable != null)
        {
            if (!dmgable.IsDead)
                dmgable.TakeDamage(damage);
            return true;
        }

        // 2) 구현체 fallback
        var hp = hitGo.GetComponentInParent<EnemyHealth2D>();
        if (hp != null)
        {
            if (!hp.IsDead)
                hp.TakeDamage(damage);
            return true;
        }

        return false;
    }

    // 레거시 별칭: ApplyDamage(리턴 없는 버전)로 쓰던 코드 호환
    public static void ApplyDamage(Collider2D hit, int damage)
    {
        TryApplyDamage(hit, damage);
    }

    public static void ApplyDamage(GameObject hitGo, int damage)
    {
        TryApplyDamage(hitGo, damage);
    }
}