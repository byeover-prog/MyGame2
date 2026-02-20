using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/Run/Run Config", fileName = "RunConfigAsset")]
public sealed class RunConfigAsset : ScriptableObject
{
    [Header("모드 ID (예: casual / trial / tower)")]
    public string mode_id = "casual";

    [Header("캐주얼 스폰 커브(캐주얼일 때만 사용)")]
    public CasualSpawnCurveConfigSO casual_spawn_curve;
}
