using UnityEngine;

/// <summary>
/// 플레이어를 향해 단순 추적 이동하는 컴포넌트입니다.
///
/// 이번 단계에서 바꾸는 이유:
/// - 기존에는 moveSpeed가 프리팹 고정값에 가까웠고,
///   detectRange 개념도 없어서 MonsterDefinitionSO와 바로 연결하기 어려웠습니다.
/// - 이제는 런타임에 moveSpeed와 detectRange를 주입받아
///   "detectRange 안에서만 추적"하도록 맞춥니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyChaser2D : MonoBehaviour
{
    [Header("1. 이동 기준")]
    [SerializeField, Tooltip("추적 이동 속도입니다.\n"
                             + "기본값은 Inspector 확인용이며,\n"
                             + "실제 게임에서는 MonsterRuntimeApplier2D가\n"
                             + "SO 값으로 덮어쓸 수 있습니다.\n"
                             + "단위는 world unit / second 기준입니다.")]
    private float moveSpeed = 3f;

    [SerializeField, Tooltip("플레이어를 인식하고 추적을 시작할 거리입니다.\n"
                             + "단위는 world unit입니다.\n"
                             + "이 거리 밖이면 정지합니다.")]
    private float detectRange = 8f;

    [Header("2. 대상 연결")]
    [SerializeField, Tooltip("현재 추적 대상입니다.\n"
                             + "비워두면 playerTag 기준으로 자동 탐색합니다.")]
    private Transform target;

    [SerializeField, Tooltip("이동에 사용할 Rigidbody2D입니다.\n"
                             + "보통 같은 루트 오브젝트의 Rigidbody2D를 연결합니다.")]
    private Rigidbody2D rb;

    [SerializeField, Tooltip("자동 탐색할 플레이어 태그입니다.\n"
                             + "보통 Player를 사용합니다.")]
    private string playerTag = "Player";

    private static Transform cachedPlayer;
    private static float lastSearchTime;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (target == null && cachedPlayer != null)
            target = cachedPlayer;
    }

    /// <summary>
    /// 런타임에 추적 수치를 주입합니다.
    /// </summary>
    public void ConfigureRuntime(float runtimeMoveSpeed, float runtimeDetectRange)
    {
        moveSpeed = Mathf.Max(0f, runtimeMoveSpeed);
        detectRange = Mathf.Max(0f, runtimeDetectRange);
    }

    /// <summary>
    /// 외부에서 타겟을 직접 지정할 때 사용합니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
            cachedPlayer = target;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!TryResolveTarget())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (detectRange <= 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float sqrDistance = toTarget.sqrMagnitude;
        float sqrDetectRange = detectRange * detectRange;

        if (sqrDistance > sqrDetectRange)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (sqrDistance < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = toTarget.normalized * moveSpeed;
    }

    private bool TryResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return true;

        if (Time.time - lastSearchTime <= 0.5f)
            return false;

        lastSearchTime = Time.time;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
            return false;

        cachedPlayer = player.transform;
        target = cachedPlayer;
        return true;
    }
}