using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 뇌운 번개 투사체.
///   - NoeunCloud2D에서 호출되어 스폰 위치에서 즉시 AOE 데미지
///   - 짧은 VFX 표시 후 풀로 반환
///
/// 데미지 속성: Electric (전기)
/// 하율 패시브와 시너지: 전파/체인 효과 트리거
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
    private int _damage;
    private float _radius;
    private LayerMask _enemyMask;
    private float _age;
    private bool _hasHit;

    private static readonly Collider2D[] s_buffer = new Collider2D[32];

    private void Awake()
    {
        if (boltRenderer == null)
            boltRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void Initialize(int damage, float radius, LayerMask enemyMask)
    {
        _damage = damage;
        _radius = Mathf.Max(0.1f, radius);
        _enemyMask = enemyMask;
        _age = 0f;
        _hasHit = false;

        if (boltRenderer != null)
        {
            Color c = boltRenderer.color;
            c.a = 1f;
            boltRenderer.color = c;
        }
    }

    private void OnEnable()
    {
        _age = 0f;
        _hasHit = false;
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

        // VFX 페이드 + 수명
        if (boltRenderer != null && vfxDuration > 0f)
        {
            float t = _age / vfxDuration;
            Color c = boltRenderer.color;
            c.a = Mathf.Clamp01(1f - t);
            boltRenderer.color = c;
        }

        if (_age >= vfxDuration)
        {
            ReturnToPool();
        }
    }

    private void ApplyDamage()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_enemyMask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            (Vector2)transform.position, _radius, filter, s_buffer);

        if (hitCount == 0) return;

        var hitIds = new HashSet<int>();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = s_buffer[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (!hitIds.Add(rootId)) continue;

            // Electric 속성 — 하율 패시브의 전파 효과 트리거
            DamageUtil2D.TryApplyDamage(col, _damage, DamageElement2D.Electric);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _radius > 0f ? _radius : 1f);
    }
#endif
}