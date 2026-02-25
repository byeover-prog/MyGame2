// UTF-8
// 요약: SkillRunner가 찾는 ILevelableSkill 어댑터.
// - 무기/투사체 어디에 붙어도 "장착됨"을 만족시킴
// - 옵션으로 하위 컴포넌트에 ApplyLevel을 전달(전용 무기 스크립트가 ApplyLevel/SetLevel 등을 갖고 있으면 자동 호출)

using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelableSkillMarker2D : MonoBehaviour, ILevelableSkill
{
    [Header("디버그 식별자(선택)")]
    [SerializeField] private string debugSkillId;

    [Header("레벨")]
    [SerializeField] private int maxLevel = 8;
    [SerializeField] private int level = 0;

    [Header("전달(중요)")]
    [Tooltip("켜면 ApplyLevel이 들어올 때 하위 컴포넌트에도 레벨을 전달합니다.")]
    [SerializeField] private bool forwardLevelToChildren = true;

    [Tooltip("전달 대상 메서드 이름 후보(프로젝트 무기 스크립트마다 이름이 다를 수 있어서 여러 개 시도)")]
    [SerializeField] private string[] forwardMethodNames = new[]
    {
        "ApplyLevel", "SetLevel", "SetSkillLevel", "UpgradeLevel", "OnLevelChanged"
    };

    [Header("디버그")]
    [SerializeField] private bool log = true;

    private Transform _owner;

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (log) Debug.Log($"[LevelableSkillMarker2D] OnAttached: {debugSkillId}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        int clamped = Mathf.Clamp(newLevel, 1, maxLevel);
        level = clamped;

        if (log) Debug.Log($"[LevelableSkillMarker2D] ApplyLevel: {debugSkillId} => Lv.{level}", this);

        if (forwardLevelToChildren)
            ForwardLevel(level);
    }

    private void ForwardLevel(int lv)
    {
        // 자기 자신 제외하고, 같은 오브젝트/자식의 MonoBehaviour들 중
        // int 1개 받는 메서드를 찾으면 호출
        var all = GetComponentsInChildren<MonoBehaviour>(true);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;
            if (mb == this) continue;

            var t = mb.GetType();

            for (int n = 0; n < forwardMethodNames.Length; n++)
            {
                var m = t.GetMethod(forwardMethodNames[n], flags);
                if (m == null) continue;

                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
                {
                    try
                    {
                        m.Invoke(mb, new object[] { lv });
                        if (log) Debug.Log($"[LevelableSkillMarker2D] Forward => {t.Name}.{m.Name}({lv})", mb);
                        break;
                    }
                    catch { /* 전달 실패는 무시(스킬은 계속 진행) */ }
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxLevel < 1) maxLevel = 1;
        if (level < 0) level = 0;
        if (level > maxLevel) level = maxLevel;
        if (forwardMethodNames == null || forwardMethodNames.Length == 0)
            forwardMethodNames = new[] { "ApplyLevel", "SetLevel" };
    }
#endif
}