// ──────────────────────────────────────────────
// LevelUpCardView.cs
// 기존 3장 레벨업 시스템용 카드 뷰
// LevelUpCardPicker에서 참조한다.
// (새 4장 시스템은 LevelUpNewCardView를 사용)
// ──────────────────────────────────────────────

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpCardView : MonoBehaviour
{
    [Header("=== UI 참조 ===")]

    [SerializeField, Tooltip("카드 아이콘")]
    private Image iconImage;

    [SerializeField, Tooltip("카드 제목")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("카드 설명")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("카드 태그")]
    private TMP_Text tagText;

    // ── 무기 업그레이드 카드 바인딩 ────────────

    public void BindWeaponUpgradeCard(WeaponUpgradeCardSO card)
    {
        if (card == null)
        {
            SetEmpty();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = card.icon;
            iconImage.enabled = card.icon != null;
        }

        if (titleText != null)
            titleText.text = card.GetTitleForUI();

        if (descriptionText != null)
            descriptionText.text = card.GetDescriptionForUI();

        if (tagText != null)
            tagText.text = card.GetTagsForUI();

        gameObject.SetActive(true);
    }

    // ── 공통 스킬 카드 바인딩 ──────────────────

    public void BindCommonSkillCard(CommonSkillCardSO card)
    {
        if (card == null || card.skill == null)
        {
            SetEmpty();
            return;
        }

        // CommonSkillCardSO는 직접 UI 필드가 없으므로
        // 연결된 skill(CommonSkillConfigSO)에서 가져온다
        var skill = card.skill;

        if (iconImage != null)
        {
            iconImage.sprite = skill.icon;
            iconImage.enabled = skill.icon != null;
        }

        if (titleText != null)
            titleText.text = !string.IsNullOrWhiteSpace(skill.displayName)
                ? skill.displayName
                : skill.name;

        if (descriptionText != null)
            descriptionText.text = !string.IsNullOrWhiteSpace(skill.visualDescriptionKr)
                ? skill.visualDescriptionKr
                : string.Empty;

        if (tagText != null)
            tagText.text = "공통 스킬";

        gameObject.SetActive(true);
    }

    // ── 인터페이스 기반 바인딩 ─────────────────

    public void Bind(ILevelUpCardData data)
    {
        if (data == null)
        {
            SetEmpty();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (titleText != null)
            titleText.text = data.TitleKorean;

        if (descriptionText != null)
            descriptionText.text = data.DescriptionKorean;

        if (tagText != null)
        {
            // Tags는 IReadOnlyList<SkillTag>이므로 문자열로 변환
            var tags = data.Tags;
            if (tags != null && tags.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < tags.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(tags[i].ToString());
                }
                tagText.text = sb.ToString();
            }
            else
            {
                tagText.text = string.Empty;
            }
        }

        gameObject.SetActive(true);
    }

    // ── 내부 ──────────────────────────────────

    private void SetEmpty()
    {
        if (iconImage != null)
            iconImage.enabled = false;

        if (titleText != null)
            titleText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (tagText != null)
            tagText.text = string.Empty;

        gameObject.SetActive(false);
    }
}