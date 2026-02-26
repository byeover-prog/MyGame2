// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨타임마다 "가장 가까운 적"을 찾고, DarkOrbProjectile2D를 발사한다.
// - DarkOrbProjectile2D는 반드시 Init(...)를 가진다(풀/Instantiate 모두 호환).
// - 이 스크립트는 Skill_Scripts 쪽 “구버전/별도 루트”가 살아있을 때 컴파일을 살리기 위한 런타임 발사기다.

[DisallowMultipleComponent]
public sealed class DarkOrbSkill2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체 프리팹(메인 오브)")]
    [Tooltip("반드시 DarkOrbProjectile2D가 붙은 프리팹을 넣으세요.")]
    [SerializeField] private DarkOrbProjectile2D projectilePrefab;

    [Header("옵션(분열용 풀)")]
    [Tooltip("분열체 풀(없어도 동작은 함: splitCount=0 권장)")]
    [SerializeField] private ProjectilePool2D splitPool;

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발/분열")]
    [SerializeField] private float explosionRadius = 1.0f;
    [SerializeField] private int splitCount = 0;
    [SerializeField] private float splitSpeed = 10f;
    [SerializeField] private float splitLifeSeconds = 0.6f;
    [SerializeField] private int splitDamage = 6;

    [Header("표현")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    [Header("데미지")]
    [SerializeField] private int damage = 12;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private int _level;
    private float _nextFireTime;

    // 인터페이스 오타(OnAttaced) 호환
    public void OnAttaced(Transform owner) => OnAttached(owner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (log) Debug.Log($"[DarkOrbSkill2D] OnAttached owner={owner?.name}", this);
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        if (log) Debug.Log($"[DarkOrbSkill2D] ApplyLevel => Lv.{_level}", this);
    }

    private void Update()
    {
        if (_level <= 0) return;
        if (_owner == null) return;
        if (Time.time < _nextFireTime) return;

        var target = FindNearestEnemy();
        if (target == null)
        {
            _nextFireTime = Time.time + 0.05f;
            return;
        }

        Fire(target);
        _nextFireTime = Time.time + Mathf.Max(0.05f, cooldown);
    }

    private void Fire(Transform target)
    {
        if (projectilePrefab == null) return;

        Vector2 origin = _owner.position;
        Vector2 dir = (Vector2)(target.position - _owner.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 레벨에 따라 분열을 켜고 싶으면 여기서 조절
        int lv = Mathf.Clamp(_level, 1, 8);

        int dmg = damage;
        int splitN = splitCount;

        // 예시: 레벨이 오르면 splitCount 증가시키고 싶으면 여기서만 변경
        // splitN = Mathf.Max(splitCount, lv - 1);

        var orb = Instantiate(projectilePrefab, origin, Quaternion.identity);
        orb.Init(
            enemyMask,
            dmg,
            projectileSpeed,
            projectileLifeSeconds,
            dir,
            Mathf.Max(0.1f, explosionRadius),
            Mathf.Max(0, splitN),
            Mathf.Max(0.1f, splitSpeed),
            Mathf.Max(0.1f, splitLifeSeconds),
            Mathf.Max(0, splitDamage),
            splitPool,
            orbAlpha
        );
    }

    private Transform FindNearestEnemy()
    {
        if (enemyMask.value == 0) return null;

        var hits = Physics2D.OverlapCircleAll(_owner.position, aimRange, enemyMask);
        if (hits == null || hits.Length == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;

        Vector2 o = _owner.position;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;

            float d = ((Vector2)c.transform.position - o).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        return bestT;
    }
}