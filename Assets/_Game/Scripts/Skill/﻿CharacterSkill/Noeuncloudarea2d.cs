using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뇌운 구름 오브젝트입니다.
/// 풀링으로 재사용되며, 고정 위치에서 틱 피해를 줍니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class NoeunCloudArea2D : PooledObject2D
{
    [Header("비주얼")]
    [Tooltip("구름 비주얼 컴포넌트입니다.")]
    [SerializeField] private Component visualBehaviour;

    [Tooltip("SpriteSkillVisual 기준 반경입니다.")]
    [SerializeField] private float visualBaseRadius = 1f;

    private ISkillVisual _visual;
    private LayerMask _enemyMask;
    private DamageElement2D _element;
    private int _tickDamage;
    private float _tickInterval;
    private float _radius;
    private float _duration;
    private float _age;
    private float _tickTimer;
    private bool _debugLog;
    private bool _returned;
    private Vector3 _spawnPosition;
    private Action<NoeunCloudArea2D> _onReturned;

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(32);

    private void Awake()
    {
        ResolveVisual();
    }

    private void OnDisable()
    {
        if (_visual != null)
            _visual.Stop();

        NotifyReturned();
    }

    public void BindReturnCallback(Action<NoeunCloudArea2D> onReturned)
    {
        _onReturned = onReturned;
    }

    public void Init(
        LayerMask enemyMask,
        DamageElement2D damageElement,
        int tickDamage,
        float tickInterval,
        float radius,
        float duration,
        Vector3 startPosition,
        bool enableLog)
    {
        _enemyMask = enemyMask;
        _element = damageElement;
        _tickDamage = Mathf.Max(1, tickDamage);
        _tickInterval = Mathf.Max(0.1f, tickInterval);
        _radius = Mathf.Max(0.1f, radius);
        _duration = Mathf.Max(0.1f, duration);
        _spawnPosition = startPosition;
        _debugLog = enableLog;
        _age = 0f;
        _tickTimer = _tickInterval;
        _returned = false;

        transform.SetParent(null, true);
        transform.position = _spawnPosition;

        ResolveVisual();
        if (_visual != null)
        {
            _visual.Play(_spawnPosition);
            _visual.UpdatePosition(_spawnPosition);
            _visual.UpdateScale(_radius / Mathf.Max(0.01f, visualBaseRadius));
        }
    }

    private void Update()
    {
        _age += Time.deltaTime;
        _tickTimer -= Time.deltaTime;

        if (_visual != null)
            _visual.UpdatePosition(_spawnPosition);

        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            TickDamage();
        }

        if (_age >= _duration)
            Despawn();
    }

    private void TickDamage()
    {
        // 구름 중심에서 반경 내 모든 적을 수집 (EnemyRegistry2D 직접 경유)
        _targets.Clear();
        float sqrRadius = _radius * _radius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;

        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D enemy = members[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            Vector2 delta = enemy.Position - (Vector2)_spawnPosition;
            if (delta.sqrMagnitude > sqrRadius) continue;

            _targets.Add(enemy);
        }

        if (_targets.Count == 0) return;

        int appliedCount = 0;
        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D enemy = _targets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            if (DamageUtil2D.TryApplyDamage(enemy.gameObject, _tickDamage, _element))
                appliedCount++;
        }

        _targets.Clear();

        if (_debugLog && appliedCount > 0)
            CombatLog.Log($"[뇌운] 틱 피해 {appliedCount}명 | dmg={_tickDamage}", this);
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

    private void Despawn()
    {
        if (_returned) return;

        transform.SetParent(null, true);
        NotifyReturned();
        ReturnToPool();
    }

    private void NotifyReturned()
    {
        if (_returned) return;
        _returned = true;
        _onReturned?.Invoke(this);
    }
}