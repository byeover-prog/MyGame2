using System;
using UnityEngine;

/// <summary>
/// 프로젝트 전역 이벤트 허브(델리게이트 집합).
/// 규칙:
/// - UI는 "EventHub만" 구독한다.
/// - 시스템은 EventHub에 Emit만 한다.
/// - 구독은 OnEnable, 해제는 OnDisable에서.
/// - 람다로 구독하지 말고 메서드로 구독(해제 실수 방지).
/// 복잡도: O(1)
/// </summary>
[DefaultExecutionOrder(-9000)]
[DisallowMultipleComponent]
public sealed class EventHub : MonoBehaviour
{
    public static EventHub Instance { get; private set; }

    [Header("옵션")]
    [SerializeField, Tooltip("씬 전환에도 유지")]
    private bool dontDestroyOnLoad = true;

    // ----------------------------
    // Squad / Character
    // ----------------------------
    public event Action<int, CharacterDefinitionSO> OnSquadSlotChanged; // slotIndex(0~2), character
    public event Action<int> OnSquadSlotSelected; // slotIndex

    // ----------------------------
    // LevelUp
    // ----------------------------
    public event Action OnLevelUpPanelOpened;
    public event Action OnLevelUpPanelClosed;
    public event Action<ILevelUpCardData[]> OnLevelUpOffers; // 3장(또는 n장)
    public event Action<ILevelUpCardData> OnLevelUpPicked;

    // ----------------------------
    // Skill state (optional)
    // ----------------------------
    public event Action<int, int> OnWeaponSlotLevelChanged; // slotIndex, level
    public event Action<CommonSkillKind, int> OnCommonSkillLevelChanged; // kind, level

    // ----------------------------
    // Pause (optional)
    // ----------------------------
    public event Action<bool> OnPauseRequested; // true=Pause, false=Resume

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

    // -------- Emit API (시스템이 호출) --------

    public void EmitSquadSlotChanged(int slotIndex, CharacterDefinitionSO character)
        => OnSquadSlotChanged?.Invoke(slotIndex, character);

    public void EmitSquadSlotSelected(int slotIndex)
        => OnSquadSlotSelected?.Invoke(slotIndex);

    public void EmitLevelUpOpened()
        => OnLevelUpPanelOpened?.Invoke();

    public void EmitLevelUpClosed()
        => OnLevelUpPanelClosed?.Invoke();

    public void EmitLevelUpOffers(ILevelUpCardData[] offers)
        => OnLevelUpOffers?.Invoke(offers);

    public void EmitLevelUpPicked(ILevelUpCardData picked)
        => OnLevelUpPicked?.Invoke(picked);

    public void EmitWeaponSlotLevelChanged(int slotIndex, int level)
        => OnWeaponSlotLevelChanged?.Invoke(slotIndex, level);

    public void EmitCommonSkillLevelChanged(CommonSkillKind kind, int level)
        => OnCommonSkillLevelChanged?.Invoke(kind, level);

    public void EmitPauseRequested(bool pause)
        => OnPauseRequested?.Invoke(pause);
}