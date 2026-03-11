using TMPro;
using UnityEngine;
using _Game.Scripts.UI.HUD;

/// <summary>
/// 인게임 HUD 전체를 실제 게임 시스템에 연결하는 브릿지.
///
/// 담당:
///   - HP bar: PlayerHealth → PlayerHPUI (매 프레임 폴링)
///   - 타이머: 경과 시간 표시 (00:01부터 올라감)
///   - 스킬 슬롯: CommonSkillManager2D 이벤트 → InGameHudUI
///   - HP 텍스트: "현재/최대" 형식
///   - 퀘스트 UI: 이벤트 없으면 숨기기
/// </summary>
[DisallowMultipleComponent]
public sealed class HudConnector : MonoBehaviour
{
    // ── HP ──────────────────────────────────────

    [Header("=== HP 바 ===")]

    [SerializeField, Tooltip("HP Fill 이미지를 제어하는 PlayerHPUI")]
    private PlayerHPUI hpUI;

    [SerializeField, Tooltip("HP 텍스트 (예: 200/200). 비우면 텍스트 표시 안 함")]
    private TMP_Text hpText;

    [SerializeField, Tooltip("플레이어 체력 컴포넌트")]
    private PlayerHealth playerHealth;

    // ── 타이머 ─────────────────────────────────

    [Header("=== 경과 시간 ===")]

    [SerializeField, Tooltip("경과 시간 텍스트 (예: 01:23)")]
    private TMP_Text timerText;

    [SerializeField, Tooltip("스테이지 매니저 (경과 시간 읽기)")]
    private StageManager2D stageManager;

    // ── 스킬 슬롯 ──────────────────────────────

    [Header("=== 스킬 슬롯 HUD ===")]

    [SerializeField, Tooltip("인게임 HUD (스킬 슬롯 관리)")]
    private InGameHudUI hudUI;

    [SerializeField, Tooltip("공통 스킬 매니저 (스킬 획득/레벨업 이벤트)")]
    private CommonSkillManager2D commonSkillManager;

    [SerializeField, Tooltip("공통 스킬 카탈로그 (아이콘 조회용)")]
    private CommonSkillCatalogSO commonSkillCatalog;

    // ── 퀘스트 UI ──────────────────────────────

    [Header("=== 퀘스트/이벤트 UI (이벤트 없으면 숨김) ===")]

    [SerializeField, Tooltip("퀘스트 패널 루트 (이벤트 없으면 비활성화)")]
    private GameObject questPanelRoot;

    [SerializeField, Tooltip("목표 거리 패널 루트")]
    private GameObject distancePanelRoot;

    // ── 내부 상태 ──────────────────────────────

    private int _lastMaxHp;
    private int _lastCurrentHp;
    private int _lastTimerSeconds = -1;

    // ════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════

