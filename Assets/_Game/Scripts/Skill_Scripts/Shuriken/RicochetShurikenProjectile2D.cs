// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 수리검 투사체.
/// [구현 원리 요약]
/// - Weapon_Shuriken 프리팹에는 ProjectilePool2D가 없어서 매 샷 Instantiate/Destroy가 일어나고 있었다.
/// - 그래서 이 투사체 내부에 static 풀을 넣어 항상 재사용되게 바꾼다.
/// - 외주 바디 VFX는 끄고, 스프라이트 + 회전 + 완만한 추적으로 레이저빔처럼 보이지 않게 고친다.
/// </summary>
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
    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    [Header("연출")]
    [Tooltip("날아가는 동안 회전 속도(도/초). 0이면 회전 안 함.")]
    [SerializeField] private float rotateDegPerSec = 1080f;

    [Header("추적")]
    [Tooltip("타겟을 향해 방향을 꺾는 속도(도/초)")]
    [SerializeField] private float turnSpeedDegPerSec = 1440f;

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
    private Vector2 _moveDir = Vector2.right;
    private Vector2 _lastDir = Vector2.right;
    private Vector2 _despawnDir = Vector2.right;
    private EnemyRegistryMember2D _pendingTarget;
    private bool _despawnScheduled;
    private float _despawnTimer;
    private Collider2D _collider;
    private bool _visualPrepared;

    private static readonly Dictionary<int, Queue<RicochetShurikenProjectile2D>> _pool = new Dictionary<int, Queue<RicochetShurikenProjectile2D>>();
    private static Transform _poolRoot;
    private static Transform _inactiveRoot;
    private int _poolKey;

    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[ShurikenPool]");
                DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }

            return _poolRoot;
        }
    }

    private static Transform InactiveRoot
    {
        get
        {
            if (_inactiveRoot == null)
            {
                var go = new GameObject("[ShurikenInactiveRoot]");
                go.SetActive(false);
                DontDestroyOnLoad(go);
                _inactiveRoot = go.transform;
            }

            return _inactiveRoot;
        }
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (GetTemplate(prefab) == null) return;

        int key = prefab.GetInstanceID();
        if (!_pool.ContainsKey(key)) _pool[key] = new Queue<RicochetShurikenProjectile2D>();

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, InactiveRoot);
            go.SetActive(false);

            var inst = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            if (inst == null)
            {
                Destroy(go);
                continue;
            }

            inst._poolKey = key;
            inst.PrepareVisual();
            inst.transform.SetParent(PoolRoot, false);
            _pool[key].Enqueue(inst);
        }
    }

    public static RicochetShurikenProjectile2D Spawn(GameObject prefab, Vector2 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        if (GetTemplate(prefab) == null) return null;

        int key = prefab.GetInstanceID();
        RicochetShurikenProjectile2D inst = null;

        if (_pool.TryGetValue(key, out var q))
        {
            while (q.Count > 0)
            {
                inst = q.Dequeue();
                if (inst != null) break;
                inst = null;
            }
        }

        if (inst == null)
        {
            var go = Instantiate(prefab, InactiveRoot);
            go.SetActive(false);

            inst = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            if (inst == null)
            {
                Destroy(go);
                return null;
            }

            inst._poolKey = key;
            inst.PrepareVisual();
        }

        inst.transform.SetParent(null, false);
        inst.transform.SetPositionAndRotation(pos, rot);
        inst.gameObject.SetActive(true);
        return inst;
    }

    private static RicochetShurikenProjectile2D GetTemplate(GameObject prefab)
    {
        if (prefab == null) return null;
        return prefab.GetComponent<RicochetShurikenProjectile2D>() ?? prefab.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
    }

    private void PrepareVisual()
    {
        if (_visualPrepared) return;

        if (_collider == null)
            _collider = GetComponent<Collider2D>();

        var vfx = GetComponent<ProjectileVFXChild>();
        if (vfx != null)
        {
            vfx.SetVFXEnabled(false, false);
            vfx.enabled = false;
        }

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] != null)
                srs[i].enabled = true;
        }

        _visualPrepared = true;
    }

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
        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;
        _moveDir = Vector2.right;
        _lastDir = Vector2.right;
        _despawnDir = Vector2.right;
        _despawnScheduled = false;
        _despawnTimer = 0f;

        PrepareVisual();

        if (_collider != null)
            _collider.enabled = true;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, int bounces, EnemyRegistryMember2D startTarget)
    {
        Vector2 initialDir = Vector2.right;
        if (startTarget != null)
        {
            initialDir = startTarget.Position - (Vector2)transform.position;
            if (initialDir.sqrMagnitude < 0.0001f)
                initialDir = Vector2.right;
        }

        Init(mask, dmg, spd, lifeSeconds, bounces, startTarget, initialDir);
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, int bounces, EnemyRegistryMember2D startTarget, Vector2 initialDir)
    {
        enemyMask = mask;
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;
        remainingBounces = Mathf.Max(0, bounces);
        target = startTarget;

        hitSet.Clear();

        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;
        _despawnScheduled = false;
        _despawnTimer = 0f;

        _moveDir = initialDir.sqrMagnitude > 0.0001f ? initialDir.normalized : Vector2.right;
        _lastDir = _moveDir;
        _despawnDir = _moveDir;

        if (_collider == null)
            _collider = GetComponent<Collider2D>();

        if (_collider != null)
            _collider.enabled = true;

        _initialized = true;
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            ReturnSelf();
            return;
        }

        float dt = Time.fixedDeltaTime;

        if (_despawnScheduled)
        {
            transform.position += (Vector3)(_despawnDir * speed * exitKickSpeedMul * dt);
            _despawnTimer -= dt;
            if (_despawnTimer <= 0f)
                ReturnSelf();
            return;
        }

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * dt);

        age += dt;
        if (age >= life)
        {
            ReturnSelf();
            return;
        }

        if (_hitImmunityLeft > 0f)
            _hitImmunityLeft -= dt;

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

        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            ScheduleDespawn(_moveDir);
            return;
        }

        Vector2 desired = target.Position - (Vector2)transform.position;
        if (desired.sqrMagnitude < 0.0001f)
        {
            ScheduleDespawn(-_moveDir);
            return;
        }

        Vector2 desiredDir = desired.normalized;
        float maxRad = Mathf.Max(0f, turnSpeedDegPerSec) * Mathf.Deg2Rad * dt;

        // ★ 수정: Vector2.RotateTowards는 존재하지 않음 → Vector3.RotateTowards 사용 후 캐스팅
        _moveDir = maxRad > 0f
            ? ((Vector2)Vector3.RotateTowards(_moveDir, desiredDir, maxRad, 0f)).normalized
            : desiredDir;

        _lastDir = _moveDir;
        transform.position += (Vector3)(_moveDir * speed * dt);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider2D other)
    {
        if (!_initialized) return;
        if (_despawnScheduled) return;
        if (other == null) return;
        if (_hitImmunityLeft > 0f) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);

        if (hitSet.Contains(id))
        {
            HandleAlreadyHitContact();
            return;
        }

        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next) && next != null)
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds);
                return;
            }
        }

        ScheduleDespawn(-_lastDir);
    }

    private void HandleAlreadyHitContact()
    {
        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next) && next != null)
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds);
                return;
            }
        }

        ScheduleDespawn(-_lastDir);
    }

    private void ScheduleDespawn(Vector2 despawnDir)
    {
        if (_despawnScheduled) return;

        _despawnScheduled = true;
        _despawnDir = despawnDir.sqrMagnitude > 0.0001f ? despawnDir.normalized : -_lastDir;

        if (_collider == null)
            _collider = GetComponent<Collider2D>();

        if (_collider != null)
            _collider.enabled = false;

        _despawnTimer = Mathf.Max(0.01f, despawnDelay);
    }

    private void ReturnSelf()
    {
        _initialized = false;
        _despawnScheduled = false;
        _pendingTarget = null;

        if (_collider != null)
            _collider.enabled = false;

        gameObject.SetActive(false);
        transform.SetParent(PoolRoot, false);

        if (!_pool.ContainsKey(_poolKey))
            _pool[_poolKey] = new Queue<RicochetShurikenProjectile2D>();

        _pool[_poolKey].Enqueue(this);
    }
}