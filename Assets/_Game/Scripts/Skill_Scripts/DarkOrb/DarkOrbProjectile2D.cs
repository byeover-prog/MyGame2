// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 기존(잘못된) 구조: 폭발 시 splitCount만큼 -40~+40에 "균등 분산"으로 한 번에 방사형 생성(=GPT 그림)
// - 수정(원하는) 구조: Player 그림처럼 계층 트리 분열
//   depth=1(메인) 폭발 -> 2개 생성
//   depth=2 폭발 -> 2개씩 생성(총 4)
//   depth=3 폭발 -> 2개씩 생성(총 8)
// - splitCount는 "한 번에 발사 수"가 아니라, "최대 분열 단계"로 해석한다.
//   0 => maxDepth=1 (분열 없음)
//   2 => maxDepth=2 (1->2)
//   4 => maxDepth=3 (1->2->4)
//   8 => maxDepth=4 (1->2->4->8)

[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    [Header("분열 프리팹(권장: 자기 자신 프리팹)")]
    [Tooltip("분열로 찍어낼 프리팹(보통 자기 자신 DarkOrbProjectile2D 프리팹)\n비우면 현재 오브젝트를 복제합니다(권장X).")]
    [SerializeField] private DarkOrbProjectile2D splitSpawnPrefab;

    [Header("분열 각도(도)")]
    [Tooltip("V자 분열 각도(도). 예) 40 = ±40도")]
    [Range(1f, 89f)]
    [SerializeField] private float splitAngleDeg = 40f;

    [Header("겹침 방지(유닛)")]
    [Tooltip("분열체를 폭발 위치에서 살짝 밀어내서, 같은 콜라이더 안에서 즉시 연쇄폭발하는 현상을 줄입니다.")]
    [Min(0f)]
    [SerializeField] private float spawnEps = 0.4f;

    private LayerMask _enemyMask;

    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;

    private float _explosionRadius;

    // 외부 Init에서 받는 값(0/2/4/8)
    private int _splitCount;
    private float _splitSpeed;
    private float _splitLife;
    private int _splitDamage;
    private ProjectilePool2D _splitPool;

    private float _alpha = 0.55f;

    // 트리 분열용
    private int _depth = 1;      // 현재 단계(1부터)
    private int _maxDepth = 1;   // 최대 단계(1~4)

    private bool _inited;
    private bool _exploding;
    private readonly Collider2D[] _hits = new Collider2D[32];

    // DarkOrbSkill2D가 요구하는 Init(12개 인자) 유지
    public void Init(
        LayerMask enemyMask,
        int damage,
        float speed,
        float lifeSeconds,
        Vector2 dir,
        float explosionRadius,
        int splitCount,
        float splitSpeed,
        float splitLifeSeconds,
        int splitDamage,
        ProjectilePool2D splitPool,
        float orbAlpha
    )
    {
        _enemyMask = enemyMask;
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, lifeSeconds);
        _age = 0f;

        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        _explosionRadius = Mathf.Max(0.05f, explosionRadius);

        _splitCount = Mathf.Max(0, splitCount);
        _splitSpeed = Mathf.Max(0.1f, splitSpeed);
        _splitLife = Mathf.Max(0.05f, splitLifeSeconds);
        _splitDamage = Mathf.Max(0, splitDamage);
        _splitPool = splitPool;

        _alpha = Mathf.Clamp01(orbAlpha);

        // 트리 규칙: splitCount를 maxDepth로 변환
        _depth = 1;
        _maxDepth = SplitCountToMaxDepth(_splitCount);

        ApplyAlpha(_alpha);

        _inited = true;
        _exploding = false;
    }

    // 자식 스폰 시 depth/maxDepth를 유지하기 위한 세팅
    private void SetTreeDepth(int depth, int maxDepth)
    {
        _depth = Mathf.Max(1, depth);
        _maxDepth = Mathf.Max(1, maxDepth);
    }

    private static int SplitCountToMaxDepth(int splitCount)
    {
        if (splitCount <= 0) return 1; // 분열 없음
        if (splitCount <= 2) return 2; // 1->2
        if (splitCount <= 4) return 3; // 1->2->4
        return 4;                      // 1->2->4->8
    }

    private void ApplyAlpha(float a)
    {
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            var sr = srs[i];
            if (sr == null) continue;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    private void Update()
    {
        if (!_inited) return;

        float dt = Time.deltaTime;
        _age += dt;

        transform.position += (Vector3)(_dir * (_speed * dt));

        if (_age >= _life)
        {
            Explode((Vector2)transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_inited) return;
        if (other == null) return;

        if (DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask))
        {
            Explode((Vector2)transform.position);
        }
    }

    private void Explode(Vector2 pos)
    {
        if (!_inited) return;
        if (_exploding) return;
        _exploding = true;

        // 1) 폭발 데미지 1회
        int count = Physics2D.OverlapCircleNonAlloc(pos, _explosionRadius, _hits, _enemyMask);
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (h == null) continue;
            DamageUtil2D.ApplyDamage(h, _damage);
        }

        // 2) Player 그림 구조: "항상 2개만" 분열 (depth 기반)
        if (_depth < _maxDepth)
        {
            Vector2 dirA = Rotate(_dir, +splitAngleDeg).normalized;
            Vector2 dirB = Rotate(_dir, -splitAngleDeg).normalized;

            SpawnChild(pos + dirA * spawnEps, dirA, _depth + 1, _maxDepth);
            SpawnChild(pos + dirB * spawnEps, dirB, _depth + 1, _maxDepth);
        }

        Destroy(gameObject);
    }

    private void SpawnChild(Vector2 pos, Vector2 d, int childDepth, int maxDepth)
    {
        DarkOrbProjectile2D child;

        // 프리팹 우선(권장)
        if (splitSpawnPrefab != null)
        {
            child = Instantiate(splitSpawnPrefab, pos, Quaternion.identity);
        }
        else
        {
            // 폴백(권장X): 현재 오브젝트 복제
            child = Instantiate(this, pos, Quaternion.identity);
        }

        // 자식은 "splitCount=0"으로 넣고, depth/maxDepth는 별도로 세팅해서 트리 유지
        int childDmg = (_splitDamage > 0) ? _splitDamage : _damage;

        child.Init(
            _enemyMask,
            childDmg,
            _splitSpeed,
            _splitLife,
            d,
            _explosionRadius,
            0,              // 중요: 방사형 분열 방지(이 값을 쓰지 않음)
            _splitSpeed,
            _splitLife,
            0,
            _splitPool,
            _alpha
        );

        child.SetTreeDepth(childDepth, maxDepth);
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}