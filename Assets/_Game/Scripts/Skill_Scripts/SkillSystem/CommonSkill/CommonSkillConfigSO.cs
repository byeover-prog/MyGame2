using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/스킬 설정", fileName = "CSkill_")]
public sealed class CommonSkillConfigSO : ScriptableObject
{
    public CommonSkillKind kind;
    public string displayName = "공통 스킬";
    public Sprite icon;
    public GameObject weaponPrefab;

    [Min(1)]
    public int maxLevel = 10;

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
        if (maxLevel < 1) maxLevel = 1;
        if (levels == null) return;

        if (levels.Length == 0) return;

        // levels 길이를 강제로 늘리진 않음(에셋 덮어쓰기 방지).
        // AutoBuilder 사용 시, 자동으로 maxLevel 길이로 생성됨.
    }
#endif
}