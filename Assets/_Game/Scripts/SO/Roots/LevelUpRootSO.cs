using UnityEngine;

/// <summary>
/// 레벨업(카드 제시/선택) 시스템 설정 Root.
/// 
/// 원칙
/// - 설정값의 '진실의 원천(Source of Truth)'을 한 곳으로 모은다.
/// - 동일한 설정(offerCount, pause 등)을 MonoBehaviour에도 중복 보관하지 않는다.
/// </summary>
[CreateAssetMenu(menuName = "그날이후/Roots/LevelUpRoot", fileName = "Root_LevelUp")]
public sealed class LevelUpRootSO : ScriptableObject
{
    [Header("카드 제시")]
    [Tooltip("레벨업 시 제시할 카드 수(현재 UI는 3장 고정)")]
    [Min(1)]
    public int offerCount = 3;

    [Tooltip("패널이 열린 동안 게임을 일시정지(Time.timeScale=0)할지")]
    public bool pauseGameWhileOpen = true;

    [Tooltip("연속 레벨업(여러 번 레벨업) 시 다음 패널 오픈까지의 간격(Realtime)")]
    [Min(0f)]
    public float openIntervalRealtime = 0.25f;

    [Header("디버그")]
    [Tooltip("강제 레벨업(패널 오픈) 단축키")]
    public KeyCode debugOpenKey = KeyCode.F1;

    [Header("가중치(상대 비율)")]
    [Tooltip("무기 업그레이드 후보 가중치에 곱하는 배수")]
    [Min(0)]
    public int weaponWeightMultiplier = 1;

    [Tooltip("공통 스킬 후보 가중치에 곱하는 배수")]
    [Min(0)]
    public int commonSkillWeightMultiplier = 10;
}