// ============================================================================
// EnemyRegistryExtensions.cs
// 경로: Assets/_Game/Scripts/Common/EnemyRegistryExtensions.cs
//
// CentralProjectileManager의 호밍/부메랑 타겟 추적에 필요한
// InstanceID → GameObject 역참조 기능을 추가합니다.
//
// [기존 EnemyRegistry2D.cs 수정 없이 확장]
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyRegistry2D에 InstanceID 기반 조회를 추가하는 확장 클래스.
/// </summary>
public static class EnemyRegistryExtensions
{
    // 캐시: InstanceID → Member (Register/Unregister 시 갱신)
    private static readonly Dictionary<int, EnemyRegistryMember2D> _idMap
        = new Dictionary<int, EnemyRegistryMember2D>(256);

    private static bool _hooked;

    /// <summary>
    /// 부팅 시 1회 호출. EnemyRegistry2D의 기존 Register/Unregister에
    /// 후킹하여 _idMap을 동기화합니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoHook()
    {
        if (_hooked) return;
        _hooked = true;

        // 씬 전환 시 초기화
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += _ => _idMap.Clear();
    }

    /// <summary>
    /// 적 등록 시 호출. EnemyRegistryMember2D.OnEnable에서 호출하세요.
    /// </summary>
    public static void RegisterById(EnemyRegistryMember2D member)
    {
        if (member == null) return;
        _idMap[member.gameObject.GetInstanceID()] = member;
    }

    /// <summary>
    /// 적 해제 시 호출. EnemyRegistryMember2D.OnDisable에서 호출하세요.
    /// </summary>
    public static void UnregisterById(EnemyRegistryMember2D member)
    {
        if (member == null) return;
        _idMap.Remove(member.gameObject.GetInstanceID());
    }

    /// <summary>
    /// InstanceID로 적을 조회합니다. O(1).
    /// </summary>
    public static bool TryGetById(int instanceId, out EnemyRegistryMember2D result)
    {
        return _idMap.TryGetValue(instanceId, out result);
    }
}