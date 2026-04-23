using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerProfileUIController : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("바람속성 확인")]
    [SerializeField] private CharacterCatalogSO characterCatalog;
    [SerializeField] private GameObject thirdDashSlot; // 3번째 대쉬 슬롯 오브젝트

    private Tween _hpTween;
    private int _lastHp;

    private void Start()
    {
        CheckWindAttribute();
        _lastHp = playerHealth.CurrentHp;
        hpFill.fillAmount = (float)playerHealth.CurrentHp / playerHealth.MaxHp;
        UpdateHpText();
    }

    private void CheckWindAttribute()
    {
        bool hasWind = false;

        string[] ids = new[]
        {
            SquadLoadoutRuntime.MainId,
            SquadLoadoutRuntime.Support1Id,
            SquadLoadoutRuntime.Support2Id
        };

        foreach (string id in ids)
        {
            if (characterCatalog.TryFindById(id, out CharacterDefinitionSO def))
            {
                if (def.Attribute == CharacterAttributeKind.Wind)
                {
                    hasWind = true;
                    break;
                }
            }
        }

        if (thirdDashSlot != null)
            thirdDashSlot.SetActive(hasWind);
    }

    private void Update()
    {
        UpdateHp();
    }

    private void UpdateHp()
    {
        if (hpFill == null || playerHealth == null) return;
        if (_lastHp == playerHealth.CurrentHp) return;

        _lastHp = playerHealth.CurrentHp;
        float ratio = (float)playerHealth.CurrentHp / playerHealth.MaxHp;

        _hpTween?.Kill();
        _hpTween = hpFill
            .DOFillAmount(ratio, 0.3f)
            .SetEase(Ease.OutCubic);

        UpdateHpText();
    }

    private void UpdateHpText()
    {
        if (hpText != null)
            hpText.text = $"{playerHealth.CurrentHp}/{playerHealth.MaxHp}";
    }

    private void OnDestroy()
    {
        _hpTween?.Kill();
    }
}