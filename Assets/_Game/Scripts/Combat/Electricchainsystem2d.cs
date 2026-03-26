using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 속성 데미지 적중 시 주변 적에게 자동 전파하는 시스템입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ElectricChainSystem2D : MonoBehaviour
{
    [Header("전파 설정")]
    [Tooltip("대상 1명당 최대 전파 횟수입니다. 하율 패시브가 추가로 증가시킵니다.")]
    [SerializeField] private int baseMaxChain = 3;

    [Tooltip("전파 데미지 비율입니다. 0.15 = 원본의 15%")]
    [SerializeField] private float chainDamageRate = 0.15f;

    [Tooltip("전파 탐색 반경입니다.")]
    [SerializeField] private float searchRadius = 5f;

    [Tooltip("적 레이어 마스크입니다.")]
    [SerializeField] private LayerMask enemyMask;

    [Header("성능 보호")]
    [Tooltip("프레임당 최대 전파 총 횟수입니다. 이 이상은 다음 프레임으로 넘깁니다.")]
    [SerializeField] private int maxChainsPerFrame = 10;

    [Tooltip("같은 소스 적에서 다시 전파가 발생하기까지의 쿨타임(초)입니다.")]
    [SerializeField] private float chainCooldown = 0.5f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════

    /// <summary>적 rootId → 이번 프레임 전파 받은 횟수</summary>
    private readonly Dictionary<int, int> _chainCountMap = new Dictionary<int, int>(32);

    /// <summary>소스 rootId → 마지막 전파 트리거 시각</summary>
    private readonly Dictionary<int, float> _sourceCooldownMap = new Dictionary<int, float>(32);

    private readonly Collider2D[] _searchBuffer = new Collider2D[16];
    private ContactFilter2D _filter;

    /// <summary>이번 프레임 총 전파 횟수</summary>
    private int _chainsThisFrame;
    private int _lastResetFrame = -1;

    private void Awake()
    {
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyMask);
        _filter.useLayerMask = true;
        _filter.useTriggers = true;
    }

    private void OnEnable()
    {
        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;
    }

    private void OnDisable()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;
    }

    // ═══════════════════════════════════════════════════════
    //  이벤트 처리
    // ═══════════════════════════════════════════════════════

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        // 보너스/시너지/전파 데미지에는 반응하지 않음
        if (DamageChainGuard.IsProcessingBonus) return;

        // 전기 속성만
        if (info.Element != DamageElement2D.Electric) return;
        if (info.Target == null) return;
        if (info.Amount <= 0) return;

        // 매 프레임 카운터 리셋
        int currentFrame = Time.frameCount;
        if (_lastResetFrame != currentFrame)
        {
            _chainCountMap.Clear();
            _chainsThisFrame = 0;
            _lastResetFrame = currentFrame;
        }

        // 프레임당 총 전파 제한
        if (_chainsThisFrame >= maxChainsPerFrame) return;

        // 소스별 쿨타임 체크
        int sourceId = DamageUtil2D.GetRootId(info.Target);
        if (_sourceCooldownMap.TryGetValue(sourceId, out float lastTime))
        {
            if (Time.time - lastTime < chainCooldown) return;
        }
        _sourceCooldownMap[sourceId] = Time.time;

        // 전파 실행
        PropagateChain(info.Target.transform, info.Amount);
    }

    private void PropagateChain(Transform source, int originalDamage)
    {
        int chainDmg = Mathf.Max(1, Mathf.RoundToInt(originalDamage * chainDamageRate));
        int maxChain = baseMaxChain + HayulPassive_Dosa.ChainBonus;

        int hitCount = Physics2D.OverlapCircle(
            source.position,
            searchRadius,
            _filter,
            _searchBuffer
        );

        int propagated = 0;

        DamageChainGuard.BeginBonus();

        for (int i = 0; i < hitCount; i++)
        {
            // 프레임당 총 제한 재확인
            if (_chainsThisFrame >= maxChainsPerFrame) break;

            Collider2D col = _searchBuffer[i];
            if (col == null) continue;

            // 자기 자신 제외
            if (col.transform.root == source.root) continue;

            int targetId = DamageUtil2D.GetRootId(col);

            // 대상별 전파 횟수 제한
            _chainCountMap.TryGetValue(targetId, out int currentCount);
            if (currentCount >= maxChain) continue;

            if (DamageUtil2D.TryApplyDamage(col, chainDmg, DamageElement2D.Electric))
            {
                _chainCountMap[targetId] = currentCount + 1;
                _chainsThisFrame++;
                propagated++;
            }
        }

        DamageChainGuard.EndBonus();

        if (debugLog && propagated > 0)
        {
            GameLogger.Log($"[전기 전파] 전파={chainDmg}×{propagated} " +
                      $"(프레임 합계={_chainsThisFrame}/{maxChainsPerFrame})");
        }
    }
}