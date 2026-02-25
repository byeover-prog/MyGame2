// UTF-8
// 요약(의도): 런 시작 시 "시작 스킬"을 1회만 적용한다.
// - 시작 스킬은 여기서만 주고(=고정 시작 로드아웃), 레벨업 후보(카탈로그/카드풀/덱/트랙)에서는 제외한다.
// - startSkills가 비어있어도 경고로 도배하지 않도록 옵션 제공
// - manager가 비어있으면 자동 탐색
// - 디버깅을 위한 수동 적용(컨텍스트 메뉴) 제공

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillStartBinder2D : MonoBehaviour
{
    [Header("연결")]
    [SerializeField, Tooltip("시작 스킬을 적용할 CommonSkillManager2D. 비우면 씬에서 자동으로 찾습니다.")]
    private CommonSkillManager2D manager;

    [Header("시작 스킬 목록(런 시작 시 1회 적용)")]
    [SerializeField, Tooltip("여기에 넣은 스킬은 '시작 고정'으로 1회만 적용됩니다. 레벨업 후보(카드풀/덱)에는 넣지 마세요.")]
    private List<CommonSkillConfigSO> startSkills = new List<CommonSkillConfigSO>();

    [Header("동작 옵션")]
    [SerializeField, Tooltip("켜져 있으면 Start에서 자동 1회 적용합니다.")]
    private bool applyOnStart = true;

    [SerializeField, Tooltip("startSkills가 비어있을 때 경고 로그를 띄울지 여부입니다. (시작 스킬 없는 캐릭터도 허용하려면 끄세요)")]
    private bool warnWhenEmpty = false;

    [Header("디버그")]
    [SerializeField, Tooltip("시작 적용 로그를 출력합니다.")]
    private bool logOnStart = true;

    // 런타임 1회 적용 보장(씬이 다시 Enable/Disable 되더라도 중복 적용 방지)
    private bool _applied;

    private void Awake()
    {
        // 매니저 자동 연결(실수 방지)
        if (manager == null)
            manager = FindFirstObjectByType<CommonSkillManager2D>();
    }

    private void Start()
    {
        if (applyOnStart)
            ApplyOnce();
    }

    /// <summary>
    /// 시작 스킬을 1회 적용(중복 적용 방지)
    /// </summary>
    public void ApplyOnce()
    {
        if (_applied) return;
        _applied = true;

        if (manager == null)
        {
            if (logOnStart) Debug.LogError("[CommonSkillStartBinder2D] manager가 비어있음(자동 탐색 실패).", this);
            return;
        }

        if (startSkills == null || startSkills.Count == 0)
        {
            if (warnWhenEmpty && logOnStart)
                Debug.LogWarning("[CommonSkillStartBinder2D] startSkills가 비어있음(시작 스킬 없음).", this);
            return;
        }

        for (int i = 0; i < startSkills.Count; i++)
        {
            var cfg = startSkills[i];
            if (cfg == null) continue;

            // 시작 고정 스킬 지급(여기서만 지급한다)
            manager.Upgrade(cfg);

            if (logOnStart)
                Debug.Log($"[CommonSkillStartBinder2D] StartSkill applied: {cfg.kind} ({cfg.displayName})", this);
        }
    }

    /// <summary>
    /// 테스트용: 에디터에서 우클릭으로 수동 적용
    /// </summary>
    [ContextMenu("시작 스킬 1회 적용(테스트)")]
    private void ContextApplyOnce()
    {
        ApplyOnce();
    }

    /// <summary>
    /// 테스트용: 다시 적용하고 싶을 때(플레이 중 디버그)
    /// </summary>
    [ContextMenu("적용 플래그 리셋(테스트)")]
    private void ContextResetApplied()
    {
        _applied = false;
        if (logOnStart) Debug.Log("[CommonSkillStartBinder2D] _applied 리셋(테스트).", this);
    }
}