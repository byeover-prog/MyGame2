// EnemyContactDamage2D.cs
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyContactDamage2D : MonoBehaviour
{
    [Header("접촉 데미지")]
    [SerializeField] private int damage_per_tick = 5;

    [Header("데미지 주기(초)")]
    [SerializeField] private float damage_interval = 0.25f;

    [Header("플레이어 태그")]
    [SerializeField] private string player_tag = "Player";

    [Header("디버그")]
    [Tooltip("로그 스팸이 심하니, 정말 필요할 때만 켜세요.")]
    [SerializeField] private bool verbose_log = false;

    private float _nextDamageTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        // 플레이어가 아니면 무시
        if (!other.CompareTag(player_tag)) return;

        // 틱 쿨타임
        if (Time.time < _nextDamageTime) return;
        _nextDamageTime = Time.time + damage_interval;

        // PlayerHealth 찾기 (자식/부모 어느 쪽이어도 대응)
        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();

        if (hp == null)
        {
            if (verbose_log)
                Debug.LogWarning("[EnemyContactDamage2D] PlayerHealth를 찾지 못했습니다. Player 루트(YunSeol)에 PlayerHealth가 있는지 확인하세요.");
            return;
        }

        // 사망/무적이면 데미지 중단 (2중 방어 중 1)
        if (hp.IsDead) return;
        if (hp.IsInvincible) return;

        if (verbose_log)
            Debug.Log("[EnemyContactDamage2D] 접촉 데미지 발생");

        hp.TakeDamage(damage_per_tick);
    }
}