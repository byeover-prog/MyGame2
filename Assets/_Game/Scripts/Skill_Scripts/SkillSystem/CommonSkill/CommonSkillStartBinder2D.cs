// UTF-8
// [구현 원리 요약]
// - 시작 스킬은 런 시작 시 1회만 보장하면 된다.
// - 현재 프로젝트는 Player 프리팹에 Manager가 비어 있어 시작 스킬이 안 나가는 경우가 있으므로,
//   전역 탐색 없이 같은 오브젝트/부모에서 찾고 없으면 경고를 출력한다.
using System.Collections;
using UnityEngine;

/// <summary>
/// 런 시작 시 CommonSkillManager2D에 시작 스킬을 1회 적용하는 바인더.
/// Player 프리팹에 부착하여 사용한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CommonSkillStartBinder2D : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("스킬 매니저 참조 (비우면 자동 탐색)")]
    [SerializeField] private CommonSkillManager2D manager;

    [Header("시작 스킬")]
    [Tooltip("런 시작 시 1회 적용할 스킬 목록")]
    [SerializeField] private CommonSkillConfigSO[] startSkills;

    [Header("동작 옵션")]
    [Tooltip("Start()에서 자동 적용할지 여부")]
    [SerializeField] private bool applyOnStart = true;

    [Tooltip("스킬이 비어있을 때 경고 로그 출력")]
    [SerializeField] private bool warnWhenEmpty = true;

    [Tooltip("매니저를 못 찾았을 때 자동 생성 (주의: Inspector 참조가 비어있는 상태로 생성됨)")]
    [SerializeField] private bool autoCreateManagerIfMissing = false;

    [Header("디버그")]
    [Tooltip("시작 스킬 적용 로그 출력")]
    [SerializeField] private bool logOnStart = false;

    private bool _appliedThisRun;
    private bool _warnedManagerMissing;

    private void OnEnable()
    {
        RunSignals.StageStarted += OnStageStarted;
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.StageStarted -= OnStageStarted;
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    private void Start()
    {
        if (applyOnStart)
            StartCoroutine(ApplyRoutine("Start"));
    }

    private void OnStageStarted()
    {
        _appliedThisRun = false;
        StartCoroutine(ApplyRoutine("StageStarted"));
    }

    private void OnPlayerDead()
    {
        _appliedThisRun = false;
    }

    private void ResolveManager()
    {
        if (manager != null) return;

        manager = GetComponent<CommonSkillManager2D>();
        if (manager != null) return;

        manager = GetComponentInParent<CommonSkillManager2D>();
        if (manager != null) return;

        manager = GetComponentInChildren<CommonSkillManager2D>(true);
        if (manager != null) return;

        if (!autoCreateManagerIfMissing) return;

        manager = gameObject.AddComponent<CommonSkillManager2D>();
        Debug.LogWarning("[CommonSkillStartBinder2D] Manager를 자동 생성했습니다. " +
            "Inspector 참조가 비어있으므로 반드시 확인하세요!", this);
    }

    private IEnumerator ApplyRoutine(string from)
    {
        if (_appliedThisRun) yield break;

        yield return null;

        if (_appliedThisRun) yield break;

        ResolveManager();

        if (manager == null)
        {
            if (warnWhenEmpty && !_warnedManagerMissing)
            {
                _warnedManagerMissing = true;
                Debug.LogWarning($"[CommonSkillStartBinder2D] Manager를 찾지 못했습니다 (from={from})", this);
            }
            yield break;
        }

        if (startSkills == null || startSkills.Length == 0)
        {
            if (warnWhenEmpty)
                Debug.LogWarning($"[CommonSkillStartBinder2D] StartSkills가 비어있음 (from={from})", this);
            yield break;
        }

        _appliedThisRun = true;

        if (logOnStart)
            Debug.Log($"[CommonSkillStartBinder2D] 시작 스킬 적용 (from={from}) count={startSkills.Length}", this);

        for (int i = 0; i < startSkills.Length; i++)
        {
            var cfg = startSkills[i];
            if (cfg == null) continue;
            manager.Upgrade(cfg);
        }
    }
}
