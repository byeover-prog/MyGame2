using UnityEngine;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    [Header("냥")]
    [SerializeField] private TextMeshProUGUI txtNyang;
    [SerializeField] private PlayerCurrency2D playerCurrency;

    [Header("혼령")]
    [SerializeField] private TextMeshProUGUI txtSoul;
    [SerializeField] private PlayerSpirit2D playerSpirit;

    private void Awake()
    {
        if (playerCurrency == null)
            playerCurrency = FindFirstObjectByType<PlayerCurrency2D>();
        if (playerSpirit == null)
            playerSpirit = FindFirstObjectByType<PlayerSpirit2D>();
    }

    private void OnEnable()
    {
        playerCurrency.OnGoldChanged   += OnNyangChanged;
        playerSpirit.OnSpiritChanged   += OnSoulChanged;

        // 초기값 표시
        UpdateNyang(playerCurrency.CurrentGold);
        UpdateSoul(playerSpirit.CurrentSpirit);
    }

    private void OnDisable()
    {
        playerCurrency.OnGoldChanged   -= OnNyangChanged;
        playerSpirit.OnSpiritChanged   -= OnSoulChanged;
    }

    private void OnNyangChanged(int total, int delta) => UpdateNyang(total);
    private void OnSoulChanged(int total, int delta)  => UpdateSoul(total);

    private void UpdateNyang(int value) 
    { 
        if (txtNyang != null) txtNyang.text = value.ToString(); 
    }
    
    private void UpdateSoul(int value)  
    { 
        if (txtSoul != null) txtSoul.text = value.ToString();  
    }
}