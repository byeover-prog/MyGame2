// ──────────────────────────────────────────────
// LevelUpCardPanelController.cs
// 새 4장용 레벨업 패널 컨트롤러
//
// 시간 정지/복구는 이 클래스가 하지 않는다.
// LevelUpFlowCoordinator가 큐 단위로 관리한다.
// Close 시 코디네이터에 닫힘을 통보할 뿐이다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace _Game.LevelUp.UI
{
    public sealed class LevelUpCardPanelController : MonoBehaviour
    {
        // ── 참조 ───────────────────────────────────

        [Header("=== 패널 루트 ===")]

        [SerializeField, Tooltip("패널 전체를 On/Off할 루트 오브젝트")]
        private GameObject panelRoot;

        [Header("=== 카드 4장 ===")]

        [SerializeField, Tooltip("카드 뷰 슬롯 1")]
        private LevelUpNewCardView cardView1;

        [SerializeField, Tooltip("카드 뷰 슬롯 2")]
        private LevelUpNewCardView cardView2;

        [SerializeField, Tooltip("카드 뷰 슬롯 3")]
        private LevelUpNewCardView cardView3;

        [SerializeField, Tooltip("카드 뷰 슬롯 4")]
        private LevelUpNewCardView cardView4;

        [Header("=== 보상 적용 ===")]

        [SerializeField, Tooltip("카드 선택 시 보상을 적용하는 서비스")]
        private LevelUpRewardApplier rewardApplier;

        [Header("=== 흐름 코디네이터 ===")]

        [SerializeField, Tooltip("큐/시간 관리를 담당하는 코디네이터 (닫힘 통보용)")]
        private LevelUpFlowCoordinator flowCoordinator;

        // ── 내부 상태 ─────────────────────────────

        private readonly List<LevelUpCardData> currentCards = new List<LevelUpCardData>(4);
        private bool isOpen;

        public bool IsOpen => isOpen;

        // ════════════════════════════════════════════
        //  외부 API
        // ════════════════════════════════════════════

        /// <summary>
        /// 카드 데이터를 받아 패널을 연다.
        /// 시간 정지는 코디네이터가 이미 처리한 상태.
        /// </summary>
        public void Open(List<LevelUpCardData> cards)
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

            BindCards();
            SetPanelVisible(true);
            SetCardInteractable(true);

            isOpen = true;
        }

        /// <summary>
        /// 패널을 닫는다.
        /// 시간 복구는 하지 않는다 — 코디네이터가 큐 종료 시 처리.
        /// </summary>
        public void Close()
        {
            if (!isOpen)
                return;

            currentCards.Clear();
            SetPanelVisible(false);
            isOpen = false;

            // 코디네이터에 닫힘 통보 (시간 복구는 코디네이터 판단)
            if (flowCoordinator != null)
                flowCoordinator.NotifyPanelClosed();
        }

        // ════════════════════════════════════════════
        //  내부 로직
        // ════════════════════════════════════════════

        private void BindCards()
        {
            BindCard(cardView1, 0);
            BindCard(cardView2, 1);
            BindCard(cardView3, 2);
            BindCard(cardView4, 3);
        }

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
                $"[CardPanel] 카드 선택 | " +
                $"index={index} | " +
                $"type={selectedCard.RewardType} | " +
                $"title={selectedCard.Title} | " +
                $"applied={applied}",
                this);

            Close();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private void SetCardInteractable(bool interactable)
        {
            if (cardView1 != null) cardView1.SetInteractable(interactable);
            if (cardView2 != null) cardView2.SetInteractable(interactable);
            if (cardView3 != null) cardView3.SetInteractable(interactable);
            if (cardView4 != null) cardView4.SetInteractable(interactable);
        }
    }
}