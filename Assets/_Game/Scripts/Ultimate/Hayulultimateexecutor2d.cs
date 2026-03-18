// UTF-8
// Assets/_Game/Scripts/Ultimate/Hayul/HayulUltimateExecutor2D.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 하율 궁극기 실행 흐름: 연출 시작 → 반복 피해+전파 → 종료.
///
/// [v2 수정 사항]
/// 1. duration 기본값 4.5초
/// 2. SpawnTalismanWave() 호출 제거 (부적 VFX 삭제됨)
/// 3. 2회차 실행 보장 — 이전 루틴 강제 정리 후 새로 시작
/// </summary>
[DisallowMultipleComponent]
public sealed class HayulUltimateExecutor2D : MonoBehaviour
{
    [Header("타이밍")]
    [SerializeField, Tooltip("궁극기 총 지속 시간(초)")]
    private float duration = 4.5f;

    [SerializeField, Tooltip("첫 피해까지 대기 시간(초). 연출 여유용.")]
    private float hitDelay = 0.5f;

    [SerializeField, Tooltip("피해 적용 간격(초). 지속 시간 동안 반복.")]
    private float hitInterval = 0.8f;

    [Header("참조")]
    [SerializeField, Tooltip("연출 담당")]
    private HayulUltimatePresenter2D presenter;

    [SerializeField, Tooltip("판정 담당")]
    private HayulUltimateHitResolver2D hitResolver;

    private Action _onFinished;
    private Coroutine _routine;

    private void Awake()
    {
        if (presenter == null)
            presenter = GetComponentInChildren<HayulUltimatePresenter2D>();
        if (hitResolver == null)
            hitResolver = GetComponentInChildren<HayulUltimateHitResolver2D>();
    }

    /// <summary>
    /// 궁극기 실행. UltimateController2D에서 호출.
    /// 2회차 이상도 정상 동작하도록 이전 루틴을 강제 정리한다.
    /// </summary>
    public void Execute(Action onFinished)
    {
        _onFinished = onFinished;

        // ★ 이전 루틴이 남아있으면 강제 종료 + 연출 정리
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;

            // 이전 연출이 남아있을 수 있으니 정리
            if (presenter != null)
                presenter.EndPresentation();
        }

        _routine = StartCoroutine(ExecuteRoutine());
    }

    private IEnumerator ExecuteRoutine()
    {
        // 1. 연출 시작 (풀스크린 VFX — 카메라 기준)
        if (presenter != null)
            presenter.BeginPresentation(duration);

        // 2. 첫 피해 대기
        yield return new WaitForSeconds(hitDelay);

        // 3. 지속 시간 동안 반복 피해 + 전파
        float elapsed = hitDelay;
        while (elapsed < duration)
        {
            if (hitResolver != null)
                hitResolver.ResolveHit();

            yield return new WaitForSeconds(hitInterval);
            elapsed += hitInterval;
        }

        // 4. 종료
        if (presenter != null)
            presenter.EndPresentation();

        _routine = null;
        _onFinished?.Invoke();
    }
}