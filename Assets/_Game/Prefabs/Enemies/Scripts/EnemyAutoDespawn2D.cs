// ============================================================
// 파일: Assets/Scripts/Enemy_Scripts/EnemyAutoDespawn2D.cs
// 역할: 적 자동 정리(폭주 방지 안전장치)
// - 플레이어에서 일정 거리 이상 멀어지면 제거
// - 스폰 폭주/길막/추적 실패로 쌓이는 경우를 강제로 정리
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAutoDespawn2D : MonoBehaviour
{
    [Header("기준(플레이어)")]
    [SerializeField] private Transform player;

    [Header("정리 거리")]
    [Tooltip("플레이어에서 이 거리 이상 멀어지면 적 제거")]
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

    private void Update()
    {
        if (player == null) return;

        float d = (player.position - transform.position).sqrMagnitude;
        if (d >= despawn_distance_sqr)
        {
            Destroy(gameObject);
        }
    }
}
