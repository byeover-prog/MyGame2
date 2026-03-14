using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Monster/MonsterCatalog")]
public class MonsterCatalogSO : ScriptableObject
{
    [SerializeField, Tooltip("등록된 몬스터 Definition 목록")]
    private List<MonsterDefinitionSO> monsters = new List<MonsterDefinitionSO>();

    // 몬스터 전체 목록 반환
    public List<MonsterDefinitionSO> Monsters => monsters;

    // ID로 몬스터 데이터를 찾음
    public MonsterDefinitionSO GetMonsterById(string id)
    {
        foreach (var monster in monsters)
        {
            if (monster == null)
                continue;

            if (monster.MonsterId == id)
                return monster;
        }

        Debug.LogWarning($"[MonsterCatalogSO] ID가 {id} 인 몬스터를 찾을 수 없습니다.");
        return null;
    }

    // 타입으로 몬스터 목록을 가져옴
    public List<MonsterDefinitionSO> GetMonstersByType(MonsterType type)
    {
        List<MonsterDefinitionSO> results = new List<MonsterDefinitionSO>();

        foreach (var monster in monsters)
        {
            if (monster == null)
                continue;

            if (monster.MonsterType == type)
                results.Add(monster);
        }

        return results;
    }
}