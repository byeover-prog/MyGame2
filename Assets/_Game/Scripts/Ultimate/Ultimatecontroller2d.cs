// R키 궁극기 입력 + 쿨다운 관리 + ULT 모션 유지/복귀.
// Player 루트에 부착.
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
    [SerializeField, Tooltip("공용 궁극기 실행기. Player에 붙은 UltimateExecutor2D.")]
    private UltimateExecutor2D executor;

    [Header("애니메이션")]
    [SerializeField, Tooltip("Animator 참조. 없으면 자동 탐색.")]
    private Animator animator;

    [Tooltip("Animator의 ULT 상태 이름입니다. CrossFade로 강제 유지합니다.")]
    [SerializeField] private string ultStateName = "ULT";

    [Tooltip("Animator의 Idle 상태 이름입니다. 궁극기 종료 후 복귀합니다.")]
    [SerializeField] private string idleStateName = "Idle";

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
            executor = GetComponentInChildren<UltimateExecutor2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _cooldownTimer = cooldownSeconds;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F9))
        {
            _cooldownTimer = 0f;
            Debug.Log("[궁극기] F9 키 — 쿨다운 초기화");
        }

        if (Input.GetKeyDown(ultimateKey))
            TryActivate();
    }

    /// <summary>
    /// 현재 메인 캐릭터의 궁극기를 설정한다.
    /// 편성 시스템에서 메인 캐릭터가 바뀔 때 호출.
    /// </summary>
    public void SetCharacter(CharacterDefinitionSO charDef)
    {
        if (executor != null)
            executor.SetCharacter(charDef);
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

        if (!executor.IsExecuting && executor.CurrentCharacterId == null)
        {
            Debug.LogWarning("[궁극기] 캐릭터가 설정되지 않았습니다! SetCharacter()를 먼저 호출하세요.");
            return;
        }

        // ★ ULT 모션 강제 시작 — CrossFade로 FSM 전환 무시하고 유지
        ForceUltAnimation();

        _isExecuting = true;
        string charId = executor.CurrentCharacterId ?? "unknown";
        Debug.Log($"[궁극기] R키 발동 — {charId}");
        executor.Execute(OnUltimateFinished);
    }

    private void OnUltimateFinished()
    {
        _isExecuting = false;
        _cooldownTimer = cooldownSeconds;

        // ★ 궁극기 끝 → Idle 복귀
        ForceIdleAnimation();

        Debug.Log($"[궁극기] 종료 — 쿨다운 {cooldownSeconds}초 시작");
    }

    // ═══════════════════════════════════════════════════════
    //  Animator 제어
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// ULT 애니메이션을 강제로 재생하고 유지합니다.
    /// CrossFade를 사용하여 FSM의 Has Exit Time/전환 조건에 관계없이
    /// 코드가 명시적으로 ULT 상태를 잡아둡니다.
    /// </summary>
    private void ForceUltAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Trigger_Ult");
        animator.ResetTrigger("Trigger_Land");

        // Trigger도 세팅 (FSM 호환)
        animator.SetTrigger("Trigger_Ult");

        // ★ CrossFade로 ULT 상태 강제 진입 — 1프레임 문제 해결
        animator.CrossFade(ultStateName, 0.05f, 0);

        Debug.Log("[궁극기] ULT 모션 강제 시작");
    }

    /// <summary>
    /// Idle 애니메이션으로 강제 복귀합니다.
    /// </summary>
    private void ForceIdleAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Trigger_Ult");
        animator.ResetTrigger("Trigger_Land");

        animator.CrossFade(idleStateName, 0.1f, 0);

        Debug.Log("[궁극기] Idle 모션 복귀");
    }

    [ContextMenu("디버그: 쿨다운 리셋")]
    public void DebugResetCooldown()
    {
        _cooldownTimer = 0f;
        Debug.Log("[궁극기] 디버그 — 쿨다운 리셋");
    }
}