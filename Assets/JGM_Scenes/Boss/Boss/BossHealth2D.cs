// UTF-8
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [구현 원리 요약]
/// 보스 체력을 관리합니다.
/// 인터페이스 호출, 메시지 호출, 자식 콜라이더 피격까지 최대한 모두 수용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("체력")]
    [Tooltip("보스 최대 HP입니다.")]
    [SerializeField] private int maxHp = 100;

    [Tooltip("현재 HP입니다.")]
    [SerializeField] private int currentHp = 100;

    [Header("피격 콜라이더")]
    [Tooltip("대표 피격 콜라이더입니다.\n비워두면 자동 탐색합니다.")]
    [SerializeField] private Collider2D hitCollider;

    [Tooltip("체크하면 자식의 모든 Collider2D에 BossDamageReceiver2D를 자동 추가합니다.")]
    [SerializeField] private bool autoAddReceiverToChildren = true;

    [Header("사망 처리")]
    [Tooltip("사망 시 오브젝트를 파괴할지 여부입니다.")]
    [SerializeField] private bool destroyOnDeath = false;

    [Tooltip("사망 시 오브젝트를 비활성화할지 여부입니다.")]
    [SerializeField] private bool disableOnDeath = true;

    [Tooltip("파괴 지연 시간입니다.")]
    [SerializeField] private float destroyDelay = 0f;

    [Header("이벤트")]
    [Tooltip("피격 시 1회 실행할 이벤트입니다.")]
    [SerializeField] private UnityEvent onDamaged;

    [Tooltip("사망 시 1회 실행할 이벤트입니다.")]
    [SerializeField] private UnityEvent onDeath;

    [Header("디버그")]
    [Tooltip("체크하면 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsDead => isDead;
    public Collider2D HitCollider => hitCollider;

    private bool isDead;

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        isDead = currentHp <= 0;

        AutoFindHitCollider();
        EnsureReceivers();

        if (debugLog)
            Debug.Log($"[BossHealth2D] Awake | {name} hp={currentHp}/{maxHp}", this);
    }

    private void OnEnable()
    {
        maxHp = Mathf.Max(1, maxHp);

        if (currentHp <= 0 || currentHp > maxHp)
            currentHp = maxHp;

        isDead = false;

        AutoFindHitCollider();
        EnsureReceivers();

        if (debugLog)
            Debug.Log($"[BossHealth2D] OnEnable | {name} hp={currentHp}/{maxHp}", this);
    }

    private void AutoFindHitCollider()
    {
        if (hitCollider != null) return;

        hitCollider = GetComponent<Collider2D>();

        if (hitCollider == null)
            hitCollider = GetComponentInChildren<Collider2D>(true);
    }

    private void EnsureReceivers()
    {
        AddReceiverIfMissing(gameObject);

        if (!autoAddReceiverToChildren) return;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            AddReceiverIfMissing(cols[i].gameObject);
        }
    }

    private void AddReceiverIfMissing(GameObject target)
    {
        if (target == null) return;

        if (target.GetComponent<BossDamageReceiver2D>() == null)
            target.AddComponent<BossDamageReceiver2D>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (damage <= 0) return;

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (debugLog)
            Debug.Log($"[BossHealth2D] 피해 적용 성공 | damage={damage} hp={currentHp}/{maxHp}", this);

        onDamaged?.Invoke();

        if (currentHp <= 0)
            Die();
    }

    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void ApplyPlayerDamage(int damage)
    {
        TakeDamage(damage);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();

        if (destroyOnDeath)
        {
            if (destroyDelay <= 0f) Destroy(gameObject);
            else Destroy(gameObject, destroyDelay);
        }
        else if (disableOnDeath)
        {
            gameObject.SetActive(false);
        }
    }
}