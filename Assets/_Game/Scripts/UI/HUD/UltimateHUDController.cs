using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UltimateHUDController : MonoBehaviour
{
    [Header("R키 궁극기")]
    [SerializeField] private UltimateController2D ultimateController;
    [SerializeField] private Image icon_R;
    [SerializeField] private Image cooldownFill_R;
    [SerializeField] private TextMeshProUGUI text_R;

    [Header("T키 궁극기")]
    [SerializeField] private SupportUltimateController2D supportController;
    [SerializeField] private Image icon_T;
    [SerializeField] private Image cooldownFill_T;
    [SerializeField] private TextMeshProUGUI text_T;

    [Header("스쿼드 참조")]
    [SerializeField] private SquadLoadout2D squadLoadout;

    private void Start()
    {
        ApplyIcons();
        if (squadLoadout != null)
            squadLoadout.OnLoadoutChanged += ApplyIcons;
    }

    private void OnDestroy()
    {
        if (squadLoadout != null)
            squadLoadout.OnLoadoutChanged -= ApplyIcons;
    }

    private void ApplyIcons()
    {
        if (squadLoadout == null) return;

        // R키 = 메인 캐릭터 궁극기
        var mainIcon = squadLoadout.Main?.UltimateSkillIcon;
        if (icon_R != null && mainIcon != null)
            icon_R.sprite = mainIcon;

        // T키 = 지원1 궁극기 (지원1 우선, 없으면 지원2)
        var supportIcon = squadLoadout.Support1?.UltimateSkillIcon
                       ?? squadLoadout.Support2?.UltimateSkillIcon;
        if (icon_T != null && supportIcon != null)
            icon_T.sprite = supportIcon;
    }

    private void Update()
    {
        UpdateSlot(
            ultimateController != null ? ultimateController.CooldownRemaining : 0f,
            ultimateController != null ? ultimateController.CooldownTotal : 1f,
            ultimateController != null && ultimateController.IsReady,
            cooldownFill_R, icon_R, text_R);

        UpdateSlot(
            supportController != null ? supportController.CooldownRemaining : 0f,
            supportController != null ? supportController.CooldownTotal : 1f,
            supportController != null && supportController.IsReady,
            cooldownFill_T, icon_T, text_T);
    }

    private void UpdateSlot(float remaining, float total, bool isReady,
        Image fill, Image icon, TextMeshProUGUI text)
    {
        if (fill != null)
            fill.fillAmount = isReady ? 0f : remaining / total;

        if (icon != null)
            if (icon != null)
                icon.color = Color.white;

        if (text != null)
            text.text = isReady ? "" : Mathf.CeilToInt(remaining).ToString();
    }
}