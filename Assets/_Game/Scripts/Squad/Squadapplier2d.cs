using System.Collections;
using UnityEngine;

/// <summary>
/// 스쿼드 편성을 런타임 시스템에 적용하는 허브.
/// 게임 시작 시(StageStarted) 또는 편성 변경 시 자동 실행.
///
/// [적용하는 것들]
/// 1. 메인 캐릭터의 기본 스킬 → CommonSkillManager2D.Upgrade()
/// 2. 메인 캐릭터의 궁극기 → UltimateController2D.SetCharacter()
/// 3. 활성 속성 목록 로깅 (속성 시스템 연동 준비)
///
/// [주의]
/// 기존 CommonSkillStartBinder2D는 비활성화하거나 startSkills를 비워야 함.
/// 이 컴포넌트가 기본 스킬 할당을 대체함.
///
/// [Hierarchy]
/// Player 오브젝트에 부착.
/// </summary>
[DisallowMultipleComponent]
public sealed class SquadApplier2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터")]
    [SerializeField] private SquadLoadout2D loadout;

    [Tooltip("공통 스킬 매니저 (기본 스킬 적용용)")]
    [SerializeField] private CommonSkillManager2D skillManager;

    [Tooltip("궁극기 컨트롤러 (메인 궁극기 설정용)")]
    [SerializeField] private UltimateController2D ultimateController;

    [Header("비주얼 교체 (Player)")]
    [Tooltip("Player의 SpriteRenderer. 메인 캐릭터에 따라 Idle 스프라이트 교체.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Tooltip("Player의 Animator. 메인 캐릭터에 따라 Controller 교체.")]
    [SerializeField] private Animator playerAnimator;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private bool _appliedThisRun;

    // ═══════════════════════════════════════════════════════
    //  이벤트 구독
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<SquadLoadout2D>();
        if (skillManager == null)
            skillManager = GetComponentInChildren<CommonSkillManager2D>();
        if (ultimateController == null)
            ultimateController = GetComponent<UltimateController2D>();
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        RunSignals.StageStarted += OnStageStarted;

        if (loadout != null)
            loadout.OnLoadoutChanged += OnLoadoutChanged;
    }

    private void Start()
    {
        // StageStarted가 OnEnable보다 먼저 발생했을 수 있으므로 Start에서도 시도
        StartCoroutine(ApplyRoutine("Start"));
    }

    private void OnDisable()
    {
        RunSignals.StageStarted -= OnStageStarted;

        if (loadout != null)
            loadout.OnLoadoutChanged -= OnLoadoutChanged;
    }

    // ═══════════════════════════════════════════════════════
    //  트리거
    // ═══════════════════════════════════════════════════════

    private void OnStageStarted()
    {
        _appliedThisRun = false;
        StartCoroutine(ApplyRoutine("StageStarted"));
    }

    private void OnLoadoutChanged()
    {
        _appliedThisRun = false;
        StartCoroutine(ApplyRoutine("LoadoutChanged"));
    }

    private IEnumerator ApplyRoutine(string source)
    {
        if (_appliedThisRun) yield break;

        // 1프레임 대기 (다른 시스템 초기화 완료 보장)
        yield return null;

        if (_appliedThisRun) yield break;
        _appliedThisRun = true;

        if (loadout == null)
        {
            Debug.LogWarning("[스쿼드 적용] SquadLoadout2D가 연결되지 않았습니다!", this);
            yield break;
        }

        ApplyAll(source);
    }

    // ═══════════════════════════════════════════════════════
    //  적용 로직
    // ═══════════════════════════════════════════════════════

    private void ApplyAll(string source)
    {
        CharacterDefinitionSO main = loadout.Main;

        if (main == null)
        {
            Debug.LogWarning("[스쿼드 적용] 메인 캐릭터가 설정되지 않았습니다!", this);
            return;
        }

        // ── 1. 메인 캐릭터 비주얼 교체 ──
        ApplyVisual(main);

        // ── 2. 기본 스킬 적용 ──
        ApplyStartingSkill(main);

        // ── 3. 궁극기 설정 ──
        ApplyUltimate(main);

        // ── 4. 활성 속성 로깅 (속성 시스템 연동 시 확장) ──
        LogActiveAttributes();

        if (debugLog)
        {
            string s1 = loadout.Support1 != null ? loadout.Support1.DisplayName : "(없음)";
            string s2 = loadout.Support2 != null ? loadout.Support2.DisplayName : "(없음)";

            Debug.Log($"[스쿼드 적용] 완료 (from={source}) | " +
                      $"메인={main.DisplayName} " +
                      $"스킬={main.StartingSkill?.name ?? "없음"} " +
                      $"궁극기={main.UltimateData?.DisplayName ?? "없음"} " +
                      $"속성={main.Attribute.ToKorean()} | " +
                      $"지원1={s1} 지원2={s2}", this);
        }
    }

    /// <summary>
    /// 메인 캐릭터에 따라 Player의 스프라이트와 Animator Controller를 교체.
    /// </summary>
    private void ApplyVisual(CharacterDefinitionSO main)
    {
        // Animator Controller 교체
        if (playerAnimator != null && main.AnimatorController != null)
        {
            playerAnimator.runtimeAnimatorController = main.AnimatorController;

            if (debugLog)
                Debug.Log($"[스쿼드 적용] Animator Controller 교체: {main.AnimatorController.name}", this);
        }

        // Idle 스프라이트 교체 (Animator가 없거나 전환 전 기본 표시)
        if (playerSpriteRenderer != null && main.PlayerIdleSprite != null)
        {
            playerSpriteRenderer.sprite = main.PlayerIdleSprite;

            if (debugLog)
                Debug.Log($"[스쿼드 적용] 스프라이트 교체: {main.PlayerIdleSprite.name}", this);
        }
    }

    /// <summary>
    /// 메인 캐릭터의 시작 스킬을 CommonSkillManager에 적용.
    /// 기존 CommonSkillStartBinder2D의 역할을 대체.
    /// </summary>
    private void ApplyStartingSkill(CharacterDefinitionSO main)
    {
        if (skillManager == null)
        {
            Debug.LogWarning("[스쿼드 적용] CommonSkillManager2D가 연결되지 않았습니다!", this);
            return;
        }

        CommonSkillConfigSO startSkill = main.StartingSkill;
        if (startSkill == null)
        {
            Debug.LogWarning($"[스쿼드 적용] {main.DisplayName}의 Starting Skill이 비어있습니다!", this);
            return;
        }

        skillManager.Upgrade(startSkill);

        if (debugLog)
            Debug.Log($"[스쿼드 적용] 기본 스킬 적용: {startSkill.name}", this);
    }

    /// <summary>
    /// 메인 캐릭터의 궁극기를 UltimateController에 설정.
    /// </summary>
    private void ApplyUltimate(CharacterDefinitionSO main)
    {
        if (ultimateController == null)
        {
            Debug.LogWarning("[스쿼드 적용] UltimateController2D가 연결되지 않았습니다!", this);
            return;
        }

        ultimateController.SetCharacter(main);

        if (debugLog)
            Debug.Log($"[스쿼드 적용] 궁극기 설정: {main.UltimateData?.DisplayName ?? "없음"}", this);
    }

    /// <summary>
    /// 현재 편성의 활성 속성을 로깅.
    /// 나중에 속성 시스템(ElementManager2D)에 전달하는 코드로 확장.
    /// </summary>
    private void LogActiveAttributes()
    {
        if (!debugLog) return;

        var attrs = loadout.GetActiveAttributes();
        if (attrs.Length == 0) return;

        string attrStr = "";
        for (int i = 0; i < attrs.Length; i++)
        {
            if (i > 0) attrStr += ", ";
            attrStr += attrs[i].ToKorean();
        }

        Debug.Log($"[스쿼드 적용] 활성 속성: {attrStr}", this);
    }
}