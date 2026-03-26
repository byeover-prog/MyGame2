// UTF-8
using UnityEngine;

/// <summary>
/// Outgame 편성 저장값을 Scene_Game의 Player 프리팹에 실제로 적용합니다.
/// - SaveManager / SquadLoadoutRuntime의 현재 편성을 읽음
/// - SquadLoadout2D에 메인/지원 캐릭터를 주입
/// - MetaBattleSnapshotRuntime2D를 다시 구성
/// - 바람 속성이 있으면 PlayerDashController를 다시 초기화
/// </summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public sealed class SquadRuntimeBattleBootstrap2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private SquadLoadout2D squadLoadout;
    [SerializeField] private PlayerDashController playerDash;
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("옵션")]
    [SerializeField] private bool buildMetaSnapshot = true;
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (squadLoadout == null)
            squadLoadout = GetComponent<SquadLoadout2D>();

        if (squadLoadout == null)
            squadLoadout = FindFirstObjectByType<SquadLoadout2D>();

        if (playerDash == null)
            playerDash = GetComponent<PlayerDashController>();

        if (playerDash == null)
            playerDash = FindFirstObjectByType<PlayerDashController>();

        if (catalog == null && RootBootstrapper.Instance != null && RootBootstrapper.Instance.CharacterRoot != null)
            catalog = RootBootstrapper.Instance.CharacterRoot.catalog;

        ApplyRuntimeLoadout();
    }

    [ContextMenu("런타임 편성 다시 적용")]
    public void ApplyRuntimeLoadout()
    {
        SquadLoadoutRuntime.LoadFromSave();
        SquadLoadoutRuntime.Loadout runtime = SquadLoadoutRuntime.Current;

        if (catalog == null)
        {
            Debug.LogError("[SquadRuntimeBattleBootstrap2D] CharacterCatalogSO가 없습니다.", this);
            return;
        }

        if (squadLoadout == null)
        {
            Debug.LogError("[SquadRuntimeBattleBootstrap2D] SquadLoadout2D가 없습니다.", this);
            return;
        }

        CharacterDefinitionSO main = Resolve(runtime.mainId);
        CharacterDefinitionSO support1 = Resolve(runtime.support1Id);
        CharacterDefinitionSO support2 = Resolve(runtime.support2Id);

        if (main == null)
        {
            GameLogger.LogWarning("[SquadRuntimeBattleBootstrap2D] 저장된 메인 캐릭터가 없어 기본 편성을 유지합니다.", this);
            return;
        }

        squadLoadout.SetLoadout(main, support1, support2);

        if (buildMetaSnapshot)
            RebuildMetaSnapshot();

        if (playerDash != null)
            playerDash.Initialize(HasWindAttribute(main, support1, support2));

        if (debugLog)
        {
            GameLogger.Log(
                $"[SquadRuntimeBattleBootstrap2D] 전투 편성 적용 | 메인={NameOf(main)} | 지원1={NameOf(support1)} | 지원2={NameOf(support2)}",
                this);
        }
    }

    private void RebuildMetaSnapshot()
    {
        if (catalog == null)
            return;

        CharacterMetaResolver2D resolver = new CharacterMetaResolver2D(catalog, SaveManager2D.Instance);
        MetaBattleSnapshotRuntime2D.SetCurrent(resolver.BuildCurrentSquadSnapshot());
    }

    private CharacterDefinitionSO Resolve(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        return catalog != null && catalog.TryFindById(characterId, out CharacterDefinitionSO found)
            ? found
            : null;
    }

    private static bool HasWindAttribute(CharacterDefinitionSO main, CharacterDefinitionSO support1, CharacterDefinitionSO support2)
    {
        return (main != null && main.Attribute == CharacterAttributeKind.Wind)
            || (support1 != null && support1.Attribute == CharacterAttributeKind.Wind)
            || (support2 != null && support2.Attribute == CharacterAttributeKind.Wind);
    }

    private static string NameOf(CharacterDefinitionSO def)
    {
        return def != null ? def.DisplayName : "(없음)";
    }
}