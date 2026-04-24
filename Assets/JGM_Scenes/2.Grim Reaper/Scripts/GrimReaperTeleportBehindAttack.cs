// UTF-8
using System.Collections;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 저승사자가 서서히 사라진 뒤 플레이어 뒤로 순간이동하여 기습 공격하는 패턴입니다.
/// - 기존 GrimReapermove, GrimReaperBasicAttack은 수정하지 않고 그대로 사용합니다.
/// - 패턴 중에는 GrimReapermove.SetAttacking(true)로 이동을 멈춥니다.
/// - 페이드 아웃 → 플레이어 뒤 순간이동 → 방향 보정 → 페이드 인 → 잠깐 대기
/// - 순간이동 후 기존 기본 공격 스크립트가 자동으로 공격하도록 시간을 벌어주는 구조입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GrimReaperTeleportBehindAttack : MonoBehaviour
{
    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("저승사자 SpriteRenderer입니다. 비워두면 자식에서 자동 탐색합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("기존 이동 스크립트입니다.")]
    [SerializeField] private GrimReapermove moveController;

    [Tooltip("기존 기본 공격 스크립트입니다.")]
    [SerializeField] private GrimReaperBasicAttack basicAttack;

    [Header("===== 패턴 발동 설정 =====")]
    [Tooltip("패턴 재사용 대기시간입니다.")]
    [SerializeField] private float patternCooldown = 5f;

    [Tooltip("플레이어와 이 거리 안일 때만 패턴을 사용할 수 있습니다.")]
    [SerializeField] private float usableDistance = 8f;

    [Tooltip("플레이어와 너무 가까우면 패턴을 사용하지 않도록 하는 최소 거리입니다.")]
    [SerializeField] private float minDistanceToUse = 2f;

    [Tooltip("패턴 발동 확률입니다. 0~1 사이 값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float useChance = 0.45f;

    [Header("===== 순간이동 위치 설정 =====")]
    [Tooltip("플레이어 뒤쪽으로 얼마나 떨어져 나타날지 설정합니다.")]
    [SerializeField] private float backOffset = 1.2f;

    [Tooltip("플레이어 뒤 위치에 약간의 세로 보정값을 더합니다.")]
    [SerializeField] private float yOffset = 0f;

    [Header("===== 사라짐 / 등장 연출 =====")]
    [Tooltip("서서히 사라지는 시간입니다.")]
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Tooltip("완전히 사라진 상태로 잠깐 유지하는 시간입니다.")]
    [SerializeField] private float hiddenDuration = 0.12f;

    [Tooltip("다시 나타나는 시간입니다.")]
    [SerializeField] private float fadeInDuration = 0.2f;

    [Header("===== 공격 대기 설정 =====")]
    [Tooltip("순간이동 후 공격이 자연스럽게 나가도록 잠시 대기하는 시간입니다.")]
    [SerializeField] private float waitBeforeAllowMove = 0.75f;

    [Tooltip("패턴 종료 후 이동 재개 전 추가 후딜 시간입니다.")]
    [SerializeField] private float recoveryDelay = 0.15f;

    [Header("===== 충돌 / 위치 보정 =====")]
    [Tooltip("플레이어와 너무 겹치지 않도록 추가 보정할 거리입니다.")]
    [SerializeField] private float overlapSafetyOffset = 0.25f;

    [Tooltip("장애물 체크에 사용할 레이어 마스크입니다.")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("순간이동 목적지 충돌 체크 반경입니다.")]
    [SerializeField] private float positionCheckRadius = 0.2f;

    [Header("===== 디버그 =====")]
    [Tooltip("콘솔 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool showDebugLog = true;

    [Tooltip("씬에 순간이동 위치 디버그를 표시할지 여부입니다.")]
    [SerializeField] private bool showGizmos = true;

    [Header("===== 현재 상태 (디버그용) =====")]
    [Tooltip("현재 순간이동 패턴 실행 중인지 여부입니다.")]
    [SerializeField] private bool isPatternPlaying;

    [Tooltip("마지막으로 계산된 순간이동 목표 위치입니다.")]
    [SerializeField] private Vector3 lastTeleportPosition;

    private Color cachedColor;
    private Coroutine patternCoroutine;
    private float cooldownTimer;

    public bool IsPatternPlaying => isPatternPlaying;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (moveController == null)
            moveController = GetComponent<GrimReapermove>();

        if (basicAttack == null)
            basicAttack = GetComponent<GrimReaperBasicAttack>();

        if (spriteRenderer != null)
            cachedColor = spriteRenderer.color;
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        cooldownTimer = patternCooldown;
    }

    private void Update()
    {
        FindPlayerIfNeeded();

        if (playerTarget == null)
            return;

        if (isPatternPlaying)
            return;

        cooldownTimer += Time.deltaTime;

        if (cooldownTimer < patternCooldown)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (distance > usableDistance)
            return;

        if (distance < minDistanceToUse)
            return;

        if (Random.value > useChance)
            return;

        patternCoroutine = StartCoroutine(CoTeleportBehindAttack());
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerTarget = playerObject.transform;
    }

    private IEnumerator CoTeleportBehindAttack()
    {
        isPatternPlaying = true;
        cooldownTimer = 0f;

        if (showDebugLog)
            Debug.Log("[저승사자 순간이동 패턴] 시작");

        // 한글 주석: 이동 정지
        if (moveController != null)
            moveController.SetAttacking(true);

        // 한글 주석: 공격 스크립트도 자동으로 공격을 시도할 수 있으므로
        // 이동만 멈춘 상태에서 패턴을 진행합니다.

        // 한글 주석: 서서히 사라짐
        yield return StartCoroutine(FadeAlpha(1f, 0f, fadeOutDuration));

        // 한글 주석: 완전히 사라진 상태 유지
        if (hiddenDuration > 0f)
            yield return new WaitForSeconds(hiddenDuration);

        // 한글 주석: 플레이어 뒤쪽 위치 계산
        Vector3 targetPosition = CalculateTeleportPosition();
        lastTeleportPosition = targetPosition;

        // 한글 주석: 순간이동 위치로 이동
        transform.position = targetPosition;

        // 한글 주석: 등장 직후 플레이어 방향을 바라보게 보정
        FacePlayer();

        if (showDebugLog)
            Debug.Log($"[저승사자 순간이동 패턴] 순간이동 위치: {targetPosition}");

        // 한글 주석: 다시 나타남
        yield return StartCoroutine(FadeAlpha(0f, 1f, fadeInDuration));

        // 한글 주석: 등장 직후 기본 공격 스크립트가 자동으로 공격할 시간을 벌어줌
        if (waitBeforeAllowMove > 0f)
            yield return new WaitForSeconds(waitBeforeAllowMove);

        // 한글 주석: 약간의 후딜 후 이동 재개
        if (recoveryDelay > 0f)
            yield return new WaitForSeconds(recoveryDelay);

        if (moveController != null)
            moveController.SetAttacking(false);

        isPatternPlaying = false;
        patternCoroutine = null;

        if (showDebugLog)
            Debug.Log("[저승사자 순간이동 패턴] 종료");
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (spriteRenderer == null)
            yield break;

        if (duration <= 0f)
        {
            Color instantColor = spriteRenderer.color;
            instantColor.a = to;
            spriteRenderer.color = instantColor;
            yield break;
        }

        float elapsed = 0f;
        Color color = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            color.a = Mathf.Lerp(from, to, t);
            spriteRenderer.color = color;

            yield return null;
        }

        color.a = to;
        spriteRenderer.color = color;
    }

    private Vector3 CalculateTeleportPosition()
    {
        if (playerTarget == null)
            return transform.position;

        // 한글 주석: 플레이어 기준으로 저승사자가 있던 반대 방향 쪽에 나타나도록 계산
        Vector2 bossToPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        if (bossToPlayer.sqrMagnitude <= 0.001f)
            bossToPlayer = Vector2.right;

        Vector2 behindDirection = bossToPlayer.normalized;
        Vector2 basePosition = (Vector2)playerTarget.position + behindDirection * backOffset;
        basePosition += Vector2.up * yOffset;

        // 한글 주석: 플레이어와 너무 겹치지 않도록 추가 보정
        Vector2 safeDirection = behindDirection.normalized;
        Vector2 safePosition = basePosition + safeDirection * overlapSafetyOffset;

        Vector2 finalPosition = safePosition;

        // 한글 주석: 장애물과 겹치면 살짝 앞으로 당겨서 대체 위치 찾기
        if (Physics2D.OverlapCircle(finalPosition, positionCheckRadius, obstacleMask) != null)
        {
            for (int i = 1; i <= 6; i++)
            {
                Vector2 testPosition = safePosition - safeDirection * (0.25f * i);

                if (Physics2D.OverlapCircle(testPosition, positionCheckRadius, obstacleMask) == null)
                {
                    finalPosition = testPosition;
                    break;
                }
            }
        }

        return new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
    }

    private void FacePlayer()
    {
        if (playerTarget == null || spriteRenderer == null)
            return;

        float diffX = playerTarget.position.x - transform.position.x;

        if (Mathf.Abs(diffX) < 0.01f)
            return;

        spriteRenderer.flipX = diffX < 0f;
    }

    public void ForceUsePattern()
    {
        if (isPatternPlaying)
            return;

        if (patternCoroutine != null)
            return;

        FindPlayerIfNeeded();

        if (playerTarget == null)
            return;

        patternCoroutine = StartCoroutine(CoTeleportBehindAttack());
    }

    private void OnDisable()
    {
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            Color resetColor = spriteRenderer.color;
            resetColor.a = 1f;
            spriteRenderer.color = resetColor;
        }

        if (moveController != null)
            moveController.SetAttacking(false);

        isPatternPlaying = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, usableDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minDistanceToUse);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastTeleportPosition, 0.2f);
    }
}