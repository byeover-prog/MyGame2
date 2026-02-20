using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/업그레이드/밸런스 테이블", fileName = "UpgradeBalanceTable")]
public sealed class UpgradeBalanceTableSO : ScriptableObject
{
    [Header("레벨 설정")]
    [Min(1)]
    [SerializeField, Tooltip("레벨 곡선의 최대 레벨(배열 길이). 기본 10 권장")]
    private int maxLevel = 10;

    [Header("기본 업그레이드 수치(레거시/초기값)")]
    [Tooltip("배열이 비어있을 때 곡선을 자동 채우는 기본값(레거시 지원)")]
    public int damageAdd = 5;

    [Tooltip("배열이 비어있을 때 곡선을 자동 채우는 기본값(레거시 지원)")]
    [Range(0.01f, 5f)]
    public float cooldownMul = 0.95f;

    [Tooltip("배열이 비어있을 때 곡선을 자동 채우는 기본값(레거시 지원)")]
    public float rangeAdd = 1f;

    [Header("레벨별 곡선(1~MaxLevel)")]
    [Tooltip("레벨 1~MaxLevel에 대응하는 값. 비어있으면 damageAdd로 자동 채움")]
    [SerializeField] private int[] damageAddByLevel;

    [Tooltip("레벨 1~MaxLevel에 대응하는 값. 비어있으면 cooldownMul로 자동 채움")]
    [SerializeField] private float[] cooldownMulByLevel;

    [Tooltip("레벨 1~MaxLevel에 대응하는 값. 비어있으면 rangeAdd로 자동 채움")]
    [SerializeField] private float[] rangeAddByLevel;

    public int MaxLevel => maxLevel;

    public int GetDamageAdd(int level)
    {
        EnsureCurves();
        return GetByLevel(damageAddByLevel, level);
    }

    public float GetCooldownMul(int level)
    {
        EnsureCurves();
        return GetByLevel(cooldownMulByLevel, level);
    }

    public float GetRangeAdd(int level)
    {
        EnsureCurves();
        return GetByLevel(rangeAddByLevel, level);
    }

    private static int GetByLevel(int[] arr, int level)
    {
        if (arr == null || arr.Length == 0) return 0;

        int idx = ToIndex(level, arr.Length);
        return arr[idx];
    }

    private static float GetByLevel(float[] arr, int level)
    {
        if (arr == null || arr.Length == 0) return 0f;

        int idx = ToIndex(level, arr.Length);
        return arr[idx];
    }

    private static int ToIndex(int level, int length)
    {
        if (length <= 0) return 0;
        int lv = Mathf.Max(1, level);          // 1-based 보정
        int idx = lv - 1;                      // 0-based 변환
        if (idx < 0) idx = 0;
        if (idx >= length) idx = length - 1;   // 범위 초과는 마지막 값
        return idx;
    }

    private void OnValidate()
    {
        if (maxLevel < 1) maxLevel = 1;

        // 길이만 맞춰두고, 값 채우기는 EnsureCurves에서 처리(레거시/초기값 자동 반영)
        damageAddByLevel = EnsureLength(damageAddByLevel, maxLevel);
        cooldownMulByLevel = EnsureLength(cooldownMulByLevel, maxLevel);
        rangeAddByLevel = EnsureLength(rangeAddByLevel, maxLevel);

        EnsureCurves();
    }

    private void EnsureCurves()
    {
        damageAddByLevel = EnsureLength(damageAddByLevel, maxLevel);
        cooldownMulByLevel = EnsureLength(cooldownMulByLevel, maxLevel);
        rangeAddByLevel = EnsureLength(rangeAddByLevel, maxLevel);

        // 레거시/초기값 기반 자동 채움(배열이 전부 0/기본값이면 채움)
        FillIfAllZero(damageAddByLevel, damageAdd);
        FillIfAllZero(cooldownMulByLevel, cooldownMul);
        FillIfAllZero(rangeAddByLevel, rangeAdd);
    }

    private static int[] EnsureLength(int[] arr, int length)
    {
        if (arr == null || arr.Length != length) return new int[length];
        return arr;
    }

    private static float[] EnsureLength(float[] arr, int length)
    {
        if (arr == null || arr.Length != length) return new float[length];
        return arr;
    }

    private static void FillIfAllZero(int[] arr, int fillValue)
    {
        if (arr == null || arr.Length == 0) return;

        bool allZero = true;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != 0) { allZero = false; break; }
        }

        if (!allZero) return;

        for (int i = 0; i < arr.Length; i++)
            arr[i] = fillValue;
    }

    private static void FillIfAllZero(float[] arr, float fillValue)
    {
        if (arr == null || arr.Length == 0) return;

        bool allZero = true;
        for (int i = 0; i < arr.Length; i++)
        {
            if (!Mathf.Approximately(arr[i], 0f)) { allZero = false; break; }
        }

        if (!allZero) return;

        for (int i = 0; i < arr.Length; i++)
            arr[i] = fillValue;
    }
}
