// ──────────────────────────────────────────────
// AttributeSynergyManager2D.cs  (v3 — 시작 스킬 전용)
// 지원 캐릭터의 속성을 시작 스킬에만 부여
//
// [v3 변경사항]
// - 이벤트 구독 완전 제거 (공통 스킬에 시너지 안 걸림)
// - 정적 메서드 TryApplySynergy() 제공 → 시작 스킬이 직접 호출
// - 시작 스킬(발시/낙뢰부/좌격요세)만 데미지 후 이 메서드를 호출
//
// [사용법 — 시작 스킬 코드에서]
// bool applied = DamageUtil2D.TryApplyDamage(target, damage, element);
// if (applied) AttributeSynergyManager2D.TryApplySynergy(target, damage);
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 지원 캐릭터의 속성을 시작 스킬에만 시너지로 부여하는 매니저입니다.
/// Player 오브젝트에 부착합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AttributeSynergyManager2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private SquadLoadout2D loadout;

    [Header("시너지 설정")]
    [Tooltip("시너지 추가 데미지 비율입니다. 0.10 = 원래 데미지의 10%")]
    [SerializeField] private float synergyDamageRate = 0.10f;

    [Tooltip("시너지 데미지 최소값입니다.")]
    [SerializeField] private int minSynergyDamage = 1;

    [Header("디버그")]
    [Tooltip("체크 시 시너지 적용 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    // ═══════════════════════════════════════════════════════
    //  정적 접근자
    // ═══════════════════════════════════════════════════════

    private static AttributeSynergyManager2D _instance;

    /// <summary>현재 활성화된 시너지 속성 배열입니다.</summary>
    public static DamageElement2D[] ActiveSynergyElements { get; private set; }
        = System.Array.Empty<DamageElement2D>();

    public static bool HasSynergy => ActiveSynergyElements.Length > 0;

    // ═══════════════════════════════════════════════════════
    //  인스턴스 상태
    // ═══════════════════════════════════════════════════════

    private DamageElement2D _support1Element = DamageElement2D.Physical;
    private DamageElement2D _support2Element = DamageElement2D.Physical;
    private int _synergyCount;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        _instance = this;
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
        // ★ v3: 이벤트 구독 없음 — 시작 스킬이 직접 호출
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RebuildSynergy;

        ActiveSynergyElements = System.Array.Empty<DamageElement2D>();

        if (_instance == this)
            _instance = null;
    }

    // ═══════════════════════════════════════════════════════
    //  공개 API — 시작 스킬이 직접 호출
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 시작 스킬이 데미지 적용 후 호출하는 시너지 메서드입니다.
    /// 지원 캐릭터 속성으로 추가 데미지를 적용합니다.
    ///
    /// [사용 예시 — BalsiProjectile2D]
    /// bool applied = DamageUtil2D.TryApplyDamage(target, damage, DamageElement2D.Ice);
    /// if (applied) AttributeSynergyManager2D.TryApplySynergy(target.gameObject, damage);
    /// </summary>
    /// <param name="target">피격 대상 GameObject</param>
    /// <param name="baseDamage">원본 데미지 (시너지 비율 계산용)</param>
    public static void TryApplySynergy(GameObject target, int baseDamage)
    {
        if (_instance == null) return;
        if (_instance._synergyCount == 0) return;
        if (target == null || baseDamage <= 0) return;

        int synergyDmg = Mathf.Max(
            _instance.minSynergyDamage,
            Mathf.RoundToInt(baseDamage * _instance.synergyDamageRate)
        );

        DamageChainGuard.BeginBonus();

        // 지원1 속성 시너지
        if (_instance._synergyCount >= 1 &&
            _instance._support1Element != DamageElement2D.Physical)
        {
            DamageUtil2D.TryApplyDamage(target, synergyDmg, _instance._support1Element);
        }

        // 지원2 속성 시너지
        if (_instance._synergyCount >= 2 &&
            _instance._support2Element != DamageElement2D.Physical)
        {
            DamageUtil2D.TryApplyDamage(target, synergyDmg, _instance._support2Element);
        }

        DamageChainGuard.EndBonus();
    }

    /// <summary>Collider2D 버전 오버로드.</summary>
    public static void TryApplySynergy(Collider2D target, int baseDamage)
    {
        if (target == null) return;
        TryApplySynergy(target.gameObject, baseDamage);
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

        DamageElement2D mainElement = DamageElement2D.Physical;
        if (loadout.Main != null && loadout.Main.Attribute != CharacterAttributeKind.None)
            mainElement = loadout.Main.Attribute.ToDamageElement();

        if (loadout.Support1 != null && loadout.Support1.Attribute != CharacterAttributeKind.None)
        {
            DamageElement2D elem = loadout.Support1.Attribute.ToDamageElement();
            if (elem != mainElement && elem != DamageElement2D.Physical)
            {
                _support1Element = elem;
                _synergyCount++;
            }
        }

        if (loadout.Support2 != null && loadout.Support2.Attribute != CharacterAttributeKind.None)
        {
            DamageElement2D elem = loadout.Support2.Attribute.ToDamageElement();
            if (elem != mainElement && elem != DamageElement2D.Physical && elem != _support1Element)
            {
                _support2Element = elem;
                _synergyCount++;
            }
        }

        // 정적 배열 갱신
        if (_synergyCount == 0)
            ActiveSynergyElements = System.Array.Empty<DamageElement2D>();
        else if (_synergyCount == 1)
            ActiveSynergyElements = new[] { _support1Element };
        else
            ActiveSynergyElements = new[] { _support1Element, _support2Element };

        if (debugLog)
        {
            string mainName = loadout.Main != null ? loadout.Main.DisplayName : "(없음)";
            string s1 = _synergyCount >= 1 ? _support1Element.ToString() : "없음";
            string s2 = _synergyCount >= 2 ? _support2Element.ToString() : "없음";
            Debug.Log($"[속성 시너지] 구성 완료 | 메인={mainName}({mainElement}) " +
                      $"시너지1={s1} 시너지2={s2} 비율={synergyDamageRate * 100f}% " +
                      $"★ 시작 스킬에만 적용");
        }
    }
}