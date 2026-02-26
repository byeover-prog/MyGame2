// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 풀 제약(ProjectilePool2D.Get<T>)을 완전히 피한다.
// - projectilePrefab(GameObject)을 Instantiate 후 HomingMissileProjectile2D를 찾아 Init 호출.
// - startTarget(Transform)을 항상 넘겨서 CS7036을 없앤다.

[DisallowMultipleComponent]
public sealed class HomingMissileWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체 프리팹")]
    [Tooltip("HomingMissileProjectile2D가 붙은 프리팹(GameObject)")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("발사 위치(비우면 owner)")]
    [SerializeField] private Transform spawnPoint;

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;
    [SerializeField] private bool requireTargetToFire = true;

    [Header("스탯")]
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifeSeconds = 2.0f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float turnSpeedDeg = 720f;

    [Header("체인(=추가 타격 횟수)")]
    [Tooltip("0이면 단발, 1이면 1번 더 타겟 변경 가능")]
    [SerializeField] private int chainCount = 0;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private int _level;
    private float _nextFireTime;

    public void OnAttaced(Transform owner) => OnAttached(owner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (spawnPoint == null) spawnPoint = transform;
        if (log) Debug.Log($"[HomingMissileWeapon2D] OnAttached owner={owner?.name}", this);
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        if (log) Debug.Log($"[HomingMissileWeapon2D] ApplyLevel => Lv.{_level}", this);
    }

    private void Update()
    {
        if (_level <= 0) return;
        if (_owner == null) return;
        if (Time.time < _nextFireTime) return;

        Transform target = FindFarthestEnemy();
        if (target == null && requireTargetToFire) return;

        Fire(target);
        _nextFireTime = Time.time + Mathf.Max(0.05f, cooldown);
    }

    private void Fire(Transform target)
    {
        if (projectilePrefab == null) return;

        Vector2 origin = (spawnPoint != null) ? (Vector2)spawnPoint.position : (Vector2)_owner.position;

        Vector2 dir;
        if (target != null)
        {
            dir = (Vector2)target.position - origin;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();
        }
        else
        {
            dir = Vector2.right;
        }

        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);

        // 루트/자식 어디 붙어있든 찾기
        var proj = go.GetComponent<HomingMissileProjectile2D>();
        if (proj == null) proj = go.GetComponentInChildren<HomingMissileProjectile2D>(true);

        if (proj == null)
        {
            Debug.LogError("[HomingMissileWeapon2D] projectilePrefab에 HomingMissileProjectile2D가 없습니다.", go);
            Destroy(go);
            return;
        }

        // 여기서 startTarget을 반드시 넘긴다(에러 CS7036 방지)
        proj.Init(
            enemyMask,
            aimRange,
            damage,
            projectileSpeed,
            turnSpeedDeg,
            Mathf.Max(0, chainCount),
            projectileLifeSeconds,
            dir,
            target
        );
    }

    private Transform FindFarthestEnemy()
    {
        if (enemyMask.value == 0) return null;

        var hits = Physics2D.OverlapCircleAll(_owner.position, aimRange, enemyMask);
        if (hits == null || hits.Length == 0) return null;

        float best = -1f;
        Transform bestT = null;

        Vector2 o = _owner.position;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;

            float d = ((Vector2)c.transform.position - o).sqrMagnitude;
            if (d > best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        return bestT;
    }
}