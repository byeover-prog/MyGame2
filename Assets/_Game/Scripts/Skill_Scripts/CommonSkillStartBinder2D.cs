using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillStartBinder2D : MonoBehaviour
{
    [Serializable]
    public struct StartSkillEntry
    {
        [Header("스킬 SO(공통 스킬/무기/패시브 등)")]
        public ScriptableObject skill;

        [Header("시작 레벨(1 이상)")]
        [Min(1)] public int startLevel;
    }

    [Header("대상(스킬을 받는 컴포넌트)")]
    [SerializeField] private MonoBehaviour target;

    [Header("시작 스킬 목록")]
    [SerializeField] private StartSkillEntry[] startSkills;

    [Header("적용 타이밍")]
    [SerializeField] private bool applyOnAwake = true;

    [Header("디버그 로그")]
    [SerializeField] private bool verboseLog = true;

    private ISkillGrantReceiver2D _receiver;
    private bool _applied;

    private void Awake()
    {
        _receiver = target as ISkillGrantReceiver2D;

        if (_receiver == null)
            Debug.LogWarning("[CommonSkillStartBinder2D] target이 ISkillGrantReceiver2D를 구현하지 않았음.", this);

        if (applyOnAwake)
            ApplyOnce();
    }

    public void ApplyOnce()
    {
        if (_applied) return;
        _applied = true;

        if (_receiver == null) return;
        if (startSkills == null || startSkills.Length == 0) return;

        int ok = 0;

        for (int i = 0; i < startSkills.Length; i++)
        {
            var e = startSkills[i];
            if (e.skill == null) continue;

            int level = Mathf.Max(1, e.startLevel);

            bool success = _receiver.TryGrantSkill(e.skill, level);
            if (success) ok++;

            if (verboseLog)
                Debug.Log($"[CommonSkillStartBinder2D] {(success ? "OK" : "FAIL")} {e.skill.name} Lv.{level}", this);
        }

        if (verboseLog)
            Debug.Log($"[CommonSkillStartBinder2D] done {ok}/{startSkills.Length}", this);
    }
}