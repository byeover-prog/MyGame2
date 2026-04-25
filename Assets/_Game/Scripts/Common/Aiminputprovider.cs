using UnityEngine;

/// <summary>
/// 조준 입력 제공자입니다.
/// PC: 마우스 위치 기준 방향
/// 모바일/마우스 비활성: 가장 가까운 적 자동 조준
/// </summary>
public static class AimInputProvider
{
    /// <summary>
    /// 발사 방향(정규화 벡터)을 반환합니다.
    /// </summary>
    /// <param name="originWorldPos">발사 원점 (보통 플레이어 위치).</param>
    /// <param name="enemyMask">자동 조준 시 사용할 적 레이어.</param>
    /// <param name="autoAimRadius">자동 조준 시 적 탐색 최대 반경.</param>
    /// <returns>정규화된 방향 벡터. 적이 없거나 마우스가 같은 위치면 Vector2.right 반환.</returns>
    public static Vector2 GetAimDirection(
        Vector3 originWorldPos,
        LayerMask enemyMask,
        float autoAimRadius = 15f)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        // PC: 마우스 우선 시도
        Vector2 mouseDir = TryGetMouseAimDirection(originWorldPos);
        if (mouseDir.sqrMagnitude > 0.01f)
            return mouseDir.normalized;
#endif
        // 모바일 또는 PC에서 마우스 실패: 자동 조준
        return GetAutoAimDirection(originWorldPos, enemyMask, autoAimRadius);
    }

    /// <summary>마우스 위치 기반 조준 (PC 전용).</summary>
    private static Vector2 TryGetMouseAimDirection(Vector3 originWorldPos)
    {
        if (Camera.main == null) return Vector2.zero;

        Vector3 mouseScreen = Input.mousePosition;
        // 마우스가 화면 안에 있는지 체크
        if (mouseScreen.x < 0 || mouseScreen.y < 0 ||
            mouseScreen.x > Screen.width || mouseScreen.y > Screen.height)
            return Vector2.zero;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = originWorldPos.z;

        Vector2 dir = mouseWorld - originWorldPos;
        return dir;
    }

    /// <summary>가장 가까운 적 자동 조준.</summary>
    private static Vector2 GetAutoAimDirection(
        Vector3 originWorldPos,
        LayerMask enemyMask,
        float radius)
    {
        Vector2 origin = originWorldPos;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyMask);
        if (hits == null || hits.Length == 0)
            return Vector2.right; // 적 없으면 오른쪽으로 기본 발사

        Collider2D closest = null;
        float closestDistSqr = float.MaxValue;
        foreach (var col in hits)
        {
            if (col == null) continue;
            // 죽은 적 필터링
            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            float distSqr = ((Vector2)col.bounds.center - origin).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = col;
            }
        }

        if (closest == null)
            return Vector2.right;

        Vector2 dir = (Vector2)closest.bounds.center - origin;
        return dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
    }

    /// <summary>
    /// 4방향 (상/하/좌/우) 마우스 기준 십자 벡터를 반환합니다.
    /// 월참 각성용. 마우스 방향을 기준으로 +90, +180, +270 회전한 4개 방향.
    /// </summary>
    public static Vector2[] GetCrossDirections(
        Vector3 originWorldPos,
        LayerMask enemyMask,
        float autoAimRadius = 15f)
    {
        Vector2 baseDir = GetAimDirection(originWorldPos, enemyMask, autoAimRadius);
        Vector2 perpendicular = new Vector2(-baseDir.y, baseDir.x);

        return new Vector2[]
        {
            baseDir,                  // 정면
            -baseDir,                 // 후면
            perpendicular,            // 좌
            -perpendicular            // 우
        };
    }
}