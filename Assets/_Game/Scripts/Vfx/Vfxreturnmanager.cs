// UTF-8
// Assets/_Game/Scripts/VFX/VFXReturnManager.cs
// ★ VFX v2 — GPT 리뷰 #2: 매니저 1개가 일괄 틱
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 활성 VFX의 수명을 Update 1회로 일괄 관리.
/// VFXAutoReturn은 더 이상 자체 Update를 돌리지 않음.
///
/// [성능]
/// 이전: VFX 30개 → MonoBehaviour.Update 30회
/// 이후: VFXReturnManager.Update 1회 → for 루프 30회 (MonoBehaviour 오버헤드 0)
/// </summary>
[DisallowMultipleComponent]
public sealed class VFXReturnManager : MonoBehaviour
{
    public static VFXReturnManager Instance { get; private set; }

    // 활성 VFX 리스트 (Add/Remove 빈번하므로 List + swap-back)
    private readonly List<VFXAutoReturn> _active = new List<VFXAutoReturn>(64);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(VFXAutoReturn ar)
    {
        if (ar == null) return;
        _active.Add(ar);
    }

    public void Unregister(VFXAutoReturn ar)
    {
        if (ar == null) return;
        // swap-back remove (순서 무관)
        int idx = _active.IndexOf(ar);
        if (idx >= 0)
        {
            int last = _active.Count - 1;
            if (idx != last) _active[idx] = _active[last];
            _active.RemoveAt(last);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 역순 순회 (swap-back remove 안전)
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ar = _active[i];

            // 파괴된 오브젝트 정리
            if (ar == null || ar.gameObject == null)
            {
                int last = _active.Count - 1;
                if (i != last) _active[i] = _active[last];
                _active.RemoveAt(last);
                continue;
            }

            ar.timer -= dt;
            if (ar.timer > 0f) continue;

            // 수명 만료 → 풀 반환
            ar.registered = false;

            // swap-back remove
            int lastIdx = _active.Count - 1;
            if (i != lastIdx) _active[i] = _active[lastIdx];
            _active.RemoveAt(lastIdx);

            if (ar.sourcePrefab != null)
                VFXPool.Return(ar.sourcePrefab, ar.gameObject);
            else
                ar.gameObject.SetActive(false);
        }
    }
}