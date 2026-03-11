using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 플레이어 체력을 관리합니다.
/// - 피격 시 무적 + 피격 이펙트(HitFlash2D) + 즉시 넉백(1프레임 위치 밀기)
/// - 회복 / 임시 무적 / 받는 피해 배율 지원
/// - 사망 시 RunSignals.PlayerDead를 발행합니다.
/// - FixedUpdate를 사용하지 않습니다(PlayerMover2D와 rb.linearVelocity 충돌 방지).
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [Header("체력")]
    [Tooltip("플레이어 최대 HP입니다.")]
    [SerializeField] private int maxHp = 100;

    [Tooltip("현재 HP입니다(시작 시 maxHp로 초기화).")]
    [SerializeField] private int currentHp = 100;

    [Header("피격 무적(불합리함 방지)")]
    [Tooltip("피격 후 이 시간 동안 추가 피해를 무시합니다. (권장: 0.12 ~ 0.25)")]
    [SerializeField] private float invincibleDuration = 0.18f;

    [Header("피격 리액션")]
    [Tooltip("피격 시 플래시 이펙트 컴포넌트입니다.\n비우면 자동 탐색합니다.")]
    [SerializeField] private HitFlash2D hitFlash;

    [Tooltip("피격 시 밀려나는 거리(월드 유닛)입니다. 0이면 넉백 없음.")]
    [Min(0f)]
    [SerializeField] private float knockbackDistance = 0.3f;

    [Header("추가 스탯")]
    [Tooltip("받는 피해 배율을 읽을 전투 스탯 컴포넌트입니다.")]
    [SerializeField] private PlayerCombatStats2D combatStats;

    [Header("사망 처리")]
    [Tooltip("사망 시 충돌/트리거를 끊기 위해 콜라이더를 끕니다.")]
    [SerializeField] private bool disableColliderOnDead = true;

    [Tooltip("사망 시 물리 시뮬레이션을 끊기 위해 Rigidbody2D.simulated를 끕니다.")]
    [SerializeField] private bool disableRigidbodyOnDead = true;

    [Header("디버그")]
    [Tooltip("피해/회복 같은 로그를 보고 싶을 때만 켜세요. (사망 로그는 항상 출력)")]
    [SerializeField] private bool debugLog = false;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsDead => _isDead;
    public bool IsInvincible => Time.time < _invincibleUntil;

    private bool _isDead = false;
    private float _invincibleUntil = 0f;
    private int _baseMaxHp;

    private Collider2D _col;
    private Rigidbody2D _rb;

    private void Awake()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        _baseMaxHp = maxHp;
        currentHp = maxHp;

        _col = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();

        if (hitFlash == null) hitFlash = GetComponent<HitFlash2D>();
        if (hitFlash == null) hitFlash = GetComponentInChildren<HitFlash2D>();

        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();
    }

    public void SetMaxHpBonus(int bonus, bool healToFull)
    {
        int newMax = Mathf.Max(1, _baseMaxHp + Mathf.Max(0, bonus));
        maxHp = newMax;

        if (healToFull) currentHp = maxHp;
        else currentHp = Mathf.Min(currentHp, maxHp);
    }

    // ── 회복 (A4 대체 카드용) ──────────────────

    /// <summary>체력을 즉시 회복합니다.</summary>
    public void Heal(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;

        int prev = currentHp;
        currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);

        if (debugLog)
            Debug.Log($"[PlayerHealth] 회복 {amount} | HP {prev} -> {currentHp}/{maxHp}", this);
    }

    // ── 임시 무적 (A4 대체 카드용) ────────────

    /// <summary>지정 시간 동안 무적 상태를 부여합니다.</summary>
    public void ActivateTemporaryInvincibility(float duration)
    {
        if (_isDead) return;
        if (duration <= 0f) return;

        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + duration);

        if (debugLog)
            Debug.Log($"[PlayerHealth] 임시 무적 {duration:0.##}초", this);
    }

    // ── 피격 ───────────────────────────────────

    /// <summary>피격 (넉백 없음). 기존 호환용.</summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;
        if (IsInvincible) return;

        int finalDamage = ApplyIncomingDamageMultiplier(amount);
        currentHp = Mathf.Max(0, currentHp - finalDamage);
        _invincibleUntil = Time.time + invincibleDuration;

        if (hitFlash != null)
            hitFlash.Play();

        if (debugLog)
            Debug.Log($"[PlayerHealth] 피해 {finalDamage} | HP {currentHp}/{maxHp}", this);

        if (currentHp <= 0)
            Die();
    }

    /// <summary>피격 (넉백 포함). 적의 위치를 넘기면 반대 방향으로 즉시 밀립니다.</summary>
    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (_isDead) return;
        if (amount <= 0) return;
        if (IsInvincible) return;

        if (knockbackDistance > 0f)
        {
            Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            transform.position += (Vector3)(dir * knockbackDistance);
        }

        TakeDamage(amount);
    }

    /// <summary>받는 피해에 방어력 배율을 적용합니다.</summary>
    private int ApplyIncomingDamageMultiplier(int rawDamage)
    {
        if (combatStats == null)
            return rawDamage;

        float finalDamage = rawDamage * combatStats.IncomingDamageMul;
        return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    }

    // ── 사망 ───────────────────────────────────

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log("[PlayerHealth] 사망", this);

        if (disableColliderOnDead && _col != null)
            _col.enabled = false;

        if (disableRigidbodyOnDead && _rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        RunSignals.RaisePlayerDead();
    }

    public void HealFull()
    {
        _isDead = false;
        currentHp = maxHp;
        _invincibleUntil = 0f;

        if (_col != null) _col.enabled = true;
        if (_rb != null) _rb.simulated = true;
    }
}