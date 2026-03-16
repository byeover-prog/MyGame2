// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 저승사자의 "망혼 부활" 패턴입니다.
/// - 플레이어가 범위 안에 있으면 일정 주기로 발동합니다.
/// - 저승사자 앞쪽에서 망령들을 소환합니다.
/// - 소환된 망령은 잠깐 등장 연출 후 플레이어를 추적하고 공격합니다.
/// - 일정 시간이 지나면 자동으로 사라집니다.
/// - 보스가 죽거나 비활성화되면 소환된 망령도 함께 제거합니다.
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperSoulSummon : MonoBehaviour
{
    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform입니다.\n비워두면 Player 태그를 자동 탐색합니다.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("저승사자 Animator입니다.\n애니메이션이 아직 없다면 비워도 됩니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("좌우 반전용 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("===== 망혼 부활 발동 조건 =====")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 망혼 부활 패턴을 사용할 수 있습니다.")]
    [SerializeField] private float skillRange = 8f;

    [Tooltip("망혼 부활 재사용 대기시간입니다.")]
    [SerializeField] private float skillCooldown = 10f;

    [Tooltip("스킬 시작 후 실제 소환까지 대기 시간입니다.\n추후 소환 애니메이션 길이에 맞춰 조정하면 됩니다.")]
    [SerializeField] private float summonCastDelay = 0.8f;

    [Tooltip("망혼 부활 사용 중 저승사자가 멈출지 여부입니다.")]
    [SerializeField] private bool stopWhileCasting = true;

    [Header("===== 망령 소환 설정 =====")]
    [Tooltip("망령으로 사용할 프리팹입니다.\n현재는 저승사자 프리팹 복사본을 연결하면 됩니다.")]
    [SerializeField] private GameObject soulMinionPrefab;

    [Tooltip("한 번에 소환할 망령 수입니다.")]
    [SerializeField] private int summonCount = 3;

    [Tooltip("저승사자 앞쪽으로 얼마나 떨어진 곳에서 소환할지 설정합니다.")]
    [SerializeField] private float forwardDistance = 2.2f;

    [Tooltip("좌우로 퍼지는 간격입니다.")]
    [SerializeField] private float sideSpacing = 1.2f;

    [Tooltip("소환 위치에 약간의 랜덤 오차를 줍니다.")]
    [SerializeField] private float randomOffset = 0.25f;

    [Tooltip("소환 직후 땅속에서 올라오는 듯한 시작 높이입니다.\n음수 값으로 두면 아래에서 올라오는 느낌을 줄 수 있습니다.")]
    [SerializeField] private float spawnStartYOffset = -0.8f;

    [Header("===== 망령 스탯 전달 =====")]
    [Tooltip("망령 유지 시간입니다.")]
    [SerializeField] private float minionLifetime = 8f;

    [Tooltip("망령 이동 속도입니다.")]
    [SerializeField] private float minionMoveSpeed = 2.4f;

    [Tooltip("망령 공격 거리입니다.")]
    [SerializeField] private float minionAttackRange = 1.4f;

    [Tooltip("망령 공격 데미지입니다.")]
    [SerializeField] private int minionAttackDamage = 8;

    [Tooltip("망령 공격 간격입니다.")]
    [SerializeField] private float minionAttackInterval = 0.9f;

    [Tooltip("망령이 등장 후 실제 행동 시작까지 대기 시간입니다.")]
    [SerializeField] private float minionRiseDelay = 0.5f;

    [Header("===== 상태 확인 =====")]
    [Tooltip("현재 망혼 부활 시전 중인지 표시합니다.")]
    [SerializeField] private bool isCasting;

    [Tooltip("현재 살아있는 망령 목록입니다.")]
    [SerializeField] private List<GrimReaperSoulMinion> activeMinions = new List<GrimReaperSoulMinion>();

    private float cooldownTimer;
    private Coroutine castCoroutine;

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        CleanupMinionList();

        if (playerTarget == null)
            return;

        cooldownTimer += Time.deltaTime;

        FacePlayer();

        if (isCasting)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);
        if (distance > skillRange)
            return;

        if (cooldownTimer < skillCooldown)
            return;

        castCoroutine = StartCoroutine(CoCastSoulSummon());
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerTarget = playerObject.transform;
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTarget == null)
            return;

        if (playerTarget.position.x < transform.position.x)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }

    private IEnumerator CoCastSoulSummon()
    {
        isCasting = true;
        cooldownTimer = 0f;

        if (animator != null)
        {
            animator.SetTrigger("SoulSummon");
        }

        float elapsed = 0f;
        while (elapsed < summonCastDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        SummonSouls();

        isCasting = false;
        castCoroutine = null;
    }

    private void SummonSouls()
    {
        if (soulMinionPrefab == null)
        {
            Debug.LogWarning("[GrimReaperSoulSummon] soulMinionPrefab이 비어 있습니다.");
            return;
        }

        Vector2 forward = GetForwardDirection();
        Vector2 right = new Vector2(-forward.y, forward.x);

        for (int i = 0; i < summonCount; i++)
        {
            float centerOffset = i - (summonCount - 1) * 0.5f;

            Vector2 basePosition =
                (Vector2)transform.position +
                forward * forwardDistance +
                right * (centerOffset * sideSpacing);

            Vector2 random = new Vector2(
                Random.Range(-randomOffset, randomOffset),
                Random.Range(-randomOffset, randomOffset)
            );

            Vector2 finalPosition = basePosition + random;
            finalPosition.y += spawnStartYOffset;

            GameObject minionObject = Instantiate(
                soulMinionPrefab,
                finalPosition,
                Quaternion.identity
            );

            GrimReaperSoulMinion minion = minionObject.GetComponent<GrimReaperSoulMinion>();
            if (minion != null)
            {
                minion.Setup(
                    owner: this.transform,
                    player: playerTarget,
                    moveSpeed: minionMoveSpeed,
                    attackRange: minionAttackRange,
                    attackDamage: minionAttackDamage,
                    attackInterval: minionAttackInterval,
                    riseDelay: minionRiseDelay,
                    lifeTime: minionLifetime,
                    spawnYOffset: -spawnStartYOffset
                );

                activeMinions.Add(minion);
            }
            else
            {
                Debug.LogWarning("[GrimReaperSoulSummon] 소환 프리팹에 GrimReaperSoulMinion 컴포넌트가 없습니다.");
            }
        }

        CleanupMinionList();
    }

    private Vector2 GetForwardDirection()
    {
        if (playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
                return dir;
        }

        if (spriteRenderer != null && spriteRenderer.flipX)
            return Vector2.left;

        return Vector2.right;
    }

    private void CleanupMinionList()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] == null)
                activeMinions.RemoveAt(i);
        }
    }

    private void DestroyAllMinions()
    {
        CleanupMinionList();

        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] != null)
            {
                Destroy(activeMinions[i].gameObject);
            }
        }

        activeMinions.Clear();
    }

    public bool IsCasting()
    {
        return isCasting;
    }

    private void OnDisable()
    {
        if (castCoroutine != null)
        {
            StopCoroutine(castCoroutine);
            castCoroutine = null;
        }

        isCasting = false;
        DestroyAllMinions();
    }

    private void OnDestroy()
    {
        DestroyAllMinions();
    }
}