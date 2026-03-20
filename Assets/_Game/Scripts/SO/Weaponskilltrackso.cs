using UnityEngine;

/// <summary>
/// 무기 스킬 업그레이드 트랙 SO입니다.
/// SkillRootSO.weaponSkillTracks에서 참조합니다.
/// 현재는 컴파일 통과용 빈 껍데기이며, 무기 트랙 시스템 구현 시 확장하세요.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/스킬/무기 스킬 트랙", fileName = "WeaponSkillTrack_")]
public sealed class WeaponSkillTrackSO : ScriptableObject
{
    [Header("식별")]
    [Tooltip("디버그용 이름입니다.")]
    [SerializeField] private string displayName;

    /// <summary>디버그용 이름입니다.</summary>
    public string DisplayName => displayName;
}