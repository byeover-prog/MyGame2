// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 윤설 궁극기 "혹한의 집념" Resolver.
///
/// [동작 — LoL 애쉬 Q(Ranger's Focus) 참고]
/// 1. 0.5초 간격으로 ResolveHit 호출 (4초간 = 8번)
/// 2. 매 ResolveHit마다 6발을 짧은 간격(0.08초)으로 다다닥 burst 발사
/// 3. 각 화살은 같은 타겟에 적중 (단일 타겟)
/// 4. 화살 비주얼은 살짝 각도가 퍼져서 날아감 → 빠르게 날면 삼각형처럼 보임
/// 5. 각 화살: 10 데미지 + 혹한 중첩 1
/// 6. 타겟: 보스 > 엘리트 > 노말 우선순위
/// 7. 타겟 사망 시 자동 재탐색
///
/// [UltimateDataSO 설정]
/// duration=4, hitDelay=0.2, hitInterval=0.5, baseDamage=10, element=Ice
///
/// [프리팹 구조]
/// ULT_Yoonseol (GameObject)
/// └─ YoonseolUltimateResolver
/// </summary>
public sealed class YoonseolUltimateResolver : UltimateResolverBase
{
    [Header("윤설 전용: Burst 발사")]
    [Tooltip("1회 ResolveHit당 발사하는 화살 수")]
    [SerializeField] private int arrowsPerBurst = 6;

    [Tooltip("burst 내 화살 간 딜레이(초). 0.08 = 다다닥 느낌")]
    [SerializeField] private float burstDelay = 0.08f;

    [Header("윤설 전용: 화살 비주얼")]
    [Tooltip("화살 VFX 프리팹 (콜라이더 없음, UltimateArrowVisual2D 자동 부착)")]
    [SerializeField] private GameObject arrowVfxPrefab;

    [Tooltip("화살 비행 속도")]
    [SerializeField] private float arrowSpeed = 18f;

    [Tooltip("화살 수명(초). 이후 자동 소멸")]
    [SerializeField] private float arrowLifetime = 0.8f;

    [Tooltip("화살 출발 위치 좌우 퍼짐 폭 (월드 단위).\n" +
             "1.5면 플레이어 좌우 ±0.75 범위에서 출발.\n" +
             "전부 같은 타겟을 향해 수렴하며 날아감.")]
    [SerializeField] private float spreadWidth = 1.5f;

    [Header("윤설 전용: 혹한 중첩")]
    [Tooltip("혹한 최대 중첩 수")]
    [SerializeField] private int maxExtremeColdStacks = 100;

    [Header("윤설 전용: 궁극기 VFX")]
    [Tooltip("시전 중 플레이어 위치에 독립 생성할 VFX (ULT_윤설)")]
    [SerializeField] private GameObject ultimateVfxPrefab;

    // ── 런타임 ──
    private readonly Dictionary<int, int> _coldStacks = new Dictionary<int, int>();
    private GameObject _currentTarget;
    private GameObject _ultimateVfxInstance;

    public override void OnCastBegin()
    {
        _coldStacks.Clear();
        _currentTarget = null;

        if (ultimateVfxPrefab != null && casterTransform != null)
        {
            _ultimateVfxInstance = Object.Instantiate(
                ultimateVfxPrefab, casterTransform.position, Quaternion.identity
            );
        }

        Debug.Log("[윤설 궁극기] 시전 시작 — 혹한 중첩 초기화");
    }

    public override void OnCastEnd()
    {
        int totalStacks = 0;
        foreach (var pair in _coldStacks)
            totalStacks += pair.Value;

        if (_ultimateVfxInstance != null)
        {
            Object.Destroy(_ultimateVfxInstance);
            _ultimateVfxInstance = null;
        }

        Debug.Log($"[윤설 궁극기] 시전 종료 | 총 혹한 중첩={totalStacks}");
        _coldStacks.Clear();
        _currentTarget = null;
    }

    /// <summary>
    /// Executor에서 0.5초마다 호출.
    /// 6발을 다다닥 burst 발사.
    /// </summary>
    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        // ── 타겟 유효성 확인 ──
        if (_currentTarget == null || !_currentTarget.activeInHierarchy || IsTargetDead(_currentTarget))
        {
            FindEnemiesInRadius(data.HitRadius);
            _currentTarget = FindPriorityTarget();
        }

        if (_currentTarget == null)
        {
            Debug.Log("[윤설 궁극기] 타겟 없음");
            return;
        }

