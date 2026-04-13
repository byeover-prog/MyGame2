using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerExpUIController : MonoBehaviour
{
    [SerializeField] private Image expFill;        // exp_in
    [SerializeField] private TextMeshProUGUI expText; // exp_Text
    [SerializeField] private PlayerExp playerExp;

    private void Start()
    {
        playerExp.OnLevelUp += OnLevelUp;
        UpdateLevel();
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= OnLevelUp;
    }

    private void Update()
    {
        if (expFill == null) return;
        expFill.fillAmount = (float)playerExp.CurrentExp / playerExp.RequiredExp;
    }

    private void OnLevelUp(int level)
    {
        UpdateLevel();
    }

    private void UpdateLevel()
    {
        if (expText != null)
            expText.text = $"Lv. {playerExp.Level}";
    }
}