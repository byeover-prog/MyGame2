using UnityEngine;
using System.Collections;

/// <summary>
/// 구미호 여우구슬 버프 시스템
/// FirePointController와 동일한 방식으로
/// 플레이어 기준 "보스 뒤쪽"에 여우구슬을 배치한다.
/// </summary>
[DisallowMultipleComponent]
public class KumihoFoxOrbBuff : MonoBehaviour
{
    [Header("===== 대상 설정 =====")]

    [Tooltip("구미호 보스 Transform")]
    [SerializeField] private Transform boss;

    [Tooltip("플레이어 Transform (비워두면 자동 탐색)")]
    [SerializeField] private Transform player;

    [Tooltip("여우구슬 생성 위치 Transform")]
    [SerializeField] private Transform foxOrbSpawnPoint;

    [Tooltip("여우구슬 프리팹")]
    [SerializeField] private GameObject foxOrbPrefab;

    [Tooltip("기본 공격 스크립트")]
    [SerializeField] private KumihoBasicAttack basicAttack;



    [Header("===== 위치 설정 =====")]

    [Tooltip("보스 뒤쪽 거리")]
    [SerializeField] private float distance = 1.0f;



    [Header("===== 여우구슬 설정 =====")]

    [Tooltip("여우구슬 지속시간")]
    [SerializeField] private float orbDuration = 6f;

    [Tooltip("강화 상태 투사체 수")]
    [SerializeField] private int buffProjectileCount = 3;

    [Tooltip("여우구슬 회전 속도")]
    [SerializeField] private float rotateSpeed = 120f;



    [Header("===== 패턴 설정 =====")]

    [Tooltip("여우구슬 패턴 간격")]
    [SerializeField] private float patternInterval = 10f;



    private GameObject currentOrb;

    private float timer;

    private bool isBuffActive;



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
        if (boss == null || player == null || foxOrbSpawnPoint == null)
            return;

        UpdateSpawnPoint();

        if (currentOrb != null)
        {
            currentOrb.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        timer += Time.deltaTime;

        if (timer >= patternInterval)
        {
            timer = 0;

            ActivateFoxOrbBuff();
        }
    }



    /// <summary>
    /// SpawnPoint를 보스 뒤쪽으로 이동
    /// </summary>
    void UpdateSpawnPoint()
    {
        Vector2 dir = (player.position - boss.position).normalized;

        // 뒤쪽 위치
        foxOrbSpawnPoint.position = boss.position - (Vector3)(dir * distance);
    }



    /// <summary>
    /// 여우구슬 버프 시작
    /// </summary>
    public void ActivateFoxOrbBuff()
    {
        if (foxOrbPrefab == null || isBuffActive)
            return;

        StartCoroutine(CoFoxOrbBuff());
    }



    IEnumerator CoFoxOrbBuff()
    {
        isBuffActive = true;

        if (currentOrb != null)
            Destroy(currentOrb);

        // SpawnPoint 자식으로 생성
        currentOrb = Instantiate(foxOrbPrefab, foxOrbSpawnPoint);

        currentOrb.transform.localPosition = Vector3.zero;

        if (basicAttack != null)
            basicAttack.SetProjectileCount(buffProjectileCount);

        yield return new WaitForSeconds(orbDuration);

        if (basicAttack != null)
            basicAttack.SetProjectileCount(1);

        if (currentOrb != null)
            Destroy(currentOrb);

        isBuffActive = false;
    }
}