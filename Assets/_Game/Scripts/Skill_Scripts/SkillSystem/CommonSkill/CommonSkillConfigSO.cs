using UnityEngine;

/// <summary>
/// 공통 스킬 하나의 전체 설정.
/// - displayName: 카드/UI에 표시할 스킬 이름
/// - behaviorDescriptionKr: 스킬이 어떻게 동작하는지 설명하는 한 문장 (레벨 무관)
/// - levels[]: SkillEffectConfig 배열 – 레벨 1부터 Inspector에서 직접 수치 조정 가능
/// </summary>
[CreateAssetMenu(menuName = "그날이후/공통스킬/스킬 설정", fileName = "CSkill_")]
public sealed class CommonSkillConfigSO : ScriptableObject
{
    [Header("식별 / 분류")]
    public CommonSkillKind kind;

    [Header("카드 표시")]
    [Tooltip("카드/UI에 보여줄 스킬 이름(한글 권장).")]
    public string displayName = "공통 스킬";

    [Tooltip("카드 설명 문장. 레벨과 무관하게 스킬의 동작 방식을 한 문장으로 기술하세요.\n" +
             "예) '검이 플레이어 주위를 회전하며 인접한 적을 타격합니다.'\n" +
             "비워두면 CommonSkillCardView2D가 기본 텍스트를 사용합니다.")]
    [TextArea(2, 4)]
    public string behaviorDescriptionKr = "";

    [Tooltip("카드에 표시할 아이콘. 없으면 아이콘 영역을 숨깁니다.")]
    public Sprite icon;

    [Header("무기 프리팹")]
    [Tooltip("CommonSkillManager2D가 스폰할 무기 프리팹.\n" +
             "이 프리팹에 CommonSkillWeapon2D를 상속한 컴포넌트(예: OrbitingBladeWeapon2D)가 붙어 있어야 합니다.")]
    public GameObject weaponPrefab;

    [Header("레벨 상한")]
    [Min(1)]
    public int maxLevel = 10;

    [Header("레벨별 효과 설정 (SkillEffectConfig)")]
    [Tooltip("인덱스 0 = 레벨 1.\n" +
             "배열 길이가 maxLevel보다 짧으면 마지막 값을 반복 사용합니다.\n" +
             "Inspector에서 각 레벨의 수치를 수정하면 즉시 런타임에 반영됩니다.")]
    public SkillEffectConfig[] levels;

    /// <summary>
    /// 지정 레벨의 SkillEffectConfig를 반환합니다.
    /// 배열 범위를 벗어나면 마지막 값을 반환(안전 클램프).
    /// </summary>
    public SkillEffectConfig GetLevelParams(int level)
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
        // 배열 길이를 강제로 늘리지 않음(기존 에셋 덮어쓰기 방지).
        // SOAutoBuilderMenu 또는 수동으로 길이를 maxLevel에 맞춰 추가하세요.
    }
#endif
}
