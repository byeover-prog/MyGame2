// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 무기/투사체들이 공통으로 쓰는 "데미지 유틸"을 단일 API로 고정한다.
// - Collider2D 또는 GameObject에서 IDamageable2D(권장) → 없으면 EnemyHealth2D(구현체) 순으로 찾아 데미지를 적용한다.
// - GetRootId는 "동일 적 중복 타격 방지(HashSet)"에 쓰이는 안정 키를 만든다(루트 기준).

public static class DamageUtil2D
{
    // 레이어 마스크 판정(빠르고 명확)
    public static bool IsInLayerMask(int layer, LayerMask mask)
    {
        int bit = 1 << layer;
        return (mask.value & bit) != 0;
    }

    // ─────────────────────────────────────────────
    // RootId: 같은 적 판정용 키
    // ─────────────────────────────────────────────

    // (구버전/호환) int를 받는 코드가 있으면 그대로 통과시키기 위한 오버로드
    public static int GetRootId(int alreadyId) => alreadyId;

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

    // ─────────────────────────────────────────────
    // Damage Apply
    // ─────────────────────────────────────────────

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

        // 2) 프로젝트 구현체 fallback
        var hp = hitGo.GetComponentInParent<EnemyHealth2D>();
        if (hp != null)
        {
            if (!hp.IsDead)
                hp.TakeDamage(damage);
            return true;
        }

        return false;
    }

    // (구버전/호환) 첫 인자가 int로 들어오는 코드가 남아있을 때 컴파일을 살리기 위한 오버로드
    // - 실제로는 hitGo를 못 찾으면 적용 불가. "컴파일 우선" 방어용.
    public static bool TryApplyDamage(int targetRootId, int damage)
    {
        // targetRootId만으로는 대상 GameObject를 역참조할 수 없어서 실패 반환.
        // 이 오버로드가 호출된다면, 호출부가 "Collider2D/GameObject"를 넘기도록 수정하는 게 정답.
        return false;
    }
}