using UnityEngine;

public sealed class WeaponLoadApplier2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private WeaponDatabaseSO database;
    [SerializeField] private WeaponShooterSystem2D shooter;

    [Header("저장/로드 핫키")]
    [SerializeField] private bool enableHotkey = true;
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;

    private void Awake()
    {
        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        ApplyLoad(); // 시작 시 자동 로드
    }

    private void Update()
    {
        if (!enableHotkey) return;

        if (Input.GetKeyDown(saveKey))
            SaveCurrent();

        if (Input.GetKeyDown(loadKey))
            ApplyLoad();
    }

    public void ApplyLoad()
    {
        if (database == null || shooter == null) return;

        WeaponSaveData data = WeaponSaveSystem.Load();
        shooter.ClearSlots();

        for (int i = 0; i < data.slots.Count; i++)
        {
            var s = data.slots[i];
            if (s == null) continue;

            if (!database.TryGet(s.weaponId, out var weaponSo))
                continue;

            shooter.AddSlot(
                weaponSo,
                s.enabled,
                s.bonusDamage,
                s.cooldownMul,
                s.rangeAdd
            );
        }

        Debug.Log("[WeaponLoadApplier2D] JSON 로드 적용 완료");
    }

    public void SaveCurrent()
    {
        if (shooter == null) return;

        WeaponSaveData data = shooter.BuildSaveData();
        WeaponSaveSystem.Save(data);
    }
}