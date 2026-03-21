// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public class DarkWaveMover : MonoBehaviour
{
    [Header("데미지")]

    [Tooltip("플레이어 데미지")]
    [SerializeField] private int damage = 10;

    [Tooltip("지속 시간")]
    [SerializeField] private float lifeTime = 5f;

    private Vector3 dir;
    private float speed;


    public void Init(Vector3 moveDir, float moveSpeed)
    {
        dir = moveDir;
        speed = moveSpeed;
    }


    private void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }


    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }
    }
}