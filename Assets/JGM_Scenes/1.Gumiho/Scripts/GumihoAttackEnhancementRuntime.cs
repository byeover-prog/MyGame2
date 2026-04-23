// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 구미호 기본 공격의 강화 상태를 런타임에서만 관리한다.
// 여우구슬 패턴이 시작되면 강화 수치를 적용하고,
// 종료되면 기본값으로 되돌린다.

[DisallowMultipleComponent]
public sealed class GumihoAttackEnhancementRuntime : MonoBehaviour
{
    [Header("기본 상태")]

    [Tooltip("강화가 꺼져 있을 때 기본 투사체 개수입니다.")]
    [Min(1)]
    [SerializeField] private int defaultProjectileCount = 1;

    [Tooltip("강화가 꺼져 있을 때 기본 데미지 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float defaultDamageMultiplier = 1f;

    [Tooltip("강화가 꺼져 있을 때 기본 발사 각도 간격입니다.")]
    [Min(0f)]
    [SerializeField] private float defaultSpreadAngle = 0f;


    [Header("현재 강화 상태")]

    [Tooltip("현재 기본 공격 강화 적용 여부입니다.")]
    [SerializeField] private bool isEnhanced = false;

    [Tooltip("현재 적용 중인 투사체 개수입니다.")]
    [Min(1)]
    [SerializeField] private int currentProjectileCount = 3;

    [Tooltip("현재 적용 중인 데미지 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float currentDamageMultiplier = 1.5f;

    [Tooltip("현재 적용 중인 투사체 사이 각도 간격입니다.")]
    [Min(0f)]
    [SerializeField] private float currentSpreadAngle = 8f;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    public bool IsEnhanced => isEnhanced;

    public int CurrentProjectileCount => isEnhanced ? Mathf.Max(1, currentProjectileCount) : Mathf.Max(1, defaultProjectileCount);
    public float CurrentDamageMultiplier => isEnhanced ? Mathf.Max(0.1f, currentDamageMultiplier) : Mathf.Max(0.1f, defaultDamageMultiplier);
    public float CurrentSpreadAngle => isEnhanced ? Mathf.Max(0f, currentSpreadAngle) : Mathf.Max(0f, defaultSpreadAngle);

    private void Awake()
    {
        ClearEnhancement();
    }

    public void ApplyEnhancement(int projectileCount, float damageMultiplier, float spreadAngle)
    {
        isEnhanced = true;
        currentProjectileCount = Mathf.Max(1, projectileCount);
        currentDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
        currentSpreadAngle = Mathf.Max(0f, spreadAngle);

        if (debugLog)
        {
            Debug.Log(
                $"[GumihoAttackEnhancementRuntime] 강화 적용 | count={currentProjectileCount} damageMul={currentDamageMultiplier} spread={currentSpreadAngle}",
                this);
        }
    }

    public void ClearEnhancement()
    {
        isEnhanced = false;
        currentProjectileCount = Mathf.Max(1, defaultProjectileCount);
        currentDamageMultiplier = Mathf.Max(0.1f, defaultDamageMultiplier);
        currentSpreadAngle = Mathf.Max(0f, defaultSpreadAngle);

        if (debugLog)
        {
            Debug.Log("[GumihoAttackEnhancementRuntime] 강화 해제", this);
        }
    }
}