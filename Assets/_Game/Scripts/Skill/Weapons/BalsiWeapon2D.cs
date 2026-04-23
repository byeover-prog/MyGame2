using UnityEngine;

/// <summary>
/// 발시 무기: 가장 가까운 적을 향해 얼음 화살 1발 발사 (비관통)
///
/// [구조 — 낙뢰부(ThunderTalismanWeapon2D)와 통일]
/// 1. Update()에서 쿨타임 확인 → Fire()
/// 2. Fire()에서 ProjectilePool2D로 화살 투사체 생성
/// 3. 투사체가 적에게 닿으면 DamageUtil2D.TryApplyDamage(Ice) → 팝업 + 속성 VFX 자동
///
/// [설계 문서 기준]
/// - 기본 데미지: 30
/// - 비관통 (적 1명만 타격)
/// - 가장 가까운 적 타겟팅
/// - 속성: Ice (윤설 전용)
/// </summary>
[DisallowMultipleComponent]
public sealed class BalsiWeapon2D : CommonSkillWeapon2D
{
    [Header("발시 전용")]
    [Tooltip("화살 투사체 풀. Weapon_Balsi 프리팹 하위의 Pool 오브젝트를 연결하세요.")]
    [SerializeField] private ProjectilePool2D pool;

    [Tooltip("화살 발사 위치. 비우면 이 오브젝트의 Transform을 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
        if (pool == null) pool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        EnemyRegistryMember2D target = null;

        if (requireTargetToFire)
        {
            if (!TryGetNearest(out target) || target == null)
                return;
        }
        else
        {
            TryGetNearest(out target);
        }

        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null)
            ? (target.Position - origin).normalized
            : Vector2.right;

        var proj = pool.Get<BalsiProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(proj.gameObject);

        proj.Init(
            mask: enemyMask,
            dmg: StatDamage,
            spd: StatProjectileSpeed,
            life: StatProjectileLife,
            direction: dir
        );

        if (debugLog)
            GameLogger.Log($"[발시] 발사 | dmg={StatDamage} spd={StatProjectileSpeed} cd={P.cooldown:F2}");
    }
}