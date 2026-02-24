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
    [SerializeField] private Transform owner; // 비우면 Awake에서 self

    [Header("스킬 장착 위치")]
    [Tooltip("비우면 owner 아래에 SkillMount를 자동 생성/탐색")]
    [SerializeField] private Transform mount;

    [SerializeField] private bool autoCreateMount = true;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    private readonly Dictionary<string, ILevelableSkill> _instances = new Dictionary<string, ILevelableSkill>(32);

    private void Awake()
    {
        if (owner == null)
        {
            // 가장 흔한 실수: SkillRunner를 Systems 오브젝트에 붙이고 owner를 안 넣는 경우.
            // PlayerExp를 기준으로 자동 탐색을 한 번 시도한다.
            var pe = FindFirstObjectByType<PlayerExp>();
            owner = pe != null ? pe.transform : transform;
        }
        EnsureMount();
    }

    private void EnsureMount()
    {
        if (mount != null) return;

        // 1) 이름으로 탐색
        var found = owner != null ? owner.Find("SkillMount") : null;
        if (found != null)
        {
            mount = found;
            return;
        }

        // 2) 자동 생성
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
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 루트가 아니라 "자식까지" 검색(프리팹 구조가 제각각이라 이게 안전)
        var levelable = go.GetComponentInChildren<ILevelableSkill>(true);
        if (levelable == null)
        {
            Debug.LogError($"[SkillRunner] ILevelableSkill 없음: {id} (루트/자식 포함 검색 실패)", go);
            Destroy(go);
            return;
        }

        _instances.Add(id, levelable);
        levelable.OnAttached(owner);

        if (enableLogs)
            Debug.Log($"[SkillRunner] Attach '{id}'", this);
    }

    public void ApplyLevel(string id, int level)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (level <= 0) return;

        if (!_instances.TryGetValue(id, out var levelable) || levelable == null)
        {
            if (enableLogs)
                Debug.LogWarning($"[SkillRunner] ApplyLevel 무시(미장착): '{id}'", this);
            return;
        }

        levelable.ApplyLevel(level);
    }

    public bool IsAttached(string id) => !string.IsNullOrWhiteSpace(id) && _instances.ContainsKey(id);
}

public interface ILevelableSkill
{
    void OnAttached(Transform owner);
    void ApplyLevel(int level);
}