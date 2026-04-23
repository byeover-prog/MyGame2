using System;
using UnityEngine;

// 방해요소 종류입니다.

public enum HazardType
{
    /// <summary>독안개 지대 — 영역 진입 시 지속 피해</summary>
    PoisonFog = 0,

    /// <summary>저주 토템 — 주변 적 버프, 파괴 가능</summary>
    CurseTotem = 1,

    /// <summary>귀화 추적체 — 느리게 추적, 접촉 시 큰 피해</summary>
    GhostFire = 2,

    /// <summary>봉인 구역 — 맵 일부 진입 불가</summary>
    SealZone = 3,

    /// <summary>혼백 폭발물 — 바닥 설치, 시간 후 폭발</summary>
    SoulBomb = 4,

    /// <summary>어둠의 장막 — 시야 제한</summary>
    DarknessVeil = 5,
}

// 한 스테이지에서 사용할 방해요소 설정입니다.
// StageDefinitionSO에 배열로 등록합니다.

[Serializable]
public sealed class HazardConfig
{
    [Tooltip("방해요소 종류입니다.")]
    public HazardType type;

    [Tooltip("방해요소가 활성화되는 게임 시간(초)입니다.")]
    [Min(0f)]
    public float activateTime;

    [Tooltip("방해요소 프리팹입니다. (독안개 영역, 토템 등)")]
    public GameObject prefab;

    [Header("수치")]
    [Tooltip("초당 피해량 / 폭발 피해량 / HP 등 방해요소별 주요 수치입니다.")]
    public float primaryValue;

    [Tooltip("생성 간격(초)입니다.")]
    [Min(1f)]
    public float spawnInterval = 30f;

    [Tooltip("동시 최대 개수입니다.")]
    [Min(1)]
    public int maxCount = 3;

    [Tooltip("지속 시간(초)입니다. 0이면 영구.")]
    [Min(0f)]
    public float duration;
}