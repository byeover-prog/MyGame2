using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "[그날이후]/무기 DB(WeaponDatabase)", fileName = "WeaponDatabase")]
public sealed class WeaponDatabaseSO : ScriptableObject
{
    [Tooltip("게임에서 사용할 무기 정의(SO) 목록")]
    public List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    private Dictionary<string, WeaponDefinitionSO> _map;

    public bool TryGet(string weaponId, out WeaponDefinitionSO weapon)
    {
        if (_map == null) Build();
        return _map.TryGetValue(weaponId, out weapon);
    }

    private void OnEnable()
    {
        // 에디터/런타임 모두에서 안전하게 재구축되도록 초기화
        _map = null;
    }

    private void Build()
    {
        int capacity = (weapons != null) ? weapons.Count : 0;
        _map = new Dictionary<string, WeaponDefinitionSO>(capacity);

        if (weapons == null) return;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDefinitionSO w = weapons[i];
            if (w == null) continue;

            string id = w.weaponId;
            if (string.IsNullOrWhiteSpace(id)) continue;

            // 중복 방지(중복이면 첫 번째 항목을 유지)
            if (!_map.ContainsKey(id))
                _map.Add(id, w);
        }
    }
}