using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회전검 무기 (CommonSkillWeapon2D 기반)
/// 
///  레벨업 처리 방식:
/// - 레벨업해도 현재 돌고 있는 사이클에는 아무 변화 없음
/// - 현재 사이클이 끝나고(OFF) → 다음 사이클이 시작될 때(ON) 새 개수 적용
/// - 보간/재배치 로직 불필요 → 깔끔하고 버벅거림 없음
/// </summary>
[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 설정")]
    [SerializeField] private GameObject bladeTemplate;
    [SerializeField] private float hitRadius = 1.0f;

    [Header("비주얼(방향)")]
    [SerializeField] private bool rotateBladeToFaceOutward = true;
    [SerializeField] private float bladeVisualRotationOffsetDeg = 0f;

    [Header("쿨타임(ON/OFF 사이클)")]
    [SerializeField] private bool useCooldownCycle = true;
    [SerializeField, Min(0.1f)] private float activeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 1.5f;

    [Header("안전 캡")]
    [SerializeField] private float minOrbitRadius = 2.0f;
    [SerializeField] private float minHitInterval = 0.20f;
    [SerializeField] private int maxDamagePerHit = 50;

    private readonly List<GameObject> blades = new List<GameObject>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);
    private readonly Collider2D[] hitResults = new Collider2D[10];

    private float baseAngle;

    // 쿨타임 사이클
    private bool _isActive = true;
    private float _stateTimer;

    //  핵심: 현재 사이클에서 실제로 사용 중인 블레이드 수
    //   레벨업해도 이 값은 바뀌지 않음 → 다음 사이클에서만 갱신
    private int _currentCycleCount;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        EnsureBladeInstances();

        // 최초 획득 시에는 즉시 적용
        _currentCycleCount = Mathf.Max(1, P.projectileCount);
        PositionAndActivateBlades(_currentCycleCount);

        _isActive = true;
        _stateTimer = 0f;
    }

    private void LateUpdate()
    {
        if (owner == null) return;
        transform.position = owner.position;
    }

    protected override void OnLevelChanged()
    {
        EnsureBladeInstances();

        // ★ 현재 사이클에는 아무것도 건드리지 않음
        // _currentCycleCount는 다음 ON 사이클에서 갱신됨
        // (쿨다운 사이클 미사용 시에는 즉시 적용)
        if (!useCooldownCycle)
        {
            _currentCycleCount = Mathf.Max(1, P.projectileCount);
            PositionAndActivateBlades(_currentCycleCount);
        }

        lastHitTime.Clear();
    }

    private void Update()
    {
        if (owner == null || config == null) return;

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

                    // ★ 다음 사이클 시작: 여기서 새 블레이드 수 적용
                    _currentCycleCount = Mathf.Max(1, P.projectileCount);
                    PositionAndActivateBlades(_currentCycleCount);
                }
                else
                {
                    return;
                }
            }
        }

        // 회전/타격
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float angSpeed = P.orbitAngularSpeed;

        baseAngle = (baseAngle + angSpeed * Time.deltaTime) % 360f;
        float step = 360f / _currentCycleCount;

        for (int i = 0; i < _currentCycleCount && i < blades.Count; i++)
        {
            if (!blades[i].activeSelf) continue;

            float angleDeg = baseAngle + step * i;
            float a = angleDeg * Mathf.Deg2Rad;

            blades[i].transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;

            if (rotateBladeToFaceOutward)
                blades[i].transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

            CheckHitForBlade(blades[i].transform.position);
        }
    }

    private void CheckHitForBlade(Vector3 bladePosition)
    {
        int hitCount = Physics2DCompat.OverlapCircleNonAlloc(bladePosition, hitRadius, hitResults, enemyMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D enemy = hitResults[i];
            if (enemy == null) continue;

            int id = DamageUtil2D.GetRootId(enemy);
            float now = Time.time;
            float interval = Mathf.Max(minHitInterval, P.hitInterval);

            if (lastHitTime.TryGetValue(id, out float t))
            {
                if (now - t < interval) continue;
            }

            lastHitTime[id] = now;

            int dmg = Mathf.Clamp(P.damage, 1, Mathf.Max(1, maxDamagePerHit));
            DamageUtil2D.TryApplyDamage(enemy, dmg);
        }
    }

    private void EnsureBladeInstances()
    {
        if (bladeTemplate == null) return;

        int want = Mathf.Max(1, config != null ? config.maxLevel : 1);
        while (blades.Count < want)
        {
            GameObject b = Instantiate(bladeTemplate, bladeTemplate.transform.parent);
            b.SetActive(false);
            blades.Add(b);
        }

        if (bladeTemplate.activeSelf)
            bladeTemplate.SetActive(false);
    }

    /// <summary>
    /// 위치 먼저 계산 후 활성화 (1프레임 점프 방지)
    /// </summary>
    private void PositionAndActivateBlades(int activeCount)
    {
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float step = 360f / Mathf.Max(1, activeCount);

        for (int i = 0; i < blades.Count; i++)
        {
            var blade = blades[i];

            if (i < activeCount)
            {
                float angleDeg = baseAngle + step * i;
                float a = angleDeg * Mathf.Deg2Rad;

                blade.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;

                if (rotateBladeToFaceOutward)
                    blade.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

                if (!blade.activeSelf)
                    blade.SetActive(true);
            }
            else
            {
                if (blade.activeSelf)
                    blade.SetActive(false);
            }
        }
    }

    private void SetBladesVisible(bool visible)
    {
        if (visible)
            PositionAndActivateBlades(_currentCycleCount);
        else
            for (int i = 0; i < blades.Count; i++)
                blades[i].SetActive(false);
    }
}