using System;
using System.Collections.Generic;

/// <summary>
/// 아웃게임 메타 프로필의 최상위 세이브 데이터입니다.
/// SaveManager2D.Data 안에 이 객체를 포함시켜야 합니다.
/// </summary>
[Serializable]
public sealed class MetaProfileSaveData2D
{
    /// <summary>보유 냥 (재화)입니다.</summary>
    public int nyang;

    /// <summary>편성 정보입니다.</summary>
    public FormationSaveData2D formation;

    /// <summary>캐릭터별 강화 상태입니다.</summary>
    public CharacterUpgradeCollectionSaveData2D upgrades;

    /// <summary>캐릭터별 진행 상태(레벨, 해금)입니다.</summary>
    public CharacterProgressionCollectionSaveData2D progression;

    /// <summary>캐릭터별 장비 장착 상태입니다.</summary>
    public CharacterEquipmentCollectionSaveData equipment;

    /// <summary>퀘스트 진행 상태입니다.</summary>
    public QuestProgressSaveData questProgress;

    /// <summary>기본값을 보장합니다.</summary>
    public void EnsureDefaults()
    {
        if (formation == null) formation = FormationSaveData2D.CreateDefault();
        formation.EnsureDefaults();

        if (upgrades == null) upgrades = new CharacterUpgradeCollectionSaveData2D();
        if (progression == null) progression = new CharacterProgressionCollectionSaveData2D();
        if (equipment == null) equipment = new CharacterEquipmentCollectionSaveData();
        if (questProgress == null) questProgress = new QuestProgressSaveData();
    }

    public static MetaProfileSaveData2D CreateDefault()
    {
        MetaProfileSaveData2D data = new MetaProfileSaveData2D
        {
            nyang = 0,
        };
        data.EnsureDefaults();
        return data;
    }
}

// ─────────────────────────────────────────────────────────────
// 강화 세이브
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 모든 캐릭터의 강화 상태를 모아 두는 컬렉션입니다.
/// </summary>
[Serializable]
public sealed class CharacterUpgradeCollectionSaveData2D
{
    public List<CharacterUpgradeStateSaveData2D> entries = new List<CharacterUpgradeStateSaveData2D>(3);

    /// <summary>
    /// 해당 캐릭터의 강화 상태를 찾거나, 없으면 새로 만듭니다.
    /// </summary>
    public CharacterUpgradeStateSaveData2D GetOrCreate(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].characterId == characterId)
                return entries[i];
        }

        CharacterUpgradeStateSaveData2D newState = new CharacterUpgradeStateSaveData2D { characterId = characterId };
        entries.Add(newState);
        return newState;
    }
}

/// <summary>
/// 한 캐릭터의 강화 트리 구매 상태입니다.
/// </summary>
[Serializable]
public sealed class CharacterUpgradeStateSaveData2D
{
    public string characterId;
    public List<CharacterPurchasedUpgradeNodeSaveData2D> purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(10);

    /// <summary>해당 노드의 현재 랭크를 반환합니다.</summary>
    public int GetRank(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || purchasedNodes == null) return 0;

        for (int i = 0; i < purchasedNodes.Count; i++)
        {
            CharacterPurchasedUpgradeNodeSaveData2D entry = purchasedNodes[i];
            if (entry != null && entry.nodeId == nodeId)
                return entry.rank;
        }
        return 0;
    }

    /// <summary>해당 노드의 랭크를 설정합니다.</summary>
    public void SetRank(string nodeId, int rank)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return;

        for (int i = 0; i < purchasedNodes.Count; i++)
        {
            CharacterPurchasedUpgradeNodeSaveData2D entry = purchasedNodes[i];
            if (entry != null && entry.nodeId == nodeId)
            {
                entry.rank = rank;
                return;
            }
        }

        purchasedNodes.Add(new CharacterPurchasedUpgradeNodeSaveData2D { nodeId = nodeId, rank = rank });
    }

    /// <summary>모든 구매 기록을 초기화합니다.</summary>
    public void ClearAll()
    {
        if (purchasedNodes != null)
            purchasedNodes.Clear();
    }
}

/// <summary>
/// 구매한 노드 하나의 저장 데이터입니다.
/// </summary>
[Serializable]
public sealed class CharacterPurchasedUpgradeNodeSaveData2D
{
    public string nodeId;
    public int rank;
}

// ─────────────────────────────────────────────────────────────
// 진행(레벨·해금) 세이브
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 모든 캐릭터의 레벨·해금 상태를 모아 두는 컬렉션입니다.
/// </summary>
[Serializable]
public sealed class CharacterProgressionCollectionSaveData2D
{
    public List<CharacterProgressionEntrySaveData2D> entries = new List<CharacterProgressionEntrySaveData2D>(3);

    public CharacterProgressionEntrySaveData2D GetOrCreate(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].characterId == characterId)
                return entries[i];
        }

        CharacterProgressionEntrySaveData2D newEntry = new CharacterProgressionEntrySaveData2D
        {
            characterId = characterId,
            level = 1,
            unlocked = false
        };
        entries.Add(newEntry);
        return newEntry;
    }
}

/// <summary>
/// 한 캐릭터의 레벨·해금 상태입니다.
/// </summary>
[Serializable]
public sealed class CharacterProgressionEntrySaveData2D
{
    public string characterId;
    public int level = 1;
    public bool unlocked = false;
    public int totalExp = 0;
}

// ─────────────────────────────────────────────────────────────
// 퀘스트 세이브
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 퀘스트 진행 상태 저장 데이터입니다.
/// </summary>
[Serializable]
public sealed class QuestProgressSaveData
{
    /// <summary>완료한 퀘스트 ID 목록입니다.</summary>
    public List<string> completedQuestIds = new List<string>(16);

    /// <summary>획득한 각성 효과 ID 목록입니다.</summary>
    public List<string> unlockedAwakeningIds = new List<string>(8);

    /// <summary>해당 퀘스트가 완료되었는지 확인합니다.</summary>
    public bool IsCompleted(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return false;
        return completedQuestIds.Contains(questId);
    }

    /// <summary>해당 각성이 해금되었는지 확인합니다.</summary>
    public bool IsAwakeningUnlocked(string awakeningId)
    {
        if (string.IsNullOrWhiteSpace(awakeningId)) return false;
        return unlockedAwakeningIds.Contains(awakeningId);
    }
}