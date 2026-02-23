using System.Collections.Generic;
using UnityEngine;

public sealed class SkillRunner : MonoBehaviour
{
    [SerializeField] private Transform owner; // 플레이어(없으면 자기 자신)

    private readonly Dictionary<string, MonoBehaviour> _instances = new Dictionary<string, MonoBehaviour>(32);

    private void Awake()
    {
        if (owner == null) owner = transform;
    }

    public void AttachSkillPrefab(string skillId, GameObject prefab)
    {
        if (prefab == null) return;
        if (_instances.ContainsKey(skillId)) return;

        var go = Instantiate(prefab, owner);
        // 스킬 프리팹 루트에 ILevelableSkill 같은 인터페이스를 붙이는 걸 권장
        var levelable = go.GetComponent<ILevelableSkill>();
        if (levelable == null)
        {
            Debug.LogError($"[SkillRunner] 스킬 프리팹에 ILevelableSkill이 없습니다: {skillId}", go);
            Destroy(go);
            return;
        }

        _instances.Add(skillId, levelable as MonoBehaviour);
        levelable.OnAttached(owner);
    }

    public void ApplyLevel(string skillId, int level)
    {
        if (!_instances.TryGetValue(skillId, out var mb)) return;
        var levelable = mb as ILevelableSkill;
        if (levelable == null) return;

        levelable.ApplyLevel(level);
    }
}

public interface ILevelableSkill
{
    void OnAttached(Transform owner);
    void ApplyLevel(int level);
}