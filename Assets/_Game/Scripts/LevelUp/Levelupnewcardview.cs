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
        // UI 레퍼런스

        [Header("=== UI 레퍼런스 ===")]

        [SerializeField, Tooltip("카드 아이콘")]
        private Image iconImage;

        [SerializeField, Tooltip("카드 제목 텍스트")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("카드 설명 텍스트")]
        private TMP_Text descText;

        [SerializeField, Tooltip("카드 태그 텍스트")]
        private TMP_Text tagText;

        // 전용 스킬 시각 구분

        [Header("=== 전용 스킬 시각 구분 ===")]

        [SerializeField, Tooltip("전용 스킬일 때 색상을 변경할 테두리 이미지 (없으면 무시)")]
        private Image borderImage;

        [SerializeField, Tooltip("전용 스킬 테두리 색상")]
        private Color exclusiveColor = new Color(1f, 0.84f, 0f, 1f); // 금색

        [SerializeField, Tooltip("일반 카드 테두리 색상")]
        private Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField, Tooltip("전용 태그 텍스트 색상")]
        private Color exclusiveTagColor = new Color(1f, 0.84f, 0f, 1f);

        [SerializeField, Tooltip("일반 태그 텍스트 색상")]
        private Color normalTagColor = Color.white;

        // 클릭 버튼

        [Header("=== 클릭 버튼(없으면 자동 탐색) ===")]

        [SerializeField, Tooltip("카드 선택 버튼")]
        private Button button;

        // 강조 효과 (알파값)

        [Header("=== 강조 효과 (알파값) ===")]

        [SerializeField, Range(0f, 1f)]
        private float normalAlpha = 0.6f;

        [SerializeField, Range(0f, 1f)]
        private float hoverAlpha = 0.85f;

        [SerializeField, Range(0f, 1f)]
        private float selectedAlpha = 1f;

        // 내부 상태

        private CanvasGroup canvasGroup;
        private int cardIndex;
        private Action<int> onClick;
        private bool isHovering;
        private bool isSelected;
        
        //  초기화

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button == null)
                button = GetComponentInChildren<Button>();

            SetAlpha(normalAlpha);
        }
        
        //  외부 API
        // 카드 데이터를 UI에 바인딩한다.
        public void Bind(LevelUpCardData data, int index, Action<int> clickAction)
        {
            cardIndex = index;
            onClick   = clickAction;

            if (nameText != null)
                nameText.text = data != null ? data.Title : string.Empty;

            if (descText != null)
                descText.text = data != null ? data.Description : string.Empty;

            if (tagText != null)
                tagText.text = data != null ? data.Tag : string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite  = data != null ? data.Icon : null;
                iconImage.enabled = data != null && data.Icon != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            isSelected = false;
            isHovering = false;
            SetAlpha(normalAlpha);
        }

        // 카드 선택 가능 여부를 설정한다.
        public void SetInteractable(bool isInteractable)
        {
            if (button != null)
                button.interactable = isInteractable;
        }

        // 선택 강조 효과를 켜거나 끈다.
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateAlpha();
        }
        
        // 전용 스킬 시각 구분을 설정한다.
        // true면 테두리 색상 변경 + 태그 텍스트 색상 변경.
        
        public void SetExclusive(bool exclusive)
        {
            if (borderImage != null)
                borderImage.color = exclusive ? exclusiveColor : normalBorderColor;

            if (tagText != null)
                tagText.color = exclusive ? exclusiveTagColor : normalTagColor;
        }
        
        //  마우스 호버 이벤트

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
        
        //  내부 로직

        private void HandleClick()
        {
            onClick?.Invoke(cardIndex);
        }

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