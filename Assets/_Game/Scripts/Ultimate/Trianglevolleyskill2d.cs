using UnityEngine;

/// <summary>
/// 정삼각형 대형 VFX 웨이브를 발사하는 비주얼 전용 스킬.
/// 데미지는 Resolver에서 처리 — 이 컴포넌트는 "날아가는 연출"만 담당.
///
/// [형태]
/// 선두 1발 → 뒤에 2발 → 더 뒤에 3발
/// 빠르게 날아가면 삼각형 대형으로 보임 (애쉬 W 스타일)
///
/// [사용법]
/// Resolver에서 PlayOnce(target)를 호출하면 1웨이브 생성.
/// </summary>
[DisallowMultipleComponent]
public sealed class TriangleVolleySkill2D : MonoBehaviour
{
    [Header("웨이브 형태")]
    [Tooltip("화살 1개 VFX 프리팹 (콜라이더 없이 시각효과만)")]
    [SerializeField] private GameObject pieceVfxPrefab;

    [Tooltip("삼각형 줄 수. 3이면 선두1 + 중간2 + 후미3 = 6개")]
    [Min(1)]
    [SerializeField] private int triangleRows = 3;

    [Tooltip("좌우 화살 간격")]
    [SerializeField] private float lateralSpacing = 0.45f;

    [Tooltip("전방 간격. 0이면 정삼각형 비율 자동 계산")]
    [SerializeField] private float forwardSpacing = 0f;

    [Tooltip("웨이브 비행 속도")]
    [SerializeField] private float flightSpeed = 13f;

    [Tooltip("최대 비행 시간 (초). 이후 자동 소멸")]
    [SerializeField] private float maxFlightTime = 0.9f;

    [Tooltip("발사 시작 시 앞으로 띄울 거리")]
    [SerializeField] private float forwardSpawnOffset = 0.35f;

    [Tooltip("웨이브 전체 크기 배율")]
    [SerializeField] private float waveScale = 1f;

    /// <summary>
    /// 타겟을 향해 삼각형 대형 웨이브 1회를 발사한다.
    /// Resolver의 ResolveHit()에서 호출.
    /// </summary>
    public void PlayOnce(Vector3 origin, Transform mainTarget)
    {
        if (mainTarget == null || pieceVfxPrefab == null) return;

        Vector3 toTarget = mainTarget.position - origin;
        toTarget.z = 0f;

        Vector3 dir = toTarget.sqrMagnitude > 0.0001f
            ? toTarget.normalized
            : Vector3.right;

        Vector3 spawnPos = origin + dir * forwardSpawnOffset;

        float resolvedForward = forwardSpacing > 0f
            ? forwardSpacing
            : lateralSpacing * 0.8660254f; // sqrt(3)/2 — 정삼각형 비율

        GameObject waveObj = new GameObject("TriangleVolleyWave");
        waveObj.transform.position = spawnPos;
        waveObj.transform.localScale = Vector3.one * waveScale;

        var wave = waveObj.AddComponent<TriangleVolleyWave2D>();
        wave.Initialize(
            dir,
            pieceVfxPrefab,
            triangleRows,
            lateralSpacing,
            resolvedForward,
            flightSpeed,
            maxFlightTime
        );
    }
}