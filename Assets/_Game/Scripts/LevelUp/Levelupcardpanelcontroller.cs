// ──────────────────────────────────────────────
// LevelUpCardPanelController.cs
// UI Toolkit 기반 레벨업 카드 패널
// LevelUpUI.uxml / LevelUpUI.uss 사용
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Game.LevelUp;
using _Game.Player;

namespace _Game.LevelUp.UI
{
    public sealed class LevelUpCardPanelController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;

        [Header("연결")]
        [SerializeField] private LevelUpRewardApplier    rewardApplier;
        [SerializeField] private LevelUpFlowCoordinator  flowCoordinator;
        [SerializeField] private LevelUpCardGenerator    cardGenerator;
        [SerializeField] private PlayerSkillLoadout       loadout;
        
        [Header("HUD 연결")]
        [SerializeField] private HUDController hudController;

        [Header("리롤")]
        [SerializeField, Min(0)] private int rerollMaxCount = 1;

        // ── 런타임 ───────────────────────────────
        private VisualElement            root;
        private VisualElement            levelUpRoot;
        private VisualElement[]          cardEls   = new VisualElement[4];
        private Label[]                  cardNames = new Label[4];
        private Label[]                  cardDescs = new Label[4];
        private Label[]                  cardTags  = new Label[4];
        private VisualElement[]          cardIcons = new VisualElement[4];
        private Button                   btnMinimize;
        private Button                   btnReroll;

        private List<LevelUpCardData>    currentCards = new List<LevelUpCardData>(4);
        private bool                     isOpen;
        private int                      rerollsLeft;

        public bool IsOpen => isOpen;
        private bool _isMinimized = false;

        // ─────────────────────────────────────────
        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            root        = uiDocument.rootVisualElement;
            levelUpRoot = root.Q<VisualElement>("LevelUpRoot");

            // 카드 요소 캐시 + 클릭 등록
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                cardEls[i]   = root.Q<VisualElement>($"Card{i}");
                cardNames[i] = root.Q<Label>($"Card{i}Name");
                cardDescs[i] = root.Q<Label>($"Card{i}Desc");
                cardTags[i]  = root.Q<Label>($"Card{i}Tag");
                cardIcons[i] = root.Q<VisualElement>($"Card{i}Icon");

                if (cardEls[i] == null) continue;

                cardEls[i].RegisterCallback<ClickEvent>(_ => OnCardClicked(idx));

