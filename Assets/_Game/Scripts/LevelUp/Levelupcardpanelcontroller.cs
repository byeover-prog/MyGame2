// ──────────────────────────────────────────────
// LevelUpCardPanelController.cs
// 새 4장용 레벨업 패널 컨트롤러
//
// 구현 원리 요약:
// 레벨업 패널은 "현재 레벨업 세션에서 전달받은 loadout"만 사용한다.
// 리롤 시 인스펙터의 별도 loadout을 보지 않고, Open() 때 받은 동일 인스턴스를 재사용한다.
// 보상 적용기에도 같은 loadout을 주입해서 생성/리롤/적용 경로를 하나로 고정한다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.Player;

namespace _Game.LevelUp.UI
{
    /// <summary>
    /// 레벨업 카드 패널 표시/선택/리롤을 담당한다.
    /// 시간 정지/복구는 담당하지 않으며, FlowCoordinator가 관리한다.
    /// </summary>
    public sealed class LevelUpCardPanelController : MonoBehaviour
    {
        [Header("=== 패널 루트 ===")]
        [SerializeField, Tooltip("패널 전체를 On/Off할 루트 오브젝트입니다.")]
        private GameObject panelRoot;

        [Header("=== 카드 4장 ===")]
        [SerializeField, Tooltip("카드 뷰 슬롯 1입니다.")]
        private LevelUpNewCardView cardView1;

        [SerializeField, Tooltip("카드 뷰 슬롯 2입니다.")]
        private LevelUpNewCardView cardView2;

        [SerializeField, Tooltip("카드 뷰 슬롯 3입니다.")]
        private LevelUpNewCardView cardView3;

        [SerializeField, Tooltip("카드 뷰 슬롯 4입니다.")]
        private LevelUpNewCardView cardView4;

        [Header("=== 보상 적용 ===")]
        [SerializeField, Tooltip("카드 선택 시 보상을 적용하는 서비스입니다.")]
        private LevelUpRewardApplier rewardApplier;

        [Header("=== 흐름 코디네이터 ===")]
        [SerializeField, Tooltip("큐/시간 관리를 담당하는 코디네이터입니다.")]
        private LevelUpFlowCoordinator flowCoordinator;

        [Header("=== 리롤 ===")]
        [SerializeField, Tooltip("리롤 버튼입니다.")]
        private Button rerollButton;

        [SerializeField, Tooltip("남은 리롤 횟수 표시 텍스트입니다.")]
        private TextMeshProUGUI rerollCountText;

        [SerializeField, Tooltip("패널 1회 오픈 당 최대 리롤 횟수입니다.")]
        [Min(0)]
        private int rerollMaxCount = 1;

        [Header("=== 리롤용 카드 재생성 참조 ===")]
        [SerializeField, Tooltip("카드 생성기입니다. 리롤 시 같은 loadout 기준으로 다시 뽑습니다.")]
        private LevelUpCardGenerator cardGenerator;

        [SerializeField, Tooltip("초기 디버그용 기본 loadout입니다. 실제 동작은 Open()에서 전달받은 loadout을 우선 사용합니다.")]
        private PlayerSkillLoadout fallbackLoadout;

        /// <summary>현재 패널에 표시 중인 카드 목록</summary>
        private readonly List<LevelUpCardData> currentCards = new List<LevelUpCardData>(4);

        /// <summary>패널 열림 여부</summary>
        private bool isOpen;

        /// <summary>현재 패널에서 남은 리롤 횟수</summary>
        private int rerollsLeft;

        /// <summary>
        /// 현재 레벨업 세션에서 강제로 사용해야 하는 loadout.
        /// 생성 / 리롤 / 보상 적용이 모두 이 인스턴스를 공유한다.
        /// </summary>
        private PlayerSkillLoadout currentLoadout;

        /// <summary>패널 열림 여부</summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (rerollButton != null)
                rerollButton.onClick.AddListener(OnRerollClicked);

            UpdateRerollUI();
        }

        /// <summary>
        /// 레벨업 패널을 연다.
        /// 반드시 이번 세션의 loadout을 함께 받아서 리롤/보상 경로를 동일 인스턴스로 고정한다.
        /// </summary>
        public void Open(List<LevelUpCardData> cards, PlayerSkillLoadout runtimeLoadout)
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

            currentLoadout = runtimeLoadout != null ? runtimeLoadout : fallbackLoadout;

