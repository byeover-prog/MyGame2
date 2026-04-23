// UTF-8
using UnityEngine;
using UnityEngine.Events;

// 구현 원리 요약:
// 보스 체력을 관리한다.
// 실제 피해 적용 진입점은 TakeDamage 하나만 사용한다.
// 피격 수신기는 프리팹에 수동 배치하는 것을 기본으로 하고,
// 루트/대표 피격 콜라이더에만 최소한의 자동 보정을 허용한다.

[DisallowMultipleComponent]
public sealed class BossHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("체력")]

    [Tooltip("보스 최대 HP")]
    [Min(1)]
    [SerializeField] private int maxHp = 100;

    [Tooltip("시작 HP\n0 이하이면 최대 HP로 자동 맞춤")]
    [SerializeField] private int startHp = 100;


    [Header("피격 구조")]

    [Tooltip("대표 피격 콜라이더\n비어 있으면 자동 탐색한다.")]
    [SerializeField] private Collider2D hitCollider;

    [Tooltip("루트 오브젝트에 BossDamageReceiver2D가 없을 때만 자동 보정")]
    [SerializeField] private bool autoEnsureReceiverOnRoot = true;

    [Tooltip("대표 피격 콜라이더에 BossDamageReceiver2D가 없을 때만 자동 보정")]
    [SerializeField] private bool autoEnsureReceiverOnHitCollider = true;

    [Tooltip("자식 전체 Collider2D에 자동 부착한다.\n임시 호환용이며 최종 운영에서는 비추천")]
    [SerializeField] private bool autoAddReceiverToAllChildren = false;


    [Header("사망 처리")]

    [Tooltip("사망 시 오브젝트를 비활성화할지 여부")]
    [SerializeField] private bool disableOnDeath = true;

    [Tooltip("사망 시 오브젝트를 파괴할지 여부")]
    [SerializeField] private bool destroyOnDeath = false;

    [Tooltip("파괴 지연 시간")]
    [Min(0f)]
    [SerializeField] private float destroyDelay = 0f;


    [Header("이벤트")]

    [Tooltip("피격 시 실행할 이벤트")]
    [SerializeField] private UnityEvent onDamaged;

    [Tooltip("사망 시 실행할 이벤트")]
    [SerializeField] private UnityEvent onDeath;


    [Header("디버그")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private int currentHp;
    private bool isDead;


    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsDead => isDead;
    public Collider2D HitCollider => hitCollider;


    private void Reset()
    {
        AutoFindHitCollider();
    }

    private void Awake()
    {
        InitializeHealthState();
        EnsureHitStructure();
    }

    private void OnEnable()
    {
        InitializeHealthState();
        EnsureHitStructure();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (debugLog)
        {
            Debug.Log($"[BossHealth2D] 피해 적용 | object={name} damage={damage} hp={currentHp}/{maxHp}", this);
        }

        onDamaged?.Invoke();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void InitializeHealthState()
    {
        maxHp = Mathf.Max(1, maxHp);

        if (startHp <= 0)
        {
            startHp = maxHp;
        }

        currentHp = Mathf.Clamp(startHp, 0, maxHp);
        isDead = currentHp <= 0;

        if (debugLog)
        {
            Debug.Log($"[BossHealth2D] 초기화 | object={name} hp={currentHp}/{maxHp}", this);
        }
    }

    private void EnsureHitStructure()
    {
        AutoFindHitCollider();

        if (autoEnsureReceiverOnRoot)
        {
            EnsureReceiverOnGameObject(gameObject, "루트");
        }

        if (autoEnsureReceiverOnHitCollider && hitCollider != null)
        {
            EnsureReceiverOnGameObject(hitCollider.gameObject, "대표 피격 콜라이더");
        }

        if (autoAddReceiverToAllChildren)
        {
            EnsureReceiverOnAllChildColliders();
        }
    }

    private void AutoFindHitCollider()
    {
        if (hitCollider != null)
        {
            return;
        }

        hitCollider = GetComponent<Collider2D>();

        if (hitCollider == null)
        {
            hitCollider = GetComponentInChildren<Collider2D>(true);
        }
    }

    private void EnsureReceiverOnAllChildColliders()
    {
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < childColliders.Length; i++)
        {
            Collider2D col = childColliders[i];
            if (col == null)
            {
                continue;
            }

            EnsureReceiverOnGameObject(col.gameObject, "자식 콜라이더");
        }
    }

    private void EnsureReceiverOnGameObject(GameObject targetObject, string reason)
    {
        if (targetObject == null)
        {
            return;
        }

        if (targetObject.GetComponent<BossDamageReceiver2D>() != null)
        {
            return;
        }

        targetObject.AddComponent<BossDamageReceiver2D>();

        if (debugLog)
        {
            Debug.Log($"[BossHealth2D] BossDamageReceiver2D 자동 추가 | target={targetObject.name} reason={reason}", this);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (debugLog)
        {
            Debug.Log($"[BossHealth2D] 사망 처리 | object={name}", this);
        }

        onDeath?.Invoke();

        if (destroyOnDeath)
        {
            if (destroyDelay > 0f)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }

            return;
        }

        if (disableOnDeath)
        {
            gameObject.SetActive(false);
        }
    }
}