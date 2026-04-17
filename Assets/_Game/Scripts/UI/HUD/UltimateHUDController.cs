using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UltimateHUDController : MonoBehaviour
{
    [Header("R키 궁극기")]
    [SerializeField] private UltimateController2D ultimateController;
    [SerializeField] private Image icon_R;
    [SerializeField] private Image cooldownFill_R;  // In_ULT_R
    [SerializeField] private TextMeshProUGUI text_R; // 쿨타임 숫자 (선택)

    [Header("T키 궁극기")]
    [SerializeField] private SupportUltimateController2D supportController;
    [SerializeField] private Image icon_T;
    [SerializeField] private Image cooldownFill_T;  // In_ULT_T
    [SerializeField] private TextMeshProUGUI text_T;

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
            icon.color = isReady ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);

        if (text != null)
            text.text = isReady ? "" : Mathf.CeilToInt(remaining).ToString();
    }
}