// UTF-8
using UnityEngine;

/// <summary>
/// 궁극기용 화살 비주얼 1개.
/// 콜라이더/데미지 없음 — 방향으로 날아가다가 타겟에 도달하면 소멸.
///
/// [v2 최적화]
/// - Destroy 대신 콜백(onReturn)으로 풀에 반환
/// - Reset() 메서드로 재사용 가능
/// - Update 내 불필요한 연산 최소화
/// </summary>
public sealed class UltimateArrowVisual2D : MonoBehaviour
{
    private Transform _target;
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _hitRadiusSqr; // ★ v2: 거리비교 sqrt 제거
    private float _age;
    private bool _initialized;

    /// <summary>풀 반환 콜백. 설정되면 Destroy 대신 이 콜백을 호출합니다.</summary>
    private System.Action<UltimateArrowVisual2D> _onReturn;

    /// <summary>
    /// 화살 초기화. 풀에서 꺼낸 뒤 호출.
    /// </summary>
    public void Init(Transform target, Vector2 direction, float speed, float lifetime,
                     float hitRadius = 0.5f, System.Action<UltimateArrowVisual2D> onReturn = null)
    {
        _target = target;
        _direction = direction.normalized;
        _speed = speed;
        _lifetime = lifetime;
        _hitRadiusSqr = hitRadius * hitRadius;
        _age = 0f;
        _initialized = true;
        _onReturn = onReturn;

        // 스프라이트 회전
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!_initialized) return;

        _age += Time.deltaTime;

        if (_age >= _lifetime)
        {
            ReturnOrDestroy();
            return;
        }

        // 전진
        Vector3 pos = transform.position;
        pos.x += _direction.x * _speed * Time.deltaTime;
        pos.y += _direction.y * _speed * Time.deltaTime;
        transform.position = pos;

        // 타겟 도달 체크 (sqrMagnitude로 sqrt 제거)
        if (_target != null)
        {
            float dx = pos.x - _target.position.x;
            float dy = pos.y - _target.position.y;
            if (dx * dx + dy * dy <= _hitRadiusSqr)
            {
                ReturnOrDestroy();
            }
        }
    }

    private void ReturnOrDestroy()
    {
        _initialized = false;

        if (_onReturn != null)
        {
            gameObject.SetActive(false);
            _onReturn.Invoke(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}