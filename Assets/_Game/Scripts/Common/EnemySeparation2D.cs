using UnityEngine;

// [구현 원리 요약]
// - "적끼리 물리 충돌" 대신, 겹치는 만큼만 살짝 밀어내서 한 점 뭉침을 완화한다.
// - 동적 Rigidbody 난리/지터 없이도 시각적으로 분산이 된다.
// - 적 이동 스크립트를 바꾸기 싫을 때 LateUpdate에서 보정하는 방식.
[DisallowMultipleComponent]
public sealed class EnemySeparation2D : MonoBehaviour
{
    [Header("Separation")]
    [Tooltip("겹침 판정 반경(적 콜라이더 반경과 비슷하게)")]
    [SerializeField] private float separationRadius = 0.35f;

    [Tooltip("밀어내는 힘(너무 크면 떨림)")]
    [SerializeField] private float pushStrength = 1.8f;

    [Tooltip("초당 최대 보정 이동량(난리 방지용 상한)")]
    [SerializeField] private float maxPushPerSecond = 2.5f;

    [Tooltip("적 레이어 마스크(Enemy 레이어를 넣어주세요)")]
    [SerializeField] private LayerMask enemyMask;

    private static readonly Collider2D[] _buffer = new Collider2D[64];

    private void LateUpdate()
    {
        Vector2 pos = transform.position;

        int count = Physics2DCompat.OverlapCircleNonAlloc(pos, separationRadius, _buffer, enemyMask);
        if (count <= 1) return;

        Vector2 sum = Vector2.zero;
        int contributors = 0;

        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (col == null) continue;

            // 자기 자신 제외
            if (col.transform == transform) continue;

            // 중심점 기준으로 밀어내기(간단/안정)
            Vector2 other = col.bounds.center;
            Vector2 diff = pos - other;
            float dist = diff.magnitude;

            if (dist < 0.0001f)
                continue;

            float t = 1f - Mathf.Clamp01(dist / separationRadius);
            if (t <= 0f) continue;

            sum += diff.normalized * t;
            contributors++;
        }

        if (contributors <= 0) return;

        Vector2 dir = (sum / contributors).normalized;
        float step = Mathf.Min(maxPushPerSecond * Time.deltaTime, maxPushPerSecond);

        transform.position += (Vector3)(dir * pushStrength * step);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}