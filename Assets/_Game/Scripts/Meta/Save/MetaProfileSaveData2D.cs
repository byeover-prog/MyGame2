using System;
using System.Collections.Generic;

[Serializable]
public sealed class MetaProfileSaveData2D
{
    public WalletSaveData2D wallet = WalletSaveData2D.CreateDefault();
    public FormationSaveData2D formation = FormationSaveData2D.CreateDefault();
    public CharacterProgressionCollectionSaveData2D progression = CharacterProgressionCollectionSaveData2D.CreateDefault();
    public CharacterUpgradeCollectionSaveData2D upgrades = CharacterUpgradeCollectionSaveData2D.CreateDefault();

    public static MetaProfileSaveData2D CreateDefault()
    {
        return new MetaProfileSaveData2D
        {
            wallet = WalletSaveData2D.CreateDefault(),
            formation = FormationSaveData2D.CreateDefault(),
            progression = CharacterProgressionCollectionSaveData2D.CreateDefault(),
            upgrades = CharacterUpgradeCollectionSaveData2D.CreateDefault()
        };
    }

    public void EnsureDefaults()
    {
        if (wallet == null) wallet = WalletSaveData2D.CreateDefault();
        if (formation == null) formation = FormationSaveData2D.CreateDefault();
        if (progression == null) progression = CharacterProgressionCollectionSaveData2D.CreateDefault();
        if (upgrades == null) upgrades = CharacterUpgradeCollectionSaveData2D.CreateDefault();

        wallet.EnsureDefaults();
        formation.EnsureDefaults();
        progression.EnsureDefaults();
        upgrades.EnsureDefaults();
    }
}

[Serializable]
public sealed class WalletSaveData2D
{
    public int nyang = 0;

    public static WalletSaveData2D CreateDefault()
    {
        return new WalletSaveData2D { nyang = 0 };
    }

    public void EnsureDefaults()
    {
        if (nyang < 0) nyang = 0;
    }
}

[Serializable]
public sealed class CharacterProgressionCollectionSaveData2D
{
    public List<CharacterProgressionSaveData2D> characters = new List<CharacterProgressionSaveData2D>(16);

    public static CharacterProgressionCollectionSaveData2D CreateDefault()
    {
        return new CharacterProgressionCollectionSaveData2D();
    }

    public void EnsureDefaults()
    {
        if (characters == null) characters = new List<CharacterProgressionSaveData2D>(16);

        for (int i = characters.Count - 1; i >= 0; i--)
        {
            if (characters[i] == null || string.IsNullOrWhiteSpace(characters[i].characterId))
            {
                characters.RemoveAt(i);
                continue;
            }

            characters[i].EnsureDefaults();
        }
    }

    public CharacterProgressionSaveData2D GetOrCreate(string characterId, bool unlockedByDefault)
    {
        if (characters == null) characters = new List<CharacterProgressionSaveData2D>(16);

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterProgressionSaveData2D entry = characters[i];
            if (entry == null) continue;
            if (entry.characterId != characterId) continue;

            entry.EnsureDefaults();
            return entry;
        }

        CharacterProgressionSaveData2D created = new CharacterProgressionSaveData2D
        {
            characterId = characterId,
            isUnlocked = unlockedByDefault,
            level = 1,
            currentXp = 0,
            totalXp = 0
        };

        created.EnsureDefaults();
        characters.Add(created);
        return created;
    }
}

[Serializable]
public sealed class CharacterProgressionSaveData2D
{
    public string characterId;
    public bool isUnlocked = true;
    public int level = 1;
    public int currentXp = 0;
    public int totalXp = 0;

    public void EnsureDefaults()
    {
        if (level < 1) level = 1;
        if (currentXp < 0) currentXp = 0;
        if (totalXp < 0) totalXp = 0;
    }
}

[Serializable]
public sealed class CharacterUpgradeCollectionSaveData2D
{
    public List<CharacterUpgradeStateSaveData2D> characters = new List<CharacterUpgradeStateSaveData2D>(16);

    public static CharacterUpgradeCollectionSaveData2D CreateDefault()
    {
        return new CharacterUpgradeCollectionSaveData2D();
    }

    public void EnsureDefaults()
    {
        if (characters == null) characters = new List<CharacterUpgradeStateSaveData2D>(16);

        for (int i = characters.Count - 1; i >= 0; i--)
        {
            if (characters[i] == null || string.IsNullOrWhiteSpace(characters[i].characterId))
            {
                characters.RemoveAt(i);
                continue;
            }

            characters[i].EnsureDefaults();
        }
    }

    public CharacterUpgradeStateSaveData2D GetOrCreate(string characterId)
    {
        if (characters == null) characters = new List<CharacterUpgradeStateSaveData2D>(16);

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterUpgradeStateSaveData2D entry = characters[i];
            if (entry == null) continue;
            if (entry.characterId != characterId) continue;

            entry.EnsureDefaults();
            return entry;
        }

        CharacterUpgradeStateSaveData2D created = new CharacterUpgradeStateSaveData2D
        {
            characterId = characterId,
            purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(16)
        };

        created.EnsureDefaults();
        characters.Add(created);
        return created;
    }
}

[Serializable]
public sealed class CharacterUpgradeStateSaveData2D
{
    public string characterId;
    public List<CharacterPurchasedUpgradeNodeSaveData2D> purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(16);

    public void EnsureDefaults()
    {
        if (purchasedNodes == null) purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(16);

        for (int i = purchasedNodes.Count - 1; i >= 0; i--)
        {
            if (purchasedNodes[i] == null || string.IsNullOrWhiteSpace(purchasedNodes[i].nodeId))
            {
                purchasedNodes.RemoveAt(i);
                continue;
            }

            purchasedNodes[i].EnsureDefaults();
        }
    }

    public int GetRank(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || purchasedNodes == null) return 0;

        for (int i = 0; i < purchasedNodes.Count; i++)
        {
            CharacterPurchasedUpgradeNodeSaveData2D entry = purchasedNodes[i];
            if (entry == null) continue;
            if (entry.nodeId != nodeId) continue;
            return entry.rank;
        }

        return 0;
    }

    public void SetRank(string nodeId, int rank)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return;
        if (purchasedNodes == null) purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(16);

        for (int i = 0; i < purchasedNodes.Count; i++)
        {
            CharacterPurchasedUpgradeNodeSaveData2D entry = purchasedNodes[i];
            if (entry == null) continue;
            if (entry.nodeId != nodeId) continue;

            if (rank <= 0)
            {
                purchasedNodes.RemoveAt(i);
                return;
            }

            entry.rank = rank;
            entry.EnsureDefaults();
            return;
        }

        if (rank <= 0) return;

        CharacterPurchasedUpgradeNodeSaveData2D created = new CharacterPurchasedUpgradeNodeSaveData2D
        {
            nodeId = nodeId,
            rank = rank
        };
        created.EnsureDefaults();
        purchasedNodes.Add(created);
    }

    public void ClearAll()
    {
        if (purchasedNodes == null) purchasedNodes = new List<CharacterPurchasedUpgradeNodeSaveData2D>(16);
        purchasedNodes.Clear();
    }
}

[Serializable]
public sealed class CharacterPurchasedUpgradeNodeSaveData2D
{
    public string nodeId;
    public int rank = 1;

    public void EnsureDefaults()
    {
        if (rank < 1) rank = 1;
    }
}
