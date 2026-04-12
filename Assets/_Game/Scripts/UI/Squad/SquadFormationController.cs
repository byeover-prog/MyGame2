using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SquadFormationController : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("상단 슬롯")]
    [SerializeField] private SquadSlotView slotSupport1;
    [SerializeField] private SquadSlotView slotMain;
    [SerializeField] private SquadSlotView slotSupport2;

    [Header("하단 목록")]
    [SerializeField] private CharacterGridView grid;

    [Header("버튼")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnClear;

    [Header("메시지")]
    [SerializeField] private TMP_Text hintText;

    [Header("다음 씬")]
    [Tooltip("편성 완료 후 이동할 씬 이름")]
    [SerializeField] private string nextSceneName = "Scene_Game";

    [Header("시작 조건")]
    [Tooltip("체크 시: 메인 슬롯이 반드시 있어야 시작 가능")]
    [SerializeField] private bool requireMainToStart = true;

    [Header("디버그")]
    [Tooltip("체크 시: 버튼 연결/클릭 로그를 출력")]
    [SerializeField] private bool debugLog = true;

    private SquadSlotView.SlotKind _armedSlot = SquadSlotView.SlotKind.Main;

    private void Awake()
    {
        // 인스펙터 참조가 빠졌을 때 자동으로 찾아준다.
        AutoBindIfMissing();
    }

    private void OnEnable()
    {
        if (grid != null) grid.OnClickCharacter += OnClickCharacter;

        HookSlot(slotSupport1);
        HookSlot(slotMain);
        HookSlot(slotSupport2);

        if (btnStart != null)
        {
            btnStart.onClick.AddListener(OnClickStart);
            if (debugLog) GameLogger.Log("[SquadFormation] btnStart listener attached.");
        }
        else
        {
            if (debugLog) GameLogger.LogWarning("[SquadFormation] btnStart is NULL. Start won't work.");
        }

        if (btnClear != null)
        {
            btnClear.onClick.AddListener(OnClickClear);
            if (debugLog) GameLogger.Log("[SquadFormation] btnClear listener attached.");
        }

        SquadLoadoutRuntime.OnChanged += OnLoadoutChanged;
    }

    private void OnDisable()
    {
        if (grid != null) grid.OnClickCharacter -= OnClickCharacter;

        UnhookSlot(slotSupport1);
        UnhookSlot(slotMain);
        UnhookSlot(slotSupport2);

        if (btnStart != null) btnStart.onClick.RemoveListener(OnClickStart);
        if (btnClear != null) btnClear.onClick.RemoveListener(OnClickClear);

        SquadLoadoutRuntime.OnChanged -= OnLoadoutChanged;
    }

    private void Start()
    {
        if (grid != null)
        {
            if (catalog != null) grid.SetCatalog(catalog);
            grid.Rebuild();
        }

        ApplyToViews(SquadLoadoutRuntime.Current);
        SetArmedSlot(SquadSlotView.SlotKind.Main);
        RefreshStartButton();
    }
    
    private void AutoBindIfMissing()
    {
        if (btnStart == null)
        {
            // 이름 기반(권장: Btn_Start)
            btnStart = FindButtonByName("Btn_Start") ?? FindButtonByText("시작");
        }

        if (btnClear == null)
        {
            btnClear = FindButtonByName("Btn_Clear") ?? FindButtonByText("초기화");
        }

        if (grid == null) grid = FindFirstObjectByType<CharacterGridView>(FindObjectsInactive.Include);

        if (catalog == null)
        {

            catalog = FindFirstObjectByType<CharacterCatalogSO>(FindObjectsInactive.Include);
        }

        if (debugLog)
        {
            GameLogger.Log($"[SquadFormation] AutoBind => start={(btnStart != null)}, clear={(btnClear != null)}, grid={(grid != null)}, catalog={(catalog != null)}");
        }
    }

    private static Button FindButtonByName(string goName)
    {
        var t = GameObject.Find(goName);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Button FindButtonByText(string label)
    {
        // 씬 전체에서 TMP_Text를 훑어 "시작" 같은 텍스트를 가진 Button을 찾는다.
        // (초보 단계에서만 쓰고, 나중엔 인스펙터로 고정 연결 권장)
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;
            if (texts[i].text != label) continue;

            var b = texts[i].GetComponentInParent<Button>(true);
            if (b != null) return b;
        }
        return null;
    }

    // ------------------------
    // 슬롯/선택
    // ------------------------
    private void HookSlot(SquadSlotView slot)
    {
        if (slot == null || slot.Button == null) return;
        slot.Button.onClick.AddListener(() => SetArmedSlot(slot.Kind));
    }

    private void UnhookSlot(SquadSlotView slot)
    {
        if (slot == null || slot.Button == null) return;
        slot.Button.onClick.RemoveAllListeners();
    }

    private void SetArmedSlot(SquadSlotView.SlotKind kind)
    {
        _armedSlot = kind;
        if (hintText != null) hintText.text = $"배치할 슬롯 선택됨: {ToKorean(kind)} (아래 목록에서 캐릭터 선택)";

        if (grid != null) grid.RefreshInteractable(CanPickCharacter);
    }

    private static string ToKorean(SquadSlotView.SlotKind kind)
    {
        return kind switch
        {
            SquadSlotView.SlotKind.Support1 => "지원1",
            SquadSlotView.SlotKind.Support2 => "지원2",
            _ => "메인",
        };
    }

    private void OnClickCharacter(CharacterDefinitionSO def)
    {
        if (def == null) return;

        if (SquadLoadoutRuntime.Contains(def.CharacterId))
        {
            if (hintText != null) hintText.text = "이미 다른 슬롯에 배치된 캐릭터입니다.";
            return;
        }

        switch (_armedSlot)
        {
            case SquadSlotView.SlotKind.Support1: SquadLoadoutRuntime.SetSupport1(def.CharacterId); break;
            case SquadSlotView.SlotKind.Support2: SquadLoadoutRuntime.SetSupport2(def.CharacterId); break;
            default: SquadLoadoutRuntime.SetMain(def.CharacterId); break;
        }

        if (grid != null) grid.RefreshInteractable(CanPickCharacter);
        RefreshStartButton();
    }

    private bool CanPickCharacter(CharacterDefinitionSO def)
    {
        return def != null && !SquadLoadoutRuntime.Contains(def.CharacterId);
    }

    private void OnLoadoutChanged(SquadLoadoutRuntime.Loadout loadout)
    {
        ApplyToViews(loadout);
        RefreshStartButton();

        if (grid != null) grid.RefreshInteractable(CanPickCharacter);
    }

    private void ApplyToViews(SquadLoadoutRuntime.Loadout loadout)
    {
        ApplySlot(slotSupport1, loadout.support1Id);
        ApplySlot(slotMain, loadout.mainId);
        ApplySlot(slotSupport2, loadout.support2Id);
    }

    private void ApplySlot(SquadSlotView slot, string id)
    {
        if (slot == null) return;

        if (catalog != null && catalog.TryFindById(id, out var def) && def != null)
            slot.SetCharacter(def);
        else
            slot.SetEmpty();
    }

    private void RefreshStartButton()
    {
        if (btnStart == null) return;

        bool ok = !requireMainToStart || SquadLoadoutRuntime.Current.HasMain;
        btnStart.interactable = ok;

        if (hintText != null)
        {
            hintText.text = ok
                ? "준비 완료. 시작을 누르세요."
                : "메인 슬롯은 반드시 선택해야 합니다.";
        }
    }

    private void OnClickStart()
    {
        if (debugLog) GameLogger.Log("[SquadFormation] OnClickStart fired.");

        if (requireMainToStart && !SquadLoadoutRuntime.Current.HasMain)
        {
            if (hintText != null) hintText.text = "메인 캐릭터를 먼저 선택하세요.";
            if (debugLog) GameLogger.LogWarning("[SquadFormation] Blocked: HasMain=false");
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (hintText != null) hintText.text = "nextSceneName이 비어있습니다.";
            if (debugLog) Debug.LogError("[SquadFormation] nextSceneName is empty.");
            return;
        }

        if (debugLog) GameLogger.Log($"[SquadFormation] LoadScene => {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnClickClear()
    {
        if (debugLog) GameLogger.Log("[SquadFormation] OnClickClear fired.");
        SquadLoadoutRuntime.ClearAll();
        SetArmedSlot(SquadSlotView.SlotKind.Main);
        RefreshStartButton();
    }
}