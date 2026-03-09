// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

// <summary>
// [요약]
// - 폭발 트리거(충돌 또는 선택적 거리 도달) 시 폭발 데미지 적용
// - 폭발 후 depth < maxDepth 이면 V자(±splitAngleDeg)로 2개 분열
// - 분열은 "항상 현재 투사체의 폭발 위치"에서만 생성된다(플레이어/발사포인트로 되돌아가 분열 금지)
// - depth/maxDepth 예:
//   depth=1,maxDepth=1 => 1개
//   depth=1,maxDepth=2 => 1 -> 2
//   depth=1,maxDepth=3 => 1 -> 2 -> 4
//   depth=1,maxDepth=4 => 1 -> 2 -> 4 -> 8
// - 성능 보호: 트리 전체 공유 예산(budget)
// - 자식 생성은 ProjectilePool2D 우선, 없으면 Instantiate 폴백
// </summary>
[DisallowMultipleComponent]
public sealed class DarkOrbSplitProjectile2D : PooledObject2D
{
    // -------------------------
    // 공유 예산(트리 전체가 같은 참조를 공유)
    // -------------------------
    // 외부(메인 오브)에서 예산을 만들어 자식들에게 동일 참조로 전달할 수 있어야 하므로 public.
    public sealed class FragmentBudget
    {
        public int used;
        public int max;
    }

    // -------------------------
    // 런타임 파라미터 (Init으로만 세팅)
    // -------------------------
    private LayerMask _enemyMask;
    private int _explosionDamage;
    private float _speed;
    private float _life;
    private float _age;

    private Vector2 _dir;
    private float _explodeRadius;

    // 0이면 거리 트리거 비활성(충돌로만 폭발/분열)
    private float _splitDistance;
    private float _splitAngleDeg;

    // depth 기반 분열
    private int _depth;     // 1부터 시작
    private int _maxDepth;  // 예: 4면 최종 8개까지

    private FragmentBudget _budget;         // 공유 예산
    private ProjectilePool2D _splitPool;    // 자식 스폰용(브릿지) (부모 _pool과 이름 분리)

    // "거리 트리거"용 시작 위치: 반드시 Init에서 스폰 위치가 확정된 뒤에 잡아야 함
    private Vector2 _startPos;

    private bool _initialized;
    private bool _loggedInitError;
    private bool _exploding; // 중복 폭발 방지

    // “누가 Init 없이 켰는지” 잡는 스폰 스택
    private string _spawnStack;

    private readonly List<Collider2D> _hits = new List<Collider2D>(16);
    private ContactFilter2D _enemyFilter;

    private void OnEnable()
    {
        Debug.LogError($"[CHECK] SplitProjectile ExplodeAndSplit RUNNING depth={_depth}/{_maxDepth} name={gameObject.name} id={GetInstanceID()}");
        _age = 0f;

        _enemyMask = 0;
        _explosionDamage = 0;
        _speed = 0f;
        _life = 0f;

        _dir = Vector2.right;
        _explodeRadius = 0f;

        _splitDistance = 0f;     // 기본은 비활성(충돌로만 폭발)
        _splitAngleDeg = 40f;

        _depth = 1;
        _maxDepth = 1;

        _budget = null;
        _splitPool = null;

        _startPos = Vector2.zero; // Init에서 다시 잡는다

        _initialized = false;
        _loggedInitError = false;
        _exploding = false;

        _hits.Clear();
        _spawnStack = Environment.StackTrace;
    }

    /// <summary>
    /// Init은 반드시 1번 호출되어야 함.
    /// depth/maxDepth:
    /// - 최초 투사체는 depth=1로 시작.
    /// - maxDepth=4면 1->2->4->8 분열이 성립.
    /// sharedBudget/sharedPool은 "트리 전체 공유 참조"로 넘긴다.
    /// </summary>
    public void Init(
        LayerMask enemyMask,
        int explosionDmg,
        float speed,
        float lifeSeconds,
        Vector2 direction,
        float explosionRadius,
        float splitDistance,
        float splitAngleDeg,
        int depth,
        int maxDepth,
        object sharedBudget,
        int maxBudget,
        object sharedPool)
    {
        _enemyMask = enemyMask;
        _explosionDamage = Mathf.Max(1, explosionDmg);

        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, lifeSeconds);
        _age = 0f;

        _dir = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector2.right;
        _explodeRadius = Mathf.Max(0f, explosionRadius);

        // 0이면 비활성. (네가 원한 “맞으면 분열” 기본값)
        _splitDistance = Mathf.Max(0f, splitDistance);
        _splitAngleDeg = splitAngleDeg;

