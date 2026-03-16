using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExpOrb2D : MonoBehaviour
{
    [Header("EXP")]
    [Min(0)]
    [SerializeField] private int expValue = 3;

    [Header("플레이어 참조")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("자석(흡수) 설정")]
    [Min(0.1f)]  [SerializeField] private float magnetRange = 3.0f;
    [Min(0.01f)] [SerializeField] private float magnetMinSpeed = 1.5f;
    [Min(0.01f)] [SerializeField] private float magnetMaxSpeed = 10.0f;
    [Min(0.1f)]  [SerializeField] private float magnetCurvePower = 2.0f;
    [Min(0.01f)] [SerializeField] private float pickupDistance = 0.25f;

    [Header("물리(선택)")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    [SerializeField] private bool useRigidbodyIfExists = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private PlayerExp _playerExp;
    private PlayerCombatStats2D _playerStats;
    private ExpOrbPool _pool;
    private GameObject _prefabKey;
    private bool _collected;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();

        magnetRange = Mathf.Max(0.1f, magnetRange);
        magnetMinSpeed = Mathf.Max(0.01f, magnetMinSpeed);
        magnetMaxSpeed = Mathf.Max(magnetMinSpeed, magnetMaxSpeed);
        pickupDistance = Mathf.Max(0.01f, pickupDistance);
        magnetCurvePower = Mathf.Max(0.1f, magnetCurvePower);
    }

    private void OnEnable()
    {
        _collected = false;

        if (col != null) col.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        CachePlayer(forceRefreshExp: true);
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void SetExp(int value)
    {
        expValue = Mathf.Max(0, value);
    }

    public void SetPool(ExpOrbPool pool) => _pool = pool;
    public void SetOriginPrefab(GameObject prefabKey) => _prefabKey = prefabKey;

    private void CachePlayer(bool forceRefreshExp)
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }

        if (player == null) return;

        if (forceRefreshExp || _playerExp == null)
        {
            if (!player.TryGetComponent(out _playerExp))
            {
                _playerExp = player.GetComponentInParent<PlayerExp>();
                if (_playerExp == null)
                    _playerExp = player.GetComponentInChildren<PlayerExp>(true);
            }
        }

        if (forceRefreshExp || _playerStats == null)
        {
            if (!player.TryGetComponent(out _playerStats))
            {
                _playerStats = player.GetComponentInParent<PlayerCombatStats2D>();
                if (_playerStats == null)
                    _playerStats = player.GetComponentInChildren<PlayerCombatStats2D>(true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (_collected) return;

        if (player == null)
            CachePlayer(forceRefreshExp: false);

        if (player == null) return;

        Vector2 orbPos = transform.position;
        Vector2 playerPos = player.position;
        float dist = Vector2.Distance(orbPos, playerPos);

        float finalMagnetRange = magnetRange * (_playerStats != null ? _playerStats.PickupRangeMul : 1f);

        if (dist <= pickupDistance)
        {
            Collect();
            return;
        }

        if (dist > finalMagnetRange)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float t = 1f - Mathf.Clamp01(dist / Mathf.Max(0.01f, finalMagnetRange));
        float curved = Mathf.Pow(t, magnetCurvePower);

        float speed = Mathf.Lerp(magnetMinSpeed, magnetMaxSpeed, curved);
        Vector2 dir = (playerPos - orbPos).normalized;
        Vector2 vel = dir * speed;

        if (useRigidbodyIfExists && rb != null) rb.linearVelocity = vel;
        else transform.position = orbPos + vel * Time.fixedDeltaTime;
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (expValue > 0)
        {
            if (_playerExp == null)
                CachePlayer(forceRefreshExp: true);

            if (_playerExp != null)
            {
                _playerExp.AddExp(Mathf.Max(1, Mathf.RoundToInt(expValue * (_playerStats != null ? _playerStats.ExpGainMul : 1f))));
                if (debugLog) Debug.Log($"[ExpOrb2D] Collect exp={expValue} x{(_playerStats != null ? _playerStats.ExpGainMul : 1f):F2} (AddExp 호출 성공)", this);
            }
            else
            {
                Debug.LogError($"[ExpOrb2D] PlayerExp를 찾지 못해 EXP 지급 실패 | player={(player != null ? player.name : "NULL")} | root={(player != null ? player.root.name : "NULL")}", this);
            }
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);

        if (_pool != null && _prefabKey != null)
        {
            _pool.Release(_prefabKey, gameObject);
            return;
        }
    }
}