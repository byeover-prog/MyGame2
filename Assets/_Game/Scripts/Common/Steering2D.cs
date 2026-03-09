// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - Lerp(t)로 방향을 섞으면 프레임 조건에 따라 "즉시 스냅"이 나올 수 있다.
// - 이 유틸은 "초당 turnSpeedDeg" 만큼만 방향을 꺾어, 유도 떨림/박힘을 줄인다.
public static class Steering2D
{
    public static Vector2 TurnTowards(Vector2 currentDir, Vector2 desiredDir, float turnSpeedDeg, float deltaTime)
    {
        if (currentDir.sqrMagnitude < 0.0001f) currentDir = Vector2.right;
        if (desiredDir.sqrMagnitude < 0.0001f) return currentDir.normalized;

        currentDir = currentDir.normalized;
        desiredDir = desiredDir.normalized;

        float maxDeg = Mathf.Max(0f, turnSpeedDeg) * Mathf.Max(0f, deltaTime);

        float signed = Vector2.SignedAngle(currentDir, desiredDir);
        float clamped = Mathf.Clamp(signed, -maxDeg, +maxDeg);

        float rad = clamped * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);

        return new Vector2(
            currentDir.x * cs - currentDir.y * sn,
            currentDir.x * sn + currentDir.y * cs
        ).normalized;
    }
}