    private void Awake()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (stageManager == null) stageManager = FindFirstObjectByType<StageManager2D>();
        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
        if (hudUI == null) hudUI = GetComponent<InGameHudUI>();
        if (hudUI == null) hudUI = FindFirstObjectByType<InGameHudUI>();
    }

    private void OnEnable()
    {
        if (commonSkillManager != null)
        {
            commonSkillManager.OnSkillAcquired += HandleSkillAcquired;
            commonSkillManager.OnSkillLevelChanged += HandleSkillLevelChanged;
        }
    }

    private void OnDisable()
    {
        if (commonSkillManager != null)
        {
            commonSkillManager.OnSkillAcquired -= HandleSkillAcquired;
            commonSkillManager.OnSkillLevelChanged -= HandleSkillLevelChanged;
        }
    }

    private void Start()
    {
        // HP 초기화
        if (playerHealth != null && hpUI != null)
        {
            hpUI.SetMaxHp(playerHealth.MaxHp);
            hpUI.UpdateHp(playerHealth.CurrentHp);
            _lastMaxHp = playerHealth.MaxHp;
            _lastCurrentHp = playerHealth.CurrentHp;
            UpdateHpText();
        }

        // 퀘스트/거리 UI 숨기기 (이벤트 없을 때)
        HideQuestUI();

        // ★ 이미 획득된 스킬 스캔 (시작 스킬 등)
        // 약간의 딜레이를 두어 CommonSkillStartBinder2D 실행 후에 스캔
        Invoke(nameof(ScanExistingSkills), 0.5f);
    }

    // ════════════════════════════════════════════
    //  매 프레임 폴링
    // ════════════════════════════════════════════

    private void Update()
    {
        UpdateHP();
        UpdateTimer();
    }

    // ── HP 갱신 ────────────────────────────────

    private void UpdateHP()
    {
        if (playerHealth == null || hpUI == null) return;

        int curHp = playerHealth.CurrentHp;
        int maxHp = playerHealth.MaxHp;

        if (maxHp != _lastMaxHp)
        {
            hpUI.SetMaxHp(maxHp);
            _lastMaxHp = maxHp;
        }

        if (curHp != _lastCurrentHp || maxHp != _lastMaxHp)
        {
            hpUI.UpdateHp(curHp);
            _lastCurrentHp = curHp;
            UpdateHpText();
        }
    }

    private void UpdateHpText()
    {
        if (hpText == null) return;
        hpText.text = $"{_lastCurrentHp}/{_lastMaxHp}";
    }

    // ── 타이머 갱신 (경과 시간, 00:00부터 올라감) ──

    private void UpdateTimer()
    {
        if (timerText == null || stageManager == null) return;

        float elapsed = stageManager.ElapsedTime;
        if (elapsed < 0f) elapsed = 0f;

        int totalSeconds = Mathf.FloorToInt(elapsed);

        // 같은 초면 텍스트 갱신 안 함 (성능)
        if (totalSeconds == _lastTimerSeconds) return;
        _lastTimerSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // ── 퀘스트 UI ─────────────────────────────

    private void HideQuestUI()
    {
        if (questPanelRoot != null)
            questPanelRoot.SetActive(false);

        if (distancePanelRoot != null)
            distancePanelRoot.SetActive(false);
    }

    /// <summary>
    /// 이벤트 시작 시 외부에서 호출하면 퀘스트 UI를 보여줍니다.
    /// </summary>
    public void ShowQuestUI()
    {
        if (questPanelRoot != null)
            questPanelRoot.SetActive(true);

        if (distancePanelRoot != null)
            distancePanelRoot.SetActive(true);
    }

    // ── 스킬 슬롯 갱신 ────────────────────────

    private void HandleSkillAcquired(CommonSkillKind kind)
    {
        UpdateSkillSlot(kind, 1);
    }

    private void HandleSkillLevelChanged(CommonSkillKind kind, int newLevel)
    {
        UpdateSkillSlot(kind, newLevel);
    }

    private void UpdateSkillSlot(CommonSkillKind kind, int level)
    {
        if (hudUI == null || commonSkillCatalog == null) return;

        if (!commonSkillCatalog.TryGetByKind(kind, out CommonSkillConfigSO config))
            return;

        Sprite icon = config != null ? config.icon : null;

        int slotIndex = FindOrAssignSlotIndex(kind);
        if (slotIndex < 0) return;

        string levelLabel = level > 1 ? $"Lv.{level}" : "";
        hudUI.SetSkillSlot(slotIndex, icon, "", levelLabel);
    }

    // ── 시작 시 이미 획득된 스킬 스캔 ──────────

    private void ScanExistingSkills()
    {
        if (commonSkillManager == null || commonSkillCatalog == null || hudUI == null)
            return;

        if (commonSkillCatalog.skills == null) return;

        for (int i = 0; i < commonSkillCatalog.skills.Count; i++)
        {
            CommonSkillConfigSO config = commonSkillCatalog.skills[i];
            if (config == null) continue;

            int level = commonSkillManager.GetLevel(config.kind);
            if (level <= 0) continue;

            Sprite icon = config.icon;
            int slotIndex = FindOrAssignSlotIndex(config.kind);
            if (slotIndex < 0) continue;

            string levelLabel = level > 1 ? $"Lv.{level}" : "";
            hudUI.SetSkillSlot(slotIndex, icon, "", levelLabel);
        }
    }

    // ── 슬롯 할당 로직 ────────────────────────

    private readonly int[] _kindToSlot = new int[32];
    private int _nextSlotIndex = 0;
    private bool _slotMapInitialized;

    private void EnsureSlotMapInitialized()
    {
        if (_slotMapInitialized) return;
        _slotMapInitialized = true;

        for (int i = 0; i < _kindToSlot.Length; i++)
            _kindToSlot[i] = -1;
    }

    private int FindOrAssignSlotIndex(CommonSkillKind kind)
    {
        EnsureSlotMapInitialized();

        int kindIdx = (int)kind;
        if (kindIdx < 0 || kindIdx >= _kindToSlot.Length)
            return -1;

        // 이미 할당된 슬롯 반환
        if (_kindToSlot[kindIdx] >= 0)
            return _kindToSlot[kindIdx];

        // 새 슬롯 할당 (0~4번, 최대 5개 액티브)
        if (_nextSlotIndex >= 5)
            return -1;

        int slotIdx = _nextSlotIndex;
        _kindToSlot[kindIdx] = slotIdx;
        _nextSlotIndex++;
        return slotIdx;
    }
}