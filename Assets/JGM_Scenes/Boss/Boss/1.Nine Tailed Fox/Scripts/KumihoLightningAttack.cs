// UTF-8
using System.Collections;
using UnityEngine;

public class KumihoLightningAttack : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("플레이어")]
    [SerializeField] private Transform player;

    [Tooltip("번개 프리팹")]
    [SerializeField] private GameObject lightningPrefab;

    [Tooltip("경고 원 프리팹")]
    [SerializeField] private GameObject warningPrefab;


    [Header("범위 설정")]

    [Tooltip("최소 거리")]
    [SerializeField] private float minRange = 1.5f;

    [Tooltip("최대 거리")]
    [SerializeField] private float maxRange = 4f;


    [Header("패턴 설정")]

    [Tooltip("번개 개수")]
    [SerializeField] private int lightningCount = 6;

    [Tooltip("번개 간격")]
    [SerializeField] private float delay = 0.3f;

    [Tooltip("경고 후 떨어지는 시간")]
    [SerializeField] private float warningTime = 0.8f;


    void Start()
    {
        StartCoroutine(LightningPattern());
    }


    IEnumerator LightningPattern()
    {
        while (true)
        {
            for (int i = 0; i < lightningCount; i++)
            {
                StartCoroutine(SpawnLightningWithWarning());

                yield return new WaitForSeconds(delay);
            }

            yield return new WaitForSeconds(3f);
        }
    }


    IEnumerator SpawnLightningWithWarning()
    {
        if (player == null) yield break;

        // 랜덤 위치 계산
        Vector2 dir = Random.insideUnitCircle.normalized;
        float dist = Random.Range(minRange, maxRange);
        Vector3 pos = player.position + (Vector3)(dir * dist);

        // 🔥 경고 원 생성
        GameObject warning = null;

        if (warningPrefab != null)
        {
            warning = Instantiate(warningPrefab, pos, Quaternion.identity);
        }

        // 🔥 대기 (경고 시간)
        yield return new WaitForSeconds(warningTime);

        // 🔥 번개 생성
        Instantiate(lightningPrefab, pos, Quaternion.identity);

        // 🔥 경고 제거
        if (warning != null)
        {
            Destroy(warning);
        }
    }
}