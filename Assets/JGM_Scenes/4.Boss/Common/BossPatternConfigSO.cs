// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 모든 보스 패턴 SO가 공통으로 물려받는 기본 데이터다.
// 패턴 ID와 쿨타임 같은 공통 정보만 먼저 둔다.

public abstract class BossPatternConfigSO : ScriptableObject
{
    [Header("공통 패턴 정보")]

    [Tooltip("패턴 식별용 ID입니다.")]
    [SerializeField] private string patternId = "boss_pattern";

    [Tooltip("패턴 1회 종료 후 다시 시작되기까지의 쿨타임입니다.")]
    [Min(0f)]
    [SerializeField] private float cooldown = 6f;

    [Tooltip("보스가 활성화되자마자 첫 패턴을 바로 시작할지 여부입니다.")]
    [SerializeField] private bool playOnEnable = false;

    public string PatternId => patternId;
    public float Cooldown => cooldown;
    public bool PlayOnEnable => playOnEnable;
}