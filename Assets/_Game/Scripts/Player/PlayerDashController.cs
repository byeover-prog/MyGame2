using UnityEngine;

/// <summary>
/// 플레이어 대쉬 사용/충전만 담당.
/// 편성 보정은 외부에서 받아오고, UI는 상태가 바뀔 때만 갱신한다.
/// </summary>
public sealed class PlayerDashController : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("기본 최대 대쉬 충전 수")]
    [SerializeField] private int baseDashCount = 2;

    [Tooltip("기본 대쉬 충전 시간(초)")]
    [SerializeField] private float baseDashCooldown = 3f;

    [Tooltip("바람 속성 편성 시 대쉬 쿨타임 감소 비율")]
    [SerializeField, Range(0f, 0.9f)] private float windDashCooldownReductionRate = 0.2f;

    [Header("UI")]
    [Tooltip("대쉬 UI")]
    [SerializeField] private DashChargeUI dashChargeUI;

    private int currentDashCount;
    private int maxDashCount;
    private float dashCooldown;
    private float rechargeTimer;

    /// <summary>
    /// 현재 충전 수
    /// </summary>
    public int Current_Dash_Count => currentDashCount;

    /// <summary>
    /// 최대 충전 수
    /// </summary>
    public int Max_Dash_Count => maxDashCount;

    /// <summary>
    /// 실제 적용 쿨타임
    /// </summary>
    public float Dash_Cooldown => dashCooldown;
    private void Awake()
    {
        // Initialize가 외부에서 안 불릴 경우 기본값으로 초기화
        Initialize(false);
    }
    private void Update()
    {
        RechargeDash();
    }

    /// <summary>
    /// 전투 시작 시 편성 보정 적용
    /// </summary>
    public void Initialize(bool hasWindAttribute)
    {
        maxDashCount = hasWindAttribute ? baseDashCount + 1 : baseDashCount;

        float reductionRate = hasWindAttribute ? windDashCooldownReductionRate : 0f;
        dashCooldown = baseDashCooldown * (1f - reductionRate);

        currentDashCount = maxDashCount;
        rechargeTimer = 0f;

        RefreshUI();
    }

    /// <summary>
    /// 대쉬 사용 가능 여부
    /// </summary>
    public bool CanUseDash()
    {
        return currentDashCount > 0;
    }

    /// <summary>
    /// 대쉬 1회 사용
    /// </summary>
    public bool TryUseDash()
    {
        if (currentDashCount <= 0)
            return false;

        currentDashCount--;

        /// 충전이 비기 시작한 순간 타이머 시작
        if (currentDashCount < maxDashCount && rechargeTimer <= 0f)
        {
            rechargeTimer = dashCooldown;
        }

        RefreshUI();
        return true;
    }

    /// <summary>
    /// 대쉬 충전 복구
    /// </summary>
    private void RechargeDash()
    {
        Debug.Log($"RechargeDash 호출 - current: {currentDashCount}, max: {maxDashCount}");
        if (currentDashCount >= maxDashCount)
            return;
        
        if (dashChargeUI != null)
            dashChargeUI.UpdateChargingFill(rechargeTimer, dashCooldown);


        if (rechargeTimer > 0f)
        {
            rechargeTimer -= Time.deltaTime;
            return;
        }

        currentDashCount++;
        RefreshUI();

        /// 아직 다 안 찼으면 다음 충전 타이머 계속 진행
        if (currentDashCount < maxDashCount)
        {
            rechargeTimer = dashCooldown;
        }
    }

    /// <summary>
    /// UI 갱신
    /// </summary>
    private void RefreshUI()
    {
        if (dashChargeUI == null)
            return;

        dashChargeUI.Refresh(currentDashCount, maxDashCount);
    }
}