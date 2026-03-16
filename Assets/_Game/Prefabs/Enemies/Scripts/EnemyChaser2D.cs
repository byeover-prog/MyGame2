// UTF-8
using UnityEngine;

/// <summary>
/// 적 추적 이동.
/// [최적화] FindGameObjectWithTag를 매 FixedUpdate에서 호출하지 않고 캐싱.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyChaser2D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float move_speed = 3.0f;

    [Header("타겟(비워두면 자동 탐색)")]
    [SerializeField] private Transform target;

    [Header("물리")]
    [SerializeField] private Rigidbody2D rb;

    [Header("탐색 태그")]
    [SerializeField] private string player_tag = "Player";

    // ★ static 캐싱: 모든 적이 같은 플레이어를 참조하므로 한 번만 탐색
    private static Transform _cachedPlayer;
    private static float _lastSearchTime;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // 풀에서 재활성화될 때 캐시된 플레이어 사용
        if (target == null && _cachedPlayer != null)
            target = _cachedPlayer;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (target == null)
        {
            // ★ 0.5초에 한 번만 탐색 (이전: 매 FixedUpdate → 40마리 × 50fps = 2000회/초)
            if (Time.time - _lastSearchTime > 0.5f)
            {
                _lastSearchTime = Time.time;
                GameObject player = GameObject.FindGameObjectWithTag(player_tag);
                if (player != null)
                {
                    _cachedPlayer = player.transform;
                    target = _cachedPlayer;
                }
            }

            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = (target.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        dir.Normalize();
        rb.linearVelocity = dir * move_speed;
    }
}