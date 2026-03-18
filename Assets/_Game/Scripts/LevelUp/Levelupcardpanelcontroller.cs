// ──────────────────────────────────────────────
// LevelUpCardPanelController.cs
// 새 4장용 레벨업 패널 컨트롤러
//
// 시간 정지/복구는 이 클래스가 하지 않는다.
// LevelUpFlowCoordinator가 큐 단위로 관리한다.
// 보상 적용이 실패하면 패널을 닫지 않고 다시 선택 가능 상태로 되돌린다.
//
// 리롤: 패널이 열린 상태에서 카드를 재생성한다.
// - cardGenerator + loadout 참조 필요
// - 리롤 횟수는 패널이 열릴 때마다 초기화
// ──────────────────────────────────────────────

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.Player;

namespace _Game.LevelUp.UI
{
    /// <summary>
    /// 레벨업 카드 패널 표시/선택/리롤을 담당하는 컨트롤러입니다.
    /// 시간 정지와 큐 흐름은 별도 코디네이터가 관리합니다.
    /// </summary>
    public sealed class LevelUpCardPanelController : MonoBehaviour
    {
        [Header("패널 루트")]
        [SerializeField, Tooltip("패널 전체를 On/Off할 루트 오브젝트입니다.")]
        private GameObject panelRoot;

        [Header("카드 4장")]
        [SerializeField, Tooltip("카드 뷰 슬롯 1입니다.")]
        private LevelUpNewCardView cardView1;

        [SerializeField, Tooltip("카드 뷰 슬롯 2입니다.")]
        private LevelUpNewCardView cardView2;

        [SerializeField, Tooltip("카드 뷰 슬롯 3입니다.")]
        private LevelUpNewCardView cardView3;

        [SerializeField, Tooltip("카드 뷰 슬롯 4입니다.")]
        private LevelUpNewCardView cardView4;

        [Header("보상 적용")]
        [SerializeField, Tooltip("카드 선택 시 보상을 적용하는 서비스입니다.")]
        private LevelUpRewardApplier rewardApplier;

        [Header("흐름 코디네이터")]
        [SerializeField, Tooltip("큐/시간 관리를 담당하는 코디네이터입니다. 패널 닫힘 통보에 사용합니다.")]
        private LevelUpFlowCoordinator flowCoordinator;

        [Header("리롤")]
        [SerializeField, Tooltip("리롤 버튼입니다.")]
        private Button rerollButton;

        [SerializeField, Tooltip("남은 리롤 횟수 표시 텍스트입니다.")]
        private TextMeshProUGUI rerollCountText;

        [SerializeField, Tooltip("패널 1회 오픈 당 최대 리롤 횟수입니다.")]
        [Min(0)]
        private int rerollMaxCount = 1;

        [Header("리롤용 카드 재생성 참조")]
        [SerializeField, Tooltip("리롤 시 카드를 다시 생성할 카드 생성기입니다.")]
        private LevelUpCardGenerator cardGenerator;

        [SerializeField, Tooltip("리롤 시 카드 생성에 필요한 플레이어 스킬 로드아웃입니다.")]
        private PlayerSkillLoadout loadout;

        [Header("디버그 표시")]
        [SerializeField, Tooltip("대기 중 레벨업 횟수 표시 텍스트입니다. 없으면 비워도 됩니다.")]
        private TextMeshProUGUI queuedCountText;

        private readonly List<LevelUpCardData> currentCards = new List<LevelUpCardData>(4);
        private bool isOpen;
        private int rerollsLeft;
        private int currentQueuedCount;

        /// <summary>
        /// 현재 패널이 열려 있는지 반환합니다.
        /// </summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(OnRerollClicked);
            }

