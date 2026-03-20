// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 보스를 따라다니는 어둠 영역입니다.
/// 자식 CircleCollider2D를 직접 검사해서 플레이어가 범위 안에 있으면
/// 일정 간격마다 독 데미지를 줍니다.
/// 트리거 이벤트에 의존하지 않아 구조 문제에 강합니다.
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperDarkZone : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("따라갈 보스 Transform")]
    [SerializeField] private Transform boss;

    [Tooltip("데미지 판정용 원형 콜라이더")]
    [SerializeField] private CircleCollider2D damageArea;



    [Header("데미지 설정")]

    [Tooltip("1틱당 독 데미지")]
    [SerializeField] private int damage = 5;

    [Tooltip("데미지 적용 간격(초)")]
    [SerializeField] private float tickInterval = 1f;



    [Header("영역 유지 설정")]

    [Tooltip("영역 유지 시간(초)")]
    [SerializeField] private float duration = 5f;



    [Header("대상 레이어")]

    [Tooltip("플레이어 레이어만 체크")]
    [SerializeField] private LayerMask playerLayer;



    private float tickTimer;
    private float lifeTimer;
    private readonly Collider2D[] hitResults = new Collider2D[16];
    private bool isRunning;



    /// <summary>
    /// 보스 추적 대상 초기화
    /// </summary>
    public void Init(Transform bossTransform)
    {
        boss = bossTransform;
    }



    /// <summary>
    /// 영역 시작
    /// </summary>
    public void ActivateZone()
    {
        isRunning = true;
        tickTimer = 0f;
        lifeTimer = 0f;
    }



    private void Awake()
    {
        // 자식 Circle을 자동 탐색
        if (damageArea == null)
        {
            damageArea = GetComponentInChildren<CircleCollider2D>();
        }
    }



    private void Update()
    {
        if (!isRunning)
            return;

        // 보스를 따라다님
        if (boss != null)
        {
            transform.position = boss.position;
        }

        // 유지 시간 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // 틱 간격 체크
        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval)
            return;

        tickTimer = 0f;

        ApplyPoisonDamage();
    }



    /// <summary>
    /// 현재 원형 범위 안의 플레이어를 직접 검사해서 데미지를 적용
    /// </summary>
    private void ApplyPoisonDamage()
    {
        if (damageArea == null)
        {
            Debug.LogError("GrimReaperDarkZone: damageArea가 비어 있습니다.");
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = playerLayer;
        filter.useTriggers = true;

        int hitCount = damageArea.Overlap(filter, hitResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D target = hitResults[i];
            if (target == null)
                continue;

            PlayerHealth playerHealth =
                target.GetComponent<PlayerHealth>() ??
                target.GetComponentInParent<PlayerHealth>() ??
                target.GetComponentInChildren<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}