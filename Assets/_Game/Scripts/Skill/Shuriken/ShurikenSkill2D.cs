// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - "획득(Owned)" + "장착(Equipped)" 상태일 때만 쿨타임마다 발사한다.
// - 발사 순간 1회 가장 가까운 적을 찾고, 그 적 방향으로 수리검을 발사한다.
// - 레벨 규칙: (1) 튕김 기본 2, (2) 2~3레벨 투사체 수 증가, (3) 4~8레벨 튕김 증가(8은 6으로 캡).
[DisallowMultipleComponent]
public sealed class ShurikenSkill2D : MonoBehaviour, ISkill2D
{
    [Header("Prefab")]
    [Tooltip("수리검 투사체 프리팹(ShurikenProjectile2D가 붙어 있어야 함)")]
    [SerializeField] private ShurikenProjectile2D projectilePrefab;

    [Tooltip("발사 위치(비우면 자기 위치). 권장: Player/SpawnPoint")]
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
    [SerializeField] private int damage = 10;

    [Tooltip("투사체 속도")]
    [SerializeField] private float projectileSpeed = 10f;

    [Tooltip("유도 회전 속도(도/초)")]
    [SerializeField] private float turnSpeedDeg = 900f;

    [Tooltip("최대 생존 시간(초)")]
    [SerializeField] private float lifeTime = 6f;

    [Header("획득/장착(중요)")]
    [Tooltip("게임 시작 시 이미 획득한 상태로 둘지")]
    [SerializeField] private bool startOwned = false;

    [Tooltip("게임 시작 시 장착까지 할지(켜면 자동 발사됨). 보통은 false가 맞습니다.")]
    [SerializeField] private bool startEquipped = false;

    [Header("Level")]
    [Tooltip("현재 레벨(1~8)")]
    [Range(1, 8)]
    [SerializeField] private int level = 1;

    [Header("Debug")]
    [Tooltip("디버그: 재생 시작 시 강제 장착(테스트용). 출시/정식 흐름에서는 꺼두세요.")]
    [SerializeField] private bool debugForceEquipOnPlay = false;

    private bool _owned;
    private bool _equipped;
    private float _t;

    public bool IsOwned => _owned;
    public bool IsEquipped => _equipped;
    public int Level => level;

    private void Awake()
    {
        _owned = startOwned;
        _equipped = startEquipped;

        if (_equipped) _owned = true;
        if (debugForceEquipOnPlay)
        {
            _owned = true;
            _equipped = true;
        }
    }

    private void Update()
    {
        if (!_owned || !_equipped) return;
        if (Time.timeScale <= 0f) return;

        _t += Time.deltaTime;
        if (_t >= cooldown)
        {
            _t = 0f;
            TryFire();
        }
    }

    public void Grant(int startLevelValue, bool equip)
    {
        _owned = true;
        _equipped = equip;
        SetLevel(startLevelValue);
    }

    public void Upgrade(int delta)
    {
        if (!_owned)
        {
            Grant(1, true);
            return;
        }

        SetLevel(level + delta);
        _equipped = true;
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 8);
    }

    public void SetEquipped(bool equip)
    {
        if (!_owned && equip) _owned = true;
        _equipped = equip;
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

        float spread = count <= 1 ? 0f : 10f;

        // Init이 int 기반일 수 있어 명시 변환(호환 목적)
        int searchRadiusInt = Mathf.Max(0, Mathf.RoundToInt(searchRadius));
        int projectileSpeedInt = Mathf.Max(0, Mathf.RoundToInt(projectileSpeed));
        int turnSpeedDegInt = Mathf.Max(0, Mathf.RoundToInt(turnSpeedDeg));

        for (int i = 0; i < count; i++)
        {
            float offset = 0f;
            if (count == 2) offset = (i == 0) ? -spread * 0.5f : spread * 0.5f;
            else if (count == 3) offset = (i == 0) ? -spread : (i == 1 ? 0f : spread);

            Vector2 shotDir = Rotate(dir, offset);

            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            
            p.Init(enemyMask, searchRadiusInt, damage, projectileSpeedInt, shotDir, turnSpeedDegInt, bounce, lifeTime, target);
        }
    }

    private static int GetProjectileCount(int lv)
    {
        if (lv >= 3) return 3;
        if (lv == 2) return 2;
        return 1;
    }

    // 요청 규칙 반영: 8레벨 튕김=6으로 캡
    private static int GetBounceCount(int lv)
    {
        if (lv <= 3) return 2;
        return Mathf.Min(6, 2 + (lv - 3)); // 4~7에서 증가, 8은 6으로 고정
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
}