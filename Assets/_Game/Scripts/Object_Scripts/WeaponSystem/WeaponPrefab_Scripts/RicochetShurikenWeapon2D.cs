using UnityEngine;

// [구현 원리 요약]
// 수리검 무기의 발사기(Spawner) 역할입니다. 레벨업 시스템이 이 클래스(CommonSkillWeapon2D)를 찾아 실행합니다.
// 쿨타임마다 가장 가까운 적을 탐색하여 SO에 설정된 개수(projectileCount)만큼 투사체를 발사합니다.
[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("발사 설정")]
    [Tooltip("실제로 날아갈 수리검 투사체(Projectile) 프리팹을 여기에 넣으세요.")]
    [SerializeField] private GameObject shurikenProjectilePrefab;
    
    [Tooltip("적을 감지할 최대 사거리(반경)입니다.")]
    [SerializeField] private float detectRange = 10f;

    private float _timer;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f; // 스킬 획득 시 타이머 초기화
    }

    private void Update()
    {
        // 1. 발사기 본체는 항상 플레이어를 따라다닙니다. (물리 충돌 완전히 배제)
        if (owner == null || config == null) return;
        transform.position = owner.position;

        // 2. 인스펙터(SO)에서 설정한 쿨타임 가져오기
        _timer += Time.deltaTime;
        float cooldown = Mathf.Max(0.1f, P.hitInterval); 

        // 3. 쿨타임이 차면 발사!
        if (_timer >= cooldown)
        {
            _timer = 0f;
            FireShuriken();
        }
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        // 물리 엔진(Rigidbody) 충돌 대신, 수학적 원(OverlapCircle)을 그려 주변 적을 찾습니다.
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectRange, enemyMask);
        if (enemies.Length == 0) return; // 사거리 내에 적이 없으면 쏘지 않음

        // 가장 가까운 적 찾기 로직
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

        // 인스펙터(SO)에 적힌 '투사체 수(projectileCount)' 만큼 수리검 발사
        int count = Mathf.Max(1, P.projectileCount);
        for (int i = 0; i < count; i++)
        {
            GameObject shuriken = Instantiate(shurikenProjectilePrefab, transform.position, Quaternion.identity);
            
            // TODO: 생성된 투사체(shuriken)의 스크립트를 가져와서 targetEnemy 방향으로 날아가게 하고, P.damage(데미지)를 넘겨줍니다.
            // (투사체 이동 스크립트 리빌딩 시 이 부분을 연결하면 됩니다)
        }
    }
    
    // 유니티 씬 화면에서 적 감지 사거리를 붉은 선으로 미리 볼 수 있게 해줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}