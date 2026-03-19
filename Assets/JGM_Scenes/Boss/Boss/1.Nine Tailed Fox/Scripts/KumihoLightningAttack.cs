// UTF-8
using UnityEngine;
using System.Collections;

/// <summary>
/// [구현 원리 요약]
/// 보스 주변 "원형(링)" 형태로 번개를 떨어뜨리는 패턴
/// - 각도를 나눠서 일정 간격으로 생성
/// - 부모 없이 생성하여 위치 고정
/// </summary>

[DisallowMultipleComponent]
public class KumihoLightningAttack : MonoBehaviour
{
    [Header("===== 참조 =====")]

    [Tooltip("플레이어 Transform\n비워두면 자동 탐색")]
    [SerializeField] private Transform player;

    [Tooltip("번개 프리팹")]
    [SerializeField] private GameObject lightningPrefab;


    [Header("===== 원형 설정 =====")]

    [Tooltip("원 반경")]
    [SerializeField] private float radius = 5f;

    [Tooltip("번개 개수 (원에 배치됨)")]
    [SerializeField] private int spawnCount = 8;


    [Header("===== 타이밍 =====")]

    [Tooltip("패턴 반복 간격")]
    [SerializeField] private float interval = 4f;

    [Tooltip("경고 후 떨어지는 시간")]
    [SerializeField] private float delayBeforeStrike = 1f;


    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(LightningRoutine());
    }


    private IEnumerator LightningRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            SpawnCircleLightning();
        }
    }


    // 🔥 원형 배치 핵심 함수
    private void SpawnCircleLightning()
    {
        Vector2 center = transform.position;

        for (int i = 0; i < spawnCount; i++)
        {
            float angle = i * Mathf.PI * 2f / spawnCount;

            Vector2 pos = new Vector2(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius
            );

            StartCoroutine(SpawnLightning(pos));
        }
    }


    private IEnumerator SpawnLightning(Vector2 pos)
    {
        yield return new WaitForSeconds(delayBeforeStrike);

        // 부모 없음 → 고정 위치
        Instantiate(lightningPrefab, pos, Quaternion.identity);
    }
}