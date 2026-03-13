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
            yield break;
        }

        _isBurstFiring = true;

        Vector2 origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)owner.position;
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        // ★ 타겟까지의 실제 거리 + 살짝 오버슈트 (설계: "대상이 있던 곳 보다 살짝 멀리 날라가고 돌아와야 한다")
        float targetDist = (target != null) ? Vector2.Distance(origin, target.Position) : 3f;
        float overshoot = 1.5f;
        float actualMaxDist = targetDist + overshoot;

        int count = Mathf.Max(1, P.projectileCount);
        float speed = Mathf.Max(0.5f, P.projectileSpeed);

        // ★ 복귀 속도: 나가는 속도의 1.2배 (빠르게 돌아오도록)
        float backSpeed = speed * 1.2f;

        // ★ 수명: 왕복에 충분한 시간 확보 (나가기 + 돌아오기 + 여유)
        float estimatedLife = (actualMaxDist / speed) + (actualMaxDist / backSpeed) + 1f;

        for (int i = 0; i < count; i++)
        {
            var proj = pool.Get<BoomerangProjectile2D>(origin, Quaternion.identity);
            proj.Init(owner, dir, enemyMask, P.damage, speed, backSpeed, actualMaxDist, estimatedLife);

            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
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