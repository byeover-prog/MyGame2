using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSettingsRuntime : MonoBehaviour
{
    public static GameSettingsRuntime Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public event Action<float> OnSkillOpacityChanged;
    public event Action<float> OnDamageNumberOpacityChanged;
    public event Action<float> OnTimeScaleChanged;
    public event Action<bool> OnSkillSlotsVisibleChanged;

    private const string KEY_SKILL_OPACITY = "SET_skillOpacity";
    private const string KEY_DMG_OPACITY   = "SET_damageNumberOpacity";
    private const string KEY_TIME_SCALE    = "SET_timeScale";
    private const string KEY_SLOT_VISIBLE  = "SET_skillSlotsVisible";
    private const string KEY_MASTER_VOLUME = "SET_masterVolume";
    private const string KEY_BGM_VOLUME    = "SET_bgmVolume";
    private const string KEY_SFX_VOLUME    = "SET_sfxVolume";
    
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBGMVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;

    [SerializeField, InspectorName("스킬 투명도(0~1)")]
    private float skillOpacity = 0.25f;

    [SerializeField, InspectorName("숫자 투명도(0~1)")]
    private float damageNumberOpacity = 1f;

    [SerializeField, InspectorName("배속(0.5/1/2)")]
    private float timeScale = 1f;

    [SerializeField, InspectorName("스킬 슬롯 표시")]
    private bool skillSlotsVisible = true;
    
    [SerializeField, InspectorName("전체 음량(0~1)")]
    private float masterVolume = 1f;

    [SerializeField, InspectorName("배경음(0~1)")]
    private float bgmVolume = 1f;

    [SerializeField, InspectorName("효과음(0~1)")]
    private float sfxVolume = 1f;
    
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(masterVolume, v)) return;
            masterVolume = v;
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVolume);
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }
    }
    
    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(bgmVolume, v)) return;
            bgmVolume = v;
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolume);
            OnBGMVolumeChanged?.Invoke(bgmVolume);
        }
    }
    
    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(sfxVolume, v)) return;
            sfxVolume = v;
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
            OnSFXVolumeChanged?.Invoke(sfxVolume);
        }
    }

    public float SkillOpacity
    {
        get => skillOpacity;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(skillOpacity, v)) return;
            skillOpacity = v;
            PlayerPrefs.SetFloat(KEY_SKILL_OPACITY, skillOpacity);
            OnSkillOpacityChanged?.Invoke(skillOpacity);
        }
    }

    public float DamageNumberOpacity
    {
        get => damageNumberOpacity;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(damageNumberOpacity, v)) return;
            damageNumberOpacity = v;
            PlayerPrefs.SetFloat(KEY_DMG_OPACITY, damageNumberOpacity);
            OnDamageNumberOpacityChanged?.Invoke(damageNumberOpacity);
        }
    }

    public float TimeScale
    {
        get => timeScale;
        set
        {
            float v = value;
            // 허용값을 강제(원하면 여기서만 바꾸면 됨)
            if (v < 0.75f) v = 0.5f;
            else if (v < 1.5f) v = 1f;
            else v = 2f;

            if (Mathf.Approximately(timeScale, v)) return;
            timeScale = v;
            PlayerPrefs.SetFloat(KEY_TIME_SCALE, timeScale);
            OnTimeScaleChanged?.Invoke(timeScale);

            // ★ GamePauseGate2D 연동: 일시정지 중이면 Time.timeScale을 건드리지 않음
            if (!GamePauseGate2D.IsPaused)
                Time.timeScale = timeScale;
        }
    }

    public bool SkillSlotsVisible
    {
        get => skillSlotsVisible;
        set
        {
            if (skillSlotsVisible == value) return;
            skillSlotsVisible = value;
            PlayerPrefs.SetInt(KEY_SLOT_VISIBLE, skillSlotsVisible ? 1 : 0);
            OnSkillSlotsVisibleChanged?.Invoke(skillSlotsVisible);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장값 로드
        skillOpacity = PlayerPrefs.GetFloat(KEY_SKILL_OPACITY, skillOpacity);
        damageNumberOpacity = PlayerPrefs.GetFloat(KEY_DMG_OPACITY, damageNumberOpacity);
        timeScale = PlayerPrefs.GetFloat(KEY_TIME_SCALE, timeScale);
        skillSlotsVisible = PlayerPrefs.GetInt(KEY_SLOT_VISIBLE, skillSlotsVisible ? 1 : 0) == 1;
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, masterVolume);
        bgmVolume    = PlayerPrefs.GetFloat(KEY_BGM_VOLUME,    bgmVolume);
        sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX_VOLUME,    sfxVolume);

        // ★ 저장된 배속을 즉시 적용 (일시정지 중이 아닐 때만)
        if (!GamePauseGate2D.IsPaused)
            Time.timeScale = timeScale;
    }
}