        _depth = Mathf.Max(1, depth);
        _maxDepth = Mathf.Max(1, maxDepth);

        // 거리 기준 시작점은 "Init 시점의 현재 위치"로 확정
        _startPos = transform.position;

        // 타격 필터
        _enemyFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _enemyMask,
            useTriggers = true
        };

        // budget 공유
        if (sharedBudget is FragmentBudget fb)
        {
            _budget = fb;
        }
        else
        {
            _budget = (maxBudget > 0) ? new FragmentBudget { used = 0, max = maxBudget } : null;
        }

        // pool(브릿지) 공유
        if (sharedPool is ProjectilePool2D p)
        {
            _splitPool = p;
        }
        else
        {
            _splitPool = null;
        }

        _initialized = true;
        _loggedInitError = false;
        _exploding = false;
    }

    private void Update()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogError(
                    $"[DarkOrbSplitProjectile2D] Init() 없이 활성화됨. 스폰 경로 확인 필요.\n{_spawnStack}",
                    this);
            }

            ReturnToPool();
            return;
        }

        _age += Time.deltaTime;

        // 수명 만료
        if (_age >= _life)
        {
            ReturnToPool();
            return;
        }

        // 이동
        Vector2 pos = transform.position;
        pos += _dir * (_speed * Time.deltaTime);
        transform.position = pos;

        // 선택 기능: 거리 도달 시 폭발(원하면 splitDistance>0일 때만)
        if (_splitDistance > 0.0001f)
        {
            float dist = Vector2.Distance(_startPos, pos);
            if (dist >= _splitDistance)
                ExplodeAndSplit();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;

        // 적 레이어만(반드시 value 사용)
        if (((1 << other.gameObject.layer) & _enemyMask.value) == 0) return;

        // 충돌은 "폭발 트리거"만
        ExplodeAndSplit();
    }

    private void ExplodeAndSplit()
    {
        if (_exploding) return; // 중복 방지
        _exploding = true;

        ApplyExplosionDamage();

        // depth 기반 분열
        if (_depth < _maxDepth)
        {
            // 자식 2개 생성
            if (TryConsumeBudget(2))
            {
                Vector2 pos = transform.position;

                // 즉시 재충돌/겹침 방지로 살짝 앞으로 밀어준다
                const float spawnEps = 0.05f;

                Vector2 dirA = Rotate(_dir, +_splitAngleDeg).normalized;
                Vector2 dirB = Rotate(_dir, -_splitAngleDeg).normalized;

                SpawnChild(pos + dirA * spawnEps, dirA, _depth + 1, _maxDepth);
                SpawnChild(pos + dirB * spawnEps, dirB, _depth + 1, _maxDepth);
            }
        }

        ReturnToPool();
    }

    private void ApplyExplosionDamage()
    {
        if (_explodeRadius > 0.05f)
        {
            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, _explodeRadius, _enemyFilter, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                var c = _hits[i];
                if (c == null) continue;
                DamageUtil2D.TryApplyDamage(c, _explosionDamage);
            }
        }
        else
        {
            var one = Physics2D.OverlapCircle(transform.position, 0.1f, _enemyMask);
            if (one != null) DamageUtil2D.TryApplyDamage(one, _explosionDamage);
        }
    }

    private bool TryConsumeBudget(int amount)
    {
        if (_budget == null) return true;

        if (_budget.max <= 0) return false;
        if (_budget.used + amount > _budget.max) return false;

        _budget.used += amount;
        return true;
    }

    private void SpawnChild(Vector2 pos, Vector2 childDir, int childDepth, int maxDepth)
    {
        DarkOrbSplitProjectile2D child = null;

        // 1) 풀 스폰
        if (_splitPool != null)
        {
            child = _splitPool.Get<DarkOrbSplitProjectile2D>(pos, Quaternion.identity);
        }

        // 2) 폴백(가능하면 실제 게임에서는 _splitPool 전달해서 이 루트 안 타게 만들기)
        if (child == null)
        {
            child = Instantiate(this, pos, Quaternion.identity);
        }

        child.Init(
            enemyMask: _enemyMask,
            explosionDmg: _explosionDamage,
            speed: _speed,
            lifeSeconds: _life,
            direction: childDir,
            explosionRadius: _explodeRadius,
            splitDistance: _splitDistance,
            splitAngleDeg: _splitAngleDeg,
            depth: childDepth,
            maxDepth: maxDepth,
            sharedBudget: _budget,
            maxBudget: (_budget != null) ? _budget.max : 0,
            sharedPool: _splitPool
        );
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}