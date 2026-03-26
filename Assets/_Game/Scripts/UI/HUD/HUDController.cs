using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// InGameHUD.uxml ↔ 팀원 스크립트 연결
///
/// [Inspector에서 연결할 것]
///   playerHealth    → PlayerHealth
///   playerExp       → PlayerExp
///   playerCurrency  → PlayerCurrency2D
///   playerDash      → PlayerDashController
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class HUDController : MonoBehaviour
{
    // ── Inspector 연결 ────────────────────────────
    [Header("플레이어 컴포넌트")]
    [SerializeField] private PlayerHealth         playerHealth;
    [SerializeField] private PlayerExp            playerExp;
    [SerializeField] private PlayerCurrency2D     playerCurrency;
    [SerializeField] private PlayerSpirit2D       playerSpirit;
    [SerializeField] private PlayerDashController playerDash;

    // ── UI 요소 캐시 ──────────────────────────────
    private VisualElement   xpFill;
    private Label           levelLabel;
    private Label           timerLabel;
    private Label           nyangValue;
    private Label           spiritValue;
    private VisualElement   hpFill;
    private Label           hpText;
    private VisualElement[] dashPips = new VisualElement[3];
    private Button btnLevelUpPending;

    // ── 내부 상태 ─────────────────────────────────
    private float elapsedTime;
    private int   _prevDashCur = -1;
    private int   _prevDashMax = -1;
    private int _prevHp  = -1;
    private int _prevMaxHp = -1;
    private int _prevXP = -1;

    // ─────────────────────────────────────────────
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        CacheElements(root);
        CenterHotbar(root);
        SubscribeEvents();
        RefreshAll();
        
        var qusetPanel = root.Q<VisualElement>("QusetPanel");
        if(qusetPanel != null)
            qusetPanel.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void Update()
    {
        // 타이머
        elapsedTime += Time.deltaTime;
        if (timerLabel != null)
        {
            int m = (int)(elapsedTime / 60);
            int s = (int)(elapsedTime % 60);
            timerLabel.text = $"{m:00} : {s:00}";
        }

        PollHP();

        // 대쉬는 이벤트 없으므로 폴링 (변화 없으면 DOM 갱신 스킵)
        PollDash();
        PollXP();
    }

    // ── 캐시 ─────────────────────────────────────
    private void CacheElements(VisualElement root)
    {
        xpFill     = root.Q<VisualElement>("XPBarFill");
        levelLabel = root.Q<Label>("LevelLabel");
        timerLabel = root.Q<Label>("TimerLabel");
        nyangValue = root.Q<Label>("NyangValue");
        spiritValue = root.Q<Label>("SpiritValue");
        hpFill     = root.Q<VisualElement>("HPBarFill");
        hpText     = root.Q<Label>("HPText");
        btnLevelUpPending = root.Q<Button>("BtnLevelUpPending");

        for (int i = 0; i < dashPips.Length; i++)
            dashPips[i] = root.Q<VisualElement>($"DashPip{i}");

        root.Q<VisualElement>("QusetPanel")?.SetDisplay(false);
    }
    // 클릭 콜백 참조 보관 (UnregisterCallback용)
    private EventCallback<ClickEvent> _pendingClickCallback;

    public void ShowPendingButton(bool show, System.Action onClicked)
    {
        GameLogger.Log($"[HUD] ShowPendingButton | btnLevelUpPending={btnLevelUpPending}");
        if (btnLevelUpPending == null) return;

        btnLevelUpPending.style.display = show
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        // 기존 콜백 제거
        if (_pendingClickCallback != null)
        {
            btnLevelUpPending.UnregisterCallback<ClickEvent>(_pendingClickCallback);
            _pendingClickCallback = null;
        }

        // 새 콜백 등록
        if (show && onClicked != null)
        {
            _pendingClickCallback = _ => onClicked();
            btnLevelUpPending.RegisterCallback<ClickEvent>(_pendingClickCallback);
        }
    }

    // ── 핫바 중앙 정렬 ────────────────────────────
    private void CenterHotbar(VisualElement root)
    {
        var hotbar = root.Q<VisualElement>("Hotbar");

        root.schedule.Execute(() =>
        {
            float rootW = root.resolvedStyle.width;

            if (hotbar != null)
            {
                float hw = hotbar.resolvedStyle.width;
                if (rootW > 0f && hw > 0f)
                    hotbar.style.left = (rootW - hw) * 0.5f;
            }
        }).ExecuteLater(100);
    }

    // ── 이벤트 구독 ──────────────────────────────
    private void SubscribeEvents()
    {
        if (playerExp      != null) playerExp.OnLevelUp          += OnLevelUp;
        if (playerCurrency != null) playerCurrency.OnGoldChanged += OnGoldChanged;
        if (playerSpirit   != null) playerSpirit.OnSpiritChanged += OnSpiritChanged;
    }

    private void UnsubscribeEvents()
    {
        if (playerExp      != null) playerExp.OnLevelUp          -= OnLevelUp;
        if (playerCurrency != null) playerCurrency.OnGoldChanged -= OnGoldChanged;
        if (playerSpirit   != null) playerSpirit.OnSpiritChanged -= OnSpiritChanged;
    }

    // ── 전체 초기화 ──────────────────────────────
    private void RefreshAll()
    {
        RefreshHP();
        RefreshXP();
        RefreshLevel();
        if (playerCurrency != null) SetNyang(playerCurrency.CurrentGold);
        if (playerSpirit   != null) SetSpirit(playerSpirit.CurrentSpirit);
    }

    // ── HP ───────────────────────────────────────
    /// PlayerHealth에 이벤트가 없으므로
    /// 피격/회복이 일어나는 시점에 외부(적 스크립트 등)에서 호출하거나,
    /// 필요하면 Update()에서 매 프레임 호출해도 무방합니다.
    public void RefreshHP()
    {
        if (playerHealth == null) return;

        float cur = playerHealth.CurrentHp;
        float max = playerHealth.MaxHp;
        float pct = max > 0f ? cur / max : 0f;

        if (hpFill != null)
            hpFill.style.width = Length.Percent(pct * 100f);

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(max)}";
    }
    
    private void PollHP()
    {
        if (playerHealth == null) return;

        int cur = playerHealth.CurrentHp;
        int max = playerHealth.MaxHp;

        // 변화 없으면 스킵
        if (cur == _prevHp && max == _prevMaxHp) return;
        _prevHp    = cur;
        _prevMaxHp = max;

        float pct = max > 0f ? (float)cur / max : 0f;

        if (hpFill != null)
            hpFill.style.width = Length.Percent(pct * 100f);

        if (hpText != null)
            hpText.text = $"{cur}/{max}";
    }

    // ── XP 바 ────────────────────────────────────
    private void RefreshXP()
    {
        if (playerExp == null || xpFill == null) return;

        float pct = playerExp.RequiredExp > 0
            ? (float)playerExp.CurrentExp / playerExp.RequiredExp
            : 0f;

        xpFill.style.width = Length.Percent(pct * 100f);
    }
    private void PollXP()
    {
        if (playerExp == null || xpFill == null) return;

        int cur = playerExp.CurrentExp;
        if (cur == _prevXP) return;
        _prevXP = cur;

        float pct = playerExp.RequiredExp > 0
            ? (float)cur / playerExp.RequiredExp
            : 0f;

        xpFill.style.width = Length.Percent(pct * 100f);
    }

    // ── 레벨 ─────────────────────────────────────
    private void RefreshLevel()
    {
        if (playerExp == null || levelLabel == null) return;
        levelLabel.text = $"Lv.{playerExp.Level}";
    }

    // ── 재화 ─────────────────────────────────────
    private void SetNyang(int value)
    {
        if (nyangValue == null) return;
        nyangValue.text = value >= 1000 ? $"{value / 1000f:F1}k" : value.ToString();
    }

    private void SetSpirit(int value)
    {
        if (spiritValue == null) return;
        spiritValue.text = value.ToString("N0");
    }
    

    // ── 대쉬 pip 폴링 ────────────────────────────
    private void PollDash()
    {
        if (playerDash == null) return;

        int cur = playerDash.Current_Dash_Count;
        int max = playerDash.Max_Dash_Count;

        // max가 0이면 아직 Initialize 안 된 것 → 스킵
        if (max == 0) return;

        if (cur == _prevDashCur && max == _prevDashMax) return;
        _prevDashCur = cur;
        _prevDashMax = max;

        for (int i = 0; i < dashPips.Length; i++)
        {
            if (dashPips[i] == null) continue;

            bool exists  = i < max;
            bool charged = i < cur;

            dashPips[i].style.display = exists
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!exists) continue;

            bool isWind = (i == 2);
            dashPips[i].EnableInClassList("dash-pip--charged",  !isWind && charged);
            dashPips[i].EnableInClassList("dash-pip--wind",      isWind && charged);
            dashPips[i].EnableInClassList("dash-pip--cooldown", !charged);
        }
    }

    // ── 이벤트 콜백 ──────────────────────────────
    private void OnLevelUp(int newLevel)
    {
        if (levelLabel != null) levelLabel.text = $"Lv.{newLevel}";
        RefreshXP(); // 레벨업 시 XP 바 0으로 리셋
    }

    private void OnGoldChanged(int total, int delta)
    {
        SetNyang(total);
    }

    private void OnSpiritChanged(int total, int delta)
    {
        SetSpirit(total);
    }

    // ════════════════════════════════════════════
    //  외부 호출 API
    // ════════════════════════════════════════════

    /// <summary>
    /// 퀘스트 진행 갱신.
    /// KillCountManager 등에서 호출하세요.
    /// </summary>
    public void UpdateQuest(string mobName, int current, int target)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var questPanel = root.Q<VisualElement>("QuestPanel");
        if (questPanel != null)
            questPanel.style.display = DisplayStyle.Flex; // 퀘스트 생기면 표시

        root.Q<Label>("QuestMob")?.SetText(mobName);
        root.Q<Label>("QuestCount")?.SetText($"처치 {current} / {target}");

        var bar = root.Q<VisualElement>("QuestBarFill");
        if (bar != null)
        {
            float pct = target > 0 ? (float)current / target : 0f;
            bar.style.width = Length.Percent(pct * 100f);
        }
    }

    /// <summary>
    /// 스킬 슬롯 상태 갱신.
    /// SkillManager/PlayerSkillLoadout 쪽에서 스킬 획득 시 호출하세요.
    /// </summary>
    public void SetSkillSlot(int index, bool isPassive, string skillName, bool hasSkill)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        string id = isPassive ? $"Passive{index}" : $"Skill{index}";
        var slot = root.Q<VisualElement>(id);
        if (slot == null) return;

        slot.EnableInClassList("hslot--has-skill", hasSkill);
        slot.EnableInClassList("hslot--empty",    !hasSkill);

        var label = slot.Q<Label>(className: "slot-label");
        if (label != null) label.text = hasSkill ? skillName : "";
    }

    /// <summary>
    /// 스킬 쿨다운 표시.
    /// SkillSystem 쪽에서 매 프레임 또는 쿨다운 변화 시 호출하세요.
    /// remaining이 0 이하면 오버레이 숨김.
    /// </summary>
    public void SetSkillCooldown(int index, float remaining)
    {
        var root  = GetComponent<UIDocument>().rootVisualElement;
        var slot  = root.Q<VisualElement>($"Skill{index}");
        var cdLbl = slot?.Q<Label>(className: "slot-cd");
        if (cdLbl == null) return;

        bool show = remaining > 0f;
        cdLbl.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show) cdLbl.text = $"{remaining:F1}s";
    }
}

internal static class VEExt
{
    public static void SetDisplay(this VisualElement ve, bool visible)
    {
        ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    public static void SetText(this Label label, string text)
    {
        if (label != null) label.text = text;
    }
}