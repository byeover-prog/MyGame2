// UTF-8
// 요약: "스킬 프리팹(무기)"에 붙이는 공용 발사기.
// - OfferService 카탈로그에는 '투사체'가 아니라 이 무기 프리팹이 들어가야 함.
// - projectilePrefab은 반드시 ProjectileBase2D(구현체 포함)를 가지고 있어야 함.

using UnityEngine;

[DisallowMultipleComponent]
public sealed class GenericProjectileWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("식별자(Offer id와 동일)")]
    [SerializeField] private string skillId = "ArrowProjectile";

    [Header("투사체 프리팹(ProjectileBase2D 구현체 포함 필수)")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;
    [SerializeField] private bool requireTargetToFire = true;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 2.0f;
    [SerializeField] private int baseDamage = 10;

    [Header("레벨(임시 규칙)")]
    [Tooltip("레벨당 추가 투사체 수(예: 화살=멀티샷). 0이면 단발")]
    [SerializeField] private int extraProjectilesPerLevel = 0;

    [Tooltip("레벨당 데미지 증가(임시). 0이면 고정")]
    [SerializeField] private int damageAddPerLevel = 0;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private int _level;
    private float _nextFireTime;

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (log) Debug.Log($"[GenericProjectileWeapon2D] OnAttached owner={owner?.name}", this);
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        if (log) Debug.Log($"[GenericProjectileWeapon2D] ApplyLevel {skillId} => Lv.{_level}", this);
    }

    private void Update()
    {
        if (_level <= 0) return;
        if (Time.time < _nextFireTime) return;

        Transform target = FindNearestEnemy();
        if (target == null && requireTargetToFire) return;

        Fire(target);
        _nextFireTime = Time.time + Mathf.Max(0.05f, cooldown);
    }

    private void Fire(Transform target)
    {
        if (projectilePrefab == null || _owner == null) return;

        Vector2 dir;
        if (target != null)
        {
            dir = (Vector2)(target.position - _owner.position);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();
        }
        else
        {
            dir = Vector2.right; // 타겟 없어도 발사 허용일 때 기본 방향
        }

        int dmg = baseDamage + damageAddPerLevel * Mathf.Max(0, _level - 1);

        int shotCount = 1 + extraProjectilesPerLevel * Mathf.Max(0, _level - 1);
        shotCount = Mathf.Clamp(shotCount, 1, 20);

        // 멀티샷은 간단하게 작은 각도 분산
        float spread = (shotCount <= 1) ? 0f : 10f; // 총 벌어짐 10도(임시)
        for (int i = 0; i < shotCount; i++)
        {
            float t = (shotCount == 1) ? 0f : (i / (float)(shotCount - 1)) * 2f - 1f; // -1..+1
            Vector2 d = Rotate(dir, t * (spread * 0.5f));

            var go = Instantiate(projectilePrefab, _owner.position, Quaternion.identity);
            var p = go.GetComponentInChildren<ProjectileBase2D>(true);
            if (p == null)
            {
                Debug.LogWarning($"[GenericProjectileWeapon2D] 투사체 프리팹에 ProjectileBase2D가 없습니다: {projectilePrefab.name}", go);
                Destroy(go);
                return;
            }

            p.Launch(d, dmg, projectileSpeed, projectileLifeSeconds, enemyMask, _owner);
        }
    }

    private Transform FindNearestEnemy()
    {
        if (_owner == null) return null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_owner.position, aimRange, enemyMask);
        float best = float.PositiveInfinity;
        Transform bestT = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == null) continue;

            float d = (t.position - _owner.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        return bestT;
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
}