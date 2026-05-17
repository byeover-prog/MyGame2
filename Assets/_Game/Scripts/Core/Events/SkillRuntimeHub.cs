using UnityEngine;

// 외부(UI/튜토리얼/로그)가 시스템들을 직접 참조하지 않도록 중간에서 묶어주는 허브.

[DisallowMultipleComponent]
public sealed class SkillRuntimeHub : MonoBehaviour
{
    [Header("허브")]
    [SerializeField] private GameSceneContext sceneContext;
    [SerializeField] private EventHub eventHub;

    [Header("참조(선택: 자동 탐색)")]
    [SerializeField] private WeaponShooterSystem2D shooter;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    private void Awake()
    {
        ResolveSceneContext();

        if (eventHub == null) eventHub = EventHub.Instance;
        if (eventHub == null && sceneContext != null) eventHub = sceneContext.GetSystemsComponent<EventHub>();
        if (eventHub == null) eventHub = FindFirstObjectByType<EventHub>();

        if (shooter == null && sceneContext != null) shooter = sceneContext.GetPlayerComponent<WeaponShooterSystem2D>();
        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();

        if (commonSkillManager == null && sceneContext != null) commonSkillManager = sceneContext.GetSystemsComponent<CommonSkillManager2D>();
        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
    }

    private void ResolveSceneContext()
    {
        if (sceneContext == null)
            sceneContext = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (sceneContext != null)
            sceneContext.ResolveMissingReferences();
    }
    
    // Squad

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
    
    // LevelUp

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

    // Skill State
    
    public void NotifyCommonSkillLevelChanged(CommonSkillKind kind)
    {
        if (eventHub == null || commonSkillManager == null) return;
        int lv = commonSkillManager.GetLevel(kind);
        eventHub.EmitCommonSkillLevelChanged(kind, lv);
    }
}
