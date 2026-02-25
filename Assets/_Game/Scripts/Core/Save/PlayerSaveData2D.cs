using System;
using UnityEngine;

[Serializable]
public sealed class PlayerSaveData2D
{
    [Header("버전")]
    [Tooltip("세이브 데이터 버전. 필드 구조가 바뀌면 올린다.")]
    public int version = 1;

    [Header("진행도")]
    [Tooltip("누적 플레이 시간(초)")]
    public float totalPlaySeconds = 0f;

    [Tooltip("최고 레벨(예시)")]
    public int bestLevel = 1;

    [Header("밸런스/옵션")]
    [Tooltip("다크오브 투명도(0~1). 옵션 저장 예시")]
    [Range(0f, 1f)]
    public float darkOrbAlpha = 0.75f;

    [Header("마지막 저장 시각(디버그)")]
    public long lastSavedUnixMs = 0;

    public static PlayerSaveData2D CreateDefault()
    {
        return new PlayerSaveData2D
        {
            version = 1,
            totalPlaySeconds = 0f,
            bestLevel = 1,
            darkOrbAlpha = 0.75f,
            lastSavedUnixMs = 0
        };
    }
}