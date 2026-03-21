// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 보스를 따라다니는 어둠 영역
/// - 외부에서 지속시간을 받아 동작
/// - 범위 내 플레이어에게 지속 데미지
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperDarkZone : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("보스 Transform")]
    [SerializeField] private Transform boss;

    [Tooltip("데미지 판정 영역")]
    [SerializeField] private CircleCollider2D damageArea;



    [Header("데미지 설정")]

    [Tooltip("1틱 데미지")]
    [SerializeField] private int damage = 5;

    [Tooltip("데미지 간격")]
    [SerializeField] private float tickInterval = 1f;



    [Header("레이어")]

    [Tooltip("플레이어 레이어")]
    [SerializeField] private LayerMask playerLayer;



    private float tickTimer;
    private float lifeTimer;
    private float duration;

    private readonly Collider2D[] results = new Collider2D[16];

    private bool isRunning;



    public void Init(Transform bossTransform)
    {
        boss = bossTransform;
    }



    // 🔥 duration을 외부에서 받음
    public void ActivateZone(float durationValue)
    {
        duration = durationValue;
        lifeTimer = 0f;
        tickTimer = 0f;
        isRunning = true;
    }



    private void Awake()
    {
        if (damageArea == null)
            damageArea = GetComponentInChildren<CircleCollider2D>();
    }



    private void Update()
    {
        if (!isRunning) return;

        // 보스 따라가기
        if (boss != null)
            transform.position = boss.position;

        // 지속시간 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // 데미지 타이머
        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval) return;

        tickTimer = 0f;

        ApplyDamage();
    }



    private void ApplyDamage()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = playerLayer;
        filter.useTriggers = true;

        int count = damageArea.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            var col = results[i];
            if (col == null) continue;

            PlayerHealth hp =
                col.GetComponent<PlayerHealth>() ??
                col.GetComponentInParent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }
    }
}