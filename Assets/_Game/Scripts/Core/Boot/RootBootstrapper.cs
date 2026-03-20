using UnityEngine;

/// <summary>
/// 아웃게임/인게임 씬의 루트 부트스트래퍼 싱글톤입니다.
/// 캐릭터 카탈로그, 레벨업 설정, 스킬 설정 등 공용 참조를 한 곳에서 제공합니다.
/// </summary>
public sealed class RootBootstrapper : MonoBehaviour
{
    [Header("캐릭터 시스템")]
    [Tooltip("캐릭터 카탈로그 SO를 가진 컨테이너입니다.")]
    [SerializeField] private CharacterRootContainer characterRoot;

    [Header("레벨업 시스템")]
    [Tooltip("레벨업 설정 SO입니다. LevelUpSystem2D에서 참조합니다.")]
    [SerializeField] private LevelUpRootSO levelUpRoot;

    [Header("스킬 시스템")]
    [Tooltip("스킬 루트 설정 SO입니다. LevelUpSystem2D/CommonSkillManager에서 참조합니다.")]
    [SerializeField] private SkillRootSO skillRoot;

    /// <summary>싱글톤 인스턴스입니다.</summary>
    public static RootBootstrapper Instance { get; private set; }

    /// <summary>캐릭터 시스템 루트 참조입니다.</summary>
    public CharacterRootContainer CharacterRoot => characterRoot;

    /// <summary>레벨업 설정 SO입니다.</summary>
    public LevelUpRootSO LevelUpRoot => levelUpRoot;

    /// <summary>스킬 루트 설정 SO입니다.</summary>
    public SkillRootSO SkillRoot => skillRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 세이브에서 편성 로드
        SquadLoadoutRuntime.LoadFromSave();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

/// <summary>
/// CharacterCatalogSO를 Inspector에서 할당받기 위한 컨테이너입니다.
/// </summary>
[System.Serializable]
public sealed class CharacterRootContainer
{
    [Tooltip("캐릭터 카탈로그 SO입니다.")]
    public CharacterCatalogSO catalog;
}