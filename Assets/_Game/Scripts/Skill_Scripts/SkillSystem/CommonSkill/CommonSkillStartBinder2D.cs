// UTF-8
using System.Collections;
using UnityEngine;

// [구현 원리 요약]
// - 시작 스킬은 "런 시작 시점"에 1번 적용하는 게 의도.
// - 리트라이/씬 재로드/타임스케일 꼬임으로 Apply가 스킵될 수 있어,
//   StageStarted(런 시작 신호)에서 강제로 재적용 시도를 허용한다.
[DisallowMultipleComponent]
public sealed class CommonSkillStartBinder2D : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private CommonSkillManager2D manager;

    [Header("시작 스킬 목록(런 시작 시 1회 적용)")]
    [SerializeField] private CommonSkillConfigSO[] startSkills;

    [Header("동작 옵션")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool warnWhenEmpty = true;

    [Header("디버그")]
    [SerializeField] private bool logOnStart = false;

    private bool _appliedThisRun;

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
        // 런 시작 신호가 오면, 이번 런에서 다시 적용 가능하도록 리셋 후 적용 시도
        _appliedThisRun = false;
        StartCoroutine(ApplyRoutine("StageStarted"));
    }

    private void OnPlayerDead()
    {
        // 사망 후 다음 런에서 다시 적용되도록 플래그 리셋
        _appliedThisRun = false;
    }

    private IEnumerator ApplyRoutine(string from)
    {
        if (_appliedThisRun) yield break;

        // 매니저가 Awake/Indexing 끝날 때까지 1프레임 기다리기(실전에서 꽤 중요)
        yield return null;

        if (manager == null)
        {
            if (warnWhenEmpty)
                Debug.LogWarning($"[CommonSkillStartBinder2D] Manager가 비어있음(from={from})", this);
            yield break;
        }

        if (startSkills == null || startSkills.Length == 0)
        {
            if (warnWhenEmpty)
                Debug.LogWarning($"[CommonSkillStartBinder2D] StartSkills가 비어있음(from={from})", this);
            yield break;
        }

        _appliedThisRun = true;

        if (logOnStart)
            Debug.Log($"[CommonSkillStartBinder2D] 시작 스킬 적용(from={from}) count={startSkills.Length}", this);

        for (int i = 0; i < startSkills.Length; i++)
        {
            var cfg = startSkills[i];
            if (cfg == null) continue;
            manager.Upgrade(cfg); // 너 프로젝트 기존 흐름 유지
        }
    }
}