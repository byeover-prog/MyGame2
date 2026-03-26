// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하린 궁극기 "월광쇄도" Resolver.
///
/// [v2 최적화]
/// - WaitForSeconds 캐싱: new WaitForSeconds(readyDuration) 1회만 생성
/// - GetComponent 캐싱: FindPriorityTargetCached() 사용 (Base 클래스)
/// - VFX Instantiate는 궁극기당 1~2회라 풀링 대비 효과 적음 → 유지
/// </summary>
public sealed class HarinUltimateResolver : UltimateResolverBase
{
    [Header("하린 전용: 참격")]
    [SerializeField] private int primaryHitCount = 6;
    [SerializeField] private int surroundingHitCount = 3;

    [Header("하린 전용: 발도(Ready) VFX")]
    [SerializeField] private GameObject readyVfxPrefab;
    [SerializeField] private float readyDuration = 0.6f;
    [SerializeField] private float readyVfxLifetime = 1.5f;

    [Header("하린 전용: 참격(월광쇄도) VFX")]
    [SerializeField] private GameObject slashVfxPrefab;
    [SerializeField] private float slashVfxLifetime = 2f;

    // ── GC-free 버퍼 ──
    private readonly List<Collider2D> _splashBuffer = new List<Collider2D>(32);
    private readonly HashSet<int> _processedRoots = new HashSet<int>();

    // ★ v2: WaitForSeconds 캐싱
    private WaitForSeconds _cachedReadyWait;

    protected override void OnInit()
    {
        _cachedReadyWait = readyDuration > 0f ? new WaitForSeconds(readyDuration) : null;
    }

    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        FindEnemiesInRadius(data.HitRadius);
        GameObject primaryTarget = FindPriorityTargetCached(); // ★ v2: 캐시 버전

        if (primaryTarget == null) return;

        StartCoroutine(ReadyThenSlash(primaryTarget));
    }

    private IEnumerator ReadyThenSlash(GameObject primaryTarget)
    {
        // ── 1단계: 발도 VFX ──
        if (readyVfxPrefab != null && casterTransform != null)
        {
            GameObject readyVfx = Object.Instantiate(
                readyVfxPrefab, casterTransform.position, Quaternion.identity
            );
            readyVfx.transform.SetParent(casterTransform, true);
            readyVfx.transform.localPosition = Vector3.zero;

            if (readyVfxLifetime > 0f)
                Object.Destroy(readyVfx, readyVfxLifetime);
        }

        // ★ v2: 캐싱된 WaitForSeconds
        if (_cachedReadyWait != null)
            yield return _cachedReadyWait;

        // 타겟 유효성 재확인 (★ v2: 캐시 버전)
        if (primaryTarget == null || !primaryTarget.activeInHierarchy || IsTargetDeadCached(primaryTarget))
        {
            FindEnemiesInRadius(data.HitRadius);
            primaryTarget = FindPriorityTargetCached();
            if (primaryTarget == null) yield break;
        }

        // ── 2단계: 참격 VFX ──
        SpawnSlashVFX(primaryTarget.transform.position);

        int baseDmg = CalcFinalDamage(data.BaseDamage);

        // ── 주 대상 참격 ──
        for (int i = 0; i < primaryHitCount; i++)
        {
            if (primaryTarget == null || !primaryTarget.activeInHierarchy) break;
            DamageUtil2D.TryApplyDamage(primaryTarget, baseDmg, data.DamageElement);
        }

        // ── 주변 대상 참격 ──
        _processedRoots.Clear();
        _processedRoots.Add(primaryTarget.GetInstanceID());

        int splashCount = FindEnemiesInRadius(
            (Vector2)primaryTarget.transform.position,
            data.SecondaryRadius,
            _splashBuffer
        );

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

            // ★ v2: 캐시된 사망 체크
            if (IsTargetDeadCached(root)) continue;

            for (int hit = 0; hit < surroundingHitCount; hit++)
            {
                if (root == null || !root.activeInHierarchy) break;
                DamageUtil2D.TryApplyDamage(root, baseDmg, data.DamageElement);
            }
        }
    }

    private void SpawnSlashVFX(Vector3 targetPosition)
    {
        if (slashVfxPrefab == null) return;
        GameObject vfx = Object.Instantiate(slashVfxPrefab, targetPosition, Quaternion.identity);
        if (slashVfxLifetime > 0f)
            Object.Destroy(vfx, slashVfxLifetime);
    }
}