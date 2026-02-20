using UnityEngine;

/// <summary>
/// [시련(Trial) 모드] 킬 수 기반 스폰 배율 스케일러
/// - KillCountSource.KillCount로 현재 킬 수를 읽는다.
/// - 킬 수 구간/곡선에 따라 배율을 계산한다.
/// - EnemySpawner2D에는 SetSpawnRateMultiplier(mult)만 호출한다.
/// 
/// 설계 의도:
/// - EnemySpawner2D 내부 로직을 건드리지 않고, 외부 주입으로 모드 차이를 분리.
/// - 스폰 배율 계산 로직을 스케일러로 봉인.
/// 
/// 디버깅 포인트:
/// - 킬 수가 변할 때만 배율 갱신 (매 프레임 재계산/호출 방지)
/// - 인스펙터에서 구간/배율/상한을 조절 가능
/// </summary>
public sealed class TrialSpawnRateScaler : MonoBehaviour
{
    [Header("레퍼런스")]
    [SerializeField] private EnemySpawner2D spawner;          // 대상 스포너
    [SerializeField] private KillCountSource killSource;      // KillCountSource.KillCount를 읽음

    [Header("킬 기반 스폰 배율 규칙")]
    [Tooltip("N킬마다 배율 1스텝 증가 (예: 25면 25킬마다 증가)")]
    [Min(1)]
    [SerializeField] private int killsPerStep = 25;

    [Tooltip("스텝 1회당 스폰 배율 증가량 (예: 0.10이면 10%씩 증가)")]
    [Min(0f)]
    [SerializeField] private float multiplierPerStep = 0.10f;

    [Tooltip("최소 배율 (보통 1.0)")]
    [Min(0f)]
    [SerializeField] private float minMultiplier = 1.0f;

    [Tooltip("최대 배율 (폭주 방지용 상한)")]
    [Min(0f)]
    [SerializeField] private float maxMultiplier = 3.0f;

    [Header("옵션")]
    [Tooltip("시작 시 1회 강제 반영")]
    [SerializeField] private bool applyOnEnable = true;

    private int _lastKillCount = int.MinValue;
    private float _lastAppliedMultiplier = -1f;

    private void OnEnable()
    {
        if (applyOnEnable)
            ForceApply();
        else
            CacheSnapshotOnly();
    }

    private void Update()
    {
        if (spawner == null || killSource == null)
            return;

        int currentKills = killSource.KillCount;

        // 킬 수 변화가 없으면 아무것도 하지 않음(성능/로그 스팸 방지)
        if (currentKills == _lastKillCount)
            return;

        _lastKillCount = currentKills;

        float mult = ComputeMultiplier(currentKills);

        // 같은 값이면 스포너 호출도 생략 (불필요한 내부 재계산 방지)
        if (ApproximatelySame(mult, _lastAppliedMultiplier))
            return;

        _lastAppliedMultiplier = mult;
        spawner.SetSpawnRateMultiplier(mult);
    }

    /// <summary>
    /// 킬 수 기반 배율 계산:
    /// - step = floor(kills / killsPerStep)
    /// - mult = minMultiplier + step * multiplierPerStep
    /// - [minMultiplier, maxMultiplier]로 clamp
    /// </summary>
    private float ComputeMultiplier(int killCount)
    {
        if (killCount < 0) killCount = 0;

        int step = killCount / killsPerStep;
        float mult = minMultiplier + (step * multiplierPerStep);

        if (maxMultiplier < minMultiplier)
            maxMultiplier = minMultiplier; // 인스펙터 실수 방어

        return Mathf.Clamp(mult, minMultiplier, maxMultiplier);
    }

    private void ForceApply()
    {
        _lastKillCount = int.MinValue;
        _lastAppliedMultiplier = -1f;

        if (spawner == null || killSource == null)
            return;

        int currentKills = killSource.KillCount;
        _lastKillCount = currentKills;

        float mult = ComputeMultiplier(currentKills);
        _lastAppliedMultiplier = mult;
        spawner.SetSpawnRateMultiplier(mult);
    }

    private void CacheSnapshotOnly()
    {
        if (killSource == null)
        {
            _lastKillCount = int.MinValue;
            return;
        }

        _lastKillCount = killSource.KillCount;
        _lastAppliedMultiplier = -1f;
    }

    private bool ApproximatelySame(float a, float b)
    {
        // 배율은 소수점 누적이 있을 수 있어, 아주 작은 오차는 동일로 취급
        return Mathf.Abs(a - b) < 0.0001f;
    }
}