        StartCoroutine(BurstFire(_currentTarget));
    }

    private IEnumerator BurstFire(GameObject target)
    {
        if (target == null) yield break;

        int targetId = target.GetInstanceID();
        _coldStacks.TryGetValue(targetId, out int currentStacks);

        int hits = 0;

        for (int i = 0; i < arrowsPerBurst; i++)
        {
            // 타겟이 죽었으면 재탐색
            if (target == null || !target.activeInHierarchy || IsTargetDead(target))
            {
                FindEnemiesInRadius(data.HitRadius);
                target = FindPriorityTarget();
                if (target == null) break;

                targetId = target.GetInstanceID();
                _coldStacks.TryGetValue(targetId, out currentStacks);
                currentStacks += hits;
            }

            // ── 데미지 ──
            float bonusPercent = Mathf.Clamp(currentStacks + hits, 0, maxExtremeColdStacks) * 0.01f;
            int finalDmg = Mathf.Max(1, Mathf.RoundToInt(
                data.BaseDamage * runtimeDamageMultiplier * (1f + bonusPercent)
            ));

            if (DamageUtil2D.TryApplyDamage(target, finalDmg, data.DamageElement))
                hits++;

            // ── 화살 비주얼 (살짝 퍼지는 각도) ──
            SpawnArrowVisual(target.transform, i);

            // 다다닥 딜레이
            if (i < arrowsPerBurst - 1 && burstDelay > 0f)
                yield return new WaitForSeconds(burstDelay);
        }

        // ── 혹한 중첩 갱신 ──
        if (hits > 0 && target != null)
        {
            int newStacks = Mathf.Clamp(currentStacks + hits, 0, maxExtremeColdStacks);
            _coldStacks[targetId] = newStacks;

            Debug.Log($"[윤설 궁극기] burst | 대상={target.name} " +
                      $"화살={hits} 혹한={currentStacks}→{newStacks}");
        }

        if (target != null && IsTargetDead(target))
            _currentTarget = null;
    }

    /// <summary>
    /// 화살 비주얼 1개 생성.
    /// 출발 위치만 좌우로 살짝 오프셋하고, 전부 같은 타겟을 향해 날아감.
    /// → 수렴하는 부채꼴 형태 (애쉬 Q 스타일)
    /// </summary>
    private void SpawnArrowVisual(Transform target, int arrowIndex)
    {
        if (arrowVfxPrefab == null || casterTransform == null || target == null) return;

        Vector3 origin = casterTransform.position; // 윤설 몸에서 발사
        Vector2 toTarget = ((Vector2)target.position - (Vector2)origin);
        Vector2 forward = toTarget.normalized;

        // forward에 수직인 좌우 벡터
        Vector2 lateral = new Vector2(-forward.y, forward.x);

        // 출발 위치를 좌우로 오프셋: -halfSpread ~ +halfSpread
        float halfSpread = spreadWidth * 0.5f;
        float step = arrowsPerBurst > 1
            ? spreadWidth / (arrowsPerBurst - 1)
            : 0f;
        float offset = -halfSpread + step * arrowIndex;

        Vector3 spawnPos = origin + (Vector3)(lateral * offset);

        // 방향: 출발 위치 → 타겟 (전부 같은 타겟을 향함)
        Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;

        GameObject arrowObj = Object.Instantiate(arrowVfxPrefab, spawnPos, Quaternion.identity);

        var visual = arrowObj.GetComponent<UltimateArrowVisual2D>();
        if (visual == null)
            visual = arrowObj.AddComponent<UltimateArrowVisual2D>();

        visual.Init(target, dir, arrowSpeed, arrowLifetime, 0.5f);
    }

    // ═══════════════════════════════════════════════════════
    //  우선순위 탐색: 보스 > 엘리트 > 노말
    // ═══════════════════════════════════════════════════════

    private GameObject FindPriorityTarget()
    {
        if (hitBuffer.Count == 0) return null;

        GameObject best = null;
        EnemyGrade bestGrade = (EnemyGrade)999;
        float bestDist = float.MaxValue;
        Vector2 playerPos = playerTransform.position;

        for (int i = 0; i < hitBuffer.Count; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            GameObject root = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.transform.root.gameObject;

            if (root == null || !root.activeInHierarchy) continue;
            if (IsTargetDead(root)) continue;

            EnemyGrade grade = EnemyGrade.Normal;
            var gradeTag = root.GetComponent<EnemyGradeTag>();
            if (gradeTag != null) grade = gradeTag.Grade;

            float dist = Vector2.Distance(playerPos, (Vector2)root.transform.position);

            if (grade < bestGrade || (grade == bestGrade && dist < bestDist))
            {
                best = root;
                bestGrade = grade;
                bestDist = dist;
            }
        }

        return best;
    }

    private bool IsTargetDead(GameObject target)
    {
        if (target == null) return true;
        var hp = target.GetComponentInChildren<EnemyHealth2D>();
        return hp == null || hp.IsDead;
    }
}