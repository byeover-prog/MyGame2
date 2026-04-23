// UTF-8
using UnityEngine;

/// <summary>
/// 궁극기 공통 데이터 ScriptableObject.
/// 캐릭터별 Resolver가 이 SO를 읽어서 수치를 적용한다.
///
/// [사용법]
/// 1. Project 창에서 Create > 그날이후 > 궁극기 > 궁극기 데이터
/// 2. Inspector에서 수치/VFX 설정
/// 3. CharacterDefinitionSO의 Ultimate Data 슬롯에 연결
///
/// [설계 원칙]
/// - 공통 필드 (모든 궁극기가 사용): 데미지, 지속시간, 쿨다운, VFX 등
/// - 캐릭터별 고유 필드: 각 Resolver가 추가 SO나 Inspector로 관리
///   (예: 윤설의 혹한 중첩, 하율의 전파 비율 등)
/// </summary>
[CreateAssetMenu(menuName = "그날이후/궁극기/궁극기 데이터", fileName = "SO_Ultimate_")]
public sealed class UltimateDataSO : ScriptableObject
{
    // ═══════════════════════════════════════════════════════
    //  식별
    // ═══════════════════════════════════════════════════════

    [Header("식별")]
    [Tooltip("궁극기 고유 ID")]
    [SerializeField] private string ultimateId;

    [Tooltip("궁극기 한글 이름 (UI 표시용)")]
    [SerializeField] private string displayName;

    public string UltimateId => ultimateId;
    public string DisplayName => displayName;

    // ═══════════════════════════════════════════════════════
    //  타이밍
    // ═══════════════════════════════════════════════════════

    [Header("타이밍")]
    [Tooltip("궁극기 총 지속시간 (초)")]
    [SerializeField] private float duration = 4.5f;

    [Tooltip("첫 피해/발사 전 대기시간 (초)")]
    [SerializeField] private float hitDelay = 0.5f;

    [Tooltip("피해 적용 간격 (초). 0이면 단발 실행.")]
    [SerializeField] private float hitInterval = 0.8f;

    public float Duration => duration;
    public float HitDelay => hitDelay;
    public float HitInterval => hitInterval;

    // ═══════════════════════════════════════════════════════
    //  데미지
    // ═══════════════════════════════════════════════════════

    [Header("데미지")]
    [Tooltip("기본 피해량")]
    [SerializeField] private float baseDamage = 10f;

    [Tooltip("데미지 속성 (캐릭터 SO와 별개로 궁극기 자체 속성 지정 가능).\n" +
             "보통 캐릭터 속성과 동일하게 설정.")]
    [SerializeField] private DamageElement2D damageElement = DamageElement2D.Physical;

    public float BaseDamage => baseDamage;
    public DamageElement2D DamageElement => damageElement;

    // ═══════════════════════════════════════════════════════
    //  범위
    // ═══════════════════════════════════════════════════════

    [Header("범위")]
    [Tooltip("적 탐색 반경 (월드 단위). 카메라와 무관한 고정 값.")]
    [SerializeField] private float hitRadius = 18f;

    [Tooltip("부가 반경 (전파 탐색, 주변 피해 범위 등 Resolver가 용도 결정)")]
    [SerializeField] private float secondaryRadius = 5f;

    public float HitRadius => hitRadius;
    public float SecondaryRadius => secondaryRadius;

    // ═══════════════════════════════════════════════════════
    //  연출 VFX
    // ═══════════════════════════════════════════════════════

    [Header("연출 VFX")]
    [Tooltip("카메라에 부착할 풀스크린 VFX 프리팹")]
    [SerializeField] private GameObject fullscreenVfxPrefab;

    [Tooltip("플레이어 몸에 부착할 오라 VFX 프리팹")]
    [SerializeField] private GameObject playerAuraVfxPrefab;

    public GameObject FullscreenVfxPrefab => fullscreenVfxPrefab;
    public GameObject PlayerAuraVfxPrefab => playerAuraVfxPrefab;

    // ═══════════════════════════════════════════════════════
    //  풀스크린 VFX 스케일
    // ═══════════════════════════════════════════════════════

    [Header("풀스크린 VFX 스케일")]
    [Tooltip("VFX 프리팹이 Scale(1,1,1)일 때 커버하는 월드 크기.\n" +
             "카메라 뷰포트에 맞게 자동 스케일링에 사용.")]
    [SerializeField] private float vfxOriginalWorldSize = 10f;

    [Tooltip("스케일 여유 배수 (1.2 = 카메라보다 20% 크게)")]
    [SerializeField] private float vfxScaleMargin = 1.2f;

    public float VfxOriginalWorldSize => vfxOriginalWorldSize;
    public float VfxScaleMargin => vfxScaleMargin;
}