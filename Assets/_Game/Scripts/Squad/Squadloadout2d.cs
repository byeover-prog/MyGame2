using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SquadLoadout2D : MonoBehaviour
{
    [Header("캐릭터 카탈로그")]
    [Tooltip("전체 캐릭터 목록 SO")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("기본 편성 (Inspector 설정)")]
    [Tooltip("메인 캐릭터")]
    [SerializeField] private CharacterDefinitionSO defaultMain;

    [Tooltip("지원 1 캐릭터")]
    [SerializeField] private CharacterDefinitionSO defaultSupport1;

    [Tooltip("지원 2 캐릭터")]
    [SerializeField] private CharacterDefinitionSO defaultSupport2;

    // 런타임 편성
    private CharacterDefinitionSO _main;
    private CharacterDefinitionSO _support1;
    private CharacterDefinitionSO _support2;

    /// <summary>현재 메인 캐릭터 SO</summary>
    public CharacterDefinitionSO Main => _main;

    /// <summary>현재 지원1 캐릭터 SO</summary>
    public CharacterDefinitionSO Support1 => _support1;

    /// <summary>현재 지원2 캐릭터 SO</summary>
    public CharacterDefinitionSO Support2 => _support2;

    /// <summary>카탈로그 참조</summary>
    public CharacterCatalogSO Catalog => catalog;

    /// <summary>편성이 바뀌면 발생하는 이벤트. SquadApplier2D가 구독.</summary>
    public event Action OnLoadoutChanged;

    private void Awake()
    {
        _main = defaultMain;
        _support1 = defaultSupport1;
        _support2 = defaultSupport2;
        
        if (string.IsNullOrWhiteSpace(SquadLoadoutRuntime.MainId))
        {
            if (_main != null)     SquadLoadoutRuntime.SetMain(_main.CharacterId);
            if (_support1 != null) SquadLoadoutRuntime.SetSupport1(_support1.CharacterId);
            if (_support2 != null) SquadLoadoutRuntime.SetSupport2(_support2.CharacterId);

            GameLogger.Log($"[SquadLoadout2D] Runtime 동기화 — 메인:{SquadLoadoutRuntime.MainId}, " +
                           $"지원1:{SquadLoadoutRuntime.Support1Id}, 지원2:{SquadLoadoutRuntime.Support2Id}");
        }
    }

    /// <summary>
    /// 편성을 변경한다. 캐릭터 선택 UI에서 호출.
    /// null을 넣으면 해당 슬롯은 비어있는 상태.
    /// </summary>
    public void SetLoadout(CharacterDefinitionSO main, CharacterDefinitionSO support1, CharacterDefinitionSO support2)
    {
        _main = main;
        _support1 = support1;
        _support2 = support2;

        GameLogger.Log($"[스쿼드 편성] 변경 | 메인={GetName(_main)} " +
                  $"지원1={GetName(_support1)} 지원2={GetName(_support2)}");

        OnLoadoutChanged?.Invoke();
    }

    /// <summary>
    /// 메인 캐릭터만 변경.
    /// </summary>
    public void SetMain(CharacterDefinitionSO main)
    {
        SetLoadout(main, _support1, _support2);
    }

    /// <summary>
    /// 현재 편성된 캐릭터들의 속성 목록을 반환.
    /// 속성 시스템에서 활성 속성을 결정할 때 사용.
    /// </summary>
    public CharacterAttributeKind[] GetActiveAttributes()
    {
        // 최대 3개 (메인 + 지원1 + 지원2)
        var attrs = new CharacterAttributeKind[3];
        int count = 0;

        if (_main != null && _main.Attribute != CharacterAttributeKind.None)
            attrs[count++] = _main.Attribute;
        if (_support1 != null && _support1.Attribute != CharacterAttributeKind.None)
            attrs[count++] = _support1.Attribute;
        if (_support2 != null && _support2.Attribute != CharacterAttributeKind.None)
            attrs[count++] = _support2.Attribute;

        // 실제 크기에 맞게 반환
        var result = new CharacterAttributeKind[count];
        System.Array.Copy(attrs, result, count);
        return result;
    }

    private static string GetName(CharacterDefinitionSO def)
    {
        return def != null ? def.DisplayName : "(없음)";
    }
}