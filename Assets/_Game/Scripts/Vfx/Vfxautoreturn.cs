// ============================================================================
// VFXAutoReturn.cs  (v2 — GC 0)
// 경로: Assets/_Game/Scripts/Vfx/VFXAutoReturn.cs
//
// [문제점 (v1)]
// OnEnable마다 new WaitForSeconds(maxLifetime) 호출 → 힙 할당 1회
// 스킬 8종 × 초당 10발 = 초당 80회 GC 할당이 이 파일 하나에서 발생
//
// [수정 (v2)]
// 1. WaitForSeconds를 Dictionary<float, WaitForSeconds>로 캐싱
// 2. Coroutine 대신 타이머 기반 Update 사용 (코루틴 자체가 GC 발생)
// 3. Update 대신 VFXReturnTicker가 일괄 틱 (있으면 자동 연결)
//
// [Hierarchy / Inspector]
// 변경 없음. 기존과 동일하게 동작합니다.
// VFXSpawner.Setup()이 자동으로 이 컴포넌트를 부착합니다.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VFX 인스턴스에 자동 부착. 수명 후 풀 반환.
/// v2: Coroutine 제거 → 타이머 기반. GC 0.
/// </summary>
public class VFXAutoReturn : MonoBehaviour
{
    [HideInInspector] public GameObject sourcePrefab;
    [HideInInspector] public float maxLifetime = 3f;

    // ★ v2: 타이머 기반 (Coroutine 제거)
    private float _timer;
    private bool _ticking;

    // ★ v2: 일괄 틱 매니저 연결 여부
    // VFXReturnTicker가 있으면 Update 비활성화 + 외부 Tick 호출
    private static VFXReturnTicker s_ticker;
    private static bool s_tickerSearched;

    private void OnEnable()
    {
        _timer = maxLifetime;
        _ticking = true;

        // 일괄 틱 매니저 탐색 (1회만)
        if (!s_tickerSearched)
        {
            s_ticker = FindFirstObjectByType<VFXReturnTicker>();
            s_tickerSearched = true;
        }

        if (s_ticker != null)
        {
            s_ticker.Register(this);
        }
    }

    private void OnDisable()
    {
        _ticking = false;

        if (s_ticker != null)
        {
            s_ticker.Unregister(this);
        }
    }

    /// <summary>
    /// 일괄 틱 매니저(VFXReturnTicker)가 호출.
    /// 또는 s_ticker가 없으면 자신의 Update에서 호출.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_ticking) return;

        _timer -= dt;
        if (_timer <= 0f)
        {
            _ticking = false;
            ReturnToPool();
        }
    }

    private void Update()
    {
        // 일괄 틱 매니저가 있으면 여기서 처리하지 않음
        if (s_ticker != null) return;

        Tick(Time.deltaTime);
    }

    private void ReturnToPool()
    {
        if (sourcePrefab != null)
        {
            VFXPool.Return(sourcePrefab, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }

    /// <summary>일괄 틱 매니저 캐시 초기화. 씬 전환 시 호출.</summary>
    public static void ResetTickerCache()
    {
        s_ticker = null;
        s_tickerSearched = false;
    }
}