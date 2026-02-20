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
    [SerializeField] private string nextSceneName = "Scene_Lobby";

    private SquadSlotView.SlotKind _armedSlot = SquadSlotView.SlotKind.Main;

    private void OnEnable()
    {
        if (grid != null) grid.OnClickCharacter += OnClickCharacter;

        HookSlot(slotSupport1);
        HookSlot(slotMain);
        HookSlot(slotSupport2);

        if (btnStart != null) btnStart.onClick.AddListener(OnClickStart);
        if (btnClear != null) btnClear.onClick.AddListener(OnClickClear);

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

        // 초기 표시
        ApplyToViews(SquadLoadoutRuntime.Current);
        SetArmedSlot(SquadSlotView.SlotKind.Main);
        RefreshStartButton();
    }

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

        // 선택 슬롯을 바꿔도 중복 배치 규칙은 유지
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

        // 같은 캐릭터를 다른 슬롯에 중복 배치 금지
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
        // 중복 배치만 막는다. (필요하면 여기서 모드 제한/해금 조건 추가)
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

        var ok = SquadLoadoutRuntime.Current.HasMain;
        btnStart.interactable = ok;

        if (hintText != null)
        {
            hintText.text = ok
                ? $"준비 완료. 시작을 누르세요."
                : "메인 슬롯은 반드시 선택해야 합니다.";
        }
    }

    private void OnClickStart()
    {
        if (!SquadLoadoutRuntime.Current.HasMain)
        {
            if (hintText != null) hintText.text = "메인 캐릭터를 먼저 선택하세요.";
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (hintText != null) hintText.text = "nextSceneName이 비어있습니다.";
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnClickClear()
    {
        SquadLoadoutRuntime.ClearAll();
        SetArmedSlot(SquadSlotView.SlotKind.Main);
        RefreshStartButton();
    }
}
