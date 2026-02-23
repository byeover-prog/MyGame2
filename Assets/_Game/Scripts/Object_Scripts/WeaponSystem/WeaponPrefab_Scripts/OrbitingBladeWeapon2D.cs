using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// 리지드바디(물리엔진)를 전혀 사용하지 않고 수학적으로 검의 위치를 회전시킵니다.
// 검이 이동한 위치에 가상의 원(OverlapCircle)을 그려 적을 찾아내므로 충돌/끼임 오류가 절대 발생하지 않습니다.
[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 설정")]
    [Tooltip("회전할 검의 형태(이미지)를 가진 자식 게임오브젝트입니다. 리지드바디와 콜라이더는 삭제해도 됩니다!")]
    [SerializeField] private GameObject bladeTemplate;

    [Tooltip("검 하나당 적을 타격하는 판정 크기(반경)입니다. 인스펙터에서 조절하세요.")]
    [SerializeField] private float hitRadius = 1.0f;

    [Header("안전 캡 (수치 조정용)")]
    [Tooltip("회전 반경 최솟값(유닛). SO값이 너무 작을 때를 대비한 하한선입니다.")]
    [SerializeField] private float minOrbitRadius = 2.0f;

    [Tooltip("타격 간격 최솟값(초). 동일한 적을 다시 때리기까지의 최소 시간입니다.")]
    [SerializeField] private float minHitInterval = 0.20f;

    [Tooltip("1타 피해 상한. 밸런스 붕괴를 막기 위한 최대 데미지 제한입니다.")]
    [SerializeField] private int maxDamagePerHit = 50;

    private readonly List<GameObject> blades = new List<GameObject>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);
    private float baseAngle;

    // 물리 연산 최적화를 위한 배열 (가비지 컬렉터 방지)
    private readonly Collider2D[] hitResults = new Collider2D[10];

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        EnsureBladeInstances();
        OnLevelChanged();
    }

    private void LateUpdate()
    {
        if (owner == null) return;
        // 회전 중심을 항상 플레이어(주인) 위치로 고정합니다.
        transform.position = owner.position;
    }

    protected override void OnLevelChanged()
    {
        EnsureBladeInstances();
        int activeCount = Mathf.Max(1, P.projectileCount);

        for (int i = 0; i < blades.Count; i++)
        {
            blades[i].SetActive(i < activeCount);
        }
        lastHitTime.Clear();
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        int activeCount = Mathf.Max(1, P.projectileCount);
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float angSpeed = P.orbitAngularSpeed;
        
        // 시간의 흐름에 따라 각도를 증가시킵니다.
        baseAngle = (baseAngle + angSpeed * Time.deltaTime) % 360f;
        float step = 360f / activeCount;

        for (int i = 0; i < activeCount && i < blades.Count; i++)
        {
            // 1. 수학적 위치 계산 (물리 충돌 없음)
            float a = (baseAngle + step * i) * Mathf.Deg2Rad;
            Vector3 local = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            blades[i].transform.localPosition = local;

            // 2. 해당 위치에 가상의 원을 그려 적 타격 판정
            CheckHitForBlade(blades[i].transform.position);
        }
    }

    private void CheckHitForBlade(Vector3 bladePosition)
    {
        // hitRadius 반경 내의 적(enemyMask 레이어)만 찾아냅니다.
        int hitCount = Physics2D.OverlapCircleNonAlloc(bladePosition, hitRadius, hitResults, enemyMask);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D enemy = hitResults[i];
            if (enemy == null) continue;

            int id = DamageUtil2D.GetRootId(enemy);
            float now = Time.time;
            float interval = Mathf.Max(minHitInterval, P.hitInterval);

            // 쿨타임 체크 (너무 빠르게 연속 타격되는 것 방지)
            if (lastHitTime.TryGetValue(id, out float t))
            {
                if (now - t < interval) continue;
            }

            // 데미지 적용
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
}