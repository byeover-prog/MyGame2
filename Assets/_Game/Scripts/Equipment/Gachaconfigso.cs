using UnityEngine;

/// <summary>
/// 가챠 시스템 전체 설정. 확률 / 천장 / 단가 / 중복 환산 냥을 한곳에서 관리.
/// 이 SO 하나만 Assets/GameData/GachaConfig.asset 에 존재한다 (싱글톤 설정).
/// 기획자/밸런스 팀이 이 SO의 Inspector 값만 수정해서 가챠 밸런스를 조정한다.
/// </summary>
[CreateAssetMenu(
    fileName = "GachaConfig",
    menuName = "혼령검/가챠/가챠 설정 SO",
    order = 200)]
public class GachaConfigSO : ScriptableObject
{
    [Header("등급별 기본 확률 (총합 1.0)")]
    [Tooltip("에픽 드랍 확률 (0.03 = 3%)")]
    [Range(0f, 1f)]
    public float epicRate = 0.03f;

    [Tooltip("레어 드랍 확률 (0.10 = 10%)")]
    [Range(0f, 1f)]
    public float rareRate = 0.10f;

    [Tooltip("희귀 드랍 확률 (0.30 = 30%)")]
    [Range(0f, 1f)]
    public float uncommonRate = 0.30f;

    [Tooltip("일반 드랍 확률 (0.57 = 57%)")]
    [Range(0f, 1f)]
    public float commonRate = 0.57f;
    
    [Header("가챠 단가 (냥)")]
    [Tooltip("1회 뽑기 비용")]
    public int singlePullCost = 500;

    [Tooltip("10연 뽑기 비용 (10% 할인)")]
    public int tenPullCost = 4500;

    [Header("천장 시스템")]
    [Tooltip("에픽 천장 카운트 — N뽑째에 에픽 확정. 에픽 자연 드랍 시 0으로 리셋")]
    public int epicPityCount = 30;

    [Header("10연 보장")]
    [Tooltip("10연 뽑기 시 최소 보장 등급. 10연 안에 이 등급 이상이 없으면 마지막 1개를 이 등급으로 교체")]
    public EquipmentRarity tenPullMinGuarantee = EquipmentRarity.Rare;

    [Header("중복 환산 냥 (등급별 고정)")]
    [Tooltip("에픽 중복 환산 냥")]
    public int epicRefund = 2000;

    [Tooltip("레어 중복 환산 냥")]
    public int rareRefund = 500;

    [Tooltip("희귀 중복 환산 냥")]
    public int uncommonRefund = 150;

    [Tooltip("일반 중복 환산 냥")]
    public int commonRefund = 50;

    /// <summary>
    /// 등급에 해당하는 중복 환산 냥을 반환.
    /// </summary>
    public int GetRefundAmount(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.Epic: return epicRefund;
            case EquipmentRarity.Rare: return rareRefund;
            case EquipmentRarity.Uncommon: return uncommonRefund;
            case EquipmentRarity.Common: return commonRefund;
            default: return 0;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        float total = epicRate + rareRate + uncommonRate + commonRate;
        if (Mathf.Abs(total - 1f) > 0.001f)
            Debug.LogWarning($"[GachaConfigSO] 확률 총합이 1.0이 아닙니다: {total:F4}", this);
    }
#endif
}