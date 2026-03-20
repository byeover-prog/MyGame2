using UnityEngine;

public static class MetaAutoBootstrap2D
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSaveManagerExists()
    {
        if (SaveManager2D.Instance != null)
            return;

        GameObject go = new GameObject("@SaveManager2D_Auto");
        go.AddComponent<SaveManager2D>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        ApplySavedFormationToRuntime();
        RebuildBattleSnapshotIfPossible();
    }

    public static void ApplySavedFormationToRuntime()
    {
        SaveManager2D saveManager = SaveManager2D.Instance;
        if (saveManager == null || saveManager.Data == null) return;

        saveManager.Data.EnsureDefaults();
        SquadLoadoutRuntime.CopyFromSave(saveManager.Data.metaProfile.formation, notify: false);
    }

    public static void RebuildBattleSnapshotIfPossible()
    {
        SaveManager2D saveManager = SaveManager2D.Instance;
        if (saveManager == null || saveManager.Data == null)
        {
            MetaBattleSnapshotRuntime2D.Clear();
            return;
        }

        CharacterCatalogSO catalog = ResolveCatalog();
        if (catalog == null)
        {
            MetaBattleSnapshotRuntime2D.Clear();
            return;
        }

        saveManager.Data.EnsureDefaults();
        CharacterMetaResolver2D resolver = new CharacterMetaResolver2D(catalog, saveManager);
        SquadMetaBattleSnapshot2D snapshot = resolver.BuildSquadSnapshot(saveManager.Data.metaProfile.formation);
        MetaBattleSnapshotRuntime2D.SetCurrent(snapshot);
    }

    private static CharacterCatalogSO ResolveCatalog()
    {
        if (RootBootstrapper.Instance != null && RootBootstrapper.Instance.CharacterRoot != null)
            return RootBootstrapper.Instance.CharacterRoot.catalog;

        return null;
    }
}
