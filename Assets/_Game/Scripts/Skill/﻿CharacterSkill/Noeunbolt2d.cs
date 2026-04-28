using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뇌운 번개 투사체.
///   - NoeunCloud2D에서 호출되어 스폰 위치에서 즉시 AOE 데미지
///   - 짧은 VFX 표시 후 풀로 반환
///
/// 데미지 속성: Electric (전기) — 하율 패시브 트리거 (전파/체인)
/// 적 검색은 EnemyRegistry2D 사용.
/// </summary>
[DisallowMultipleComponent]
public sealed class NoeunBolt2D : PooledObject2D
{
    [Header("VFX 지속")]
    [Tooltip("번개 VFX 표시 시간(초). 짧을수록 빠르게 사라짐.")]
    [SerializeField] private float vfxDuration = 0.3f;

    [Header("VFX (선택)")]
    [Tooltip("자식 SpriteRenderer. 알파 페이드 적용.")]
    [SerializeField] private SpriteRenderer boltRenderer;

    // ── 런타임 상태 ──
    private DamageElement2D _element;
    private int _damage;
    private float _radius;
    private float _age;
    private bool _hasHit;
    private Color _baseSpriteColor;
    private bool _hasBaseSpriteColor;

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(32);

    private void Awake()
    {
        if (boltRenderer == null)
            boltRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (boltRenderer != null)
        {
            _baseSpriteColor = boltRenderer.color;
            _hasBaseSpriteColor = true;
        }
    }

    public void Initialize(int damage, float radius, DamageElement2D element)
    {
        _damage = Mathf.Max(1, damage);
        _radius = Mathf.Max(0.1f, radius);
        _element = element;
        _age = 0f;
        _hasHit = false;

        if (_hasBaseSpriteColor && boltRenderer != null)
            boltRenderer.color = _baseSpriteColor;
    }

    private void OnEnable()
    {
        _age = 0f;
        _hasHit = false;
    }

    private void OnDisable()
    {
        if (_hasBaseSpriteColor && boltRenderer != null)
            boltRenderer.color = _baseSpriteColor;
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // 첫 프레임에 즉시 데미지
        if (!_hasHit)
        {
            ApplyDamage();
            _hasHit = true;
        }

        // VFX 페이드
        if (_hasBaseSpriteColor && boltRenderer != null && vfxDuration > 0f)
        {
            float t = _age / vfxDuration;
            Color c = _baseSpriteColor;
            c.a *= Mathf.Clamp01(1f - t);
            boltRenderer.color = c;
        }

        // 수명 만료
        if (_age >= vfxDuration)
        {
            ReturnToPool();
        }
    }

    private void ApplyDamage()
    {
        // EnemyRegistry2D O(N)
        _targets.Clear();
        Vector2 center = transform.position;
        float sqrR = _radius * _radius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;

        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D enemy = members[i];
            if (enemy == null || !enemy.IsValidTarget) continue;
            if ((enemy.Position - center).sqrMagnitude > sqrR) continue;
            _targets.Add(enemy);
        }

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D enemy = _targets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            // Electric 속성 → 하율 패시브의 전파 효과 트리거
            DamageUtil2D.TryApplyDamage(enemy.gameObject, _damage, _element);
        }

        _targets.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _radius > 0f ? _radius : 1f);
    }
#endif
}