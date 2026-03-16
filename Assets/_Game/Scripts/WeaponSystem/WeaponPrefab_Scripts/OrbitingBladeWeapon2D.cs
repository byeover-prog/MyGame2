// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회전검 무기.
/// [구현 원리 요약]
/// - 현재 보이지 않는 진짜 블레이드가 돌고, 루트의 정적인 외주 이펙트만 가운데 남아 있어 안 도는 것처럼 보였다.
/// - 그래서 루트 시각 요소는 끄고, 실제 회전하는 블레이드 클론의 SpriteRenderer를 강제로 켠다.
/// - 블레이드 위치/회전/타격은 이 스크립트 한 곳에서만 관리한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 설정")]
    [SerializeField, Tooltip("회전검 원본 오브젝트입니다.")]
    private GameObject bladeTemplate;

    [SerializeField, Min(0.05f), Tooltip("회전검 한 자루의 타격 반경입니다.")]
    private float hitRadius = 1.0f;

    [Header("비주얼(방향)")]
    [SerializeField, Tooltip("체크 시 검이 바깥 방향을 바라봅니다.")]
    private bool rotateBladeToFaceOutward = true;

    [SerializeField, Tooltip("검 스프라이트 방향 보정값입니다.")]
    private float bladeVisualRotationOffsetDeg = 0f;

    [Header("ON/OFF 사이클")]
    [SerializeField, Tooltip("체크 시 회전검이 ON/OFF 사이클로 동작합니다.")]
    private bool useCooldownCycle = true;

    [SerializeField, Min(0.1f), Tooltip("회전검이 활성 상태로 도는 시간입니다.")]
    private float activeSeconds = 1.5f;

    [SerializeField, Min(0.1f), Tooltip("회전검이 꺼져 있는 시간입니다.")]
    private float cooldownSeconds = 1.5f;

    [Header("안전 캡")]
    [SerializeField, Min(0.5f), Tooltip("회전 반지름 최소값입니다.")]
    private float minOrbitRadius = 1.5f;

    [SerializeField, Min(30f), Tooltip("회전 속도 최소값(도/초)입니다. SO 값이 0이어도 최소 이 속도로 돕니다.")]
    private float minOrbitAngularSpeed = 120f;

    [SerializeField, Min(0.01f), Tooltip("같은 적 재타격 최소 간격입니다.")]
    private float minHitInterval = 0.20f;

    [SerializeField, Min(1), Tooltip("회전검 1타 최대 데미지 상한입니다.")]
    private int maxDamagePerHit = 50;

    private readonly List<GameObject> blades = new List<GameObject>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);
    private readonly Collider2D[] hitResults = new Collider2D[10];

    private float baseAngle;
    private bool _isActive = true;
    private float _stateTimer;
    private int _currentCycleCount;
    private bool _rootVisualPrepared;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);

        PrepareRootVisual();
        EnsureBladeInstances();

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
        PrepareRootVisual();
        EnsureBladeInstances();

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
                    PositionAndActivateBlades(_currentCycleCount);
                }
                else
                {
                    return;
                }
            }
        }

        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float angSpeed = Mathf.Max(minOrbitAngularSpeed, P.orbitAngularSpeed);

        baseAngle = (baseAngle + angSpeed * Time.deltaTime) % 360f;
        float step = 360f / Mathf.Max(1, _currentCycleCount);

        for (int i = 0; i < _currentCycleCount && i < blades.Count; i++)
        {
            GameObject blade = blades[i];
            if (blade == null || !blade.activeSelf) continue;

            float angleDeg = baseAngle + step * i;
            float rad = angleDeg * Mathf.Deg2Rad;

            blade.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;

            if (rotateBladeToFaceOutward)
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

            CheckHitForBlade(blade.transform.position);
        }
    }

    private void PrepareRootVisual()
    {
        if (_rootVisualPrepared) return;

        var rootVfx = GetComponent<ProjectileVFXChild>();
        if (rootVfx != null)
        {
            rootVfx.SetVFXEnabled(false, false);
            rootVfx.enabled = false;
        }

        var rootRenderers = GetComponents<SpriteRenderer>();
        for (int i = 0; i < rootRenderers.Length; i++)
        {
            if (rootRenderers[i] != null)
                rootRenderers[i].enabled = false;
        }

        if (bladeTemplate != null)
        {
            for (int i = bladeTemplate.transform.parent.childCount - 1; i >= 0; i--)
            {
                Transform child = bladeTemplate.transform.parent.GetChild(i);
                if (child == bladeTemplate.transform) continue;

                // 초기 생성 시 루트 VFX가 자식으로 붙어 있었다면 여기서 정리된다.
                if (child.GetComponentInChildren<OrbitingBladeHitbox2D>(true) == null)
                    Destroy(child.gameObject);
            }
        }

        _rootVisualPrepared = true;
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

            if (lastHitTime.TryGetValue(id, out float t) && now - t < interval)
                continue;

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
            GameObject blade = Instantiate(bladeTemplate, bladeTemplate.transform.parent);
            PrepareBladeVisual(blade);
            blade.SetActive(false);
            blades.Add(blade);
        }

        if (bladeTemplate.activeSelf)
            bladeTemplate.SetActive(false);
    }

    private void PrepareBladeVisual(GameObject blade)
    {
        if (blade == null) return;

        var bladeVfx = blade.GetComponent<ProjectileVFXChild>();
        if (bladeVfx != null)
        {
            bladeVfx.SetVFXEnabled(false, false);
            bladeVfx.enabled = false;
        }

        var srs = blade.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] != null)
                srs[i].enabled = true;
        }
    }

    private void PositionAndActivateBlades(int activeCount)
    {
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float step = 360f / Mathf.Max(1, activeCount);

        for (int i = 0; i < blades.Count; i++)
        {
            GameObject blade = blades[i];
            if (blade == null) continue;

            if (i < activeCount)
            {
                float angleDeg = baseAngle + step * i;
                float rad = angleDeg * Mathf.Deg2Rad;

                blade.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;

                if (rotateBladeToFaceOutward)
                    blade.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

                PrepareBladeVisual(blade);

                if (!blade.activeSelf)
                    blade.SetActive(true);
            }
            else if (blade.activeSelf)
            {
                blade.SetActive(false);
            }
        }
    }

    private void SetBladesVisible(bool visible)
    {
        if (visible)
        {
            PositionAndActivateBlades(_currentCycleCount);
            return;
        }

        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null)
                blades[i].SetActive(false);
        }
    }
}