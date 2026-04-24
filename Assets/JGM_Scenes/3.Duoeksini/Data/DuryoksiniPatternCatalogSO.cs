// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니가 사용하는 패턴 설정 SO를 한 곳에 묶는다.
// 파쇄 돌진, 대지 분쇄, 분노 포효를 여기서 연결한다.
// 새 패턴이 추가되어도 이 카탈로그 기준으로 확장한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Duryoksini/패턴 카탈로그",
    fileName = "DuryoksiniPatternCatalogSO")]
public sealed class DuryoksiniPatternCatalogSO : ScriptableObject
{
    [Header("기본 패턴")]

    [Tooltip("두억시니 파쇄 돌진 패턴 설정 SO이다.")]
    [SerializeField] private DuryoksiniCrushChargeConfigSO crushChargeConfig;

    [Tooltip("두억시니 대지 분쇄 패턴 설정 SO이다.")]
    [SerializeField] private DuryoksiniGroundSmashConfigSO groundSmashConfig;

    [Tooltip("두억시니 분노 포효 낙석 패턴 설정 SO이다.")]
    [SerializeField] private DuryoksiniRoarRockfallConfigSO roarRockfallConfig;


    public DuryoksiniCrushChargeConfigSO CrushChargeConfig => crushChargeConfig;
    public DuryoksiniGroundSmashConfigSO GroundSmashConfig => groundSmashConfig;
    public DuryoksiniRoarRockfallConfigSO RoarRockfallConfig => roarRockfallConfig;
}