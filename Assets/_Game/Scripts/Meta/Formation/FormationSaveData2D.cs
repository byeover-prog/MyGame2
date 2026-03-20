using System;

[Serializable]
public sealed class FormationSaveData2D
{
    public string support1Id;
    public string mainId;
    public string support2Id;

    public static FormationSaveData2D CreateDefault()
    {
        return new FormationSaveData2D();
    }

    public void EnsureDefaults()
    {
        // 문자열은 비어 있어도 정상 상태입니다.
    }

    public bool HasMain => !string.IsNullOrWhiteSpace(mainId);

    public string GetCharacterId(FormationSlotType2D slot)
    {
        return slot switch
        {
            FormationSlotType2D.Support1 => support1Id,
            FormationSlotType2D.Support2 => support2Id,
            _ => mainId,
        };
    }

    public void SetCharacterId(FormationSlotType2D slot, string characterId)
    {
        switch (slot)
        {
            case FormationSlotType2D.Support1:
                support1Id = characterId;
                break;
            case FormationSlotType2D.Support2:
                support2Id = characterId;
                break;
            default:
                mainId = characterId;
                break;
        }
    }

    public bool Contains(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;
        return support1Id == characterId || mainId == characterId || support2Id == characterId;
    }

    public void ClearSlot(FormationSlotType2D slot)
    {
        SetCharacterId(slot, null);
    }

    public void ClearAll()
    {
        support1Id = null;
        mainId = null;
        support2Id = null;
    }
}
