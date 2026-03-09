using UnityEngine;

// [구현 원리 요약]
// - Owned + Equipped일 때만 쿨마다 발사한다.
// - 발사 순간 1회 "가장 가까운 적"을 찾는다(없으면 발사 안 함).
// - 레벨은 "총 타격 수"로 사용한다. (1레벨=1회, 8레벨=8회)
//   => 프로젝타일 Init의 chainCount는 '추가 체인 수'라서 (총타격-1)로 넘긴다.
[DisallowMultipleComponent]
public sealed class HomingMissileSkill2D : MonoBehaviour, ISkill2D
{
    [Header("Prefab")]
    [Tooltip("호밍 미사일 투사체 프리팹(HomingMissileProjectile2D가 붙어 있어야 함)")]
    [SerializeField] private HomingMissileProjectile2D projectilePrefab;

    [Tooltip("발사 위치(비우면 자기 위치). 권장: Player/SpawnPoint")]
    [SerializeField] private Transform firePoint;

    [Header("Target")]
    [Tooltip("적 레이어 마스크(Enemy 레이어를 넣어주세요)")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("타겟 탐색 반경")]
    [Min(0.1f)]
    [SerializeField] private float searchRadius = 18f;

    [Header("Stats(기본값)")]
    [Tooltip("쿨타임(초)")]
    [Min(0.05f)]
    [SerializeField] private float cooldown = 3.0f;

    [Tooltip("1회 타격 데미지")]
    [Min(0)]
    [SerializeField] private int damage = 10;

    [Tooltip("투사체 속도")]
    [Min(0.1f)]
    [SerializeField] private float projectileSpeed = 8.5f;

    [Tooltip("유도 회전 속도(도/초)")]
    [Min(1f)]
    [SerializeField] private float turnSpeedDeg = 1200f;

    [Tooltip("최대 생존 시간(초)")]
    [Min(0.1f)]
    [SerializeField] private float lifeTime = 7f;

    [Header("획득/장착(중요)")]
    [SerializeField] private bool startOwned = false;
    [SerializeField] private bool startEquipped = false;

    [Header("Level")]
    [Range(1, 8)]
    [SerializeField] private int level = 1;

    [Header("Debug")]
    [Tooltip("발사가 안 될 때 원인을 로그로 표시(테스트용)")]
    [SerializeField] private bool debugLog = false;

    private readonly Collider2D[] _hits = new Collider2D[32];

    private bool _owned;
    private bool _equipped;
    private float _t;

    public bool IsOwned => _owned;
    public bool IsEquipped => _equipped;
    public int Level => level;

    private void Awake()
    {
        _owned = startOwned;
        _equipped = startEquipped;
        if (_equipped) _owned = true;
    }

    private void OnValidate()
    {
        searchRadius = Mathf.Max(0.1f, searchRadius);
        cooldown = Mathf.Max(0.05f, cooldown);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        turnSpeedDeg = Mathf.Max(1f, turnSpeedDeg);
        lifeTime = Mathf.Max(0.1f, lifeTime);
        damage = Mathf.Max(0, damage);

        level = Mathf.Clamp(level, 1, 8);

        if (startEquipped) startOwned = true;
    }

    private void Update()
    {
        if (!_owned || !_equipped) return;
        if (Time.timeScale <= 0f) return;

        _t += Time.deltaTime;
        if (_t >= cooldown)
        {
            _t = 0f;
            TryFire();
        }
    }

    public void Grant(int startLevelValue, bool equip)
    {
        _owned = true;
        _equipped = equip;
        SetLevel(startLevelValue);
    }

    public void Upgrade(int delta)
    {
        if (!_owned)
        {
            Grant(1, true);
            return;
        }

        SetLevel(level + delta);
        _equipped = true;
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 8);
    }

    public void SetEquipped(bool equip)
    {
        if (!_owned && equip) _owned = true;
        _equipped = equip;
    }

    private void TryFire()
    {
        if (projectilePrefab == null)
        {
            if (debugLog) Debug.LogWarning("[HomingMissileSkill2D] projectilePrefab이 비어있음", this);
            return;
        }

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        int hitCount = Physics2DCompat.OverlapCircleNonAlloc(origin, searchRadius, _hits, enemyMask);

        // 디버그 혼란 방지(이전 프레임 잔상 제거)
        for (int i = hitCount; i < _hits.Length; i++) _hits[i] = null;

        if (hitCount <= 0)
        {
            if (debugLog) Debug.LogWarning("[HomingMissileSkill2D] 반경 내 Enemy가 없음(EnemyMask/Enemy Layer/Collider 확인)", this);
            return;
        }

        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            var c = _hits[i];
            if (c == null) continue;

            Transform t = (c.attachedRigidbody != null) ? c.attachedRigidbody.transform : c.transform;
            float sqr = ((Vector2)t.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        if (best == null)
        {
            if (debugLog) Debug.LogWarning("[HomingMissileSkill2D] Collider는 잡히는데 target Transform을 못 잡음", this);
            return;
        }

        int totalHits = Mathf.Clamp(level, 1, 8);

        // 프로젝타일의 chainCount는 "추가 체인 수"로 사용됨.
        // 총 1회 타격만 원하면 chainCount=0이어야 함.
        int chainCount = Mathf.Max(0, totalHits - 1);

        Vector2 startDir = ((Vector2)best.position - origin);
        if (startDir.sqrMagnitude < 0.0001f) startDir = Vector2.right;
        else startDir.Normalize();

        var p = Instantiate(projectilePrefab, origin, Quaternion.identity);

        // Init(9개) 시그니처에 정확히 맞춤
        p.Init(
            enemyMask,
            searchRadius,
            damage,
            projectileSpeed,
            turnSpeedDeg,
            chainCount,
            lifeTime,
            startDir,
            best
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, searchRadius);
    }
}