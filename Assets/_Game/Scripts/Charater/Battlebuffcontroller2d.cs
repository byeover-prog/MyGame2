using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 캐릭터에게 시간제 버프를 적용·관리하는 컨트롤러입니다.
/// Player 오브젝트에 부착합니다.
///
/// [동작 원리]
/// 1. ApplyBuff()가 호출되면 버프를 즉시 적용하고 코루틴으로 지속시간 카운트
/// 2. 지속시간 종료 시 버프를 되돌림
/// 3. 같은 종류의 버프가 중복 시 기존 버프를 갱신(지속시간 리셋)
///
/// [Hierarchy / Inspector 설정]
/// - Player 오브젝트에 컴포넌트 부착
/// - Combat Stats: PlayerCombatStats2D 자동 탐색 (같은 오브젝트 또는 부모)
/// - Stat Applier: PlayerStatRuntimeApplier2D 자동 탐색
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleBuffController2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("전투 스탯 컴포넌트입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private PlayerCombatStats2D combatStats;

    [Tooltip("스탯 적용기입니다. 버프 해제 시 원래 스탯을 복원하는 데 사용합니다.")]
    [SerializeField] private PlayerStatRuntimeApplier2D statApplier;

    [Header("디버그")]
    [Tooltip("버프 적용/해제 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    /// <summary>현재 활성화된 버프 목록입니다.</summary>
    private readonly Dictionary<SupportBuffKind2D, ActiveBuff> _activeBuffs
        = new Dictionary<SupportBuffKind2D, ActiveBuff>(4);

    /// <summary>활성 버프 하나의 정보입니다.</summary>
    private sealed class ActiveBuff
    {
        public SupportBuffKind2D kind;
        public float value;
        public float remainingTime;
        public Coroutine coroutine;
    }

    // ─── 공개 API ──────────────────────────────────────

    /// <summary>
    /// 지원 버프를 메인 캐릭터에게 적용합니다.
    /// SupportUltimateController2D에서 지원 궁극기 시전 시 호출합니다.
    /// </summary>
    /// <param name="kind">버프 종류</param>
    /// <param name="value">버프 수치 (%, 고정값 등)</param>
    /// <param name="duration">지속시간(초)</param>
    public void ApplyBuff(SupportBuffKind2D kind, float value, float duration)
    {
        if (kind == SupportBuffKind2D.None || duration <= 0f) return;

        EnsureTargets();

        // 이미 같은 버프가 활성화되어 있으면 제거 후 갱신
        if (_activeBuffs.TryGetValue(kind, out ActiveBuff existing))
        {
            RemoveBuffEffect(existing);
            if (existing.coroutine != null)
                StopCoroutine(existing.coroutine);
            _activeBuffs.Remove(kind);
        }

        // 버프 효과 적용
        ActiveBuff buff = new ActiveBuff
        {
            kind = kind,
            value = value,
            remainingTime = duration,
        };

        ApplyBuffEffect(buff);
        buff.coroutine = StartCoroutine(BuffTimerRoutine(buff));
        _activeBuffs[kind] = buff;

        if (debugLog)
            Debug.Log($"[BattleBuffController] 버프 적용 — {kind} +{value} ({duration}초)");
    }

    /// <summary>
    /// 특정 버프를 즉시 해제합니다.
    /// </summary>
    public void RemoveBuff(SupportBuffKind2D kind)
    {
        if (!_activeBuffs.TryGetValue(kind, out ActiveBuff buff)) return;

        RemoveBuffEffect(buff);
        if (buff.coroutine != null)
            StopCoroutine(buff.coroutine);
        _activeBuffs.Remove(kind);

        if (debugLog)
            Debug.Log($"[BattleBuffController] 버프 해제 — {kind}");
    }

    /// <summary>
    /// 모든 활성 버프를 즉시 해제합니다.
    /// </summary>
    public void RemoveAllBuffs()
    {
        foreach (var kvp in _activeBuffs)
        {
            RemoveBuffEffect(kvp.Value);
            if (kvp.Value.coroutine != null)
                StopCoroutine(kvp.Value.coroutine);
        }
        _activeBuffs.Clear();

        if (debugLog)
            Debug.Log("[BattleBuffController] 모든 버프 해제");
    }

    /// <summary>현재 활성화된 버프가 있는지 확인합니다.</summary>
    public bool HasBuff(SupportBuffKind2D kind)
    {
        return _activeBuffs.ContainsKey(kind);
    }

    /// <summary>특정 버프의 남은 시간을 반환합니다. 없으면 0입니다.</summary>
    public float GetRemainingTime(SupportBuffKind2D kind)
    {
        return _activeBuffs.TryGetValue(kind, out ActiveBuff buff) ? buff.remainingTime : 0f;
    }

    // ─── 버프 효과 적용/해제 ──────────────────────────

    /// <summary>버프 효과를 PlayerCombatStats2D에 즉시 적용합니다.</summary>
    private void ApplyBuffEffect(ActiveBuff buff)
    {
        if (combatStats == null) return;

        switch (buff.kind)
        {
            case SupportBuffKind2D.AttackPowerPercent:
                // 현재 DamageMul에 버프 배율을 곱합니다.
                // 예: 공격력 +30% → DamageMul × 1.3
                float atkMultiplier = 1f + (buff.value / 100f);
                combatStats.SetDamageMul(combatStats.DamageMul * atkMultiplier);
                break;

            case SupportBuffKind2D.SkillHasteFlat:
                // 스킬 가속 +60 → 쿨타임 배율 재계산
                // 현재 CooldownMul에서 추가 가속을 반영합니다.
                // 공식: 새 쿨타임 = 현재 쿨타임 × 100 / (100 + 추가가속)
                float hasteMul = 100f / (100f + buff.value);
                combatStats.SetCooldownMul(combatStats.CooldownMul * hasteMul);
                break;

            case SupportBuffKind2D.LifestealPercent:
                // 흡혈 % 추가
                combatStats.SetLifestealPercent(combatStats.LifestealPercent + buff.value);
                break;
        }
    }

    /// <summary>버프 효과를 되돌립니다. 정확한 역연산 대신 ReapplyFromLoadout()으로 전체 재계산합니다.</summary>
    private void RemoveBuffEffect(ActiveBuff buff)
    {
        if (buff == null) return;

        // 버프 해제 시 스탯을 원래 값으로 복원하는 가장 안전한 방법:
        // PlayerStatRuntimeApplier2D.ReapplyFromLoadout()으로 전체 재계산
        if (statApplier != null)
        {
            statApplier.ReapplyFromLoadout();
        }
        else
        {
            // statApplier가 없으면 역연산으로 복원 시도
            if (combatStats == null) return;

            switch (buff.kind)
            {
                case SupportBuffKind2D.AttackPowerPercent:
                    float atkDiv = 1f + (buff.value / 100f);
                    if (atkDiv > 0.001f)
                        combatStats.SetDamageMul(combatStats.DamageMul / atkDiv);
                    break;

                case SupportBuffKind2D.SkillHasteFlat:
                    float hasteDiv = 100f / (100f + buff.value);
                    if (hasteDiv > 0.001f)
                        combatStats.SetCooldownMul(combatStats.CooldownMul / hasteDiv);
                    break;

                case SupportBuffKind2D.LifestealPercent:
                    combatStats.SetLifestealPercent(combatStats.LifestealPercent - buff.value);
                    break;
            }
        }
    }

    // ─── 타이머 ────────────────────────────────────────

    /// <summary>버프 지속시간이 끝나면 자동 해제하는 코루틴입니다.</summary>
    private IEnumerator BuffTimerRoutine(ActiveBuff buff)
    {
        while (buff.remainingTime > 0f)
        {
            buff.remainingTime -= Time.deltaTime;
            yield return null;
        }

        buff.remainingTime = 0f;

        // 해제 시 다른 활성 버프에 영향을 주지 않도록 개별 처리
        if (_activeBuffs.ContainsKey(buff.kind))
        {
            _activeBuffs.Remove(buff.kind);

            // 전체 스탯 재계산으로 깔끔하게 복원
            if (statApplier != null)
                statApplier.ReapplyFromLoadout();

            // 재계산 후 아직 활성인 다른 버프들을 다시 적용
            ReapplyRemainingBuffs();

            if (debugLog)
                Debug.Log($"[BattleBuffController] 버프 만료 — {buff.kind} ({buff.value})");
        }
    }

    /// <summary>
    /// 스탯 재계산(ReapplyFromLoadout) 후 아직 남아있는 버프들을 다시 적용합니다.
    /// </summary>
    private void ReapplyRemainingBuffs()
    {
        foreach (var kvp in _activeBuffs)
        {
            ApplyBuffEffect(kvp.Value);
        }
    }

    // ─── 내부 ──────────────────────────────────────────

    private void Awake()
    {
        EnsureTargets();
    }

    private void EnsureTargets()
    {
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = FindFirstObjectByType<PlayerCombatStats2D>();

        if (statApplier == null) statApplier = GetComponent<PlayerStatRuntimeApplier2D>();
        if (statApplier == null) statApplier = GetComponentInParent<PlayerStatRuntimeApplier2D>();
        if (statApplier == null) statApplier = FindFirstObjectByType<PlayerStatRuntimeApplier2D>();
    }

    private void OnDisable()
    {
        // 씬 전환 등으로 비활성화 시 모든 버프 정리
        foreach (var kvp in _activeBuffs)
        {
            if (kvp.Value.coroutine != null)
                StopCoroutine(kvp.Value.coroutine);
        }
        _activeBuffs.Clear();
    }

    // ─── 디버그 ────────────────────────────────────────

    [ContextMenu("디버그: 공격력 +30% (10초)")]
    public void DebugApplyAttackBuff()
    {
        ApplyBuff(SupportBuffKind2D.AttackPowerPercent, 30f, 10f);
    }

    [ContextMenu("디버그: 스킬가속 +60 (10초)")]
    public void DebugApplyHasteBuff()
    {
        ApplyBuff(SupportBuffKind2D.SkillHasteFlat, 60f, 10f);
    }

    [ContextMenu("디버그: 흡혈 +10% (10초)")]
    public void DebugApplyLifestealBuff()
    {
        ApplyBuff(SupportBuffKind2D.LifestealPercent, 10f, 10f);
    }

    [ContextMenu("디버그: 모든 버프 해제")]
    public void DebugRemoveAllBuffs()
    {
        RemoveAllBuffs();
    }
}