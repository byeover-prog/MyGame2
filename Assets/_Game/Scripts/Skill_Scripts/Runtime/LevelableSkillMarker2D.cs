// UTF-8
// 요약: SkillRunner가 찾는 ILevelableSkill 어댑터.
// - 무기/투사체 어디에 붙어도 "장착됨"을 만족시킴
// - 옵션으로 하위 컴포넌트에 ApplyLevel을 전달(전용 무기 스크립트가 ApplyLevel/SetLevel 등을 갖고 있으면 자동 호출)
//
// ★ 수정 내역:
// OnAttached(Transform owner) 호출 시 하위 컴포넌트에도 포워딩 추가.
// 이전에는 ApplyLevel만 포워딩하고 OnAttached는 Marker 혼자 먹고 끝냈음.
// → CommonSkillWeapon2D 계열(OrbitingBladeWeapon2D 등)의 Initialize()가
//   영원히 호출되지 않아서 owner=null → Update에서 매 프레임 return → 무기 동작 안 함.

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
    [Tooltip("켜면 ApplyLevel/OnAttached가 들어올 때 하위 컴포넌트에도 전달합니다.")]
    [SerializeField] private bool forwardLevelToChildren = true;

    [Tooltip("전달 대상 메서드 이름 후보(프로젝트 무기 스크립트마다 이름이 다를 수 있어서 여러 개 시도)")]
    [SerializeField] private string[] forwardMethodNames =
    {
        "ApplyLevel", "SetLevel", "SetSkillLevel", "UpgradeLevel", "OnLevelChanged"
    };

    [Header("디버그")]
    [SerializeField] private bool log = true;

    private Transform _owner;

    // ══════════════════════════════════════════════════════════════
    // ILevelableSkill 구현
    // ══════════════════════════════════════════════════════════════

    public void OnAttaced(Transform owner)
    {
        OnAttached(owner);
    }

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (log) GameLogger.Log($"[LevelableSkillMarker2D] OnAttached: {debugSkillId}", this);

        // ★ 핵심 수정: 하위 컴포넌트에도 OnAttached 포워딩
        if (forwardLevelToChildren)
            ForwardAttached(owner);
    }

    public void ApplyLevel(int newLevel)
    {
        int clamped = Mathf.Clamp(newLevel, 1, maxLevel);
        level = clamped;

        if (log) GameLogger.Log($"[LevelableSkillMarker2D] ApplyLevel: {debugSkillId} => Lv.{level}", this);

        if (forwardLevelToChildren)
            ForwardLevel(level);
    }

    // ══════════════════════════════════════════════════════════════
    // ★ 신규: OnAttached(Transform) 포워딩
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 하위 MonoBehaviour 중 OnAttached(Transform) 또는 OnAttaced(Transform)를
    /// 가진 컴포넌트에 owner를 전달한다.
    ///
    /// [설계 이유]
    /// CommonSkillWeapon2D.OnAttached(Transform)는 내부에서 Initialize()를 호출해
    /// owner, config, level 등을 세팅한다.
    /// 이 호출이 빠지면 owner=null → Update 매 프레임 return → 무기 동작 안 함.
    /// </summary>
    private void ForwardAttached(Transform owner)
    {
        var all = GetComponentsInChildren<MonoBehaviour>(true);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // OnAttached / OnAttaced (오타 호환) 둘 다 시도
        string[] attachMethodNames = { "OnAttached", "OnAttaced" };

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;
            if (mb == this) continue;
            if (mb is LevelableSkillMarker2D) continue;

            var t = mb.GetType();

            for (int n = 0; n < attachMethodNames.Length; n++)
            {
                var m = t.GetMethod(attachMethodNames[n], flags);
                if (m == null) continue;

                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(Transform))
                {
                    try
                    {
                        m.Invoke(mb, new object[] { owner });
                        if (log) GameLogger.Log($"[LevelableSkillMarker2D] Forward => {t.Name}.{m.Name}(owner={owner?.name})", mb);
                        break; // 한 컴포넌트당 1회만
                    }
                    catch (System.Exception e)
                    {
                        GameLogger.LogWarning($"[LevelableSkillMarker2D] Forward 실패: {t.Name}.{m.Name} → {e.Message}", mb);
                    }
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 기존: ApplyLevel(int) 포워딩
    // ══════════════════════════════════════════════════════════════

    private void ForwardLevel(int lv)
    {
        var all = GetComponentsInChildren<MonoBehaviour>(true);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;
            if (mb == this) continue;
            if (mb is LevelableSkillMarker2D) continue;

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
                        if (log) GameLogger.Log($"[LevelableSkillMarker2D] Forward => {t.Name}.{m.Name}({lv})", mb);
                        break;
                    }
                    catch
                    {
                        // 전달 실패는 무시(스킬은 계속 진행)
                    }
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