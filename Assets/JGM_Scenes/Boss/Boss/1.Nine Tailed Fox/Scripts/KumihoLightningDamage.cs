// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 번개가 플레이어와 충돌하면 PlayerHealth를 찾아 데미지를 줍니다.
/// </summary>
[DisallowMultipleComponent]
public class KumihoLightningDamage : MonoBehaviour
{
    [Header("===== 데미지 설정 =====")]

    [Tooltip("플레이어에게 줄 데미지")]
    [SerializeField] private int damage = 20;

    [Tooltip("한 번 맞고 사라질지 여부")]
    [SerializeField] private bool destroyOnHit = true;


    // ─────────────────────────────────────────────
    // 충돌 감지 (Trigger)
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player 태그 체크
        if (!other.CompareTag("Player")) return;

        // PlayerHealth 찾기
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player != null)
        {
            // 데미지 적용
            player.TakeDamage(damage);
        }

        // 번개 삭제 (옵션)
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}