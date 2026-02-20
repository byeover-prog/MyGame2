using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 공통 스킬 레벨업 카드 뷰.
/// - 카드에 레벨 숫자를 표시하지 않습니다.
/// - 설명은 CommonSkillConfigSO.behaviorDescriptionKr(스킬 동작 방식)을 우선 사용하며,
///   비어있으면 CommonSkillKind 기준 기본 동작 문장을 자동 생성합니다.
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

    /// <summary>
    /// 카드 데이터를 바인딩합니다.
    /// currentLevel: 이 스킬의 현재 보유 레벨(0=미보유, 1+=보유중).
    /// </summary>
    public void Bind(CommonSkillCardPicker2D picker, CommonSkillCardSO card, int currentLevel)
    {
        _picker = picker;
        _card = card;

        var skill = (card != null) ? card.skill : null;

        // ── 제목: 스킬 이름만 표시, 레벨 숫자 없음 ──
        if (titleText != null)
        {
            string nm = (skill != null) ? skill.displayName : "NULL";
            titleText.text = nm;
        }

        // ── 설명: 동작 방식 중심의 한 문장 ──
        if (descText != null)
            descText.text = BuildDesc(skill, currentLevel);

        // ── 아이콘 ──
        if (iconImage != null)
        {
            bool hasIcon = (skill != null && skill.icon != null);
            iconImage.enabled = hasIcon;
            iconImage.sprite = hasIcon ? skill.icon : null;
        }

        if (button != null)
            button.interactable = (skill != null);
    }

    // ──────────────────────────────────────────────────────────────────
    // 설명 생성 규칙:
    //  1) CommonSkillConfigSO.behaviorDescriptionKr 가 있으면 그것을 사용.
    //  2) 없으면 CommonSkillKind 기반 고정 동작 문장 반환.
    //     → 레벨/수치를 노출하지 않고 "어떻게 작동하는가"만 전달합니다.
    // ──────────────────────────────────────────────────────────────────
    private static string BuildDesc(CommonSkillConfigSO skill, int curLv)
    {
        if (skill == null) return string.Empty;

        // 1) 디자이너가 직접 작성한 행동 설명 우선
        if (!string.IsNullOrWhiteSpace(skill.behaviorDescriptionKr))
            return skill.behaviorDescriptionKr.Trim();

        // 2) 스킬 종류별 기본 동작 문장
        switch (skill.kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return "검이 플레이어 주위를 회전하며 인접한 적을 지속적으로 타격합니다.";

            case CommonSkillKind.Boomerang:
                return "가장 먼 적을 향해 날아갔다가 플레이어에게 돌아오며, 오가는 경로의 모든 적을 타격합니다.";

            case CommonSkillKind.PiercingBullet:
                return "가장 가까운 적을 향해 관통하는 총알을 발사합니다.";

            case CommonSkillKind.HomingMissile:
                return "가장 먼 적을 추적하는 미사일을 발사합니다. 명중 시 다음 가까운 적으로 연쇄 이동합니다.";

            case CommonSkillKind.DarkOrb:
                return "적에게 명중하면 폭발한 뒤 방사형으로 분열하는 암흑 구체를 발사합니다.";

            case CommonSkillKind.Shuriken:
                return "수리검이 적을 맞힌 후 다음으로 가장 가까운 적에게 자동으로 튕겨 나갑니다.";

            case CommonSkillKind.ArrowShot:
                return "가장 가까운 적을 향해 화살을 발사합니다.";

            case CommonSkillKind.ArrowRain:
                return "가장 가까운 적 위치에 화살비 장판을 소환합니다. 장판 안의 모든 적에게 지속 피해를 줍니다.";

            case CommonSkillKind.Balsi:
                return "가장 가까운 적을 향해 투사체를 발사합니다. 관통 시 뒤의 적도 연속으로 피해를 받습니다.";

            default:
                return "새 스킬을 획득합니다.";
        }
    }

    private void OnClick()
    {
        Debug.Log($"[CardView2D] OnClick | picker={_picker != null} | card={_card != null}");
        if (_picker == null) { Debug.LogError("[CardView2D] _picker null - Bind() 미호출"); return; }
        if (_card == null)   { Debug.LogError("[CardView2D] _card null"); return; }
        _picker.Pick(_card);
    }
}