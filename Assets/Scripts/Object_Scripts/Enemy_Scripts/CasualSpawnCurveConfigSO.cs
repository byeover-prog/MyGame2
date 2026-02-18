using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/Mode/Casual Spawn Curve", fileName = "CasualSpawnCurveConfigSO")]
public sealed class CasualSpawnCurveConfigSO : ScriptableObject
{
    [Serializable]
    public struct Segment
    {
        public float start_seconds;
        public float end_seconds;      // 끝 없으면 -1
        public float spawn_multiplier; // 1=기본
    }

    [SerializeField] private List<Segment> segments = new List<Segment>();
    [Min(0.01f)][SerializeField] private float fallback_multiplier = 1.0f;

    public float EvaluateMultiplier(float elapsed_seconds)
    {
        if (segments == null || segments.Count == 0) return fallback_multiplier;
        if (elapsed_seconds < 0f) elapsed_seconds = 0f;

        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            bool in_start = elapsed_seconds >= s.start_seconds;
            bool in_end = (s.end_seconds < 0f) || (elapsed_seconds < s.end_seconds);

            if (in_start && in_end)
                return Mathf.Max(0.01f, s.spawn_multiplier);
        }

        var last = segments[segments.Count - 1];
        if (last.end_seconds < 0f && elapsed_seconds >= last.start_seconds)
            return Mathf.Max(0.01f, last.spawn_multiplier);

        return fallback_multiplier;
    }
}
