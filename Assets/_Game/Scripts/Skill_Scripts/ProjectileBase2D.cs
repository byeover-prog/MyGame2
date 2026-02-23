using UnityEngine;

public abstract class ProjectileBase2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] protected Collider2D col;

    protected int damage;
    protected float dieAt;
    protected LayerMask enemyMask;
    protected Transform owner;

    protected Vector2 velocity;

    protected virtual void Awake()
    {
        if (col == null) col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 발사 코어. 방향/데미지/속도/수명/마스크를 받아 초기화.
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

        velocity = direction.normalized * Mathf.Max(0f, speed);

        // 투사체는 월드에 남는 게 정답(부모 붙어있으면 떼기)
        if (transform.parent != null)
            transform.SetParent(null, true);
    }

    protected virtual void Update()
    {
        if (Time.time >= dieAt)
        {
            OnLifeEnded();
            return;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    protected virtual void OnLifeEnded()
    {
        Destroy(gameObject);
    }
}