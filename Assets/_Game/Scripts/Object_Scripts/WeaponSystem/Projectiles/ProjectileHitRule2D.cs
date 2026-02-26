// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - EnemyMask에 들어온 Collider2D만 처리.
// - 같은 적 중복 타격은 "타겟별 재타격 쿨"로 제어.
// - 재타격을 원하면 OnTriggerEnter만으로는 불가능하므로, OnTriggerStay에서 시간 조건으로 재타격 처리.
// - 관통이 아니면 첫 히트 후 delay 뒤 비활성화(풀 친화적).
[DisallowMultipleComponent]
public sealed class ProjectileHitRule2D : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("피격 대상으로 인정할 레이어 마스크(Enemy)")]
    [SerializeField] private LayerMask enemyMask;

    [Header("데미지")]
    [Tooltip("1회 타격 데미지")]
    [Min(0)]
    [SerializeField] private int damage = 1;

    [Header("규칙")]
    [Tooltip("관통 여부. ON이면 맞아도 계속 진행")]
    [SerializeField] private bool pierce = true;

    [Tooltip("같은 적 재타격 허용 간격(초). 0이면 같은 적은 1번만 맞음")]
    [Min(0f)]
    [SerializeField] private float sameTargetHitInterval = 0f;

    [Header("재타격 처리 방식")]
    [Tooltip("sameTargetHitInterval > 0일 때,\n- OFF: Enter에서만 판정(재진입해야 재타격 가능)\n- ON: Stay에서 시간 조건으로 재타격(겹쳐 있어도 재타격 가능)")]
    [SerializeField] private bool allowRehitWhileStaying = true;

    [Tooltip("Stay 판정 주기(초). 너무 촘촘하면 낭비, 너무 크면 재타격이 늦게 느껴짐.\n권장: 0.05~0.1")]
    [Min(0.01f)]
    [SerializeField] private float stayCheckInterval = 0.05f;

    [Tooltip("적에게 닿은 뒤 비활성화까지 지연(초). 관통 OFF일 때 사용")]
    [Min(0f)]
    [SerializeField] private float despawnDelayAfterHit = 0.2f;

    [Tooltip("히트 후 콜라이더를 꺼서 추가 트리거를 막음(관통 OFF일 때 추천)")]
    [SerializeField] private bool disableColliderOnHit = true;

    private readonly Dictionary<int, float> _nextHitTimeByTarget = new Dictionary<int, float>(64);
    private bool _despawnScheduled;
    private float _nextStayCheckTime;

    // 외부(무기/스킬)에서 런타임 세팅하고 싶을 때 사용
    public void Configure(LayerMask mask, int dmg, bool isPierce, float sameTargetInterval, float despawnDelay)
    {
        enemyMask = mask;
        damage = Mathf.Max(0, dmg);
        pierce = isPierce;
        sameTargetHitInterval = Mathf.Max(0f, sameTargetInterval);
        despawnDelayAfterHit = Mathf.Max(0f, despawnDelay);

        _nextHitTimeByTarget.Clear();
        _despawnScheduled = false;
        _nextStayCheckTime = 0f;
    }

    private void OnEnable()
    {
        _nextHitTimeByTarget.Clear();
        _despawnScheduled = false;
        _nextStayCheckTime = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 재타격을 "겹친 상태에서도" 만들려면 Stay가 필요
        if (!allowRehitWhileStaying) return;
        if (sameTargetHitInterval <= 0f) return;

        // Stay는 매 프레임 오므로, 체크 주기로 스로틀
        if (Time.time < _nextStayCheckTime) return;
        _nextStayCheckTime = Time.time + stayCheckInterval;

        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        // 타겟 ID(루트 기준)로 "같은 적" 판정
        int id = DamageUtil2D.GetRootInstanceId(other);

        // 같은 적 재타격 제한
        if (sameTargetHitInterval > 0f)
        {
            if (_nextHitTimeByTarget.TryGetValue(id, out float nextT) && Time.time < nextT)
                return;

            _nextHitTimeByTarget[id] = Time.time + sameTargetHitInterval;
        }
        else
        {
            // interval=0이면 같은 적은 1번만 맞게 처리
            if (_nextHitTimeByTarget.ContainsKey(id))
                return;

            _nextHitTimeByTarget[id] = float.PositiveInfinity;
        }

        // 데미지 적용(유틸 규격 유지)
        DamageUtil2D.TryApplyDamage(other, damage);

        // 관통이 아니면 소멸 스케줄
        if (!pierce && !_despawnScheduled)
        {
            _despawnScheduled = true;

            if (disableColliderOnHit)
            {
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }

            StartCoroutine(CoDespawnAfter(despawnDelayAfterHit));
        }
    }

    private IEnumerator CoDespawnAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}