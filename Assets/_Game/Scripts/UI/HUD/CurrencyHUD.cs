using UnityEngine;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    [Header("Scene Context")]
    [SerializeField] private GameSceneContext sceneContext;

    [Header("냥")]
    [SerializeField] private TextMeshProUGUI txtNyang;
    [SerializeField] private PlayerCurrency2D playerCurrency;

    [Header("혼령")]
    [SerializeField] private TextMeshProUGUI txtSoul;
    [SerializeField] private PlayerSpirit2D playerSpirit;

    private void Awake()
    {
        ResolveSceneContext();

        if (playerCurrency == null && sceneContext != null)
            playerCurrency = sceneContext.GetPlayerComponent<PlayerCurrency2D>();

        if (playerCurrency == null)
            playerCurrency = FindFirstObjectByType<PlayerCurrency2D>();

        if (playerSpirit == null && sceneContext != null)
            playerSpirit = sceneContext.GetPlayerComponent<PlayerSpirit2D>();

        if (playerSpirit == null)
            playerSpirit = FindFirstObjectByType<PlayerSpirit2D>();
    }

    private void OnEnable()
    {
        if (playerCurrency != null)
            playerCurrency.OnGoldChanged += OnNyangChanged;

        if (playerSpirit != null)
            playerSpirit.OnSpiritChanged += OnSoulChanged;

        UpdateNyang(playerCurrency != null ? playerCurrency.CurrentGold : 0);
        UpdateSoul(playerSpirit != null ? playerSpirit.CurrentSpirit : 0);
    }

    private void OnDisable()
    {
        if (playerCurrency != null)
            playerCurrency.OnGoldChanged -= OnNyangChanged;

        if (playerSpirit != null)
            playerSpirit.OnSpiritChanged -= OnSoulChanged;
    }

    private void ResolveSceneContext()
    {
        if (sceneContext == null)
            sceneContext = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (sceneContext != null)
            sceneContext.ResolveMissingReferences();
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
