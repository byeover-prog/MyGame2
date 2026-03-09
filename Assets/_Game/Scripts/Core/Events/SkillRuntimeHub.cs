using UnityEngine;

/// <summary>
/// "스킬 매니저 분리"를 유지하면서, 외부(UI/튜토리얼/로그)가
/// 시스템들을 직접 참조하지 않도록 중간에서 묶어주는 허브.
///
/// 역할:
/// - EventHub를 찾아서 캐싱
/// - WeaponShooter / CommonSkillManager / LevelUpSystem / Squad 컨트롤러 등과의 연결 지점을 제공
/// - 지금 단계에서는 '자동 구독'보다, 각 시스템에서 허브 메서드 1줄 호출하는 방식이 가장 안전(오버코딩 방지)
///
/// 복잡도: O(1)
/// </summary>
[DisallowMultipleComponent]
public sealed class SkillRuntimeHub : MonoBehaviour
{
    [Header("허브")]
    [SerializeField] private EventHub eventHub;

    [Header("참조(선택: 자동 탐색)")]
    [SerializeField] private WeaponShooterSystem2D shooter;
    [SerializeField] private SkillLevelRuntimeState2D levelState;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    private void Awake()
    {
        if (eventHub == null) eventHub = EventHub.Instance;
        if (eventHub == null) eventHub = FindFirstObjectByType<EventHub>();

        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        if (levelState == null) levelState = FindFirstObjectByType<SkillLevelRuntimeState2D>();
        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
    }

    // ----------------------------
    // Squad
    // ----------------------------
    public void NotifySquadSlotSelected(int slotIndex)
    {
        if (eventHub == null) return;
        eventHub.EmitSquadSlotSelected(slotIndex);
    }

    public void NotifySquadSlotChanged(int slotIndex, CharacterDefinitionSO character)
    {
        if (eventHub == null) return;
        eventHub.EmitSquadSlotChanged(slotIndex, character);
    }

    // ----------------------------
    // LevelUp
    // ----------------------------
    public void NotifyLevelUpOpened()
    {
        if (eventHub == null) return;
        eventHub.EmitLevelUpOpened();
        eventHub.EmitPauseRequested(true);
    }

    public void NotifyLevelUpClosed()
    {
        if (eventHub == null) return;
        eventHub.EmitLevelUpClosed();
        eventHub.EmitPauseRequested(false);
    }

    public void NotifyLevelUpOffers(ILevelUpCardData[] offers)
    {
        if (eventHub == null) return;
        eventHub.EmitLevelUpOffers(offers);
    }

    public void NotifyLevelUpPicked(ILevelUpCardData picked)
    {
        if (eventHub == null) return;
        eventHub.EmitLevelUpPicked(picked);
    }

    // ----------------------------
    // Skill State (선택)
    // ----------------------------
    public void NotifyWeaponSlotLevelChanged(int slotIndex)
    {
        if (eventHub == null || levelState == null) return;
        int lv = levelState.GetLevel(slotIndex);
        eventHub.EmitWeaponSlotLevelChanged(slotIndex, lv);
    }

    public void NotifyCommonSkillLevelChanged(CommonSkillKind kind)
    {
        if (eventHub == null || commonSkillManager == null) return;
        int lv = commonSkillManager.GetLevel(kind);
        eventHub.EmitCommonSkillLevelChanged(kind, lv);
    }
}