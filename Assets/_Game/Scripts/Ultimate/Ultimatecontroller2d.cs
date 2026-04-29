// R키 궁극기 입력 + 쿨다운 관리 + ULT 모션 유지/복귀.
// Player 루트에 부착.
//
// [v2 패치 — 동시 입력 deadlock 방지 + 양방향 가드]
//  - IsReady에 executor.IsExecuting + supportController.IsBusy 양방향 체크
//  - Execute() bool 반환을 받아 실패 시 _isExecuting/애니메이션을 건드리지 않음
//  - 자기 _isExecuting을 외부에서 읽을 수 있도록 IsBusy 프로퍼티 추가

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

    [Tooltip("지원 궁극기 컨트롤러. 양방향 가드용. 비워두면 자동 탐색.")]
    [SerializeField] private SupportUltimateController2D supportController;

    [Header("애니메이션")]
    [SerializeField, Tooltip("Animator 참조. 없으면 자동 탐색.")]
    private Animator animator;

    [Tooltip("Animator의 ULT 상태 이름입니다. CrossFade로 강제 유지합니다.")]
    [SerializeField] private string ultStateName = "ULT";

    [Tooltip("Animator의 Idle 상태 이름입니다. 궁극기 종료 후 복귀합니다.")]
    [SerializeField] private string idleStateName = "Idle";

    [Header("디버그")]
    [SerializeField, Tooltip("R키 궁극기 상세 로그를 콘솔에 출력합니다.")]
    private bool debugLog = true;

    // 런타임
    private float _cooldownTimer;
    private bool _isExecuting;

    /// <summary>외부에서 쿨다운 잔여 시간 읽기 (UI용)</summary>
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
    public float CooldownTotal => cooldownSeconds;

    /// <summary>★ v2 추가: 자기 시전 상태 외부 노출 (양방향 가드용)</summary>
    public bool IsBusy => _isExecuting;

    /// <summary>
    /// R키 입력 가능 여부.
    /// ★ v2 패치: executor.IsExecuting + 지원궁 시퀀스 IsBusy 모두 체크.
    /// </summary>
    public bool IsReady =>
        _cooldownTimer <= 0f
        && !_isExecuting
        && (executor == null || !executor.IsExecuting)
        && (supportController == null || !supportController.IsBusy);

    private void Awake()
    {
        if (executor == null)
            executor = GetComponentInChildren<UltimateExecutor2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        // ★ v2: 같은 GameObject에서 SupportUltimateController2D 자동 검색
        if (supportController == null)
            supportController = GetComponent<SupportUltimateController2D>();
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
            GameLogger.Log("[궁극기] F9 키 — 쿨다운 초기화");
        }

        if (Input.GetKeyDown(ultimateKey))
            TryActivate();
    }

    /// <summary>
    /// 현재 메인 캐릭터의 궁극기를 설정한다.
    /// 편성 시스템(SquadApplier2D)에서 메인 캐릭터가 바뀔 때 호출.
    /// ★ v2: executor.SetCharacter의 bool 반환을 받아 실패 시 로그.
    /// </summary>
    public void SetCharacter(CharacterDefinitionSO charDef)
    {
        if (executor == null) return;

        bool ok = executor.SetCharacter(charDef);
        if (!ok && debugLog)
            GameLogger.LogWarning($"[궁극기] SetCharacter 실패 — 실행 중이거나 데이터 없음 ({(charDef != null ? charDef.DisplayName : "null")})");
    }

    private void TryActivate()
    {
        if (_isExecuting)
        {
            if (debugLog)
                GameLogger.Log("[궁극기] 거부 — 이미 시전 중");
            return;
        }

        if (_cooldownTimer > 0f)
        {
            if (debugLog)
                GameLogger.Log($"[궁극기] 거부 — 쿨다운 중 (남은 {_cooldownTimer:F1}초)");
            return;
        }

        if (executor == null)
        {
            Debug.LogError("[궁극기] Executor가 연결되지 않았습니다!", this);
            return;
        }

        // ★ v2: 지원궁 시퀀스 진행 중이면 거부 (등장/퇴장 연출 중에도 막힘)
        if (supportController != null && supportController.IsBusy)
        {
            if (debugLog)
                GameLogger.LogWarning("[궁극기] 거부 — 지원궁 시퀀스 진행 중");
            return;
        }

        // executor 점유 중이면 거부 (드물게 다른 호출자가 있을 경우)
        if (executor.IsExecuting)
        {
            if (debugLog)
                GameLogger.LogWarning("[궁극기] 거부 — Executor가 이미 사용 중");
            return;
        }

        if (executor.CurrentCharacterId == null)
        {
            GameLogger.LogWarning("[궁극기] 캐릭터가 설정되지 않았습니다! SetCharacter()를 먼저 호출하세요.");
            return;
        }

        // ★ v2: Execute 호출 → 성공 여부 확인 후 상태/애니메이션 변경
        bool started = executor.Execute(OnUltimateFinished);
        if (!started)
        {
            if (debugLog)
                GameLogger.LogWarning("[궁극기] 거부 — Executor.Execute가 false를 반환");
            return;
        }

        // ★ 성공 확정 후에만 _isExecuting과 애니메이션 변경
        ForceUltAnimation();
        _isExecuting = true;

        string charId = executor.CurrentCharacterId ?? "unknown";
        if (debugLog)
            GameLogger.Log($"[궁극기] R키 발동 — {charId}");
    }

    private void OnUltimateFinished()
    {
        _isExecuting = false;
        _cooldownTimer = cooldownSeconds;

        // 궁극기 끝 → Idle 복귀
        ForceIdleAnimation();

        if (debugLog)
            GameLogger.Log($"[궁극기] 종료 — 쿨다운 {cooldownSeconds}초 시작");
    }

    // ════════════════════════════════════════════════════
    //  Animator 제어 (변경 없음)
    // ════════════════════════════════════════════════════

    private void ForceUltAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Trigger_Ult");
        animator.ResetTrigger("Trigger_Land");
        animator.SetTrigger("Trigger_Ult");
        animator.CrossFade(ultStateName, 0.05f, 0);

        if (debugLog)
            GameLogger.Log("[궁극기] ULT 모션 강제 시작");
    }

    private void ForceIdleAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Trigger_Ult");
        animator.ResetTrigger("Trigger_Land");
        animator.CrossFade(idleStateName, 0.1f, 0);

        if (debugLog)
            GameLogger.Log("[궁극기] Idle 모션 복귀");
    }

    [ContextMenu("디버그: 쿨다운 리셋")]
    public void DebugResetCooldown()
    {
        _cooldownTimer = 0f;
        GameLogger.Log("[궁극기] 디버그 — 쿨다운 리셋");
    }
}