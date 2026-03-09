// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - TextAsset(JSON)을 읽어 SkillId+Level로 수치를 조회한다.
// - JsonUtility는 Dictionary가 약하므로 배열 기반으로 저장하고 선형 탐색(프로토타입 충분).
public sealed class BalanceDB : MonoBehaviour
{
    public static BalanceDB Instance { get; private set; }

    [Header("Source")]
    [Tooltip("Balance JSON(TextAsset)을 여기에 드래그")]
    [SerializeField] private TextAsset balanceJson;

    [Tooltip("Awake에서 자동 로드")]
    [SerializeField] private bool loadOnAwake = true;

    private BalanceRoot _root;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadOnAwake)
            Load();
    }

    [ContextMenu("Load Balance JSON")]
    public void Load()
    {
        if (balanceJson == null)
        {
            Debug.LogWarning("[BalanceDB] balanceJson이 비어있음", this);
            _root = null;
            return;
        }

        _root = JsonUtility.FromJson<BalanceRoot>(balanceJson.text);
        if (_root == null || _root.skills == null)
        {
            Debug.LogWarning("[BalanceDB] JSON 파싱 실패(형식 확인 필요)", this);
            return;
        }

        Debug.Log($"[BalanceDB] 로드 완료 skills={_root.skills.Length}", this);
    }

    public bool TryGetSkillLevel(string skillId, int level, out SkillLevelStat stat)
    {
        stat = default;

        if (_root == null || _root.skills == null) return false;
        if (string.IsNullOrEmpty(skillId)) return false;

        int lvIndex = Mathf.Clamp(level, 1, 8) - 1;

        for (int i = 0; i < _root.skills.Length; i++)
        {
            var s = _root.skills[i];
            if (s == null) continue;
            if (s.id != skillId) continue;

            if (s.levels == null || s.levels.Length <= lvIndex) return false;

            stat = s.levels[lvIndex];
            return true;
        }

        return false;
    }
}

[System.Serializable]
public sealed class BalanceRoot
{
    public SkillStatBlock[] skills;
}

[System.Serializable]
public sealed class SkillStatBlock
{
    public string id;
    public SkillLevelStat[] levels;
}

[System.Serializable]
public struct SkillLevelStat
{
    public float cooldown;
    public float damage;

    public float projectileSpeed;
    public float turnSpeedDeg;

    public int count;
    public int bounce;
    public int hits;

    public float travelDistance;
    public float explosionRadius;
    public float splitAngleDeg;
    public int maxSplitGen;
}