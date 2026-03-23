using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하율 궁극기 "천강뇌전부" Resolver.
/// UltimateResolverBase를 상속하여 하율 고유 로직(범위 반복 데미지 + 전파)을 구현.
///
/// [동작]
/// 1. hitRadius 내 모든 적에게 baseDamage × multiplier
/// 2. 각 피격 대상 주변 secondaryRadius → baseDamage × chainDamageRate 전파
/// 3. 한 대상당 최대 maxChainCount회 전파 (전기속성 규칙)
/// 4. 데미지는 DamageUtil2D.TryApplyDamage(Electric) — 팝업 + VFX 자동
///
/// [프리팹 만들기]
/// 1. 빈 GameObject 생성 → 이름: UltResolver_Hayul
/// 2. 이 컴포넌트 부착
/// 3. 프리팹 저장 → CharacterDefinitionSO(하율).ultimateResolverPrefab에 연결
/// </summary>
public sealed class HayulUltimateResolver : UltimateResolverBase
{
    [Header("하율 전용: 전파 설정")]
    [Tooltip("전파 데미지 비율 (0.15 = 본체의 15%)")]
    [SerializeField] private float chainDamageRate = 0.15f;

    [Tooltip("한 대상당 최대 전파 횟수 (전기속성 규칙: 5회)")]
    [SerializeField] private int maxChainCount = 5;

    // ── GC-free 버퍼 ──
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();
    private readonly Dictionary<int, int> _chainCountMap = new Dictionary<int, int>();
    private readonly List<Collider2D> _chainBuffer = new List<Collider2D>(32);

    public override void OnCastBegin()
    {
        _alreadyHit.Clear();
        _chainCountMap.Clear();
    }

    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        _alreadyHit.Clear();
        _chainCountMap.Clear();

        // ── 1단계: 고정 반경 내 모든 적에게 본체 데미지 ──
        int hitCount = FindEnemiesInRadius(data.HitRadius);
        int baseDmg = CalcFinalDamage(data.BaseDamage);
        int damagedCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            int id = col.gameObject.GetInstanceID();
            if (_alreadyHit.Contains(id)) continue;

            if (DamageUtil2D.TryApplyDamage(col.gameObject, baseDmg, data.DamageElement))
            {
                _alreadyHit.Add(id);
                damagedCount++;
            }
        }

        Debug.Log($"[하율 궁극기] 본체 데미지 | 탐색={hitCount} 피격={damagedCount} dmg={baseDmg}");

        // ── 2단계: 전파 데미지 ──
        int chainDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * chainDamageRate));
        int totalChains = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D sourceCol = hitBuffer[i];
            if (sourceCol == null) continue;

            int sourceId = sourceCol.gameObject.GetInstanceID();
            if (!_alreadyHit.Contains(sourceId)) continue;

            totalChains += PropagateChain(sourceCol.transform, chainDmg);
        }

        if (totalChains > 0)
        {
            Debug.Log($"[하율 궁극기] 전파 | 전파피해={chainDmg} " +
                      $"(본체의 {chainDamageRate * 100f}%) 총전파횟수={totalChains}");
        }
    }

    private int PropagateChain(Transform source, int chainDmg)
    {
        int chainHitCount = FindEnemiesInRadius(
            (Vector2)source.position, data.SecondaryRadius, _chainBuffer
        );

        int propagated = 0;
        for (int i = 0; i < chainHitCount; i++)
        {
            Collider2D col = _chainBuffer[i];
            if (col == null) continue;

            int targetId = col.gameObject.GetInstanceID();

            _chainCountMap.TryGetValue(targetId, out int currentCount);

            // ★ 하율 고유 패시브 "도사란 무엇인가?" — 전파 최대치 보너스 적용
            int finalMaxChain = maxChainCount + HayulPassive_Dosa.ChainBonus;
            if (currentCount >= finalMaxChain) continue;

            if (DamageUtil2D.TryApplyDamage(col.gameObject, chainDmg, data.DamageElement))
            {
                _chainCountMap[targetId] = currentCount + 1;
                propagated++;
            }
        }

        return propagated;
    }
}