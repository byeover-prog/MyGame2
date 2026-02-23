using UnityEngine;

// [구현 원리 요약]
// 더 이상 빈 껍데기가 아닙니다! 타겟을 찾은 뒤, 투사체를 생성하고 직접 Launch() 명령을 내려 발사합니다.
[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("발사 설정")]
    [Tooltip("날아갈 수리검 투사체 프리팹 (StraightProjectile2D가 붙어있어야 함)")]
    [SerializeField] private GameObject shurikenProjectilePrefab;
    
    [Tooltip("적을 감지할 최대 사거리(반경)")]
    [SerializeField] private float detectRange = 15f;

    private float _timer;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f;
    }

    private void Update()
    {
        if (owner == null || config == null) return;
        transform.position = owner.position; // 플레이어 위치 따라가기

        _timer += Time.deltaTime;
        float cooldown = Mathf.Max(0.1f, P.hitInterval); 

        if (_timer >= cooldown)
        {
            _timer = 0f;
            FireShuriken();
        }
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        // 1. 적 탐색
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectRange, enemyMask);
        if (enemies.Length == 0) return;

        // 가장 가까운 적 찾기
        Transform targetEnemy = enemies[0].transform;
        float minDistance = Vector2.Distance(transform.position, targetEnemy.position);

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                targetEnemy = enemy.transform;
            }
        }

        // 2. 발사 방향 계산
        Vector2 origin = transform.position;
        Vector2 dir = ((Vector2)targetEnemy.position - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // 3. 투사체 생성 및 "진짜 발사(Launch)" 명령 내리기
        int count = Mathf.Max(1, P.projectileCount);
        for (int i = 0; i < count; i++)
        {
            // (차후 ObjectPool로 교체하면 성능이 더 좋아집니다. 우선은 Instantiate로 즉시 작동하게 둡니다)
            GameObject go = Instantiate(shurikenProjectilePrefab, origin, Quaternion.identity);
            
            // 투사체에 붙어있는 ProjectileBase2D (또는 StraightProjectile2D) 가져오기
            if (go.TryGetComponent<ProjectileBase2D>(out var proj))
            {
                // 이전 대화에서 물리 엔진을 끈 투사체의 Launch를 호출!
                proj.Launch(dir, P.damage, P.projectileSpeed, P.lifeSeconds, enemyMask, owner);
            }
            else
            {
                Debug.LogWarning("투사체 프리팹에 ProjectileBase2D 스크립트가 없습니다!");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}