using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 세션에서 관리되는 데이터를 ecs 데이터로 공유하기 위한 컴포넌트 입니다
/// Bridge 에서 필요한 객체들을 참조하고 데이터를 갱신하여 ecs에 보냅니다
/// </summary>
//세션 내 시간 정보
public struct SessionTimeData : IComponentData
{
    public float Time;
}
// 세션 내 플레이어 정보    
public struct PlayerData : IComponentData
{
    public float3 Position;
}