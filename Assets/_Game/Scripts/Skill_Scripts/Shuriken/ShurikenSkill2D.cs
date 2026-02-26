// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨타임마다 "가장 가까운 적"을 기준으로 수리검을 발사한다(적 없으면 발사 안 함).
// - 레벨에 따라 수리검 개수/튕김 횟수를 규칙대로 계산한다.
// - 실제 튕김/타격은 ShurikenProjectile2D가 담당한다(SRP).
[DisallowMultipleComponent]
public sealed class ShurikenSkill2D : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("수리검 투사체 프리팹(ShurikenProjectile2D가 붙어 있어야 함)")]
    [SerializeField] private ShurikenProjectile2D projectilePrefab;

    [Tooltip("발사 위치(비우면 자기 위치)")]
    [SerializeField] private Transform firePoint;

    [Header("Target")]
    [Tooltip("적 레이어 마스크(Enemy 레이어를 넣어주세요)")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("타겟 탐색 반경")]
    [SerializeField] private float searchRadius = 12f;

    [Header("Stats(기본값, 나중에 JSON으로 덮어써도 됨)")]
    [Tooltip("쿨타임(초)")]
    [SerializeField] private float cooldown = 1.8f;

    [Tooltip("1회 타격 데미지")]
    [SerializeField] private float damage = 10f;

    [Tooltip("투사체 속도")]
    [SerializeField] private float projectileSpeed = 10f;

    [Tooltip("유도 회전 속도(도/초)")]
    [SerializeField] private float turnSpeedDeg = 900f;

    [Tooltip("최대 생존 시간(초)")]
    [SerializeField] private float lifeTime = 6f;

    [Header("Level")]
    [Tooltip("현재 레벨(1~8)")]
    [Range(1, 8)]
    [SerializeField] private int level = 1;

    [Header("Debug")]
    [Tooltip("플레이 시작 시 자동 발사")]
    [SerializeField] private bool autoFire = true;

    private float _t;

    private void Update()
    {
        if (!autoFire) return;
        if (Time.timeScale <= 0f) return;

        _t += Time.deltaTime;
        if (_t >= cooldown)
        {
            _t = 0f;
            TryFire();
        }
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 8);
    }

    private void TryFire()
    {
        if (projectilePrefab == null) return;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        // 발사 순간 1회 타겟 탐색
        if (!Targeting2D.TryGetClosestEnemy(origin, searchRadius, enemyMask, 0, out Transform target))
            return;

        int count = GetProjectileCount(level);
        int bounce = GetBounceCount(level);

        Vector2 dir = ((Vector2)target.position - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // 3개까지라서 간단 스프레드(겹침 완화)
        float spread = count <= 1 ? 0f : 10f;

        for (int i = 0; i < count; i++)
        {
            float offset = 0f;
            if (count == 2) offset = (i == 0) ? -spread * 0.5f : spread * 0.5f;
            else if (count == 3) offset = (i == 0) ? -spread : (i == 1 ? 0f : spread);

            Vector2 shotDir = Rotate(dir, offset);

            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.Init(enemyMask, searchRadius, damage, projectileSpeed, turnSpeedDeg, bounce, lifeTime, shotDir, target);
        }
    }

    private static int GetProjectileCount(int lv)
    {
        if (lv >= 3) return 3;   // 3레벨부터 3개 유지
        if (lv == 2) return 2;
        return 1;
    }

    private static int GetBounceCount(int lv)
    {
        // 기본 2, 4~8에서 +1씩, 단 6에서 캡(8레벨=6)
        int b = 2 + Mathf.Max(0, lv - 3);
        return Mathf.Min(6, b);
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
}