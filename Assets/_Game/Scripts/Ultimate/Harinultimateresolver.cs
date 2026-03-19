// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하린 궁극기 "월광쇄도" Resolver.
///
/// [동작]
/// 1. 하린 몸에서 발도(Ready) VFX 재생 — 칼을 뽑는 연출
/// 2. 발도 끝나면 → 주 대상 위치에 월광쇄도 참격 VFX 생성
/// 3. 주 대상: 6회 전체 타격
/// 4. 주변 대상 (secondaryRadius 내): 3회 타격
/// 5. 타겟: 보스 > 엘리트 > 노말, 같은 등급이면 가장 가까운 적
///
/// [프리팹 구조]
/// UltResolver_Harin
/// └─ HarinUltimateResolver
///     - Ready Vfx Prefab: Ready_하린 (발도 이펙트)
///     - Slash Vfx Prefab: 월광쇄도 이펙트
/// </summary>
public sealed class HarinUltimateResolver : UltimateResolverBase
{
    [Header("하린 전용: 참격")]
    [Tooltip("주 대상 총 타격 횟수")]
    [SerializeField] private int primaryHitCount = 6;

    [Tooltip("주변 대상 총 타격 횟수 (주 대상의 절반)")]
    [SerializeField] private int surroundingHitCount = 3;

    [Header("하린 전용: 발도(Ready) VFX")]
    [Tooltip("하린 몸에서 재생할 발도 VFX 프리팹 (Ready_하린).\n" +
             "칼을 뽑는 연출. 이 이펙트가 끝나면 참격으로 넘어감.")]
    [SerializeField] private GameObject readyVfxPrefab;

    [Tooltip("발도 VFX 재생 시간(초). 이 시간 후 참격 시작.")]
    [SerializeField] private float readyDuration = 0.6f;

    [Tooltip("발도 VFX 자동 파괴 시간(초)")]
    [SerializeField] private float readyVfxLifetime = 1.5f;

    [Header("하린 전용: 참격(월광쇄도) VFX")]
    [Tooltip("주 대상 위치에 생성할 참격 VFX 프리팹.")]
    [SerializeField] private GameObject slashVfxPrefab;

    [Tooltip("참격 VFX 자동 파괴 시간(초)")]
    [SerializeField] private float slashVfxLifetime = 2f;

    // ── GC-free 버퍼 ──
    private readonly List<Collider2D> _splashBuffer = new List<Collider2D>(32);
    private readonly HashSet<int> _processedRoots = new HashSet<int>();

    /// <summary>
    /// Executor에서 호출. 발도 → 참격 순서로 코루틴 실행.
    /// </summary>
    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        FindEnemiesInRadius(data.HitRadius);
        GameObject primaryTarget = FindPriorityTarget();

        if (primaryTarget == null)
        {
            Debug.Log("[하린 궁극기] 주 대상 없음");
            return;
        }

        StartCoroutine(ReadyThenSlash(primaryTarget));
    }

    private IEnumerator ReadyThenSlash(GameObject primaryTarget)
    {
        // ═══════════════════════════════════════════════════
        //  1단계: 발도(Ready) VFX — 하린 몸에서 재생
        // ═══════════════════════════════════════════════════

        if (readyVfxPrefab != null && casterTransform != null)
        {
            GameObject readyVfx = Object.Instantiate(
                readyVfxPrefab,
                casterTransform.position,
                Quaternion.identity
            );
            // 캐스터 몸에 부착 (하린 몸에서 발도)
            readyVfx.transform.SetParent(casterTransform, true);
            readyVfx.transform.localPosition = Vector3.zero;

            if (readyVfxLifetime > 0f)
                Object.Destroy(readyVfx, readyVfxLifetime);

            Debug.Log("[하린 궁극기] 발도 시작");
        }

        // 발도 재생 대기
        yield return new WaitForSeconds(readyDuration);

        // 타겟 유효성 재확인
        if (primaryTarget == null || !primaryTarget.activeInHierarchy)
        {
            FindEnemiesInRadius(data.HitRadius);
            primaryTarget = FindPriorityTarget();
            if (primaryTarget == null) yield break;
        }

        // ═══════════════════════════════════════════════════
        //  2단계: 참격(월광쇄도) VFX — 적 위치에 생성
        // ═══════════════════════════════════════════════════

        SpawnSlashVFX(primaryTarget.transform.position);

        int baseDmg = CalcFinalDamage(data.BaseDamage);

        // ── 주 대상 참격 (6회) ──
        int primaryHits = 0;
        for (int i = 0; i < primaryHitCount; i++)
        {
            if (primaryTarget == null || !primaryTarget.activeInHierarchy) break;

            if (DamageUtil2D.TryApplyDamage(primaryTarget, baseDmg, data.DamageElement))
                primaryHits++;
        }

        // ── 주변 대상 참격 (3회) ──
        _processedRoots.Clear();
        _processedRoots.Add(primaryTarget.GetInstanceID());

        int splashCount = FindEnemiesInRadius(
            (Vector2)primaryTarget.transform.position,
            data.SecondaryRadius,
            _splashBuffer
        );

        int surroundingTargets = 0;

        for (int i = 0; i < splashCount; i++)
        {
            Collider2D col = _splashBuffer[i];
            if (col == null) continue;

            GameObject root = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.transform.root.gameObject;

            if (root == null || !root.activeInHierarchy) continue;

            int rootId = root.GetInstanceID();
            if (_processedRoots.Contains(rootId)) continue;
            _processedRoots.Add(rootId);

            for (int hit = 0; hit < surroundingHitCount; hit++)
            {
                if (root == null || !root.activeInHierarchy) break;
                DamageUtil2D.TryApplyDamage(root, baseDmg, data.DamageElement);
            }

            surroundingTargets++;
        }

        Debug.Log($"[하린 궁극기] 참격 완료 | 주 대상={primaryTarget.name} " +
                  $"주타격={primaryHits}/{primaryHitCount} " +
                  $"주변대상={surroundingTargets} 주변타격={surroundingHitCount}회씩 " +
                  $"dmg={baseDmg}");
    }

    private void SpawnSlashVFX(Vector3 targetPosition)
    {
        if (slashVfxPrefab == null) return;

        GameObject vfx = Object.Instantiate(slashVfxPrefab, targetPosition, Quaternion.identity);

        if (slashVfxLifetime > 0f)
            Object.Destroy(vfx, slashVfxLifetime);
    }

    // ═══════════════════════════════════════════════════════
    //  우선순위 탐색: 보스 > 엘리트 > 노말 (같은 등급이면 가장 가까운 적)
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

            var hp = root.GetComponentInChildren<EnemyHealth2D>();
            if (hp == null || hp.IsDead) continue;

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
}