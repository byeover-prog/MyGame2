using System.IO;
using UnityEngine;

/// <summary>
/// JSON 저장/로드 (변화값만 저장)
/// - SO는 정의 데이터, JSON은 강화/해금/장착 상태
/// </summary>
public sealed class WeaponSaveSystem : MonoBehaviour
{
    private const string FileName = "weapon_save.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(WeaponSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        GameLogger.Log($"[WeaponSaveSystem] Saved: {SavePath}");
    }

    public static WeaponSaveData Load()
    {
        if (!File.Exists(SavePath))
            return new WeaponSaveData();

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<WeaponSaveData>(json);
        return data ?? new WeaponSaveData();
    }
}