            if (currentLoadout == null)
            {
                Debug.LogWarning("[CardPanel] loadout이 없습니다. 패널을 열 수 없습니다.", this);
                return;
            }

            if (rewardApplier != null)
                rewardApplier.SetRuntimeLoadout(currentLoadout);

            currentCards.Clear();
            currentCards.AddRange(cards);

            rerollsLeft = rerollMaxCount;

            BindCards();
            SetPanelVisible(true);
            SetCardInteractable(true);

            isOpen = true;
            UpdateRerollUI();

            Debug.Log(
                $"[CardPanel] 오픈 | 카드={currentCards.Count} | loadoutInstanceId={currentLoadout.GetInstanceID()} | loadoutName={currentLoadout.name}",
                currentLoadout);
        }

        /// <summary>
        /// 레벨업 패널을 닫는다.
        /// </summary>
        public void Close()
        {
            if (!isOpen)
                return;

            currentCards.Clear();
            SetPanelVisible(false);
            isOpen = false;

            UpdateRerollUI();

            Debug.Log("[CardPanel] 닫힘", this);

            if (flowCoordinator != null)
                flowCoordinator.NotifyPanelClosed();
        }

        /// <summary>
        /// 리롤 버튼 처리.
        /// 현재 세션에서 전달받은 동일 loadout으로만 다시 생성한다.
        /// </summary>
        private void OnRerollClicked()
        {
            if (!isOpen || rerollsLeft <= 0)
                return;

            if (cardGenerator == null)
            {
                Debug.LogWarning("[CardPanel] 리롤 불가 — cardGenerator가 연결되지 않았습니다.", this);
                return;
            }

            if (currentLoadout == null)
            {
                currentLoadout = fallbackLoadout;
            }

            if (currentLoadout == null)
            {
                Debug.LogWarning("[CardPanel] 리롤 불가 — currentLoadout이 없습니다.", this);
                return;
            }

            rerollsLeft--;

            Debug.Log(
                $"[CardPanel] 리롤 시작 | loadoutInstanceId={currentLoadout.GetInstanceID()} | loadoutName={currentLoadout.name} | 남은 리롤(차감 후)={rerollsLeft}",
                currentLoadout);

            List<LevelUpCardData> newCards = cardGenerator.Generate(currentLoadout);

            if (newCards == null || newCards.Count == 0)
            {
                Debug.LogWarning("[CardPanel] 리롤 실패 — 생성된 카드 없음.", this);
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
        /// 리롤 UI를 갱신한다.
        /// </summary>
        private void UpdateRerollUI()
        {
            bool canReroll = isOpen && rerollsLeft > 0;

            if (rerollButton != null)
                rerollButton.interactable = canReroll;

            if (rerollCountText != null)
                rerollCountText.text = rerollsLeft.ToString();
        }

        /// <summary>
        /// 현재 카드 목록을 카드 뷰에 바인딩한다.
        /// </summary>
        private void BindCards()
        {
            BindCard(cardView1, 0);
            BindCard(cardView2, 1);
            BindCard(cardView3, 2);
            BindCard(cardView4, 3);
        }

        /// <summary>
        /// 카드 1장을 특정 카드 뷰에 바인딩한다.
        /// </summary>
        private void BindCard(LevelUpNewCardView view, int index)
        {
            if (view == null)
                return;

            if (index < 0 || index >= currentCards.Count)
            {
                view.gameObject.SetActive(false);
                return;
            }

            view.gameObject.SetActive(true);
            view.Bind(currentCards[index], index, HandleCardSelected);
        }

        /// <summary>
        /// 카드 선택 처리.
        /// </summary>
        private void HandleCardSelected(int index)
        {
            if (!isOpen)
                return;

            if (index < 0 || index >= currentCards.Count)
                return;

            SetCardInteractable(false);

            LevelUpCardData selectedCard = currentCards[index];
            bool applied = rewardApplier != null && rewardApplier.Apply(selectedCard);

            Debug.Log(
                $"[CardPanel] 카드 선택 | index={index} | type={selectedCard.RewardType} | title={selectedCard.Title} | applied={applied}",
                this);

            if (!applied)
            {
                Debug.LogWarning($"[CardPanel] 보상 적용 실패 → 패널 유지: {selectedCard.Title}", this);
                SetCardInteractable(true);
                return;
            }

            Close();
        }

        /// <summary>
        /// 패널 표시 상태를 설정한다.
        /// </summary>
        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>
        /// 카드 버튼 상호작용을 일괄 설정한다.
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