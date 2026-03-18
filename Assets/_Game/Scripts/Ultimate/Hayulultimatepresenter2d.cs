// UTF-8
// Assets/_Game/Scripts/Ultimate/Hayul/HayulUltimatePresenter2D.cs
using UnityEngine;

/// <summary>
/// 하율 궁극기 "천강뇌전부" — 연출(Presenter) 담당.
///
/// [v4 수정 사항]
/// 1. 부적 VFX(Talisman) 관련 필드/로직 전부 제거
/// 2. 풀스크린 VFX를 카메라 뷰포트 전체에 꽉 채움
///    → 카메라 orthographicSize + aspect ratio로 뷰포트 크기 계산
///    → VFX를 카메라 자식으로 부착 + 뷰포트에 맞게 스케일 조정
/// 3. 데미지 판정은 HitResolver가 플레이어 중심 고정 반경으로 처리 (여기선 안 건드림)
///
/// [핵심 설계]
/// - 이펙트 = 카메라 뷰포트 전체를 뒤덮음 (모니터 크기에 맞춰 연출)
/// - 데미지 = 플레이어 중심 고정 반경 (모니터 무관, HitResolver 담당)
/// </summary>
public class HayulUltimatePresenter2D : MonoBehaviour
{
    [Header("전체 연출 VFX")]
    [Tooltip("화면 전체를 덮는 궁극기 연출 VFX 프리팹 (ULT_하율)")]
    [SerializeField] private GameObject fullscreenVfxPrefab;

    [Header("VFX 스케일 설정")]
    [Tooltip("VFX 프리팹이 Scale(1,1,1)일 때 커버하는 월드 크기.\n" +
             "예: 프리팹이 10×10 범위를 채운다면 10.\n" +
             "이 값을 기준으로 카메라 뷰포트에 맞게 자동 스케일링된다.")]
    [SerializeField] private float vfxOriginalWorldSize = 10f;

    [Tooltip("스케일 여유 배수. 1.2 = 카메라보다 20% 크게 (가장자리 빈틈 방지)")]
    [SerializeField] private float scaleMargin = 1.2f;

    [Header("카메라 참조 (연출용)")]
    [Tooltip("메인 카메라. 비워두면 Camera.main 자동 탐색.")]
    [SerializeField] private Camera mainCamera;

    private GameObject _currentVfx;

    /// <summary>
    /// 궁극기 연출 시작. Executor에서 호출.
    /// VFX를 카메라 자식으로 붙이고, 카메라 뷰포트 전체를 꽉 채우도록 스케일링.
    /// </summary>
    public void BeginPresentation(float duration)
    {
        EnsureCameraRef();

        if (fullscreenVfxPrefab == null)
        {
            Debug.LogWarning("[하율 궁극기] Fullscreen Vfx Prefab이 비어있습니다!");
            return;
        }
        if (mainCamera == null)
        {
            Debug.LogWarning("[하율 궁극기] 카메라를 찾을 수 없습니다!");
            return;
        }

        // ── 카메라 뷰포트 월드 크기 계산 (Orthographic 기준) ──
        float viewHeight = mainCamera.orthographicSize * 2f;
        float viewWidth  = viewHeight * mainCamera.aspect;

        // 뷰포트의 긴 쪽 기준으로 스케일 결정
        // (VFX가 정사각형이든 직사각형이든 화면 전체를 덮도록)
        float viewMax = Mathf.Max(viewWidth, viewHeight);
        float scaleFactor = (viewMax / vfxOriginalWorldSize) * scaleMargin;

        // ── VFX 생성 (카메라 위치, z=0) ──
        Vector3 camPos = mainCamera.transform.position;
        Vector3 spawnPos = new Vector3(camPos.x, camPos.y, 0f);

        _currentVfx = Instantiate(fullscreenVfxPrefab, spawnPos, Quaternion.identity);

        // 카메라 자식으로 부착 (카메라가 움직여도 VFX가 따라감)
        _currentVfx.transform.SetParent(mainCamera.transform, true);

        // 뷰포트에 맞게 스케일 적용
        _currentVfx.transform.localScale = Vector3.one * scaleFactor;

        Destroy(_currentVfx, duration + 1f);

        Debug.Log($"[하율 궁극기] 풀스크린 VFX 생성 | " +
                  $"viewSize=({viewWidth:F1}×{viewHeight:F1}) " +
                  $"scale={scaleFactor:F2} duration={duration}s");
    }

    /// <summary>
    /// 궁극기 연출 종료. Executor에서 호출.
    /// </summary>
    public void EndPresentation()
    {
        if (_currentVfx != null)
        {
            _currentVfx.transform.SetParent(null);
            Destroy(_currentVfx);
            _currentVfx = null;
        }
        Debug.Log("[하율 궁극기] Presenter 종료");
    }

    private void Awake() => EnsureCameraRef();

    private void EnsureCameraRef()
    {
        if (mainCamera != null) return;
        mainCamera = Camera.main;
    }
}