using Unity.Entities;
using UnityEngine;

/// <summary>
/// 전역적으로 ECS 월드 및 매니져를 참조하기 위한 코어 클래스
/// </summary>
public static class ECSCore
{
    private static EntityManager _em;

    public static EntityManager EM
    {
        get
        {
            var world = World.DefaultGameObjectInjectionWorld;
            
            if (world == null || !world.IsCreated)
            {
                return default;
            }

            if (_em == default)
            {
                _em = world.EntityManager;
            }

            return _em;
        }
    }
    
    // 에디터 도메인 리로드 대응
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _em = default;
    }
}
