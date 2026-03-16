// UTF-8
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class BossHealth2D : MonoBehaviour, IDamageable2D
{
    [Header("===== 체력 설정 =====")]
    [Tooltip("보스 최대 체력입니다.")]
    [Min(1)]
    [SerializeField] private int maxHp = 300;

    [Tooltip("현재 체력입니다. 시작 시 maxHp 범위로 자동 보정됩니다.")]
    [Min(0)]
    [SerializeField] private int currentHp = 300;

    [Header("===== 사망 처리 =====")]
    [Tooltip("체력이 0 이하가 되면 오브젝트를 파괴할지 결정합니다.")]
    [SerializeField] private bool destroyOnDeath = false;

    [Tooltip("파괴 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float destroyDelay = 0f;

    [Tooltip("destroyOnDeath가 꺼져 있을 때, 사망 후 오브젝트를 비활성화할지 결정합니다.")]
    [SerializeField] private bool disableOnDeath = true;

    [Header("===== 피격 판정 =====")]
    [Tooltip("보스 피격 판정에 사용할 콜라이더입니다.\n비워두면 현재 오브젝트 또는 자식의 Collider2D를 자동으로 찾습니다.")]
    [SerializeField] private Collider2D hitCollider;

    [Tooltip("이 값이 켜져 있으면 피격 관련 로그를 콘솔에 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    [Header("===== 보스 연출 / 이벤트 =====")]
    [Tooltip("피격 시 실행할 이벤트입니다.\n예: 체력바 갱신, 피격 이펙트, 사운드")]
    [SerializeField] private UnityEvent onDamaged;

    [Tooltip("사망 시 1회 실행할 이벤트입니다.\n예: 사망 애니메이션, 포털 생성, 보스전 종료 처리")]
    [SerializeField] private UnityEvent onDeath;

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

        if (debugLog)
        {
            Debug.Log(
                $"[BossHealth2D] Awake | name={name} hp={currentHp}/{maxHp} isDead={isDead} hitCollider={(hitCollider != null ? hitCollider.name : "NULL")}",
                this
            );
        }
    }

    private void OnEnable()
    {
        maxHp = Mathf.Max(1, maxHp);

        if (currentHp <= 0 || currentHp > maxHp)
            currentHp = maxHp;

        isDead = false;

        AutoFindHitCollider();

        if (debugLog)
        {
            Debug.Log(
                $"[BossHealth2D] OnEnable | name={name} hp={currentHp}/{maxHp} hitCollider={(hitCollider != null ? hitCollider.name : "NULL")}",
                this
            );
        }
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 보스가 맞을 콜라이더를 자동 탐색합니다.
    /// 현재 오브젝트에 없으면 자식 콜라이더까지 찾아 연결합니다.
    /// </summary>
    private void AutoFindHitCollider()
    {
        if (hitCollider != null) return;

        hitCollider = GetComponent<Collider2D>();

        if (hitCollider == null)
            hitCollider = GetComponentInChildren<Collider2D>();

        if (debugLog)
        {
            Debug.Log(
                $"[BossHealth2D] AutoFindHitCollider | result={(hitCollider != null ? hitCollider.name : "NULL")}",
                this
            );
        }
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 외부에서 보스 최대 체력을 새로 넣고 현재 체력도 가득 채웁니다.
    /// </summary>
    public void SetMaxAndFill(int hp)
    {
        maxHp = Mathf.Max(1, hp);
        currentHp = maxHp;
        isDead = false;

        if (debugLog)
            Debug.Log($"[BossHealth2D] 체력 초기화 완료 | HP {currentHp}/{maxHp}", this);
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 현재 최대 체력 기준으로 현재 체력을 가득 채웁니다.
    /// </summary>
    public void FullHeal()
    {
        currentHp = maxHp;
        isDead = false;

        if (debugLog)
            Debug.Log($"[BossHealth2D] 전체 회복 | HP {currentHp}/{maxHp}", this);
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 플레이어 공격이나 투사체가 보스에게 데미지를 줄 때 호출하는 기본 함수입니다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (debugLog)
            Debug.Log($"[BossHealth2D] TakeDamage 호출 | 대상={name} damage={damage} isDead={isDead}", this);

        if (isDead)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] 이미 죽은 상태라 피해 무시", this);
            return;
        }

        if (damage <= 0)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] damage가 0 이하라 피해 무시", this);
            return;
        }

        currentHp -= damage;
        currentHp = Mathf.Max(0, currentHp);

        if (debugLog)
            Debug.Log($"[BossHealth2D] 피해 적용 성공 | 피해={damage} | HP {currentHp}/{maxHp}", this);

        onDamaged?.Invoke();

        if (currentHp <= 0)
            Die();
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 플레이어가 BossHealth2D를 직접 찾아서 호출할 때 쓰는 보조 함수입니다.
    /// 내부적으로 TakeDamage와 동일하게 처리합니다.
    /// </summary>
    public void ApplyPlayerDamage(int damage)
    {
        if (debugLog)
            Debug.Log($"[BossHealth2D] ApplyPlayerDamage 호출 | damage={damage}", this);

        TakeDamage(damage);
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 충돌한 오브젝트에서 데미지를 받을 수 있는 대상을 찾아 적용합니다.
    /// 플레이어 공격 스크립트에서 이 함수를 호출하면 더 쉽게 사용할 수 있습니다.
    /// </summary>
    public static bool TryApplyDamage(Collider2D targetCollider, int damage)
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("[BossHealth2D] TryApplyDamage 실패 | targetCollider가 NULL");
            return false;
        }

        if (damage <= 0)
        {
            Debug.LogWarning($"[BossHealth2D] TryApplyDamage 실패 | damage가 0 이하 ({damage})", targetCollider);
            return false;
        }

        Debug.Log(
            $"[BossHealth2D] TryApplyDamage 시작 | target={targetCollider.name} root={targetCollider.transform.root.name} layer={LayerMask.LayerToName(targetCollider.gameObject.layer)} damage={damage}",
            targetCollider
        );

        IDamageable2D damageable = targetCollider.GetComponent<IDamageable2D>();
        if (damageable == null)
            damageable = targetCollider.GetComponentInParent<IDamageable2D>();

        if (damageable != null)
        {
            Debug.Log($"[BossHealth2D] IDamageable2D 발견 | target={targetCollider.name}", targetCollider);
            damageable.TakeDamage(damage);
            return true;
        }

        BossHealth2D bossHealth = targetCollider.GetComponent<BossHealth2D>();
        if (bossHealth == null)
            bossHealth = targetCollider.GetComponentInParent<BossHealth2D>();

        if (bossHealth != null)
        {
            Debug.Log($"[BossHealth2D] BossHealth2D 발견 | target={targetCollider.name}", targetCollider);
            bossHealth.TakeDamage(damage);
            return true;
        }

        Debug.LogWarning(
            $"[BossHealth2D] TryApplyDamage 실패 | {targetCollider.name} 또는 부모에서 IDamageable2D / BossHealth2D를 찾지 못함",
            targetCollider
        );

        return false;
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 회복이 필요할 때 현재 체력을 증가시킵니다.
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] 죽은 상태라 회복 무시", this);
            return;
        }

        if (amount <= 0)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] amount가 0 이하라 회복 무시", this);
            return;
        }

        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        if (debugLog)
            Debug.Log($"[BossHealth2D] 회복 {amount} | HP {currentHp}/{maxHp}", this);
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 즉시 사망 처리합니다.
    /// 컷신, 디버그, 강제 처치용으로 사용할 수 있습니다.
    /// </summary>
    public void Kill()
    {
        if (isDead)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] 이미 사망 상태라 Kill 무시", this);
            return;
        }

        currentHp = 0;
        Die();
    }

    /// <summary>
    /// [구현 원리 요약]
    /// 사망은 한 번만 처리되며,
    /// 사망 이벤트 호출 후 파괴 또는 비활성화가 진행됩니다.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (debugLog)
            Debug.Log("[BossHealth2D] 보스 사망 처리 시작", this);

        onDeath?.Invoke();

        if (destroyOnDeath)
        {
            if (debugLog)
                Debug.Log($"[BossHealth2D] destroyOnDeath 실행 | delay={destroyDelay}", this);

            if (destroyDelay <= 0f)
                Destroy(gameObject);
            else
                Destroy(gameObject, destroyDelay);
        }
        else if (disableOnDeath)
        {
            if (debugLog)
                Debug.Log("[BossHealth2D] disableOnDeath 실행 | 오브젝트 비활성화", this);

            gameObject.SetActive(false);
        }
    }
}