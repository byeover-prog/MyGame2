// UTF-8
using UnityEngine;

/// <summary>
/// 플레이어에서 일정 거리 이상 멀어진 적을 자동 정리.
/// [최적화] Destroy → EnemyPoolTag.ReturnToPool
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAutoDespawn2D : MonoBehaviour
{
    [Header("기준(플레이어)")]
    [SerializeField] private Transform player;

    [Header("정리 거리")]
    [SerializeField] private float despawn_distance = 25f;

    private float despawn_distance_sqr;

    private void Start()
    {
        despawn_distance_sqr = despawn_distance * despawn_distance;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    // ★ OnEnable에서 플레이어 재탐색 (풀에서 재활성화될 때 대비)
    private void OnEnable()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float d = (player.position - transform.position).sqrMagnitude;
        if (d >= despawn_distance_sqr)
        {
            // ★ Destroy → 풀 반환
            EnemyPoolTag.ReturnToPool(gameObject);
        }
    }
}