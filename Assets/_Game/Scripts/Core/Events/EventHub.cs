using System;
using UnityEngine;

// 프로젝트 전역 이벤트 허브(델리게이트 집합).

[DefaultExecutionOrder(-9000)]
[DisallowMultipleComponent]
public sealed class EventHub : MonoBehaviour
{
    public static EventHub Instance { get; private set; }

    [Header("옵션")]
    [SerializeField, Tooltip("씬 전환에도 유지")]
    private bool dontDestroyOnLoad = true;
    
    // Squad / Character
    
    public event Action<int, CharacterDefinitionSO> OnSquadSlotChanged;
    public event Action<int> OnSquadSlotSelected;
    
    // LevelUp
    
    public event Action OnLevelUpPanelOpened;
    public event Action OnLevelUpPanelClosed;
    
    // Skill state
    
    public event Action<int, int> OnWeaponSlotLevelChanged;
    public event Action<CommonSkillKind, int> OnCommonSkillLevelChanged;
    
    // Pause
    
    public event Action<bool> OnPauseRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    // Emit API

    public void EmitSquadSlotChanged(int slotIndex, CharacterDefinitionSO character)
        => OnSquadSlotChanged?.Invoke(slotIndex, character);

    public void EmitSquadSlotSelected(int slotIndex)
        => OnSquadSlotSelected?.Invoke(slotIndex);

    public void EmitLevelUpOpened()
        => OnLevelUpPanelOpened?.Invoke();

    public void EmitLevelUpClosed()
        => OnLevelUpPanelClosed?.Invoke();

    public void EmitWeaponSlotLevelChanged(int slotIndex, int level)
        => OnWeaponSlotLevelChanged?.Invoke(slotIndex, level);

    public void EmitCommonSkillLevelChanged(CommonSkillKind kind, int level)
        => OnCommonSkillLevelChanged?.Invoke(kind, level);

    public void EmitPauseRequested(bool pause)
        => OnPauseRequested?.Invoke(pause);
}