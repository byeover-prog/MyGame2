// ============================================================
// 파일: Assets/Scripts/Enemy_Scripts/CasualSpawnCurveSO.cs
// 역할: 캐주얼 모드(시간 기반) 스폰량 구간형 커브 데이터
// - 스테이지 시작 후 경과 시간(seconds)을 입력으로 받아
//   스폰 배율(spawn_multiplier)을 반환한다.
// - "스폰량" 구현은 스폰 레이트(초당 스폰 수)에 배율을 곱하는 방식.
//   (ex: base 1.0 * 1.5 = 초당 1.5마리)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/Mode/Casual Spawn Curve", fileName = "CasualSpawnCurveSO")]
public sealed class CasualSpawnCurveSO : ScriptableObject
{
    [Serializable]
    public struct Segment
    {
        [Tooltip("이 구간이 시작되는 시간(초). 예: 0")]
        public float start_seconds;

        [Tooltip("이 구간이 끝나는 시간(초). 끝이 없으면 -1")]
        public float end_seconds;

        [Tooltip("스폰 배율. 1 = 기본, 1.5 = 50% 더 자주")]
        public float spawn_multiplier;
    }

    [Header("구간 목록(오름차순 추천)")]
    [SerializeField]
    private List<Segment> segments = new List<Segment>
    {
        new Segment{ start_seconds = 0f,  end_seconds = 60f,  spawn_multiplier = 1.0f },
        new Segment{ start_seconds = 60f, end_seconds = 180f, spawn_multiplier = 1.2f },
        new Segment{ start_seconds = 180f,end_seconds = -1f,  spawn_multiplier = 1.5f },
    };

    [Header("예외/방어")]
    [Tooltip("구간이 비어있거나 매칭 실패 시 기본 배율")]
    [Min(0.01f)]
    [SerializeField] private float fallback_multiplier = 1.0f;

    public float EvaluateMultiplier(float elapsed_seconds)
    {
        if (segments == null || segments.Count == 0)
            return fallback_multiplier;

        if (elapsed_seconds < 0f) elapsed_seconds = 0f;

        for (int i = 0; i < segments.Count; i++)
        {
            Segment s = segments[i];
            float start = s.start_seconds;
            float end = s.end_seconds;

            bool in_start = elapsed_seconds >= start;
            bool in_end = (end < 0f) || (elapsed_seconds < end); // end=-1이면 무한

            if (in_start && in_end)
                return Mathf.Max(0.01f, s.spawn_multiplier);
        }

        // 어떤 구간도 못 찾으면 마지막 구간 또는 fallback 사용
        Segment last = segments[segments.Count - 1];
        if (last.end_seconds < 0f && elapsed_seconds >= last.start_seconds)
            return Mathf.Max(0.01f, last.spawn_multiplier);

        return fallback_multiplier;
    }
}
