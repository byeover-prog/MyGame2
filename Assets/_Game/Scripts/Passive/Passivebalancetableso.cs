using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Skills
{
    [CreateAssetMenu(
        fileName = "PassiveBalanceTable",
        menuName = "혼령검/Passive/PassiveBalanceTable",
        order = 1)]
    public class PassiveBalanceTableSO : ScriptableObject
    {
        [Header("패시브 밸런스 테이블")]
        [Tooltip("패시브 종류별 수치 설정")]
        [SerializeField]
        private PassiveBalanceEntry[] entries = new PassiveBalanceEntry[]
        {
            new() { statType = PassiveStatType.AttackPowerPercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.PickupRangePercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.MoveSpeedPercent, baseValue = 5f, valuePerLevel = 5f },
            new() { statType = PassiveStatType.DefensePercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.SkillHastePercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.SkillAreaPercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.ExpGainPercent, baseValue = 10f, valuePerLevel = 10f },
            new() { statType = PassiveStatType.MaxHpFlat, baseValue = 50f, valuePerLevel = 50f },
        };

        private Dictionary<PassiveStatType, PassiveBalanceEntry> _cache;

        public float GetValueAtLevel(PassiveStatType statType, int level)
        {
            if (_cache == null)
                BuildCache();

            if (!_cache.TryGetValue(statType, out PassiveBalanceEntry entry))
                return 0f;

            if (level < 1)
                level = 1;

            return entry.baseValue + entry.valuePerLevel * (level - 1);
        }

        public PassiveBalanceEntry GetEntry(PassiveStatType statType)
        {
            if (_cache == null)
                BuildCache();

            return _cache.TryGetValue(statType, out PassiveBalanceEntry entry) ? entry : default;
        }

        private void BuildCache()
        {
            _cache = new Dictionary<PassiveStatType, PassiveBalanceEntry>(entries.Length);

            foreach (PassiveBalanceEntry entry in entries)
            {
                if (entry.statType == PassiveStatType.None)
                    continue;

                if (!_cache.ContainsKey(entry.statType))
                    _cache[entry.statType] = entry;
            }
        }

        private void OnValidate()
        {
            _cache = null;
        }
    }

    [Serializable]
    public struct PassiveBalanceEntry
    {
        [Tooltip("대상 능력치 종류")]
        public PassiveStatType statType;

        [Tooltip("1레벨 기본값")]
        public float baseValue;

        [Tooltip("레벨당 증가량")]
        public float valuePerLevel;
    }
}
