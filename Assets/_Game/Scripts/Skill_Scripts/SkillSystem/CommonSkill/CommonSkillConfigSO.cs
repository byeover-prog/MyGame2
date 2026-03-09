using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/스킬 설정", fileName = "CSkill_")]
public sealed class CommonSkillConfigSO : ScriptableObject
{
    // 공통 스킬 레벨 절대 상한(기획상 8). 런타임/카드/UI에서 공통으로 사용.
    public const int HardMaxLevel = 8;

    public CommonSkillKind kind;
    public string displayName = "공통 스킬";

    [Tooltip("레벨업 카드에 반드시 표시되는 '공격 방식' 설명(레벨 표기 금지).\n예: '플레이어 주변을 원형으로 회전하며 접촉 피해를 줍니다.'")]
    [TextArea]
    public string visualDescriptionKr;

    public Sprite icon;
    public GameObject weaponPrefab;

    [Min(1)]
    public int maxLevel = HardMaxLevel;

    [Tooltip("레벨 1..maxLevel 데이터(길이가 부족하면 마지막 값을 반복 사용)")]
    public CommonSkillLevelParams[] levels;

    public CommonSkillLevelParams GetLevelParams(int level)
    {
        if (levels == null || levels.Length == 0)
            return default;

        int lv = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        int idx = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
        return levels[idx];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 기획 상한(8) 강제. 원치 않으면 이 줄을 제거하고 maxLevel만 수동 관리해도 됨.
        if (maxLevel < 1) maxLevel = 1;
        if (maxLevel > HardMaxLevel) maxLevel = HardMaxLevel;

        if (levels == null) return;
        if (levels.Length == 0) return;

        // levels 길이를 강제로 늘리진 않음(에셋 덮어쓰기 방지).
        // AutoBuilder 사용 시, 자동으로 maxLevel 길이로 생성됨.
    }
#endif
}