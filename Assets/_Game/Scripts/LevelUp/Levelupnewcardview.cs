// ──────────────────────────────────────────────
// LevelUpNewCardView.cs
// 새 4장 레벨업 시스템 전용 카드 뷰
//
// 강조 효과: CanvasGroup 알파값으로 처리
//   - 기본: 약간 투명 (normalAlpha)
//   - 호버: 밝게 (hoverAlpha)
//   - 선택: 가장 밝게 (selectedAlpha)
// ──────────────────────────────────────────────

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Game.LevelUp.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LevelUpNewCardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        // ── UI 레퍼런스 ───────────────────────────

        [Header("=== UI 레퍼런스 ===")]

        [SerializeField, Tooltip("카드 아이콘")]
        private Image iconImage;

        [SerializeField, Tooltip("카드 제목 텍스트")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("카드 설명 텍스트")]
        private TMP_Text descText;

        [SerializeField, Tooltip("카드 태그 텍스트")]
        private TMP_Text tagText;

        // ── 클릭 버튼 ─────────────────────────────

        [Header("=== 클릭 버튼(없으면 자동 탐색) ===")]

        [SerializeField, Tooltip("카드 선택 버튼")]
        private Button button;

        // ── 강조 효과 (알파값) ─────────────────────

        [Header("=== 강조 효과 (알파값) ===")]

        [SerializeField, Tooltip("기본 상태 알파값")]
        [Range(0f, 1f)]
        private float normalAlpha = 0.6f;

        [SerializeField, Tooltip("마우스 호버 시 알파값")]
        [Range(0f, 1f)]
        private float hoverAlpha = 0.85f;

        [SerializeField, Tooltip("선택 시 알파값")]
        [Range(0f, 1f)]
        private float selectedAlpha = 1f;

        // ── 내부 상태 ─────────────────────────────

        private CanvasGroup canvasGroup;
        private int cardIndex;
        private Action<int> onClick;
        private bool isHovering;
        private bool isSelected;

        // ════════════════════════════════════════════
        //  초기화
        // ════════════════════════════════════════════

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button == null)
                button = GetComponentInChildren<Button>();

            SetAlpha(normalAlpha);
        }

        // ════════════════════════════════════════════
        //  외부 API
        // ════════════════════════════════════════════

        /// <summary>
        /// 카드 데이터를 UI에 바인딩한다.
        /// </summary>
        public void Bind(LevelUpCardData data, int index, Action<int> clickAction)
        {
            cardIndex = index;
            onClick   = clickAction;

            // 제목
            if (nameText != null)
                nameText.text = data != null ? data.Title : string.Empty;

            // 설명
            if (descText != null)
                descText.text = data != null ? data.Description : string.Empty;

            // 태그
            if (tagText != null)
                tagText.text = data != null ? data.Tag : string.Empty;

            // 아이콘
            if (iconImage != null)
            {
                iconImage.sprite  = data != null ? data.Icon : null;
                iconImage.enabled = data != null && data.Icon != null;
            }

            // 버튼 클릭 연결
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            // 강조 초기화
            isSelected = false;
            isHovering = false;
            SetAlpha(normalAlpha);
        }

        /// <summary>
        /// 카드 선택 가능 여부를 설정한다.
        /// </summary>
        public void SetInteractable(bool isInteractable)
        {
            if (button != null)
                button.interactable = isInteractable;
        }

        /// <summary>
        /// 선택 강조 효과를 켜거나 끈다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateAlpha();
        }

        // ════════════════════════════════════════════
        //  마우스 호버 이벤트
        // ════════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            UpdateAlpha();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            UpdateAlpha();
        }

        // ════════════════════════════════════════════
        //  내부 로직
        // ════════════════════════════════════════════

        private void HandleClick()
        {
            onClick?.Invoke(cardIndex);
        }

        /// <summary>
        /// 현재 상태에 따라 알파값을 결정한다.
        /// 우선순위: 선택 > 호버 > 기본
        /// </summary>
        private void UpdateAlpha()
        {
            if (isSelected)
                SetAlpha(selectedAlpha);
            else if (isHovering)
                SetAlpha(hoverAlpha);
            else
                SetAlpha(normalAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }
    }
}