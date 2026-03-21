using UnityEngine;

/// <summary>
/// 아웃게임 재화(냥)를 관리하는 서비스입니다.
/// SaveManager2D.Data.metaProfile.nyang을 읽고 씁니다.
/// </summary>
public sealed class MetaWalletService2D
{
    private readonly SaveManager2D _saveManager;

    public MetaWalletService2D(SaveManager2D saveManager = null)
    {
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
    }

    /// <summary>현재 보유 냥입니다.</summary>
    public int Nyang
    {
        get
        {
            MetaProfileSaveData2D meta = EnsureMeta();
            return meta != null ? meta.nyang : 0;
        }
    }

    /// <summary>해당 금액을 지불할 수 있는지 확인합니다.</summary>
    public bool CanSpendNyang(int amount)
    {
        return amount >= 0 && Nyang >= amount;
    }

    /// <summary>
    /// 냥을 지불합니다. 성공하면 true를 반환합니다.
    /// </summary>
    public bool SpendNyang(int amount, bool autoSave = true)
    {
        if (amount < 0) return false;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null || meta.nyang < amount) return false;

        meta.nyang -= amount;

        if (autoSave) Save();
        return true;
    }

    /// <summary>
    /// 냥을 추가합니다. (보상, 환급 등)
    /// </summary>
    public void AddNyang(int amount, bool autoSave = true)
    {
        if (amount <= 0) return;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;

        meta.nyang += amount;

        if (autoSave) Save();
    }

    /// <summary>
    /// 디버그용: 냥을 직접 설정합니다.
    /// </summary>
    public void DebugSetNyang(int value)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;
        meta.nyang = Mathf.Max(0, value);
        Save();
        Debug.Log($"[MetaWalletService2D] 디버그: 냥을 {meta.nyang}으로 설정했습니다.");
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save()
    {
        if (_saveManager != null) _saveManager.Save();
    }
}