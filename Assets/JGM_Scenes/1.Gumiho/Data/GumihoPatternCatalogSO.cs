// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 구미호가 사용하는 패턴 SO를 한 곳에서 묶어 관리한다.
// 기본 공격, 요화, 여우구슬 패턴을 모두 여기서 연결한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Gumiho/패턴 카탈로그",
    fileName = "GumihoPatternCatalogSO")]
public sealed class GumihoPatternCatalogSO : ScriptableObject
{
    [Header("기본 공격")]

    [Tooltip("구미호 기본 화염구 공격 설정 SO입니다.")]
    [SerializeField] private GumihoBasicAttackConfigSO basicAttackConfig;


    [Header("특수 패턴")]

    [Tooltip("구미호 요화 패턴 설정 SO입니다.")]
    [SerializeField] private GumihoYoHwaPatternConfigSO yoHwaPatternConfig;

    [Tooltip("구미호 여우구슬 패턴 설정 SO입니다.")]
    [SerializeField] private GumihoYohoFoxBeadPatternConfigSO yohoFoxBeadPatternConfig;


    public GumihoBasicAttackConfigSO BasicAttackConfig => basicAttackConfig;
    public GumihoYoHwaPatternConfigSO YoHwaPatternConfig => yoHwaPatternConfig;
    public GumihoYohoFoxBeadPatternConfigSO YohoFoxBeadPatternConfig => yohoFoxBeadPatternConfig;
}