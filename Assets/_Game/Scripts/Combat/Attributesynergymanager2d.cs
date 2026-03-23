// ──────────────────────────────────────────────
// AttributeSynergyManager2D.cs
// 속성 시너지 시스템 — 지원 캐릭터의 속성을 메인 캐릭터 스킬에 부여
//
// [핵심 설계]
// "메인 캐릭터의 시작 스킬에 지원 1/2의 속성 효과가 부여된다"
// → 덱 빌딩 요소: 지원 캐릭터 조합에 따라 전투 스타일이 달라짐
//
// [동작 원리]
// 1. SquadLoadout2D에서 지원1, 지원2 캐릭터 속성을 읽음
// 2. DamageEvents2D.OnEnemyDamageApplied 구독
// 3. 데미지 적중 시 → 지원 속성으로 추가 데미지 자동 적용
// 4. 추가 속성 데미지 → 속성 VFX 자동 발생 (ElementVFXObserver2D)
// 5. 추가 속성 데미지 → 캐릭터 패시브 자동 트리거
//    예: 지원=윤설(빙결) → Ice 추가 데미지 → 혹한 중첩 자동 부여
//
// [예시: 메인=하율, 지원1=윤설(빙결), 지원2=하린(음)]
// 낙뢰부 100 데미지 적중 시:
//   → 전기 100 데미지 (본체)
//   → 빙결 10 데미지 (시너지, 10%) → 혹한 중첩 +1
//   → 음 10 데미지 (시너지, 10%) → 음 VFX 표시
//
// [Hierarchy / Inspector]
// Player 오브젝트에 컴포넌트 부착
// Loadout 필드: 비워두면 자동 탐색
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 지원 캐릭터의 속성을 메인 캐릭터의 스킬에 시너지로 부여하는 매니저입니다.
/// Player 오브젝트에 부착합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AttributeSynergyManager2D : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  설정
    // ═══════════════════════════════════════════════════════

    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private SquadLoadout2D loadout;

    [Header("시너지 설정")]
    [Tooltip("시너지 추가 데미지 비율입니다. 0.10 = 원래 데미지의 10%")]
    [SerializeField] private float synergyDamageRate = 0.10f;

    [Tooltip("시너지 데미지 최소값입니다. 비율 계산 결과가 이보다 작으면 이 값을 사용합니다.")]
    [SerializeField] private int minSynergyDamage = 1;

    [Header("디버그")]
    [Tooltip("체크 시 시너지 적용 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    // ═══════════════════════════════════════════════════════
    //  정적 접근자 (외부 조회용)
    // ═══════════════════════════════════════════════════════

    /// <summary>현재 활성화된 시너지 속성 배열입니다. 비어 있으면 시너지 없음.</summary>
    public static DamageElement2D[] ActiveSynergyElements { get; private set; }
        = System.Array.Empty<DamageElement2D>();

    /// <summary>시너지 시스템이 활성 상태인지 여부입니다.</summary>
    public static bool HasSynergy => ActiveSynergyElements.Length > 0;

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════

    /// <summary>이벤트 재진입 방지 플래그</summary>
    private bool _applyingSynergy;

    /// <summary>지원1 속성 (DamageElement)</summary>
    private DamageElement2D _support1Element = DamageElement2D.Physical;

    /// <summary>지원2 속성 (DamageElement)</summary>
    private DamageElement2D _support2Element = DamageElement2D.Physical;

    /// <summary>활성 시너지 수 (0~2)</summary>
    private int _synergyCount;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<SquadLoadout2D>();
    }

    private void Start()
    {
        if (loadout != null)
        {
            loadout.OnLoadoutChanged += RebuildSynergy;
            RebuildSynergy();
        }
        else
        {
            Debug.LogWarning("[속성 시너지] SquadLoadout2D를 찾을 수 없습니다!", this);
        }

        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;
    }

    private void OnDestroy()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;

        if (loadout != null)
            loadout.OnLoadoutChanged -= RebuildSynergy;

        ActiveSynergyElements = System.Array.Empty<DamageElement2D>();
    }

    // ═══════════════════════════════════════════════════════
    //  시너지 구성
    // ═══════════════════════════════════════════════════════

    private void RebuildSynergy()
    {
        _synergyCount = 0;
        _support1Element = DamageElement2D.Physical;
        _support2Element = DamageElement2D.Physical;

        if (loadout == null) return;

        // 메인 캐릭터 속성 (시너지 대상에서 제외 — 이미 본인 속성)
        DamageElement2D mainElement = DamageElement2D.Physical;
        if (loadout.Main != null && loadout.Main.Attribute != CharacterAttributeKind.None)
            mainElement = loadout.Main.Attribute.ToDamageElement();

        // 지원1 속성
        if (loadout.Support1 != null && loadout.Support1.Attribute != CharacterAttributeKind.None)
        {
            DamageElement2D elem = loadout.Support1.Attribute.ToDamageElement();
            // 메인과 같은 속성이면 시너지 중복 → 스킵
            if (elem != mainElement && elem != DamageElement2D.Physical)
            {
                _support1Element = elem;
                _synergyCount++;
            }
        }

        // 지원2 속성
        if (loadout.Support2 != null && loadout.Support2.Attribute != CharacterAttributeKind.None)
        {
            DamageElement2D elem = loadout.Support2.Attribute.ToDamageElement();
            // 메인/지원1과 같으면 스킵
            if (elem != mainElement && elem != DamageElement2D.Physical && elem != _support1Element)
            {
                _support2Element = elem;
                _synergyCount++;
            }
        }

        // 정적 배열 갱신 (외부 조회용)
        RebuildStaticArray();

        if (debugLog)
        {
            string mainName = loadout.Main != null ? loadout.Main.DisplayName : "(없음)";
            string s1 = _synergyCount >= 1 ? _support1Element.ToString() : "없음";
            string s2 = _synergyCount >= 2 ? _support2Element.ToString() : "없음";
            Debug.Log($"[속성 시너지] 구성 완료 | 메인={mainName}({mainElement}) " +
                      $"시너지1={s1} 시너지2={s2} 비율={synergyDamageRate * 100f}%");
        }
    }

    private void RebuildStaticArray()
    {
        if (_synergyCount == 0)
        {
            ActiveSynergyElements = System.Array.Empty<DamageElement2D>();
        }
        else if (_synergyCount == 1)
        {
            ActiveSynergyElements = new[] { _support1Element };
        }
        else
        {
            ActiveSynergyElements = new[] { _support1Element, _support2Element };
        }
    }

    // ═══════════════════════════════════════════════════════
    //  데미지 이벤트 처리
    // ═══════════════════════════════════════════════════════

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        // 재진입 방지 (시너지 데미지가 다시 이벤트를 발생시킴)
        if (_applyingSynergy) return;
        if (_synergyCount == 0) return;
        if (info.Target == null) return;
        if (info.Amount <= 0) return;

        // 시너지 데미지 계산
        int synergyDamage = Mathf.Max(
            minSynergyDamage,
            Mathf.RoundToInt(info.Amount * synergyDamageRate)
        );

        _applyingSynergy = true;

        // 지원1 속성 시너지 적용
        if (_synergyCount >= 1 && _support1Element != DamageElement2D.Physical)
        {
            // 원본 데미지가 이미 이 속성이면 중복 적용 안 함
            if (info.Element != _support1Element)
            {
                DamageUtil2D.TryApplyDamage(info.Target, synergyDamage, _support1Element);
            }
        }

        // 지원2 속성 시너지 적용
        if (_synergyCount >= 2 && _support2Element != DamageElement2D.Physical)
        {
            if (info.Element != _support2Element)
            {
                DamageUtil2D.TryApplyDamage(info.Target, synergyDamage, _support2Element);
            }
        }

        _applyingSynergy = false;
    }
}