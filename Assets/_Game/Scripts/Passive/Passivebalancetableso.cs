// ──────────────────────────────────────────────
// PassiveBalanceTableSO.cs
// 패시브 8종의 밸런스 수치를 한 곳에서 관리하는 SO
//
// 사용법:
//   Project 우클릭 → Create → Game → Skill → PassiveBalanceTable
//   프로젝트에 1개만 만들고, PlayerSkillLoadout 인스펙터에 할당
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace _Game.Skills
{
    /// <summary>
    /// 패시브 하나의 밸런스 수치 (Inspector에 표시)
    /// </summary>
    [System.Serializable]
    public struct PassiveBalanceEntry
    {
        [Tooltip("대상 능력치 종류")]
        public PassiveStatType statType;

        [Tooltip("1레벨 기본값")]
        public float baseValue;

        [Tooltip("레벨당 증가량")]
        public float valuePerLevel;
    }

    [CreateAssetMenu(
        fileName = "PassiveBalanceTable",
        menuName = "혼령검/Passive/PassiveBalanceTable",
        order = 1)]
    public class PassiveBalanceTableSO : ScriptableObject
    {
        [Header("=== 패시브 밸런스 테이블 ===")]
        [Tooltip("패시브 종류별 수치 설정 (8종)")]
        [SerializeField]
        private PassiveBalanceEntry[] entries = new PassiveBalanceEntry[]
        {
            // ── 퍼센트형 ────────────────────────
            new() { statType = PassiveStatType.AttackPowerPercent,   baseValue = 10f,  valuePerLevel = 10f },
            new() { statType = PassiveStatType.PickupRangePercent,   baseValue = 10f,  valuePerLevel = 10f },
            new() { statType = PassiveStatType.MoveSpeedPercent,     baseValue = 5f,   valuePerLevel = 5f  },
            new() { statType = PassiveStatType.DefensePercent,       baseValue = 10f,  valuePerLevel = 10f },
            new() { statType = PassiveStatType.SkillHastePercent, baseValue = 10f,  valuePerLevel = 10f },
            new() { statType = PassiveStatType.SkillAreaPercent,      baseValue = 10f,  valuePerLevel = 10f },
            new() { statType = PassiveStatType.ExpGainPercent,       baseValue = 10f,  valuePerLevel = 10f },

            // ── 정수형 ─────────────────────────
            new() { statType = PassiveStatType.MaxHpFlat,            baseValue = 50f,  valuePerLevel = 50f },
        };

        // ── 런타임 캐시 (Dictionary) ───────────

        private Dictionary<PassiveStatType, PassiveBalanceEntry> _cache;

        /// <summary>
        /// 캐시를 빌드한다. 첫 조회 시 자동 호출.
        /// </summary>
        private void BuildCache()
        {
            _cache = new Dictionary<PassiveStatType, PassiveBalanceEntry>(entries.Length);

            foreach (var entry in entries)
            {
                if (entry.statType == PassiveStatType.None)
                    continue;

                // 중복 키 방지 (Inspector에서 실수로 같은 타입 두 번 넣었을 때)
                if (!_cache.ContainsKey(entry.statType))
                    _cache[entry.statType] = entry;
            }
        }

        // ── 외부 API ──────────────────────────

        /// <summary>
        /// 지정 패시브 타입의 특정 레벨 수치를 반환한다.
        /// 테이블에 없는 타입이면 0f.
        /// </summary>
        public float GetValueAtLevel(PassiveStatType statType, int level)
        {
            if (_cache == null)
                BuildCache();

            if (!_cache.TryGetValue(statType, out var entry))
                return 0f;

            if (level < 1)
                level = 1;

            return entry.baseValue + entry.valuePerLevel * (level - 1);
        }

        /// <summary>
        /// 특정 패시브 타입의 밸런스 엔트리를 반환한다.
        /// 존재하지 않으면 default.
        /// </summary>
        public PassiveBalanceEntry GetEntry(PassiveStatType statType)
        {
            if (_cache == null)
                BuildCache();

            return _cache.TryGetValue(statType, out var entry) ? entry : default;
        }

        /// <summary>
        /// Inspector에서 값 변경 시 캐시를 무효화한다.
        /// </summary>
        private void OnValidate()
        {
            _cache = null;
        }
    }
}