            UpdateQueuedCountUI();
            UpdateRerollUI();
        }

        /// <summary>
        /// 전달받은 카드 목록으로 패널을 엽니다.
        /// </summary>
        public void Open(List<LevelUpCardData> cards)
        {
            Open(cards, 0);
        }

        /// <summary>
        /// 전달받은 카드 목록과 대기 큐 개수로 패널을 엽니다.
        /// 기존 호출부 호환용 오버로드입니다.
        /// </summary>
        public void Open(List<LevelUpCardData> cards, int queuedCount)
        {
            if (cards == null || cards.Count == 0)
            {
                Debug.LogWarning("[CardPanel] 표시할 카드가 없습니다.", this);
                return;
            }

            if (isOpen)
            {
                Debug.LogWarning("[CardPanel] 이미 열려있습니다.", this);
                return;
            }

            currentCards.Clear();
            currentCards.AddRange(cards);
            currentQueuedCount = Mathf.Max(0, queuedCount);

            // 패널이 열릴 때마다 리롤 횟수를 초기화합니다.
            rerollsLeft = rerollMaxCount;

            BindCards();
            SetPanelVisible(true);
            SetCardInteractable(true);

            isOpen = true;

            UpdateQueuedCountUI();
            UpdateRerollUI();
        }

        /// <summary>
        /// 패널을 닫고 카드 목록을 정리합니다.
        /// </summary>
        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            currentCards.Clear();
            currentQueuedCount = 0;

            SetPanelVisible(false);
            isOpen = false;

            UpdateQueuedCountUI();
            UpdateRerollUI();

            if (flowCoordinator != null)
            {
                flowCoordinator.NotifyPanelClosed();
            }
        }

        /// <summary>
        /// 리롤 버튼 클릭 시 카드를 다시 생성합니다.
        /// </summary>
        private void OnRerollClicked()
        {
            if (!isOpen || rerollsLeft <= 0)
            {
                return;
            }

            if (cardGenerator == null || loadout == null)
            {
                Debug.LogWarning("[CardPanel] 리롤 불가 - cardGenerator 또는 loadout이 연결되지 않았습니다.", this);
                return;
            }

            rerollsLeft--;

            List<LevelUpCardData> newCards = cardGenerator.Generate(loadout);

            if (newCards == null || newCards.Count == 0)
            {
                Debug.LogWarning("[CardPanel] 리롤 실패 - 생성된 카드가 없습니다.", this);
                UpdateRerollUI();
                return;
            }

            currentCards.Clear();
            currentCards.AddRange(newCards);

            BindCards();
            SetCardInteractable(true);
            UpdateRerollUI();

            Debug.Log($"[CardPanel] 리롤 완료 | 카드 {newCards.Count}장 | 남은 리롤 {rerollsLeft}", this);
        }

        /// <summary>
        /// 리롤 버튼과 카운트 표시를 갱신합니다.
        /// </summary>
        private void UpdateRerollUI()
        {
            bool canReroll = isOpen && rerollsLeft > 0;

            if (rerollButton != null)
            {
                rerollButton.interactable = canReroll;
            }

            if (rerollCountText != null)
            {
                rerollCountText.text = rerollsLeft.ToString();
            }
        }

        /// <summary>
        /// 대기 중 레벨업 횟수 표시를 갱신합니다.
        /// </summary>
        private void UpdateQueuedCountUI()
        {
            if (queuedCountText != null)
            {
                queuedCountText.text = currentQueuedCount.ToString();
            }
        }

        /// <summary>
        /// 현재 카드 목록을 각 카드 뷰에 바인딩합니다.
        /// </summary>
        private void BindCards()
        {
            BindCard(cardView1, 0);
            BindCard(cardView2, 1);
            BindCard(cardView3, 2);
            BindCard(cardView4, 3);
        }

        /// <summary>
        /// 단일 카드 뷰에 카드 데이터를 연결합니다.
        /// </summary>
        private void BindCard(LevelUpNewCardView view, int index)
        {
            if (view == null)
            {
                return;
            }

            if (index < 0 || index >= currentCards.Count)
            {
                view.gameObject.SetActive(false);
                return;
            }

            view.gameObject.SetActive(true);
            view.Bind(currentCards[index], index, HandleCardSelected);
        }

        /// <summary>
        /// 카드 선택 시 보상을 적용하고 성공하면 패널을 닫습니다.
        /// </summary>
        private void HandleCardSelected(int index)
        {
            if (!isOpen)
            {
                return;
            }

            if (index < 0 || index >= currentCards.Count)
            {
                return;
            }

            SetCardInteractable(false);

            LevelUpCardData selectedCard = currentCards[index];
            bool applied = rewardApplier != null && rewardApplier.Apply(selectedCard);

            Debug.Log(
                $"[CardPanel] 카드 선택 | index={index} | type={selectedCard.RewardType} | title={selectedCard.Title} | applied={applied}",
                this);

            if (!applied)
            {
                Debug.LogWarning($"[CardPanel] 보상 적용 실패 -> 패널 유지: {selectedCard.Title}", this);
                SetCardInteractable(true);
                return;
            }

            Close();
        }

        /// <summary>
        /// 패널 루트 활성화 상태를 변경합니다.
        /// </summary>
        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 모든 카드의 입력 가능 여부를 변경합니다.
        /// </summary>
        private void SetCardInteractable(bool interactable)
        {
            if (cardView1 != null) cardView1.SetInteractable(interactable);
            if (cardView2 != null) cardView2.SetInteractable(interactable);
            if (cardView3 != null) cardView3.SetInteractable(interactable);
            if (cardView4 != null) cardView4.SetInteractable(interactable);
        }
    }
}