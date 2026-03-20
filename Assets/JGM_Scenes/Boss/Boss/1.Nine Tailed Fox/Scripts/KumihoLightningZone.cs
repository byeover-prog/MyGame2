// UTF-8
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [구현 원리 요약]
/// 장판 내부에서
/// - 플레이어 방향으로 위치 선택
/// - 번개끼리 최소 거리 유지
/// - 경고 원 표시 (타원 + 회전)
/// - 일정 시간 후 번개 낙하
/// </summary>

[DisallowMultipleComponent]
public class KumihoLightningZone : MonoBehaviour
{
    [Header("===== 번개 =====")]

    [Tooltip("번개 프리팹")]
    [SerializeField] private GameObject lightningPrefab;

    [Tooltip("경고 원 프리팹")]
    [SerializeField] private GameObject warningPrefab;

    [Tooltip("번개 생성 간격")]
    [SerializeField] private float spawnInterval = 0.4f;

    [Tooltip("한 번에 생성 개수")]
    [SerializeField] private int spawnPerTick = 2;

    [Tooltip("경고 후 번개 떨어지는 시간")]
    [SerializeField] private float warningTime = 0.6f;


    [Header("===== 장판 =====")]

    [Tooltip("장판 유지 시간")]
    [SerializeField] private float duration = 3f;

    private float radius;
    private Transform player;


    [Header("===== 간격 설정 =====")]

    [Tooltip("번개 최소 거리")]
    [SerializeField] private float minDistance = 1.5f;

    [Tooltip("재시도 횟수")]
    [SerializeField] private int maxTries = 10;

    private List<Vector2> recentPositions = new List<Vector2>();


    [Header("===== 경고 원 연출 =====")]

    [Tooltip("경고 원 Y축 눌림 (0.5~0.7 추천)")]
    [SerializeField] private float warningScaleY = 0.6f;

    [Tooltip("경고 원 회전 각도")]
    [SerializeField] private float warningRotation = 45f;


    // 초기화
    public void Init(float r, Transform target)
    {
        radius = r;
        player = target;

        // 장판 크기
        transform.localScale = Vector3.one * radius * 2f;

        StartCoroutine(ZoneRoutine());
    }


    private IEnumerator ZoneRoutine()
    {
        float time = 0f;

        while (time < duration)
        {
            SpawnLightning();

            yield return new WaitForSeconds(spawnInterval);
            time += spawnInterval;
        }

        Destroy(gameObject);
    }


    private void SpawnLightning()
    {
        if (lightningPrefab == null)
        {
            Debug.LogError("번개 프리팹 없음");
            return;
        }

        for (int i = 0; i < spawnPerTick; i++)
        {
            Vector2 pos = GetSpacedPosition();

            StartCoroutine(SpawnWithWarning(pos));
        }
    }


    /// <summary>
    /// 경고 → 번개 낙하
    /// </summary>
    private IEnumerator SpawnWithWarning(Vector2 pos)
    {
        GameObject warning = null;

        if (warningPrefab != null)
        {
            // 🔥 회전 적용
            Quaternion rot = Quaternion.Euler(0f, 0f, warningRotation);

            warning = Instantiate(warningPrefab, pos, rot);

            // 🔥 타원 형태 (바닥 느낌)
            warning.transform.localScale = new Vector3(1f, warningScaleY, 1f);
        }

        yield return new WaitForSeconds(warningTime);

        Instantiate(lightningPrefab, pos, Quaternion.identity);

        if (warning != null)
            Destroy(warning);
    }


    /// <summary>
    /// 최소 거리 보장
    /// </summary>
    private Vector2 GetSpacedPosition()
    {
        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            Vector2 pos = GetRandomPointTowardsPlayer();

            bool valid = true;

            foreach (var prev in recentPositions)
            {
                if (Vector2.Distance(pos, prev) < minDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                recentPositions.Add(pos);

                if (recentPositions.Count > 10)
                    recentPositions.RemoveAt(0);

                return pos;
            }
        }

        return GetRandomPointTowardsPlayer();
    }


    /// <summary>
    /// 플레이어 방향 기준 랜덤 위치
    /// </summary>
    private Vector2 GetRandomPointTowardsPlayer()
    {
        Vector2 center = transform.position;

        if (player == null)
            return center;

        Vector2 dir = ((Vector2)player.position - center).normalized;

        float baseAngle = Mathf.Atan2(dir.y, dir.x);

        float spread = Mathf.PI; // 넓게 퍼짐

        float randomAngle = baseAngle + Random.Range(-spread, spread);

        float randomRadius = Random.Range(radius * 0.7f, radius);

        Vector2 pos = new Vector2(
            center.x + Mathf.Cos(randomAngle) * randomRadius,
            center.y + Mathf.Sin(randomAngle) * randomRadius
        );

        return pos;
    }
}