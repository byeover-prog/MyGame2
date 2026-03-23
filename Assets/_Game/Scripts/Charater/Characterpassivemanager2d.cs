// ──────────────────────────────────────────────
// CharacterPassiveManager2D.cs
// 메인 캐릭터에 따라 고유 패시브를 활성화하는 매니저
//
// [동작 원리]
// 1. SquadLoadout2D의 편성 변경 이벤트를 구독
// 2. 메인 캐릭터의 characterId에 따라 적절한 패시브를 활성화
// 3. 이전 패시브는 자동 비활성화
//
// [Hierarchy 설정]
// Player 오브젝트에 컴포넌트 부착
//
// [Inspector 설정]
// - Loadout: SquadLoadout2D 참조 (같은 Player 오브젝트에서 자동 탐색)
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 캐릭터에 따라 고유 패시브를 관리하는 매니저입니다.
/// Player 오브젝트에 부착합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterPassiveManager2D : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  설정
    // ═══════════════════════════════════════════════════════

    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private SquadLoadout2D loadout;

    [Header("디버그")]
    [Tooltip("체크 시 패시브 전환 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = true;

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════

    /// <summary>캐릭터 ID → 패시브 컴포넌트 매핑</summary>
    private readonly Dictionary<string, CharacterPassiveBase> _passiveMap = new();

    /// <summary>현재 활성화된 패시브</summary>
    private CharacterPassiveBase _activePassive;

    // ═══════════════════════════════════════════════════════
    //  공개 API
    // ═══════════════════════════════════════════════════════

    /// <summary>현재 활성화된 고유 패시브입니다. null이면 없음.</summary>
    public CharacterPassiveBase ActivePassive => _activePassive;

    /// <summary>
    /// 특정 타입의 패시브를 가져옵니다.
    /// 예: manager.GetPassive&lt;YoonseolPassive_Hokhan&gt;()
    /// </summary>
    public T GetPassive<T>() where T : CharacterPassiveBase
    {
        foreach (var pair in _passiveMap)
        {
            if (pair.Value is T typed) return typed;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<SquadLoadout2D>();

        // 모든 패시브를 컴포넌트로 미리 생성 (비활성 상태)
        RegisterPassive("yunseol", gameObject.AddComponent<YoonseolPassive_Hokhan>());
        RegisterPassive("hayul", gameObject.AddComponent<HayulPassive_Dosa>());
        RegisterPassive("harin", gameObject.AddComponent<HarinPassive_Bongukkembeop>());
    }

    private void Start()
    {
        // SquadLoadout2D 이벤트 구독
        if (loadout != null)
        {
            loadout.OnLoadoutChanged += HandleLoadoutChanged;

            // 현재 편성으로 즉시 적용
            ApplyPassiveForCharacter(loadout.Main?.CharacterId);
        }
        else
        {
            Debug.LogWarning("[CharacterPassiveManager2D] SquadLoadout2D를 찾을 수 없습니다!", this);
        }
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= HandleLoadoutChanged;

        // 모든 패시브 비활성화
        if (_activePassive != null)
            _activePassive.Deactivate();

        // 메인 속성 초기화
        MainElementProvider.Reset();
    }

    // ═══════════════════════════════════════════════════════
    //  내부 로직
    // ═══════════════════════════════════════════════════════

    private void RegisterPassive(string characterId, CharacterPassiveBase passive)
    {
        _passiveMap[characterId] = passive;
        // 생성 시 비활성 — 컴포넌트 자체는 존재하되 이벤트 구독은 안 함
        passive.enabled = false;
    }

    private void HandleLoadoutChanged()
    {
        if (loadout == null) return;
        ApplyPassiveForCharacter(loadout.Main?.CharacterId);
    }

    private void ApplyPassiveForCharacter(string characterId)
    {
        // ★ 메인 캐릭터 속성을 전역 제공자에 설정
        if (loadout != null && loadout.Main != null &&
            loadout.Main.Attribute != CharacterAttributeKind.None)
        {
            MainElementProvider.Set(loadout.Main.Attribute.ToDamageElement());
        }
        else
        {
            MainElementProvider.Reset();
        }

        // 같은 캐릭터면 무시
        if (_activePassive != null &&
            _passiveMap.TryGetValue(characterId ?? "", out var same) &&
            same == _activePassive)
        {
            return;
        }

        // 기존 패시브 비활성화
        if (_activePassive != null)
        {
            _activePassive.Deactivate();
            _activePassive.enabled = false;
            _activePassive = null;
        }

        // 새 패시브 활성화
        if (!string.IsNullOrWhiteSpace(characterId) &&
            _passiveMap.TryGetValue(characterId, out var passive))
        {
            passive.enabled = true;
            passive.Activate();
            _activePassive = passive;

            if (debugLog)
                Debug.Log($"[CharacterPassiveManager2D] 고유 패시브 전환 → " +
                          $"'{passive.PassiveName}' (캐릭터: {characterId})");
        }
        else
        {
            if (debugLog)
                Debug.Log($"[CharacterPassiveManager2D] 캐릭터 '{characterId}'에 " +
                          $"대응하는 고유 패시브 없음");
        }
    }
}