// UTF-8
using UnityEngine;

/// <summary>
/// 공용 궁극기 연출 담당.
/// UltimateDataSO에서 VFX 프리팹을 읽어서 카메라/플레이어에 부착.
/// 모든 캐릭터가 이 1개 클래스를 공용으로 사용한다.
///
/// [Hierarchy 위치]
/// Player 오브젝트 아래 자식으로 배치.
///
/// [Inspector]
/// - Main Camera: 비워두면 Camera.main 자동 탐색
/// - Player Transform: 비워두면 "Player" 태그 자동 탐색
/// </summary>
[DisallowMultipleComponent]
public sealed class UltimatePresenter2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("메인 카메라. 비워두면 Camera.main 자동 탐색.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("플레이어 Transform. 비워두면 'Player' 태그로 자동 탐색.")]
    [SerializeField] private Transform playerTransform;

    // ── 런타임 ──
    private GameObject _currentFullscreenVfx;
    private GameObject _currentAuraVfx;

    /// <summary>
    /// 궁극기 연출 시작. Executor에서 호출.
    /// SO에서 VFX 프리팹과 스케일 정보를 읽는다.
    /// </summary>
    public void BeginPresentation(UltimateDataSO data, float duration)
    {
        EndPresentation();  // 이전 연출이 남아있으면 정리

        EnsureRefs();

        if (data == null)
        {
            GameLogger.LogWarning("[궁극기 연출] UltimateDataSO가 null입니다!");
            return;
        }

        // ── 풀스크린 VFX (카메라 부착) ──
        if (data.FullscreenVfxPrefab != null && mainCamera != null)
        {
            Vector3 camPos = mainCamera.transform.position;
            Vector3 spawnPos = new Vector3(camPos.x, camPos.y, 0f);

            _currentFullscreenVfx = Instantiate(data.FullscreenVfxPrefab, spawnPos, Quaternion.identity);
            _currentFullscreenVfx.transform.SetParent(mainCamera.transform, true);

            // 카메라 뷰포트에 맞게 스케일 자동 조정
            if (data.VfxOriginalWorldSize > 0f)
            {
                float viewHeight = mainCamera.orthographicSize * 2f;
                float viewWidth = viewHeight * mainCamera.aspect;
                float viewMax = Mathf.Max(viewWidth, viewHeight);
                float scaleFactor = (viewMax / data.VfxOriginalWorldSize) * data.VfxScaleMargin;
                _currentFullscreenVfx.transform.localScale = Vector3.one * scaleFactor;
            }

            Destroy(_currentFullscreenVfx, duration + 1f);
        }

        // ── 플레이어 오라 VFX ──
        if (data.PlayerAuraVfxPrefab != null && playerTransform != null)
        {
            _currentAuraVfx = Instantiate(data.PlayerAuraVfxPrefab, playerTransform);
            _currentAuraVfx.transform.localPosition = Vector3.zero;
            Destroy(_currentAuraVfx, duration + 1f);
        }

        GameLogger.Log($"[궁극기 연출] 시작 | {data.DisplayName} duration={duration}s");
    }

    /// <summary>
    /// 궁극기 연출 종료. Executor에서 호출.
    /// </summary>
    public void EndPresentation()
    {
        if (_currentFullscreenVfx != null)
        {
            _currentFullscreenVfx.transform.SetParent(null);
            Destroy(_currentFullscreenVfx);
            _currentFullscreenVfx = null;
        }

        if (_currentAuraVfx != null)
        {
            Destroy(_currentAuraVfx);
            _currentAuraVfx = null;
        }
    }

    private void Awake() => EnsureRefs();

    private void EnsureRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }
}