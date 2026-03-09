// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 플레이어와 접촉 시 일정 주기로 데미지를 준다.
// - 기본값은 인스펙터 설정.
// - EnemyRootSO → EnemyStatsApplier2D에서 SetDamage()로 런타임 주입 가능.
[DisallowMultipleComponent]
public sealed class EnemyContactDamage2D : MonoBehaviour
{
    [Header("접촉 데미지(기본값)")]
    [SerializeField] private int damage_per_tick = 5;

    [Header("데미지 주기(초)")]
    [SerializeField] private float damage_interval = 0.25f;

    [Header("플레이어 태그")]
    [SerializeField] private string player_tag = "Player";

    [Header("런타임 적용 결과(디버그)")]
    [SerializeField] private int current_damage;

    [Header("디버그")]
    [Tooltip("로그 스팸이 심하니, 정말 필요할 때만 켜세요.")]
    [SerializeField] private bool verbose_log = false;

    private float _nextDamageTime = 0f;

    private void Awake()
    {
        // 시작 시 기본값을 current에 반영
        current_damage = Mathf.Max(0, damage_per_tick);
    }

    /// <summary>
    /// EnemyStatsApplier2D에서 호출됨.
    /// EnemyRootSO의 BaseContactDamage가 여기로 들어온다.
    /// </summary>
    public void SetDamage(int value)
    {
        current_damage = Mathf.Max(0, value);
    }

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
                Debug.LogWarning("[EnemyContactDamage2D] PlayerHealth를 찾지 못했습니다. Player 루트에 PlayerHealth가 있는지 확인하세요.");
            return;
        }

        // 사망/무적이면 중단
        if (hp.IsDead) return;
        if (hp.IsInvincible) return;

        if (verbose_log)
            Debug.Log($"[EnemyContactDamage2D] 접촉 데미지 발생: {current_damage}");

        hp.TakeDamage(current_damage);
    }
}