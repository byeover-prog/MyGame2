using System;
using UnityEngine;

/// <summary>
/// 적의 체력만 관리하는 컴포넌트입니다.
///
/// 왜 이렇게 바꾸는가:
/// - 이전에는 EnemyHealth2D가 체력, 경험치 드랍, 킬 카운트, 풀 반환까지
///   한 번에 담당할 가능성이 높았습니다.
/// - 새 구조에서는 EnemyHealth2D는 체력 변화와 죽음 판정만 담당하고,
///   실제 사망 후처리는 MonsterDeathHandler2D가 맡도록 분리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("1. 체력 설정")]
    [SerializeField, Min(1), Tooltip("몬스터 최대 체력입니다.\n"
                                     + "실제 게임에서는 MonsterRuntimeApplier2D가\n"
                                     + "MonsterDefinitionSO의 MaxHp를 반올림해 주입합니다.")]
    private int maxHp = 30;

    [SerializeField, Min(0), Tooltip("현재 체력입니다.\n"
                                     + "주로 디버그 확인용이며,\n"
                                     + "활성화 시 유효 범위로 자동 보정됩니다.")]
    private int currentHp = 30;

    [Header("2. 디버그")]
    [SerializeField, Tooltip("피해와 사망 로그를 출력할지 여부입니다.\n"
                             + "전투 검증 시에만 켜는 것을 권장합니다.")]
    private bool debugLog = false;

    /// <summary>최대 체력</summary>
    public int MaxHp => maxHp;

    /// <summary>현재 체력</summary>
    public int CurrentHp => currentHp;

    /// <summary>사망 여부</summary>
    public bool IsDead => isDead;

    /// <summary>
    /// 체력이 0 이하가 되어 사망했을 때 발생하는 이벤트입니다.
    /// 실제 사망 후처리는 MonsterDeathHandler2D가 이 이벤트를 받아 처리합니다.
    /// </summary>
    public event Action<EnemyHealth2D> Died;

    private bool isDead;

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        isDead = false;
    }

    private void OnEnable()
    {
        isDead = false;
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 1, maxHp);
    }

    /// <summary>
    /// 최대 체력을 바꾸고 현재 체력도 가득 채웁니다.
    /// 런타임 주입기에서 MaxHp 적용 시 사용합니다.
    /// </summary>
    public void ResetHp(int newMaxHp)
    {
        maxHp = Mathf.Max(1, newMaxHp);
        currentHp = maxHp;
        isDead = false;
    }

    /// <summary>
    /// MaxHp를 설정하고 현재 체력을 가득 채우는 편의 메서드입니다.
    /// </summary>
    public void SetMaxAndFill(int hp)
    {
        ResetHp(hp);
    }

    /// <summary>
    /// 피해를 적용합니다.
    /// 체력이 0 이하가 되면 죽음 이벤트만 발생시키고,
    /// 실제 후처리는 여기서 하지 않습니다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
            return;

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (debugLog)
        {
            Debug.Log(
                $"[EnemyHealth2D] 피해 {damage} | HP {currentHp}/{maxHp}",
                this);
        }

        if (currentHp > 0)
            return;

        Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHp = 0;

        if (debugLog)
            Debug.Log("[EnemyHealth2D] 사망 판정", this);

        Died?.Invoke(this);
    }
}