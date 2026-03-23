// UTF-8
// Assets/_Game/Scripts/Combat/Skills/ThunderTalisman/ThunderStrikeArea2D.cs
using UnityEngine;

/// <summary>
/// 번개 범위 공격 + VFX 파티클 연출
///
/// Strike() 호출 → 범위 내 적 즉시 데미지 + VFX 파티클 재생 → 자동 비활성화
///
/// [변경 요약 — Animator→VFX 전환]
/// - Animator, SpriteRenderer 완전 제거
/// - VFXSpawner.Spawn()으로 번개 파티클 프리팹을 풀링 기반 생성
/// - VFX 크기는 vfxScaleMultiplier로 조절 (데미지 범위와 독립)
/// - VFX 수명은 VFXAutoReturn이 자동 관리 → 이 스크립트에서 관여하지 않음
/// </summary>
[DisallowMultipleComponent]
public sealed class ThunderStrikeArea2D : MonoBehaviour
{
    [Header("VFX 파티클")]
    [Tooltip("번개 이펙트 파티클 프리팹.\nVFXSpawner를 통해 풀링 관리됩니다.")]
    [SerializeField] private GameObject thunderVFXPrefab;

    [Tooltip("VFX 크기 배수. 1=원본, 2=2배.\n데미지 범위와 무관하게 비주얼만 조절합니다.")]
    [SerializeField, Min(0.1f)] private float vfxScaleMultiplier = 1f;

    [Header("타이밍")]
    [Tooltip("Strike 후 이 오브젝트가 비활성화되기까지의 시간(초).\nVFX 수명과 별개로, 데미지 판정 오브젝트의 재사용 대기 시간입니다.")]
    [SerializeField, Min(0.05f)] private float strikeDuration = 0.5f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private float _timer;
    private bool _active;
    private readonly Collider2D[] _hits = new Collider2D[64];
    private ContactFilter2D _enemyFilter;

    /// <summary>
    /// 마지막으로 사용한 반경 (기즈모 표시용)
    /// </summary>
    [SerializeField, Min(0.05f)] private float _lastRadius = 1.5f;

    /* ───────────── 공개 API ───────────── */

    /// <summary>
    /// 번개 소환: 현재 위치에서 범위 데미지 + VFX 파티클 생성
    /// </summary>
    /// <param name="radius">데미지 판정 반경 (VFX 크기와 별개)</param>
    /// <param name="damage">적용 피해량</param>
    /// <param name="mask">적 레이어 마스크</param>
    public void Strike(float radius, int damage, LayerMask mask)
    {
        gameObject.SetActive(true);

        radius = Mathf.Max(0.05f, radius);
        damage = Mathf.Max(1, damage);
        _lastRadius = radius;

        if (debugLog)
            Debug.Log($"[ThunderStrike] Strike pos={transform.position} radius={radius} " +
                      $"dmg={damage} mask={mask.value}");

        // ── 1. VFX 파티클 생성 ──
        SpawnVFX();

        // ── 2. 범위 데미지 (즉시 1회) ──
        ApplyAreaDamage(radius, damage, mask);

        // ── 3. 비활성화 타이머 시작 ──
        _timer = 0f;
        _active = true;
    }

    /* ───────────── 내부 로직 ───────────── */

    /// <summary>
    /// VFXSpawner를 통해 번개 파티클을 풀링 기반으로 생성합니다.
    /// VFX 수명 관리(반환)는 VFXAutoReturn이 담당합니다.
    /// </summary>
    private void SpawnVFX()
    {
        if (thunderVFXPrefab == null)
        {
            if (debugLog)
                Debug.LogWarning("[ThunderStrike] thunderVFXPrefab이 null! " +
                                 "Inspector에서 VFX 프리팹을 연결하세요.");
            return;
        }

        GameObject vfx = VFXSpawner.Spawn(
            thunderVFXPrefab,
            transform.position,
            Quaternion.identity
        );

        if (vfx == null)
        {
            if (debugLog)
                Debug.LogWarning("[ThunderStrike] VFXSpawner.Spawn()이 null 반환!");
            return;
        }

        // VFX 스케일 적용 (비주얼만, 데미지 범위와 무관)
        if (!Mathf.Approximately(vfxScaleMultiplier, 1f))
        {
            vfx.transform.localScale = Vector3.one * vfxScaleMultiplier;
        }

        if (debugLog)
            Debug.Log($"[ThunderStrike] VFX 생성 완료 scale={vfxScaleMultiplier}");
    }

    /// <summary>
    /// OverlapCircle로 범위 내 적에게 즉시 데미지를 적용합니다.
    /// </summary>
    private void ApplyAreaDamage(float radius, int damage, LayerMask mask)
    {
        // Unity 6: ContactFilter2D + 배열 오버로드 (deprecated NonAlloc 대체, GC 0)
        _enemyFilter.useLayerMask = true;
        _enemyFilter.layerMask = mask;
        _enemyFilter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            (Vector2)transform.position,
            radius,
            _enemyFilter,
            _hits
        );

        if (debugLog)
            Debug.Log($"[ThunderStrike] OverlapCircle hitCount={hitCount}");

        int damageApplied = 0;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            // ★ 낙뢰부는 전기 속성 — Electric 명시 (기존: Physical 기본값)
            bool success = DamageUtil2D.TryApplyDamage(col, damage, DamageElement2D.Electric);
            if (!success)
            {
                var rootCol = col.transform.root.GetComponent<Collider2D>();
                if (rootCol != null)
                    success = DamageUtil2D.TryApplyDamage(rootCol, damage, DamageElement2D.Electric);
            }

            if (debugLog)
                Debug.Log($"[ThunderStrike]   hit[{i}]={col.name} " +
                          $"layer={LayerMask.LayerToName(col.gameObject.layer)} " +
                          $"dmgApplied={success}");

            if (success) damageApplied++;
        }

        if (debugLog)
            Debug.Log($"[ThunderStrike] 총 데미지 적용={damageApplied}/{hitCount}");
    }

    /* ───────────── 라이프사이클 ───────────── */

    private void Update()
    {
        if (!_active) return;

        _timer += Time.deltaTime;
        if (_timer >= strikeDuration)
            Finish();
    }

    private void Finish()
    {
        _active = false;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _lastRadius);
    }
#endif
}