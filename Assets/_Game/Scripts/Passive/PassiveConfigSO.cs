using UnityEngine;

// [구현 원리 요약]
// 패시브 카드의 이름, 아이콘, 텍스트(수치)를 관리하는 데이터 파일입니다.
[CreateAssetMenu(menuName = "혼령검/패시브/패시브 설정", fileName = "Passive_")]
public sealed class PassiveConfigSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("패시브의 종류를 설정합니다.")]
    public PassiveKind kind;
    
    [Tooltip("UI에 표시될 패시브의 이름입니다. (예: 곰의 완력)")]
    public string displayName = "패시브";

    [TextArea]
    [Tooltip("UI 카드에 표시될 내용입니다. '공격력 10% 증가' 처럼 수치만 명확하게 적어주세요. (LV 표시 안 됨)")]
    public string descriptionKr;

    [Tooltip("UI 카드에 들어갈 패시브 아이콘(그림)입니다.")]
    public Sprite icon;

    [Header("레벨별 능력치 (내부용)")]
    [Tooltip("이 패시브의 최대 중첩(강화) 횟수입니다.")]
    [Min(1)]
    public int maxLevel = 8;

    [Tooltip("내부적으로 적용될 레벨별 수치 데이터입니다.")]
    public PassiveLevelParams[] levels;

    public PassiveLevelParams GetLevelParams(int level)
    {
        if (levels == null || levels.Length == 0) return default;

        int lv = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        int idx = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
        return levels[idx];
    }
}