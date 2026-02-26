// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 관통형: 충돌 데미지 없음(Trigger/OnHit 사용 안 함).
// - 시작 위치에서 일정 거리 도달 시 폭발(AOE) 후, 세대가 남아있으면 V자로 2개 분열한다.
// - 분열은 반드시 "현재 투사체 위치"에서 이루어진다(플레이어 위치 분열 버그 제거).
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    private LayerMask _enemyMask;
    private float _damage;
    private float _speed;
    private float _travelDistance;
    private float _explosionRadius;
    private float _splitAngleDeg;

    private int _maxSplitGen;
    private int _gen;

    private Vector2 _dir;
    private Vector2 _startPos;
    private bool _initialized;

    // 폭발시 적 탐색 버퍼
    private static readonly Collider2D[] _buffer = new Collider2D[128];

    public void Init(
        LayerMask enemyMask,
        float damage,
        float speed,
        float travelDistance,
        float explosionRadius,
        float splitAngleDeg,
        int maxSplitGen,
        int currentGen,
        Vector2 startDir)
    {
        _enemyMask = enemyMask;
        _damage = damage;
        _speed = speed;
        _travelDistance = travelDistance;
        _explosionRadius = explosionRadius;
        _splitAngleDeg = splitAngleDeg;

        _maxSplitGen = Mathf.Max(0, maxSplitGen);
        _gen = Mathf.Max(0, currentGen);

        _dir = startDir.sqrMagnitude > 0.0001f ? startDir.normalized : Vector2.right;
        _startPos = transform.position;

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        if (Time.timeScale <= 0f) return;

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

        float dist = ((Vector2)transform.position - _startPos).magnitude;
        if (dist >= _travelDistance)
        {
            ExplodeAndSplit();
        }
    }

    private void ExplodeAndSplit()
    {
        Vector2 center = transform.position;

        // 1) 폭발 데미지(AOE)
        int count = Physics2D.OverlapCircleNonAlloc(center, _explosionRadius, _buffer, _enemyMask);
        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (col == null) continue;
            DamageUtil2D.ApplyDamage(col, _damage);
        }

        // 2) 분열(세대 남아있으면 V자 2개)
        if (_gen < _maxSplitGen)
        {
            int nextGen = _gen + 1;

            Vector2 dirA = Rotate(_dir, -_splitAngleDeg);
            Vector2 dirB = Rotate(_dir, +_splitAngleDeg);

            // 같은 프리팹을 복제해서 세대만 올려서 재귀적으로 진행
            var a = Instantiate(this, center, Quaternion.identity);
            a.Init(_enemyMask, _damage, _speed, _travelDistance, _explosionRadius, _splitAngleDeg, _maxSplitGen, nextGen, dirA);

            var b = Instantiate(this, center, Quaternion.identity);
            b.Init(_enemyMask, _damage, _speed, _travelDistance, _explosionRadius, _splitAngleDeg, _maxSplitGen, nextGen, dirB);
        }

        Destroy(gameObject);
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs).normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_initialized) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
#endif
}