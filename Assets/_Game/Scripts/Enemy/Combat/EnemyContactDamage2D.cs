using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 적이 플레이어와 접촉하면 일정 주기로 데미지를 줍니다.
/// 넉백 방향 계산을 위해 자신의 위치도 전달합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyContactDamage2D : MonoBehaviour
{
    [Header("접촉 데미지(기본값)")]
    [Tooltip("틱당 데미지 수치입니다.")]
    [SerializeField] private int damage_per_tick = 5;

    [Header("데미지 주기(초)")]
    [Tooltip("접촉 데미지를 주는 간격입니다.")]
    [SerializeField] private float damage_interval = 0.25f;

    [Header("플레이어 태그")]
    [Tooltip("플레이어를 식별하는 태그입니다.")]
    [SerializeField] private string player_tag = "Player";

    [Header("런타임 적용 결과(디버그)")]
    [SerializeField] private int current_damage;

    [Header("디버그")]
    [Tooltip("로그 스팸이 심하니, 정말 필요할 때만 켜세요.")]
    [SerializeField] private bool verbose_log = false;

    private float _nextDamageTime = 0f;

    private void Awake()
    {
        current_damage = Mathf.Max(0, damage_per_tick);
    }

    /// <summary>
    /// EnemyStatsApplier2D에서 호출. EnemyRootSO의 BaseContactDamage가 여기로 주입됩니다.
    /// </summary>
    public void SetDamage(int value)
    {
        current_damage = Mathf.Max(0, value);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(player_tag)) return;
        if (Time.time < _nextDamageTime) return;
        _nextDamageTime = Time.time + damage_interval;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();

        if (hp == null)
        {
            if (verbose_log)
                Debug.LogWarning("[EnemyContactDamage2D] PlayerHealth를 찾지 못했습니다.", this);
            return;
        }

        if (hp.IsDead) return;
        if (hp.IsInvincible) return;

        if (verbose_log)
            Debug.Log($"[EnemyContactDamage2D] 접촉 데미지 발생: {current_damage}");

        // 넉백 방향을 위해 적의 위치를 함께 전달
        hp.TakeDamage(current_damage, (Vector2)transform.position);
    }
}