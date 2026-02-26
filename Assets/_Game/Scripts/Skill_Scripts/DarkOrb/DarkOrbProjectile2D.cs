// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 직선 이동 후, 수명 종료 또는 적 충돌 시 폭발한다.
// - 폭발은 OverlapCircle로 범위 데미지를 1회 준다.
// - 분열은 "폭발 지점"에서만 생성한다(플레이어/발사점으로 돌아가면 안 됨).
// - DarkOrbSkill2D가 호출하는 Init(...) 시그니처를 제공한다(컴파일 안정화).

[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    [Header("분열 프리팹(선택)")]
    [Tooltip("비우면 자기 자신으로 분열합니다.")]
    [SerializeField] private DarkOrbProjectile2D splitSpawnPrefab;

    private LayerMask _enemyMask;

    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;

    private float _explosionRadius;

    private int _splitCount;
    private float _splitSpeed;
    private float _splitLife;
    private int _splitDamage;
    private ProjectilePool2D _splitPool;

    private float _alpha = 0.55f;

    private bool _inited;
    private readonly Collider2D[] _hits = new Collider2D[32];

    // DarkOrbSkill2D가 요구하는 Init(12개 인자) 제공
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

        if (splitSpawnPrefab == null) splitSpawnPrefab = this;

        ApplyAlpha(_alpha);
        _inited = true;
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
        _inited = false;

        int count = Physics2D.OverlapCircleNonAlloc(pos, _explosionRadius, _hits, _enemyMask);
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (h == null) continue;
            DamageUtil2D.ApplyDamage(h, _damage);
        }

        if (_splitCount > 0)
        {
            float baseAngle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            const float splitAngle = 40f;

            // splitCount는 "한 번 폭발에서 생성할 수"로 해석
            int spawnN = Mathf.Clamp(_splitCount, 0, 64);

            for (int i = 0; i < spawnN; i++)
            {
                // 균등 분산(-40~+40)
                float t = (spawnN == 1) ? 0f : (i / (float)(spawnN - 1)) * 2f - 1f; // -1..+1
                float ang = baseAngle + t * splitAngle;

                Vector2 d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                SpawnChild(pos, d);
            }
        }

        Destroy(gameObject);
    }

    private void SpawnChild(Vector2 pos, Vector2 d)
    {
        DarkOrbProjectile2D child = null;

        if (_splitPool != null)
        {
            // 풀을 쓰고 싶으면 여기서 Get<DarkOrbProjectile2D>가 가능한 구조여야 함.
            // 프로젝트마다 풀 구현이 다르니, 안전하게 Instantiate로 처리.
        }

        child = Instantiate(splitSpawnPrefab, pos, Quaternion.identity);
        child.Init(_enemyMask, _splitDamage > 0 ? _splitDamage : _damage, _splitSpeed, _splitLife, d,
                   _explosionRadius, 0, 0f, 0.1f, 0, null, _alpha);
    }
}