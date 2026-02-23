using System;
using UnityEngine;

/// <summary>
/// 공통 스킬 런타임 관리자.
///
/// 역할
/// - 스킬 레벨(1~8) 상태를 보관
/// - 첫 획득 시 weaponPrefab 스폰 후 Initialize
/// - 업그레이드 시 무기 파라미터 갱신
///
/// 이벤트(델리게이트)
/// - UI/로그/튜토리얼 등이 결합하지 않도록 이벤트로만 신호를 보낸다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CommonSkillManager2D : MonoBehaviour
{
    [Header("카탈로그")]
    [SerializeField] private CommonSkillCatalogSO catalog;

    [Header("오너")]
    [SerializeField] private Transform owner;
    [SerializeField] private bool spawnAsChild = true;

    // kind -> weapon instance
    private CommonSkillWeapon2D[] _weapons;
    // kind -> level (0 = 미보유)
    private int[] _levels;

    public event Action<CommonSkillKind> OnSkillAcquired;
    public event Action<CommonSkillKind, int> OnSkillLevelChanged;

    public CommonSkillCatalogSO Catalog => catalog;

    private void Awake()
    {
        if (owner == null) owner = transform;

        int kindCount = Enum.GetValues(typeof(CommonSkillKind)).Length;
        _weapons = new CommonSkillWeapon2D[kindCount];
        _levels = new int[kindCount];
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
        int max = Mathf.Clamp(skill.maxLevel, 1, CommonSkillConfigSO.HardMaxLevel);
        return cur >= max;
    }

    /// <summary>
    /// 스킬을 1레벨로 획득하거나, 이미 있으면 +1 업그레이드.
    /// </summary>
    public void Upgrade(CommonSkillConfigSO skill)
    {
        if (skill == null) return;

        int idx = (int)skill.kind;
        if (_levels == null || idx < 0 || idx >= _levels.Length) return;

        int max = Mathf.Clamp(skill.maxLevel, 1, CommonSkillConfigSO.HardMaxLevel);

        int cur = _levels[idx];
        if (cur >= max)
            return;

        int next = Mathf.Clamp(cur + 1, 1, max);
        _levels[idx] = next;

        bool isFirstAcquire = (cur == 0 && next == 1);

        // 첫 획득이면 프리팹 스폰/연결
        if (_weapons[idx] == null)
        {
            if (skill.weaponPrefab == null)
            {
                Debug.LogWarning($"[CommonSkillManager2D] weaponPrefab 누락: {skill.name}", this);
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

            // GetComponentInChildren(true): 루트뿐 아니라 자식 오브젝트도 탐색
            // → 프리팹 루트가 아닌 자식에 CommonSkillWeapon2D가 붙어있어도 찾음
            var weapon = go.GetComponentInChildren<CommonSkillWeapon2D>(true);
            if (weapon == null)
            {
                Debug.LogWarning(
                    $"[CommonSkillManager2D] weaponPrefab 루트/자식 어디에도 CommonSkillWeapon2D 없음: {skill.weaponPrefab.name}" +
                    $" → 프리팹에 스크립트가 있는지 확인하세요.", this);
                Destroy(go);
                return;
            }

            Debug.Log(
                $"[CommonSkillManager2D] 스킬 획득: {skill.kind} (weaponObj={weapon.gameObject.name}, prefab루트={go.name})",
                this);

            weapon.Initialize(skill, owner, next);
            _weapons[idx] = weapon;
        }
        else
        {
            _weapons[idx].SetOwner(owner);
            _weapons[idx].Initialize(skill, owner, next);
        }

        if (isFirstAcquire)
            OnSkillAcquired?.Invoke(skill.kind);

        OnSkillLevelChanged?.Invoke(skill.kind, next);
    }
}