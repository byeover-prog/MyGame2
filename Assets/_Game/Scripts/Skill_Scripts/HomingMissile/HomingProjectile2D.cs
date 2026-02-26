// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 타겟을 계속 유도해서 따라간다.
// - 적중 시 데미지 + 남은 타격 횟수 감소, 0이면 종료.
// - 적중 직후 아주 짧게 밀어내어 콜라이더 내부 박힘/중복 판정 꼬임을 줄인다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HomingMissileProjectile2D : MonoBehaviour
{
    private LayerMask _enemyMask;
    private float _searchRadius;
    private float _damage;
    private float _speed;
    private float _turnSpeedDeg;
    private int _hitsLeft;
    private float _lifeTime;

    private Transform _target;
    private Vector2 _dir;
    private float _alive;
    private bool _initialized;

    private float _postHitTimer;
    private Vector2 _postHitDir;

    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true; // 투사체는 트리거 권장
    }

    private void OnEnable()
    {
        _alive = 0f;
        _postHitTimer = 0f;
    }

    public void Init(
        LayerMask enemyMask,
        float searchRadius,
        float damage,
        float speed,
        float turnSpeedDeg,
        int totalHits,
        float lifeTime,
        Vector2 startDir,
        Transform startTarget)
    {
        _enemyMask = enemyMask;
        _searchRadius = searchRadius;
        _damage = damage;
        _speed = speed;
        _turnSpeedDeg = turnSpeedDeg;
        _hitsLeft = Mathf.Max(1, totalHits);
        _lifeTime = Mathf.Max(0.2f, lifeTime);

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

        // 적중 직후 잠깐은 "밀려나기"로 탈출(박힘 방지)
        if (_postHitTimer > 0f)
        {
            _postHitTimer -= Time.deltaTime;
            transform.position += (Vector3)(_postHitDir * _speed * Time.deltaTime);
            return;
        }

        // 타겟이 없으면 재탐색(투사체 기준)
        if (_target == null)
        {
            if (!Targeting2D.TryGetClosestEnemy(pos, _searchRadius, _enemyMask, 0, out _target))
            {
                transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
                return;
            }
        }

        Vector2 desired = (Vector2)_target.position - pos;
        if (desired.sqrMagnitude > 0.0001f)
        {
            desired.Normalize();
            _dir = Steering2D.TurnTowards(_dir, desired, _turnSpeedDeg, Time.deltaTime);
        }

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (other == null) return;

        // ✅ 레이어마스크 체크(프로젝트 유틸 의존 제거)
        if (!IsInLayerMask(other.gameObject.layer, _enemyMask))
            return;

        // ✅ 데미지 적용(프로젝트별 Health/TakeDamage 구현 차이 흡수)
        TryApplyDamage(other.gameObject, _damage);

        _hitsLeft--;
        if (_hitsLeft <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 적중 직후 탈출(콜라이더 내부 박힘 방지)
        Vector2 pos = transform.position;
        Vector2 hitCenter = other.bounds.center;
        Vector2 away = pos - hitCenter;
        if (away.sqrMagnitude < 0.0001f) away = -_dir;

        _postHitDir = away.normalized;
        _postHitTimer = 0.06f;

        // 다음 타겟(투사체 기준 가장 가까운 적)
        Targeting2D.TryGetClosestEnemy(pos, _searchRadius, _enemyMask, 0, out _target);
    }

    // ----------------------------
    // 내부 유틸(외부 DamageUtil2D 의존 제거)
    // ----------------------------
    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        int bit = 1 << layer;
        return (mask.value & bit) != 0;
    }

    private static void TryApplyDamage(GameObject target, float damage)
    {
        if (target == null) return;

        // 1) 흔한 패턴: TakeDamage(int)
        target.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);

        // 2) 흔한 패턴: TakeDamage(float)
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // 3) 다른 이름 패턴도 최소 커버(프로젝트마다 다름)
        target.SendMessage("ApplyDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
        target.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}