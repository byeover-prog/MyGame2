using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 열참 참격 판정 오브젝트입니다.
/// 플레이어 중심 원형 360° 판정.
/// 외곽 링 적중 = 추가 데미지, 내부 적중 = 감소 데미지 (LOL 다리우스 Q 메카닉).
/// 각성 시 외곽 적에게 출혈(DoT) 부여.
///
/// 참고: 적 검색은 EnemyRegistry2D O(N) 사용 (Physics 쿼리 X).
/// </summary>
[DisallowMultipleComponent]
public sealed class YeolchamSlash2D : PooledObject2D
{
    [Header("히트 타이밍")]
    [Tooltip("스폰 후 데미지 판정까지 지연 시간(초). VFX 시작과 동기화용.")]
    [SerializeField] private float hitDelay = 0.1f;

    [Header("베기 VFX")]
    [Tooltip("열참 VFX 프리팹입니다. 비워두면 VFX 없이 작동.")]
    [SerializeField] private GameObject slashVfxPrefab;

    [Tooltip("VFX 스케일 보정. 1.0 = 원본 크기.")]
    [SerializeField] private float vfxScaleMultiplier = 1.0f;

    // ── 런타임 상태 ──
    private int _baseDamage;
    private float _radius;
    private float _lifetime;
    private Transform _owner;
    private DamageElement2D _element;

    private float _outerRingRatio;
    private float _outerMultiplier;
    private float _innerMultiplier;

    private bool _awakened;
    private float _bleedDuration;
    private float _bleedDpsRatio;

    private float _timer;
    private bool _hasHit;
    private GameObject _activeVfx;

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(32);
    private readonly HashSet<int> _hitIds = new HashSet<int>(32);

    /// <summary>무기에서 호출. 모든 파라미터 주입.</summary>
    public void Initialize(
        int damage,
        float radius,
        float lifetime,
        Transform owner,
        DamageElement2D element,
        float outerRingRatio,
        float outerMultiplier,
        float innerMultiplier,
        bool awakened,
        float bleedDuration,
        float bleedDpsRatio)
    {
        _baseDamage = damage;
        _radius = Mathf.Max(0.1f, radius);
        _lifetime = Mathf.Max(0.1f, lifetime);
        _owner = owner;
        _element = element;

        _outerRingRatio = Mathf.Clamp(outerRingRatio, 0.1f, 0.95f);
        _outerMultiplier = outerMultiplier;
        _innerMultiplier = innerMultiplier;

        _awakened = awakened;
        _bleedDuration = bleedDuration;
        _bleedDpsRatio = bleedDpsRatio;

        _timer = 0f;
        _hasHit = false;
        _hitIds.Clear();

        SpawnSlashVfx();
    }

    private void SpawnSlashVfx()
    {
        if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
        if (slashVfxPrefab == null || _owner == null) return;

        Vector3 spawnPos = _owner.position;
        _activeVfx = Instantiate(slashVfxPrefab, spawnPos, Quaternion.identity);

        if (!Mathf.Approximately(vfxScaleMultiplier, 1f))
            _activeVfx.transform.localScale = Vector3.one * vfxScaleMultiplier;

        Destroy(_activeVfx, _lifetime + 0.5f);
    }

    private void OnEnable()
    {
        _timer = 0f;
        _hasHit = false;
        _hitIds.Clear();
    }

    private void OnDisable()
    {
        if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 플레이어 + VFX 위치 추적
        if (_owner != null)
        {
            transform.position = _owner.position;
            if (_activeVfx != null)
                _activeVfx.transform.position = _owner.position;
        }

        // hitDelay 후 1회 판정
        if (!_hasHit && _timer >= hitDelay)
        {
            PerformSlashDamage();
            _hasHit = true;
        }

        // 수명 만료
        if (_timer >= _lifetime)
        {
            if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
            _owner = null;
            ReturnToPool();
        }
    }

    // ── 차등 데미지 판정 (EnemyRegistry2D 사용) ──

    private void PerformSlashDamage()
    {
        if (_owner == null) return;

        Vector2 center = _owner.position;
        float innerRadius = _radius * _outerRingRatio;
        float innerRadiusSqr = innerRadius * innerRadius;
        float outerRadiusSqr = _radius * _radius;

        _targets.Clear();
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;
        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D e = members[i];
            if (e == null || !e.IsValidTarget) continue;
            if ((e.Position - center).sqrMagnitude > outerRadiusSqr) continue;
            _targets.Add(e);
        }

        if (_targets.Count == 0) return;

        int outerHits = 0;
        int innerHits = 0;

        StatusEffectInfo bleedInfo = default;
        bool useBleed = _awakened && _bleedDuration > 0f;
        if (useBleed)
        {
            int bleedDps = Mathf.Max(1, Mathf.RoundToInt(_baseDamage * _bleedDpsRatio));
            bleedInfo = StatusEffectInfo.Bleed(_bleedDuration, bleedDps);
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D enemy = _targets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            // 중복 히트 방지
            int rootId = enemy.gameObject.GetInstanceID();
            if (!_hitIds.Add(rootId)) continue;

            // 외곽/내부 분류
            float distSqr = (enemy.Position - center).sqrMagnitude;
            bool isOuter = distSqr > innerRadiusSqr;
            float multiplier = isOuter ? _outerMultiplier : _innerMultiplier;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(_baseDamage * multiplier));

            if (DamageUtil2D.TryApplyDamage(enemy.gameObject, finalDamage, _element))
            {
                if (isOuter)
                {
                    outerHits++;

                    // 각성: 외곽 적에게 출혈 부여
                    if (useBleed)
                    {
                        IStatusReceiver[] receivers = enemy.GetComponentsInChildren<IStatusReceiver>(true);
                        if (receivers != null)
                        {
                            for (int j = 0; j < receivers.Length; j++)
                            {
                                if (receivers[j] != null)
                                    receivers[j].TryApplyStatus(bleedInfo);
                            }
                        }
                    }
                }
                else
                {
                    innerHits++;
                }
            }
        }

        _targets.Clear();

        if (outerHits > 0 || innerHits > 0)
        {
            CombatLog.Log(
                $"[열참] 적중! 외곽={outerHits}({_outerMultiplier:P0}) 내부={innerHits}({_innerMultiplier:P0})",
                this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 ownerPos = _owner != null ? _owner.position : transform.position;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(ownerPos, _radius > 0f ? _radius : 3f);

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(ownerPos, (_radius > 0f ? _radius : 3f) * _outerRingRatio);
    }
#endif
}