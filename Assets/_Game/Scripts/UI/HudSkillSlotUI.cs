using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.UI.HUD
{
    /// <summary>
    /// 인게임 하단 스킬 슬롯 1칸 표시 전용.
    /// 표시만 담당하고, 실제 스킬 발동 로직은 넣지 않는다.
    /// 궁극기/지원 궁극기는 현재 "참조용 자리"로도 사용할 수 있다.
    /// </summary>
    public sealed class HudSkillSlotUI : MonoBehaviour
    {
        [Header("기본 참조")]
        [SerializeField, Tooltip("슬롯 테두리 이미지")]
        private Image frame_Image;

        [SerializeField, Tooltip("스킬 아이콘 이미지")]
        private Image icon_Image;

        [SerializeField, Tooltip("쿨타임 마스크 이미지 (Fill Amount 사용)")]
        private Image cooldown_Mask_Image;

        [SerializeField, Tooltip("입력키 텍스트")]
        private TextMeshProUGUI key_Text;

        [SerializeField, Tooltip("레벨 또는 강화 수치 텍스트")]
        private TextMeshProUGUI level_Text;

        [SerializeField, Tooltip("잠금 상태 아이콘")]
        private GameObject lock_Object;

        [SerializeField, Tooltip("비활성 오버레이")]
        private GameObject disabled_Overlay_Object;

        [Header("상태")]
        [SerializeField, Tooltip("현재 빈 슬롯인지 여부")]
        private bool is_Empty = true;

        [SerializeField, Tooltip("현재 잠금 상태인지 여부")]
        private bool is_Locked = false;

        [SerializeField, Tooltip("현재 미구현/참조 전용 상태인지 여부")]
        private bool is_Placeholder = false;

        /// <summary>
        /// 빈 슬롯 상태로 초기화.
        /// </summary>
        public void SetEmpty(string key_Label = "")
        {
            is_Empty = true;
            is_Locked = false;
            is_Placeholder = false;

            if (icon_Image != null)
            {
                icon_Image.enabled = false;
                icon_Image.sprite = null;
            }

            if (key_Text != null)
            {
                key_Text.text = key_Label;
            }

            if (level_Text != null)
            {
                level_Text.text = string.Empty;
            }

            if (cooldown_Mask_Image != null)
            {
                cooldown_Mask_Image.fillAmount = 0f;
                cooldown_Mask_Image.gameObject.SetActive(false);
            }

            if (lock_Object != null)
            {
                lock_Object.SetActive(false);
            }

            if (disabled_Overlay_Object != null)
            {
                disabled_Overlay_Object.SetActive(true);
            }
        }

        /// <summary>
        /// 일반 스킬 슬롯 표시.
        /// </summary>
        public void SetSkill(Sprite icon_Sprite, string key_Label = "", string level_Label = "")
        {
            is_Empty = false;
            is_Locked = false;
            is_Placeholder = false;

            if (icon_Image != null)
            {
                icon_Image.enabled = true;
                icon_Image.sprite = icon_Sprite;
            }

            if (key_Text != null)
            {
                key_Text.text = key_Label;
            }

            if (level_Text != null)
            {
                level_Text.text = level_Label;
            }

            if (cooldown_Mask_Image != null)
            {
                cooldown_Mask_Image.fillAmount = 0f;
                cooldown_Mask_Image.gameObject.SetActive(true);
            }

            if (lock_Object != null)
            {
                lock_Object.SetActive(false);
            }

            if (disabled_Overlay_Object != null)
            {
                disabled_Overlay_Object.SetActive(false);
            }
        }

        /// <summary>
        /// 궁극기/지원 궁극기처럼 아직 미구현인 슬롯을
        /// "참조용 자리" 상태로 표시한다.
        /// </summary>
        public void SetPlaceholder(Sprite icon_Sprite, string key_Label = "")
        {
            is_Empty = false;
            is_Locked = true;
            is_Placeholder = true;

            if (icon_Image != null)
            {
                icon_Image.enabled = true;
                icon_Image.sprite = icon_Sprite;
                icon_Image.color = new Color(1f, 1f, 1f, 0.45f);
            }

            if (key_Text != null)
            {
                key_Text.text = key_Label;
            }

            if (level_Text != null)
            {
                level_Text.text = string.Empty;
            }

            if (cooldown_Mask_Image != null)
            {
                cooldown_Mask_Image.fillAmount = 0f;
                cooldown_Mask_Image.gameObject.SetActive(false);
            }

            if (lock_Object != null)
            {
                lock_Object.SetActive(true);
            }

            if (disabled_Overlay_Object != null)
            {
                disabled_Overlay_Object.SetActive(true);
            }
        }

        /// <summary>
        /// 쿨타임 표시 갱신.
        /// remainingRatio = 1이면 가득 막힘, 0이면 사용 가능.
        /// </summary>
        public void SetCooldown(float remainingRatio)
        {
            if (cooldown_Mask_Image == null)
            {
                return;
            }

            cooldown_Mask_Image.gameObject.SetActive(true);
            cooldown_Mask_Image.fillAmount = Mathf.Clamp01(remainingRatio);
        }
    }
}