using UnityEngine;
using UnityEditor;

public sealed class LevelUpOfferDebugProbe : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private PlayerSkillUpgradeSystem system;

    [ContextMenu("레벨업 후보 덤프(Deck/DB/Def 유효성)")]
    private void Dump()
    {
        if (system == null) system = FindFirstObjectByType<PlayerSkillUpgradeSystem>();
        if (system == null)
        {
            Debug.LogError("[Probe] PlayerSkillUpgradeSystem을 못 찾음");
            return;
        }

        var soSys = new SerializedObject(system);
        var deck = soSys.FindProperty("deck")?.objectReferenceValue;
        var db = soSys.FindProperty("weaponDatabase")?.objectReferenceValue;

        Debug.Log($"[Probe] deck={(deck ? deck.name : "NULL")} db={(db ? db.name : "NULL")}");

        DumpWeapons("DECK", deck);
        DumpWeapons("DB", db);
    }

    private static void DumpWeapons(string label, Object obj)
    {
        if (obj == null)
        {
            Debug.LogWarning($"[Probe] {label}: NULL");
            return;
        }

        var so = new SerializedObject(obj);
        var weapons = so.FindProperty("weapons");
        if (weapons == null || !weapons.isArray)
        {
            Debug.LogWarning($"[Probe] {label}: 'weapons' 배열을 못 찾음 (SO 구조가 다름) : {obj.name}");
            return;
        }

        Debug.Log($"[Probe] {label}: weapons count={weapons.arraySize}");

        for (int i = 0; i < weapons.arraySize; i++)
        {
            var e = weapons.GetArrayElementAtIndex(i);
            var def = e.objectReferenceValue;
            if (def == null)
            {
                Debug.LogWarning($"[Probe] {label}[{i}] = NULL");
                continue;
            }

            var soDef = new SerializedObject(def);
            var weaponId = soDef.FindProperty("weaponId")?.stringValue;
            var weight = soDef.FindProperty("weight")?.intValue;
            var include = soDef.FindProperty("includeInPrototype")?.boolValue;
            var proj = soDef.FindProperty("projectilePrefab")?.objectReferenceValue;

            Debug.Log($"[Probe] {label}[{i}] {def.name} | id={weaponId} include={include} weight={weight} projectile={(proj ? proj.name : "NULL")}");
        }
    }
#endif
}