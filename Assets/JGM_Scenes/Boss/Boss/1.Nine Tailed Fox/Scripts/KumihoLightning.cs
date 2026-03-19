// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 번개가 생성된 위치에서 충돌 시 데미지를 주고
/// 일정 시간 후 자동 삭제
/// </summary>

[DisallowMultipleComponent]
public class KumihoLightning : MonoBehaviour
{
    [Header("===== 데미지 =====")]

    [Tooltip("번개 데미지")]
    [SerializeField] private int damage = 20;

    [Tooltip("지속 시간")]
    [SerializeField] private float lifeTime = 1.5f;


    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth hp = collision.GetComponent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }
    }
}