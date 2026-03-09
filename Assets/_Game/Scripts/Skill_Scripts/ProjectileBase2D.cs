using UnityEngine;

public abstract class ProjectileBase2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] protected Collider2D col;

    [Header("충돌")]
    [Tooltip("발사 시 오너(플레이어) 콜라이더와 충돌을 무시한다(몸에 갇힘 방지)")]
    [SerializeField] private bool ignoreOwnerCollision = true;

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

        if (ignoreOwnerCollision)
            IgnoreOwnerCollisions();

        // 투사체는 월드에 남는 게 정답(부모 붙어있으면 떼기)
        if (transform.parent != null)
            transform.SetParent(null, true);
    }

    private void IgnoreOwnerCollisions()
    {
        if (owner == null) return;

        // 내 콜라이더가 없으면 스킵
        var myCols = GetComponentsInChildren<Collider2D>(true);
        if (myCols == null || myCols.Length == 0) return;

        var ownerCols = owner.GetComponentsInChildren<Collider2D>(true);
        if (ownerCols == null || ownerCols.Length == 0) return;

        for (int i = 0; i < myCols.Length; i++)
        {
            var a = myCols[i];
            if (a == null) continue;

            for (int j = 0; j < ownerCols.Length; j++)
            {
                var b = ownerCols[j];
                if (b == null) continue;
                Physics2D.IgnoreCollision(a, b, true);
            }
        }
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