using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 캐릭터 강화 트리 프레젠터입니다.
/// 노드 배치와 구매 판정은 코드가 맡고, 스타일링은 UI 담당자가 맡도록 분리했습니다.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[DisallowMultipleComponent]
public sealed class OutgameUpgradeToolkitPresenter2D : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("캐릭터 카탈로그입니다. 비우면 RootBootstrapper에서 자동으로 찾습니다.")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("루트 요소 이름")]
    [Tooltip("좌측 캐릭터 목록 컨테이너 이름입니다.")]
    [SerializeField] private string characterListName = "character-list";
    [Tooltip("강화 트리 컨테이너 이름입니다.")]
    [SerializeField] private string treeRootName = "upgrade-tree";
    [Tooltip("현재 냥 표시 라벨 이름입니다.")]
    [SerializeField] private string walletLabelName = "wallet-label";
    [Tooltip("상세 제목 라벨 이름입니다.")]
    [SerializeField] private string detailTitleName = "node-title";
    [Tooltip("상세 설명 라벨 이름입니다.")]
    [SerializeField] private string detailDescName = "node-description";
    [Tooltip("상세 상태 라벨 이름입니다.")]
    [SerializeField] private string detailStateName = "node-state";
    [Tooltip("구매 버튼 이름입니다.")]
    [SerializeField] private string purchaseButtonName = "purchase-button";
    [Tooltip("트리 초기화 버튼 이름입니다.")]
    [SerializeField] private string resetButtonName = "reset-button";

    [Header("트리 배치")]
    [Tooltip("UI Toolkit 절대 배치 시 가로 간격입니다.")]
    [Min(80f)] [SerializeField] private float nodeSpacingX = 180f;
    [Tooltip("UI Toolkit 절대 배치 시 세로 간격입니다.")]
    [Min(80f)] [SerializeField] private float nodeSpacingY = 120f;

    private UIDocument _uiDocument;
    private OutgameUpgradeService2D _upgradeService;
    private CharacterProgressionService2D _progressionService;
    private ScrollView _characterList;
    private VisualElement _treeRoot;
    private Label _walletLabel;
    private Label _detailTitle;
    private Label _detailDesc;
    private Label _detailState;
    private Button _purchaseButton;
    private Button _resetButton;

    private string _selectedCharacterId;
    private string _selectedNodeId;

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
        if (_purchaseButton != null)
            _purchaseButton.clicked -= OnClickPurchase;

        if (_resetButton != null)
            _resetButton.clicked -= OnClickReset;
    }

    private void Build()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null || catalog == null)
            return;

        _upgradeService = new OutgameUpgradeService2D(catalog);
        _progressionService = new CharacterProgressionService2D(catalog);

        VisualElement root = _uiDocument.rootVisualElement;
        _characterList = MetaUiToolkitUtil2D.QueryOrCreateScrollView(root, characterListName, "upgrade-character-list");
        _treeRoot = MetaUiToolkitUtil2D.QueryOrCreate(root, treeRootName, "upgrade-tree-root");
        _walletLabel = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, walletLabelName, "냥: 0");
        _detailTitle = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, detailTitleName, "노드 선택");
        _detailDesc = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, detailDescName, "강화 트리 노드를 선택하면 상세 설명이 표시됩니다.");
        _detailState = MetaUiToolkitUtil2D.QueryOrCreateLabel(root, detailStateName, string.Empty);
        _purchaseButton = MetaUiToolkitUtil2D.QueryOrCreateButton(root, purchaseButtonName, "구매");
        _resetButton = MetaUiToolkitUtil2D.QueryOrCreateButton(root, resetButtonName, "트리 초기화");

        _purchaseButton.clicked -= OnClickPurchase;
        _purchaseButton.clicked += OnClickPurchase;
        _resetButton.clicked -= OnClickReset;
        _resetButton.clicked += OnClickReset;

        _treeRoot.style.position = Position.Relative;
        _treeRoot.style.minHeight = 540f;

        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            _selectedCharacterId = FindFirstUnlockedCharacterId();

        BuildCharacterList();
        BuildTree();
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

            bool unlocked = _progressionService == null || _progressionService.IsUnlocked(definition.CharacterId);

            Button button = new Button(() => SelectCharacter(definition.CharacterId)) { text = string.Empty };
            button.AddToClassList("upgrade-character-item");
            if (_selectedCharacterId == definition.CharacterId)
                button.AddToClassList("is-selected");
            if (!unlocked)
                button.AddToClassList("is-locked");
            button.SetEnabled(unlocked);

            VisualElement portrait = new VisualElement();
            portrait.style.width = 52;
            portrait.style.height = 52;
            portrait.AddToClassList("upgrade-character-portrait");
            MetaUiToolkitUtil2D.SetSpriteBackground(portrait, definition.Portrait);
            button.Add(portrait);

            VisualElement textRoot = new VisualElement();
            textRoot.Add(new Label(definition.DisplayName));
            textRoot.Add(new Label(unlocked ? $"구매 노드 {_upgradeService.GetPurchasedNodeCount(definition.CharacterId)}개" : "해금 필요"));
            button.Add(textRoot);

            _characterList.Add(button);
        }
    }

    private void BuildTree()
    {
        if (_treeRoot == null) return;
        _treeRoot.Clear();

        CharacterUpgradeTreeSO tree = _upgradeService.GetTree(_selectedCharacterId);
        if (tree == null)
        {
            _treeRoot.Add(new Label("강화 트리가 준비되지 않았습니다."));
            return;
        }

        IReadOnlyList<CharacterUpgradeNodeData2D> nodes = tree.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            CharacterUpgradeNodeData2D node = nodes[i];
            if (node == null) continue;

            int rank = _upgradeService.GetRank(_selectedCharacterId, node.nodeId);
            bool canBuy = _upgradeService.CanPurchase(_selectedCharacterId, node.nodeId, out _);

            Button button = new Button(() => SelectNode(node.nodeId)) { text = string.Empty };
            button.AddToClassList("upgrade-node");
            button.AddToClassList($"branch-{node.branch.ToString().ToLowerInvariant()}");
            if (_selectedNodeId == node.nodeId) button.AddToClassList("is-selected");
            if (rank > 0) button.AddToClassList("is-owned");
            else if (canBuy) button.AddToClassList("is-unlocked");
            else button.AddToClassList("is-locked");

            button.style.position = Position.Absolute;
            button.style.left = node.gridPosition.x * nodeSpacingX;
            button.style.top = node.gridPosition.y * nodeSpacingY;
            button.style.width = 150f;
            button.style.height = 90f;

            Label title = new Label(node.titleKr);
            title.AddToClassList("upgrade-node-title");
            button.Add(title);

            Label rankLabel = new Label($"{rank}/{node.maxRank}");
            rankLabel.AddToClassList("upgrade-node-rank");
            button.Add(rankLabel);

            Label costLabel = new Label(rank >= node.maxRank
                ? "최대"
                : $"냥 {_upgradeService.GetNextCost(_selectedCharacterId, node.nodeId)}");
            costLabel.AddToClassList("upgrade-node-cost");
            button.Add(costLabel);

            _treeRoot.Add(button);
        }
    }

    private void SelectCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return;

        _selectedCharacterId = characterId;
        _selectedNodeId = null;
        BuildCharacterList();
        BuildTree();
        RefreshDetail();
    }

    private void SelectNode(string nodeId)
    {
        _selectedNodeId = nodeId;
        BuildTree();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_walletLabel != null)
            _walletLabel.text = $"냥: {_upgradeService.Nyang}";

        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
        {
            if (_detailTitle != null) _detailTitle.text = "캐릭터 없음";
            if (_detailDesc != null) _detailDesc.text = "캐릭터를 선택하세요.";
            if (_detailState != null) _detailState.text = string.Empty;
            if (_purchaseButton != null) _purchaseButton.SetEnabled(false);
            return;
        }

        CharacterUpgradeTreeSO tree = _upgradeService.GetTree(_selectedCharacterId);
        if (tree == null || string.IsNullOrWhiteSpace(_selectedNodeId) || !tree.TryFindNode(_selectedNodeId, out CharacterUpgradeNodeData2D node) || node == null)
        {
            if (_detailTitle != null) _detailTitle.text = "노드 선택";
            if (_detailDesc != null) _detailDesc.text = "강화 트리 노드를 선택하면 상세 설명이 표시됩니다.";
            if (_detailState != null) _detailState.text = $"보유 냥: {_upgradeService.Nyang}";
            if (_purchaseButton != null) _purchaseButton.SetEnabled(false);
            return;
        }

        int rank = _upgradeService.GetRank(_selectedCharacterId, node.nodeId);
        bool canBuy = _upgradeService.CanPurchase(_selectedCharacterId, node.nodeId, out string reason);

        if (_detailTitle != null)
            _detailTitle.text = node.titleKr;

        if (_detailDesc != null)
            _detailDesc.text = node.descriptionKr;

        if (_detailState != null)
        {
            string costText = rank >= node.maxRank ? "최대 랭크" : $"다음 비용: 냥 {_upgradeService.GetNextCost(_selectedCharacterId, node.nodeId)}";
            string gateText = canBuy ? "구매 가능" : reason;
            _detailState.text = $"현재 랭크 {rank}/{node.maxRank} | 요구 레벨 {node.requiredCharacterLevel} | {costText}\n{gateText}";
        }

        if (_purchaseButton != null)
            _purchaseButton.SetEnabled(canBuy);
    }

    private void OnClickPurchase()
    {
        if (string.IsNullOrWhiteSpace(_selectedCharacterId) || string.IsNullOrWhiteSpace(_selectedNodeId))
            return;

        if (_upgradeService.TryPurchase(_selectedCharacterId, _selectedNodeId, out string reason))
        {
            BuildCharacterList();
            BuildTree();
            RefreshDetail();
            return;
        }

        if (_detailState != null)
            _detailState.text = reason;
    }

    private void OnClickReset()
    {
        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            return;

        int refund = _upgradeService.ResetCharacterTree(_selectedCharacterId, refund: true, out string reason);
        BuildCharacterList();
        BuildTree();
        RefreshDetail();

        if (!string.IsNullOrWhiteSpace(reason) && _detailState != null)
            _detailState.text = reason;
        else if (_detailState != null)
            _detailState.text = $"강화 트리를 초기화했습니다. 환급 냥: {refund}";
    }

    private string FindFirstUnlockedCharacterId()
    {
        IReadOnlyList<CharacterDefinitionSO> characters = catalog.Characters;
        string firstValid = null;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDefinitionSO definition = characters[i];
            if (definition == null) continue;

            if (string.IsNullOrWhiteSpace(firstValid))
                firstValid = definition.CharacterId;

            if (_progressionService == null || _progressionService.IsUnlocked(definition.CharacterId))
                return definition.CharacterId;
        }

        return firstValid;
    }
}
