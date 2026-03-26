// ============================================================================
// VFXReturnTicker.cs
// 경로: Assets/_Game/Scripts/Vfx/VFXReturnTicker.cs
//
// VFXAutoReturn 인스턴스들을 일괄 Tick하는 매니저.
// VFX 100개가 있어도 MonoBehaviour.Update 1회만 실행.
//
// [Hierarchy / Inspector]
// Hierarchy: [CentralProjectileManager] 또는 별도 빈 오브젝트
// 컴포넌트: VFXReturnTicker (Add Component)
// Inspector: 설정 없음
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 활성 VFXAutoReturn들을 한 곳에서 일괄 Tick합니다.
/// 개별 VFXAutoReturn.Update()를 비활성화하고 이 매니저가 대신 처리.
/// </summary>
[DisallowMultipleComponent]
public sealed class VFXReturnTicker : MonoBehaviour
{
    // GC 0: 초기 용량만 할당, 런타임 resize 없음 (없으면 자동 확장)
    private readonly List<VFXAutoReturn> _active = new List<VFXAutoReturn>(256);

    /// <summary>VFXAutoReturn.OnEnable에서 호출.</summary>
    public void Register(VFXAutoReturn ar)
    {
        if (ar == null) return;
        _active.Add(ar);
    }

    /// <summary>VFXAutoReturn.OnDisable에서 호출.</summary>
    public void Unregister(VFXAutoReturn ar)
    {
        if (ar == null) return;
        _active.Remove(ar);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 역순 순회 — Tick 내에서 비활성화 → Remove 발생 가능
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ar = _active[i];
            if (ar == null || !ar.isActiveAndEnabled)
            {
                _active.RemoveAt(i);
                continue;
            }

            ar.Tick(dt);
        }
    }
}