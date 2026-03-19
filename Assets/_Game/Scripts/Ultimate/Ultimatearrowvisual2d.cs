// UTF-8
using UnityEngine;

/// <summary>
/// 궁극기용 화살 비주얼 1개.
/// 콜라이더/데미지 없음 — 방향으로 날아가다가 타겟에 도달하면 소멸.
/// </summary>
public sealed class UltimateArrowVisual2D : MonoBehaviour
{
    private Transform _target;
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _hitRadius;
    private float _age;
    private bool _initialized;

    /// <summary>
    /// 화살 초기화.
    /// </summary>
    /// <param name="target">도달하면 소멸할 타겟</param>
    /// <param name="direction">비행 방향 (퍼짐 적용된 방향)</param>
    /// <param name="speed">비행 속도</param>
    /// <param name="lifetime">최대 수명 (타겟 못 닿아도 이 시간 후 소멸)</param>
    /// <param name="hitRadius">타겟 중심 이 거리 안에 들어오면 소멸</param>
    public void Init(Transform target, Vector2 direction, float speed, float lifetime, float hitRadius = 0.5f)
    {
        _target = target;
        _direction = direction.normalized;
        _speed = speed;
        _lifetime = lifetime;
        _hitRadius = hitRadius;
        _initialized = true;

        // 스프라이트 회전 (오른쪽 = 기본 전방)
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!_initialized) return;

        _age += Time.deltaTime;

        // 수명 초과 → 소멸
        if (_age >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 전진
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

        // 타겟 도달 체크 → 소멸
        if (_target != null)
        {
            float dist = Vector2.Distance(transform.position, _target.position);
            if (dist <= _hitRadius)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}