using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("체력")]
    [Min(1)] [SerializeField] private int maxHp = 30;
    [Min(0)] [SerializeField] private int currentHp = 30;

    [Header("EXP 풀(필수)")]
    [SerializeField] private ExpOrbPool expOrbPool;

    [Header("EXP 드랍")]
    [SerializeField] private ExpOrb2D expOrbPrefab;
    [Min(0)] [SerializeField] private int expAmount = 3;
    [SerializeField] private bool splitIntoMultipleOrbs = false;
    [Min(0)] [SerializeField] private int maxOrbs = 6;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsDead => _isDead;
    public bool _IsDead => currentHp <= 0;

    private bool _isDead;
    
    private KillCountSource _killCountSource;

    private static ExpOrbPool s_sharedPool;
    private static bool s_prewarmed;

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        _isDead = false;

        EnsureExpPool();

        if (!s_prewarmed && expOrbPool != null && expOrbPrefab != null)
        {
            expOrbPool.Prewarm(expOrbPrefab.gameObject, 64);
            s_prewarmed = true;
        }
        
        _killCountSource = FindFirstObjectByType<KillCountSource>();
    }

    private void OnEnable()
    {
        _isDead = false;
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 1, maxHp);
        EnsureExpPool();
    }

    private void EnsureExpPool()
    {
        if (expOrbPool != null) return;
        if (s_sharedPool != null) { expOrbPool = s_sharedPool; return; }
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

    public void SetMaxAndFill(int hp) => ResetHp(hp);

    /// <summary>
    /// 즉시 사망 처리. Stage0Director 등 연출에서 강제 제거 시 사용.
    /// </summary>
    public void KillImmediate()
    {
        if (_isDead) return;
        TakeDamage(currentHp + 1);
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || damage <= 0) return;
        currentHp -= damage;

#if UNITY_EDITOR
        if (debugLog)
            Debug.Log($"[EnemyHealth2D] 피해 {damage} | HP {Mathf.Max(0, currentHp)}/{maxHp}", this);
#endif

        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (_killCountSource != null) _killCountSource.AddKill();

        if (debugLog)
            Debug.Log("[EnemyHealth2D] 사망", this);

        DropExp();

        // ★ Destroy → 풀 반환
        EnemyPoolTag.ReturnToPool(gameObject);
    }

    private void DropExp()
    {
        if (expAmount <= 0 || expOrbPrefab == null || expOrbPool == null) return;

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

            if (remain <= 0) break;
        }
    }
}