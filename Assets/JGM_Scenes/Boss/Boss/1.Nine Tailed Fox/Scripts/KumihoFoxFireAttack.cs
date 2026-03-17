// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class KumihoFoxFireAttack : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("구미호 보스\n비워두면 현재 오브젝트 Transform을 자동 사용합니다.")]
    [SerializeField] private Transform boss;

    [Tooltip("플레이어\n비워두면 Player 태그를 자동 탐색합니다.")]
    [SerializeField] private Transform player;

    [Tooltip("여우불 프리팹")]
    [SerializeField] private GameObject foxFirePrefab;

    [Tooltip("보스 체력 스크립트\n비워두면 같은 오브젝트에서 자동 탐색합니다.")]
    [SerializeField] private BossHealth2D bossHealth;


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


    [Header("사망 처리")]

    [Tooltip("보스가 죽으면 남아있는 여우불을 모두 삭제합니다.")]
    [SerializeField] private bool destroyFoxFireOnBossDeath = true;


    private readonly List<KumihoFoxFire> foxFires = new List<KumihoFoxFire>();

    private float angle;
    private bool isOrbiting = false;
    private bool isDeadHandled = false;
    private Coroutine patternCoroutine;


    private void Start()
    {
        if (boss == null)
            boss = transform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth2D>();

        patternCoroutine = StartCoroutine(PatternLoop());
    }


    private void Update()
    {
        // 보스 사망 체크
        CheckBossDeath();

        if (!isOrbiting) return;
        if (boss == null) return;

        angle += angularSpeed * Time.deltaTime;

        CleanupNullFoxFires();

        float step = 360f / Mathf.Max(1, foxFires.Count);

        for (int i = 0; i < foxFires.Count; i++)
        {
            if (foxFires[i] == null) continue;
            if (foxFires[i].IsLaunched) continue;

            float a = (angle + step * i) * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(a),
                Mathf.Sin(a),
                0f
            ) * radius;

            foxFires[i].transform.position = boss.position + offset;
        }
    }


    private IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            if (IsBossDead())
                yield break;

            yield return StartCoroutine(CreateFoxFires());

            if (IsBossDead())
                yield break;

            isOrbiting = true;

            yield return new WaitForSeconds(orbitTime);

            if (IsBossDead())
                yield break;

            LaunchAll();

            isOrbiting = false;

            yield return new WaitForSeconds(patternDelay);
        }
    }


    private IEnumerator CreateFoxFires()
    {
        foxFires.Clear();
        isOrbiting = true;

        for (int i = 0; i < fireCount; i++)
        {
            if (IsBossDead())
                yield break;

            if (foxFirePrefab == null)
                yield break;

            GameObject obj = Instantiate(foxFirePrefab);

            KumihoFoxFire fire = obj.GetComponent<KumihoFoxFire>();
            if (fire == null)
            {
                Debug.LogWarning("KumihoFoxFire 컴포넌트가 여우불 프리팹에 없습니다.", obj);
                Destroy(obj);
                continue;
            }

            fire.SetOwner(this);
            foxFires.Add(fire);

            // 생성되자마자 보스 주변 위치에 배치
            float step = 360f / fireCount;
            float a = (step * i) * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(a),
                Mathf.Sin(a),
                0f
            ) * radius;

            if (boss != null)
                fire.transform.position = boss.position + offset;

            yield return new WaitForSeconds(spawnDelay);
        }
    }


    private void LaunchAll()
    {
        CleanupNullFoxFires();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        for (int i = 0; i < foxFires.Count; i++)
        {
            if (foxFires[i] == null) continue;
            if (player == null) continue;

            Vector2 dir = (player.position - foxFires[i].transform.position).normalized;
            foxFires[i].Launch(dir);
        }

        // 발사 후 리스트 정리
        foxFires.Clear();
    }


    private void CheckBossDeath()
    {
        if (isDeadHandled)
            return;

        if (IsBossDead())
        {
            isDeadHandled = true;
            isOrbiting = false;

            if (patternCoroutine != null)
            {
                StopCoroutine(patternCoroutine);
                patternCoroutine = null;
            }

            if (destroyFoxFireOnBossDeath)
            {
                DestroyAllFoxFires();
            }
        }
    }


    private bool IsBossDead()
    {
        if (bossHealth == null)
            return false;

        return bossHealth.IsDead;
    }


    /// <summary>
    /// [구현 원리 요약]
    /// 보스가 죽거나 파괴될 때 남은 여우불을 전부 제거합니다.
    /// </summary>
    public void DestroyAllFoxFires()
    {
        CleanupNullFoxFires();

        for (int i = 0; i < foxFires.Count; i++)
        {
            if (foxFires[i] != null)
            {
                Destroy(foxFires[i].gameObject);
            }
        }

        foxFires.Clear();
    }


    /// <summary>
    /// [구현 원리 요약]
    /// 여우불이 스스로 파괴될 때 공격 스크립트 목록에서도 제거합니다.
    /// </summary>
    public void UnregisterFoxFire(KumihoFoxFire fire)
    {
        if (fire == null)
            return;

        foxFires.Remove(fire);
    }


    /// <summary>
    /// [구현 원리 요약]
    /// 이미 삭제된 여우불 null 참조를 정리합니다.
    /// </summary>
    private void CleanupNullFoxFires()
    {
        for (int i = foxFires.Count - 1; i >= 0; i--)
        {
            if (foxFires[i] == null)
            {
                foxFires.RemoveAt(i);
            }
        }
    }


    private void OnDestroy()
    {
        if (destroyFoxFireOnBossDeath)
        {
            DestroyAllFoxFires();
        }
    }
}