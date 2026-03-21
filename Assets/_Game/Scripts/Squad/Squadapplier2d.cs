using System.Collections;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 스쿼드 편성을 런타임 시스템에 적용하는 허브입니다.
/// 메인 캐릭터의 비주얼/궁극기/활성 속성만 적용합니다.
/// 캐릭터 기본 무기(StartingSkill)는 CommonSkillManager2D가 아니라
/// 기존 발사/무기 시스템에서 처리하도록 분리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SquadApplier2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터입니다.")]
    [SerializeField] private SquadLoadout2D loadout;

    [Tooltip("궁극기 컨트롤러입니다.")]
    [SerializeField] private UltimateController2D ultimateController;

    [Header("비주얼 교체 (Player)")]
    [Tooltip("Player의 SpriteRenderer입니다. 메인 캐릭터에 따라 Idle 스프라이트를 교체합니다.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Tooltip("Player의 Animator입니다. 메인 캐릭터에 따라 Animator Controller를 교체합니다.")]
    [SerializeField] private Animator playerAnimator;

    [Header("디버그")]
    [Tooltip("체크 시 편성 적용 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    private bool _appliedThisRun;

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<SquadLoadout2D>();

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
        // StageStarted보다 Start가 늦게 올 수 있으므로 한 번 더 시도
        StartCoroutine(ApplyRoutine("Start"));
    }

    private void OnDisable()
    {
        RunSignals.StageStarted -= OnStageStarted;

        if (loadout != null)
            loadout.OnLoadoutChanged -= OnLoadoutChanged;
    }

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
        if (_appliedThisRun)
            yield break;

        // 다른 초기화가 먼저 끝나도록 1프레임 대기
        yield return null;

        if (_appliedThisRun)
            yield break;

        _appliedThisRun = true;

        if (loadout == null)
        {
            Debug.LogWarning("[스쿼드 적용] SquadLoadout2D가 연결되지 않았습니다.", this);
            yield break;
        }

        ApplyAll(source);
    }

    private void ApplyAll(string source)
    {
        CharacterDefinitionSO main = loadout.Main;

        if (main == null)
        {
            Debug.LogWarning("[스쿼드 적용] 메인 캐릭터가 설정되지 않았습니다.", this);
            return;
        }

        // 1. 메인 캐릭터 비주얼 적용
        ApplyVisual(main);

        // 2. 메인 캐릭터 궁극기 적용
        ApplyUltimate(main);

        // 3. 활성 속성 로그
        LogActiveAttributes();

        if (debugLog)
        {
            string support1Name = loadout.Support1 != null ? loadout.Support1.DisplayName : "(없음)";
            string support2Name = loadout.Support2 != null ? loadout.Support2.DisplayName : "(없음)";
            string startingWeaponName = main.StartingSkill != null ? main.StartingSkill.name : "없음";
            string ultimateName = main.UltimateData != null ? main.UltimateData.DisplayName : "없음";

            Debug.Log(
                $"[스쿼드 적용] 완료 (from={source}) | " +
                $"메인={main.DisplayName} | " +
                $"기본무기={startingWeaponName} | " +
                $"궁극기={ultimateName} | " +
                $"속성={main.Attribute.ToKorean()} | " +
                $"지원1={support1Name} | 지원2={support2Name}",
                this
            );
        }
    }

    /// <summary>
    /// 메인 캐릭터의 비주얼을 Player에 반영합니다.
    /// </summary>
    private void ApplyVisual(CharacterDefinitionSO main)
    {
        if (main == null)
            return;

        // Animator Controller 교체
        if (playerAnimator != null && main.AnimatorController != null)
        {
            playerAnimator.runtimeAnimatorController = main.AnimatorController;

            if (debugLog)
                Debug.Log($"[스쿼드 적용] Animator Controller 교체: {main.AnimatorController.name}", this);
        }

        // Idle 스프라이트 교체
        if (playerSpriteRenderer != null && main.PlayerIdleSprite != null)
        {
            playerSpriteRenderer.sprite = main.PlayerIdleSprite;

            if (debugLog)
                Debug.Log($"[스쿼드 적용] Idle 스프라이트 교체: {main.PlayerIdleSprite.name}", this);
        }
    }

    /// <summary>
    /// 메인 캐릭터의 궁극기를 UltimateController에 반영합니다.
    /// </summary>
    private void ApplyUltimate(CharacterDefinitionSO main)
    {
        if (ultimateController == null)
        {
            Debug.LogWarning("[스쿼드 적용] UltimateController2D가 연결되지 않았습니다.", this);
            return;
        }

        ultimateController.SetCharacter(main);

        if (debugLog)
        {
            string ultimateName = main.UltimateData != null ? main.UltimateData.DisplayName : "없음";
            Debug.Log($"[스쿼드 적용] 궁극기 설정: {ultimateName}", this);
        }
    }

    /// <summary>
    /// 현재 편성의 활성 속성을 로그로 출력합니다.
    /// </summary>
    private void LogActiveAttributes()
    {
        if (!debugLog || loadout == null)
            return;

        CharacterAttributeKind[] attributes = loadout.GetActiveAttributes();
        if (attributes == null || attributes.Length == 0)
            return;

        string text = string.Empty;

        for (int i = 0; i < attributes.Length; i++)
        {
            if (i > 0)
                text += ", ";

            text += attributes[i].ToKorean();
        }

        Debug.Log($"[스쿼드 적용] 활성 속성: {text}", this);
    }
}