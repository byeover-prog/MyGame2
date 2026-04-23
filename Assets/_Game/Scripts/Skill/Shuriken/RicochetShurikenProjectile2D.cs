// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - hitSet(이미 때린 적)로 "중복 데미지"는 막는다.
// - 하지만 ★ 이미 때린 적과 다시 겹치면(return)으로 끝내면 "박힘/회전" 버그가 난다.
// - 따라서 "중복 데미지"만 막고, 튕김/소멸은 정상 진행하도록 처리한다.
// - 겹친 몬스터(같은 프레임에 여러 콜라이더) 상황은 hitImmunitySeconds로 안정화한다.

[DisallowMultipleComponent]
public sealed class RicochetShurikenProjectile2D : PooledObject2D
{
    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;

    private int remainingBounces;
    private EnemyRegistryMember2D target;

    private bool _initialized;
    private bool _loggedInitError;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    [Header("연출")]
    [Tooltip("날아가는 동안 회전 속도(도/초). 0이면 회전 안 함.")]
    [SerializeField] private float rotateDegPerSec = 1080f;

    [Header("튕김 탐색")]
    [Tooltip("튕길 때 다음 적을 찾는 최대 반경. 이 범위 밖이면 튕기지 않고 소멸.")]
    [Min(0.5f)]
    [SerializeField] private float bounceSearchRadius = 8f;

    [Header("박힘 방지")]
    [Tooltip("적을 맞춘 직후, 잠깐 강제 전진해서 콜라이더 밖으로 빠져나오는 시간(초)")]
    [SerializeField] private float exitKickSeconds = 0.08f;

    [Tooltip("ExitKick 동안의 이동 속도 배율(기본 속도 * 배율)")]
    [SerializeField] private float exitKickSpeedMul = 1.5f;

    [Header("겹침 방지")]
    [Tooltip("적을 맞춘 뒤, 이 시간 동안 다른 적과의 충돌을 무시(겹친 몬스터 관통용)")]
    [SerializeField] private float hitImmunitySeconds = 0.15f;

    [Header("소멸 연출")]
    [Tooltip("마지막 타격 후 사라지기까지 대기 시간(초). 이 동안 반대 방향으로 빠져나옴.")]
    [SerializeField] private float despawnDelay = 0.10f;

    private float _exitKickLeft;
    private float _hitImmunityLeft;
    private Vector2 _lastDir = Vector2.right;
    private Vector2 _despawnDir = Vector2.right;

    private EnemyRegistryMember2D _pendingTarget;

    private bool _despawnScheduled;
    private float _despawnTimer;

    private Collider2D _collider;

    private void OnEnable()
    {
        age = 0f;
        enemyMask = 0;
        damage = 0;
        speed = 0f;
        life = 0f;

        remainingBounces = 0;
        target = null;

        hitSet.Clear();

        _initialized = false;
        _loggedInitError = false;

        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;
        _lastDir = Vector2.right;
        _despawnDir = Vector2.right;

        _despawnScheduled = false;
        _despawnTimer = 0f;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = true;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, int bounces, EnemyRegistryMember2D startTarget)
    {
        enemyMask = mask;
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        remainingBounces = Mathf.Max(0, bounces);
        target = startTarget;

        hitSet.Clear();

        _initialized = true;
        _loggedInitError = false;

        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;

        _despawnScheduled = false;
        _despawnTimer = 0f;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = true;

        if (target != null)
        {
            Vector2 d = (target.Position - (Vector2)transform.position);
            if (d.sqrMagnitude > 0.0001f) _lastDir = d.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogError("[RicochetShurikenProjectile2D] Init()이 호출되지 않았습니다.", this);
            }
            SafeReturn();
            return;
        }

        float dt = Time.fixedDeltaTime;

