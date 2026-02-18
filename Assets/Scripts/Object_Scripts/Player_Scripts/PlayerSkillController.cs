using UnityEngine;

/// <summary>
/// 플레이어 스킬을 관리하는 컨트롤러
/// - 투사체 생성
/// - 스킬 파라미터 전달
/// - 이후 다른 스킬들이 여기에 추가됨
/// </summary>
public sealed class PlayerSkillController : MonoBehaviour
{
    [Header("호밍 투사체 설정")]
    [SerializeField] private HomingProjectile2D homingPrefab;
    [SerializeField] private Transform firePoint;

    [Header("호밍 파라미터")]
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float turnSpeedDeg = 360f;
    [SerializeField] private int damage = 10;
    [SerializeField] private int pierceCount = 0;

    [Header("적 레이어")]
    [SerializeField] private LayerMask enemyMask;

    /// <summary>
    /// 호밍 스킬 발사
    /// 실제 게임에서는 쿨타임/자동 발동 등에서 호출됨
    /// </summary>
    public void FireHoming()
    {
        if (homingPrefab == null || firePoint == null)
        {
            Debug.LogWarning("호밍 투사체 또는 발사 위치가 설정되지 않았습니다.");
            return;
        }

        // 투사체 생성
        HomingProjectile2D proj = Instantiate(
            homingPrefab,
            firePoint.position,
            Quaternion.identity
        );

        // 초기 방향 (오른쪽 기준)
        Vector2 initialDir = firePoint.right;

        // 투사체 초기화
        proj.Setup(
            owner: this,
            initialDir: initialDir,
            speed: projectileSpeed,
            turnSpeedDeg: turnSpeedDeg,
            damage: damage,
            pierce: pierceCount,
            enemyMask: enemyMask
        );

        // 이펙트 투명도 옵션 반영
        proj.ApplyVfxAlphaFromSettings();
    }
}
