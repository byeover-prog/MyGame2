using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공통 스킬 런타임 관리자.
///
/// 역할
/// - 스킬 레벨(1~8) 상태를 보관
/// - 첫 획득 시 weaponPrefab 스폰 후 런타임 컴포넌트 연결
/// - 업그레이드 시 실제 스킬 컴포넌트에 레벨 반영
///
/// 지원 대상
/// - CommonSkillWeapon2D 기반 무기
/// - ILevelableSkill 인터페이스를 구현한 기존 무기 프리팹 (DarkOrb, HomingMissile 등)
/// </summary>
[DisallowMultipleComponent]
public sealed class CommonSkillManager2D : MonoBehaviour
{
    [Serializable]
    private sealed class RuntimeBinding
    {
        public GameObject root;
        public MonoBehaviour[] runtimeComponents;
    }

    [Header("카탈로그")]
    [SerializeField] private CommonSkillCatalogSO catalog;

    [Header("오너")]
    [SerializeField] private Transform owner;
    [SerializeField] private bool spawnAsChild = true;

    private RuntimeBinding[] _bindings;
    private int[] _levels;

    public event Action<CommonSkillKind> OnSkillAcquired;
    public event Action<CommonSkillKind, int> OnSkillLevelChanged;

    public CommonSkillCatalogSO Catalog => catalog;

    private void Awake()
    {
        if (owner == null)
            owner = transform;

        int kindCount = Enum.GetValues(typeof(CommonSkillKind)).Length;
        _bindings = new RuntimeBinding[kindCount];
        _levels = new int[kindCount];
    }

    public int GetLevel(CommonSkillKind kind)
    {
        int idx = (int)kind;
        if (_levels == null || idx < 0 || idx >= _levels.Length)
            return 0;

        return _levels[idx];
    }

    public bool IsMaxLevel(CommonSkillConfigSO skill)
    {
        if (skill == null)
            return true;

        int cur = GetLevel(skill.kind);
        int max = Mathf.Clamp(skill.maxLevel, 1, CommonSkillConfigSO.HardMaxLevel);
        return cur >= max;
    }

    /// <summary>
    /// 스킬을 1레벨로 획득하거나, 이미 있으면 +1 업그레이드합니다.
    /// </summary>
    public void Upgrade(CommonSkillConfigSO skill)
    {
        if (skill == null)
            return;

        int idx = (int)skill.kind;
        if (_levels == null || idx < 0 || idx >= _levels.Length)
            return;

        int max = Mathf.Clamp(skill.maxLevel, 1, CommonSkillConfigSO.HardMaxLevel);
        int cur = _levels[idx];

        if (cur >= max)
            return;

        bool isFirstAcquire = cur == 0;

        // 첫 획득이면 프리팹 스폰/연결
        if (isFirstAcquire)
        {
            if (!TryCreateRuntimeBinding(skill, idx))
                return;
        }

        int next = Mathf.Clamp(cur + 1, 1, max);
        _levels[idx] = next;

        ApplyRuntimeLevel(skill, idx, next);

        if (isFirstAcquire)
            OnSkillAcquired?.Invoke(skill.kind);

        OnSkillLevelChanged?.Invoke(skill.kind, next);
    }

    private bool TryCreateRuntimeBinding(CommonSkillConfigSO skill, int index)
    {
        if (skill.weaponPrefab == null)
        {
            GameLogger.LogWarning($"[CommonSkillManager2D] weaponPrefab 누락: {skill.name}", this);
            return false;
        }

        GameObject root = Instantiate(skill.weaponPrefab);

        if (spawnAsChild && owner != null)
        {
            root.transform.SetParent(owner, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
        }
        else if (owner != null)
        {
            root.transform.position = owner.position;
        }

        // ★ CommonSkillWeapon2D와 ILevelableSkill 모두 탐색
        MonoBehaviour[] runtimeComponents = CollectRuntimeComponents(root);
        if (runtimeComponents.Length == 0)
        {
            GameLogger.LogWarning(
                $"[CommonSkillManager2D] 런타임 스킬 컴포넌트를 찾지 못했습니다: {skill.weaponPrefab.name}\n" +
                "프리팹 루트/자식 중 하나에 CommonSkillWeapon2D 또는 ILevelableSkill 구현 스크립트가 필요합니다.",
                this);

            Destroy(root);
            return false;
        }

        _bindings[index] = new RuntimeBinding
        {
            root = root,
            runtimeComponents = runtimeComponents
        };

        InitializeRuntimeComponents(skill, runtimeComponents);

        GameLogger.Log(
            $"[CommonSkillManager2D] 스킬 획득: {skill.kind} " +
            $"(weaponObj={root.name}, components={runtimeComponents.Length})",
            this);

        return true;
    }

    private MonoBehaviour[] CollectRuntimeComponents(GameObject root)
    {
        if (root == null)
            return Array.Empty<MonoBehaviour>();

        MonoBehaviour[] all = root.GetComponentsInChildren<MonoBehaviour>(true);
        List<MonoBehaviour> found = new List<MonoBehaviour>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour component = all[i];
            if (component == null)
                continue;

            if (component is CommonSkillWeapon2D || component is ILevelableSkill)
                found.Add(component);
        }

        return found.ToArray();
    }

    private void InitializeRuntimeComponents(CommonSkillConfigSO skill, MonoBehaviour[] runtimeComponents)
    {
        for (int i = 0; i < runtimeComponents.Length; i++)
        {
            MonoBehaviour component = runtimeComponents[i];
            if (component == null)
                continue;

            if (component is CommonSkillWeapon2D commonWeapon)
            {
                commonWeapon.Initialize(skill, owner, 1);
                continue;
            }

            if (component is ILevelableSkill levelable)
            {
                levelable.OnAttached(owner);
                levelable.ApplyLevel(1);
            }
        }
    }

    private void ApplyRuntimeLevel(CommonSkillConfigSO skill, int index, int level)
    {
        RuntimeBinding binding = _bindings[index];
        if (binding == null || binding.runtimeComponents == null)
            return;

        for (int i = 0; i < binding.runtimeComponents.Length; i++)
        {
            MonoBehaviour component = binding.runtimeComponents[i];
            if (component == null)
                continue;

            if (component is CommonSkillWeapon2D commonWeapon)
            {
                commonWeapon.SetOwner(owner);
                commonWeapon.ApplyLevel(level);
                continue;
            }

            if (component is ILevelableSkill levelable)
            {
                levelable.ApplyLevel(level);
            }
        }

        GameLogger.Log($"[CommonSkillManager2D] 레벨 반영: {skill.kind} -> Lv.{level}", this);
    }
}