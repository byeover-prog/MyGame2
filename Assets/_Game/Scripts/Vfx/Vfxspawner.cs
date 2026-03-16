using UnityEngine;

/// <summary>
/// VFX를 풀링 기반으로 생성하는 static 헬퍼.
/// </summary>
public static class VFXSpawner
{
    /// <summary>월드 좌표에 고정 생성 (폭발, 화살비 등).</summary>
    public static GameObject Spawn(GameObject prefab, Vector3 pos,
        Quaternion rot, float lifetime = 3f)
    {
        if (prefab == null) return null;
        var vfx = VFXPool.Get(prefab, pos, rot);
        Setup(vfx, prefab, lifetime);
        return vfx;
    }

    /// <summary>부모 자식으로 부착 (투사체 추종).</summary>
    public static GameObject SpawnAsChild(GameObject prefab, Transform parent,
        float lifetime = 5f)
    {
        if (prefab == null || parent == null) return null;
        var vfx = VFXPool.Get(prefab, parent.position, parent.rotation, parent);
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
    }
}