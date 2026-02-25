using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 설정")]
    [Tooltip("회전할 검(이미지) 템플릿. 리지드바디/콜라이더는 없어도 됩니다.")]
    [SerializeField] private GameObject bladeTemplate;

    [Tooltip("검 하나당 적 타격 판정 반경")]
    [SerializeField] private float hitRadius = 1.0f;

    [Header("비주얼(방향)")]
    [SerializeField] private bool rotateBladeToFaceOutward = true;

    [Tooltip("스프라이트 기본 방향 보정(도). 90/-90/180로 맞추면 대부분 해결됩니다.")]
    [SerializeField] private float bladeVisualRotationOffsetDeg = 0f;

    [Header("쿨타임(ON/OFF 사이클)")]
    [Tooltip("체크하면 회전검이 '활성 시간' 동안만 동작하고 이후 '쿨타임' 동안 꺼집니다.")]
    [SerializeField] private bool useCooldownCycle = true;

    [Tooltip("활성 상태로 유지되는 시간(초)")]
    [SerializeField, Min(0.1f)] private float activeSeconds = 1.5f;

    [Tooltip("비활성(쿨타임) 시간(초)")]
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 1.5f;

    [Header("안전 캡(수치 조정용)")]
    [SerializeField] private float minOrbitRadius = 2.0f;
    [SerializeField] private float minHitInterval = 0.20f;
    [SerializeField] private int maxDamagePerHit = 50;

    private readonly List<GameObject> blades = new List<GameObject>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);
    private readonly Collider2D[] hitResults = new Collider2D[10];

    private float baseAngle;

    // 쿨타임 사이클 상태
    private bool _isActive = true;
    private float _stateTimer;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        EnsureBladeInstances();
        OnLevelChanged();

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
        ApplyActiveCountVisible();
        lastHitTime.Clear();
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        // ON/OFF 사이클 처리
        if (useCooldownCycle)
        {
            _stateTimer += Time.deltaTime;

            if (_isActive)
            {
                if (_stateTimer >= activeSeconds)
                {
                    _isActive = false;
                    _stateTimer = 0f;
                    SetBladesVisible(false);      // 꺼지는 동안 아예 안 보이게
                    lastHitTime.Clear();          // 재활성 시 즉시 중복타격 방지
                    return;
                }
            }
            else
            {
                if (_stateTimer >= cooldownSeconds)
                {
                    _isActive = true;
                    _stateTimer = 0f;
                    ApplyActiveCountVisible();    // 다시 켜짐 + 개수 반영
                }
                else
                {
                    return; // 쿨타임 동안 로직 완전 정지(사기 방지)
                }
            }
        }

        // 활성 상태에서만 회전/타격
        int activeCount = Mathf.Max(1, P.projectileCount);
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float angSpeed = P.orbitAngularSpeed;

        baseAngle = (baseAngle + angSpeed * Time.deltaTime) % 360f;
        float step = 360f / activeCount;

        for (int i = 0; i < activeCount && i < blades.Count; i++)
        {
            if (!blades[i].activeSelf) continue;

            float angleDeg = (baseAngle + step * i);
            float a = angleDeg * Mathf.Deg2Rad;

            Vector3 local = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            blades[i].transform.localPosition = local;

            if (rotateBladeToFaceOutward)
                blades[i].transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg + bladeVisualRotationOffsetDeg);

            CheckHitForBlade(blades[i].transform.position);
        }
    }

    private void CheckHitForBlade(Vector3 bladePosition)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(bladePosition, hitRadius, hitResults, enemyMask);

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

    private void ApplyActiveCountVisible()
    {
        int activeCount = Mathf.Max(1, P.projectileCount);
        for (int i = 0; i < blades.Count; i++)
            blades[i].SetActive(i < activeCount);
    }

    private void SetBladesVisible(bool visible)
    {
        for (int i = 0; i < blades.Count; i++)
            blades[i].SetActive(visible && i < Mathf.Max(1, P.projectileCount));
    }
}