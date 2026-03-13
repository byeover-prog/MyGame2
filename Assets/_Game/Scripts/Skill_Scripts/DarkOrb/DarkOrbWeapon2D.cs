// UTF-8
// 다크오브 무기: 메인 오브(DarkOrbProjectile2D)만 발사한다.
// SplitProjectile은 절대 무기가 직접 발사하지 않는다.

using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체(메인 오브) 프리팹")]
    [Tooltip("여기엔 반드시 DarkOrbProjectile2D 프리팹을 넣어야 합니다. (Split 프리팹 금지)")]
    [SerializeField] private DarkOrbProjectile2D projectilePrefab;

    [Header("분열(옵션)")]
    [Tooltip("분열체를 풀로 찍고 싶을 때만 넣으세요. 없으면 Instantiate 기반으로만 동작합니다.")]
    [SerializeField] private ProjectilePool2D splitPool;

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발")]
    [Tooltip("폭발 반경")]
    [SerializeField] private float explosionRadius = 1.0f;

    [Header("데미지")]
    [Tooltip("1~4레벨 기본 폭발 데미지")]
    [SerializeField] private int baseExplosionDamage = 12;

    [Tooltip("5레벨부터 레벨당 추가 폭발 데미지 (5~8만 적용)")]
    [SerializeField] private int bonusDamagePerLevelFrom5 = 6;

    [Header("분열 규칙")]
    [Tooltip("레벨에 따른 분열 수(간단 규칙).\nLv1=0, Lv2=2, Lv3=4, Lv4+=8")]
    [SerializeField] private bool useSimpleSplitRule = true;

    [Tooltip("분열체 속도")]
    [SerializeField] private float splitSpeed = 10f;

    [Tooltip("분열체 수명")]
    [SerializeField] private float splitLifeSeconds = 0.6f;

    [Tooltip("분열체 데미지(0이면 메인 데미지와 동일)")]
    [SerializeField] private int splitDamage = 0;

    [Header("표현")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private float _nextFireTime;
    private int _level;
    private PlayerCombatStats2D _stats;

    // 인터페이스 오타 대응(필수)
    public void OnAttaced(Transform owner) => OnAttached(owner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (_owner != null)
        {
            _stats = _owner.GetComponent<PlayerCombatStats2D>();
            if (_stats == null) _stats = _owner.GetComponentInParent<PlayerCombatStats2D>();
        }
        if (log) Debug.Log($"[DarkOrbWeapon2D] OnAttached owner={owner?.name}", this);
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        if (log) Debug.Log($"[DarkOrbWeapon2D] ApplyLevel => Lv.{_level}", this);
    }

    private void Update()
    {
        if (_level <= 0) return;
        if (_owner == null) return;
        if (Time.time < _nextFireTime) return;

        if (projectilePrefab == null)
        {
            Debug.LogError("[DarkOrbWeapon2D] projectilePrefab이 비었습니다. (메인 오브 프리팹을 넣으세요)", this);
            _nextFireTime = Time.time + 0.5f;
            return;
        }

        // Split 프리팹 금지(정책)
        if (projectilePrefab.GetComponent<DarkOrbSplitProjectile2D>() != null)
        {
            Debug.LogError("[DarkOrbWeapon2D] projectilePrefab에 DarkOrbSplitProjectile2D가 들어가 있습니다. 메인 오브(DarkOrbProjectile2D)를 넣어야 합니다.", projectilePrefab);
            enabled = false;
            return;
        }

        Transform t = FindNearestEnemy();
        if (t == null)
        {
            _nextFireTime = Time.time + 0.05f;
            return;
        }

        Fire(t);

        // ★ 캐릭터 + 패시브 쿨다운 배율 반영
        float finalCd = cooldown;
        if (_stats != null)
            finalCd *= Mathf.Max(0.05f, _stats.CooldownMul);
        _nextFireTime = Time.time + Mathf.Max(0.05f, finalCd);
    }

    private void Fire(Transform target)
    {
        Vector2 dir = (Vector2)(target.position - _owner.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // ── 폭발 데미지 규칙 ──
        float dmgF = Mathf.Max(1f, baseExplosionDamage);
        if (_level >= 5)
        {
            int bonusLevel = _level - 4; // 5→1, 8→4
            dmgF += bonusDamagePerLevelFrom5 * bonusLevel;
        }

        // ★ 캐릭터 + 패시브 공격력 배율 반영
        if (_stats != null)
            dmgF *= (_stats.DamageMul * _stats.ElementDamageMul);

        int dmg = Mathf.Max(1, Mathf.RoundToInt(dmgF));

        // enemyMask 0이면 Enemy 레이어로 보정
        if (enemyMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) enemyMask = LayerMask.GetMask("Enemy");
        }

        // 간단 분열 규칙(요청 스펙에 맞춰 여기만 바꾸면 됨)
        int splitCount = 0;
        if (useSimpleSplitRule)
        {
            // Lv1=0, Lv2=2, Lv3=4, Lv4+=8
            if (_level <= 1) splitCount = 0;
            else if (_level == 2) splitCount = 2;
            else if (_level == 3) splitCount = 4;
            else splitCount = 8;
        }

        int childDmg = (splitDamage > 0) ? splitDamage : dmg;

        var proj = Instantiate(projectilePrefab, _owner.position, Quaternion.identity);

        // Init 기반으로 통일: DarkOrbProjectile2D가 Init을 갖고 있어야 함
        proj.Init(
            enemyMask,
            dmg,
            projectileSpeed,
            Mathf.Max(0.2f, projectileLifeSeconds),
            dir,
            Mathf.Max(0.1f, explosionRadius),
            Mathf.Max(0, splitCount),
            Mathf.Max(0.1f, splitSpeed),
            Mathf.Max(0.1f, splitLifeSeconds),
            Mathf.Max(0, childDmg),
            splitPool,
            orbAlpha
        );

        if (log) Debug.Log($"[DarkOrbWeapon2D] Fire => dmg={dmg} splitCount={splitCount}", this);
    }

    private Transform FindNearestEnemy()
    {
        if (enemyMask.value == 0) return null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_owner.position, aimRange, enemyMask);
        if (hits == null || hits.Length == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;

        Vector2 o = _owner.position;
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;

            float d = ((Vector2)h.transform.position - o).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = h.transform;
            }
        }

        return bestT;
    }
}