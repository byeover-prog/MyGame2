using UnityEngine;

/// <summary>
/// 재화(냥, 혼령)의 저장 및 관리를 담당하는 싱글톤 매니저
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    // PlayerPrefs 키 상수
    private const string KEY_NYANG = "Currency_Nyang";
    private const string KEY_SPIRIT = "Currency_Spirit";

    // 기존 보유 재화 (PlayerPrefs에서 로드)
    public int BaseNyang { get; private set; }
    public int BaseSpirit { get; private set; }

    // 이번 스테이지에서 획득한 재화 (런타임 누적)
    public int StagedNyang { get; private set; }
    public int StagedSpirit { get; private set; }

    // 합산 재화 (표시용)
    public int TotalNyang => BaseNyang + StagedNyang;
    public int TotalSpirit => BaseSpirit + StagedSpirit;

    // 재화 변경 이벤트 (UI에서 구독)
    public event System.Action OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCurrency();
    }

    /// <summary>
    /// PlayerPrefs에서 기존 재화 로드
    /// </summary>
    private void LoadCurrency()
    {
        BaseNyang = PlayerPrefs.GetInt(KEY_NYANG, 0);
        BaseSpirit = PlayerPrefs.GetInt(KEY_SPIRIT, 0);
        StagedNyang = 0;
        StagedSpirit = 0;
    }

    /// <summary>
    /// 스테이지 시작 시 호출 - 스테이지 획득량 초기화
    /// </summary>
    public void ResetStageRewards()
    {
        StagedNyang = 0;
        StagedSpirit = 0;
        OnCurrencyChanged?.Invoke();
    }

    /// <summary>
    /// 스테이지 중 냥 획득
    /// </summary>
    public void AddNyang(int amount)
    {
        if (amount <= 0) return;
        StagedNyang += amount;
        OnCurrencyChanged?.Invoke();
    }

    /// <summary>
    /// 스테이지 중 혼령 획득
    /// </summary>
    public void AddSpirit(int amount)
    {
        if (amount <= 0) return;
        StagedSpirit += amount;
        OnCurrencyChanged?.Invoke();
    }

    /// <summary>
    /// 스테이지 클리어 시 호출 - 스테이지 획득분을 기존 재화에 합산하고 저장
    /// </summary>
    public void SaveStageClearRewards()
    {
        BaseNyang += StagedNyang;
        BaseSpirit += StagedSpirit;

        PlayerPrefs.SetInt(KEY_NYANG, BaseNyang);
        PlayerPrefs.SetInt(KEY_SPIRIT, BaseSpirit);
        PlayerPrefs.Save();

        // 스테이지 획득량은 ClearUI 표시용으로 유지 (ResetStageRewards 호출 전까지)
        OnCurrencyChanged?.Invoke();

        Debug.Log($"[CurrencyManager] 저장 완료 - 냥: {BaseNyang}, 혼령: {BaseSpirit}");
    }

    /// <summary>
    /// 재화 소비 (냥) - 성공 여부 반환
    /// </summary>
    public bool SpendNyang(int amount)
    {
        if (BaseNyang < amount)
        {
            Debug.LogWarning("[CurrencyManager] 냥 부족");
            return false;
        }
        BaseNyang -= amount;
        PlayerPrefs.SetInt(KEY_NYANG, BaseNyang);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 재화 소비 (혼령) - 성공 여부 반환
    /// </summary>
    public bool SpendSpirit(int amount)
    {
        if (BaseSpirit < amount)
        {
            Debug.LogWarning("[CurrencyManager] 혼령 부족");
            return false;
        }
        BaseSpirit -= amount;
        PlayerPrefs.SetInt(KEY_SPIRIT, BaseSpirit);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke();
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Add 100 Nyang")]
    private void DebugAddNyang() => AddNyang(100);

    [ContextMenu("Debug/Add 10 Spirit")]
    private void DebugAddSpirit() => AddSpirit(10);

    [ContextMenu("Debug/Reset All Currency")]
    private void DebugReset()
    {
        PlayerPrefs.DeleteKey(KEY_NYANG);
        PlayerPrefs.DeleteKey(KEY_SPIRIT);
        LoadCurrency();
        OnCurrencyChanged?.Invoke();
        Debug.Log("[CurrencyManager] 재화 초기화 완료");
    }
#endif
}
