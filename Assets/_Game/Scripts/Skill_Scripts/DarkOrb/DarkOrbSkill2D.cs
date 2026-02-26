// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨타임마다 가장 가까운 적 방향으로 다크오브를 발사한다(적 없으면 발사 안 함).
// - 투사체는 "일정 거리 도달 시 폭발 + (레벨에 따라) 분열"을 반복한다.
[DisallowMultipleComponent]
public sealed class DarkOrbSkill2D : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("다크 오브 프리팹(DarkOrbProjectile2D 필요)")]
    [SerializeField] private DarkOrbProjectile2D projectilePrefab;

    [Tooltip("발사 위치(비우면 자기 위치)")]
    [SerializeField] private Transform firePoint;

    [Header("Target")]
    [Tooltip("적 레이어 마스크(Enemy 레이어)")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("탐색 반경")]
    [SerializeField] private float searchRadius = 12f;

    [Header("Stats")]
    [Tooltip("쿨타임(초)")]
    [SerializeField] private float cooldown = 4.0f;

    [Tooltip("폭발 데미지(레벨5~8에서 증가)")]
    [SerializeField] private float explosionDamage = 20f;

    [Tooltip("투사체 속도")]
    [SerializeField] private float speed = 6.5f;

    [Tooltip("폭발까지 이동 거리")]
    [SerializeField] private float travelDistance = 6.0f;

    [Tooltip("폭발 반경")]
    [SerializeField] private float explosionRadius = 1.6f;

    [Tooltip("분열 V자 각도(도)")]
    [SerializeField] private float splitAngleDeg = 35f;

    [Header("Level")]
    [Tooltip("현재 레벨(1~8)")]
    [Range(1, 8)]
    [SerializeField] private int level = 1;

    [Header("Debug")]
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

        if (!Targeting2D.TryGetClosestEnemy(origin, searchRadius, enemyMask, 0, out Transform target))
            return;

        Vector2 dir = ((Vector2)target.position - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        int maxSplitGen = GetMaxSplitGeneration(level);
        float dmg = GetExplosionDamage(level, explosionDamage);

        var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
        p.Init(enemyMask, dmg, speed, travelDistance, explosionRadius, splitAngleDeg, maxSplitGen, 0, dir);
    }

    private static int GetMaxSplitGeneration(int lv)
    {
        // 1:0 / 2:1 / 3:2 / 4+:3
        if (lv <= 1) return 0;
        if (lv == 2) return 1;
        if (lv == 3) return 2;
        return 3;
    }

    private static float GetExplosionDamage(int lv, float baseDamage)
    {
        // 5~8레벨 폭발 공격력 증가(프로토타입 기본값: 레벨당 +15%)
        if (lv <= 4) return baseDamage;

        int extra = lv - 4; // 5:1, 6:2, 7:3, 8:4
        float mul = 1f + 0.15f * extra;
        return baseDamage * mul;
    }
}