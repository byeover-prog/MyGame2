using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 전용 편성 화면 프레젠터입니다.
/// UI 작업자는 컨테이너 이름만 맞추고 스타일링에만 집중하면 됩니다.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DisallowMultipleComponent]
public sealed class FormationToolkitPresenter2D : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("캐릭터 카탈로그입니다. 비우면 RootBootstrapper에서 자동으로 찾습니다.")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("시작 옵션")]
    [Tooltip("메인 슬롯이 반드시 있어야 시작할지 여부입니다.")]
    [SerializeField] private bool requireMainToStart = true;

    [Tooltip("시작 버튼을 누르면 이동할 씬 이름입니다.")]
    [SerializeField] private string nextSceneName = "Scene_Game";

    [Header("루트 요소 이름")]
    [Tooltip("지원1 슬롯 컨테이너 이름입니다.")]
    [SerializeField] private string support1SlotName = "slot-support-1";
    [Tooltip("메인 슬롯 컨테이너 이름입니다.")]
    [SerializeField] private string mainSlotName = "slot-main";
    [Tooltip("지원2 슬롯 컨테이너 이름입니다.")]
    [SerializeField] private string support2SlotName = "slot-support-2";
    [Tooltip("캐릭터 목록 스크롤뷰 이름입니다.")]
    [SerializeField] private string rosterListName = "character-list";
    [Tooltip("힌트 라벨 이름입니다.")]
    [SerializeField] private string hintLabelName = "hint-label";
    [Tooltip("시작 버튼 이름입니다.")]
    [SerializeField] private string startButtonName = "start-button";
    [Tooltip("초기화 버튼 이름입니다.")]
    [SerializeField] private string clearButtonName = "clear-button";

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private UIDocument _uiDocument;
    private FormationService2D _service;
    private ScrollView _rosterList;
    private Label _hintLabel;
    private Button _startButton;
    private Button _clearButton;
    private VisualElement _support1Slot;
    private VisualElement _mainSlot;
    private VisualElement _support2Slot;
    private FormationSlotType2D _armedSlot = FormationSlotType2D.Main;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();

        if (catalog == null && RootBootstrapper.Instance != null && RootBootstrapper.Instance.CharacterRoot != null)
            catalog = RootBootstrapper.Instance.CharacterRoot.catalog;
    }

    private void OnEnable()
    {
        Build();
    }

    private void OnDisable()
    {
        if (_service != null)
            _service.OnChanged -= OnFormationChanged;

        if (_startButton != null)
            _startButton.clicked -= OnClickStart;

        if (_clearButton != null)
            _clearButton.clicked -= OnClickClear;
    }

    private void Build()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            return;

        if (catalog == null)
        {
            Debug.LogWarning("[FormationToolkitPresenter2D] CharacterCatalogSO가 없습니다.", this);
            return;
        }

        _service = new FormationService2D(catalog);
        _service.OnChanged -= OnFormationChanged;
        _service.OnChanged += OnFormationChanged;

        VisualElement root = _uiDocument.rootVisualElement;
        _support1Slot = MetaUiToolkitUtil2D.QueryOrCreate(root, support1SlotName, "formation-slot");
        _mainSlot = MetaUiToolkitUtil2D.QueryOrCreate(root, mainSlotName, "formation-slot");
        _support2Slot = MetaUiToolkitUtil2D.QueryOrCreate(root, support2SlotName, "formation-slot");
        _rosterList = MetaUiToolkitUtil2D.QueryOrCreateScrollView(root, rosterListName, "formation-roster-list");
        _hintLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, hintLabelName, "슬롯을 선택한 뒤 캐릭터를 배치하세요.");
        _startButton = MetaUiToolkitUtil2D.QueryOrCreateButton(root, startButtonName, "시작");
        _clearButton = MetaUiToolkitUtil2D.QueryOrCreateButton(root, clearButtonName, "초기화");

        _startButton.clicked -= OnClickStart;
        _startButton.clicked += OnClickStart;
        _clearButton.clicked -= OnClickClear;
        _clearButton.clicked += OnClickClear;

        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_service == null) return;

        BuildSlot(_support1Slot, FormationSlotType2D.Support1, "지원1", _service.GetCharacter(FormationSlotType2D.Support1));
        BuildSlot(_mainSlot, FormationSlotType2D.Main, "메인", _service.GetCharacter(FormationSlotType2D.Main));
        BuildSlot(_support2Slot, FormationSlotType2D.Support2, "지원2", _service.GetCharacter(FormationSlotType2D.Support2));
        BuildRosterList();
        RefreshStartState();
    }

    private void BuildSlot(VisualElement container, FormationSlotType2D slot, string title, CharacterDefinitionSO definition)
    {
        if (container == null) return;
        container.Clear();
        container.AddToClassList("formation-slot-root");
        if (_armedSlot == slot) container.AddToClassList("is-armed");
        else container.RemoveFromClassList("is-armed");

        Button button = new Button(() => ArmSlot(slot)) { text = string.Empty };
        button.AddToClassList("formation-slot-button");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("formation-slot-title");
        button.Add(titleLabel);

        VisualElement portrait = new VisualElement();
        portrait.AddToClassList("formation-slot-portrait");
        portrait.style.width = 92;
        portrait.style.height = 92;
        MetaUiToolkitUtil2D.SetSpriteBackground(portrait, definition != null ? definition.Portrait : null);
        button.Add(portrait);

        Label nameLabel = new Label(definition != null ? definition.DisplayName : "비어 있음");
        nameLabel.AddToClassList("formation-slot-name");
        button.Add(nameLabel);

        Label attrLabel = new Label(definition != null ? definition.Attribute.ToKorean() : "선택 대기");
        attrLabel.AddToClassList("formation-slot-attribute");
        button.Add(attrLabel);

        VisualElement skillRow = new VisualElement();
        skillRow.AddToClassList("formation-slot-skill-row");

        VisualElement basicIcon = new VisualElement();
        basicIcon.AddToClassList("formation-slot-skill-icon");
        basicIcon.style.width = 40;
        basicIcon.style.height = 40;
        MetaUiToolkitUtil2D.SetSpriteBackground(basicIcon, definition != null ? definition.BasicSkillIcon : null);
        skillRow.Add(basicIcon);

        VisualElement ultimateIcon = new VisualElement();
        ultimateIcon.AddToClassList("formation-slot-skill-icon");
        ultimateIcon.style.width = 40;
        ultimateIcon.style.height = 40;
        MetaUiToolkitUtil2D.SetSpriteBackground(ultimateIcon, definition != null ? definition.UltimateSkillIcon : null);
        skillRow.Add(ultimateIcon);

        button.Add(skillRow);
        container.Add(button);

        if (definition != null)
        {
            Button clearButton = new Button(() =>
            {
                _service.ClearSlot(slot);
                SetHint($"{title} 슬롯을 비웠습니다.");
            })
            {
                text = "해제"
            };
            clearButton.AddToClassList("formation-slot-clear-button");
            container.Add(clearButton);
        }
    }

    private void BuildRosterList()
    {
        if (_rosterList == null || _service == null) return;
        _rosterList.Clear();

        IReadOnlyList<CharacterDefinitionSO> characters = _service.Characters;
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDefinitionSO definition = characters[i];
            if (definition == null) continue;

            bool unlocked = _service.IsUnlocked(definition.CharacterId);
            bool selected = _service.IsSelected(definition.CharacterId);

            Button item = new Button(() => OnClickCharacter(definition)) { text = string.Empty };
            item.AddToClassList("formation-roster-item");
            if (selected) item.AddToClassList("is-selected");
            if (!unlocked) item.AddToClassList("is-locked");
            item.SetEnabled(unlocked && !selected);

            VisualElement portrait = new VisualElement();
            portrait.AddToClassList("formation-roster-portrait");
            portrait.style.width = 64;
            portrait.style.height = 64;
            MetaUiToolkitUtil2D.SetSpriteBackground(portrait, definition.Portrait);
            item.Add(portrait);

            VisualElement textRoot = new VisualElement();
            textRoot.AddToClassList("formation-roster-text-root");
            textRoot.Add(new Label(definition.DisplayName) { name = $"{definition.CharacterId}-name" });
            textRoot.Add(new Label(unlocked ? definition.Attribute.ToKorean() : "해금 필요") { name = $"{definition.CharacterId}-state" });
            item.Add(textRoot);

            _rosterList.Add(item);
        }
    }

    private void ArmSlot(FormationSlotType2D slot)
    {
        _armedSlot = slot;
        RefreshAll();
        SetHint(slot switch
        {
            FormationSlotType2D.Support1 => "지원1 슬롯을 선택했습니다.",
            FormationSlotType2D.Support2 => "지원2 슬롯을 선택했습니다.",
            _ => "메인 슬롯을 선택했습니다."
        });
    }

    private void OnClickCharacter(CharacterDefinitionSO definition)
    {
        if (_service == null || definition == null) return;

        if (_service.TryAssign(_armedSlot, definition.CharacterId, out string reason))
        {
            SetHint($"{definition.DisplayName}을(를) {ToSlotKorean(_armedSlot)} 슬롯에 배치했습니다.");
            RefreshAll();
            return;
        }

        SetHint(reason);
    }

    private void RefreshStartState()
    {
        if (_startButton == null || _service == null) return;
        _startButton.SetEnabled(_service.CanStart(requireMainToStart));
    }

    private void OnClickStart()
    {
        if (_service == null) return;

        if (!_service.CanStart(requireMainToStart))
        {
            SetHint("메인 캐릭터를 먼저 편성하세요.");
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            SetHint("다음 씬 이름이 비어 있습니다.");
            return;
        }

        if (debugLog) Debug.Log($"[FormationToolkitPresenter2D] LoadScene => {nextSceneName}", this);
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnClickClear()
    {
        if (_service == null) return;

        _service.ClearAll();
        ArmSlot(FormationSlotType2D.Main);
        SetHint("편성을 초기화했습니다.");
    }

    private void OnFormationChanged(FormationSaveData2D data)
    {
        RefreshAll();
    }


    private static string ToSlotKorean(FormationSlotType2D slot)
    {
        return slot switch
        {
            FormationSlotType2D.Support1 => "지원1",
            FormationSlotType2D.Support2 => "지원2",
            _ => "메인",
        };
    }

    private void SetHint(string message)
    {
        if (_hintLabel != null)
            _hintLabel.text = message;
    }
}
