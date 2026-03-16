// UTF-8
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DuryeoksiniEarthQuakeAttack : MonoBehaviour
{
    /// <summary>
    /// [구현 원리 요약]
    /// 두억시니가 기를 모은 뒤 바닥을 내려찍으면
    /// 맵 전체 흔들림을 발생시키고 플레이어 위치와 상관없이 데미지를 줍니다.
    /// 범위 판정이 아닌 전역 공격 패턴입니다.
    /// </summary>

    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform\n비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("두억시니 Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("맵 흔들림용 카메라 Transform\n비워두면 Camera.main을 자동 사용합니다.")]
    [SerializeField] private Transform cameraTarget;

    [Header("===== 스킬 발동 설정 =====")]
    [Tooltip("스킬 재사용 대기시간입니다.")]
    [SerializeField] private float skillCooldown = 8f;

    [Tooltip("플레이어가 이 거리 안에 들어오면 스킬 사용을 시도합니다.")]
    [SerializeField] private float useDistance = 999f;

    [Tooltip("기를 모으는 시간입니다.")]
    [SerializeField] private float chargeTime = 1.2f;

    [Tooltip("내려찍기 후 실제 데미지가 들어가기까지의 시간입니다.")]
    [SerializeField] private float hitDelayAfterSlam = 0.15f;

    [Tooltip("공격 종료 후 후딜레이 시간입니다.")]
    [SerializeField] private float recoveryTime = 1.0f;

    [Header("===== 데미지 설정 =====")]
    [Tooltip("전맵 충격파 데미지입니다.")]
    [SerializeField] private int skillDamage = 25;

    [Tooltip("true면 스킬 발동 시 플레이어에게 무조건 데미지를 줍니다.")]
    [SerializeField] private bool ignoreDistanceAndHitWholeMap = true;

    [Header("===== 카메라 흔들림 =====")]
    [Tooltip("카메라 흔들림 지속 시간입니다.")]
    [SerializeField] private float shakeDuration = 0.35f;

    [Tooltip("카메라 흔들림 강도입니다.")]
    [SerializeField] private float shakeMagnitude = 0.25f;

    [Tooltip("흔들림 초당 횟수입니다.")]
    [SerializeField] private float shakeFrequency = 35f;

    [Header("===== 애니메이터 파라미터 =====")]
    [Tooltip("기를 모으기용 트리거 이름입니다.")]
    [SerializeField] private string chargeTriggerName = "EarthQuakeCharge";

    [Tooltip("내려찍기용 트리거 이름입니다.")]
    [SerializeField] private string slamTriggerName = "EarthQuakeSlam";

    [Header("===== 디버그 =====")]
    [Tooltip("true면 콘솔 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLog = true;

    private bool isUsingSkill;
    private float lastUseTime = -999f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.transform;
        }

        if (cameraTarget == null && Camera.main != null)
            cameraTarget = Camera.main.transform;
    }

    private void Update()
    {
        if (isUsingSkill)
            return;

        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.transform;
            else
                return;
        }

        if (Time.time < lastUseTime + skillCooldown)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (distance <= useDistance)
        {
            StartCoroutine(CoEarthQuakeAttack());
        }
    }

    private IEnumerator CoEarthQuakeAttack()
    {
        isUsingSkill = true;
        lastUseTime = Time.time;

        if (showDebugLog)
            Debug.Log("[두억시니] 대지 분쇄 준비 시작");

        // 1. 기 모으기
        if (animator != null && string.IsNullOrEmpty(chargeTriggerName) == false)
            animator.SetTrigger(chargeTriggerName);

        yield return new WaitForSeconds(chargeTime);

        // 2. 내려찍기
        if (animator != null && string.IsNullOrEmpty(slamTriggerName) == false)
            animator.SetTrigger(slamTriggerName);

        yield return new WaitForSeconds(hitDelayAfterSlam);

        // 3. 맵 전체 흔들림
        if (cameraTarget != null)
            StartCoroutine(CoShakeCamera());

        // 4. 플레이어 전역 데미지
        ApplyGlobalDamageToPlayer();

        // 5. 후딜레이
        yield return new WaitForSeconds(recoveryTime);

        if (showDebugLog)
            Debug.Log("[두억시니] 대지 분쇄 종료");

        isUsingSkill = false;
    }

    private void ApplyGlobalDamageToPlayer()
    {
        if (playerTarget == null)
            return;

        if (ignoreDistanceAndHitWholeMap == false)
            return;

        PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            if (showDebugLog)
                Debug.LogWarning("[두억시니] PlayerHealth를 찾지 못했습니다.");
            return;
        }

        playerHealth.TakeDamage(skillDamage);

        if (showDebugLog)
            Debug.Log("[두억시니] 전맵 충격파 데미지 적용: " + skillDamage);
    }

    private IEnumerator CoShakeCamera()
    {
        Vector3 originalPosition = cameraTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float offsetX = Mathf.Sin(Time.time * shakeFrequency) * shakeMagnitude;
            float offsetY = Mathf.Cos(Time.time * shakeFrequency * 1.2f) * shakeMagnitude;

            cameraTarget.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        cameraTarget.localPosition = originalPosition;
    }

    public bool IsUsingSkill()
    {
        return isUsingSkill;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, useDistance);
    }
}