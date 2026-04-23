// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 보스 피격 수신기다.
// 자식 콜라이더가 맞아도 실제 체력 변경은 BossHealth2D에만 전달한다.
// 최종 피해 규약은 TakeDamage 하나만 사용한다.

[DisallowMultipleComponent]
public sealed class BossDamageReceiver2D : MonoBehaviour, IDamageable2D
{
    [Header("참조")]

    [Tooltip("실제 보스 체력을 관리하는 스크립트\n비어 있으면 부모에서 자동 탐색한다.")]
    [SerializeField] private BossHealth2D bossHealth;

    [Header("디버그")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


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
        {
            FindHealth();
        }
    }

    public void TakeDamage(int damage)
    {
        EnsureHealth();

        if (bossHealth == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[BossDamageReceiver2D] BossHealth2D를 찾지 못했습니다.", this);
            }

            return;
        }

        if (damage <= 0)
        {
            return;
        }

        if (bossHealth.IsDead)
        {
            return;
        }

        if (debugLog)
        {
            Debug.Log($"[BossDamageReceiver2D] 피해 수신 | object={name} damage={damage}", this);
        }

        bossHealth.TakeDamage(damage);
    }

    private void EnsureHealth()
    {
        if (bossHealth == null)
        {
            FindHealth();
        }
    }

    private void FindHealth()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<BossHealth2D>();
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponentInParent<BossHealth2D>();
        }
    }
}