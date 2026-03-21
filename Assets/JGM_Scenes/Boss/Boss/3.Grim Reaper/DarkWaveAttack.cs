// UTF-8
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DarkWaveAttack : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("플레이어")]
    [SerializeField] private Transform player;

    [Tooltip("파도 프리팹 (긴 벽 형태)")]
    [SerializeField] private GameObject wavePrefab;


    [Header("설정")]

    [Tooltip("보스 뒤 거리")]
    [SerializeField] private float backDistance = 2f;

    [Tooltip("파도 이동 속도")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("패턴 시작 딜레이")]
    [SerializeField] private float startDelay = 3f;

    [Tooltip("재사용 대기시간")]
    [SerializeField] private float cooldown = 6f;


    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(PatternLoop());
    }


    private IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            SpawnWave();

            yield return new WaitForSeconds(cooldown);
        }
    }


    private void SpawnWave()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        // 👉 보스 뒤쪽 위치
        Vector3 spawnPos = transform.position - dir * backDistance;

        // 👉 방향 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        GameObject wave = Instantiate(wavePrefab, spawnPos, rot);

        DarkWaveMover mover = wave.GetComponent<DarkWaveMover>();
        mover.Init(dir, moveSpeed);
    }
}