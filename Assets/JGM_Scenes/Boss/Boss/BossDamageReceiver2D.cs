// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 보스 피격 수신기입니다.
/// 인터페이스 호출, 직접 함수 호출 모두 BossHealth2D로 전달합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossDamageReceiver2D : MonoBehaviour, IDamageable2D
{
    [Header("참조")]
    [Tooltip("실제 보스 체력을 관리하는 스크립트입니다.\n비워두면 자동 탐색합니다.")]
    [SerializeField] private BossHealth2D bossHealth;

    [Header("디버그")]
    [Tooltip("체크하면 피격 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    public bool IsDead
    {
        get
        {
            EnsureHealth();
            return bossHealth != null && bossHealth.IsDead;
        }
    }

    private void Reset()
    {
        FindHealth();
    }

    private void Awake()
    {
        FindHealth();
    }

    private void OnValidate()
    {
        if (bossHealth == null)
            FindHealth();
    }

    public void TakeDamage(int damage)
    {
        EnsureHealth();

        if (bossHealth == null) return;
        if (damage <= 0) return;
        if (bossHealth.IsDead) return;

        if (debugLog)
            Debug.Log($"[BossDamageReceiver2D] TakeDamage 수신 | object={name} damage={damage}", this);

        bossHealth.TakeDamage(damage);
    }

    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void ApplyPlayerDamage(int damage)
    {
        TakeDamage(damage);
    }

    private void EnsureHealth()
    {
        if (bossHealth == null)
            FindHealth();
    }

    private void FindHealth()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth2D>();

        if (bossHealth == null)
            bossHealth = GetComponentInParent<BossHealth2D>();

        if (bossHealth == null)
            bossHealth = GetComponentInChildren<BossHealth2D>(true);
    }
}