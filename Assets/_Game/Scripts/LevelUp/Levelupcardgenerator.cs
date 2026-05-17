using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    public sealed class LevelUpCardGenerator : MonoBehaviour
    {
        [Header("=== Catalogs ===")]
        [SerializeField] private SkillCatalogSO skillCatalog;
        [SerializeField] private CommonSkillCatalogSO commonSkillCatalog;
        [SerializeField] private CharacterSkillDatabaseSO characterSkillDatabase;

        [Header("=== Card Count ===")]
        [SerializeField, Min(3)] private int totalCardCount = 5;

        [Header("=== Exclusive Skill Weight ===")]
        [SerializeField, Range(1f, 5f)] private float exclusiveWeightMultiplier = 1.5f;

        [Header("=== Exclusive Skill Guarantee ===")]
        [SerializeField, Min(1)] private int guaranteeStartLevel = 2;
        [SerializeField, Range(0f, 1f)] private float rerollPityPerRoll = 0.15f;

        [Header("=== Passive Slot Rules ===")]
        [SerializeField, Min(1)] private int passiveSingleSlotUntilLevel = 5;

        [Header("=== Repeat Penalty ===")]
        [SerializeField, Range(0f, 1f)] private float recentPenalty1 = 0.5f;
        [SerializeField, Range(0f, 1f)] private float recentPenalty2 = 0.75f;

        [Header("=== Fallback Cards ===")]
        [SerializeField] private int fallbackHealAmount = 30;
        [SerializeField] private int fallbackGoldAmount = 200;
        [SerializeField] private float fallbackInvincibleDuration = 5f;
        [SerializeField] private int fallbackBonusExpAmount = 50;

        private int _levelUpCount;
        private bool _exclusiveEverOffered;
        private int _currentRerollCount;
        private float _currentPityBonus;

        private readonly Dictionary<string, int> _recentHistory = new(32);
        private readonly List<WeightedSkill> _skillCandidates = new(32);
        private readonly List<SkillDefinitionSO> _passiveCandidates = new(16);

        private struct WeightedSkill
        {
            public SkillDefinitionSO definition;
            public CharacterSkillDefinitionSO characterDefinition;
            public GameObject prefab;
            public float weight;
            public bool isExclusive;

            public string SkillId => isExclusive
                ? (characterDefinition != null ? characterDefinition.SkillId : string.Empty)
                : (definition != null ? definition.SkillId : string.Empty);

            public string DisplayName => isExclusive
                ? (characterDefinition != null ? characterDefinition.DisplayName : string.Empty)
                : (definition != null ? definition.DisplayName : string.Empty);
        }

        public void ResetRunState()
        {
            _levelUpCount = 0;
            _exclusiveEverOffered = false;
            _currentRerollCount = 0;
            _currentPityBonus = 0f;
            _recentHistory.Clear();
        }

        public void NotifyReroll()
        {
            _currentRerollCount++;
            _currentPityBonus += rerollPityPerRoll;
        }

        public void NotifyLevelUpClosed()
        {
            _currentRerollCount = 0;
            _currentPityBonus = 0f;
        }

        public List<LevelUpCardData> Generate(PlayerSkillLoadout loadout)
        {
            List<LevelUpCardData> result = new List<LevelUpCardData>(totalCardCount);

            if (skillCatalog == null || loadout == null)
            {
                GameLogger.LogWarning("[CardGen] skillCatalog or loadout is missing. Filling fallback cards.", this);
                FillWithFallbackCards(result);
                return result;
            }

            if (_currentRerollCount == 0)
                _levelUpCount++;

            int passiveSlotCount = _levelUpCount <= passiveSingleSlotUntilLevel ? 1 : 2;
            int skillSlotCount = totalCardCount - passiveSlotCount;

            BuildSkillCandidates(loadout);
            BuildPassiveCandidates(loadout);

            GameLogger.Log(
                $"[CardGen] Generate Lv#{_levelUpCount} | skillCandidates={_skillCandidates.Count} passiveCandidates={_passiveCandidates.Count} | skillSlots={skillSlotCount} passiveSlots={passiveSlotCount}",
                this);

            if (_skillCandidates.Count == 0 && _passiveCandidates.Count == 0)
            {
                FillWithFallbackCards(result);
                return result;
            }

            HashSet<string> usedIds = new HashSet<string>();
            bool needGuarantee = NeedExclusiveGuarantee();

            FillSkillSlots(result, loadout, usedIds, skillSlotCount, needGuarantee);
            FillPassiveSlots(result, loadout, usedIds, passiveSlotCount);

            if (result.Count < totalCardCount)
                FillRemainingWithFallbackCards(result);

            UpdateRecentHistory(result);

            return result;
        }

        private void BuildSkillCandidates(PlayerSkillLoadout loadout)
        {
            _skillCandidates.Clear();

            IReadOnlyList<SkillDefinitionSO> actives = skillCatalog.GetByType(SkillType.Active);
            if (actives != null)
            {
                for (int i = 0; i < actives.Count; i++)
                {
                    SkillDefinitionSO skill = actives[i];
                    if (!IsValidCandidate(skill, loadout)) continue;

                    if (commonSkillCatalog != null)
                    {
                        if (!commonSkillCatalog.TryResolve(skill, out CommonSkillConfigSO config)) continue;
                        if (config == null || config.weaponPrefab == null) continue;
                    }

                    _skillCandidates.Add(new WeightedSkill
                    {
                        definition = skill,
                        characterDefinition = null,
                        prefab = null,
                        weight = GetRecentPenalty(skill.SkillId),
                        isExclusive = false
                    });
                }
            }

            RunSetup runSetup = RunSetupHolder.GetOrCreateFromCurrentState();
            string mainId = runSetup != null ? runSetup.mainId : string.Empty;
            string support1Id = runSetup != null ? runSetup.support1Id : string.Empty;
            string support2Id = runSetup != null ? runSetup.support2Id : string.Empty;
            int databaseSetCount = characterSkillDatabase != null && characterSkillDatabase.CharacterSkillSets != null
                ? characterSkillDatabase.CharacterSkillSets.Count
                : 0;

            GameLogger.Log(
                $"[CardGen] party exclusive skill lookup | main='{mainId}' support1='{support1Id}' support2='{support2Id}' | databaseSets={databaseSetCount}",
                this);

            AppendExclusiveSkillsOf(mainId, loadout, "Main");
            AppendExclusiveSkillsOf(support1Id, loadout, "Support1");
            AppendExclusiveSkillsOf(support2Id, loadout, "Support2");
        }

        private void BuildPassiveCandidates(PlayerSkillLoadout loadout)
        {
            _passiveCandidates.Clear();

            IReadOnlyList<SkillDefinitionSO> passives = skillCatalog.GetByType(SkillType.Passive);
            if (passives == null) return;

            for (int i = 0; i < passives.Count; i++)
            {
                SkillDefinitionSO skill = passives[i];
                if (!IsValidCandidate(skill, loadout)) continue;
                if (skill.PassiveStatType == PassiveStatType.None) continue;

                _passiveCandidates.Add(skill);
            }
        }

        private bool IsValidCandidate(SkillDefinitionSO skill, PlayerSkillLoadout loadout)
        {
            if (skill == null) return false;
            if (string.IsNullOrWhiteSpace(skill.SkillId)) return false;
            if (string.IsNullOrWhiteSpace(skill.DisplayName)) return false;
            return loadout.CanAppearAsCard(skill);
        }

        private bool IsValidCandidate(CharacterSkillDefinitionSO skill, PlayerSkillLoadout loadout)
        {
            if (skill == null) return false;
            if (string.IsNullOrWhiteSpace(skill.SkillId)) return false;
            if (string.IsNullOrWhiteSpace(skill.DisplayName)) return false;
            if (skill.WeaponPrefab == null) return false;
            return loadout.CanAppearAsCard(skill);
        }

        private void FillSkillSlots(
            List<LevelUpCardData> result,
            PlayerSkillLoadout loadout,
            HashSet<string> usedIds,
            int count,
            bool needGuarantee)
        {
            if (_skillCandidates.Count == 0) return;

            if (needGuarantee)
            {
                for (int i = 0; i < _skillCandidates.Count; i++)
                {
                    if (!_skillCandidates[i].isExclusive) continue;

                    WeightedSkill candidate = _skillCandidates[i];
                    string skillId = candidate.SkillId;
                    if (string.IsNullOrWhiteSpace(skillId) || !usedIds.Add(skillId)) continue;

                    result.Add(MakeCardData(candidate, loadout));
                    _exclusiveEverOffered = true;
                    count--;

                    GameLogger.Log($"[CardGen] exclusive guarantee: {candidate.DisplayName}", this);
                    break;
                }
            }

            int safety = 256;
            while (count > 0 && safety-- > 0)
            {
                int picked = WeightedRandomPick(usedIds);
                if (picked < 0) break;

                WeightedSkill candidate = _skillCandidates[picked];
                string skillId = candidate.SkillId;
                if (string.IsNullOrWhiteSpace(skillId) || !usedIds.Add(skillId)) continue;

                result.Add(MakeCardData(candidate, loadout));
                count--;

                if (candidate.isExclusive)
                    _exclusiveEverOffered = true;
            }
        }

        private void FillPassiveSlots(
            List<LevelUpCardData> result,
            PlayerSkillLoadout loadout,
            HashSet<string> usedIds,
            int count)
        {
            if (_passiveCandidates.Count == 0) return;

            List<SkillDefinitionSO> shuffled = new List<SkillDefinitionSO>(_passiveCandidates);
            Shuffle(shuffled);

            for (int i = 0; i < shuffled.Count && count > 0; i++)
            {
                SkillDefinitionSO skill = shuffled[i];
                if (skill == null) continue;
                if (!usedIds.Add(skill.SkillId)) continue;

                string desc = loadout.BuildCardDescription(skill);
                LevelUpCardData card = LevelUpCardData.CreateSkillCard(skill, desc);

                RuntimeSkillState state = loadout.GetSkill(skill.SkillId);
                card.CurrentLevel = state != null ? state.Level : 0;
                card.NextLevel = card.CurrentLevel + 1;

                result.Add(card);
                count--;
            }
        }

        private LevelUpCardData MakeCardData(WeightedSkill weightedSkill, PlayerSkillLoadout loadout)
        {
            string skillId = weightedSkill.SkillId;
            RuntimeSkillState state = loadout.GetSkill(skillId);
            int currentLevel = state != null ? state.Level : 0;

            LevelUpCardData card;
            if (weightedSkill.isExclusive)
            {
                CharacterSkillDefinitionSO definition = weightedSkill.characterDefinition;
                string desc = loadout.BuildCardDescription(definition);
                card = LevelUpCardData.CreateCharacterSkillCard(definition, desc);
                card.AddInfo = definition != null ? definition.GetAddInfoForLevel(currentLevel + 1) : string.Empty;
            }
            else
            {
                SkillDefinitionSO definition = weightedSkill.definition;
                string desc = loadout.BuildCardDescription(definition);
                card = LevelUpCardData.CreateSkillCard(definition, desc);
                card.AddInfo = definition != null ? definition.GetAddInfoForLevel(currentLevel + 1) : string.Empty;
            }

            card.CurrentLevel = currentLevel;
            card.NextLevel = currentLevel + 1;

            return card;
        }

        private int WeightedRandomPick(HashSet<string> usedIds)
        {
            float totalWeight = 0f;
            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (usedIds.Contains(_skillCandidates[i].SkillId)) continue;
                totalWeight += Mathf.Max(0f, _skillCandidates[i].weight);
            }

            if (totalWeight <= 0f) return -1;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;

            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (usedIds.Contains(_skillCandidates[i].SkillId)) continue;
                float weight = Mathf.Max(0f, _skillCandidates[i].weight);
                accumulated += weight;
                if (roll <= accumulated) return i;
            }

            for (int i = _skillCandidates.Count - 1; i >= 0; i--)
            {
                if (!usedIds.Contains(_skillCandidates[i].SkillId))
                    return i;
            }

            return -1;
        }

        private bool NeedExclusiveGuarantee()
        {
            if (_exclusiveEverOffered) return false;

            bool hasExclusive = false;
            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (_skillCandidates[i].isExclusive)
                {
                    hasExclusive = true;
                    break;
                }
            }

            return hasExclusive && _levelUpCount >= guaranteeStartLevel;
        }

        private float GetRecentPenalty(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId)) return 1f;
            if (_recentHistory.TryGetValue(skillId, out int lastSeen))
            {
                int gap = _levelUpCount - lastSeen;
                if (gap <= 1) return recentPenalty1;
                if (gap <= 2) return recentPenalty2;
            }

            return 1f;
        }

        private void UpdateRecentHistory(List<LevelUpCardData> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                string skillId = GetCardSkillId(cards[i]);
                if (string.IsNullOrWhiteSpace(skillId)) continue;

                _recentHistory[skillId] = _levelUpCount;
            }
        }

        private string GetCardSkillId(LevelUpCardData card)
        {
            if (card == null) return string.Empty;
            if (card.CharacterSkillDefinition != null) return card.CharacterSkillDefinition.SkillId;
            if (card.SkillDefinition != null) return card.SkillDefinition.SkillId;
            return string.Empty;
        }

        private void AppendExclusiveSkillsOf(string characterId, PlayerSkillLoadout loadout, string slotLabel)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return;

            if (characterSkillDatabase == null)
            {
                GameLogger.LogWarning("[CardGen] characterSkillDatabase is missing. Exclusive skills cannot be resolved.", this);
                return;
            }

            bool hasSet = characterSkillDatabase.TryGetSkillSet(characterId, out CharacterSkillSetSO activeSet);
            GameLogger.Log($"[CardGen]   [{slotLabel}] charId='{characterId}' setFound={hasSet}", this);

            if (!hasSet || activeSet == null) return;

            List<CharacterSkillDefinitionSO> skills = activeSet.GetValidSkills();
            for (int i = 0; i < skills.Count; i++)
            {
                CharacterSkillDefinitionSO skill = skills[i];
                if (!IsValidCandidate(skill, loadout)) continue;

                float baseWeight = exclusiveWeightMultiplier;
                float pity = 1f + _currentPityBonus;
                float penalty = GetRecentPenalty(skill.SkillId);

                _skillCandidates.Add(new WeightedSkill
                {
                    definition = null,
                    characterDefinition = skill,
                    prefab = skill.WeaponPrefab,
                    weight = baseWeight * pity * penalty,
                    isExclusive = true
                });
            }
        }

        private void FillWithFallbackCards(List<LevelUpCardData> result)
        {
            result.Clear();
            result.Add(LevelUpCardData.CreateHealCard(fallbackHealAmount));
            result.Add(LevelUpCardData.CreateGoldCard(fallbackGoldAmount));
            result.Add(LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration));
            result.Add(LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount));
        }

        private void FillRemainingWithFallbackCards(List<LevelUpCardData> result)
        {
            List<LevelUpCardData> pool = new List<LevelUpCardData>
            {
                LevelUpCardData.CreateHealCard(fallbackHealAmount),
                LevelUpCardData.CreateGoldCard(fallbackGoldAmount),
                LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration),
                LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount)
            };
            Shuffle(pool);

            for (int i = 0; i < pool.Count && result.Count < totalCardCount; i++)
                result.Add(pool[i]);
        }

        private void Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