                // 호버
                cardEls[i].RegisterCallback<MouseEnterEvent>(_ =>
                {
                    cardEls[idx].style.borderTopColor    = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    cardEls[idx].style.borderBottomColor = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    cardEls[idx].style.borderLeftColor   = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    cardEls[idx].style.borderRightColor  = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    cardEls[idx].style.backgroundColor  = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 0.12f));
                });

                cardEls[i].RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    cardEls[idx].style.borderTopColor    = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 0.3f));
                    cardEls[idx].style.borderBottomColor = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 0.3f));
                    cardEls[idx].style.borderLeftColor   = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 0.3f));
                    cardEls[idx].style.borderRightColor  = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 0.3f));
                    cardEls[idx].style.backgroundColor  = new StyleColor(new Color(0.02f, 0.03f, 0.08f, 0.95f));
                });
            }
            btnMinimize = root.Q<Button>("BtnMinimize");
            btnReroll   = root.Q<Button>("BtnReroll");

            btnMinimize?.RegisterCallback<ClickEvent>(_ =>
            {
                if (!_isMinimized) Minimize();
                else Restore();
            });
            btnReroll?.RegisterCallback<ClickEvent>(_ => OnRerollClicked());

            // 버튼 호버
            foreach (var btn in new[] { btnMinimize, btnReroll })
            {
                if (btn == null) continue;
                btn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    btn.style.borderTopColor    = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    btn.style.borderBottomColor = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    btn.style.borderLeftColor   = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    btn.style.borderRightColor  = new StyleColor(new Color(0.78f, 0.63f, 0.31f, 1f));
                    btn.style.color = new StyleColor(new Color(0.91f, 0.78f, 0.47f));
                });
                btn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    btn.style.borderTopColor    = StyleKeyword.Null;
                    btn.style.borderBottomColor = StyleKeyword.Null;
                    btn.style.borderLeftColor   = StyleKeyword.Null;
                    btn.style.borderRightColor  = StyleKeyword.Null;
                    btn.style.color = StyleKeyword.Null;
                });
            }

            // 시작 시 숨김
            SetVisible(false);
        }

        // ── 외부 API ─────────────────────────────

        public void Open(List<LevelUpCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                GameLogger.LogWarning("[CardPanel] 표시할 카드가 없습니다.");
                return;
            }
            if (isOpen)
            {
                GameLogger.LogWarning("[CardPanel] 이미 열려있습니다.");
                return;
            }

            currentCards.Clear();
            currentCards.AddRange(cards);

            rerollsLeft = rerollMaxCount;
            isOpen = true;
            BindCards();
            SetCardInteractable(true);
            SetVisible(true);
            UpdateRerollUI();

        }

        public void Close()
        {
            if (!isOpen) return;
            SetVisible(false);

            currentCards.Clear();
            SetVisible(false);
            isOpen = false;

            UpdateRerollUI();
            flowCoordinator?.NotifyPanelClosed();
        }

        public void Minimize()
        {
            if (!isOpen) return;

            root.Q<VisualElement>("CardRow").style.display  = DisplayStyle.None;
            root.Q<Label>("LevelUpTitle").style.display     = DisplayStyle.None;
            root.Q<Label>("LevelUpSub").style.display       = DisplayStyle.None;
            if (btnReroll != null) btnReroll.style.display  = DisplayStyle.None;

            _isMinimized = true;
            if (btnMinimize != null) btnMinimize.text = "▲ 스킬 선택";
        }

        public void Restore()
        {
            root.Q<VisualElement>("CardRow").style.display  = DisplayStyle.Flex;
            root.Q<Label>("LevelUpTitle").style.display     = DisplayStyle.Flex;
            root.Q<Label>("LevelUpSub").style.display       = DisplayStyle.Flex;
            if (btnReroll != null) btnReroll.style.display  = DisplayStyle.Flex;

            _isMinimized = false;
            if (btnMinimize != null) btnMinimize.text = "최소화";
        }
        // ── 카드 바인딩 ──────────────────────────
        private void BindCards()
        {
            for (int i = 0; i < 4; i++)
            {
                if (cardEls[i] == null) continue;

                bool hasCard = i < currentCards.Count;
                cardEls[i].style.display = hasCard
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                if (!hasCard) continue;

                var data = currentCards[i];

                if (cardNames[i] != null)
                    cardNames[i].text = data.Title ?? "";

                if (cardDescs[i] != null)
                    cardDescs[i].text = data.Description ?? "";

                if (cardTags[i] != null)
                    cardTags[i].text = data.Tag ?? "";

                if (cardIcons[i] != null && data.Icon != null)
                    cardIcons[i].style.backgroundImage =
                        new StyleBackground(data.Icon);
                
                int idx = i;
                cardEls[i].SetEnabled(true);
            }
        }

        // ── 카드 선택 ────────────────────────────
        private void OnCardClicked(int index)
        {
            if (!isOpen) return;
            if (index < 0 || index >= currentCards.Count) return;

            SetCardInteractable(false);

            var data    = currentCards[index];
            bool applied = rewardApplier != null && rewardApplier.Apply(data);

            GameLogger.Log($"[CardPanel] 선택 | {data.Title} | applied={applied}");

            if (!applied)
            {
                GameLogger.LogWarning($"[CardPanel] 보상 적용 실패 → 패널 유지: {data.Title}");
                SetCardInteractable(true);
                return;
            }

            Close();
        }

        // ── 리롤 ─────────────────────────────────
        private void OnRerollClicked()
        {
            if (!isOpen || rerollsLeft <= 0) return;
            if (cardGenerator == null || loadout == null)
            {
                GameLogger.LogWarning("[CardPanel] 리롤 불가 — 참조 누락");
                return;
            }

            rerollsLeft--;

            var newCards = cardGenerator.Generate(loadout);
            if (newCards == null || newCards.Count == 0)
            {
                GameLogger.LogWarning("[CardPanel] 리롤 실패 — 카드 없음");
                UpdateRerollUI();
                return;
            }

            currentCards.Clear();
            currentCards.AddRange(newCards);
            BindCards();
            SetCardInteractable(true);
            UpdateRerollUI();

            GameLogger.Log($"[CardPanel] 리롤 완료 | {newCards.Count}장 | 남은 {rerollsLeft}");
        }

        private void UpdateRerollUI()
        {
            if (btnReroll == null) return;
            bool canReroll = isOpen && rerollsLeft > 0;
            btnReroll.SetEnabled(canReroll);
            btnReroll.text = $"새로고침 ({rerollsLeft})";
            
        }

        // ── 유틸 ─────────────────────────────────
        private void SetVisible(bool visible)
        {
            if (levelUpRoot == null) return;
            levelUpRoot.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void SetCardInteractable(bool interactable)
        {
            for (int i = 0; i < 4; i++)
            {
                if (cardEls[i] == null) continue;
                cardEls[i].SetEnabled(interactable);
            }
        }
    }
}