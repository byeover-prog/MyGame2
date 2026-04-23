// UTF-8
// ============================================================================
// OrbitingBladeWeapon2D.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/SkillSystem/CommonSkill/OrbitingBladeWeapon2D.cs
//
// [해결한 버그 4개]
// 1. LevelableSkillMarker2D가 OnAttached 미포워딩 → owner=null → Update 즉시 return
//    → Update에서 owner==null이면 자동 탐색 + 자체 Initialize
// 2. P.orbitRadius=0 (SO/JSON에 없으면 ScalePositive(0)=0)
//    → Inspector 폴백값(2.0) 사용
// 3. ON/OFF 사이클 기본값 1.5초 → 기획서 4초
// 4. ProjectileVFXChild는 1회성 투사체용 → 회전검에 부적합
//    → 블레이드별 VFX 직접 관리 (Instantiate + 활성/비활성)
//
// [프리팹 구조 — 이렇게 만들어야 함]
// Weapon_RotateSword (이 스크립트 + LevelableSkillMarker2D)
//   └─ Blade_Template (BoxCollider2D isTrigger)
//       └─ VisualRoot (SpriteRenderer, Scale 0.2)
//
// [Inspector 설정]
// config              → CS_SpinSowrd
// Enemy Mask          → Enemy
// Blade Template      → Blade_Template (자식 GameObject)
// Vfx Prefab          → eff_weapon_orbitsward (파티클 프리팹)
// Active Seconds      → 4
// Cooldown Seconds    → 4
// Orbit Radius        → 2
// Orbit Angular Speed → 180
//
// ★ ProjectileVFXChild 컴포넌트는 루트에서 제거할 것!
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드")]
    [SerializeField, Tooltip("회전검 원본 오브젝트 (자식 GameObject)")]
    private GameObject bladeTemplate;

    [SerializeField, Min(0.05f), Tooltip("타격 판정 반경")]
    private float hitRadius = 1.0f;

    [Header("VFX")]
    [SerializeField, Tooltip("블레이드 VFX 프리팹 (eff_weapon_orbitsward 등). 없으면 VFX 없이 동작.")]
    private GameObject vfxPrefab;

    [Header("비주얼")]
    [SerializeField, Tooltip("검이 바깥을 바라보도록 회전")]
    private bool rotateBladeToFaceOutward = true;

    [SerializeField, Tooltip("스프라이트 방향 보정(도)")]
    private float bladeVisualRotationOffsetDeg = 0f;

    [Header("ON/OFF 사이클")]
    [SerializeField, Tooltip("ON/OFF 사이클 사용")]
    private bool useCooldownCycle = true;

    [SerializeField, Min(0.1f), Tooltip("활성 지속시간(초). 기획서=4초")]
    private float activeSeconds = 4.0f;

    [SerializeField, Min(0.1f), Tooltip("비활성(쿨다운) 시간(초). 기획서=4초")]
    private float cooldownSeconds = 4.0f;

    [Header("궤도 (SO/JSON에 값이 없을 때 폴백)")]
    [SerializeField, Min(0.5f), Tooltip("회전 반지름 폴백값")]
    private float orbitRadius = 2.0f;

    [SerializeField, Min(30f), Tooltip("회전 속도(도/초) 폴백값")]
    private float orbitAngularSpeed = 180f;

    [Header("타격 제한")]
    [SerializeField, Min(0.01f), Tooltip("같은 적 재타격 최소 간격(초)")]
    private float minHitInterval = 0.20f;

    [SerializeField, Min(1), Tooltip("1타 데미지 상한")]
    private int maxDamagePerHit = 999;

    // ── 런타임 ──
    private readonly List<GameObject> blades = new List<GameObject>(16);
    private readonly List<GameObject> bladeVfx = new List<GameObject>(16); // ★ 블레이드별 VFX
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);
    private readonly Collider2D[] hitResults = new Collider2D[10];

    private float baseAngle;
    private bool _isActive = true;
    private float _stateTimer;
    private int _currentCycleCount;

    private float _radius;
    private float _angSpeed;
    private float _hitIvl;
    private int _dmg;

    private bool _selfInitialized;

    // ══════════════════════════════════════════════════════════════
    // Awake — 직렬화 오염 방지
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (orbitRadius < 0.5f) orbitRadius = 2.0f;
        if (orbitAngularSpeed < 30f) orbitAngularSpeed = 180f;
        if (activeSeconds < 0.1f) activeSeconds = 4.0f;
        if (cooldownSeconds < 0.1f) cooldownSeconds = 4.0f;

        _radius = orbitRadius;
        _angSpeed = orbitAngularSpeed;
    }

    // ══════════════════════════════════════════════════════════════
    // Initialize
    // ══════════════════════════════════════════════════════════════

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        cooldownTimer = 0f;
        DoSetup();
        _selfInitialized = true;

        GameLogger.Log($"<color=#FFD700>[OrbitingBlade] Initialize</color>" +
                  $" owner={ownerTr?.name} blades={_currentCycleCount}" +
                  $" radius={_radius:F2} angSpd={_angSpeed:F0}" +
                  $" vfx={(vfxPrefab != null ? vfxPrefab.name : "없음")}", this);
    }

    // ══════════════════════════════════════════════════════════════
    // 자체 초기화 (OnAttached 미포워딩 대비)
    // ══════════════════════════════════════════════════════════════

    private bool TrySelfInitialize()
    {
        if (owner != null) return true;

        Transform found = null;
        var stats = GetComponentInParent<PlayerCombatStats2D>();
        if (stats != null) found = stats.transform;

        if (found == null)
        {
            var playerStats = FindFirstObjectByType<PlayerCombatStats2D>();
            if (playerStats != null) found = playerStats.transform;
        }

        if (found == null) return false;

        GameLogger.Log($"<color=orange>[OrbitingBlade] 자체 초기화: owner={found.name}</color>", this);
        Initialize(config, found, Mathf.Max(1, level));
        return true;
    }

    // ══════════════════════════════════════════════════════════════
    // 레벨 변경
    // ══════════════════════════════════════════════════════════════

    protected override void OnLevelChanged()
    {
        EnsureBladeInstances();

        if (!useCooldownCycle)
        {
            _currentCycleCount = Mathf.Max(1, P.projectileCount);
            CacheParams();
            PositionAndActivateBlades(_currentCycleCount);
        }

        lastHitTime.Clear();
    }

    // ══════════════════════════════════════════════════════════════
    // Update
    // ══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (owner == null)
        {
            if (!TrySelfInitialize()) return;
        }
        if (config == null) return;

        if (!_selfInitialized)
        {
            DoSetup();
            _selfInitialized = true;
        }

        cooldownTimer = 0f;

        // ON/OFF 사이클
        if (useCooldownCycle)
        {
            _stateTimer += Time.deltaTime;

            if (_isActive)
            {
                if (_stateTimer >= activeSeconds)
                {
                    _isActive = false;
                    _stateTimer = 0f;
                    SetBladesVisible(false);
                    lastHitTime.Clear();
                    return;
                }
            }
            else
            {
                if (_stateTimer >= cooldownSeconds)
                {
                    _isActive = true;
                    _stateTimer = 0f;
                    _currentCycleCount = Mathf.Max(1, P.projectileCount);
                    CacheParams();
                    PositionAndActivateBlades(_currentCycleCount);
                }
                else
                {
                    return;
                }
            }
        }

        // 회전 + 타격
        baseAngle = (baseAngle + _angSpeed * Time.deltaTime) % 360f;
        float step = 360f / Mathf.Max(1, _currentCycleCount);

        for (int i = 0; i < _currentCycleCount && i < blades.Count; i++)
        {
            if (blades[i] == null || !blades[i].activeSelf) continue;

            float angleDeg = baseAngle + step * i;
            float a = angleDeg * Mathf.Deg2Rad;

            blades[i].transform.localPosition =
                new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _radius;

            if (rotateBladeToFaceOutward)
                blades[i].transform.localRotation =
                    Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

            CheckHit(blades[i].transform.position);
        }
    }

    private void LateUpdate()
    {
        if (owner != null)
            transform.position = owner.position;
    }

    // ══════════════════════════════════════════════════════════════
    // Setup
    // ══════════════════════════════════════════════════════════════

    private void DoSetup()
    {
        EnsureBladeInstances();
        _currentCycleCount = Mathf.Max(1, P.projectileCount);
        CacheParams();
        PositionAndActivateBlades(_currentCycleCount);
        _isActive = true;
        _stateTimer = 0f;
    }

    private void CacheParams()
    {
        var p = P;
        _radius   = (p.orbitRadius > 0.01f)       ? p.orbitRadius       : orbitRadius;
        _angSpeed = (p.orbitAngularSpeed > 0.01f)  ? p.orbitAngularSpeed  : orbitAngularSpeed;
        _hitIvl   = p.hitInterval;
        _dmg      = p.damage;

        _radius   = Mathf.Max(0.5f, _radius);
        _angSpeed = Mathf.Max(30f, _angSpeed);
    }

    // ══════════════════════════════════════════════════════════════
    // 타격
    // ══════════════════════════════════════════════════════════════

    private void CheckHit(Vector3 bladePos)
    {
        int count = Physics2DCompat.OverlapCircleNonAlloc(bladePos, hitRadius, hitResults, enemyMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D enemy = hitResults[i];
            if (enemy == null) continue;

            int id = DamageUtil2D.GetRootId(enemy);
            float now = Time.time;
            float ivl = Mathf.Max(minHitInterval, _hitIvl);

            if (lastHitTime.TryGetValue(id, out float t) && now - t < ivl)
                continue;

            lastHitTime[id] = now;
            int dmg = Mathf.Clamp(_dmg, 1, maxDamagePerHit);
            DamageUtil2D.TryApplyDamage(enemy, dmg);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 블레이드 풀 + VFX 관리
    // ══════════════════════════════════════════════════════════════

    private void EnsureBladeInstances()
    {
        if (bladeTemplate == null) return;

        int want = Mathf.Max(1, config != null ? config.maxLevel : 1);

        while (blades.Count < want)
        {
            // 블레이드 복제
            GameObject b = Instantiate(bladeTemplate, bladeTemplate.transform.parent);
            b.SetActive(false);
            blades.Add(b);

            // ★ VFX 복제: 블레이드의 자식으로 붙임
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, b.transform);
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = Quaternion.identity;
                vfx.SetActive(false);
                bladeVfx.Add(vfx);
            }
            else
            {
                bladeVfx.Add(null);
            }
        }

        if (bladeTemplate.activeSelf)
            bladeTemplate.SetActive(false);
    }

    private void PositionAndActivateBlades(int activeCount)
    {
        float step = 360f / Mathf.Max(1, activeCount);

        for (int i = 0; i < blades.Count; i++)
        {
            var blade = blades[i];
            if (blade == null) continue;

            if (i < activeCount)
            {
                float angleDeg = baseAngle + step * i;
                float a = angleDeg * Mathf.Deg2Rad;

                blade.transform.localPosition =
                    new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _radius;

                if (rotateBladeToFaceOutward)
                    blade.transform.localRotation =
                        Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

                if (!blade.activeSelf)
                    blade.SetActive(true);

                // ★ VFX 활성화 + 파티클 재시작
                ActivateVfx(i, true);
            }
            else
            {
                if (blade.activeSelf)
                    blade.SetActive(false);

                ActivateVfx(i, false);
            }
        }
    }

    private void SetBladesVisible(bool visible)
    {
        if (visible)
        {
            PositionAndActivateBlades(_currentCycleCount);
        }
        else
        {
            for (int i = 0; i < blades.Count; i++)
            {
                if (blades[i] != null) blades[i].SetActive(false);
                ActivateVfx(i, false);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ★ VFX 활성/비활성 + 파티클 재시작
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 블레이드 인덱스에 대응하는 VFX를 켜거나 끈다.
    /// ON일 때 ParticleSystem.Play()를 호출해서 매 사이클마다 파티클이 새로 시작됨.
    /// OFF일 때 ParticleSystem.Stop() + Clear()로 잔상 없이 깔끔하게 정리.
    ///
    /// [설계 이유]
    /// ProjectileVFXChild는 1회성 투사체용이라 회전검에 부적합.
    /// 회전검은 ON/OFF 사이클마다 VFX가 반복 재생돼야 하므로
    /// 블레이드별로 VFX 인스턴스를 직접 관리한다.
    /// </summary>
    private void ActivateVfx(int index, bool on)
    {
        if (index < 0 || index >= bladeVfx.Count) return;
        var vfx = bladeVfx[index];
        if (vfx == null) return;

        if (on)
        {
            if (!vfx.activeSelf)
                vfx.SetActive(true);

            // 모든 하위 파티클 시스템 재시작
            var particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear();
                particles[i].Play();
            }
        }
        else
        {
            if (vfx.activeSelf)
            {
                var particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                vfx.SetActive(false);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = (owner != null) ? owner.position : transform.position;
        float r = _radius > 0.1f ? _radius : orbitRadius;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Gizmos.DrawWireSphere(c, r);
    }
#endif
}