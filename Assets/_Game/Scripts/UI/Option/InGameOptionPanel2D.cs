using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InGameOptionPanel2D : MonoBehaviour
{
    [Header("루트")]
    [SerializeField, InspectorName("옵션 캔버스 그룹")]
    [Tooltip("옵션 패널 전체에 CanvasGroup을 붙이고 연결하세요. (alpha/클릭차단용)")]
    private CanvasGroup canvasGroup;

    [SerializeField, InspectorName("열기/닫기 키")]
    private KeyCode toggleKey = KeyCode.Escape;

    [SerializeField, InspectorName("열 때 일시정지")]
    private bool pauseGameOnOpen = true;

    [Header("슬라이더 - 투명도")]
    [SerializeField, InspectorName("스킬 투명도 슬라이더")]
    private Slider skillOpacitySlider;

    [SerializeField, InspectorName("스킬 투명도 값 텍스트")]
    private TMP_Text skillOpacityValueText;

    [SerializeField, InspectorName("숫자 투명도 슬라이더")]
    private Slider damageNumberOpacitySlider;

    [SerializeField, InspectorName("숫자 투명도 값 텍스트")]
    private TMP_Text damageNumberOpacityValueText;
    
    [Header("슬라이더 - 음량")]
    [SerializeField, InspectorName("전체 음량 슬라이더")]
    private Slider masterVolumeSlider;
    [SerializeField, InspectorName("전체 음량 값 텍스트")]
    private TMP_Text masterVolumeValueText;

    [SerializeField, InspectorName("배경음 슬라이더")]
    private Slider bgmVolumeSlider;
    [SerializeField, InspectorName("배경음 값 텍스트")]
    private TMP_Text bgmVolumeValueText;

    [SerializeField, InspectorName("효과음 슬라이더")]
    private Slider sfxVolumeSlider;
    [SerializeField, InspectorName("효과음 값 텍스트")]
    private TMP_Text sfxVolumeValueText;

    [Header("배속 버튼(1/1.5/2)")]
    [SerializeField, InspectorName("1x 버튼")]
    private Button speed10Button;

    [SerializeField, InspectorName("1.5x 버튼")]
    private Button speed15Button;

    [SerializeField, InspectorName("2.0x 버튼")]
    private Button speed20Button;

    [Header("확인 버튼")]
    [SerializeField, InspectorName("확인 버튼")]
    private Button confirmButton;

    private bool _isOpen;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        HookUIEvents();
        ApplyFromSettingsToUI();
        SetOpen(false, true);
    }

    private void HookUIEvents()
    {
        if (skillOpacitySlider != null)
            skillOpacitySlider.onValueChanged.AddListener(OnSkillOpacityChanged);

        if (damageNumberOpacitySlider != null)
            damageNumberOpacitySlider.onValueChanged.AddListener(OnDamageNumberOpacityChanged);

        if (speed10Button != null) speed10Button.onClick.AddListener(() => OnSpeedClicked(1.0f));
        if (speed15Button != null) speed15Button.onClick.AddListener(() => OnSpeedClicked(1.5f));
        if (speed20Button != null) speed20Button.onClick.AddListener(() => OnSpeedClicked(2.0f));

        if (confirmButton != null) confirmButton.onClick.AddListener(Close);
        
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        SetOpen(true, false);
    }

    public void Close()
    {
        SetOpen(false, false);
    }

    private void SetOpen(bool open, bool instant)
    {
        _isOpen = open;
        if (!instant) gameObject.SetActive(open);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = open ? 1f : 0f;
            canvasGroup.blocksRaycasts = open;
            canvasGroup.interactable = open;
        }

        if (pauseGameOnOpen)
        {
            if (open)
            {
                GamePauseGate2D.Acquire(this);
            }
            else
            {
                GamePauseGate2D.Release(this);
            }
        }

        if (open)
            ApplyFromSettingsToUI();
    }

    private void ApplyFromSettingsToUI()
    {
        if (!GameSettingsRuntime.HasInstance) return;

        if (skillOpacitySlider != null)
            skillOpacitySlider.value = GameSettingsRuntime.Instance.SkillOpacity;

        if (damageNumberOpacitySlider != null)
            damageNumberOpacitySlider.value = GameSettingsRuntime.Instance.DamageNumberOpacity;

        RefreshValueTexts();
    }

    private void RefreshValueTexts()
    {
        if (skillOpacityValueText != null && skillOpacitySlider != null)
            skillOpacityValueText.text = $"{Mathf.RoundToInt(skillOpacitySlider.value * 100f)}%";

        if (damageNumberOpacityValueText != null && damageNumberOpacitySlider != null)
            damageNumberOpacityValueText.text = $"{Mathf.RoundToInt(damageNumberOpacitySlider.value * 100f)}%";
        
        if (masterVolumeValueText != null && masterVolumeSlider != null)
            masterVolumeValueText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100f)}%";
        
        if (bgmVolumeValueText != null && bgmVolumeSlider != null)
            bgmVolumeValueText.text = $"{Mathf.RoundToInt(bgmVolumeSlider.value * 100f)}%";
        
        if (sfxVolumeValueText != null && sfxVolumeSlider != null)
            sfxVolumeValueText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100f)}%";
    }
    
    private void OnMasterVolumeChanged(float v)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.MasterVolume = v;
        RefreshValueTexts();
    }
    private void OnBGMVolumeChanged(float v)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.BGMVolume = v;
        RefreshValueTexts();
    }
    private void OnSFXVolumeChanged(float v)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.SFXVolume = v;
        RefreshValueTexts();
    }

    private void OnSkillOpacityChanged(float v)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.SkillOpacity = v;

        RefreshValueTexts();
    }

    private void OnDamageNumberOpacityChanged(float v)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.DamageNumberOpacity = v;

        RefreshValueTexts();
    }

    private void OnSpeedClicked(float timeScale)
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.TimeScale = timeScale;
        // GameSettingsRuntime.TimeScale setter가 GamePauseGate2D.IsPaused 확인 후 반영
    }
}