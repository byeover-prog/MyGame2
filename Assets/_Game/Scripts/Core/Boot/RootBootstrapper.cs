using UnityEngine;

/// <summary>
/// 프로젝트 전역 데이터 Root 3개를 "반드시" 먼저 잡아두는 부트스트랩.
/// 
/// 왜 필요한가?
/// - Unity의 Awake 순서는 같은 씬 내에서도 보장되지 않는다.
/// - 따라서 "아무 스크립트나 Awake에서 RootBootstrapper.Instance를 읽는" 방식은
///   null 참조를 일으킬 수 있다.
/// 
/// 해결
/// - DefaultExecutionOrder(-10000)으로 최대한 먼저 실행.
/// - 그래도 '절대 안전'을 위해, 다른 시스템은 Awake가 아닌 Start에서 Root를 읽는 것을 권장.
/// 
/// 복잡도
/// - O(1)
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class RootBootstrapper : MonoBehaviour
{
    public static RootBootstrapper Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private CharacterRootSO characterRoot;
    [SerializeField] private SkillRootSO skillRoot;
    [SerializeField] private LevelUpRootSO levelUpRoot;

    public CharacterRootSO CharacterRoot => characterRoot;
    public SkillRootSO SkillRoot => skillRoot;
    public LevelUpRootSO LevelUpRoot => levelUpRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 중복 부트 방지: 두 번째부터는 파괴
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 최소 검증(에디터에서 바로 원인 확인 가능)
        if (characterRoot == null)
            Debug.LogWarning("[RootBootstrapper] CharacterRootSO가 비어 있습니다.", this);
        if (skillRoot == null)
            Debug.LogWarning("[RootBootstrapper] SkillRootSO가 비어 있습니다.", this);
        if (levelUpRoot == null)
            Debug.LogWarning("[RootBootstrapper] LevelUpRootSO가 비어 있습니다.", this);
    }
}