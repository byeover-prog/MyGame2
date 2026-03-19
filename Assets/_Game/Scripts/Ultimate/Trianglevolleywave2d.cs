using UnityEngine;

/// <summary>
/// 삼각형 편대 웨이브 1회 비주얼.
/// 데미지/히트 판정 없음 — 날아가고 시간 지나면 소멸.
///
/// [배치 원리]
/// row 0 → 1개 (선두)
/// row 1 → 2개 (좌우 벌어짐)
/// row 2 → 3개 (더 벌어짐)
/// 루트를 타겟 방향으로 회전 → 자식 로컬 배치 유지 → 형태 안 무너짐
/// </summary>
public sealed class TriangleVolleyWave2D : MonoBehaviour
{
    private Vector3 _direction;
    private float _flightSpeed;
    private float _maxFlightTime;
    private float _aliveTime;
    private bool _initialized;

    public void Initialize(
        Vector3 direction,
        GameObject piecePrefab,
        int triangleRows,
        float lateralSpacing,
        float forwardSpacing,
        float flightSpeed,
        float maxFlightTime)
    {
        _direction = direction.normalized;
        _flightSpeed = Mathf.Max(0.01f, flightSpeed);
        _maxFlightTime = Mathf.Max(0.05f, maxFlightTime);

        BuildPieces(piecePrefab, Mathf.Max(1, triangleRows),
                    Mathf.Max(0.01f, lateralSpacing),
                    Mathf.Max(0.01f, forwardSpacing));

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        _aliveTime += Time.deltaTime;

        if (_aliveTime >= _maxFlightTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * (_flightSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 정삼각형 형태로 VFX 조각을 로컬 배치한다.
    /// 선두(row 0)가 가장 앞, 뒤로 갈수록 넓어짐.
    /// </summary>
    private void BuildPieces(GameObject prefab, int rows, float lateral, float forward)
    {
        // +X를 전방으로 삼고 타겟 방향으로 회전
        float angleDeg = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        for (int row = 0; row < rows; row++)
        {
            int countInRow = row + 1;

            // 선두가 앞이니까 row 0 = X:0, row 1 = X:-forward, row 2 = X:-forward*2
            // (뒤로 갈수록 -X 방향)
            float localX = -row * forward;
            float rowWidth = (countInRow - 1) * lateral;
            float startY = -rowWidth * 0.5f;

            for (int i = 0; i < countInRow; i++)
            {
                float localY = startY + (i * lateral);

                GameObject piece = Instantiate(prefab, transform);
                piece.name = $"Arrow_r{row}_i{i}";
                piece.transform.localPosition = new Vector3(localX, localY, 0f);
                piece.transform.localRotation = Quaternion.identity;
            }
        }
    }
}