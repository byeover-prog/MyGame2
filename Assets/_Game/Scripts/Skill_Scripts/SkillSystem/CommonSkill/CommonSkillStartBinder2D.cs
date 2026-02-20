using System.Collections.Generic;
using UnityEngine;

public sealed class CommonSkillStartBinder2D : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private CommonSkillManager2D manager;

    [Header("시작 스킬 목록(런 시작 시 1회 업그레이드 적용)")]
    [SerializeField] private List<CommonSkillConfigSO> startSkills = new List<CommonSkillConfigSO>();

    [Header("디버그")]
    [SerializeField] private bool logOnStart = true;

    private bool _applied;

    private void Start()
    {
        ApplyOnce();
    }

    private void ApplyOnce()
    {
        if (_applied) return;
        _applied = true;

        if (manager == null)
        {
            if (logOnStart) Debug.LogError("[CommonSkillStartBinder2D] manager가 비어있음", this);
            return;
        }

        if (startSkills == null || startSkills.Count == 0)
        {
            if (logOnStart) Debug.LogWarning("[CommonSkillStartBinder2D] startSkills가 비어있음", this);
            return;
        }

        for (int i = 0; i < startSkills.Count; i++)
        {
            var cfg = startSkills[i];
            if (cfg == null) continue;

            manager.Upgrade(cfg);

            if (logOnStart)
                Debug.Log($"[CommonSkillStartBinder2D] StartSkill applied: {cfg.kind} ({cfg.displayName})", this);
        }
    }
}