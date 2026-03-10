using UnityEngine;

/// <summary>
/// 공통스킬: 화살비(ArrowRain)
/// - "체력이 많은 적" 위치에 장판을 생성한다.
/// - 장판은 일정 시간 동안 틱 피해 + 낙하 화살 연출을 수행한다.
///
/// CommonSkillLevelParams 매핑
/// - cooldown        : 장판 생성 주기
/// - damage          : 틱 피해
/// - hitInterval     : 틱 간격(작을수록 자주 피해)
/// - lifeSeconds     : 장판 지속시간
/// - explosionRadius : 장판 반경
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainWeapon2D : CommonSkillWeapon2D
{
    [Header("ArrowRain")]
    [SerializeField] private ArrowRainArea2D areaPrefab;

    [Tooltip("타겟(적) 탐색 반경")]
    [SerializeField] private float targetSearchRadius = 20f;

    // 간단 재사용(동시에 여러 장판을 깔고 싶으면 캐시 1개 방식은 바꿔야 함)
    private ArrowRainArea2D _cachedArea;

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (!TryGetHighestHpTarget(out var targetTr))
            return;

        // 쿨다운 소비는 "실제 발사 시점"에 TryBeginFireConsumeCooldown 내부에서 처리됨
        TryBeginFireConsumeCooldown(() => Fire(targetTr));
    }

    private void Fire(Transform target)
    {
        if (owner == null) return;

        if (areaPrefab == null)
        {
            Debug.LogWarning("[ArrowRainWeapon2D] areaPrefab 누락", this);
            return;
        }

        if (target == null) return;

        var p = P;

        var area = GetAreaInstance();
        area.transform.position = target.position;

        // 레벨 파라미터 적용
        area.Setup(
            newRadius: Mathf.Max(0.5f, p.explosionRadius),
            newDurationSeconds: Mathf.Max(0.25f, p.lifeSeconds),
            newDamageTickInterval: Mathf.Max(0.05f, p.hitInterval),
            newDamagePerTick: Mathf.Max(0, p.damage),
            newEnemyMask: enemyMask
        );

        if (!area.gameObject.activeSelf)
            area.gameObject.SetActive(true);
    }

    private ArrowRainArea2D GetAreaInstance()
    {
        if (_cachedArea != null) return _cachedArea;

        var go = Instantiate(areaPrefab.gameObject);
        go.name = areaPrefab.gameObject.name;

        if (owner != null)
            go.transform.SetParent(owner, worldPositionStays: true);

        _cachedArea = go.GetComponent<ArrowRainArea2D>();
        if (_cachedArea != null)
            _cachedArea.gameObject.SetActive(false);

        return _cachedArea;
    }

    private bool TryGetHighestHpTarget(out Transform target)
    {
        target = null;

        Vector3 origin = (owner != null) ? owner.position : transform.position;
        var hits = Physics2D.OverlapCircleAll(origin, targetSearchRadius, enemyMask);
        if (hits == null || hits.Length == 0)
            return false;

        int bestHp = int.MinValue;
        float bestFallbackScore = float.MinValue;
        Transform best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;

            var hp = h.GetComponentInParent<EnemyHealth2D>();
            if (hp != null)
            {
                int cur = hp.CurrentHp;
                if (cur > bestHp)
                {
                    bestHp = cur;
                    best = h.transform;
                }
            }
            else
            {
                // fallback: 체력 컴포넌트 없으면 "멀리 있는 적"을 대신 선택
                float score = (h.transform.position - origin).sqrMagnitude;
                if (best == null && score > bestFallbackScore)
                {
                    bestFallbackScore = score;
                    best = h.transform;
                }
            }
        }

        if (best == null) return false;
        target = best;
        return true;
    }

    protected override void OnLevelChanged()
    {
        // 필요하면 레벨 변경 시 추가 세팅(캐시 초기화/연출 파라미터 등)
        // _cachedArea = null;  // 레벨 변경 때 장판을 새로 만들고 싶으면 활성화
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var o = (owner != null) ? owner.position : transform.position;
        Gizmos.DrawWireSphere(o, targetSearchRadius);
    }
#endif
}