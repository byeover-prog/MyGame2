using UnityEngine;

public class KumihoFoxFire : MonoBehaviour
{
    [Header("여우불 이동")]

    [Tooltip("여우불 이동 속도")]
    [SerializeField] private float speed = 8f;

    [Tooltip("여우불 데미지")]
    [SerializeField] private int damage = 10;

    private Vector2 direction;

    private bool isLaunch = false;



    public void Launch(Vector2 dir)
    {
        direction = dir;

        isLaunch = true;

        Destroy(gameObject, 5f);
    }


    void Update()
    {
        if (!isLaunch) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }



    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            PlayerHealth hp = col.GetComponent<PlayerHealth>();

            if (hp != null)
                hp.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}