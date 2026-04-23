using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 공통 스킬 카드 UI.
///
/// 요구사항(프로토타입)
/// - 카드에 "Lv." 같은 레벨 표기 금지
/// - 설명에는 반드시 '레벨1 동작(공격 방식)' 문장이 포함되어야 함
/// - 효과 요약은 '다음 레벨 적용 내용'을 보여주되, 레벨 숫자는 쓰지 않음
/// </summary>
public sealed class CommonSkillCardView2D : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    private CommonSkillCardPicker2D _picker;
    private CommonSkillCardSO _card;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Bind(CommonSkillCardPicker2D picker, CommonSkillCardSO card, int currentLevel)
    {
        _picker = picker;
        _card = card;

        var skill = (card != null) ? card.skill : null;

        if (titleText != null)
        {
            titleText.text = (skill != null) ? skill.displayName : "NULL";
        }

        if (descText != null)
        {
            descText.text = BuildDesc(skill, currentLevel);
        }

        if (iconImage != null)
        {
            if (skill != null && skill.icon != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = skill.icon;
            }
            else
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
        }

        if (button != null)
            button.interactable = (skill != null);
    }

    private static string BuildDesc(CommonSkillConfigSO skill, int curLv)
    {
        if (skill == null) return string.Empty;

        int max = Mathf.Clamp(skill.maxLevel, 1, CommonSkillConfigSO.HardMaxLevel);
        int nextLv = Mathf.Clamp(curLv + 1, 1, max);

        // 1) 레벨1 동작 설명(항상 포함)
        string visual = !string.IsNullOrWhiteSpace(skill.visualDescriptionKr)
            ? skill.visualDescriptionKr
            : GetFallbackVisualDescription(skill.kind);

        // 2) 다음 레벨 효과 요약(레벨 표기 금지)
        var nxt = skill.GetLevelParams(nextLv);
        string effect = BuildEffectSummary(skill.kind, nxt);

        return $"{visual}\n\n효과: {effect}";
    }

    private static string BuildEffectSummary(CommonSkillKind kind, CommonSkillLevelParams p)
    {
        // 스킬별로 의미 있는 필드만 요약
        switch (kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return $"피해 {p.damage}, 검 {p.projectileCount}개, 타격간격 {p.hitInterval:0.##}s";

            case CommonSkillKind.Boomerang:
                return $"피해 {p.damage}, 투사체 {p.projectileCount}개, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.PiercingBullet:
                return $"피해 {p.damage}, 쿨타임 {p.cooldown:0.##}s, 투사체 {p.projectileCount}개";

            case CommonSkillKind.HomingMissile:
                return $"피해 {p.damage}, 연쇄타격 {p.chainCount}회, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.DarkOrb:
                return $"피해 {p.damage}, 분열 {p.splitCount}개, 폭발반경 {p.explosionRadius:0.#}, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.Shuriken:
                return $"피해 {p.damage}, 튕김 {p.bounceCount}회, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.ArrowShot:
                return $"피해 {p.damage}, 화살 {p.projectileCount}발, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.ArrowRain:
                return $"피해 {p.damage}, 장판반경 {p.explosionRadius:0.#}, 틱 {p.hitInterval:0.##}s, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.Balsi:
                return $"피해 {p.damage}, 쿨타임 {p.cooldown:0.##}s";

            default:
                return "강화";
        }
    }

    private static string GetFallbackVisualDescription(CommonSkillKind kind)
    {
        // 레벨1 동작 설명(기본값)
        switch (kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return "플레이어 주변을 원형으로 회전하며, 닿는 적에게 지속 피해를 줍니다.";
            case CommonSkillKind.Boomerang:
                return "가장 먼 적을 향해 날아갔다가 되돌아오며 관통 공격합니다. 같은 적은 왕복 1회씩만 타격합니다.";
            case CommonSkillKind.PiercingBullet:
                return "가장 가까운 적을 향해 직선 관통 탄을 발사합니다.";
            case CommonSkillKind.HomingMissile:
                return "가장 먼 적을 추적하는 유도 탄을 발사합니다. 추가 타겟을 연속 공격할 수 있습니다.";
            case CommonSkillKind.DarkOrb:
                return "가장 가까운 적을 향해 비관통 구체를 발사합니다. 적중 시 분열/폭발합니다.";
            case CommonSkillKind.Shuriken:
                return "가장 가까운 적에게 던져 적중 시 다른 적에게 튕깁니다.";
            case CommonSkillKind.ArrowShot:
                return "가장 가까운 적을 향해 기본 화살을 발사합니다.";
            case CommonSkillKind.ArrowRain:
                return "체력이 많은 적 위치에 화살을 낙하시켜 장판 피해를 줍니다.";
            case CommonSkillKind.Balsi:
                return "가장 가까운 적을 향해 기본 투사체를 발사합니다.";
        }

        return "자동으로 적을 공격합니다.";
    }

    private void OnClick()
    {
        if (_picker == null || _card == null) return;
        _picker.Pick(_card);
    }
}
