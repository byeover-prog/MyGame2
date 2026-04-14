using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerExpUIController : MonoBehaviour
{
    [SerializeField] private Image expFill;        // exp_in
    [SerializeField] private TextMeshProUGUI expText; // exp_Text
    [SerializeField] private PlayerExp playerExp;

	private Tween _expTween;
    private float _lastRatio = -1f;
    private bool _levelingUp = false;

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
        if (expFill == null || _levelingUp) return;

		float ratio = (float)playerExp.CurrentExp / playerExp.RequiredExp;
        if(Mathf.Approximately(_lastRatio,ratio))return;

        _lastRatio = ratio;
		_expTween?.Kill();
        _expTween = DOTween.To(
            () => expFill.fillAmount,
            x => expFill.fillAmount = x,
            ratio,
            0.3f
            ).SetEase(Ease.OutCubic);
    }

    private void OnLevelUp(int level)
    {
        UpdateLevel();
        _levelingUp = true;
    
        float newRatio = (float)playerExp.CurrentExp / playerExp.RequiredExp;
    
        _expTween?.Kill();
        _expTween = DOTween.To(
                () => expFill.fillAmount,
                x => expFill.fillAmount = x,
                1f, 0.15f
            ).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                expFill.fillAmount = 0f;
                _lastRatio = 0f;
                _levelingUp = false;
        
                _expTween = DOTween.To(
                    () => expFill.fillAmount,
                    x => expFill.fillAmount = x,
                    newRatio, 0.3f
                ).SetEase(Ease.OutCubic);
            });
    }

    private void UpdateLevel()
    {
        if (expText != null)
            expText.text = $"Lv. {playerExp.Level}";
    }
}