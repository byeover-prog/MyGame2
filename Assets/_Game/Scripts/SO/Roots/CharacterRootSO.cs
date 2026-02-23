using UnityEngine;

/// <summary>
/// "캐릭터 데이터"의 단일 진입점.
/// 
/// 의도
/// - 프로젝트에서 SO/Prefab/Script가 섞여도, '어디서부터 읽는가'는 항상 Root 3개로 고정한다.
/// - CharacterCatalogSO가 어디에 있든, 이 Root만 알면 UI/로비/런타임이 같은 데이터를 참조한다.
/// </summary>
[CreateAssetMenu(menuName = "그날이후/Roots/CharacterRoot", fileName = "Root_Character")]
public sealed class CharacterRootSO : ScriptableObject
{
    [Header("캐릭터 카탈로그")]
    [Tooltip("보유 캐릭터 목록(로비/편성 UI에서 사용)")]
    public CharacterCatalogSO catalog;
}