using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "혼령검/메타/캐릭터 강화 트리", fileName = "CharacterUpgradeTree_")]
public sealed class CharacterUpgradeTreeSO : ScriptableObject
{
    [Header("트리 식별")]
    [Tooltip("디버그/확인용 이름입니다.")]
    [SerializeField] private string displayNameKr;

    [Tooltip("노드 목록입니다. UI Toolkit에서는 gridPosition을 사용해 배치합니다.")]
    [SerializeField] private List<CharacterUpgradeNodeData2D> nodes = new List<CharacterUpgradeNodeData2D>(16);

    private static readonly Dictionary<string, CharacterUpgradeTreeSO> RuntimeFallbackCache = new Dictionary<string, CharacterUpgradeTreeSO>(16);

    public string DisplayNameKr => displayNameKr;
    public IReadOnlyList<CharacterUpgradeNodeData2D> Nodes => nodes;

    public bool TryFindNode(string nodeId, out CharacterUpgradeNodeData2D found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(nodeId) || nodes == null) return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            CharacterUpgradeNodeData2D node = nodes[i];
            if (node == null) continue;
            if (node.nodeId != nodeId) continue;
            found = node;
            return true;
        }

        return false;
    }

    public static CharacterUpgradeTreeSO GetOrCreateRuntimeFallback(CharacterDefinitionSO definition)
    {
        if (definition == null)
            return null;

        string key = string.IsNullOrWhiteSpace(definition.CharacterId) ? definition.name : definition.CharacterId;
        if (RuntimeFallbackCache.TryGetValue(key, out CharacterUpgradeTreeSO cached) && cached != null)
            return cached;

        CharacterUpgradeTreeSO tree = CreateInstance<CharacterUpgradeTreeSO>();
        tree.hideFlags = HideFlags.DontSave;
        tree.displayNameKr = string.IsNullOrWhiteSpace(definition.DisplayName)
            ? "기본 강화 트리"
            : $"{definition.DisplayName} 강화 트리";
        tree.nodes = BuildDefaultNodes(definition);

        RuntimeFallbackCache[key] = tree;
        return tree;
    }


    public static void OverwriteWithRuntimeDefault(CharacterUpgradeTreeSO target, CharacterDefinitionSO definition)
    {
        if (target == null || definition == null) return;

        target.displayNameKr = string.IsNullOrWhiteSpace(definition.DisplayName)
            ? "기본 강화 트리"
            : $"{definition.DisplayName} 강화 트리";
        target.nodes = BuildDefaultNodes(definition);
    }

    private static List<CharacterUpgradeNodeData2D> BuildDefaultNodes(CharacterDefinitionSO definition)
    {
        string basicName = string.IsNullOrWhiteSpace(definition.BasicSkillId) ? "기본 스킬" : definition.BasicSkillId;
        string ultimateName = string.IsNullOrWhiteSpace(definition.UltimateSkillId) ? "궁극기" : definition.UltimateSkillId;

        List<CharacterUpgradeNodeData2D> list = new List<CharacterUpgradeNodeData2D>(10)
        {
            CreateNode($"{definition.CharacterId}_atk_training", "공격 수련", "독립 모드의 핵심이 되는 기본 화력입니다.", CharacterUpgradeBranchType2D.Core, new Vector2Int(0, 0), 3, 120, 80, 1, null,
                Modifier(OutgameModifierKind2D.AttackPowerPercent, 3f)),

            CreateNode($"{definition.CharacterId}_guard_training", "방어 수련", "유틸 대신 버티는 힘을 올립니다.", CharacterUpgradeBranchType2D.Core, new Vector2Int(0, 2), 3, 120, 80, 1, null,
                Modifier(OutgameModifierKind2D.DefensePercent, 3f)),

            CreateNode($"{definition.CharacterId}_body_training", "체력 수련", "최대 체력을 올려 독립 전투 안정성을 확보합니다.", CharacterUpgradeBranchType2D.Core, new Vector2Int(0, 4), 3, 120, 80, 1, null,
                Modifier(OutgameModifierKind2D.MaxHpFlat, 10f)),

            CreateNode($"{definition.CharacterId}_basic_damage", $"{basicName} 위력", "기본 스킬의 핵심 피해량을 밀어줍니다.", CharacterUpgradeBranchType2D.BasicSkill, new Vector2Int(2, 0), 3, 180, 100, 1,
                new List<string> { $"{definition.CharacterId}_atk_training" }, Modifier(OutgameModifierKind2D.BasicSkillDamagePercent, 6f)),

            CreateNode($"{definition.CharacterId}_basic_cooldown", $"{basicName} 숙련", "기본 스킬의 회전율을 강화합니다.", CharacterUpgradeBranchType2D.BasicSkill, new Vector2Int(4, 0), 2, 240, 120, 5,
                new List<string> { $"{definition.CharacterId}_basic_damage" }, Modifier(OutgameModifierKind2D.BasicSkillCooldownPercent, 5f)),

            CreateNode($"{definition.CharacterId}_ultimate_damage", $"{ultimateName} 위력", "궁극기의 피니시 성능을 강화합니다.", CharacterUpgradeBranchType2D.Ultimate, new Vector2Int(2, 2), 3, 220, 120, 1,
                new List<string> { $"{definition.CharacterId}_guard_training" }, Modifier(OutgameModifierKind2D.UltimateDamagePercent, 7f)),

            CreateNode($"{definition.CharacterId}_ultimate_cooldown", $"{ultimateName} 숙련", "궁극기 재사용 속도를 끌어올립니다.", CharacterUpgradeBranchType2D.Ultimate, new Vector2Int(4, 2), 2, 260, 140, 10,
                new List<string> { $"{definition.CharacterId}_ultimate_damage" }, Modifier(OutgameModifierKind2D.UltimateCooldownPercent, 6f)),

            CreateNode($"{definition.CharacterId}_passive_focus", "패시브 집중", "패시브 효율을 안정적으로 끌어올립니다.", CharacterUpgradeBranchType2D.Passive, new Vector2Int(2, 4), 3, 160, 100, 1,
                new List<string> { $"{definition.CharacterId}_body_training" }, Modifier(OutgameModifierKind2D.PassivePowerPercent, 5f)),

            CreateNode($"{definition.CharacterId}_independent_will", "독립의 의지", "혼자서도 완결되는 운영 철학을 전투 수치로 고정합니다.", CharacterUpgradeBranchType2D.Passive, new Vector2Int(4, 4), 1, 400, 0, 15,
                new List<string> { $"{definition.CharacterId}_passive_focus" },
                Modifier(OutgameModifierKind2D.AttackPowerPercent, 4f),
                Modifier(OutgameModifierKind2D.DefensePercent, 4f),
                Modifier(OutgameModifierKind2D.MaxHpFlat, 15f)),

            CreateNode($"{definition.CharacterId}_final_resolve", "최종 결의", "공격/방어/체력을 한 번 더 끌어올리는 마감 노드입니다.", CharacterUpgradeBranchType2D.Core, new Vector2Int(6, 2), 1, 700, 0, 20,
                new List<string>
                {
                    $"{definition.CharacterId}_basic_cooldown",
                    $"{definition.CharacterId}_ultimate_cooldown",
                    $"{definition.CharacterId}_independent_will"
                },
                Modifier(OutgameModifierKind2D.AttackPowerPercent, 8f),
                Modifier(OutgameModifierKind2D.DefensePercent, 8f),
                Modifier(OutgameModifierKind2D.MaxHpFlat, 30f))
        };

        return list;
    }

    private static CharacterUpgradeNodeData2D CreateNode(
        string nodeId,
        string title,
        string description,
        CharacterUpgradeBranchType2D branch,
        Vector2Int gridPosition,
        int maxRank,
        int baseCost,
        int costStep,
        int requiredCharacterLevel,
        List<string> prerequisites,
        params CharacterUpgradeModifierEntry2D[] modifiers)
    {
        CharacterUpgradeNodeData2D node = new CharacterUpgradeNodeData2D
        {
            nodeId = nodeId,
            titleKr = title,
            descriptionKr = description,
            branch = branch,
            gridPosition = gridPosition,
            maxRank = maxRank,
            baseCostNyang = baseCost,
            costStepNyang = costStep,
            requiredCharacterLevel = requiredCharacterLevel,
            prerequisiteNodeIds = prerequisites ?? new List<string>(0),
            modifiers = modifiers != null ? new List<CharacterUpgradeModifierEntry2D>(modifiers) : new List<CharacterUpgradeModifierEntry2D>(0)
        };
        return node;
    }

    private static CharacterUpgradeModifierEntry2D Modifier(OutgameModifierKind2D kind, float valuePerRank)
    {
        return new CharacterUpgradeModifierEntry2D
        {
            kind = kind,
            valuePerRank = valuePerRank
        };
    }
}

