// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [요약]
/// - "거리 도달 후 폭발" + "40도 V자 재귀 분열" 스플릿 투사체
/// - 직격 데미지 없음(충돌은 폭발 트리거만)
/// - 재귀 분열은 최대 N회(remainingSplits)까지
/// - 성능 보호: 트리 전체가 공유하는 budget(총 파편 수 상한)
/// - 자식 생성은 ProjectilePool2D(브릿지) 우선, 없으면 Instantiate로 폴백
/// </summary>
[DisallowMultipleComponent]
public sealed class DarkOrbSplitProjectile2D : PooledObject2D
{
    // -------------------------
    // 공유 예산(트리 전체가 같은 참조를 공유)
    // -------------------------
    private sealed class FragmentBudget
    {
        public int used;
        public int max;
    }

    // -------------------------
    // 런타임 파라미터 (Init으로만 세팅)
    // -------------------------
    private LayerMask _enemyMask;
    private int _explosionDamage;     // 폭발 데미지만 존재
    private float _speed;
    private float _life;
    private float _age;

    private Vector2 _dir;
    private float _explodeRadius;

    private float _splitDistance;     // 이 거리 도달 시 폭발
    private float _splitAngleDeg;     // 기본 40도(±40)
    private int _remainingSplits;     // 남은 재귀 분열 횟수

    private FragmentBudget _budget;   // 공유 예산
    private ProjectilePool2D _pool;   // 자식 스폰용(브릿지)

    private Vector2 _startPos;

    private bool _initialized;
    private bool _ended;

    // 디버그: Init 누락 추적
    private bool _loggedInitError;
    private string _spawnStack;

    // 폭발 범위 타격 리스트(재사용)
    private readonly List<Collider2D> _hits = new List<Collider2D>(32);
    private ContactFilter2D _enemyFilter;

    private void OnEnable()
    {
        // 풀 재사용 대비 리셋
        _enemyMask = 0;
        _explosionDamage = 0;
        _speed = 0f;
        _life = 0f;
        _age = 0f;

        _dir = Vector2.right;
        _explodeRadius = 0f;

        _splitDistance = 999f;
        _splitAngleDeg = 40f;
        _remainingSplits = 0;

        _budget = null;
        _pool = null;

        _startPos = transform.position;

        _initialized = false;
        _ended = false;

        _loggedInitError = false;
        _spawnStack = Environment.StackTrace;

        _hits.Clear();
    }

    /// <summary>
    /// (구버전 호환) 재귀 분열 없이 그냥 "거리 폭발"만 쓰고 싶을 때
    /// </summary>
    public void Init(LayerMask enemyMask, int explosionDmg, float speed, float lifeSeconds, Vector2 direction, float explosionRadius)
    {
        Init(
            enemyMask: enemyMask,
            explosionDmg: explosionDmg,
            speed: speed,
            lifeSeconds: lifeSeconds,
            direction: direction,
            explosionRadius: explosionRadius,
            splitDistance: 999f,
            splitAngleDeg: 40f,
            remainingSplits: 0,
            sharedBudget: null,
            maxBudget: 0,
            sharedPool: null
        );
    }

    /// <summary>
    /// 신규 Init
    /// - sharedBudget/sharedPool는 "같은 참조"로 넘겨서 트리 전체 공유
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
        int remainingSplits,
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

        _splitDistance = Mathf.Max(0.01f, splitDistance);
        _splitAngleDeg = splitAngleDeg;
        _remainingSplits = Mathf.Max(0, remainingSplits);

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
            _pool = p;
        }
        else
        {
            _pool = null;
        }

        _startPos = transform.position;

        _initialized = true;
        _ended = false;

        // Init이 정상 호출되면 스택 제거
        _spawnStack = null;
        _loggedInitError = false;
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogError(
                    "[DarkOrbSplitProjectile2D] Init()이 호출되지 않았습니다.\n" +
                    "----- Spawn Stack -----\n" + _spawnStack,
                    this
                );
            }
            ReturnToPool();
            return;
        }

        if (_ended) return;

        _age += Time.fixedDeltaTime;
        if (_age >= _life)
        {
            ReturnToPool();
            return;
        }

        // 이동(물리힘 X, 코드 이동)
        transform.position += (Vector3)(_dir * _speed * Time.fixedDeltaTime);

        // 거리 도달 시 폭발
        float moved = Vector2.Distance(_startPos, (Vector2)transform.position);
        if (moved >= _splitDistance)
        {
            ExplodeAndSplit();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (_ended) return;
        if (other == null) return;

        // 적과 닿으면 폭발 트리거만
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        ExplodeAndSplit();
    }

    private void ExplodeAndSplit()
    {
        if (_ended) return;
        _ended = true;

        // 1) 폭발 데미지(폭발에서만)
        ApplyExplosionDamage();

        // 2) 재귀 분열 (V자 ±각도)
        if (_remainingSplits > 0)
        {
            // 자식 2개 생성 → budget 2 소비
            if (TryConsumeBudget(2))
            {
                Vector2 pos = transform.position;
                SpawnChild(pos, +_splitAngleDeg, _remainingSplits - 1);
                SpawnChild(pos, -_splitAngleDeg, _remainingSplits - 1);
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
            // 반경이 거의 0이면 근처 1명만
            var one = Physics2D.OverlapCircle(transform.position, 0.1f, _enemyMask);
            if (one != null) DamageUtil2D.TryApplyDamage(one, _explosionDamage);
        }
    }

    private bool TryConsumeBudget(int amount)
    {
        // budget 미사용이면 제한 없음(디버그/구버전 호환)
        if (_budget == null) return true;

        if (_budget.max <= 0) return false;
        if (_budget.used + amount > _budget.max) return false;

        _budget.used += amount;
        return true;
    }

    private void SpawnChild(Vector2 pos, float angleOffsetDeg, int childRemainingSplits)
    {
        Vector2 childDir = Rotate(_dir, angleOffsetDeg).normalized;

        DarkOrbSplitProjectile2D child = null;

        // 1순위: ProjectilePool2D(브릿지)로 스폰 (Instantiate 금지)
        if (_pool != null)
        {
            // 브릿지 시그니처 그대로 사용 (Quaternion 변수명이 이상해도 호출은 됨)
            child = _pool.Get<DarkOrbSplitProjectile2D>(pos, Quaternion.identity);
        }

        // 2순위: 폴백(디버그용). 가능하면 실제 게임에서는 _pool를 항상 전달해서 이 루트 안 타게 만들기.
        if (child == null)
        {
            child = Instantiate(this, pos, Quaternion.identity);
        }

        // 자식 Init: budget/pool 같은 참조 공유
        child.Init(
            enemyMask: _enemyMask,
            explosionDmg: _explosionDamage,
            speed: _speed,
            lifeSeconds: _life,
            direction: childDir,
            explosionRadius: _explodeRadius,
            splitDistance: _splitDistance,
            splitAngleDeg: _splitAngleDeg,
            remainingSplits: childRemainingSplits,
            sharedBudget: _budget,
            maxBudget: (_budget != null) ? _budget.max : 0,
            sharedPool: _pool
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