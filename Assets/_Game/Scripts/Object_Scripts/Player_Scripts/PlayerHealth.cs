// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("피격 무적(불합리함 방지)")]
    [Tooltip("피격 후 이 시간 동안 추가 피해를 무시합니다. (권장: 0.12 ~ 0.25)")]
    [SerializeField] private float invincibleDuration = 0.18f;

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

        // 시작은 풀피가 자연스러움
        currentHp = maxHp;

        _col = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void SetMaxHpBonus(int bonus, bool healToFull)
    {
        int newMax = Mathf.Max(1, _baseMaxHp + Mathf.Max(0, bonus));
        maxHp = newMax;

        if (healToFull) currentHp = maxHp;
        else currentHp = Mathf.Min(currentHp, maxHp);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;
        if (IsInvincible) return;

        currentHp = Mathf.Max(0, currentHp - amount);

        // 무적 예약
        _invincibleUntil = Time.time + invincibleDuration;

        if (debugLog)
            Debug.Log($"[PlayerHealth] 피해 {amount} | HP {currentHp}/{maxHp}", this);

        if (currentHp <= 0)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 사망 로그는 항상 출력(네가 원하는 “죽었는데 안 뜸” 방지)
        Debug.Log("[PlayerHealth] 사망", this);

        // 사망 후에도 트리거가 계속 돌면서 데미지 루프가 발생하는 문제를 여기서 끊는다.
        if (disableColliderOnDead && _col != null)
            _col.enabled = false;

        if (disableRigidbodyOnDead && _rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        // 시스템 신호
        RunSignals.RaisePlayerDead();

        // 신호가 실제로 실행됐는지 확인용(필요하면 잠깐만 켜고 보자)
        if (debugLog)
            Debug.Log("[PlayerHealth] RunSignals.RaisePlayerDead() 호출 완료", this);
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