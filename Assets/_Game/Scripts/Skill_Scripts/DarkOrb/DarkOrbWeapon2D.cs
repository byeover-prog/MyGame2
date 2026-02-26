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

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발(거리 도달 후)")]
    [Tooltip("폭발 반경")]
    [SerializeField] private float explosionRadius = 1.0f;

    [Header("데미지")]
    [Tooltip("1~4레벨 기본 폭발 데미지")]
    [SerializeField] private int baseExplosionDamage = 12;

    [Tooltip("5레벨부터 레벨당 추가 폭발 데미지 (5~8만 적용)")]
    [SerializeField] private int bonusDamagePerLevelFrom5 = 6;

    [Header("분열/성능")]
    [Tooltip("한 발에서 생성될 수 있는 총 파편 수 상한(budget)")]
    [SerializeField] private int maxFragmentsBudget = 64;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private float _nextFireTime;
    private int _level;

    public void OnAttached(Transform owner)
    {
        _owner = owner;
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
        _nextFireTime = Time.time + Mathf.Max(0.05f, cooldown);
    }

    private void Fire(Transform target)
    {
        Vector2 dir = (Vector2)(target.position - _owner.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 레벨 규칙:
        // Lv1: 0회 / Lv2: 1회 / Lv3: 2회 / Lv4~8: 3회
        int splitDepth =
            (_level <= 1) ? 0 :
            (_level == 2) ? 1 :
            (_level == 3) ? 2 :
            3;

        // 폭발 데미지 규칙:
        // 1~4: baseExplosionDamage
        // 5~8: 레벨당 추가(bonusDamagePerLevelFrom5)
        int dmg = Mathf.Max(1, baseExplosionDamage);
        if (_level >= 5)
        {
            int bonusLevel = _level - 4; // 5->1, 8->4
            dmg += bonusDamagePerLevelFrom5 * bonusLevel;
        }

        // enemyMask 0이면 Enemy 레이어로 보정
        if (enemyMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) enemyMask = LayerMask.GetMask("Enemy");
        }

        var proj = Instantiate(projectilePrefab, _owner.position, Quaternion.identity);

        // Configure는 "분열 깊이, budget, 폭발 반경"만 전달
        proj.Configure(splitDepth, maxFragmentsBudget, explosionRadius);

        // Launch: dir, 폭발데미지, speed, life, enemyMask, owner
        proj.Launch(
            dir,
            dmg,
            projectileSpeed,
            Mathf.Max(0.2f, projectileLifeSeconds),
            enemyMask,
            _owner
        );

        if (log) Debug.Log($"[DarkOrbWeapon2D] Fire => dir={dir} dmg={dmg} splitDepth={splitDepth}", this);
    }

    private Transform FindNearestEnemy()
    {
        if (enemyMask.value == 0) return null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_owner.position, aimRange, enemyMask);
        if (hits == null || hits.Length == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;

            float d = ((Vector2)h.transform.position - (Vector2)_owner.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = h.transform;
            }
        }

        return bestT;
    }
}