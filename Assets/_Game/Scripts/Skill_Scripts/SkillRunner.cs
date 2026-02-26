// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "프리팹 장착"과 "레벨 적용"만 담당(SRP).
/// - 런타임 상태/오퍼/UI/시간정지 등은 책임 없음
/// </summary>
[DisallowMultipleComponent]
public sealed class SkillRunner : MonoBehaviour
{
    [Header("오너(플레이어)")]
    [Tooltip("비우면 Awake에서 PlayerExp(있으면) -> self 순으로 찾습니다.")]
    [SerializeField] private Transform owner;

    [Header("스킬 장착 위치")]
    [Tooltip("비우면 owner 아래에 SkillMount를 자동 생성/탐색")]
    [SerializeField] private Transform mount;

    [SerializeField] private bool autoCreateMount = true;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    // id -> (해당 프리팹 안의 모든 ILevelableSkill)
    private readonly Dictionary<string, List<ILevelableSkill>> _instances = new Dictionary<string, List<ILevelableSkill>>(32);

    private void Awake()
    {
        if (owner == null)
        {
            var pe = FindFirstObjectByType<PlayerExp>();
            owner = pe != null ? pe.transform : transform;
        }

        EnsureMount();
    }

    private void EnsureMount()
    {
        if (mount != null) return;

        var found = owner != null ? owner.Find("SkillMount") : null;
        if (found != null)
        {
            mount = found;
            return;
        }

        if (!autoCreateMount || owner == null) return;

        var go = new GameObject("SkillMount");
        go.transform.SetParent(owner, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        mount = go.transform;

        if (enableLogs)
            Debug.Log("[SkillRunner] SkillMount 자동 생성", this);
    }

    public void AttachSkillPrefab(string id, GameObject prefab)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (prefab == null) return;

        if (_instances.ContainsKey(id))
            return;

        EnsureMount();

        Transform parent = (mount != null) ? mount : owner;
        if (parent == null)
        {
            Debug.LogError($"[SkillRunner] owner/mount가 null이라 '{id}' 장착 불가", this);
            return;
        }

        var go = Instantiate(prefab, parent);
        go.name = id;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 같은 프리팹 안에 여러 ILevelableSkill이 있을 수 있음(무기 + 패시브 조합 등)
        var list = new List<ILevelableSkill>(4);

        // 루트 우선(있으면 1순위로 앞에 넣기)
        var rootBehaviours = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < rootBehaviours.Length; i++)
        {
            if (rootBehaviours[i] is ILevelableSkill s)
                list.Add(s);
        }

        // 자식 포함 전체 수집(중복 방지)
        var all = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is not ILevelableSkill s) continue;

            bool dup = false;
            for (int k = 0; k < list.Count; k++)
            {
                if (ReferenceEquals(list[k], s)) { dup = true; break; }
            }
            if (!dup) list.Add(s);
        }

        if (list.Count <= 0)
        {
            Debug.LogError($"[SkillRunner] ILevelableSkill 없음: {id} (루트/자식 검색 실패)", go);
            Destroy(go);
            return;
        }

        _instances.Add(id, list);

        // 장착 콜백(모두)
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            list[i].OnAttached(owner);
        }

        if (enableLogs)
            Debug.Log($"[SkillRunner] Attach '{id}' (components={list.Count})", this);
    }

    public void ApplyLevel(string id, int level)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (level <= 0) return;

        if (!_instances.TryGetValue(id, out var list) || list == null || list.Count <= 0)
        {
            if (enableLogs)
                Debug.LogWarning($"[SkillRunner] ApplyLevel 무시(미장착): '{id}'", this);
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            list[i].ApplyLevel(level);
        }
    }

    public bool IsAttached(string id) => !string.IsNullOrWhiteSpace(id) && _instances.ContainsKey(id);
}
