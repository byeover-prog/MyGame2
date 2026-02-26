// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨타임마다 "가장 가까운 적" 1회 탐색 후 발사(적 없으면 발사 안 함).
// - 레벨 N => 총 타격 횟수 N (보스 1마리면 같은 대상 반복 타격 가능).
// - 네임스페이스/타입 모호(ambiguous) 문제를 피하기 위해 프리팹은 GameObject로 받고,
//   Instantiate 후 GetComponent로 투사체 컴포넌트를 찾아 초기화한다.
[DisallowMultipleComponent]
public sealed class HomingMissileSkill2D : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("호밍 미사일 프리팹(GameObject)\n" +
             "- 프리팹 루트에 HomingMissileProjectile2D(스크립트)가 붙어 있어야 합니다.\n" +
             "- 타입 모호/네임스페이스 이슈를 피하려고 GameObject로 받습니다.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("발사 위치(비우면 자기 위치)\n권장: Player 하위 SpawnPoint(플레이어 콜라이더 바깥)")]
    [SerializeField] private Transform firePoint;

    [Header("Target")]
    [Tooltip("적 레이어 마스크(Enemy 레이어)")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("탐색 반경")]
    [SerializeField] private float searchRadius = 14f;

    [Header("Stats")]
    [Tooltip("쿨타임(초)")]
    [SerializeField] private float cooldown = 3.0f;

    [Tooltip("1회 타격 데미지")]
    [SerializeField] private float damage = 8f;

    [Tooltip("속도")]
    [SerializeField] private float speed = 8.5f;

    [Tooltip("회전 속도(도/초)")]
    [SerializeField] private float turnSpeedDeg = 1080f;

    [Tooltip("최대 생존 시간(초)")]
    [SerializeField] private float lifeTime = 8f;

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

        // 발사 순간 1회 타겟 탐색
        if (!Targeting2D.TryGetClosestEnemy(origin, searchRadius, enemyMask, 0, out Transform target))
            return;

        Vector2 dir = ((Vector2)target.position - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        int totalHits = level; // 레벨=총 타격 횟수

        // Instantiate는 GameObject로 처리(제네릭 제약/타입 모호 회피)
        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.identity);

        // 프리팹 루트(또는 자식)에 투사체 스크립트가 붙어 있어야 함
        var projectile = go.GetComponent<HomingMissileProjectile2D>();
        if (projectile == null)
        {
            // 자식에 붙어있는 경우까지 허용(프리팹 구조가 달라도 안전)
            projectile = go.GetComponentInChildren<HomingMissileProjectile2D>();
        }

        if (projectile == null)
        {
            Debug.LogError("[HomingMissileSkill2D] 프리팹에 HomingMissileProjectile2D 컴포넌트가 없습니다.", go);
            Destroy(go);
            return;
        }

        projectile.Init(enemyMask, searchRadius, damage, speed, turnSpeedDeg, totalHits, lifeTime, dir, target);
    }
}