        // 소멸 예약 중: 반대 방향으로 빠져나오면서 사라짐
        if (_despawnScheduled)
        {
            transform.position += (Vector3)(_despawnDir * speed * exitKickSpeedMul * dt);
            _despawnTimer -= dt;
            if (_despawnTimer <= 0f)
                SafeReturn();
            return;
        }

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * dt);

        age += dt;

        // ★ 수명 만료 판단: 튕김이 남아있으면 수명 무시
        // JSON life(Lv8=2.5초)가 튕김 9회를 소화하기엔 부족하므로
        // 튕김이 남아있는 동안은 절대 수명으로 죽지 않게 함.
        // 무한 방지: 10초 하드 타임아웃.
        if (remainingBounces > 0)
        {
            // 튕김 남아있음 → 수명 만료 무시, 10초 안전장치만 적용
            if (age >= 10f)
            {
                SafeReturn();
                return;
            }
        }
        else
        {
            // 튕김 다 소모 → 원래 수명 적용
            if (age >= life)
            {
                SafeReturn();
                return;
            }
        }

        if (_hitImmunityLeft > 0f)
            _hitImmunityLeft -= dt;

        // 1) ExitKick: 맞춘 직후 콜라이더 밖으로 빠져나오기
        if (_exitKickLeft > 0f)
        {
            _exitKickLeft -= dt;
            transform.position += (Vector3)(_lastDir * (speed * exitKickSpeedMul) * dt);

            if (_exitKickLeft <= 0f && _pendingTarget != null)
            {
                target = _pendingTarget;
                _pendingTarget = null;
            }
            return;
        }

        // 2) 타겟 유효성 — ★ 타겟이 죽었으면 새 타겟 탐색
        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            // 순차 발사(2~3번째 수리검)가 도착하기 전에 적이 죽는 경우 대비
            // → 남은 튕김이 있든 없든 일단 새 타겟을 찾아서 날아감
            if (TryFindBounceTarget(out var next))
            {
                target = next;
                age = 0f; // ★ 새 타겟 찾으면 수명 리셋
            }
            else
            {
                ScheduleDespawn(_lastDir);
                return;
            }
        }

        // 3) 타겟으로 이동
        Vector2 desired = (target.Position - (Vector2)transform.position);

        if (desired.sqrMagnitude < 0.0001f)
        {
            ScheduleDespawn(-_lastDir);
            return;
        }

        Vector2 dir = desired.normalized;
        _lastDir = dir;
        transform.position += (Vector3)(dir * speed * dt);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 겹친 채로 Enter가 끝난 뒤에도 계속 박힐 수 있어서 Stay에서도 처리
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider2D other)
    {
        if (!_initialized) return;
        if (_despawnScheduled) return;
        if (other == null) return;

        // 히트 무적 중이면 무시
        if (_hitImmunityLeft > 0f) return;

        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);

        // ★ 핵심 수정:
        // 이미 때린 적이면 "데미지"는 주지 않되, 박힘 방지를 위해 튕김/소멸은 진행한다.
        if (hitSet.Contains(id))
        {
            HandleAlreadyHitContact();
            return;
        }

        // 최초 타격
        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        // 튕김 처리
        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (TryFindBounceTarget(out var next))
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds);
                age = 0f; // ★ 튕길 때마다 수명 리셋
                return;
            }
        }

        // 마지막 타격(또는 다음 타겟 없음)
        ScheduleDespawn(-_lastDir);
    }

    private void HandleAlreadyHitContact()
    {
        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (TryFindBounceTarget(out var next))
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds);
                age = 0f; // ★ 튕길 때마다 수명 리셋
                return;
            }
        }

        ScheduleDespawn(-_lastDir);
    }

    /// <summary>
    /// 콜라이더를 즉시 끄고, despawnDir 방향으로 전진하다가 소멸.
    /// </summary>
    private void ScheduleDespawn(Vector2 despawnDir)
    {
        if (_despawnScheduled) return;
        _despawnScheduled = true;

        _despawnDir = despawnDir.sqrMagnitude > 0.0001f ? despawnDir.normalized : -_lastDir;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = false;

        _despawnTimer = Mathf.Max(0.01f, despawnDelay);
    }

    private void SafeReturn()
    {
        try
        {
            ReturnToPool();
        }
        catch
        {
            if (gameObject != null)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// hitSet에 없는 가장 가까운 적을 찾되, bounceSearchRadius 안에 있어야 함.
    /// 범위 밖이면 false 반환 → 튕기지 않고 소멸.
    /// </summary>
    private bool TryFindBounceTarget(out EnemyRegistryMember2D result)
    {
        result = null;

        if (!EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next) || next == null)
            return false;

        float dist = Vector2.Distance(transform.position, next.Position);
        if (dist > bounceSearchRadius)
            return false;

        result = next;
        return true;
    }
}