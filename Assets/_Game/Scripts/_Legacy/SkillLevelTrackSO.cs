using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 하나의 레벨별 업그레이드 트랙을 정의하는 SO입니다.
/// SkillLevelUpOfferBuilder2D에서 후보 카드 생성,
/// Tool_SkillRoot_WeaponTracksAndDeck_AutoGen 에디터 도구에서 자동 생성에 사용됩니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/스킬/스킬 레벨 트랙", fileName = "SkillLevelTrack_")]
public sealed class SkillLevelTrackSO : ScriptableObject
{
    [Header("식별")]
    [Tooltip("이 트랙이 대응하는 무기 ID입니다. WeaponDefinitionSO.weaponId와 일치해야 합니다.")]
    public string weaponId;

    [Tooltip("UI에 표시할 무기 한글 이름입니다.")]
    public string displayNameKr;

    [Header("레벨별 단계")]
    [Tooltip("레벨 1~8까지의 업그레이드 단계 목록입니다. 인덱스 0 = 레벨 1 업그레이드.")]
    public List<LevelStep> steps = new List<LevelStep>(8);

    /// <summary>UI에 표시할 무기 한글 이름을 반환합니다.</summary>
    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayNameKr) ? weaponId : displayNameKr;
    }

    /// <summary>
    /// 해당 레벨의 업그레이드 단계를 가져옵니다.
    /// level은 1-based입니다. (레벨 1 = steps[0])
    /// </summary>
    public bool TryGetStep(int level, out LevelStep step)
    {
        step = null;
        int index = level - 1;
        if (steps == null || index < 0 || index >= steps.Count) return false;

        step = steps[index];
        return step != null;
    }

    // ─── 내부 타입 ─────────────────────────────────────

    /// <summary>
    /// 특정 레벨에서 적용 가능한 업그레이드 목록입니다.
    /// </summary>
    [Serializable]
    public sealed class LevelStep
    {
        [Tooltip("이 레벨에서 적용 가능한 업그레이드 정의 목록입니다.")]
        public List<UpgradeDef> upgrades = new List<UpgradeDef>(4);
    }

    /// <summary>
    /// 레벨업 시 적용되는 개별 업그레이드 정의입니다.
    /// </summary>
    [Serializable]
    public sealed class UpgradeDef
    {
        [Tooltip("업그레이드 종류입니다.")]
        public WeaponUpgradeType type;

        [Tooltip("업그레이드 수치입니다.")]
        public UpgradeValue value;

        [Tooltip("UI에 표시할 태그(한글)입니다. 비우면 '공통'으로 표시됩니다.")]
        public string tagsKr;

        [Tooltip("제목 오버라이드(한글)입니다. 비우면 자동 생성됩니다.")]
        public string titleOverrideKr;
    }
}