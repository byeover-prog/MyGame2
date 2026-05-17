using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterLevelData : MonoBehaviour
{
    public static CharacterLevelData Instance { get; private set; }

    private const string KeyPrefix = "CharLv_";
    private const int DefaultLevel = 1;

    [SerializeField] private CharacterCatalogSO catalog;

    private readonly Dictionary<string, int> _cache = new Dictionary<string, int>();
    private readonly HashSet<string> _legacyImportChecked = new HashSet<string>();
    private CharacterProgressionService2D _progressionService;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetLevel(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return DefaultLevel;

        CharacterProgressionService2D progression = GetProgressionService();
        if (progression == null)
            return ReadLegacyLevel(characterId);

        int level = progression.GetLevel(characterId);
        level = ImportLegacyLevelIfHigher(characterId, level, progression);

        _cache[characterId] = level;
        return level;
    }

    public void SetLevel(string characterId, int level)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        level = Mathf.Clamp(level, 1, 50);

        CharacterProgressionService2D progression = GetProgressionService();
        if (progression == null)
        {
            _cache[characterId] = level;
            GameLogger.LogWarning("[CharacterLevelData] SaveManager2D or CharacterCatalogSO is missing. Level was cached for this session only.", this);
            return;
        }

        progression.SetLevel(characterId, level);
        _cache[characterId] = level;
    }

    public void LevelUp(string characterId)
    {
        SetLevel(characterId, GetLevel(characterId) + 1);
    }

    private CharacterProgressionService2D GetProgressionService()
    {
        if (_progressionService != null)
            return _progressionService;

        ResolveCatalog();

        if (catalog == null || SaveManager2D.Instance == null)
            return null;

        _progressionService = new CharacterProgressionService2D(catalog, SaveManager2D.Instance);
        return _progressionService;
    }

    private void ResolveCatalog()
    {
        if (catalog != null)
            return;

        if (RootBootstrapper.Instance != null && RootBootstrapper.Instance.CharacterRoot != null)
            catalog = RootBootstrapper.Instance.CharacterRoot.catalog;
    }

    private int ImportLegacyLevelIfHigher(
        string characterId,
        int currentLevel,
        CharacterProgressionService2D progression)
    {
        if (_legacyImportChecked.Contains(characterId))
            return currentLevel;

        _legacyImportChecked.Add(characterId);

        string legacyKey = KeyPrefix + characterId;
        if (!PlayerPrefs.HasKey(legacyKey))
            return currentLevel;

        int legacyLevel = Mathf.Clamp(PlayerPrefs.GetInt(legacyKey, DefaultLevel), 1, 50);
        if (legacyLevel <= currentLevel)
            return currentLevel;

        progression.SetLevel(characterId, legacyLevel);
        _cache[characterId] = legacyLevel;
        return legacyLevel;
    }

    private int ReadLegacyLevel(string characterId)
    {
        if (_cache.TryGetValue(characterId, out int cached))
            return cached;

        int level = Mathf.Clamp(PlayerPrefs.GetInt(KeyPrefix + characterId, DefaultLevel), 1, 50);
        _cache[characterId] = level;
        return level;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Set All Level 1")]
    private void DebugReset()
    {
        foreach (string key in new List<string>(_cache.Keys))
            SetLevel(key, 1);

        GameLogger.Log("[CharacterLevelData] Cached levels reset to 1.");
    }
#endif
}
