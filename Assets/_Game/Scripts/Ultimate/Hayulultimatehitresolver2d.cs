using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 하율 궁극기 본체 피해와 전파 피해를 처리한다.
/// 스파크는 틱마다 새로 생성하지 않고, 적마다 1개만 유지되는 부착형 이펙트로 갱신한다.
/// 정화구처럼 적 중심에 붙어 보이도록 Collider 중심 기준으로 연출한다.
/// </summary>
public class HayulUltimateHitResolver2D : MonoBehaviour
{
    [Header("데미지")]
    [SerializeField, Tooltip("궁극기 기본 피해량입니다.")]
    private float baseDamage = 10f;

    [SerializeField, Tooltip("전파 데미지 비율입니다. 0.15면 기본 피해의 15%입니다.")]
    private float chainDamageRate = 0.15f;

    [Header("범위 설정")]
    [SerializeField, Tooltip("궁극기 본체 피해 반경입니다.")]
    private float hitRadius = 18f;

    [SerializeField, Tooltip("전파 탐색 반경입니다.")]
    private float chainSearchRadius = 5f;

    [SerializeField, Tooltip("한 대상당 최대 전파 횟수입니다.")]
    private int maxChainCount = 5;

    [Header("탐색")]
    [SerializeField, Tooltip("적 레이어 마스크입니다.")]
    private LayerMask enemyMask;

    [Header("전기 부착 VFX")]
    [SerializeField, Tooltip("적에게 붙는 전기 스파크 프리팹입니다.")]
    private GameObject attachedSparkVfxPrefab;

    [SerializeField, Tooltip("마지막 틱 후 이펙트가 더 유지되는 시간입니다.")]
    private float attachedSparkHoldTime = 0.25f;

    [SerializeField, Tooltip("적 중심 기준 위치 보정값입니다.")]
    private Vector3 attachedSparkOffset = Vector3.zero;

    [SerializeField, Tooltip("적에 붙는 전기 이펙트 크기입니다.")]
    private Vector3 attachedSparkScale = Vector3.one;

    [SerializeField, Tooltip("비워두면 프리팹의 Sorting Layer를 그대로 사용합니다.")]
    private string attachedSparkSortingLayer = "";

    [SerializeField, Tooltip("0이면 프리팹의 Order를 그대로 사용합니다.")]
    private int attachedSparkOrderInLayer = 0;

    [Header("참조")]
    [SerializeField, Tooltip("플레이어 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    private Transform playerTransform;

    private readonly List<Collider2D> _hitResults = new List<Collider2D>(64);
    private readonly List<Collider2D> _chainResults = new List<Collider2D>(32);
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();
    private readonly Dictionary<int, int> _chainCountMap = new Dictionary<int, int>();

    private ContactFilter2D _enemyFilter;

    /// <summary>
    /// 1회 틱 데미지 + 전파 처리.
    /// Executor에서 주기적으로 호출한다.
    /// </summary>
    public void ResolveHit()
    {
        EnsurePlayerRef();
        if (playerTransform == null)
        {
            Debug.LogError("[하율 궁극기] 플레이어 Transform이 없어서 데미지를 적용할 수 없습니다.");
            return;
        }

        _alreadyHit.Clear();
        _chainCountMap.Clear();

        Vector2 center = playerTransform.position;

        _hitResults.Clear();
        int hitCount = Physics2D.OverlapCircle(center, hitRadius, _enemyFilter, _hitResults);

        int damagedCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _hitResults[i];
            if (col == null)
                continue;

            int id = col.gameObject.GetInstanceID();
            if (_alreadyHit.Contains(id))
                continue;

            if (TryDealDamage(col.gameObject, baseDamage))
            {
                _alreadyHit.Add(id);
                damagedCount++;

                RefreshAttachedSpark(col.gameObject);
            }
        }

        Debug.Log($"[하율 궁극기] 본체 틱 피해 | 탐색={hitCount} 피격={damagedCount} 피해={baseDamage}");

        float chainDamage = baseDamage * chainDamageRate;
        int totalChains = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D sourceCol = _hitResults[i];
            if (sourceCol == null)
                continue;

            int sourceId = sourceCol.gameObject.GetInstanceID();
            if (!_alreadyHit.Contains(sourceId))
                continue;

            totalChains += PropagateChain(sourceCol.transform, chainDamage);
        }

        if (totalChains > 0)
        {
            Debug.Log($"[하율 궁극기] 전파 피해 | 전파횟수={totalChains} 전파피해={chainDamage:F1}");
        }
    }

    /// <summary>
    /// 외부에서 궁극기 피해량을 주입할 때 사용한다.
    /// </summary>
    public void SetBaseDamage(float damage)
    {
        baseDamage = damage;
    }

    private int PropagateChain(Transform source, float chainDamage)
    {
        _chainResults.Clear();

        int chainHitCount = Physics2D.OverlapCircle(
            source.position,
            chainSearchRadius,
            _enemyFilter,
            _chainResults
        );

        int propagated = 0;

        for (int i = 0; i < chainHitCount; i++)
        {
            Collider2D col = _chainResults[i];
            if (col == null)
                continue;

            if (col.transform == source)
                continue;

            int targetId = col.gameObject.GetInstanceID();

            _chainCountMap.TryGetValue(targetId, out int currentCount);
            if (currentCount >= maxChainCount)
                continue;

            if (TryDealDamage(col.gameObject, chainDamage))
            {
                _chainCountMap[targetId] = currentCount + 1;
                propagated++;

                RefreshAttachedSpark(col.gameObject);
            }
        }

        return propagated;
    }

    private bool TryDealDamage(GameObject target, float damage)
    {
        if (target == null)
            return false;

        int intDamage = Mathf.RoundToInt(damage);
        if (intDamage <= 0)
            return false;

        return DamageUtil2D.TryApplyDamage(target, intDamage, DamageElement2D.Electric);
    }

    /// <summary>
    /// 정화구처럼 적 1마리당 부착형 전기 이펙트 1개만 유지한다.
    /// </summary>
    private void RefreshAttachedSpark(GameObject target)
    {
        if (target == null)
            return;

        if (attachedSparkVfxPrefab == null)
        {
            Debug.LogWarning("[하율 궁극기] Attached Spark VFX Prefab이 비어 있습니다.");
            return;
        }

        AttachedElectricEffect2D effect = target.GetComponent<AttachedElectricEffect2D>();
        if (effect == null)
        {
            effect = target.AddComponent<AttachedElectricEffect2D>();
        }

        effect.Refresh(
            attachedSparkVfxPrefab,
            attachedSparkHoldTime,
            attachedSparkOffset,
            attachedSparkScale,
            attachedSparkSortingLayer,
            attachedSparkOrderInLayer
        );
    }

    private void Awake()
    {
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers = true;

        EnsurePlayerRef();

        if (attachedSparkVfxPrefab == null)
        {
            Debug.LogWarning("[하율 궁극기] 부착형 전기 VFX 프리팹이 비어 있습니다.");
        }
        else
        {
            Debug.Log($"[하율 궁극기] HitResolver 초기화 완료 | VFX={attachedSparkVfxPrefab.name}");
        }
    }

    private void EnsurePlayerRef()
    {
        if (playerTransform != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(center, hitRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(center, chainSearchRadius);
    }
#endif
}