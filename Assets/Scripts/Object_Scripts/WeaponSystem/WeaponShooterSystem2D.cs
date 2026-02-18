using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기(SO) 기반 자동발사 시스템
/// - 슬롯 수 제한 없음(10개 이상 가능)
/// - 발사: 풀링
/// - 타겟: 슬롯별 + 중복타겟 방지 + 정책
/// - 저장(JSON): weaponId 기준으로 강화치 적용
/// 주의:
/// 1) 이 파일 안에 enum(WeaponUpgradeType)을 절대 넣지 마세요. (중복 정의 에러 원인)
/// 2) WeaponUpgradeType은 별도 파일 WeaponUpgradeType.cs 하나만 존재해야 합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponShooterSystem2D : MonoBehaviour
{
    [System.Serializable]
    public sealed class WeaponSlotRuntime
    {
        [Tooltip("장착된 무기 정의(SO)")]
        public WeaponDefinitionSO weapon;

        [Tooltip("슬롯 활성")]
        public bool enabled = true;

        [Header("강화치(런타임 적용값)")]
        public int bonusDamage = 0;          // JSON에서 로드해 적용
        public float cooldownMul = 1f;       // 1보다 작으면 더 빠름(발사 간격에 곱)
        public float rangeAdd = 0f;

        [Header("표시용 레벨")]
        [Tooltip("레벨업 카드 선택 횟수 기반. 표시/밸런스용. 기본 1")]
        public int level = 1;

        [Header("확장 업그레이드 누적(슬롯별)")]
        [Tooltip("카드 시스템(UpgradeValue) 누적치. 현재 발사 로직에 필요한 값은 위 3개에 즉시 반영됨.")]
        public WeaponSlotUpgradeState upgradeState;

        [HideInInspector] public float nextFireTime;
    }

    [Header("참조")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectilePool projectilePool;

    [Header("무기 슬롯(10개 이상 가능)")]
    [SerializeField] private List<WeaponSlotRuntime> slots = new List<WeaponSlotRuntime>(10);

    [Header("탐색 버퍼")]
    [SerializeField, Min(8)] private int overlapBufferSize = 64;

    [Header("보스 태그")]
    [SerializeField] private string bossTag = "Boss";

    private Collider2D[] _buffer;
    private readonly HashSet<int> _reservedTargetIds = new HashSet<int>();

    public IReadOnlyList<WeaponSlotRuntime> SlotsReadOnly => slots;

    private void Awake()
    {
        if (firePoint == null) firePoint = transform;

        if (projectilePool == null)
        {
            // 씬에 없으면 생성(데모 편의)
            var go = new GameObject("ProjectilePool");
            projectilePool = go.AddComponent<ProjectilePool>();
        }

        _buffer = new Collider2D[Mathf.Max(8, overlapBufferSize)];

        // 프리웜(선택): 장착된 무기들의 투사체를 미리 생성
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

            float interval = Mathf.Max(0.05f, slot.weapon.baseFireInterval * Mathf.Max(0.1f, slot.cooldownMul));
            if (now < slot.nextFireTime) continue;
            slot.nextFireTime = now + interval;

            float range = Mathf.Max(0.1f, slot.weapon.baseRange + slot.rangeAdd);
            int mask = slot.weapon.enemyLayer.value;
            if (mask == 0) continue;

            if (!TryPickTarget(origin, range, mask, slot.weapon.targetPolicy, slot.weapon.avoidDuplicateTargets, out var target))
                continue;

            Fire(slot, origin, target);
        }
    }

    private void Fire(WeaponSlotRuntime slot, Vector2 origin, Vector2 targetPos)
    {
        var weapon = slot.weapon;
        if (weapon == null || weapon.projectilePrefab == null) return;

        // 1) 발사 방향 계산
        Vector2 dir = targetPos - origin;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 2) 풀에서 꺼내기
        GameObject go = projectilePool.Get(weapon.projectilePrefab, origin, Quaternion.identity);
        if (go == null) return;

        // 3) 풀 key를 투사체에 주입(반납용)
        if (go.TryGetComponent(out StraightPooledProjectile2D straight))
            straight.SetOriginPrefab(weapon.projectilePrefab);

        // 4) 데미지 계산(슬롯 강화 반영)
        int damage = Mathf.Max(1, weapon.baseDamage + slot.bonusDamage);

        // 5) 발사
        if (go.TryGetComponent<IPooledProjectile2D>(out var pooledProj))
        {
            pooledProj.Launch(dir, damage, weapon.enemyLayer);
            return;
        }

        if (go.TryGetComponent(out Projectile2D projectile2D))
        {
            projectile2D.Launch(dir, damage, weapon.enemyLayer);
            return;
        }

        // 안전장치: 계약이 없으면 그냥 비활성
        go.SetActive(false);
    }

    private bool TryPickTarget(Vector2 origin, float range, int layerMask, TargetPolicy policy, bool avoidDup, out Vector2 targetPos)
    {
        targetPos = default;

        int count = Physics2D.OverlapCircleNonAlloc(origin, range, _buffer, layerMask);
        if (count <= 0) return false;

        Collider2D best = null;
        float bestScore = float.MaxValue;

        Collider2D bestBoss = null;
        float bestBossSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D c = _buffer[i];
            _buffer[i] = null;

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

    public bool ApplyUpgradeBySlotIndex(int slotIndex, WeaponUpgradeType type, int valueInt, float valueFloat, bool valueBool)
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
                // valueFloat는 배율(factor)로 들어옴 (예: 0.9 = 10% 쿨감)
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

    public bool ApplyUpgradeByWeaponId(string weaponId, WeaponUpgradeType type, int valueInt, float valueFloat, bool valueBool)
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

        // 1) 확장 누적
        WeaponUpgradeApplier2D.Apply(ref s.upgradeState, stat, value);

        // 2) 즉시 반영
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

        // 표시용 레벨은 "카드 선택 1회 = 레벨 1 증가"로 간주
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

    public bool TryGetSlotStat(int slotIndex, out int bonusDamage, out float cooldownMul, out float rangeAdd, out bool enabled)
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
