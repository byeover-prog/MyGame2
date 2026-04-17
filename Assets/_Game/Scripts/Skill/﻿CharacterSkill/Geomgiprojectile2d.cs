using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 검기 투사체입니다.
/// 관통하며, 같은 적에게는 한 번만 피해를 줍니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class GeomgiProjectile2D : PooledObject2D
{
    [Header("비주얼")]
    [Tooltip("검기 비주얼 컴포넌트입니다.")]
    [SerializeField] private Component visualBehaviour;

    [Tooltip("스프라이트 회전 보정입니다.")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [Header("컴포넌트")]
    [Tooltip("이동용 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private ISkillVisual _visual;
    private LayerMask _enemyMask;
    private DamageElement2D _element;
    private int _damage;
    private float _speed;
    private float _lifetime;
    private float _age;
    private Vector2 _direction;

    private readonly HashSet<int> _hitIds = new HashSet<int>(32);

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        ResolveVisual();
    }

    private void OnEnable()
    {
        _age = 0f;
        _hitIds.Clear();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        if (_visual != null)
            _visual.Stop();

        _hitIds.Clear();
    }

    public void Init(
        LayerMask enemyMask,
        DamageElement2D damageElement,
        int damage,
        float speed,
        float lifetime,
        Vector2 direction)
    {
        _enemyMask = enemyMask;
        _element = damageElement;
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.1f, lifetime);
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _age = 0f;
        _hitIds.Clear();

        // 풀에서 꺼낼 때 ProjectilePool2D가 이미 부모를 null로 만들지만, 재보장
        transform.SetParent(null, true);

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        ResolveVisual();
        if (_visual != null)
        {
            _visual.Play(transform.position);
            _visual.UpdatePosition(transform.position);
        }

        if (rb != null)
            rb.linearVelocity = _direction * _speed;
    }

    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        if (rb != null)
            rb.linearVelocity = _direction * _speed;
        else
            transform.position += (Vector3)(_direction * (_speed * Time.fixedDeltaTime));

        if (_visual != null)
            _visual.UpdatePosition(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        int rootId = DamageUtil2D.GetRootId(other);
        if (!_hitIds.Add(rootId))
            return;

        bool applied = DamageUtil2D.TryApplyDamage(other, _damage, _element);
        if (!applied)
        {
            // 적 콜라이더가 자식에 있는 경우 루트 쪽으로 재시도
            Collider2D rootCol = other.GetComponentInParent<Collider2D>();
            if (rootCol != null && rootCol != other)
                applied = DamageUtil2D.TryApplyDamage(rootCol, _damage, _element);
        }

        if (debugLog && applied)
            CombatLog.Log($"[검기] 적중 | id={rootId} dmg={_damage}", this);
    }

    private void ResolveVisual()
    {
        if (visualBehaviour is ISkillVisual cachedVisual)
        {
            _visual = cachedVisual;
            return;
        }

        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISkillVisual found)
            {
                _visual = found;
                visualBehaviour = behaviours[i];
                return;
            }
        }

        _visual = null;
    }
}