// [구현 원리]
// PooledObject2D를 상속받는 참격 판정 오브젝트.
// 활성화(OnEnable) 시 잠깐 대기(hitDelay) 후 OverlapCircle로
// 범위 내 모든 적에게 DamageUtil2D.TryApplyDamage()를 호출.
// 수명(lifetime)이 끝나면 풀로 반환.
//
// [왜 OnTriggerEnter2D를 안 쓰는가?]
// Unity 물리 규칙상, 이미 겹쳐있는 콜라이더에는 OnTriggerEnter2D가
// 발동하지 않는다. 참격은 적 위치에 스폰되므로 처음부터 겹쳐있을 수 있어서
// Physics2D.OverlapCircle로 능동적으로 탐색해야 한다.
//
// [VFX]
// 프리팹 자체에 ParticleSystem이나 SpriteRenderer 애니메이션을 넣어두면
// 활성화 시 자동 재생된다. 별도 VFX 스폰은 불필요.
// 추가 히트 VFX가 필요하면 hitVfxPrefab 슬롯에 연결.
// ============================================================================
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 좌격요세 참격 판정 오브젝트.
/// 활성화 시 범위 내 모든 적에게 음(Dark) 속성 데미지를 넣고 풀로 반환.
/// </summary>
[DisallowMultipleComponent]
public sealed class JwagyeokYoseSlash2D : PooledObject2D
{
    // ══════════════════════════════════════════════════════
    //  Inspector
    // ══════════════════════════════════════════════════════

    [Header("히트 타이밍")]
    [Tooltip("활성화 후 데미지 판정까지의 대기 시간 (초).\n참격 VFX 시작 타이밍에 맞추기 위해 사용.")]
    [SerializeField] private float hitDelay = 0.1f;

    [Header("히트 VFX (선택)")]
    [Tooltip("적 피격 시 스폰할 추가 VFX 프리팹. 없으면 비워둬도 됨.")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Tooltip("히트 VFX 수명 (초)")]
    [SerializeField] private float hitVfxLifetime = 0.5f;

    // ══════════════════════════════════════════════════════
    //  런타임 (Initialize로 주입)
    // ══════════════════════════════════════════════════════

    private int _damage;
    private float _radius;
    private float _lifetime;
    private LayerMask _enemyMask;

    private float _timer;
    private bool _hasHit;
    private ContactFilter2D _filter;

    // GC 0 탐색 버퍼
    private readonly List<Collider2D> _hitBuffer = new(16);
    // 중복 히트 방지용 HashSet
    private readonly HashSet<int> _hitIds = new(16);

    // ══════════════════════════════════════════════════════
    //  초기화 (JwagyeokYoseWeapon2D에서 호출)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 무기에서 참격 오브젝트를 초기화한다.
    /// 풀에서 꺼낸 직후, SetActive(true) 전에 호출할 것.
    /// </summary>
    /// <param name="damage">피해량</param>
    /// <param name="radius">참격 판정 반경</param>
    /// <param name="lifetime">참격 총 수명 (초)</param>
    /// <param name="enemyMask">적 레이어마스크</param>
    public void Initialize(int damage, float radius, float lifetime, LayerMask enemyMask)
    {
        _damage   = damage;
        _radius   = radius;
        _lifetime = lifetime;
        _enemyMask = enemyMask;

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyMask);
        _filter.useLayerMask = true;
        _filter.useTriggers  = true;

        _timer  = 0f;
        _hasHit = false;
        _hitIds.Clear();
    }

    // ══════════════════════════════════════════════════════
    //  라이프사이클
    // ══════════════════════════════════════════════════════

    private void OnEnable()
    {
        _timer  = 0f;
        _hasHit = false;
        _hitIds.Clear();
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // hitDelay 이후 1회 판정
        if (!_hasHit && _timer >= hitDelay)
        {
            PerformSlashDamage();
            _hasHit = true;
        }

        // 수명 만료 → 풀 반환
        if (_timer >= _lifetime)
        {
            ReturnToPool();
        }
    }

    // ══════════════════════════════════════════════════════
    //  데미지 판정
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// OverlapCircle로 범위 내 모든 적을 탐색하고
    /// DamageUtil2D.TryApplyDamage()로 데미지를 넣는다.
    /// </summary>
    private void PerformSlashDamage()
    {
        Vector2 center = transform.position;
        _hitBuffer.Clear();

        int count = Physics2D.OverlapCircle(center, _radius, _filter, _hitBuffer);
        if (count == 0) return;

        int hitCount = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null) continue;

            // 루트 ID로 중복 히트 방지
            int rootId = DamageUtil2D.GetRootId(col);
            if (!_hitIds.Add(rootId)) continue;

            // 데미지 적용 — 반드시 DamageUtil2D 경유 (팝업 + 속성 VFX)
            bool applied = DamageUtil2D.TryApplyDamage(col, _damage, DamageElement2D.Dark);

            if (applied)
            {
                hitCount++;

                // 히트 VFX 스폰 (선택)
                if (hitVfxPrefab != null)
                {
                    SpawnHitVfx(col.transform.position);
                }
            }
        }

#if UNITY_EDITOR
        if (hitCount > 0)
        {
            Debug.Log($"[좌격요세] 참격 적중! — {hitCount}명 피해량={_damage}");
        }
#endif
    }

    /// <summary>
    /// 피격 위치에 히트 VFX를 스폰한다.
    /// 간단한 Instantiate + Destroy 방식. VFX가 무거우면 VFXSpawner로 교체 권장.
    /// </summary>
    private void SpawnHitVfx(Vector3 position)
    {
        GameObject vfx = Instantiate(hitVfxPrefab, position, Quaternion.identity);
        Destroy(vfx, hitVfxLifetime);
    }

    // ══════════════════════════════════════════════════════
    //  기즈모
    // ══════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _radius > 0f ? _radius : 2f);
    }
#endif
}