using System.Collections.Generic;
using UnityEngine;

/// 캐릭터별 레벨을 PlayerPrefs에 저장/로드하는 싱글톤
/// 키 형식: "CharLv_[characterId]"
public class CharacterLevelData : MonoBehaviour
{
    public static CharacterLevelData Instance { get; private set; }

    private const string KEY_PREFIX = "CharLv_";
    private const int DEFAULT_LEVEL = 1;

    // 런타임 캐시 (씬 내 반복 접근 최적화)
    private readonly Dictionary<string, int> _cache = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>레벨 조회. 저장된 값 없으면 DEFAULT_LEVEL 반환</summary>
    public int GetLevel(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return DEFAULT_LEVEL;
        if (_cache.TryGetValue(characterId, out int cached)) return cached;

        int lv = PlayerPrefs.GetInt(KEY_PREFIX + characterId, DEFAULT_LEVEL);
        _cache[characterId] = lv;
        return lv;
    }

    // 레벨 저장
    public void SetLevel(string characterId, int level)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return;
        level = Mathf.Max(1, level);
        _cache[characterId] = level;
        PlayerPrefs.SetInt(KEY_PREFIX + characterId, level);
        PlayerPrefs.Save();
    }

    // 레벨 1 증가
    public void LevelUp(string characterId)
    {
        SetLevel(characterId, GetLevel(characterId) + 1);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Set All Level 1")]
    private void DebugReset()
    {
        foreach (var key in new List<string>(_cache.Keys))
            SetLevel(key, 1);
        Debug.Log("[CharacterLevelData] 전체 레벨 초기화");
    }
#endif
}