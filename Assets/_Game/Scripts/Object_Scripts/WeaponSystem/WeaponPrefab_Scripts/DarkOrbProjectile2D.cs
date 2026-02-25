// UTF-8
// 다크 오브(암흑구) - 거리 기반 폭발 + 40도 V자 재귀 분열
// - 투사체 직격 데미지 없음(0). "폭발" 데미지만 존재.
// - 적에 닿거나(splitDistance 도달 전이라도) splitDistance만큼 이동하면 폭발.
// - 폭발 지점에서 진행 방향 기준 ±splitAngle(기본 40도)로 2개 생성.
// - splitDepth(남은 재귀 횟수)와 maxFragmentsBudget(총 파편 예산)으로 폭증 방지.
// - 무기는 이 프리팹(메인 오브)만 발사해야 함.

using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    [Header("이동/수명")]
    [Tooltip("최대 생존 시간(초). 거리 분열 전에 만료되면 그냥 사라짐(기본).")]
    [SerializeField] private float lifeSeconds = 1.0f;

    [Tooltip("이 거리 도달 시 폭발 + 분열")]
    [SerializeField] private float splitDistance = 4.0f;

    [Header("폭발")]
    [Tooltip("폭발 반경(0이면 1명만)")]
    [SerializeField] private float explosionRadius = 1.0f;

    [Header("분열")]
    [Tooltip("V자 각도(도). 진행 방향 기준 ±각도. 우리는 40도로 고정 사용.")]
    [SerializeField] private float splitAngle = 40f;

    [Tooltip("분열체 속도 배율(1=동일)")]
    [SerializeField] private float splitSpeedMultiplier = 0.9f;

    [Tooltip("한 발에서 생성될 수 있는 총 파편 수 상한(budget). 2개 생성 시 2 소모.")]
    [SerializeField] private int maxFragmentsBudget = 64;

    [Header("오너(플레이어) 충돌 무시")]
    [Tooltip("생성 직후 이 시간 동안 오너(플레이어)와 충돌 무시")]
    [SerializeField] private float ownerIgnoreSeconds = 0.08f;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    // 런타임 파라미터(무기에서 주입)
    private int _explosionDamage;          // 폭발 데미지(직격 데미지는 없음)
    private float _speed;
    private int _splitDepth;              // 남은 재귀 횟수(0이면 분열 없음)
    private LayerMask _enemyMask;
    private Transform _owner;

    // 상태
    private Vector2 _startPos;
    private float _dieAt;
    private bool _ended;
    private Vector2 _dir;                 // 진행 방향(정규화)

    // 트리 전체 예산 공유용(참조 공유)
    private sealed class FragmentBudget
    {
        public int used;
        public int max;
    }

    private FragmentBudget _budget;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        _ended = false;
        _startPos = transform.position;
    }

    /// <summary>
    /// 무기에서 호출: 분열 깊이, 파편 예산, 폭발 반경 설정
    /// </summary>
    public void Configure(int splitDepth, int maxFragmentsBudget, float explosionRadius)
    {
        _splitDepth = Mathf.Max(0, splitDepth);
        this.maxFragmentsBudget = Mathf.Max(0, maxFragmentsBudget);
        this.explosionRadius = Mathf.Max(0f, explosionRadius);
    }

    /// <summary>
    /// 5인수 호환용(기존 코드 호환).
    /// owner가 없으면 플레이어 충돌 무시만 스킵됨.
    /// </summary>
    public void Launch(Vector2 dir, int explosionDamage, float speed, float lifeSeconds, LayerMask enemyMask)
    {
        Launch(dir, explosionDamage, speed, lifeSeconds, enemyMask, owner: null);
    }

    /// <summary>
    /// 무기에서 호출(정식): 폭발 데미지/속도/수명/적마스크/오너 세팅
    /// </summary>
    public void Launch(Vector2 dir, int explosionDamage, float speed, float lifeSeconds, LayerMask enemyMask, Transform owner)
    {
        _owner = owner;
        _enemyMask = enemyMask;

        _explosionDamage = Mathf.Max(1, explosionDamage);
        _speed = Mathf.Max(0.01f, speed);

        this.lifeSeconds = Mathf.Max(0.05f, lifeSeconds);
        _dieAt = Time.time + this.lifeSeconds;

        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        _dir = dir.normalized;

        // 루트에서만 새 budget 생성. (분열체는 SpawnChild에서 참조를 공유받는다)
        _budget = new FragmentBudget { used = 0, max = maxFragmentsBudget };

        // 플레이어에 박혀 즉시 폭발 방지
        PushOutFromOwner(_dir);
        IgnoreOwnerCollisionTemporarily();

        _startPos = transform.position;

        if (rb != null)
            rb.linearVelocity = _dir * _speed;

        if (log) Debug.Log($"[DarkOrbProjectile2D] Launch dmg={_explosionDamage} depth={_splitDepth} angle={splitAngle}", this);
    }

    private void FixedUpdate()
    {
        if (_ended) return;

        if (Time.time >= _dieAt)
        {
            // 수명 만료 시 폭발시키고 싶으면 여기서 ExplodeAndSplit()로 바꾸면 됨.
            Despawn();
            return;
        }

        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        float moved = Vector2.Distance(_startPos, pos);

        if (moved >= Mathf.Max(0.01f, splitDistance))
        {
            ExplodeAndSplit();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_ended) return;
        if (other == null) return;

        // Enemy만 반응
        if (((1 << other.gameObject.layer) & _enemyMask.value) == 0)
            return;

        ExplodeAndSplit();
    }

    private void ExplodeAndSplit()
    {
        if (_ended) return;
        _ended = true;

        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;

        // 1) 폭발 데미지(직격 데미지 없음)
        ApplyExplosionDamage(pos);

        // 2) 분열: depth>0이면 V자 2개
        if (_splitDepth > 0)
        {
            // 2개 생성 => budget 2 소비
            if (TryConsumeBudget(2))
            {
                int childDepth = _splitDepth - 1;
                SpawnChild(pos, +splitAngle, childDepth);
                SpawnChild(pos, -splitAngle, childDepth);
            }
        }

        Despawn();
    }

    private void ApplyExplosionDamage(Vector2 pos)
    {
        if (explosionRadius <= 0.01f)
        {
            var hit = Physics2D.OverlapCircle(pos, 0.1f, _enemyMask);
            if (hit != null) TryApplyDamage(hit, _explosionDamage);
            return;
        }

        var hits = Physics2D.OverlapCircleAll(pos, explosionRadius, _enemyMask);
        for (int i = 0; i < hits.Length; i++)
            TryApplyDamage(hits[i], _explosionDamage);
    }

    private void SpawnChild(Vector2 pos, float angleOffsetDeg, int childDepth)
    {
        Vector2 childDir = Rotate(_dir, angleOffsetDeg).normalized;

        var child = Instantiate(this, pos, Quaternion.identity);

        // budget 공유(중요)
        child._budget = _budget;

        // 설정 공유/전달
        child.splitDistance = splitDistance;
        child.splitAngle = splitAngle;
        child.explosionRadius = explosionRadius;
        child.maxFragmentsBudget = maxFragmentsBudget;

        child._splitDepth = Mathf.Max(0, childDepth);
        child._explosionDamage = _explosionDamage;
        child._speed = _speed * splitSpeedMultiplier;
        child._enemyMask = _enemyMask;
        child._owner = _owner;

        child.lifeSeconds = lifeSeconds;
        child._dieAt = Time.time + child.lifeSeconds;

        child._dir = childDir;
        child._startPos = pos;

        child.PushOutFromOwner(childDir);
        child.IgnoreOwnerCollisionTemporarily();

        if (child.rb != null)
            child.rb.linearVelocity = childDir * child._speed;

        if (log) Debug.Log($"[DarkOrbProjectile2D] Child angle={angleOffsetDeg} depth={child._splitDepth} budget={_budget.used}/{_budget.max}", child);
    }

    private bool TryConsumeBudget(int amount)
    {
        if (_budget == null) return false;
        if (_budget.max <= 0) return false;
        if (_budget.used + amount > _budget.max) return false;

        _budget.used += amount;
        return true;
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void PushOutFromOwner(Vector2 dir)
    {
        if (_owner == null) return;

        // 플레이어 몸에서 시작 충돌 방지(기본 0.25)
        float push = 0.25f;
        transform.position = (Vector2)_owner.position + dir * push;
    }

    private void IgnoreOwnerCollisionTemporarily()
    {
        if (_owner == null || col == null) return;

        var ownerCols = _owner.GetComponentsInChildren<Collider2D>();
        if (ownerCols == null || ownerCols.Length == 0) return;

        for (int i = 0; i < ownerCols.Length; i++)
        {
            if (ownerCols[i] == null) continue;
            Physics2D.IgnoreCollision(col, ownerCols[i], true);
        }

        CancelInvoke(nameof(ReleaseOwnerIgnore));
        Invoke(nameof(ReleaseOwnerIgnore), ownerIgnoreSeconds);
    }

    private void ReleaseOwnerIgnore()
    {
        if (_owner == null || col == null) return;

        var ownerCols = _owner.GetComponentsInChildren<Collider2D>();
        if (ownerCols == null || ownerCols.Length == 0) return;

        for (int i = 0; i < ownerCols.Length; i++)
        {
            if (ownerCols[i] == null) continue;
            Physics2D.IgnoreCollision(col, ownerCols[i], false);
        }
    }

    private void TryApplyDamage(Collider2D target, int dmg)
    {
        if (target == null) return;

        // 프로젝트 공통 데미지 시스템이 있으면 여기만 교체하면 됨.
        // 지금은 컴파일 안정성을 위해 SendMessage 사용.
        target.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    private void Despawn()
    {
        // 풀링이면 Return으로 교체 가능
        gameObject.SetActive(false);
    }
}