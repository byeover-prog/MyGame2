using UnityEngine;

namespace _Game.Scripts.UI.HUD
{
    /// <summary>
    /// 인게임 HUD 전체를 관리하는 진입점.
    /// 현재는 하단 스킬 슬롯 초기화와 자리 배치용으로 사용한다.
    /// 실제 게임 데이터 연결은 나중에 단계적으로 붙인다.
    /// </summary>
    public sealed class InGameHudUI : MonoBehaviour
    {
        [Header("하단 스킬 슬롯")]
        [SerializeField, Tooltip("하단 스킬 슬롯 8칸")]
        private HudSkillSlotUI[] skill_Slots;

        [Header("플레이스홀더 아이콘")]
        [SerializeField, Tooltip("빈 슬롯 또는 미구현 슬롯 기본 아이콘")]
        private Sprite placeholder_Icon;

        [SerializeField, Tooltip("지원 궁극기 슬롯 기본 아이콘")]
        private Sprite support_Ultimate_Placeholder_Icon;

        [SerializeField, Tooltip("궁극기 슬롯 기본 아이콘")]
        private Sprite ultimate_Placeholder_Icon;

        private void Start()
        {
            InitializeSkillSlots();
        }

        /// <summary>
        /// 현재 프로토타입 기준으로 슬롯을 초기화한다.
        /// 뒤 2칸은 지원 궁극기 / 궁극기 자리만 먼저 확보한다.
        /// </summary>
        private void InitializeSkillSlots()
        {
            if (skill_Slots == null || skill_Slots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skill_Slots.Length; i++)
            {
                if (skill_Slots[i] == null)
                {
                    continue;
                }

                skill_Slots[i].SetEmpty();
            }

            if (skill_Slots.Length >= 7 && skill_Slots[6] != null)
            {
                skill_Slots[6].SetPlaceholder(
                    support_Ultimate_Placeholder_Icon != null ? support_Ultimate_Placeholder_Icon : placeholder_Icon,
                    "R"
                );
            }

            if (skill_Slots.Length >= 8 && skill_Slots[7] != null)
            {
                skill_Slots[7].SetPlaceholder(
                    ultimate_Placeholder_Icon != null ? ultimate_Placeholder_Icon : placeholder_Icon,
                    "T"
                );
            }
        }

        /// <summary>
        /// 일반 스킬 슬롯에 실제 아이콘을 연결할 때 사용.
        /// </summary>
        public void SetSkillSlot(int slot_Index, Sprite icon_Sprite, string key_Label = "", string level_Label = "")
        {
            if (skill_Slots == null) return;
            if (slot_Index < 0 || slot_Index >= skill_Slots.Length) return;
            if (skill_Slots[slot_Index] == null) return;

            skill_Slots[slot_Index].SetSkill(icon_Sprite, key_Label, level_Label);
        }

        /// <summary>
        /// 나중에 궁극기 구현 시 이 함수로 교체 연결.
        /// </summary>
        public void SetUltimateReady(int slot_Index, Sprite icon_Sprite, string key_Label)
        {
            if (skill_Slots == null) return;
            if (slot_Index < 0 || slot_Index >= skill_Slots.Length) return;
            if (skill_Slots[slot_Index] == null) return;

            skill_Slots[slot_Index].SetSkill(icon_Sprite, key_Label);
        }
    }
}