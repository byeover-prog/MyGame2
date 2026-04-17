using UnityEngine;

/// <summary>
/// 기본 원거리 몬스터 구현체입니다.
///
/// 이 클래스의 역할:
/// - EnemyRangedBehaviorBase2D가 제공하는 공통 상태 전환 흐름을 사용합니다.
/// - 실제 투사체 생성 / 차징 프리뷰 / 발사만 담당합니다.
///
/// 즉,
/// - 공통 원거리 흐름은 Base
/// - 현재 기본 원거리형의 실제 공격 방식은 이 구현체
/// 로 역할을 분리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyRangedAttacker2D : EnemyRangedBehaviorBase2D
{
    [Header("5. 기본 원거리 발사 설정")]
    [SerializeField, Tooltip("발사할 투사체 프리팹입니다.\n"
                             + "EnemyProjectile2D가 붙어 있어야 합니다.")]
    private EnemyProjectile2D projectilePrefab;

    private EnemyProjectile2D chargingPreviewProjectile;

    protected override void Awake()
    {
        base.Awake();

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[EnemyRangedAttacker2D] Projectile Prefab이 비어 있습니다. 발사할 수 없습니다.", this);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        chargingPreviewProjectile = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        DestroyChargePreview(ref chargingPreviewProjectile);
    }

    public override void SetTarget(Transform newTarget)
    {
        base.SetTarget(newTarget);
    }

    protected override bool CanStartAttack()
    {
        return projectilePrefab != null;
    }

    protected override void BeginChargeVisual()
    {
        CreateChargePreview(ref chargingPreviewProjectile, projectilePrefab);
    }

    protected override void UpdateChargeVisualAim(Vector2 aimDirection)
    {
        UpdateChargePreviewAimDefault(chargingPreviewProjectile, aimDirection);
    }

    protected override void CancelChargeVisual()
    {
        DestroyChargePreview(ref chargingPreviewProjectile);
    }

    protected override void ExecuteAttack(Vector2 fireDirection)
    {
        FireProjectileBurst(projectilePrefab, fireDirection);
    }

    protected override void ExecuteChargedAttack(Vector2 fireDirection)
    {
        LaunchPreparedProjectileOrBurst(
            ref chargingPreviewProjectile,
            projectilePrefab,
            fireDirection);
    }
}