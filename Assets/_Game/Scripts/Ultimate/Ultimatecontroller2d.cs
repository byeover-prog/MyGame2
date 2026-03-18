// UTF-8
// Assets/_Game/Scripts/Ultimate/UltimateController2D.cs
// R키 궁극기 입력 + 쿨다운 관리. Player 루트에 부착.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UltimateController2D : MonoBehaviour
{
    [Header("쿨다운")]
    [SerializeField, Tooltip("궁극기 재사용 대기시간(초). 기획서=240초(4분)")]
    private float cooldownSeconds = 240f;

    [Header("입력")]
    [SerializeField, Tooltip("궁극기 발동 키")]
    private KeyCode ultimateKey = KeyCode.R;

    [Header("참조")]
    [SerializeField, Tooltip("하율 궁극기 실행기. 같은 오브젝트 또는 자식.")]
    private HayulUltimateExecutor2D executor;

    [Header("애니메이션")]
    [SerializeField, Tooltip("Animator 참조. 없으면 자동 탐색.")]
    private Animator animator;

    // 런타임
    private float _cooldownTimer;
    private bool _isExecuting;

    /// <summary>외부에서 쿨다운 잔여 시간 읽기 (UI용)</summary>
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
    public float CooldownTotal => cooldownSeconds;
    public bool IsReady => _cooldownTimer <= 0f && !_isExecuting;

    private void Awake()
    {
        if (executor == null)
            executor = GetComponentInChildren<HayulUltimateExecutor2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // 게임 시작 시부터 쿨다운 진행 (바로 사용 불가)
        _cooldownTimer = cooldownSeconds;
    }

    private void Update()
    {
        // 쿨다운 감소
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        // 디버그: F9 키 쿨타임 즉시 초기화
        if (Input.GetKeyDown(KeyCode.F9))
        {
            _cooldownTimer = 0f;
            Debug.Log("[궁극기] F9 키 — 쿨다운 초기화");
        }

        // 입력
        if (Input.GetKeyDown(ultimateKey))
        {
            TryActivate();
        }
    }

    private void TryActivate()
    {
        if (_isExecuting)
        {
            Debug.Log("[궁극기] 이미 시전 중");
            return;
        }

        if (_cooldownTimer > 0f)
        {
            Debug.Log($"[궁극기] 쿨다운 중 — 남은 시간:{_cooldownTimer:F1}초");
            return;
        }

        if (executor == null)
        {
            Debug.LogError("[궁극기] Executor가 연결되지 않았습니다!", this);
            return;
        }

        // 애니메이션 트리거
        if (animator != null)
            animator.SetTrigger("ULT");

        // 실행
        _isExecuting = true;
        Debug.Log("[궁극기] R키 발동 — 하율 천강뇌전부");
        executor.Execute(OnUltimateFinished);
    }

    private void OnUltimateFinished()
    {
        _isExecuting = false;
        _cooldownTimer = cooldownSeconds;
        Debug.Log($"[궁극기] 종료 — 쿨다운 {cooldownSeconds}초 시작");
    }

    /// <summary>디버그: 쿨다운 즉시 초기화</summary>
    [ContextMenu("디버그: 쿨다운 리셋")]
    public void DebugResetCooldown()
    {
        _cooldownTimer = 0f;
        Debug.Log("[궁극기] 디버그 — 쿨다운 리셋");
    }
}