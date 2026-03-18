using UnityEngine;

[CreateAssetMenu(menuName = "Game/Monster/Root_Monster")]
public class MonsterRootSO : ScriptableObject
{
    [SerializeField, Tooltip("몬스터 카탈로그 SO")]
    private MonsterCatalogSO monsterCatalog;

    // 몬스터 카탈로그 반환
    public MonsterCatalogSO MonsterCatalog => monsterCatalog;
}