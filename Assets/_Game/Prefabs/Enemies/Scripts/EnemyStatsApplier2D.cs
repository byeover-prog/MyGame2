// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - EnemyRootSO에서 뽑힌 "기본 스탯"을 프리팹에 주입한다.
// - 실제 HP/이동/데미지 컴포넌트가 무엇이든, 이 스크립트 한 곳에서만 연결한다(SRP).
[DisallowMultipleComponent]
public sealed class EnemyStatsApplier2D : MonoBehaviour, IEnemyInit2D
{
    [Header("런타임 적용 결과(디버그)")]
    [SerializeField] private int currentHP;
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private int currentContactDamage;

    // 너 프로젝트의 실제 컴포넌트 이름에 맞게 연결해라(없으면 아래 3개부터 만들어도 됨)
    [SerializeField] private EnemyHealth2D health;
    [SerializeField] private EnemyMotor2D motor;
    [SerializeField] private EnemyContactDamage2D contactDamage;

    public void ApplyBaseStats(int hp, float moveSpeed, int contactDamageValue)
    {
        currentHP = hp;
        currentMoveSpeed = moveSpeed;
        currentContactDamage = contactDamageValue;

        if (health != null) health.SetMaxAndFill(hp);
        if (motor != null) motor.SetMoveSpeed(moveSpeed);
        if (contactDamage != null) contactDamage.SetDamage(contactDamageValue);
    }
}