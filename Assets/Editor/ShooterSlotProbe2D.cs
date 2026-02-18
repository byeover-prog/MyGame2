#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ShooterSlotProbe2D
{
    [MenuItem("Tools/그날이후/진단/WeaponShooterSystem2D 슬롯 구조 덤프(상세)")]
    public static void DumpDetail()
    {
        var shooter = Object.FindFirstObjectByType<WeaponShooterSystem2D>();
        if (shooter == null)
        {
            Debug.LogError("[ShooterSlotProbe2D] 씬에 WeaponShooterSystem2D가 없습니다.");
            return;
        }

        var soShooter = new SerializedObject(shooter);
        var pSlots = soShooter.FindProperty("slots");

        Debug.Log("==== [ShooterSlotProbe2D] 시작(상세) ====");
        Debug.Log($"Shooter GameObject: {shooter.gameObject.name}");

        if (pSlots == null || !pSlots.isArray)
        {
            Debug.LogError("[ShooterSlotProbe2D] 'slots' 배열을 찾지 못했습니다(필드명이 다를 수 있음).");
            Debug.Log("==== [ShooterSlotProbe2D] 종료(상세) ====");
            return;
        }

        Debug.Log($"slots size={pSlots.arraySize}");

        if (pSlots.arraySize <= 0)
        {
            Debug.LogWarning("[ShooterSlotProbe2D] slots가 비어있습니다.");
            Debug.Log("==== [ShooterSlotProbe2D] 종료(상세) ====");
            return;
        }

        // slots[0] 요소 내부 프로퍼티를 전부 출력
        var elem = pSlots.GetArrayElementAtIndex(0);
        if (elem == null)
        {
            Debug.LogWarning("[ShooterSlotProbe2D] slots[0] element가 null입니다.");
            Debug.Log("==== [ShooterSlotProbe2D] 종료(상세) ====");
            return;
        }

        DumpChildren(elem);

        Debug.Log("==== [ShooterSlotProbe2D] 종료(상세) ====");
    }

    private static void DumpChildren(SerializedProperty element)
    {
        Debug.Log($"[DumpChildren] elementPath='{element.propertyPath}', type='{element.propertyType}'");

        // element 자체가 primitive면 거기서 끝
        if (element.propertyType == SerializedPropertyType.String)
        {
            Debug.Log($"- (string) {element.propertyPath} = '{element.stringValue}'");
            return;
        }
        if (element.propertyType == SerializedPropertyType.Integer)
        {
            Debug.Log($"- (int) {element.propertyPath} = {element.intValue}");
            return;
        }
        if (element.propertyType == SerializedPropertyType.Boolean)
        {
            Debug.Log($"- (bool) {element.propertyPath} = {element.boolValue}");
            return;
        }
        if (element.propertyType == SerializedPropertyType.ObjectReference)
        {
            var obj = element.objectReferenceValue;
            Debug.Log($"- (objref) {element.propertyPath} = {(obj ? obj.name : "null")}");
            return;
        }

        // 복합 타입이면 자식들을 전부 순회
        var copy = element.Copy();
        var end = copy.GetEndProperty();
        bool enter = true;

        while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
        {
            enter = true;

            switch (copy.propertyType)
            {
                case SerializedPropertyType.String:
                    Debug.Log($"- (string) {copy.propertyPath} = '{copy.stringValue}'");
                    break;

                case SerializedPropertyType.Integer:
                    Debug.Log($"- (int) {copy.propertyPath} = {copy.intValue}");
                    break;

                case SerializedPropertyType.Boolean:
                    Debug.Log($"- (bool) {copy.propertyPath} = {copy.boolValue}");
                    break;

                case SerializedPropertyType.Float:
                    Debug.Log($"- (float) {copy.propertyPath} = {copy.floatValue}");
                    break;

                case SerializedPropertyType.Enum:
                    Debug.Log($"- (enum) {copy.propertyPath} = '{copy.enumNames[copy.enumValueIndex]}'");
                    break;

                case SerializedPropertyType.ObjectReference:
                    var obj = copy.objectReferenceValue;
                    Debug.Log($"- (objref) {copy.propertyPath} = {(obj ? obj.name : "null")}");
                    break;
            }
        }
    }
}
#endif
