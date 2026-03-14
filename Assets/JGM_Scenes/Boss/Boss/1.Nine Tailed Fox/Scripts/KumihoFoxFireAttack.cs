using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class KumihoFoxFireAttack : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("구미호 보스")]
    [SerializeField] private Transform boss;

    [Tooltip("플레이어")]
    [SerializeField] private Transform player;

    [Tooltip("여우불 프리팹")]
    [SerializeField] private GameObject foxFirePrefab;



    [Header("공전 설정")]

    [Tooltip("공전 반경")]
    [SerializeField] private float radius = 2.5f;

    [Tooltip("공전 속도")]
    [SerializeField] private float angularSpeed = 180f;



    [Header("여우불 설정")]

    [Tooltip("최대 여우불 개수")]
    [SerializeField] private int fireCount = 8;

    [Tooltip("여우불 생성 간격")]
    [SerializeField] private float spawnDelay = 0.7f;



    [Header("패턴 설정")]

    [Tooltip("패턴 시작 딜레이")]
    [SerializeField] private float startDelay = 5f;

    [Tooltip("공전 시간")]
    [SerializeField] private float orbitTime = 2f;

    [Tooltip("패턴 반복 시간")]
    [SerializeField] private float patternDelay = 5f;



    private readonly List<Transform> foxFires = new List<Transform>();

    private float angle;

    private bool isOrbiting = false;



    void Start()
    {
        if (boss == null)
            boss = transform;

        StartCoroutine(PatternLoop());
    }



    void Update()
    {
        if (!isOrbiting) return;

        angle += angularSpeed * Time.deltaTime;

        float step = 360f / Mathf.Max(1, foxFires.Count);

        for (int i = 0; i < foxFires.Count; i++)
        {
            // Destroy된 오브젝트 방지
            if (foxFires[i] == null) continue;

            float a = (angle + step * i) * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(a),
                Mathf.Sin(a),
                0f
            ) * radius;

            foxFires[i].position = boss.position + offset;
        }
    }



    IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return StartCoroutine(CreateFoxFires());

            isOrbiting = true;

            yield return new WaitForSeconds(orbitTime);

            LaunchAll();

            isOrbiting = false;

            yield return new WaitForSeconds(patternDelay);
        }
    }



    IEnumerator CreateFoxFires()
    {
        foxFires.Clear();

        isOrbiting = true;

        for (int i = 0; i < fireCount; i++)
        {
            GameObject obj = Instantiate(foxFirePrefab);

            Transform fire = obj.transform;

            foxFires.Add(fire);

            // 생성되자마자 보스 주변 위치에 배치
            float step = 360f / fireCount;
            float a = (step * i) * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(a),
                Mathf.Sin(a),
                0f
            ) * radius;

            fire.position = boss.position + offset;

            yield return new WaitForSeconds(spawnDelay);
        }
    }



    void LaunchAll()
    {
        foreach (var fire in foxFires)
        {
            if (fire == null) continue;

            Vector2 dir = (player.position - fire.position).normalized;

            fire.GetComponent<KumihoFoxFire>().Launch(dir);
        }

        // 발사 후 리스트 정리
        foxFires.Clear();
    }
}