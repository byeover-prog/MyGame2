// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("체력")]
    [Min(1)]
    [SerializeField] private int maxHp = 30;

    [Min(0)]
    [SerializeField] private int currentHp = 30;

    [Header("사망 처리")]
    [SerializeField] private bool destroyOnDeath = true;

    [Min(0f)]
    [SerializeField] private float destroyDelay = 0f;

    [Header("EXP 풀(필수)")]
    [Tooltip("ExpOrb 풀.\n비워두면 런타임에 자동 생성(프로토타입용).\n※ 자동 생성은 '공유 풀 1개'만 생성합니다.")]
    [SerializeField] private ExpOrbPool expOrbPool;

    [Header("EXP 드랍")]
    [Tooltip("드랍에 사용할 ExpOrb 프리팹(ExpOrb2D가 붙어 있어야 함)")]
    [SerializeField] private ExpOrb2D expOrbPrefab;

    [Tooltip("드랍할 EXP 총량")]
    [Min(0)]
    [SerializeField] private int expAmount = 3;

    [Tooltip("expAmount를 여러 개로 쪼개서 드랍할지")]
    [SerializeField] private bool splitIntoMultipleOrbs = false;

    [Tooltip("여러 개로 쪼갤 때 최대 개수(0이면 자동: expAmount 기준)")]
    [Min(0)]
    [SerializeField] private int maxOrbs = 6;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsDead => _isDead;
    public bool _IsDead => currentHp <= 0;

    private bool _isDead;

    // 프로토타입 안전장치: "적마다 풀 생성"을 막기 위해 공유 풀 1개만 둔다.
    private static ExpOrbPool s_sharedPool;
    private static bool s_prewarmed;

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        _isDead = false;

        EnsureExpPool();

        // 미리 좀 만들어두면 드랍 순간 GC/스파이크 줄어듦(한 번만)
        if (!s_prewarmed && expOrbPool != null && expOrbPrefab != null)
        {
            expOrbPool.Prewarm(expOrbPrefab.gameObject, 64);
            s_prewarmed = true;
        }
    }

    private void OnEnable()
    {
        _isDead = false;

        maxHp = Mathf.Max(1, maxHp);
        // 스폰 시 기본은 "풀피로 활성화"가 자연스러워서 1~max로 클램프
        currentHp = Mathf.Clamp(currentHp, 1, maxHp);

        EnsureExpPool();
    }

    private void EnsureExpPool()
    {
        if (expOrbPool != null) return;

        // 공유 풀 있으면 그걸 사용
        if (s_sharedPool != null)
        {
            expOrbPool = s_sharedPool;
            return;
        }

        // 공유 풀 없으면 1회만 생성
        var go = new GameObject("ExpOrbPool(Shared)");
        s_sharedPool = go.AddComponent<ExpOrbPool>();
        expOrbPool = s_sharedPool;
    }

    public void ResetHp(int newMaxHp)
    {
        maxHp = Mathf.Max(1, newMaxHp);
        currentHp = maxHp;
        _isDead = false;
    }

    // EnemyStatsApplier2D가 호출하는 표준 메서드(네 에러 해결용)
    public void SetMaxAndFill(int hp)
    {
        ResetHp(hp);
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        if (damage <= 0) return;

        currentHp -= damage;

        if (debugLog)
            Debug.Log($"[EnemyHealth2D] 피해 {damage} | HP {Mathf.Max(0, currentHp)}/{maxHp}", this);

        if (currentHp <= 0)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (debugLog)
            Debug.Log("[EnemyHealth2D] 사망", this);

        DropExp();

        if (destroyOnDeath)
        {
            if (destroyDelay <= 0f) Destroy(gameObject);
            else Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void DropExp()
    {
        if (expAmount <= 0) return;
        if (expOrbPrefab == null) return;
        if (expOrbPool == null) return;

        Vector2 origin = transform.position;

        if (!splitIntoMultipleOrbs)
        {
            GameObject go = expOrbPool.Get(expOrbPrefab.gameObject, origin, Quaternion.identity);
            var orb = go.GetComponent<ExpOrb2D>();
            if (orb != null) orb.SetExp(expAmount);
            return;
        }

        int desired = (maxOrbs > 0) ? maxOrbs : expAmount;
        int count = Mathf.Clamp(desired, 1, 32);

        int remain = expAmount;

        for (int i = 0; i < count; i++)
        {
            int slotsLeft = count - i;
            int chunk = Mathf.Max(1, remain / slotsLeft);
            if (chunk > remain) chunk = remain;
            remain -= chunk;

            Vector2 offset = Random.insideUnitCircle * 0.35f;

            GameObject go = expOrbPool.Get(expOrbPrefab.gameObject, origin + offset, Quaternion.identity);
            var orb = go.GetComponent<ExpOrb2D>();
            if (orb != null) orb.SetExp(chunk);

            if (remain <= 0)
                break;
        }
    }
}