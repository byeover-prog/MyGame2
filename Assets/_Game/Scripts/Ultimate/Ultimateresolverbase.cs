// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 궁극기 타격 처리 추상 클래스.
/// 캐릭터별 Resolver는 이 클래스를 상속하고 ResolveHit()을 구현한다.
///
/// [캐릭터 추가 시]
/// 1. NewCharacterResolver : UltimateResolverBase 작성
/// 2. ResolveHit() 구현
/// 3. 프리팹 만들어서 CharacterDefinitionSO.ultimateResolverPrefab에 연결
///
/// [공용 기능]
/// - UltimateDataSO 참조 (데미지, 반경, 속성 등)
/// - 적 탐색 헬퍼: FindEnemiesInRadius(), FindHighestMaxHpEnemy()
/// - 지원 모드 데미지 배율 적용
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
    /// 메인 모드: playerTransform (본인 몸)
    /// 지원 모드: 지원 비주얼 Transform (지원 캐릭터 몸)
    /// 기본값은 playerTransform. SetCasterTransform()으로 오버라이드.
    /// </summary>
    protected Transform casterTransform;

    // ── GC-free 공용 버퍼 ──
    protected readonly List<Collider2D> hitBuffer = new List<Collider2D>(64);
    protected ContactFilter2D enemyFilter;

    // ═══════════════════════════════════════════════════════
    //  초기화 (Executor가 호출)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Executor가 런타임에 호출하여 데이터를 주입한다.
    /// </summary>
    public void Init(UltimateDataSO ultimateData, Transform player, LayerMask mask)
    {
        data = ultimateData;
        playerTransform = player;
        casterTransform = player; // 기본값: 메인 캐릭터 본인
        enemyMask = mask;

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyMask);
        enemyFilter.useLayerMask = true;
        enemyFilter.useTriggers = true;

        OnInit();
    }

    /// <summary>
    /// VFX 발사 기준 위치를 오버라이드한다.
    /// 지원 모드에서 지원 비주얼 Transform으로 설정.
    /// null이면 playerTransform으로 복원.
    /// </summary>
    public void SetCasterTransform(Transform caster)
    {
        casterTransform = caster != null ? caster : playerTransform;
    }

    /// <summary>
    /// 지원 모드 데미지 배율 설정. 1.0 = 메인, 0.55 = 지원 등.
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        runtimeDamageMultiplier = multiplier;
    }

    /// <summary>서브클래스 초기화 훅. 필요 시 override.</summary>
    protected virtual void OnInit() { }

    /// <summary>궁극기 시작 시 호출. 상태 초기화용. 필요 시 override.</summary>
    public virtual void OnCastBegin() { }

    /// <summary>궁극기 종료 시 호출. 정리용. 필요 시 override.</summary>
    public virtual void OnCastEnd() { }

    // ═══════════════════════════════════════════════════════
    //  추상 메서드 — 서브클래스가 반드시 구현
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 1회 타격 처리. Executor의 루프에서 hitInterval마다 호출.
    /// 서브클래스에서 적 탐색 + 데미지 적용을 구현한다.
    /// </summary>
    public abstract void ResolveHit();

    // ═══════════════════════════════════════════════════════
    //  공용 헬퍼: 적 탐색
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 플레이어 중심 반경 내 모든 적 콜라이더를 hitBuffer에 담고 갯수를 반환.
    /// </summary>
    protected int FindEnemiesInRadius(float radius)
    {
        if (playerTransform == null) return 0;

        hitBuffer.Clear();
        return Physics2D.OverlapCircle(
            playerTransform.position, radius, enemyFilter, hitBuffer
        );
    }

    /// <summary>
    /// 지정 위치 중심 반경 내 적 탐색. 결과는 outputBuffer에 담긴다.
    /// </summary>
    protected int FindEnemiesInRadius(Vector2 center, float radius, List<Collider2D> outputBuffer)
    {
        outputBuffer.Clear();
        return Physics2D.OverlapCircle(center, radius, enemyFilter, outputBuffer);
    }

    /// <summary>
    /// hitBuffer 내에서 maxHp가 가장 높은 적 GameObject를 반환.
    /// hitBuffer에 먼저 FindEnemiesInRadius()로 채워야 한다.
    /// </summary>
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

            var hp = root.GetComponentInChildren<EnemyHealth2D>();
            if (hp == null) continue;

            // EnemyHealth2D에 MaxHp 프로퍼티나 maxHp 필드가 있다고 가정
            float maxHp = hp.MaxHp;
            if (maxHp > bestMaxHp)
            {
                bestMaxHp = maxHp;
                best = root;
            }
        }

        return best;
    }

    /// <summary>
    /// 최종 데미지 계산: baseDamage × runtimeDamageMultiplier (지원 모드 배율 포함).
    /// </summary>
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