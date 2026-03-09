// UTF-8
// [구현 원리 요약]
// - 1순위: runtimeState에 Reset/Clear 메서드가 있으면 안전하게 호출한다.
// - 2순위: 메서드가 없을 때만, "런타임/레벨/보유" 계열로 추정되는 필드(키워드 매칭)만 제한적으로 초기화한다.
// - UnityEngine.Object(에셋/컴포넌트 참조)는 절대 비우지 않는다(프로젝트 참조 파괴 방지).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelUpRuntimeHardResetOnPlay : MonoBehaviour
{
    [Header("대상 런타임 상태(비우면 자동 탐색)")]
    [Tooltip("SkillRuntimeState 같은 런타임 상태 컴포넌트를 드래그해서 넣으세요. 비우면 자동 탐색합니다.")]
    [SerializeField] private Component runtimeState;

    [Header("자동 탐색 옵션")]
    [Tooltip("타입 이름에 아래 키워드가 포함되면 runtimeState 후보로 간주합니다.")]
    [SerializeField] private string[] typeNameContains = new[] { "skillruntimestate", "runtimestate" };

    [Header("초기화 우선순위(안전)")]
    [Tooltip("아래 메서드 이름 중 하나가 대상에 존재하면, 필드 강제 초기화 대신 해당 메서드를 호출합니다.")]
    [SerializeField] private string[] resetMethodNames = new[] { "ClearAll", "Reset", "ResetRuntime", "HardReset" };

    [Header("필드 강제 초기화 제한(안전장치)")]
    [Tooltip("필드명에 아래 키워드가 포함될 때만 초기화합니다. (런타임/레벨/보유 상태만 건드리기 위함)")]
    [SerializeField] private string[] fieldNameContains = new[] { "level", "levels", "runtime", "state", "owned", "acquire", "slot", "slots" };

    [Header("로그")]
    [Tooltip("초기화 과정 로그 출력")]
    [SerializeField] private bool log = true;

    private void Awake()
    {
        if (runtimeState == null)
            runtimeState = FindRuntimeStateCandidate();

        if (runtimeState == null)
        {
            if (log) Debug.LogWarning("[LevelUpRuntimeHardResetOnPlay] runtimeState를 찾지 못했습니다.", this);
            return;
        }

        // 1) 가장 안전: Reset/Clear 메서드가 있으면 그걸 호출
        if (TryInvokeResetMethod(runtimeState, out string invokedName))
        {
            if (log) Debug.Log($"[LevelUpRuntimeHardResetOnPlay] 메서드로 초기화 완료: {invokedName} ({runtimeState.GetType().Name})", this);
            return;
        }

        // 2) 최후 수단: 제한된 필드만 강제 초기화
        int changed = ForceZeroFieldsLimited(runtimeState);

        if (log)
            Debug.Log($"[LevelUpRuntimeHardResetOnPlay] 강제 초기화 완료: changed={changed} ({runtimeState.GetType().Name})", this);
    }

    private Component FindRuntimeStateCandidate()
    {
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;

            string tn = c.GetType().Name.ToLowerInvariant();
            for (int k = 0; k < typeNameContains.Length; k++)
            {
                var key = typeNameContains[k];
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (tn.Contains(key.ToLowerInvariant()))
                    return c;
            }
        }
        return null;
    }

    private bool TryInvokeResetMethod(Component target, out string invokedName)
    {
        invokedName = null;

        var t = target.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < resetMethodNames.Length; i++)
        {
            string name = resetMethodNames[i];
            if (string.IsNullOrWhiteSpace(name)) continue;

            var m = t.GetMethod(name, flags);
            if (m == null) continue;

            // 파라미터 없는 Reset만 허용 (안전)
            if (m.GetParameters().Length != 0) continue;

            m.Invoke(target, null);
            invokedName = name;
            return true;
        }

        return false;
    }

    private int ForceZeroFieldsLimited(object target)
    {
        int changed = 0;
        var t = target.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var f in t.GetFields(flags))
        {
            // 안전장치: 필드명 키워드 매칭이 없으면 건드리지 않음
            if (!IsFieldNameAllowed(f.Name))
                continue;

            object v = f.GetValue(target);
            if (v == null) continue;

            // 안전장치: UnityEngine.Object(컴포넌트/에셋 참조)는 절대 비우지 않음
            if (v is UnityEngine.Object)
                continue;

            // 배열류
            if (TryClearArray(v)) { changed++; continue; }

            // IList(List<>) / IDictionary(Dictionary<,>) / ISet(HashSet<> 등) / 기타 컬렉션
            if (TryClearCollections(v)) { changed++; continue; }
        }

        return changed;
    }

    private bool IsFieldNameAllowed(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;

        string fn = fieldName.ToLowerInvariant();
        for (int i = 0; i < fieldNameContains.Length; i++)
        {
            var key = fieldNameContains[i];
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (fn.Contains(key.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static bool TryClearArray(object v)
    {
        if (v is int[] ia) { Array.Clear(ia, 0, ia.Length); return true; }
        if (v is float[] fa) { Array.Clear(fa, 0, fa.Length); return true; }
        if (v is bool[] ba) { Array.Clear(ba, 0, ba.Length); return true; }

        // 그 외 타입 배열은 "레퍼런스 파괴" 위험이 있어서 클리어하지 않음
        return false;
    }

    private static bool TryClearCollections(object v)
    {
        // Dictionary / Hashtable 등
        if (v is IDictionary dict)
        {
            dict.Clear();
            return true;
        }

        // List / ArrayList 등
        if (v is IList list)
        {
            // 숫자/불리언 리스트면 0으로, 그 외는 Clear
            for (int i = 0; i < list.Count; i++)
            {
                object elem = list[i];

                if (elem is int) list[i] = 0;
                else if (elem is float) list[i] = 0f;
                else if (elem is bool) list[i] = false;
                else
                {
                    // 레퍼런스 리스트는 런타임 참조 파괴 위험 → Clear로 통일(더 안전)
                    list.Clear();
                    break;
                }
            }
            return true;
        }

        // HashSet<> 같은 ISet은 비-제네릭 인터페이스가 없음 → reflection으로 Clear() 호출
        var type = v.GetType();
        var clear = type.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        if (clear != null && clear.GetParameters().Length == 0)
        {
            // ICollection/HashSet류는 Clear만 호출
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                clear.Invoke(v, null);
                return true;
            }
        }

        return false;
    }
}