using System.Collections.Generic;
using UnityEngine;

public sealed class OutgameUpgradeService2D
{
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;
    private readonly CharacterProgressionService2D _progressionService;
    private readonly MetaWalletService2D _walletService;

    public OutgameUpgradeService2D(CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        _progressionService = new CharacterProgressionService2D(catalog, _saveManager);
        _walletService = new MetaWalletService2D(_saveManager);
        EnsureData();
    }

    public int Nyang => _walletService.Nyang;

    public CharacterUpgradeTreeSO GetTree(string characterId)
    {
        CharacterDefinitionSO definition = GetDefinition(characterId);
        if (definition == null) return null;
        return definition.UpgradeTree != null
            ? definition.UpgradeTree
            : CharacterUpgradeTreeSO.GetOrCreateRuntimeFallback(definition);
    }

    public int GetRank(string characterId, string nodeId)
    {
        CharacterUpgradeStateSaveData2D state = GetOrCreateState(characterId);
        return state != null ? state.GetRank(nodeId) : 0;
    }

    public int GetPurchasedNodeCount(string characterId)
    {
        CharacterUpgradeStateSaveData2D state = GetOrCreateState(characterId);
        return state != null && state.purchasedNodes != null ? state.purchasedNodes.Count : 0;
    }

    public int GetNextCost(string characterId, string nodeId)
    {
        CharacterUpgradeTreeSO tree = GetTree(characterId);
        if (tree == null || !tree.TryFindNode(nodeId, out CharacterUpgradeNodeData2D node) || node == null)
            return 0;

        int currentRank = GetRank(characterId, nodeId);
        if (currentRank >= node.maxRank) return 0;
        return node.GetCostForNextRank(currentRank);
    }

    public bool CanPurchase(string characterId, string nodeId, out string reason)
    {
        reason = string.Empty;

        CharacterDefinitionSO definition = GetDefinition(characterId);
        if (definition == null)
        {
            reason = "캐릭터 정의를 찾지 못했습니다.";
            return false;
        }

        if (!_progressionService.IsUnlocked(characterId))
        {
            reason = "아직 해금되지 않은 캐릭터입니다.";
            return false;
        }

        CharacterUpgradeTreeSO tree = GetTree(characterId);
        if (tree == null)
        {
            reason = "강화 트리가 비어 있습니다.";
            return false;
        }

        if (!tree.TryFindNode(nodeId, out CharacterUpgradeNodeData2D node) || node == null)
        {
            reason = "해당 노드를 찾지 못했습니다.";
            return false;
        }

        int currentRank = GetRank(characterId, nodeId);
        if (currentRank >= node.maxRank)
        {
            reason = "이미 최대 랭크입니다.";
            return false;
        }

        int level = _progressionService.GetLevel(characterId);
        if (level < Mathf.Max(1, node.requiredCharacterLevel))
        {
            reason = $"캐릭터 레벨 {node.requiredCharacterLevel}부터 구매할 수 있습니다.";
            return false;
        }

        if (node.prerequisiteNodeIds != null)
        {
            for (int i = 0; i < node.prerequisiteNodeIds.Count; i++)
            {
                string prerequisiteId = node.prerequisiteNodeIds[i];
                if (string.IsNullOrWhiteSpace(prerequisiteId)) continue;
                if (GetRank(characterId, prerequisiteId) > 0) continue;

                reason = "선행 노드를 먼저 구매해야 합니다.";
                return false;
            }
        }

        int cost = node.GetCostForNextRank(currentRank);
        if (!_walletService.CanSpendNyang(cost))
        {
            reason = "냥이 부족합니다.";
            return false;
        }

        return true;
    }

    public bool TryPurchase(string characterId, string nodeId, out string reason)
    {
        if (!CanPurchase(characterId, nodeId, out reason))
            return false;

        CharacterUpgradeTreeSO tree = GetTree(characterId);
        CharacterUpgradeStateSaveData2D state = GetOrCreateState(characterId);
        if (tree == null || state == null || !tree.TryFindNode(nodeId, out CharacterUpgradeNodeData2D node) || node == null)
        {
            reason = "노드 저장 중 오류가 발생했습니다.";
            return false;
        }

        int currentRank = state.GetRank(nodeId);
        int cost = node.GetCostForNextRank(currentRank);
        if (!_walletService.SpendNyang(cost, autoSave: false))
        {
            reason = "냥이 부족합니다.";
            return false;
        }

        state.SetRank(nodeId, currentRank + 1);
        Save();
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();
        reason = string.Empty;
        return true;
    }

    public int ResetCharacterTree(string characterId, bool refund, out string reason)
    {
        reason = string.Empty;

        CharacterUpgradeTreeSO tree = GetTree(characterId);
        CharacterUpgradeStateSaveData2D state = GetOrCreateState(characterId);
        if (tree == null || state == null)
        {
            reason = "초기화할 트리가 없습니다.";
            return 0;
        }

        int refundAmount = 0;
        if (refund && state.purchasedNodes != null)
        {
            for (int i = 0; i < state.purchasedNodes.Count; i++)
            {
                CharacterPurchasedUpgradeNodeSaveData2D entry = state.purchasedNodes[i];
                if (entry == null) continue;
                if (!tree.TryFindNode(entry.nodeId, out CharacterUpgradeNodeData2D node) || node == null) continue;

                for (int rank = 0; rank < entry.rank; rank++)
                {
                    refundAmount += node.GetCostForNextRank(rank);
                }
            }
        }

        state.ClearAll();
        if (refundAmount > 0)
            _walletService.AddNyang(refundAmount, autoSave: false);

        Save();
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();
        return refundAmount;
    }

    private CharacterUpgradeStateSaveData2D GetOrCreateState(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;
        MetaProfileSaveData2D meta = EnsureData();
        if (meta == null) return null;
        return meta.upgrades.GetOrCreate(characterId);
    }

    private CharacterDefinitionSO GetDefinition(string characterId)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(characterId)) return null;
        return _catalog.TryFindById(characterId, out CharacterDefinitionSO found) ? found : null;
    }

    private MetaProfileSaveData2D EnsureData()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save()
    {
        if (_saveManager != null)
            _saveManager.Save();
    }
}
