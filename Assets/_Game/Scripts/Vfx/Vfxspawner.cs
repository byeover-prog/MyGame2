// UTF-8
// Assets/_Game/Scripts/VFX/VFXSpawner.cs
// ★ VFX v2 — GPT 리뷰 #3 반영: SpawnAsChild 오프셋 보존 옵션
using UnityEngine;

/// <summary>
/// VFX를 풀링 기반으로 생성하는 static 헬퍼.
/// </summary>
public static class VFXSpawner
{
    /// <summary>월드 좌표에 고정 생성 (폭발, 화살비 등).</summary>
    public static GameObject Spawn(GameObject prefab, Vector3 pos,
        Quaternion rot, float lifetime = 2f)
    {
        if (prefab == null) return null;
        var vfx = VFXPool.Get(prefab, pos, rot);
        if (vfx == null) return null;  // 제한 초과 시 null
        Setup(vfx, prefab, lifetime);
        return vfx;
    }

    /// <summary>부모 자식으로 부착 (투사체 추종). localPosition = zero.</summary>
    public static GameObject SpawnAsChild(GameObject prefab, Transform parent,
        float lifetime = 2f)
    {
        if (prefab == null || parent == null) return null;
        var vfx = VFXPool.Get(prefab, parent.position, parent.rotation, parent);
        if (vfx == null) return null;
        Setup(vfx, prefab, lifetime);
        return vfx;
    }

    /// <summary>
    /// [GPT #3] 부모 자식으로 부착하되 프리팹의 로컬 오프셋을 보존.
    /// 총구/칼날/화염 꼬리 등 오프셋이 중요한 VFX에 사용.
    /// </summary>
    public static GameObject SpawnAsChildPreserveOffset(GameObject prefab, Transform parent,
        float lifetime = 2f)
    {
        if (prefab == null || parent == null) return null;
        var vfx = VFXPool.GetPreservingOffset(prefab, parent);
        if (vfx == null) return null;
        Setup(vfx, prefab, lifetime);
        return vfx;
    }

    private static void Setup(GameObject vfx, GameObject prefab, float lifetime)
    {
        if (vfx == null) return;
        var ar = vfx.GetComponent<VFXAutoReturn>();
        if (ar == null) ar = vfx.AddComponent<VFXAutoReturn>();
        ar.sourcePrefab = prefab;
        ar.maxLifetime = lifetime;
        // OnEnable에서 timer 리셋 + Manager 등록됨
    }
}