[Serializable]
public sealed class CharacterUpgradeNodeData2D
{
    [Tooltip("저장/구매 판정용 고유 ID입니다.")]
    public string nodeId;

    [Tooltip("UI 제목입니다.")]
    public string titleKr;

    [Tooltip("UI 상세 설명입니다.")]
    [TextArea]
    public string descriptionKr;

    [Tooltip("UI 분기 구분입니다.")]
    public CharacterUpgradeBranchType2D branch = CharacterUpgradeBranchType2D.Core;

    [Tooltip("UI Toolkit 트리 배치 좌표입니다. x=깊이, y=줄입니다.")]
    public Vector2Int gridPosition = Vector2Int.zero;

    [Tooltip("한 노드를 몇 번까지 올릴지 설정합니다.")]
    [Min(1)]
    public int maxRank = 1;

    [Tooltip("1랭크 구매 비용입니다.")]
    [Min(0)]
    public int baseCostNyang = 100;

    [Tooltip("랭크가 오를수록 추가되는 비용입니다.")]
    [Min(0)]
    public int costStepNyang = 0;

    [Tooltip("이 레벨 이상이어야 구매할 수 있습니다.")]
    [Min(1)]
    public int requiredCharacterLevel = 1;

    [Tooltip("선행 구매가 필요한 노드 ID 목록입니다.")]
    public List<string> prerequisiteNodeIds = new List<string>(0);

    [Tooltip("랭크마다 누적 적용할 보정값입니다.")]
    public List<CharacterUpgradeModifierEntry2D> modifiers = new List<CharacterUpgradeModifierEntry2D>(1);

    public int GetCostForNextRank(int currentRank)
    {
        int nextRank = Mathf.Clamp(currentRank + 1, 1, maxRank);
        return Mathf.Max(0, baseCostNyang + ((nextRank - 1) * costStepNyang));
    }
}

[Serializable]
public struct CharacterUpgradeModifierEntry2D
{
    [Tooltip("올릴 능력치 종류입니다.")]
    public OutgameModifierKind2D kind;

    [Tooltip("랭크 1개당 더할 값입니다.")]
    public float valuePerRank;
}
