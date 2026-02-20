using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillManager2D : MonoBehaviour
{
    [Header("카탈로그")]
    [SerializeField] private CommonSkillCatalogSO catalog;

    [Header("오너")]
    [SerializeField] private Transform owner;
    [SerializeField] private bool spawnAsChild = true;

    private CommonSkillWeapon2D[] _weapons;
    private int[] _levels;

    private void Awake()
    {
        if (owner == null) owner = transform;

        int kindCount = System.Enum.GetValues(typeof(CommonSkillKind)).Length;
        _weapons = new CommonSkillWeapon2D[kindCount];
        _levels = new int[kindCount];

        // 시작 스킬이 있다면 여기서 초기화 가능(프로토타입은 공통스킬 시작 없음 가정)
    }

    public int GetLevel(CommonSkillKind kind)
    {
        int idx = (int)kind;
        if (_levels == null || idx < 0 || idx >= _levels.Length) return 0;
        return _levels[idx];
    }

    public bool IsMaxLevel(CommonSkillConfigSO skill)
    {
        if (skill == null) return true;
        int cur = GetLevel(skill.kind);
        return cur >= Mathf.Max(1, skill.maxLevel);
    }

    public void Upgrade(CommonSkillConfigSO skill)
    {
        if (skill == null) return;
        int idx = (int)skill.kind;

        int cur = _levels[idx];
        int next = Mathf.Clamp(cur + 1, 1, Mathf.Max(1, skill.maxLevel));
        _levels[idx] = next;

        // 첫 획득이면 프리팹 스폰/연결
        if (_weapons[idx] == null)
        {
            if (skill.weaponPrefab == null) return;

            GameObject go = Instantiate(skill.weaponPrefab);
            if (spawnAsChild && owner != null)
            {
                go.transform.SetParent(owner, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }
            else if (owner != null)
            {
                go.transform.position = owner.position;
            }

            var weapon = go.GetComponent<CommonSkillWeapon2D>();
            if (weapon == null)
            {
                Destroy(go);
                return;
            }

            weapon.Initialize(skill, owner, next);
            _weapons[idx] = weapon;
        }
        else
        {
            _weapons[idx].SetOwner(owner);
            _weapons[idx].Initialize(skill, owner, next);
        }
    }
}
