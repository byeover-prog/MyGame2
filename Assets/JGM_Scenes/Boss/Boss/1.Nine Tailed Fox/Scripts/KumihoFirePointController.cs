using UnityEngine;

[DisallowMultipleComponent]
public class KumihoFirePointController : MonoBehaviour
{
    /* =========================================================
     * FirePoint 자동 위치 조정
     * - 보스가 바라보는 방향 앞에 FirePoint 배치
     * - 좌우 / 상하 모두 대응
     * ========================================================= */

    [Header("===== 대상 설정 =====")]

    [Tooltip("구미호 보스 Transform")]
    [SerializeField] private Transform boss;

    [Tooltip("플레이어 Transform (비워두면 Player 태그 자동 탐색)")]
    [SerializeField] private Transform player;


    [Header("===== 위치 설정 =====")]

    [Tooltip("보스 앞쪽 거리")]
    [SerializeField] private float distance = 0.8f;


    [Header("===== 디버그 =====")]

    [Tooltip("디버그 Gizmos 표시")]
    [SerializeField] private bool showDebug = true;


    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
    }


    void Update()
    {
        if (boss == null || player == null) return;

        // 보스 → 플레이어 방향
        Vector2 dir = (player.position - boss.position).normalized;

        // FirePoint 위치를 보스 앞쪽으로 이동
        transform.position = boss.position + (Vector3)(dir * distance);
    }


    void OnDrawGizmos()
    {
        if (!showDebug || boss == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(boss.position, transform.position);
    }
}