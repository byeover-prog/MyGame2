using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHPUI : MonoBehaviour
{
    [Header("HP 바")]
    [Tooltip("HP Fill 이미지")]
    [SerializeField] private Image hpFill;

    private float maxHp;

    /// <summary>
    /// 최대 체력 설정
    /// </summary>
    public void SetMaxHp(float hp)
    {
        maxHp = hp;
        hpFill.fillAmount = 1f;
    }

    /// <summary>
    /// 현재 체력 반영
    /// </summary>
    public void UpdateHp(float currentHp)
    {
        if (maxHp <= 0) return;

        float ratio = currentHp / maxHp;
        hpFill.fillAmount = ratio;
    }
}