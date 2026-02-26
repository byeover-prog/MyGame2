// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 유도 이동으로 타겟을 향해 날아가며, 적중 시 "다음 타겟"으로 튕긴다.
// - 겹침 난리 방지: 직전 타겟은 우선 제외하고 다음 타겟을 찾는다(없으면 예외적으로 허용).
// - 튕김 횟수(bounceCount)는 "첫 적중 이후 추가로 튕길 수 있는 횟수" 기준이다.
[RequireComponent(typeof(Collider2D))]
public sealed class ShurikenProjectile2D : MonoBehaviour
{
    [Header("Runtime(ReadOnly)")]
    [SerializeField] private bool _initialized;

    private LayerMask _enemyMask;
    private float _searchRadius;
    private float _damage;
    private float _speed;
    private float _turnSpeedDeg;
    private int _bouncesLeft;
    private float _lifeTime;

    private Transform _target;
    private Vector2 _dir;
    private float _alive;

    private int _lastHitRootId;

    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    private void OnEnable()
    {
        _alive = 0f;
        _lastHitRootId = 0;
    }

    public void Init(
        LayerMask enemyMask,
        float searchRadius,
        float damage,
        float speed,
        float turnSpeedDeg,
        int bounceCount,
        float lifeTime,
        Vector2 startDir,
        Transform startTarget)
    {
        _enemyMask = enemyMask;
        _searchRadius = searchRadius;
        _damage = damage;
        _speed = speed;
        _turnSpeedDeg = turnSpeedDeg;
        _bouncesLeft = Mathf.Max(0, bounceCount);
        _lifeTime = Mathf.Max(0.1f, lifeTime);

        _dir = startDir.sqrMagnitude > 0.0001f ? startDir.normalized : Vector2.right;
        _target = startTarget;

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        if (Time.timeScale <= 0f) return;

        _alive += Time.deltaTime;
        if (_alive >= _lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 pos = transform.position;

        // 타겟이 없으면 재탐색
        if (_target == null)
        {
            if (!Targeting2D.TryGetClosestEnemy(pos, _searchRadius, _enemyMask, 0, out _target))
            {
                // 적이 없으면 그냥 진행하다가 수명 종료
                transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
                return;
            }
        }

        Vector2 desired = ((Vector2)_target.position - pos);
        if (desired.sqrMagnitude > 0.0001f)
        {
            desired.Normalize();

            // 회전 보간(도/초)
            float maxRad = _turnSpeedDeg * Mathf.Deg2Rad * Time.deltaTime;
            _dir = Vector2.Lerp(_dir, desired, Mathf.Clamp01(maxRad)).normalized;
        }

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (other == null) return;

        if (!DamageUtil2D.IsInLayerMask(other.gameObject, _enemyMask))
            return;

        // 데미지 적용
        DamageUtil2D.ApplyDamage(other, _damage);

        // 직전 적 기록(겹침 난리 방지용)
        _lastHitRootId = DamageUtil2D.GetRootInstanceId(other);

        // 다음 타겟 결정
        if (_bouncesLeft <= 0)
        {
            Destroy(gameObject);
            return;
        }

        _bouncesLeft--;

        Vector2 pos = transform.position;

        // 1) 직전 타겟 제외하고 가장 가까운 적
        if (!Targeting2D.TryGetClosestEnemy(pos, _searchRadius, _enemyMask, _lastHitRootId, out _target))
        {
            // 2) 다른 적이 없으면(보스 1마리 같은 경우) 직전 타겟도 허용
            if (!Targeting2D.TryGetClosestEnemy(pos, _searchRadius, _enemyMask, 0, out _target))
            {
                Destroy(gameObject);
                return;
            }
        }

        // 튕김 시작 방향 갱신(0벡터 방지)
        Vector2 desired = ((Vector2)_target.position - pos);
        if (desired.sqrMagnitude < 0.0001f)
            desired = _dir;

        _dir = desired.normalized;

        // 콜라이더 안에 박혀서 이벤트가 꼬이는 것 방지(살짝 앞으로 빼기)
        transform.position += (Vector3)(_dir * 0.08f);
    }
}