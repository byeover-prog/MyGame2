using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/레벨트랙/스킬 레벨 트랙", fileName = "SkillTrack_")]
public sealed class SkillLevelTrackSO : ScriptableObject
{
    [Header("매칭 키")]
    [Tooltip("WeaponDefinitionSO.weaponId 와 매칭")]
    public string weaponId;

    [Tooltip("UI 표시용 스킬명(비면 weaponId 사용)")]
    public string displayNameKr;

    [Header("레벨 단계(1~8)")]
    public List<LevelStep> steps = new List<LevelStep>(8);

    [Serializable]
    public sealed class LevelStep
    {
        [Tooltip("이 레벨에서 제시될 업그레이드(카드 후보)들")]
        public List<UpgradeDef> upgrades = new List<UpgradeDef>(1);
    }

    [Serializable]
    public sealed class UpgradeDef
    {
        public WeaponUpgradeType type;
        public UpgradeValue value;

        [Tooltip("제목 오버라이드(비면 자동 생성)")]
        public string titleOverrideKr;

        [Tooltip("태그(비면 공통)")]
        public string tagsKr = "공통";
    }

    public bool TryGetStep(int nextLevel, out LevelStep step)
    {
        step = null;
        if (nextLevel < 1) return false;

        int idx = nextLevel - 1;
        if (idx < 0 || idx >= steps.Count) return false;

        step = steps[idx];
        return step != null;
    }

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayNameKr) ? weaponId : displayNameKr;
    }
}