using UnityEngine;

[DisallowMultipleComponent]
public sealed class BoomerangWeapon2D : CommonSkillWeapon2D
{
    [Header("풀")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("스폰")]
    [SerializeField] private Transform spawnPoint;

    [Header("연사(순차 발사)")]
    [Tooltip("투사체가 2개 이상일 때, 동시에 나가지 않고 이 간격으로 순차 발사됩니다(초).")]
    [SerializeField, Min(0f)] private float burstIntervalSeconds = 0.08f;

    [Header("비주얼")]
    [Tooltip("부메랑 스프라이트가 회전하는 속도(도/초). 0이면 회전 없음.")]
    [SerializeField] private float spinDegreesPerSecond = 720f;

    private bool _isBurstFiring;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void Update()
    {
        if (config == null) return;
        if (_isBurstFiring) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        EnemyRegistryMember2D target = null;

        if (requireTargetToFire)
        {
            if (!TryGetFarthest(out target) || target == null)
                return;
        }
        else
        {
            TryGetFarthest(out target);
        }

        StartCoroutine(FireBurst(target));
        cooldownTimer = Mathf.Max(0.01f, P.cooldown);
    }

    private System.Collections.IEnumerator FireBurst(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null)
        {
            // ✅ IEnumerator에서는 return; 금지 → yield break; 로 종료
            yield break;
        }

        _isBurstFiring = true;

        Vector2 origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)owner.position;
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        int count = Mathf.Max(1, P.projectileCount);
        float spread = Mathf.Max(0f, P.spreadAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i - (count - 1) * 0.5f);
            float ang = spread * t;
            Vector2 d = Quaternion.Euler(0f, 0f, ang) * dir;

            var proj = pool.Get<BoomerangProjectile2D>(origin, Quaternion.identity);
            proj.Init(owner, d, enemyMask, P.damage, P.projectileSpeed, Mathf.Max(0.1f, P.returnSpeed), P.maxDistance, P.lifeSeconds);

            float z = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0f, 0f, z);

            if (spinDegreesPerSecond != 0f)
            {
                if (!proj.TryGetComponent<BoomerangSpin2D>(out var spin))
                    spin = proj.gameObject.AddComponent<BoomerangSpin2D>();

                spin.SetSpin(spinDegreesPerSecond);
            }

            if (i < count - 1 && burstIntervalSeconds > 0f)
                yield return new WaitForSeconds(burstIntervalSeconds);
        }

        _isBurstFiring = false;
    }
}