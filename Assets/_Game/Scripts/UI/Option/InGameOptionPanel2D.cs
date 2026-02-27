// UTF-8
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

    [Header("배속 버튼(0.5/1/2)")]
    [SerializeField, InspectorName("0.5x 버튼")]
    private Button speed05Button;

    [SerializeField, InspectorName("1.0x 버튼")]
    private Button speed10Button;

    [SerializeField, InspectorName("2.0x 버튼")]
    private Button speed20Button;

    [Header("확인 버튼")]
    [SerializeField, InspectorName("확인 버튼")]
    private Button confirmButton;

    private bool _isOpen;
    private float _resumeTimeScale = 1f;

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

        if (speed05Button != null) speed05Button.onClick.AddListener(() => OnSpeedClicked(0.5f));
        if (speed10Button != null) speed10Button.onClick.AddListener(() => OnSpeedClicked(1.0f));
        if (speed20Button != null) speed20Button.onClick.AddListener(() => OnSpeedClicked(2.0f));

        if (confirmButton != null) confirmButton.onClick.AddListener(Close);
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
                _resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                float ts = 1f;
                if (GameSettingsRuntime.HasInstance) ts = GameSettingsRuntime.Instance.TimeScale;
                Time.timeScale = ts;
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

        // 옵션이 닫혀있는 상태에서 누를 수도 있으니 즉시 반영
        if (!_isOpen && pauseGameOnOpen)
            Time.timeScale = GameSettingsRuntime.Instance.TimeScale;
    }
}