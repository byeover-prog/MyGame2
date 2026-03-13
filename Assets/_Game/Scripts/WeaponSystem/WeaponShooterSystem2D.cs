using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// [요약] 무기 시스템에서 투사체를 생성하고 타겟을 향해 발사 명령을 내리는 핵심 로직
public sealed class WeaponShooterSystem2D : MonoBehaviour
{
    [System.Serializable]
    public sealed class WeaponSlotRuntime
    {
        [Tooltip("장착된 무기 정의(SO) 데이터입니다.")] 
        public WeaponDefinitionSO weapon;

        [Tooltip("현재 슬롯의 활성화 여부입니다.")] 
        public bool enabled = true;
        
        [Header("설정")]
        [Tooltip("플레이어의 최종 스탯을 참조하여 데미지/범위를 계산합니다.")] 
        public WeaponFinalStats2D finalStats;

        [Header("강화치(런타임 적용값)")] 
        [Tooltip("게임 진행 중 추가되는 보너스 공격력입니다.")]
        public int bonusDamage = 0; 
        
        [Tooltip("쿨타임 감소 배율 (1보다 작으면 더 빠르게 발사됩니다).")]
        public float cooldownMul = 1f; 
        
        [Tooltip("추가 사거리(발동 범위) 수치입니다.")]
        public float rangeAdd = 0f;

        [Header("표시용 레벨")] 
        [Tooltip("레벨업 카드 선택 횟수를 기반으로 한 표시/밸런스용 레벨 (기본값 1).")]
        public int level = 1;

        [Header("확장 업그레이드 누적(슬롯별)")] 
        [Tooltip("카드 시스템을 통해 누적된 업그레이드 수치들입니다.")]
        public WeaponSlotUpgradeState upgradeState;

        [HideInInspector] public float nextFireTime;
    }

    [Header("참조")] 
    [SerializeField] [Tooltip("발사체가 생성되어 날아갈 시작 위치입니다.")] 
    private Transform firePoint;
    
    [SerializeField] [Tooltip("발사체를 최적화하여 관리하는 풀링 시스템입니다.")] 
    private ProjectilePool projectilePool;

    [Header("시작 로드아웃(선택)")] 
    [SerializeField] [Tooltip("슬롯이 비어있을 때 최초 1회 장착될 기본 무기 목록입니다.")] 
    private List<WeaponDefinitionSO> defaultStartWeapons = new List<WeaponDefinitionSO>(1);

    [Header("무기 슬롯(10개 이상 가능)")] 
    [SerializeField] [Tooltip("현재 플레이어가 장착 중인 무기 슬롯들의 목록입니다.")] 
    private List<WeaponSlotRuntime> slots = new List<WeaponSlotRuntime>(10);

    [Header("탐색 버퍼")] 
    [SerializeField, Min(8)] [Tooltip("적을 탐색할 때 사용할 최대 버퍼 크기입니다.")]
    private int overlapBufferSize = 64;

    [Header("보스 태그")] 
    [SerializeField] [Tooltip("보스 몬스터를 우선 타겟팅하기 위한 태그 이름입니다.")] 
    private string bossTag = "Boss";
    
    [Header("플레이어 능력치")]
    [SerializeField] [Tooltip("플레이어의 전투 기본 스탯을 담당하는 컴포넌트입니다.")] 
    private PlayerCombatStats2D stats;

    private Collider2D[] _buffer;
    private readonly HashSet<int> _reservedTargetIds = new HashSet<int>();
    
    // 클래스 필드로 추가(버퍼 재사용을 통해 메모리 할당 방지)
    private readonly List<Collider2D> _overlapList = new List<Collider2D>(64);

    public IReadOnlyList<WeaponSlotRuntime> SlotsReadOnly => slots;

    private void Awake()
    {
        if (firePoint == null) firePoint = transform;
        
        if (stats == null) stats = GetComponentInParent<PlayerCombatStats2D>();

        if (projectilePool == null)
        {
            // 씬에 없으면 생성(데모/테스트 편의용)
            var go = new GameObject("ProjectilePool");
            projectilePool = go.AddComponent<ProjectilePool>();
        }

        _buffer = new Collider2D[Mathf.Max(8, overlapBufferSize)];

        // 0) slots가 비어있으면 시작 로드아웃을 1회 주입
        EnsureDefaultLoadoutIfEmpty();

        // 1) 프리웜(선택): 장착된 무기들의 투사체를 미리 생성해 최적화
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.weapon == null || s.weapon.projectilePrefab == null) continue;

            projectilePool.Prewarm(s.weapon.projectilePrefab, 20);
            s.nextFireTime = Time.time + 0.1f + i * 0.02f;

