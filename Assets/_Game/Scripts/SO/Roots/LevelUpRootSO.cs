using UnityEngine;

/// <summary>
/// 레벨업 시스템의 글로벌 설정입니다.
/// RootBootstrapper에 연결하면 LevelUpSystem2D가 자동으로 읽어갑니다.
/// ⚠ 이 파일이 이미 프로젝트에 존재하면 이 파일을 삭제하세요.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/시스템/레벨업 루트 설정", fileName = "LevelUpRoot")]
public sealed class LevelUpRootSO : ScriptableObject
{
    [Header("카드 제시")]
    [Tooltip("레벨업 시 제시할 카드 수입니다.")]
    [Min(1)] public int offerCount = 3;

    [Tooltip("카드 선택 중 게임을 일시정지할지 여부입니다.")]
    public bool pauseGameWhileOpen = true;

    [Tooltip("연속 레벨업 시 카드 오픈 간격(실시간 초)입니다.")]
    [Min(0f)] public float openIntervalRealtime = 0.25f;

    [Tooltip("디버그용 레벨업 키입니다. None이면 비활성화됩니다.")]
    public KeyCode debugOpenKey = KeyCode.F1;

    [Header("가중치")]
    [Tooltip("무기 카드 가중치 배율입니다.")]
    [Min(0)] public int weaponWeightMultiplier = 1;

    [Tooltip("공통 스킬 카드 가중치 배율입니다.")]
    [Min(0)] public int commonSkillWeightMultiplier = 10;
}