// ──────────────────────────────────────────────
// PlayerSkillLoadout.cs
// 런타임 스킬 보관소 (매니저는 하나, 컨테이너는 둘)
//
// 구조:
//   Active Slots  [5] + activeById  (Dictionary)
//   Passive Slots [5] + passiveById (Dictionary)
//
// 패시브 밸런스 수치는 PassiveBalanceTableSO에서 읽어온다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Skills;

namespace _Game.Player
{
    public sealed class PlayerSkillLoadout : MonoBehaviour
    {
        // ── 슬롯 크기 ─────────────────────────────

        private const int ACTIVE_SLOT_COUNT  = 5;
        private const int PASSIVE_SLOT_COUNT = 5;

        // ── 밸런스 테이블 참조 ─────────────────────

        [Header("=== 밸런스 설정 ===")]

        [SerializeField, Tooltip("패시브 밸런스 테이블 SO (한 곳에서 수치 관리)")]
        private PassiveBalanceTableSO balanceTable;

        // ── 런타임 컨테이너 ────────────────────────

        /// <summary>액티브 슬롯 배열</summary>
        private RuntimeSkillState[] activeSlots;

        /// <summary>패시브 슬롯 배열</summary>
        private RuntimeSkillState[] passiveSlots;

        /// <summary>액티브 스킬ID → 런타임 상태 (O(1) 조회)</summary>
        private readonly Dictionary<string, RuntimeSkillState> activeById = new();

        /// <summary>패시브 스킬ID → 런타임 상태 (O(1) 조회)</summary>
        private readonly Dictionary<string, RuntimeSkillState> passiveById = new();

        // ── 초기화 ─────────────────────────────────

        private void Awake()
        {
            activeSlots  = new RuntimeSkillState[ACTIVE_SLOT_COUNT];
            passiveSlots = new RuntimeSkillState[PASSIVE_SLOT_COUNT];

            if (balanceTable == null)
                Debug.LogWarning("[PlayerSkillLoadout] balanceTable이 할당되지 않았습니다!");
        }

        // ════════════════════════════════════════════
        //  외부 API: 스킬 추가 / 레벨업 / 조회
        // ════════════════════════════════════════════

        /// <summary>
        /// 신규 스킬을 획득한다.
        /// SkillType에 따라 올바른 컨테이너에 자동 라우팅된다.
        /// </summary>
        public bool TryAddSkill(SkillDefinitionSO skill)
        {
            if (skill == null)
                return false;

            return skill.SkillType switch
            {
                SkillType.Active  => TryAddToContainer(skill, activeSlots, activeById),
                SkillType.Passive => TryAddToContainer(skill, passiveSlots, passiveById),
                _ => false
            };
        }

        /// <summary>
        /// 보유 중인 스킬의 레벨을 1 올린다.
        /// </summary>
        public bool TryUpgradeSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            RuntimeSkillState state = GetSkill(skillId);

            if (state == null)
                return false;

            return state.TryLevelUp();
        }

        /// <summary>
        /// 스킬 ID로 런타임 상태를 조회한다.
        /// </summary>
        public RuntimeSkillState GetSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return null;

            if (activeById.TryGetValue(skillId, out var a))
                return a;

            if (passiveById.TryGetValue(skillId, out var p))
                return p;

