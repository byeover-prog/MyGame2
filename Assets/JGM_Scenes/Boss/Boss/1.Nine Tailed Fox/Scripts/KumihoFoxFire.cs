// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public class KumihoFoxFire : MonoBehaviour
{
    [Header("여우불 이동")]

    [Tooltip("여우불 이동 속도")]
    [SerializeField] private float speed = 8f;

    [Tooltip("여우불 데미지")]
    [SerializeField] private int damage = 10;

    [Tooltip("발사 후 자동 삭제 시간")]
    [SerializeField] private float destroyAfterSeconds = 5f;


    private Vector2 direction;
    private bool isLaunch = false;
    private KumihoFoxFireAttack owner;


    /// <summary>
    /// [구현 원리 요약]
    /// 현재 여우불이 이미 발사되었는지 외부에서 확인할 수 있게 합니다.
    /// </summary>
    public bool IsLaunched => isLaunch;


    /// <summary>
    /// [구현 원리 요약]
    /// 이 여우불을 생성한 공격 스크립트를 저장합니다.
    /// </summary>
    public void SetOwner(KumihoFoxFireAttack foxFireAttack)
    {
        owner = foxFireAttack;
    }


    /// <summary>
    /// [구현 원리 요약]
    /// 플레이어 방향으로 발사 상태로 전환합니다.
    /// </summary>
    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
        isLaunch = true;

        Destroy(gameObject, destroyAfterSeconds);
    }


    private void Update()
    {
        if (!isLaunch) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }


    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!isLaunch) return;

        if (col.CompareTag("Player"))
        {
            PlayerHealth hp = col.GetComponent<PlayerHealth>();

            if (hp != null)
                hp.TakeDamage(damage);

            Destroy(gameObject);
        }
    }


    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.UnregisterFoxFire(this);
        }
    }
}