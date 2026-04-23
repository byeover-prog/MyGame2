using UnityEngine;

public enum TargetPolicy
{
    Nearest,
    LowestHp,
    LowestHpRatio,
    BossFirst
}

[CreateAssetMenu(menuName = "그날이후/무기 정의(Weapon)", fileName = "Weapon_")]
public sealed class WeaponDefinitionSO : ScriptableObject
{
    [Header("레벨업 스텝 스케일(무기별 성장 성향)")]
    [Tooltip("DamageAdd 스텝에 곱해지는 배율(1=기본, 1.5=더 빠르게 성장)")]
    [Min(0f)]
    public float damageStepScale = 1f;

    [Tooltip("CooldownMul 스텝에 적용되는 지수(1=기본, 0.5=덜 빨라짐, 1.5=더 빨라짐)")]
    [Min(0f)]
    public float cooldownStepScale = 1f;

    [Tooltip("RangeAdd 스텝에 곱해지는 배율(1=기본, 1.2=사거리 성장 더 큼)")]
    [Min(0f)]
    public float rangeStepScale = 1f;
    
    [Header("식별자(저장용)")]
    [Tooltip("JSON 저장/로드 키로 쓰는 고유 ID (절대 중복 금지)")]
    public string weaponId = "weapon_basic";

    [Header("표시(유저용)")]
    [Tooltip("카드/UI에 보여줄 이름(예: 발시, 회전검). 비면 에셋 이름을 사용")]
    public string displayNameKr;

    [Tooltip("카드 태그(예: 공통, 빙결, 투사체)")]
    public string tagsKr = "공통";

    [Tooltip("레벨업 카드에 반드시 표시되는 '공격 방식' 설명(레벨 표기 금지).\n예: '가장 가까운 적을 향해 직선 관통 탄을 발사합니다.'")]
    [TextArea]
    public string visualDescriptionKr;

    [Tooltip("카드/UI에 표시할 아이콘(없으면 아이콘 영역을 숨김)")]
    public Sprite icon;

    [Tooltip("프로토타입에서 뽑기/업그레이드 대상에 포함할지")]
    public bool includeInPrototype = true;

    [Tooltip("메인 캐릭터 기본 스킬인지(가중치/우선권 등에서 사용)")]
    public bool isMainBasicSkill = false;

    [Tooltip("카드 등장 가중치(기본 100, 메인 기본 스킬은 120 권장)")]
    public int weight = 100;

    [Header("투사체 프리팹")]
    [Tooltip("풀링할 투사체 프리팹(필수)")]
    public GameObject projectilePrefab;

    [Header("기본 스탯")]
    [Min(1)] public int baseDamage = 10;
    [Min(0.05f)] public float baseFireInterval = 0.5f;
    [Min(0.1f)] public float baseRange = 10f;

    [Header("타겟팅")]
    public LayerMask enemyLayer = ~0;
    public TargetPolicy targetPolicy = TargetPolicy.Nearest;

    [Header("기타")]
    [Tooltip("같은 팀에서 다른 슬롯이 이미 선택한 타겟을 피할지")]
    public bool avoidDuplicateTargets = true;

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayNameKr) ? name : displayNameKr;
    }

    public string GetVisualDescription()
    {
        return string.IsNullOrWhiteSpace(visualDescriptionKr)
            ? "가장 가까운 적을 자동으로 조준해 투사체를 발사합니다."
            : visualDescriptionKr;
    }

    // --------------------
    // 하위 호환(예전 코드/필드명 유지)
    // --------------------
    public string Id => weaponId;            // 예전 UI/선택 코드가 Skill.Id 형태로 접근할 때 대응
    public string weaponNameKr => displayNameKr;
    public string tagKr => tagsKr;
}