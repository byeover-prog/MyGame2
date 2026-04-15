using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 퀘스트를 표현하기 위한 컴포넌트 구조체를 전부 한 파일에서 작성합니다.
/// 추후 알아보기 힘들어진다면 퀘스트 종류 단위로 구분해야할 수 있습니다.
/// </summary>

// 퀘스트 공통 컴포넌트
public struct QuestBase : IComponentData
{
    public int QuestId;
    public float Progress;
}

// 위치 정보 및 원형 범위 판정용 컴포넌트
public struct QuestZone : IComponentData
{
    // 중심점 (float3는 Entities 패키지에 딸려있는건데 vector3보다 계산 효율이 좋다내요)
    public float3 Center;
    // 판정용 반경의 제곱 (제곱 그대로 쓰는게 계산이 빠르다고 합니다)
    // SO에서는 반지름으로 데이터를 넣고 주입 시 제곱해서 할당합니다
    public float RadiusSq;
}

//---- 필요 UI 엔티티-----
// 방향 지시 화살표 제어용 컴포넌트
public struct QuestIndicator : IComponentData
{
    // 화살표 표시를 결정하는 기준 거리
    public float ShowThreshold;
    // 생성된 화살표 엔티티 참조
    public Entity IndicatorEntity;
}
// 0.0 ~ 1.0 사이의 진행 상태를 공유하기 위한 데이터
public struct QuestProgress : IComponentData
{
    public float Value;
}

// 퀘스트 별 컴포넌트 - QuestModuleSo를 통해 엔티티에 추가
// -----영혼 수확-----
// 범위 내 적 처치 수 카운트
public struct KillCount : IComponentData
{
    public int CurrentKillCount;
    public int TargetKillCount;
}

// ----신비한 버섯----
public struct MushroomTag : IComponentData { }
// 버섯 채집 단계 관리
public struct MushroomForaging : IComponentData
{
    public int StepIndex; //0, 1, 2 2에서 성공시 퀘스트 달성
}
// 시간 채우기
public struct TimeGauge : IComponentData
{
    // 목표 시간
    public float RequiredTime; 
    // 현재 누적 시간
    public float HoldTime;     
}
