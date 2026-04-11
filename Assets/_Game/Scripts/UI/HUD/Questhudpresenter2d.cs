using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class QuestHUDPresenter : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("퀘스트 패널의 UXML 이름입니다.")]
    [SerializeField] private string questPanelName = "QusetPanel";

    [Tooltip("동시 표시 가능한 최대 퀘스트 수입니다.")]
    [SerializeField] private int maxDisplayCount = 3;

    // ─── 캐시 ───
    private UIDocument _uiDoc;
    private VisualElement _questPanel;
    private readonly List<QuestEntryUI> _entryUIs = new List<QuestEntryUI>(3);

    // ─── 내부 상태 ───
    private bool _initialized;

    void OnEnable()
    {
        _uiDoc = GetComponent<UIDocument>();
        if (_uiDoc == null || _uiDoc.rootVisualElement == null) return;

        _questPanel = _uiDoc.rootVisualElement.Q<VisualElement>(questPanelName);
        if (_questPanel == null)
        {
            GameLogger.Log($"[QuestHUD] '{questPanelName}' 패널을 찾을 수 없습니다.");
            return;
        }

        CacheEntryElements();
        _questPanel.style.display = DisplayStyle.None;
        _initialized = true;

        // 이벤트 구독
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestChanged;
            QuestManager.Instance.OnQuestProgressUpdated += OnProgressUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
        }
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestChanged;
            QuestManager.Instance.OnQuestProgressUpdated -= OnProgressUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
        }
    }

    void LateUpdate()
    {
        if (!_initialized) return;

        // QuestManager가 늦게 초기화될 수 있으므로 재구독 시도
        if (QuestManager.Instance != null && _questPanel != null)
        {
            RefreshAll();
        }
    }

    // ─── 이벤트 핸들러 ───

    private void OnQuestChanged(string questId)
    {
        RefreshAll();
    }

    private void OnProgressUpdated(string questId, int current, int target)
    {
        RefreshAll();
    }

    // ─── UI 갱신 ───

    private void RefreshAll()
    {
        if (QuestManager.Instance == null || _questPanel == null) return;

        var activeQuests = QuestManager.Instance.ActiveQuests;
        bool hasQuests = activeQuests.Count > 0;

        _questPanel.style.display = hasQuests ? DisplayStyle.Flex : DisplayStyle.None;

        for (int i = 0; i < _entryUIs.Count; i++)
        {
            QuestEntryUI entry = _entryUIs[i];
            if (i < activeQuests.Count)
            {
                QuestManager.ActiveQuestState aq = activeQuests[i];
                entry.container.style.display = DisplayStyle.Flex;

                if (entry.nameLabel != null)
                    entry.nameLabel.text = aq.definition.DisplayName;

                if (entry.progressLabel != null)
                    entry.progressLabel.text = $"{aq.currentProgress} / {aq.definition.TargetCount}";

                if (entry.barFill != null)
                    entry.barFill.style.width = Length.Percent(aq.Progress01 * 100f);
            }
            else
            {
                entry.container.style.display = DisplayStyle.None;
            }
        }
    }

    private void CacheEntryElements()
    {
        _entryUIs.Clear();
        if (_questPanel == null) return;

        for (int i = 0; i < maxDisplayCount; i++)
        {
            VisualElement container = _questPanel.Q<VisualElement>($"quest-entry-{i}");
            if (container == null)
            {
                // 자동 생성
                container = CreateQuestEntryElement(i);
                _questPanel.Add(container);
            }

            _entryUIs.Add(new QuestEntryUI
            {
                container = container,
                nameLabel = container.Q<Label>($"quest-name-{i}"),
                progressLabel = container.Q<Label>($"quest-progress-{i}"),
                barFill = container.Q<VisualElement>($"quest-bar-fill-{i}")
            });
        }
    }

    private VisualElement CreateQuestEntryElement(int index)
    {
        VisualElement entry = new VisualElement();
        entry.name = $"quest-entry-{index}";
        entry.style.flexDirection = FlexDirection.Column;
        entry.style.marginBottom = 4;
        entry.style.paddingLeft = 8;
        entry.style.paddingRight = 8;

        // 이름 + 진행률 행
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;

        Label nameLabel = new Label("퀘스트");
        nameLabel.name = $"quest-name-{index}";
        nameLabel.style.fontSize = 12;
        nameLabel.style.color = new Color(1f, 0.9f, 0.6f);
        row.Add(nameLabel);

        Label progressLabel = new Label("0 / 0");
        progressLabel.name = $"quest-progress-{index}";
        progressLabel.style.fontSize = 11;
        progressLabel.style.color = Color.white;
        row.Add(progressLabel);

        entry.Add(row);

        // 프로그레스 바
        VisualElement barBg = new VisualElement();
        barBg.style.height = 4;
        barBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        barBg.style.borderBottomLeftRadius = 2;
        barBg.style.borderBottomRightRadius = 2;
        barBg.style.borderTopLeftRadius = 2;
        barBg.style.borderTopRightRadius = 2;

        VisualElement barFill = new VisualElement();
        barFill.name = $"quest-bar-fill-{index}";
        barFill.style.height = Length.Percent(100);
        barFill.style.width = Length.Percent(0);
        barFill.style.backgroundColor = new Color(0.9f, 0.7f, 0.2f, 1f);
        barFill.style.borderBottomLeftRadius = 2;
        barFill.style.borderBottomRightRadius = 2;
        barFill.style.borderTopLeftRadius = 2;
        barFill.style.borderTopRightRadius = 2;

        barBg.Add(barFill);
        entry.Add(barBg);

        return entry;
    }

    private struct QuestEntryUI
    {
        public VisualElement container;
        public Label nameLabel;
        public Label progressLabel;
        public VisualElement barFill;
    }
}