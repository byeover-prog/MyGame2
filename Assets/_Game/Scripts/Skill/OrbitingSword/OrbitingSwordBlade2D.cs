// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 회전검이 적과 Trigger로 접촉하면 데미지를 준다.
// - 동일 적에게 너무 촘촘히 연속 타격되지 않도록 간단한 쿨다운을 둔다.
[DisallowMultipleComponent]
public sealed class OrbitingSwordBlade2D : MonoBehaviour
{
    [Header("Hit")]
    [Tooltip("같은 적 재타격 최소 간격(초)")]
    [SerializeField] private float hitInterval = 0.25f;

    private LayerMask _enemyMask;
    private int _damage;

    // 적(리짓바디 기준)별 다음 타격 가능 시간
    private readonly Dictionary<int, float> _nextHitTime = new Dictionary<int, float>(64);

    public void Bind(LayerMask enemyMask, int damage)
    {
        _enemyMask = enemyMask;
        _damage = Mathf.Max(0, damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int layer = other.gameObject.layer;
        if (((1 << layer) & _enemyMask.value) == 0) return;

        int key = other.attachedRigidbody != null ? other.attachedRigidbody.GetInstanceID() : other.GetInstanceID();
        float now = Time.time;

        if (_nextHitTime.TryGetValue(key, out float next) && now < next)
            return;

        _nextHitTime[key] = now + hitInterval;

        var hitOwner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        // 적 체력 스크립트가 TakeDamage(int)를 갖고 있으면 적용됨
        hitOwner.SendMessage("TakeDamage", _damage, SendMessageOptions.DontRequireReceiver);
    }
}