using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


public partial struct MushroomSystem : ISystem
{
    /// <summary>
    /// 엔티티와 플레이어의 거리를 판단하여 엔티티의 HoldTime을 관리하고
    /// 플레이어가 멀어지면 초기화, 필요 시간을 전부 채우면 StepIndex를 증가시키는 시스템
    /// </summary>
    /// <param name="systemState"></param>
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        float dt = SystemAPI.Time.DeltaTime;
        
        float3 pPos = SystemAPI.GetSingleton<PlayerData>().Position;
        
        // 버섯 퀘스트 ECS 순회
        foreach (var (gauge, zone, step) in 
                 SystemAPI.Query<
                         RefRW<TimeGauge>, RefRO<QuestZone>, RefRW<MushroomForaging>
                     >().WithAll<MushroomTag>())
        {
            float distSq = math.distancesq(pPos, zone.ValueRO.Center);
            ref var timeGauge = ref gauge.ValueRW;
            
            if (distSq <= zone.ValueRO.RadiusSq) // 범위 안
            {
                // HoldTime 가공
                timeGauge.HoldTime += dt;
                // 시간을 전부 채웠을 경우
                if (timeGauge.HoldTime >= gauge.ValueRO.RequiredTime)
                {
                    // 단계 상승 및 시간 초기화 로직
                    step.ValueRW.StepIndex++;
                    timeGauge.HoldTime = 0;
                }
            }
            else // 범위 밖이면 0으로 초기화
            {
                timeGauge.HoldTime = 0;
            }
        }
    }
}
