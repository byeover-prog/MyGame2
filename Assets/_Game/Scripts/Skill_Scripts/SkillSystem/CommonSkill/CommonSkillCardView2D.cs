using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            string nm = (skill != null) ? skill.displayName : "NULL";
            titleText.text = $"{nm}  Lv.{currentLevel + 1}";
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

    private string BuildDesc(CommonSkillConfigSO skill, int curLv)
    {
        if (skill == null) return "";

        int nextLv = Mathf.Clamp(curLv + 1, 1, Mathf.Max(1, skill.maxLevel));

        var cur = (curLv <= 0) ? default : skill.GetLevelParams(curLv);
        var nxt = skill.GetLevelParams(nextLv);

        switch (skill.kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return $"검 개수 {nxt.projectileCount} / 피해 {nxt.damage} / 틱 {nxt.hitInterval:0.00}s";
            case CommonSkillKind.Boomerang:
                return $"투사체 {nxt.projectileCount} / 피해 {nxt.damage} / 쿨 {nxt.cooldown:0.00}s";
            case CommonSkillKind.PiercingBullet:
                return $"피해 {nxt.damage} / 쿨 {nxt.cooldown:0.00}s / 관통";
            case CommonSkillKind.HomingMissile:
                return $"피해 {nxt.damage} / 추가 타격 {nxt.chainCount} / 쿨 {nxt.cooldown:0.00}s";
            case CommonSkillKind.DarkOrb:
                return $"피해 {nxt.damage} / 분열 {nxt.splitCount} / 쿨 {nxt.cooldown:0.00}s";
            case CommonSkillKind.Shuriken:
                return $"피해 {nxt.damage} / 튕김 {nxt.bounceCount} / 쿨 {nxt.cooldown:0.00}s";
            case CommonSkillKind.ArrowShot:
                return $"화살 {nxt.projectileCount} / 피해 {nxt.damage} / 쿨 {nxt.cooldown:0.00}s";
            case CommonSkillKind.Balsi:
                return $"피해 {nxt.damage} / 쿨 {nxt.cooldown:0.00}s";
            default:
                return $"레벨 +1";
        }
    }

    private void OnClick()
    {
        if (_picker == null || _card == null) return;
        _picker.Pick(_card);
    }
}
