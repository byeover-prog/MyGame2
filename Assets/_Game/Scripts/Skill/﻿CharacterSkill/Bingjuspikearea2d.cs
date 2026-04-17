using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빙주 얼음 기둥 오브젝트입니다.
/// 예고 후 솟구치며, 반경 내 가장 가까운 적 1명에게 피해와 동상을 줍니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BingjuSpikeArea2D : PooledObject2D
{
    [Header("비주얼")]
    [Tooltip("빙주 비주얼 컴포넌트입니다.")]
    [SerializeField] private Component visualBehaviour;

    [Tooltip("SpriteSkillVisual 기준 반경입니다.")]
    [SerializeField] private float visualBaseRadius = 1f;

    [Tooltip("예고 시작 시 최소 스케일입니다.")]
    [SerializeField] private float minScale = 0.05f;

    private ISkillVisual _visual;
    private LayerMask _enemyMask;
    private DamageElement2D _element;
    private int _damage;
    private float _hitRadius;
    private float _armDelay;
    private float _lifetime;
    private float _age;
    private float _frostDuration;
    private float _frostSlowMultiplier;
    private bool _fired;
    private bool _debugLog;
    private Vector3 _impactPoint;

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(16);

    private void Awake()
    {
        ResolveVisual();
    }

    private void OnDisable()
    {
        if (_visual != null)
            _visual.Stop();
    }

    public void Init(
        LayerMask enemyMask,
        DamageElement2D damageElement,
        int damage,
        float hitRadius,
        float armDelay,
        float lifetime,
        Vector3 impactPoint,
        float frostDuration,
        float frostSlowMultiplier,
        bool enableLog)
    {
        _enemyMask = enemyMask;
        _element = damageElement;
        _damage = Mathf.Max(1, damage);
        _hitRadius = Mathf.Max(0.1f, hitRadius);
        _armDelay = Mathf.Max(0.05f, armDelay);
        _lifetime = Mathf.Max(_armDelay, lifetime);
        _impactPoint = impactPoint;
        _frostDuration = Mathf.Max(0f, frostDuration);
        _frostSlowMultiplier = frostSlowMultiplier;
        _fired = false;
        _age = 0f;
        _debugLog = enableLog;

        transform.SetParent(null, true);
        transform.position = _impactPoint;

        ResolveVisual();
        if (_visual != null)
        {
            _visual.Play(_impactPoint);
            _visual.UpdatePosition(_impactPoint);
            _visual.UpdateScale(minScale);
        }
    }

    private void Update()
    {
        _age += Time.deltaTime;

        if (_visual != null)
            _visual.UpdatePosition(_impactPoint);

        if (!_fired)
        {
            float t = Mathf.Clamp01(_age / _armDelay);
            float scale = Mathf.Lerp(minScale, _hitRadius / Mathf.Max(0.01f, visualBaseRadius), t);

            if (_visual != null)
                _visual.UpdateScale(scale);

            if (_age >= _armDelay)
            {
                Fire();
                _fired = true;

                if (_visual != null)
                    _visual.PlayImpact(_impactPoint);
            }
        }

        if (_age >= _lifetime)
            ReturnToPool();
    }

    private void Fire()
    {
        // 반경 내 적 수집
        _targets.Clear();
        float sqrR = _hitRadius * _hitRadius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;
        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D e = members[i];
            if (e == null || !e.IsValidTarget) continue;
            if ((e.Position - (Vector2)_impactPoint).sqrMagnitude > sqrR) continue;
            _targets.Add(e);
        }

        if (_targets.Count == 0) return;

        // 가장 가까운 적 1명 선정
        EnemyRegistryMember2D bestTarget = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D enemy = _targets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            float sqr = (enemy.Position - (Vector2)_impactPoint).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestTarget = enemy;
            }
        }

        _targets.Clear();

        if (bestTarget == null) return;

        // DamageUtil2D 경유 피해 적용
        bool applied = DamageUtil2D.TryApplyDamage(bestTarget.gameObject, _damage, _element);
        if (!applied) return;

        // IStatusReceiver 경유 동상 적용 (Reflection 없음)
        // EnemyFrostStatus2D가 IStatusReceiver를 구현하면 자동 동작
        StatusEffectInfo frostInfo = StatusEffectInfo.Frost(_frostDuration, _frostSlowMultiplier);
        IStatusReceiver[] receivers = bestTarget.GetComponentsInChildren<IStatusReceiver>(true);
        if (receivers != null)
        {
            for (int i = 0; i < receivers.Length; i++)
            {
                if (receivers[i] != null)
                    receivers[i].TryApplyStatus(frostInfo);
            }
        }

        if (_debugLog)
            CombatLog.Log($"[빙주] 적중 → {bestTarget.name} dmg={_damage}", this);
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