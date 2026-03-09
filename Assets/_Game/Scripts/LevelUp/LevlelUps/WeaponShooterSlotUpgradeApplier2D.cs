using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public sealed class WeaponShooterSlotUpgradeApplier2D : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private WeaponShooterSystem2D shooter;

    [Header("슬롯 제한")]
    [SerializeField, Tooltip("발시 슬롯 인덱스(제외 대상)")]
    private int balsiSlotIndex = 0;

    [SerializeField, Tooltip("발시를 제외한 활성 스킬 최대 개수")]
    private int maxEnabledExceptBalsi = 8;

    private FieldInfo _slotsField;

    private void Awake()
    {
        if (shooter == null)
            shooter = FindFirstObjectByType<WeaponShooterSystem2D>();

        CacheReflection();
    }

    private void CacheReflection()
    {
        if (shooter == null) return;

        var t = shooter.GetType();
        _slotsField = t.GetField("slots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_slotsField == null)
            Debug.LogError("[WeaponShooterSlotUpgradeApplier2D] shooter에서 'slots' 필드를 찾지 못했습니다.");
    }

    public int GetEnabledCountExceptBalsi()
    {
        if (!TryGetSlotsList(out IList list)) return 0;

        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (i == balsiSlotIndex) continue;

            object elem = list[i];
            if (elem == null) continue;

            bool enabled = TryGetBool(elem, "enabled", false);
            if (enabled) count++;
        }

        return count;
    }

    public bool CanEnableSlot(int slotIndex)
    {
        if (slotIndex < 0) return false;

        if (slotIndex == balsiSlotIndex)
            return true;

        if (!TryGetSlotElement(slotIndex, out object elem, out IList list))
            return false;

        bool alreadyEnabled = TryGetBool(elem, "enabled", false);
        if (alreadyEnabled) return true;

        int enabledCount = GetEnabledCountExceptBalsi();
        return enabledCount < maxEnabledExceptBalsi;
    }

    public bool Apply(WeaponUpgradeCardSO card)
    {
        if (card == null || !card.IsValid()) return false;
        if (shooter == null || _slotsField == null) return false;

        int i = card.slotIndex;
        if (!TryGetSlotElement(i, out object elem, out IList list)) return false;

        bool changed = false;

        // 활성화 카드 제한(발시 제외 8개)
        if (card.type == WeaponUpgradeType.ToggleEnabled && card.value.toggleBool)
        {
            if (!CanEnableSlot(i))
                return false;
        }

        switch (card.type)
        {
            // 슬롯 기본 강화(최상위 필드)
            case WeaponUpgradeType.ToggleEnabled:
                changed |= TrySetBool(ref elem, "enabled", card.value.toggleBool);
                break;

            case WeaponUpgradeType.DamageAdd:
                changed |= AddInt(ref elem, "bonusDamage", card.value.addInt);
                break;

            case WeaponUpgradeType.CooldownMul:
                changed |= MulFloat(ref elem, "cooldownMul", SafeMul(card.value.mulFloat));
                break;

            case WeaponUpgradeType.RangeAdd:
                changed |= AddFloat(ref elem, "rangeAdd", card.value.addFloat);
                break;

            // upgradeState 하위 강화(중첩 필드)
            case WeaponUpgradeType.UpgradeStateAddDamage:
                changed |= AddIntNested(ref elem, "upgradeState", "addDamage", card.value.addInt);
                break;

            case WeaponUpgradeType.UpgradeStateAddRange:
                changed |= AddFloatNested(ref elem, "upgradeState", "addRange", card.value.addFloat);
                break;

            case WeaponUpgradeType.UpgradeStateAddProjectileSpeed:
                changed |= AddFloatNested(ref elem, "upgradeState", "addProjectileSpeed", card.value.addFloat);
                break;

            case WeaponUpgradeType.UpgradeStateAddLifetime:
                changed |= AddFloatNested(ref elem, "upgradeState", "addLifetime", card.value.addFloat);
                break;

            // mul 계열은 기본값 0을 전제로 "추가 배율" 누적(가산)로 처리한다.
            // 실제 발사 계산에서 (1 + mulFireRate) 같은 식으로 해석하는 쪽이 안전하다.
            case WeaponUpgradeType.UpgradeStateMulFireRateAdd:
                changed |= AddFloatNested(ref elem, "upgradeState", "mulFireRate", card.value.addFloat);
                break;

            case WeaponUpgradeType.UpgradeStateMulAreaAdd:
                changed |= AddFloatNested(ref elem, "upgradeState", "mulArea", card.value.addFloat);
                break;

            case WeaponUpgradeType.UpgradeStateMulKnockbackAdd:
                changed |= AddFloatNested(ref elem, "upgradeState", "mulKnockback", card.value.addFloat);
                break;

            case WeaponUpgradeType.UpgradeStateAddPierce:
                changed |= AddIntNested(ref elem, "upgradeState", "addPierce", card.value.addInt);
                break;

            case WeaponUpgradeType.UpgradeStateAddSplit:
                changed |= AddIntNested(ref elem, "upgradeState", "addSplit", card.value.addInt);
                break;

            case WeaponUpgradeType.UpgradeStateAddShotCount:
                changed |= AddIntNested(ref elem, "upgradeState", "addShotCount", card.value.addInt);
                break;

            case WeaponUpgradeType.UpgradeStateEnableHoming:
                changed |= TrySetBoolNested(ref elem, "upgradeState", "enableHoming", card.value.toggleBool);
                break;

            case WeaponUpgradeType.UpgradeStateEnableBoomerang:
                changed |= TrySetBoolNested(ref elem, "upgradeState", "enableBoomerang", card.value.toggleBool);
                break;

            case WeaponUpgradeType.UpgradeStateEnableRotate:
                changed |= TrySetBoolNested(ref elem, "upgradeState", "enableRotate", card.value.toggleBool);
                break;
        }

        if (changed)
        {
            // slot이 struct일 수 있으므로 되돌려쓰기 필수
            list[i] = elem;
        }

        return changed;
    }

    private bool TryGetSlotsList(out IList list)
    {
        list = null;
        if (shooter == null || _slotsField == null) return false;

        object slotsObj = _slotsField.GetValue(shooter);
        list = slotsObj as IList;
        return list != null;
    }

    private bool TryGetSlotElement(int slotIndex, out object elem, out IList list)
    {
        elem = null;
        list = null;

        if (!TryGetSlotsList(out list)) return false;
        if (slotIndex < 0 || slotIndex >= list.Count) return false;

        elem = list[slotIndex];
        return elem != null;
    }

    private static float SafeMul(float mul)
    {
        return (mul <= 0f) ? 1f : mul;
    }

    private static bool AddInt(ref object obj, string fieldName, int delta)
    {
        int cur = TryGetInt(obj, fieldName, 0);
        return TrySetInt(ref obj, fieldName, cur + delta);
    }

    private static bool AddFloat(ref object obj, string fieldName, float delta)
    {
        float cur = TryGetFloat(obj, fieldName, 0f);
        return TrySetFloat(ref obj, fieldName, cur + delta);
    }

    private static bool MulFloat(ref object obj, string fieldName, float mul)
    {
        float cur = TryGetFloat(obj, fieldName, 1f);
        return TrySetFloat(ref obj, fieldName, cur * mul);
    }

    private static bool AddIntNested(ref object obj, string parentField, string childField, int delta)
    {
        object nested = TryGetObjectField(obj, parentField);
        if (nested == null) return false;

        int cur = TryGetInt(nested, childField, 0);
        bool ok = TrySetIntOnObject(nested, childField, cur + delta);
        if (!ok) return false;

        return TrySetObjectField(ref obj, parentField, nested);
    }

    private static bool AddFloatNested(ref object obj, string parentField, string childField, float delta)
    {
        object nested = TryGetObjectField(obj, parentField);
        if (nested == null) return false;

        float cur = TryGetFloat(nested, childField, 0f);
        bool ok = TrySetFloatOnObject(nested, childField, cur + delta);
        if (!ok) return false;

        return TrySetObjectField(ref obj, parentField, nested);
    }

    private static bool TrySetBoolNested(ref object obj, string parentField, string childField, bool value)
    {
        object nested = TryGetObjectField(obj, parentField);
        if (nested == null) return false;

        bool ok = TrySetBoolOnObject(nested, childField, value);
        if (!ok) return false;

        return TrySetObjectField(ref obj, parentField, nested);
    }

    private static object TryGetObjectField(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return null;
        return f.GetValue(obj);
    }

    private static bool TrySetObjectField(ref object obj, string fieldName, object value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static int TryGetInt(object obj, string fieldName, int fallback)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(int)) return fallback;
        return (int)f.GetValue(obj);
    }

    private static float TryGetFloat(object obj, string fieldName, float fallback)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(float)) return fallback;
        return (float)f.GetValue(obj);
    }

    private static bool TryGetBool(object obj, string fieldName, bool fallback)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return fallback;
        return (bool)f.GetValue(obj);
    }

    private static bool TrySetInt(ref object obj, string fieldName, int value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(int)) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static bool TrySetFloat(ref object obj, string fieldName, float value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(float)) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static bool TrySetBool(ref object obj, string fieldName, bool value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static bool TrySetIntOnObject(object obj, string fieldName, int value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(int)) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static bool TrySetFloatOnObject(object obj, string fieldName, float value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(float)) return false;
        f.SetValue(obj, value);
        return true;
    }

    private static bool TrySetBoolOnObject(object obj, string fieldName, bool value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return false;
        f.SetValue(obj, value);
        return true;
    }
}
