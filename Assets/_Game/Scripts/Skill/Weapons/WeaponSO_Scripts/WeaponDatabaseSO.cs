using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 프로젝트에 존재하는 모든 무기 정의(WeaponDefinitionSO)를 보관하는 데이터베이스입니다.
/// weaponId로 검색할 수 있으며, WeaponLoadApplier2D 등이 참조합니다.
/// </summary>
public sealed class WeaponDatabaseSO : ScriptableObject
{
    [Header("무기 정의 목록")]
    [Tooltip("게임에 존재하는 모든 WeaponDefinitionSO를 등록합니다.")]
    [SerializeField] private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;

    /// <summary>
    /// weaponId로 무기 정의를 검색합니다.
    /// </summary>
    public bool TryGet(string weaponId, out WeaponDefinitionSO def)
    {
        def = null;
        if (string.IsNullOrWhiteSpace(weaponId)) return false;

        for (int i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            if (w == null) continue;
            if (w.weaponId == weaponId)
            {
                def = w;
                return true;
            }
        }
        return false;
    }
}