            if (s.upgradeState == null) s.upgradeState = new WeaponSlotUpgradeState();
            if (s.level < 1) s.level = 1;
        }
    }

    /// <summary>
    /// slots가 비어있을 때만 defaultStartWeapons로 초기 장착(1회).
    /// </summary>
    public void EnsureDefaultLoadoutIfEmpty()
    {
        if (slots == null) slots = new List<WeaponSlotRuntime>(10);
        if (slots.Count > 0) return;

        if (defaultStartWeapons == null || defaultStartWeapons.Count == 0) return;

        for (int i = 0; i < defaultStartWeapons.Count; i++)
        {
            var w = defaultStartWeapons[i];
            if (w == null) continue;

            AddSlot(
                weapon: w,
                enabled: true,
                bonusDamage: 0,
                cooldownMul: 1f,
                rangeAdd: 0f
            );
        }
    }

    private void Update()
    {
        _reservedTargetIds.Clear();

        float now = Time.time;
        Vector2 origin = firePoint.position;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || !slot.enabled) continue;
            if (slot.weapon == null || slot.weapon.projectilePrefab == null) continue;

            // 1) 슬롯별 발사 간격 계산(무기 기본 * 슬롯 강화 * 패시브 쿨감)
            float slotInterval = Mathf.Max(0.05f, slot.weapon.baseFireInterval * Mathf.Max(0.1f, slot.cooldownMul));
            if (stats != null) slotInterval *= stats.CooldownMul;

            if (now < slot.nextFireTime) continue;
            slot.nextFireTime = now + slotInterval;

            // 2) 슬롯별 사거리 계산(무기 기본 + 슬롯 강화 + 패시브 범위)
            float slotRange = Mathf.Max(0.1f, slot.weapon.baseRange + slot.rangeAdd);
            if (stats != null) slotRange *= stats.AreaMul;

            int mask = slot.weapon.enemyLayer.value;
            if (mask == 0) continue;

            // 3) 타겟 선택
            if (!TryPickTarget(
                    origin,
                    slotRange,
                    mask,
                    slot.weapon.targetPolicy,
                    slot.weapon.avoidDuplicateTargets,
                    out var target))
                continue;

            // 4) 발사 로직 호출
            Fire(slot, origin, target);
        }
    }

    private void Fire(WeaponSlotRuntime slot, Vector2 origin, Vector2 targetPos)
    {
        var weapon = slot.weapon;
        if (weapon == null || weapon.projectilePrefab == null) return;

        Vector2 dir = targetPos - origin;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        int damage = Mathf.Max(1, weapon.baseDamage + slot.bonusDamage);
        if (stats != null) damage = Mathf.Max(1, Mathf.RoundToInt(damage * stats.DamageMul));

        // 업그레이드(멀티샷) 반영: 기본 1 + addShotCount
        int extraShots = 0;
        if (slot.upgradeState != null) extraShots = Mathf.Max(0, slot.upgradeState.addShotCount);
        int shotCount = 1 + extraShots;

        // 투사체 분산 각도 제한
        shotCount = Mathf.Clamp(shotCount, 1, 12);
        float totalSpreadDeg = (shotCount <= 1) ? 0f : Mathf.Min(30f, 6f * (shotCount - 1));
        float half = totalSpreadDeg * 0.5f;

        for (int s = 0; s < shotCount; s++)
        {
            float t = (shotCount == 1) ? 0.5f : (s / (float)(shotCount - 1));
            float angleDeg = Mathf.Lerp(-half, half, t);

            Vector2 shotDir = Rotate(dir, angleDeg);

            // 방향 회전: 스프라이트가 +X를 전방으로 쓰는 전제
            Quaternion rot = Quaternion.FromToRotation(Vector3.right, new Vector3(shotDir.x, shotDir.y, 0f));

            GameObject go = projectilePool.Get(weapon.projectilePrefab, origin, rot);
            if (go == null) continue;

            if (go.TryGetComponent(out StraightPooledProjectile2D straight))
                straight.SetOriginPrefab(weapon.projectilePrefab);

            // [수정된 발사 계약(Contract) 체결 로직]
            
            // 1순위: 공통 인터페이스가 있다면 최우선 적용 (리짓바디 off 상태여도 이걸로 이동)
            if (go.TryGetComponent<IProjectile2D>(out var interfaceProj))
            {
                interfaceProj.Launch(shotDir, damage, weapon.enemyLayer);
                continue;
            }

            // 2순위: 풀링 전용 인터페이스 적용
            if (go.TryGetComponent<IPooledProjectile2D>(out var pooledProj))
            {
                pooledProj.Launch(shotDir, damage, weapon.enemyLayer);
                continue;
            }

            // 구버전 스크립트(Projectile2D) 탐색 로직 제거 완료.

            // 3순위: 스크립트 없이 리짓바디만 있는 경우 (물리적 발사)
            if (go.TryGetComponent(out Rigidbody2D rb))
            {
                // Body Type이 Kinematic이면 Velocity로 안 움직일 수 있으므로 동적인지 확인
                if (rb.bodyType == RigidbodyType2D.Dynamic || rb.bodyType == RigidbodyType2D.Kinematic)
                {
                    float speed = 14f;
                    rb.linearVelocity = shotDir * speed;
                }
                continue;
            }

            // 위 조건에 모두 해당하지 않으면 플레이어 몸에 박혀있게 됨. (명확한 경고 출력)
            Debug.LogWarning($"[경고] '{go.name}' 프리팹에 발사 로직(IProjectile2D)이나 Rigidbody2D가 없습니다! 프리팹 인스펙터를 확인하세요.", go);
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    private bool TryPickTarget(Vector2 origin, float range, int layerMask, TargetPolicy policy, bool avoidDup,
        out Vector2 targetPos)
    {
        targetPos = default;
        _overlapList.Clear();

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = layerMask,
            useTriggers = true
        };

        Physics2D.OverlapCircle(origin, range, filter, _overlapList);

        int count = _overlapList.Count;
        if (count <= 0) return false;

        Collider2D best = null;
        float bestScore = float.MaxValue;

        Collider2D bestBoss = null;
        float bestBossSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D c = _overlapList[i];
            if (c == null) continue;

            if (avoidDup && _reservedTargetIds.Contains(c.GetInstanceID()))
                continue;

            if (!c.TryGetComponent(out EnemyHealth2D hp)) continue;
            if (hp.IsDead) continue;

            Vector2 pos = c.transform.position;
            float sqr = (pos - origin).sqrMagnitude;

            if (policy == TargetPolicy.BossFirst && c.CompareTag(bossTag))
            {
                if (sqr < bestBossSqr)
                {
                    bestBossSqr = sqr;
                    bestBoss = c;
                }
                continue;
            }

            float score = ComputeScore(policy, sqr, hp);
            if (score < bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        Collider2D chosen = (policy == TargetPolicy.BossFirst && bestBoss != null) ? bestBoss : best;
        if (chosen == null) return false;

        if (avoidDup) _reservedTargetIds.Add(chosen.GetInstanceID());
        targetPos = chosen.transform.position;
        return true;
    }

    private static float ComputeScore(TargetPolicy policy, float distanceSqr, EnemyHealth2D hp)
    {
        switch (policy)
        {
            case TargetPolicy.Nearest: return distanceSqr;
            case TargetPolicy.LowestHp: return hp.CurrentHp;
            case TargetPolicy.LowestHpRatio:
                if (hp.MaxHp > 0) return (float)hp.CurrentHp / hp.MaxHp;
                return distanceSqr;
            case TargetPolicy.BossFirst: return distanceSqr;
            default: return distanceSqr;
        }
    }

    // ============================
    // 기존 시스템 호환 API (PlayerSkillUpgradeSystem / WeaponLoadApplier2D)
    // ============================

    public void ClearSlots()
    {
        if (slots == null) slots = new List<WeaponSlotRuntime>(10);
        slots.Clear();
    }

    public void AddSlot(WeaponDefinitionSO weapon, bool enabled, int bonusDamage, float cooldownMul, float rangeAdd)
    {
        if (weapon == null) return;
        if (slots == null) slots = new List<WeaponSlotRuntime>(10);

        var slot = new WeaponSlotRuntime
        {
            weapon = weapon,
            enabled = enabled,
            bonusDamage = Mathf.Max(0, bonusDamage),
            cooldownMul = Mathf.Clamp(cooldownMul, 0.1f, 10f),
            rangeAdd = rangeAdd,
            level = 1,
            nextFireTime = Time.time + 0.1f,
            upgradeState = new WeaponSlotUpgradeState()
        };

        slots.Add(slot);

        if (projectilePool != null && weapon.projectilePrefab != null)
            projectilePool.Prewarm(weapon.projectilePrefab, 20);
    }

    public bool ApplyUpgradeBySlotIndex(int slotIndex, WeaponUpgradeType type, int valueInt, float valueFloat,
        bool valueBool)
    {
        if (slots == null) return false;
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;

        var s = slots[slotIndex];
        if (s == null || s.weapon == null) return false;

        switch (type)
        {
            case WeaponUpgradeType.DamageAdd:
                s.bonusDamage = Mathf.Max(0, s.bonusDamage + valueInt);
                return true;

            case WeaponUpgradeType.CooldownMul:
                if (valueFloat > 0f)
                    s.cooldownMul = Mathf.Clamp(s.cooldownMul * valueFloat, 0.1f, 10f);
                return true;

            case WeaponUpgradeType.RangeAdd:
                s.rangeAdd += valueFloat;
                return true;

            case WeaponUpgradeType.ToggleEnabled:
                s.enabled = valueBool;
                return true;
        }

        return false;
    }

    public bool ApplyUpgradeByWeaponId(string weaponId, WeaponUpgradeType type, int valueInt, float valueFloat,
        bool valueBool)
    {
        if (string.IsNullOrWhiteSpace(weaponId)) return false;
        if (slots == null) return false;

        bool applied = false;

        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.weapon == null) continue;
            if (s.weapon.weaponId != weaponId) continue;

            applied = true;

            switch (type)
            {
                case WeaponUpgradeType.DamageAdd:
                    s.bonusDamage = Mathf.Max(0, s.bonusDamage + valueInt);
                    break;

                case WeaponUpgradeType.CooldownMul:
                    if (valueFloat > 0f)
                        s.cooldownMul = Mathf.Clamp(s.cooldownMul * valueFloat, 0.1f, 10f);
                    break;

                case WeaponUpgradeType.RangeAdd:
                    s.rangeAdd += valueFloat;
                    break;

                case WeaponUpgradeType.ToggleEnabled:
                    s.enabled = valueBool;
                    break;
            }
        }

        return applied;
    }

    // ============================
    // 새 카드 시스템용 API (UpgradeValue)
    // ============================

    public bool ApplyUpgradeToSlot(int slotIndex, WeaponUpgradeStat2D stat, UpgradeValue value)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;

        var s = slots[slotIndex];
        if (s == null || s.weapon == null) return false;

        if (s.upgradeState == null) s.upgradeState = new WeaponSlotUpgradeState();

        WeaponUpgradeApplier2D.Apply(ref s.upgradeState, stat, value);

        int addInt = UpgradeValueCompat.GetAddInt(value);
        float addFloat = UpgradeValueCompat.GetAddFloat(value);
        float mulFloat = UpgradeValueCompat.GetMulFloat(value);

        switch (stat)
        {
            case WeaponUpgradeStat2D.Damage:
                s.bonusDamage = Mathf.Max(0, s.bonusDamage + addInt);
                break;

            case WeaponUpgradeStat2D.Range:
                s.rangeAdd += addFloat;
                break;

            case WeaponUpgradeStat2D.FireRateMul:
            case WeaponUpgradeStat2D.FireIntervalMul:
                if (mulFloat > 0f)
                    s.cooldownMul = Mathf.Clamp(s.cooldownMul * mulFloat, 0.1f, 10f);
                break;
        }

        s.level = Mathf.Max(1, s.level + 1);

        return true;
    }

    // ============================
    // 조회 API
    // ============================

    public bool TryGetSlotByWeaponId(string weaponId, out int slotIndex)
    {
        slotIndex = -1;
        if (string.IsNullOrWhiteSpace(weaponId)) return false;
        if (slots == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.weapon == null) continue;
            if (s.weapon.weaponId == weaponId)
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    public bool TryGetSlotStat(int slotIndex, out int bonusDamage, out float cooldownMul, out float rangeAdd,
        out bool enabled)
    {
        bonusDamage = 0;
        cooldownMul = 1f;
        rangeAdd = 0f;
        enabled = false;

        if (slots == null) return false;
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;

        var s = slots[slotIndex];
        if (s == null || s.weapon == null) return false;

        bonusDamage = s.bonusDamage;
        cooldownMul = s.cooldownMul;
        rangeAdd = s.rangeAdd;
        enabled = s.enabled;
        return true;
    }

    public bool TryGetSlotLevel(int slotIndex, out int level)
    {
        level = 1;

        if (slots == null) return false;
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;

        var s = slots[slotIndex];
        if (s == null || s.weapon == null) return false;

        level = Mathf.Max(1, s.level);
        return true;
    }

    public WeaponSaveData BuildSaveData()
    {
        var data = new WeaponSaveData();

        if (slots == null) return data;

        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.weapon == null) continue;

            data.slots.Add(new WeaponSlotSave
            {
                weaponId = s.weapon.weaponId,
                enabled = s.enabled,
                bonusDamage = s.bonusDamage,
                cooldownMul = s.cooldownMul,
                rangeAdd = s.rangeAdd
            });
        }

        return data;
    }
}