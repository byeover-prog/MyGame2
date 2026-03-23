// ──────────────────────────────────────────────
// AttributeSynergyManager2D.cs  (v2 — DamageChainGuard 적용)
// 속성 시너지 시스템 — 지원 캐릭터의 속성을 메인 캐릭터 스킬에 부여
//
// [v2 변경사항]
// - DamageChainGuard를 사용하여 보너스/시너지 데미지에 반응하지 않음
// - 원본 데미지에만 시너지 적용 → 데미지 체인 폭발 방지
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 지원 캐릭터의 속성을 메인 캐릭터의 스킬에 시너지로 부여하는 매니저입니다.
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

    /// <summary>현재 활성화된 시너지 속성 배열입니다.</summary>
    public static DamageElement2D[] ActiveSynergyElements { get; private set; }
        = System.Array.Empty<DamageElement2D>();

    public static bool HasSynergy => ActiveSynergyElements.Length > 0;

    private DamageElement2D _support1Element = DamageElement2D.Physical;
    private DamageElement2D _support2Element = DamageElement2D.Physical;
    private int _synergyCount;

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
            ActiveSynergyElements = System.Array.Empty<DamageElement2D>();
        else if (_synergyCount == 1)
            ActiveSynergyElements = new[] { _support1Element };
        else
            ActiveSynergyElements = new[] { _support1Element, _support2Element };
    }

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        // ★ 핵심: 보너스/시너지 데미지에는 반응하지 않음 (체인 방지)
        if (DamageChainGuard.IsProcessingBonus) return;
        if (_synergyCount == 0) return;
        if (info.Target == null) return;
        if (info.Amount <= 0) return;

        int synergyDamage = Mathf.Max(
            minSynergyDamage,
            Mathf.RoundToInt(info.Amount * synergyDamageRate)
        );

        DamageChainGuard.BeginBonus();

        if (_synergyCount >= 1 && _support1Element != DamageElement2D.Physical)
        {
            if (info.Element != _support1Element)
                DamageUtil2D.TryApplyDamage(info.Target, synergyDamage, _support1Element);
        }

        if (_synergyCount >= 2 && _support2Element != DamageElement2D.Physical)
        {
            if (info.Element != _support2Element)
                DamageUtil2D.TryApplyDamage(info.Target, synergyDamage, _support2Element);
        }

        DamageChainGuard.EndBonus();
    }
}