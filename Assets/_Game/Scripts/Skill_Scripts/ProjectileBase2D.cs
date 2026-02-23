using UnityEngine;

public abstract class ProjectileBase2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Collider2D col;

    protected int damage;
    protected float dieAt;
    protected LayerMask enemyMask;
    protected Transform owner;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 발시와 동일한 “발사 코어”.
    /// </summary>
    public virtual void Launch(
        Vector2 direction,
        int newDamage,
        float speed,
        float lifeSeconds,
        LayerMask newEnemyMask,
        Transform newOwner)
    {
        damage = Mathf.Max(1, newDamage);
        enemyMask = newEnemyMask;
        owner = newOwner;

        dieAt = Time.time + Mathf.Max(0.05f, lifeSeconds);

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction.normalized * Mathf.Max(0f, speed);
            rb.angularVelocity = 0f;
        }

        // 투사체는 월드에 남는 게 정답(부모 붙어있으면 떼기)
        if (transform.parent != null)
            transform.SetParent(null, true);
    }

    protected virtual void Update()
    {
        if (Time.time >= dieAt)
        {
            OnLifeEnded();
        }
    }

    protected virtual void OnLifeEnded()
    {
        // 여기서 Destroy / 풀 반환. 프로젝트에 풀 인터페이스 있으면 바꿔치기.
        Destroy(gameObject);
    }
}