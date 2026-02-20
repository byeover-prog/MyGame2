using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/Run/Run Config", fileName = "RunConfigSO")]
public sealed class RunConfigSO : ScriptableObject
{
    [Header("모드 ID")]
    [Tooltip("예: casual / trial / tower")]
    [SerializeField] private string mode_id = "casual";

    [Header("캐주얼 스폰 커브(캐주얼일 때만 사용)")]
    [SerializeField] private CasualSpawnCurveConfigSO casual_spawn_curve;

    // 외부에서 읽기용(Scaler가 사용하는 표준 프로퍼티)
    public string ModeId => mode_id;
    public CasualSpawnCurveConfigSO CasualSpawnCurve => casual_spawn_curve;
}