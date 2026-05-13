using UnityEngine;

[DisallowMultipleComponent]
public sealed class WolchamWeapon2D : CharacterSkillWeaponBase
{
    [Header("월참 설정")]
    [Tooltip("WolchamCrescent2D 투사체 풀입니다.")]
    [SerializeField] private ProjectilePool2D crescentPool;

    [Tooltip("발사 시작점입니다. 비우면 owner 위치 사용.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("자동 조준 시 적 탐색 최대 반경입니다.")]
    [SerializeField] private float autoAimRadius = 15f;

    [Tooltip("검기 이동 속도(유닛/초)입니다.")]
    [SerializeField] private float projectileSpeed = 14f;

    [Tooltip("검기 최대 비행 시간(초)입니다.")]
    [SerializeField] private float projectileLifetime = 1.2f;

    [Tooltip("발사 시작 시 플레이어 위치에서의 오프셋(유닛). 캐릭터 몸 안에서 시작 방지.")]
    [SerializeField] private float spawnOffset = 0.5f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.15 = +15%.")]
    [SerializeField] private float damagePerLevel = 0.15f;

    [Header("각성 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 십자 4방향 발사 활성화.")]
    [SerializeField] private bool awakeningCrossFire = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    protected override void Awake()
    {
        base.Awake();

        element = DamageElement2D.Dark;
        baseDamage = 15;
        baseCooldown = 3.0f;
        balanceId = "weapon_wolcham";

        if (crescentPool == null)
            crescentPool = GetComponentInChildren<ProjectilePool2D>(true);

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        if (owner == null) return;
        if (crescentPool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Fire();
        cooldownTimer = ScaleCooldown(GetBalanceCooldown(), 0.1f);
    }

    private void Fire()
    {
        Vector3 ownerPos = owner.position;
        int finalDamage = GetFinalDamage();

        if (IsAwakened() && awakeningCrossFire)
        {
            // 각성: 십자 4방향 발사
            Vector2[] dirs = AimInputProvider.GetCrossDirections(ownerPos, enemyMask, autoAimRadius);
            for (int i = 0; i < dirs.Length; i++)
                FireOne(ownerPos, dirs[i], finalDamage);

            if (debugLog)
                CombatLog.Log($"[월참 각성] 십자 4방향 발사! dmg={finalDamage}", this);
        }
        else
        {
            // 일반: 1방향 발사
            Vector2 dir = AimInputProvider.GetAimDirection(ownerPos, enemyMask, autoAimRadius);
            FireOne(ownerPos, dir, finalDamage);

            if (debugLog)
                CombatLog.Log($"[월참] 발사! dmg={finalDamage} dir={dir}", this);
        }
    }

    private void FireOne(Vector3 ownerPos, Vector2 direction, int damage)
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : ownerPos;
        Vector3 spawnPos = origin + (Vector3)(direction * spawnOffset);

        // 회전: 검기가 진행 방향을 향하도록
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        var crescent = crescentPool.Get<WolchamCrescent2D>(spawnPos, rot);
        if (crescent == null) return;

        crescent.Initialize(
            damage: damage,
            direction: direction,
            speed: projectileSpeed,
            lifetime: projectileLifetime,
            element: element);
    }

    // 계산 헬퍼

    private int GetFinalDamage()
    {
        int finalBase = GetBalanceDamage();
        float levelScale = 1f + damagePerLevel * Mathf.Max(0, level - 1);
        return ScaleDamage(finalBase * levelScale);
    }

    private bool IsAwakened()
    {
        return level >= awakeningLevel;
    }
}