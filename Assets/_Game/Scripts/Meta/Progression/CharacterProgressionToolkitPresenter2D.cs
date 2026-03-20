using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Game.Player;

/// <summary>
/// UI Toolkit 기반 캐릭터 영구 레벨 화면 프레젠터입니다.
/// 레벨 상승 스탯은 공격/방어/최대체력만 표시합니다.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DisallowMultipleComponent]
public sealed class CharacterProgressionToolkitPresenter2D : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("캐릭터 카탈로그입니다. 비우면 RootBootstrapper에서 자동으로 찾습니다.")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("루트 요소 이름")]
    [Tooltip("좌측 캐릭터 목록 스크롤뷰 이름입니다.")]
    [SerializeField] private string characterListName = "character-list";
    [Tooltip("레벨 라벨 이름입니다.")]
    [SerializeField] private string levelLabelName = "level-label";
    [Tooltip("경험치 라벨 이름입니다.")]
    [SerializeField] private string xpLabelName = "xp-label";
    [Tooltip("공격력 성장 라벨 이름입니다.")]
    [SerializeField] private string attackLabelName = "attack-label";
    [Tooltip("방어력 성장 라벨 이름입니다.")]
    [SerializeField] private string defenseLabelName = "defense-label";
    [Tooltip("최대체력 성장 라벨 이름입니다.")]
    [SerializeField] private string hpLabelName = "hp-label";
    [Tooltip("해금 목록 스크롤뷰 이름입니다.")]
    [SerializeField] private string unlockListName = "unlock-list";
    [Tooltip("디버그 XP 추가 버튼 이름입니다. 없으면 버튼을 만들지 않습니다.")]
    [SerializeField] private string debugAddXpButtonName = "debug-add-xp-button";

    [Header("디버그")]
    [Tooltip("디버그 버튼이 있을 때 한 번에 추가할 XP입니다.")]
    [Min(1)] [SerializeField] private int debugXpAmount = 120;

    private UIDocument _uiDocument;
    private CharacterProgressionService2D _progressionService;
    private ScrollView _characterList;
    private Label _levelLabel;
    private Label _xpLabel;
    private Label _attackLabel;
    private Label _defenseLabel;
    private Label _hpLabel;
    private ScrollView _unlockList;
    private Button _debugAddXpButton;
    private string _selectedCharacterId;

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
        if (_debugAddXpButton != null)
            _debugAddXpButton.clicked -= OnClickDebugAddXp;
    }

    private void Build()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null || catalog == null)
            return;

        _progressionService = new CharacterProgressionService2D(catalog);

        VisualElement root = _uiDocument.rootVisualElement;
        _characterList = MetaUiToolkitUtil2D.QueryOrCreateScrollView(root, characterListName, "progression-character-list");
        _levelLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, levelLabelName, "Lv 1");
        _xpLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, xpLabelName, "XP 0/0");
        _attackLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, attackLabelName, "공격력 +0%");
        _defenseLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, defenseLabelName, "방어력 +0%");
        _hpLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, hpLabelName, "최대체력 +0");
        _unlockList = MetaUiToolkitUtil2D.QueryOrCreateScrollView(root, unlockListName, "progression-unlock-list");

        if (!string.IsNullOrWhiteSpace(debugAddXpButtonName))
        {
            _debugAddXpButton = MetaUiToolkitUtil2D.QueryOrCreateButton(root, debugAddXpButtonName, $"XP +{debugXpAmount}");
            _debugAddXpButton.clicked -= OnClickDebugAddXp;
            _debugAddXpButton.clicked += OnClickDebugAddXp;
        }

        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            _selectedCharacterId = FindFirstCharacterId();

        BuildCharacterList();
        RefreshDetail();
    }

    private void BuildCharacterList()
    {
        if (_characterList == null) return;
        _characterList.Clear();

        IReadOnlyList<CharacterDefinitionSO> characters = catalog.Characters;
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDefinitionSO definition = characters[i];
            if (definition == null) continue;

            int level = _progressionService.GetLevel(definition.CharacterId);
            Button button = new Button(() => SelectCharacter(definition.CharacterId)) { text = string.Empty };
            button.AddToClassList("progression-character-item");
            if (_selectedCharacterId == definition.CharacterId)
                button.AddToClassList("is-selected");

            VisualElement portrait = new VisualElement();
            portrait.style.width = 52;
            portrait.style.height = 52;
            portrait.AddToClassList("progression-character-portrait");
            MetaUiToolkitUtil2D.SetSpriteBackground(portrait, definition.Portrait);
            button.Add(portrait);

            VisualElement textRoot = new VisualElement();
            textRoot.Add(new Label(definition.DisplayName));
            textRoot.Add(new Label($"Lv {level}"));
            button.Add(textRoot);

            _characterList.Add(button);
        }
    }

    private void SelectCharacter(string characterId)
    {
        _selectedCharacterId = characterId;
        BuildCharacterList();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            return;

        int level = _progressionService.GetLevel(_selectedCharacterId);
        int currentXp = _progressionService.GetCurrentXp(_selectedCharacterId);
        int requiredXp = _progressionService.GetRequiredXpToNextLevel(_selectedCharacterId);
        PlayerStatSnapshot bonus = _progressionService.BuildLevelBonusSnapshot(_selectedCharacterId);

        if (_levelLabel != null)
            _levelLabel.text = $"Lv {level}";

        if (_xpLabel != null)
            _xpLabel.text = requiredXp > 0 ? $"XP {currentXp}/{requiredXp}" : "최대 레벨";

        if (_attackLabel != null)
            _attackLabel.text = $"공격력 +{bonus.AttackPowerPercent:0.#}%";

        if (_defenseLabel != null)
            _defenseLabel.text = $"방어력 +{bonus.DefensePercent:0.#}%";

        if (_hpLabel != null)
            _hpLabel.text = $"최대체력 +{bonus.MaxHpFlat}";

        BuildUnlockList(_progressionService.GetUnlockedEntries(_selectedCharacterId));
    }

    private void BuildUnlockList(IReadOnlyList<CharacterLevelUnlockEntry2D> unlocks)
    {
        if (_unlockList == null) return;
        _unlockList.Clear();

        if (unlocks == null || unlocks.Count == 0)
        {
            _unlockList.Add(new Label("해금된 레벨 보상이 없습니다."));
            return;
        }

        for (int i = 0; i < unlocks.Count; i++)
        {
            CharacterLevelUnlockEntry2D entry = unlocks[i];
            if (entry == null) continue;

            VisualElement row = new VisualElement();
            row.AddToClassList("progression-unlock-item");
            row.Add(new Label($"Lv {entry.level}"));
            row.Add(new Label(entry.titleKr));
            row.Add(new Label(entry.descriptionKr));
            _unlockList.Add(row);
        }
    }

    private void OnClickDebugAddXp()
    {
        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            return;

        CharacterProgressionResult2D result = _progressionService.AddXp(_selectedCharacterId, debugXpAmount);
        BuildCharacterList();
        RefreshDetail();

        if (result.newLevel > result.previousLevel && _xpLabel != null)
            _xpLabel.text += $" | 레벨업 {result.previousLevel}→{result.newLevel}";
    }

    private string FindFirstCharacterId()
    {
        IReadOnlyList<CharacterDefinitionSO> characters = catalog.Characters;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == null) continue;
            return characters[i].CharacterId;
        }

        return null;
    }
}
