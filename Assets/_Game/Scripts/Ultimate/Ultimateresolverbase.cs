// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 궁극기 타격 처리 추상 클래스.
/// 캐릭터별 Resolver는 이 클래스를 상속하고 ResolveHit()을 구현한다.
///
/// [v2 최적화]
/// - GetComponent 캐싱: EnemyHealth2D, EnemyGradeTag를 Dictionary로 캐싱
/// - FindPriorityTarget 공용 헬퍼 추가 (윤설/하린 코드 중복 제거)
/// - OnCastBegin/OnCastEnd에서 캐시 자동 정리
/// </summary>
public abstract class UltimateResolverBase : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  데이터 참조 (Executor가 Init 시 주입)
    // ═══════════════════════════════════════════════════════

    protected UltimateDataSO data;
    protected Transform playerTransform;
    protected LayerMask enemyMask;
    protected float runtimeDamageMultiplier = 1f;

    /// <summary>
    /// VFX 발사/발도 기준 위치.
    /// 메인 모드: playerTransform / 지원 모드: 지원 비주얼 Transform
    /// </summary>
    protected Transform casterTransform;

    // ── GC-free 공용 버퍼 ──
    protected readonly List<Collider2D> hitBuffer = new List<Collider2D>(64);
    protected ContactFilter2D enemyFilter;

    // ═══════════════════════════════════════════════════════
    //  v2: GetComponent 캐싱 (매 틱 수십 회 호출 방지)
    // ═══════════════════════════════════════════════════════

    private readonly Dictionary<int, EnemyHealth2D> _healthCache = new Dictionary<int, EnemyHealth2D>(64);
    private readonly Dictionary<int, EnemyGradeTag> _gradeCache = new Dictionary<int, EnemyGradeTag>(64);

    /// <summary>
    /// 캐시된 EnemyHealth2D를 반환합니다. 없으면 GetComponentInChildren 1회 호출 후 캐싱.
    /// </summary>
    protected EnemyHealth2D GetCachedHealth(GameObject root)
    {
        if (root == null) return null;
        int id = root.GetInstanceID();

        if (_healthCache.TryGetValue(id, out var cached))
            return cached;

        var hp = root.GetComponentInChildren<EnemyHealth2D>();
        _healthCache[id] = hp;
        return hp;
    }

    /// <summary>
    /// 캐시된 EnemyGradeTag를 반환합니다. 없으면 GetComponent 1회 호출 후 캐싱.
    /// </summary>
    protected EnemyGradeTag GetCachedGrade(GameObject root)
    {
        if (root == null) return null;
        int id = root.GetInstanceID();

        if (_gradeCache.TryGetValue(id, out var cached))
            return cached;

        var grade = root.GetComponent<EnemyGradeTag>();
        _gradeCache[id] = grade;
        return grade;
    }

    /// <summary>캐시된 Health로 죽음 판정. GetComponent 반복 호출 방지.</summary>
    protected bool IsTargetDeadCached(GameObject target)
    {
        if (target == null) return true;
        var hp = GetCachedHealth(target);
        return hp == null || hp.IsDead;
    }

    /// <summary>컴포넌트 캐시를 비웁니다. OnCastBegin/OnCastEnd에서 자동 호출됩니다.</summary>
    protected void ClearComponentCache()
    {
        _healthCache.Clear();
        _gradeCache.Clear();
    }

    // ═══════════════════════════════════════════════════════
    //  v2: 공용 우선순위 탐색 (보스 > 엘리트 > 노말)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// hitBuffer 내에서 보스 > 엘리트 > 노말 우선순위로 가장 가까운 적을 반환합니다.
    /// GetComponent 캐싱 적용.
    /// </summary>
    protected GameObject FindPriorityTargetCached()
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

            var hp = GetCachedHealth(root);
            if (hp == null || hp.IsDead) continue;

            EnemyGrade grade = EnemyGrade.Normal;
            var gradeTag = GetCachedGrade(root);
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

    // ═══════════════════════════════════════════════════════
    //  초기화 (Executor가 호출)
    // ═══════════════════════════════════════════════════════

    public void Init(UltimateDataSO ultimateData, Transform player, LayerMask mask)
    {
        data = ultimateData;
        playerTransform = player;
        casterTransform = player;
        enemyMask = mask;

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyMask);
        enemyFilter.useLayerMask = true;
        enemyFilter.useTriggers = true;

        OnInit();
    }

    public void SetCasterTransform(Transform caster)
    {
        casterTransform = caster != null ? caster : playerTransform;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        runtimeDamageMultiplier = multiplier;
    }

    protected virtual void OnInit() { }

    /// <summary>궁극기 시작. 캐시 자동 초기화.</summary>
    public virtual void OnCastBegin()
    {
        ClearComponentCache();
    }

    /// <summary>궁극기 종료. 캐시 자동 정리.</summary>
    public virtual void OnCastEnd()
    {
        ClearComponentCache();
    }

    // ═══════════════════════════════════════════════════════
    //  추상 메서드
    // ═══════════════════════════════════════════════════════

    public abstract void ResolveHit();

    // ═══════════════════════════════════════════════════════
    //  공용 헬퍼
    // ═══════════════════════════════════════════════════════

    protected int FindEnemiesInRadius(float radius)
    {
        if (playerTransform == null) return 0;
        hitBuffer.Clear();
        return Physics2D.OverlapCircle(
            playerTransform.position, radius, enemyFilter, hitBuffer
        );
    }

    protected int FindEnemiesInRadius(Vector2 center, float radius, List<Collider2D> outputBuffer)
    {
        outputBuffer.Clear();
        return Physics2D.OverlapCircle(center, radius, enemyFilter, outputBuffer);
    }

    protected GameObject FindHighestMaxHpEnemy()
    {
        float bestMaxHp = float.MinValue;
        GameObject best = null;

        for (int i = 0; i < hitBuffer.Count; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            GameObject root = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.transform.root.gameObject;

            if (root == null || !root.activeInHierarchy) continue;

            var hp = GetCachedHealth(root);
            if (hp == null) continue;

            float maxHp = hp.MaxHp;
            if (maxHp > bestMaxHp)
            {
                bestMaxHp = maxHp;
                best = root;
            }
        }

        return best;
    }

    protected int CalcFinalDamage(float baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * runtimeDamageMultiplier));
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        if (data == null) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(playerTransform.position, data.HitRadius);

        if (data.SecondaryRadius > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawWireSphere(playerTransform.position, data.SecondaryRadius);
        }
    }
#endif
}