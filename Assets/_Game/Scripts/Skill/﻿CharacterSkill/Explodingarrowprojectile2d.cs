using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 폭발 부착형 화살 투사체입니다.
/// 적에게 "부착"되면 부모화 없이 해당 적 위치를 따라다니다 지연 폭발합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class ExplodingArrowProjectile2D : PooledObject2D
{
    private enum ArrowState
    {
        Flying,
        Attached,
        Exploding,
        Dead
    }

    [Header("비주얼")]
    [Tooltip("화살 비주얼 컴포넌트입니다.")]
    [SerializeField] private Component visualBehaviour;

    [Tooltip("SpriteSkillVisual 기준 반경입니다.")]
    [SerializeField] private float visualBaseRadius = 1f;

    [Tooltip("스프라이트 회전 보정입니다.")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [Header("추적")]
    [Tooltip("비행 중 목표 방향으로 꺾이는 정도입니다.")]
    [SerializeField] private float homingTurnSpeed = 12f;

    [Tooltip("폭발 임팩트 뒤 반환까지 대기 시간입니다.")]
    [SerializeField] private float impactReturnDelay = 0.08f;

    [Header("부착 위치")]
    [Tooltip("부착 시 적 위치 대비 오프셋입니다. (박힌 화살처럼 약간 위로)")]
    [SerializeField] private Vector2 attachOffset = new Vector2(0f, 0.3f);

    [Header("컴포넌트")]
    [Tooltip("이동용 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("피격용 Collider2D입니다.")]
    [SerializeField] private Collider2D hitCollider;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private ISkillVisual _visual;
    private LayerMask _enemyMask;
    private DamageElement2D _element;
    private int _explosionDamage;
    private float _speed;
    private float _lifetime;
    private float _attachDelay;
    private float _explosionRadius;
    private float _age;
    private float _attachTimer;
    private Vector2 _direction;
    private Transform _preferredTarget;
    private Transform _attachedTarget;
    private IDamageable2D _attachedDamageable;
    private ArrowState _state;
    private bool _returned;

    private Action<ExplodingArrowProjectile2D> _onReturned;
    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(32);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (hitCollider == null) hitCollider = GetComponent<Collider2D>();
        ResolveVisual();
    }

    private void OnEnable()
    {
        _age = 0f;
        _attachTimer = 0f;
        _state = ArrowState.Flying;
        _returned = false;
        _attachedTarget = null;
        _attachedDamageable = null;
    }

    private void OnDisable()
    {
        if (_visual != null)
            _visual.Stop();

        NotifyReturned();
    }

    public void BindReturnCallback(Action<ExplodingArrowProjectile2D> onReturned)
    {
        _onReturned = onReturned;
    }

    public void Init(
        LayerMask enemyMask,
        DamageElement2D damageElement,
        int explosionDamage,
        float speed,
        float lifetime,
        float attachDelay,
        float explosionRadius,
        Vector2 direction,
        Transform preferredTarget)
    {
        _enemyMask = enemyMask;
        _element = damageElement;
        _explosionDamage = Mathf.Max(1, explosionDamage);
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.1f, lifetime);
        _attachDelay = Mathf.Max(0.05f, attachDelay);
        _explosionRadius = Mathf.Max(0.1f, explosionRadius);
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _preferredTarget = preferredTarget;
        _age = 0f;
        _attachTimer = _attachDelay;
        _state = ArrowState.Flying;
        _returned = false;
        _attachedTarget = null;
        _attachedDamageable = null;

        if (hitCollider != null) hitCollider.enabled = true;
        if (rb != null) rb.linearVelocity = _direction * _speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        ResolveVisual();
        if (_visual != null)
        {
            _visual.Play(transform.position);
            _visual.UpdatePosition(transform.position);
            _visual.UpdateScale(1f);
        }
    }

    private void Update()
    {
        _age += Time.deltaTime;

        if (_state == ArrowState.Flying)
        {
            if (_age >= _lifetime)
            {
                DespawnImmediate();
                return;
            }

            if (_visual != null)
                _visual.UpdatePosition(transform.position);

            UpdateFlyingDirection();
        }
        else if (_state == ArrowState.Attached)
        {
            if (!IsAttachedTargetAlive())
            {
                ExplodeImmediate(playImpact: true, returnToPoolAfter: true);
                return;
            }

            // 적 위치 + offset으로 따라감
            Vector3 followPos = _attachedTarget.position + (Vector3)attachOffset;
            transform.position = followPos;

            if (_visual != null)
                _visual.UpdatePosition(followPos);

            _attachTimer -= Time.deltaTime;
            if (_attachTimer <= 0f)
                ExplodeImmediate(playImpact: true, returnToPoolAfter: true);
        }
    }

    private void FixedUpdate()
    {
        if (_state != ArrowState.Flying)
            return;

        if (rb != null)
            rb.linearVelocity = _direction * _speed;
        else
            transform.position += (Vector3)(_direction * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != ArrowState.Flying) return;
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        AttachTo(other);
    }

    private void UpdateFlyingDirection()
    {
        if (_preferredTarget == null) return;
        if (!_preferredTarget.gameObject.activeInHierarchy) { _preferredTarget = null; return; }

        Vector2 toTarget = (Vector2)_preferredTarget.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f) return;

        Vector2 desired = toTarget.normalized;
        _direction = Vector2.Lerp(_direction, desired, homingTurnSpeed * Time.deltaTime).normalized;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void AttachTo(Collider2D targetCollider)
    {
        _state = ArrowState.Attached;
        _attachTimer = _attachDelay;

        // 적 루트 캐싱 (SetParent는 하지 않음)
        _attachedTarget = targetCollider.transform.root;
        _attachedDamageable = targetCollider.GetComponentInParent<IDamageable2D>();
        _preferredTarget = _attachedTarget;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (hitCollider != null)
            hitCollider.enabled = false;

        // 초기 부착 위치 세팅
        Vector3 initPos = _attachedTarget.position + (Vector3)attachOffset;
        transform.position = initPos;

        if (_visual != null)
        {
            _visual.UpdatePosition(initPos);
            _visual.UpdateScale(1f);
        }

        if (debugLog)
            CombatLog.Log($"[폭발화살] 부착 → {_attachedTarget.name}", this);
    }

    private bool IsAttachedTargetAlive()
    {
        if (_attachedTarget == null) return false;
        if (!_attachedTarget.gameObject.activeInHierarchy) return false;
        if (_attachedDamageable != null && _attachedDamageable.IsDead) return false;
        return true;
    }

    private void ExplodeImmediate(bool playImpact, bool returnToPoolAfter)
    {
        if (_state == ArrowState.Dead || _state == ArrowState.Exploding)
            return;

        _state = ArrowState.Exploding;
        Vector3 explosionCenter = transform.position;
        _targets.Clear();
        float sqrR = _explosionRadius * _explosionRadius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;
        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D e = members[i];
            if (e == null || !e.IsValidTarget) continue;
            if ((e.Position - (Vector2)explosionCenter).sqrMagnitude > sqrR) continue;
            _targets.Add(e);
        }

        int appliedCount = 0;
        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D e = _targets[i];
            if (e == null || !e.IsValidTarget) continue;

            if (DamageUtil2D.TryApplyDamage(e.gameObject, _explosionDamage, _element))
                appliedCount++;
        }

        _targets.Clear();

        if (_visual != null)
        {
            _visual.Play(explosionCenter);
            _visual.UpdatePosition(explosionCenter);
            _visual.UpdateScale(_explosionRadius / Mathf.Max(0.01f, visualBaseRadius));

            if (playImpact)
                _visual.PlayImpact(explosionCenter);
        }

        if (debugLog)
            CombatLog.Log($"[폭발화살] 폭발 {appliedCount}명 | dmg={_explosionDamage}", this);

        if (returnToPoolAfter)
            StartCoroutine(ReturnAfterImpact());
    }

    private IEnumerator ReturnAfterImpact()
    {
        yield return new WaitForSeconds(impactReturnDelay);
        DespawnImmediate();
    }

    private void DespawnImmediate()
    {
        if (_returned) return;

        _state = ArrowState.Dead;
        NotifyReturned();
        ReturnToPool();
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

    private void NotifyReturned()
    {
        if (_returned) return;
        _returned = true;
        _onReturned?.Invoke(this);
    }
}