// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 구미호 보스의 패턴 전체 흐름을 상위에서 관리한다.
// 기본 공격, 요화, 여우구슬 패턴의 사용 여부를 상위에서 켜고 끈다.

[DisallowMultipleComponent]
public sealed class GumihoBossPatternCoordinator : MonoBehaviour
{
    [Header("패턴 참조")]

    [Tooltip("구미호 기본 공격 컨트롤러입니다.")]
    [SerializeField] private GumihoBasicAttackController basicAttackController;

    [Tooltip("구미호 요화 패턴 컨트롤러입니다.")]
    [SerializeField] private GumihoYoHwaPatternController yoHwaPatternController;

    [Tooltip("구미호 여우구슬 패턴 컨트롤러입니다.")]
    [SerializeField] private GumihoYohoFoxBeadPatternController yohoFoxBeadPatternController;


    [Header("패턴 사용 여부")]

    [Tooltip("기본 공격 패턴을 사용할지 여부입니다.")]
    [SerializeField] private bool useBasicAttack = true;

    [Tooltip("요화 패턴을 사용할지 여부입니다.")]
    [SerializeField] private bool useYoHwaPattern = true;

    [Tooltip("여우구슬 패턴을 사용할지 여부입니다.")]
    [SerializeField] private bool useYohoFoxBeadPattern = true;


    [Header("전투 상태")]

    [Tooltip("보스 전투 시작 여부입니다.\n끄면 패턴 전체가 멈춥니다.")]
    [SerializeField] private bool isBattleActive = true;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private void Reset()
    {
        basicAttackController = GetComponent<GumihoBasicAttackController>();
        yoHwaPatternController = GetComponent<GumihoYoHwaPatternController>();
        yohoFoxBeadPatternController = GetComponent<GumihoYohoFoxBeadPatternController>();
    }

    private void Awake()
    {
        if (basicAttackController == null)
        {
            basicAttackController = GetComponent<GumihoBasicAttackController>();
        }

        if (yoHwaPatternController == null)
        {
            yoHwaPatternController = GetComponent<GumihoYoHwaPatternController>();
        }

        if (yohoFoxBeadPatternController == null)
        {
            yohoFoxBeadPatternController = GetComponent<GumihoYohoFoxBeadPatternController>();
        }
    }

    private void OnEnable()
    {
        ApplyPatternActivation();
    }

    private void Start()
    {
        ApplyPatternActivation();
    }

    public void SetBattleActive(bool value)
    {
        isBattleActive = value;
        ApplyPatternActivation();

        if (debugLog)
        {
            Debug.Log($"[GumihoBossPatternCoordinator] 전투 활성 상태 변경: {isBattleActive}", this);
        }
    }

    public void SetUseBasicAttack(bool value)
    {
        useBasicAttack = value;
        ApplyPatternActivation();
    }

    public void SetUseYoHwaPattern(bool value)
    {
        useYoHwaPattern = value;
        ApplyPatternActivation();
    }

    public void SetUseYohoFoxBeadPattern(bool value)
    {
        useYohoFoxBeadPattern = value;
        ApplyPatternActivation();
    }

    private void ApplyPatternActivation()
    {
        bool allowBasicAttack = isBattleActive && useBasicAttack;
        bool allowYoHwa = isBattleActive && useYoHwaPattern;
        bool allowYohoFoxBead = isBattleActive && useYohoFoxBeadPattern;

        if (basicAttackController != null)
        {
            basicAttackController.enabled = allowBasicAttack;
        }

        if (yoHwaPatternController != null)
        {
            yoHwaPatternController.enabled = allowYoHwa;
        }

        if (yohoFoxBeadPatternController != null)
        {
            yohoFoxBeadPatternController.enabled = allowYohoFoxBead;
        }
    }
}