using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillManager2D : MonoBehaviour, ISkillGrantReceiver2D
{
    [Header("카탈로그")]
    [SerializeField] private CommonSkillCatalogSO catalog;

    [Header("오너")]
    [SerializeField] private Transform owner;
    [SerializeField] private bool spawnAsChild = true;

    // --- 옵저버(이벤트) ---
    // 성공: (스킬, 적용된 레벨, 최초획득 여부)
    public event Action<CommonSkillConfigSO, int, bool> OnSkillGranted;

    // 실패: (스킬SO, 요청레벨, 실패사유)
    public event Action<ScriptableObject, int, string> OnSkillGrantFailed;

    // 레벨 변화: (kind, 이전레벨, 현재레벨)
    public event Action<CommonSkillKind, int, int> OnLevelChanged;

    private CommonSkillWeapon2D[] _weapons;
    private int[] _levels;

    private void Awake()
    {
        if (owner == null) owner = transform;

        int kindCount = Enum.GetValues(typeof(CommonSkillKind)).Length;
        _weapons = new CommonSkillWeapon2D[kindCount];
        _levels = new int[kindCount];
    }

    /// <summary>
    /// 외부(시작 바인더/레벨업 시스템 등)에서 "스킬을 부여"하는 단일 진입점.
    /// - 성공/실패를 bool로 반환(디버그 친화)
    /// - 성공/실패/레벨변화는 이벤트로 발행(옵저버 패턴)
    /// </summary>
    public bool TryGrantSkill(ScriptableObject skill, int level)
    {
        if (skill == null)
        {
            OnSkillGrantFailed?.Invoke(null, level, "skill == null");
            return false;
        }

        if (level < 1) level = 1;

        // 1) 이미 CommonSkillConfigSO가 직접 들어오는 경우
        if (skill is CommonSkillConfigSO configDirect)
            return TryGrantConfig(configDirect, level);

        // 2) WeaponDefinitionSO 같은 걸 넣는 경우를 지원 (네 스샷에서 Weapon_Balsi를 넣고 있음)
        //    catalog가 매핑을 제공해야 함: "Definition -> CommonSkillConfig"
        //    아래는 '가능한 메서드'들을 다 시도하는 안전형(컴파일 타임 안전 + 런타임 fallback)
        if (catalog != null)
        {
            if (TryResolveFromCatalog(skill, out CommonSkillConfigSO resolved))
                return TryGrantConfig(resolved, level);

            OnSkillGrantFailed?.Invoke(skill, level, $"catalog에서 스킬을 찾지 못함: {skill.name}");
            return false;
        }

        OnSkillGrantFailed?.Invoke(skill, level, "catalog == null (CommonSkillConfigSO가 아님)");
        return false;
    }

    private bool TryGrantConfig(CommonSkillConfigSO skill, int requestedLevel)
    {
        if (skill == null)
        {
            OnSkillGrantFailed?.Invoke(null, requestedLevel, "config == null");
            return false;
        }

        int max = Mathf.Max(1, skill.maxLevel);

        // “요청 레벨”까지 올리는 의미로 처리
        // 예) 시작레벨 3이면 Upgrade를 3번 누른 효과
        int idx = (int)skill.kind;
        if (idx < 0 || idx >= _levels.Length)
        {
            OnSkillGrantFailed?.Invoke(skill, requestedLevel, $"kind index out of range: {skill.kind}");
            return false;
        }

        int before = _levels[idx];
        if (before >= max)
        {
            OnSkillGrantFailed?.Invoke(skill, requestedLevel, $"이미 최대 레벨({max})");
            return false;
        }

        int target = Mathf.Clamp(requestedLevel, 1, max);

        // 현재가 0이면 “최초 획득”으로 보고, 1부터 시작해야 자연스러움
        // (현재 구현의 Upgrade는 +1 방식이라, 목표 레벨까지 반복 호출)
        bool firstAcquire = (before <= 0);

        int applied = before;
        while (applied < target)
        {
            Upgrade(skill); // 내부에서 _levels 갱신 + 무기 생성/초기화 처리
            applied = _levels[idx];

            // 안전장치(무한루프 방지): Upgrade가 레벨을 못 올리는 경우 빠져나감
            if (applied <= before)
                break;

            before = applied;
        }

        int after = _levels[idx];
        if (after <= 0)
        {
            OnSkillGrantFailed?.Invoke(skill, requestedLevel, "Upgrade 적용 후 레벨이 0 이하");
            return false;
        }

        OnLevelChanged?.Invoke(skill.kind, 0, after); // 시작부여는 0->after로 보는 게 로그에 유리
        OnSkillGranted?.Invoke(skill, after, firstAcquire);
        return true;
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

        int prev = _levels[idx];
        int next = Mathf.Clamp(prev + 1, 1, Mathf.Max(1, skill.maxLevel));
        _levels[idx] = next;

        OnLevelChanged?.Invoke(skill.kind, prev, next);

        // 첫 획득이면 프리팹 스폰/연결
        if (_weapons[idx] == null)
        {
            if (skill.weaponPrefab == null)
            {
                OnSkillGrantFailed?.Invoke(skill, next, "weaponPrefab == null");
                return;
            }

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
                OnSkillGrantFailed?.Invoke(skill, next, "weaponPrefab에 CommonSkillWeapon2D가 없음");
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

        // 레벨업 카드에서 Upgrade를 눌렀을 때도 “성공 이벤트”가 필요하면 여기서도 발행 가능
        // 다만 중복 발행이 싫으면 TryGrantConfig에서만 발행하고, 여기선 LevelChanged만 발행하는 식으로 운영.
    }

    /// <summary>
    /// catalog에서 "ScriptableObject(WeaponDefinitionSO 등)" -> CommonSkillConfigSO 매핑을 찾아낸다.
    /// 프로젝트마다 Catalog 구현이 다르므로, 여기서는 "가능한 패턴"을 안전하게 지원한다.
    /// </summary>
    private bool TryResolveFromCatalog(ScriptableObject anySkillSo, out CommonSkillConfigSO result)
    {
        result = null;
        if (catalog == null || anySkillSo == null) return false;

        // (1) 가장 이상적인 케이스: catalog에 명시 메서드가 있는 경우
        // 예: bool TryGetConfig(ScriptableObject key, out CommonSkillConfigSO cfg)
        // 예: CommonSkillConfigSO FindByDefinition(WeaponDefinitionSO def)
        // → 여기선 컴파일 타임을 위해 “직접 호출”은 못함(카탈로그 코드가 없으니)
        // 대신, Catalog 쪽에 아래 중 하나를 "public"으로 만들어주면 여기서 쉽게 연결 가능:
        //
        // public bool TryGetConfig(ScriptableObject key, out CommonSkillConfigSO cfg)
        //
        // 지금은 최소 보장으로:
        // - anySkillSo가 이미 CommonSkillConfigSO면 반환
        if (anySkillSo is CommonSkillConfigSO cfg)
        {
            result = cfg;
            return true;
        }

        // 카탈로그 구현이 준비되기 전까지는 여기서 실패 처리.
        // (원하면 Catalog 코드까지 내가 같이 설계해줄게)
        return false;
    }
}