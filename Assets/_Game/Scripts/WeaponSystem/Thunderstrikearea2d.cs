// UTF-8
using UnityEngine;

/// <summary>
/// 번개 범위 공격 + 애니메이션
///
/// Strike() 호출 → 범위 내 적 즉시 데미지 + 애니메이션 재생 → 자동 비활성화
///
/// [변경 요약]
/// - 비주얼 스케일은 visualRoot에만 적용(루트 스케일 건드리지 않음)
/// - 마지막 radius를 저장해서 Gizmo가 실제 반경으로 표시되게 함
/// </summary>
[DisallowMultipleComponent]
public sealed class ThunderStrikeArea2D : MonoBehaviour
{
    [Header("연출")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Strike";
    [SerializeField, Min(0.05f)] private float thunderDuration = 0.5f;

    [Header("스케일(비주얼만)")]
    [Tooltip("번개 이미지(Animator가 달린 쪽)의 루트 Transform.\n비우면 Animator의 Transform을 사용합니다.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("비주얼 스케일 배수(이미지 크기만). 데미지 범위와 무관.\n1=원본, 2=2배")]
    [SerializeField, Min(0.1f)] private float visualScaleMultiplier = 1f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private float _timer;
    private bool _active;
    private readonly Collider2D[] _hits = new Collider2D[64];

    private Vector3 _visualBaseScale = Vector3.one;

    // 마지막으로 사용한 반경(기즈모 표시용)
    [SerializeField, Min(0.05f)] private float _lastRadius = 1.5f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;

        if (visualRoot != null)
            _visualBaseScale = visualRoot.localScale;
    }

    /// <summary>
    /// 번개 소환: 지정 위치에서 범위 데미지 + 애니메이션
    /// </summary>
    public void Strike(float radius, int damage, LayerMask mask)
    {
        gameObject.SetActive(true);

        radius = Mathf.Max(0.05f, radius);
        damage = Mathf.Max(1, damage);

        _lastRadius = radius;

        // 비주얼만 배수 스케일 적용(루트 스케일 건드리지 않음)
        if (visualRoot != null)
            visualRoot.localScale = _visualBaseScale * visualScaleMultiplier;

        if (debugLog)
            Debug.Log($"[ThunderStrike] Strike pos={transform.position} radius={radius} dmg={damage} mask={mask.value}");

        // 범위 데미지 (즉시 1회)
        int hitCount = Physics2DCompat.OverlapCircleNonAlloc(
            (Vector2)transform.position,
            radius,
            _hits,
            mask
        );

        if (debugLog)
            Debug.Log($"[ThunderStrike] OverlapCircle hitCount={hitCount}");

        int damageApplied = 0;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            // 자식 콜라이더가 잡혀도 부모 체력에 적용되도록 TryApplyDamage가 내부에서 InParent를 찾는 게 이상적
            // 여기서는 최소한 TryApplyDamage 실패 시 root쪽도 한 번 더 시도
            bool success = DamageUtil2D.TryApplyDamage(col, damage);
            if (!success)
            {
                var rootCol = col.transform.root.GetComponent<Collider2D>();
                if (rootCol != null)
                    success = DamageUtil2D.TryApplyDamage(rootCol, damage);
            }

            if (debugLog)
                Debug.Log($"[ThunderStrike]   hit[{i}]={col.name} layer={LayerMask.LayerToName(col.gameObject.layer)} dmgApplied={success}");

            if (success) damageApplied++;
        }

        if (debugLog)
            Debug.Log($"[ThunderStrike] Total damaged={damageApplied}/{hitCount}");

        if (animator != null)
            animator.SetTrigger(triggerName);

        _timer = 0f;
        _active = true;
    }

    private void Update()
    {
        if (!_active) return;

        _timer += Time.deltaTime;
        if (_timer >= thunderDuration)
            Finish();
    }

    /// <summary>
    /// Animation Event용: 클립 마지막 프레임에서 호출
    /// </summary>
    public void OnAnimationFinished()
    {
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