            return null;
        }

        /// <summary>해당 스킬을 이미 보유 중인지 확인</summary>
        public bool HasSkill(string skillId)
        {
            return activeById.ContainsKey(skillId)
                || passiveById.ContainsKey(skillId);
        }

        /// <summary>액티브 슬롯 배열 참조</summary>
        public RuntimeSkillState[] GetActiveSlots()  => activeSlots;

        /// <summary>패시브 슬롯 배열 참조</summary>
        public RuntimeSkillState[] GetPassiveSlots() => passiveSlots;

        // ════════════════════════════════════════════
        //  외부 API: 카드 후보 판정
        // ════════════════════════════════════════════

        /// <summary>
        /// 이 스킬이 레벨업 카드 후보로 나올 수 있는지 판정한다.
        /// 미보유(신규 획득 가능) 또는 보유 중이지만 레벨업 가능하면 true.
        /// 슬롯 가득 참 + 미보유이면 false.
        /// </summary>
        public bool CanAppearAsCard(SkillDefinitionSO skill)
        {
            if (skill == null)
                return false;

            RuntimeSkillState state = GetSkill(skill.SkillId);

            // 미보유 → 빈 슬롯이 있는지 확인
            if (state == null)
            {
                return skill.SkillType switch
                {
                    SkillType.Active  => HasEmptySlot(activeSlots),
                    SkillType.Passive => HasEmptySlot(passiveSlots),
                    _ => false
                };
            }

            // 보유 중 → 레벨업 가능한지
            return state.CanLevelUp();
        }

        /// <summary>
        /// 카드에 표시할 설명 텍스트를 생성한다.
        /// SO에 레벨별 설명이 있으면 그것을 사용, 없으면 기본 텍스트.
        /// </summary>
        public string BuildCardDescription(SkillDefinitionSO skill)
        {
            if (skill == null)
                return string.Empty;

            RuntimeSkillState state = GetSkill(skill.SkillId);

            // 미보유 → 다음 레벨 = 1
            int nextLevel = state == null ? 1 : state.Level + 1;

            // SO에 레벨별 설명이 있으면 우선 사용
            string soDesc = skill.GetDescriptionForLevel(nextLevel);
            if (!string.IsNullOrWhiteSpace(soDesc))
                return soDesc;

            // SO에 설명 없으면 기본 텍스트
            if (state == null)
                return "새로 획득";

            return $"Lv.{state.Level} → Lv.{nextLevel}";
        }

        // ════════════════════════════════════════════
        //  외부 API: 패시브 스탯 계산
        // ════════════════════════════════════════════

        /// <summary>
        /// 모든 패시브 슬롯을 순회하여 합산된 보너스 스탯을 반환한다.
        /// 수치는 PassiveBalanceTableSO에서 읽어온다.
        /// </summary>
        public PlayerStatSnapshot BuildStatSnapshot()
        {
            PlayerStatSnapshot snapshot = new PlayerStatSnapshot();

            if (balanceTable == null)
            {
                Debug.LogWarning("[PlayerSkillLoadout] balanceTable 미할당 → 스냅샷 0 반환");
                return snapshot;
            }

            for (int i = 0; i < passiveSlots.Length; i++)
            {
                RuntimeSkillState state = passiveSlots[i];

                if (state == null || state.Definition == null)
                    continue;

                ApplyPassive(ref snapshot, state);
            }

            return snapshot;
        }

        // ════════════════════════════════════════════
        //  내부 공통 로직
        // ════════════════════════════════════════════

        /// <summary>
        /// 스킬을 지정된 컨테이너에 추가한다.
        /// </summary>
        private bool TryAddToContainer(
            SkillDefinitionSO skill,
            RuntimeSkillState[] slots,
            Dictionary<string, RuntimeSkillState> byId)
        {
            if (byId.ContainsKey(skill.SkillId))
                return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    var state = new RuntimeSkillState(skill);

                    slots[i] = state;
                    byId.Add(skill.SkillId, state);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 슬롯 배열에 빈 칸이 있는지 확인한다.
        /// </summary>
        private bool HasEmptySlot(RuntimeSkillState[] slots)
        {
            if (slots == null) return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 패시브 하나의 수치를 스냅샷에 합산한다.
        /// 수치는 balanceTable에서 가져온다.
        /// </summary>
        private void ApplyPassive(ref PlayerStatSnapshot snapshot, RuntimeSkillState state)
        {
            PassiveStatType statType = state.Definition.PassiveStatType;
            float value = balanceTable.GetValueAtLevel(statType, state.Level);

            switch (statType)
            {
                case PassiveStatType.AttackPowerPercent:
                    snapshot.AttackPowerPercent += value;
                    break;

                case PassiveStatType.PickupRangePercent:
                    snapshot.PickupRangePercent += value;
                    break;

                case PassiveStatType.MoveSpeedPercent:
                    snapshot.MoveSpeedPercent += value;
                    break;

                case PassiveStatType.DefensePercent:
                    snapshot.DefensePercent += value;
                    break;

                case PassiveStatType.MaxHpFlat:
                    snapshot.MaxHpFlat += Mathf.RoundToInt(value);
                    break;

                case PassiveStatType.ElementDamagePercent:
                    snapshot.ElementDamagePercent += value;
                    break;

                case PassiveStatType.GoldGainPercent:
                    snapshot.GoldGainPercent += value;
                    break;

                case PassiveStatType.ExpGainPercent:
                    snapshot.ExpGainPercent += value;
                    break;
            }
        }
    }
}