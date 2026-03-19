// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 루트 GameObject에 동적으로 부착되는 속성별 VFX 호스트.
/// 정화구 방식: 속성당 VFX 인스턴스 1개만 유지하고, 재피격 시 지속시간만 갱신.
///
/// [동작 원리]
/// - ElementVFXObserver2D가 데미지 이벤트를 받으면 이 컴포넌트의 Refresh()를 호출
/// - 같은 속성이면 VFX 재생성 없이 시간만 연장
/// - 다른 속성이면 별도 VFX 생성 (동시에 빙결 + 전기 가능)
/// - 지속시간 종료 시 VFX 파괴
/// - 적 사망/비활성화 시 전체 정리
/// - VFX 위치는 Collider2D bounds 중심 기준 (피벗 발밑 문제 해결)
/// </summary>
public sealed class ElementAttachedVfxHost2D : MonoBehaviour
{
    private sealed class VfxEntry
    {
        public DamageElement2D Element;
        public GameObject Instance;
        public float ExpireTime;
    }

    private readonly Dictionary<DamageElement2D, VfxEntry> _entries =
        new Dictionary<DamageElement2D, VfxEntry>(4);

    private readonly List<DamageElement2D> _removeBuffer = new List<DamageElement2D>(4);

    private Collider2D _cachedCollider;

    /// <summary>
    /// 속성 이펙트를 부착하거나 지속시간을 갱신한다.
    /// 이미 같은 속성 VFX가 있으면 재생성하지 않고 시간만 연장.
    /// </summary>
    public void Refresh(DamageElement2D element, GameObject prefab, float duration)
    {
        if (prefab == null) return;

        if (_entries.TryGetValue(element, out VfxEntry entry))
        {
            // 이미 붙어있음 → 시간만 갱신
            entry.ExpireTime = Time.time + duration;

            // VFX가 파괴됐으면 다시 생성
            if (entry.Instance == null)
            {
                entry.Instance = Instantiate(prefab);
                entry.Instance.transform.SetParent(transform, true);
            }

            entry.Instance.SetActive(true);
        }
        else
        {
            // 처음 → 새로 생성
            entry = new VfxEntry
            {
                Element = element,
                Instance = Instantiate(prefab),
                ExpireTime = Time.time + duration
            };
            entry.Instance.transform.SetParent(transform, true);
            _entries.Add(element, entry);
        }

        // 위치를 Collider 중심으로 맞춤
        UpdateVfxPosition(entry.Instance.transform);
    }

    private void LateUpdate()
    {
        if (_entries.Count == 0) return;

        _removeBuffer.Clear();

        foreach (var pair in _entries)
        {
            VfxEntry entry = pair.Value;

            if (entry == null || entry.Instance == null)
            {
                _removeBuffer.Add(pair.Key);
                continue;
            }

            // 지속시간 만료 → 제거
            if (Time.time >= entry.ExpireTime)
            {
                Destroy(entry.Instance);
                _removeBuffer.Add(pair.Key);
                continue;
            }

            // 매 프레임 적 중심 따라가기
            UpdateVfxPosition(entry.Instance.transform);
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
            _entries.Remove(_removeBuffer[i]);
    }

    private void OnDisable() => CleanupAll();
    private void OnDestroy() => CleanupAll();

    private void CleanupAll()
    {
        foreach (var pair in _entries)
        {
            if (pair.Value?.Instance != null)
                Destroy(pair.Value.Instance);
        }
        _entries.Clear();
    }

    /// <summary>
    /// Collider2D bounds 중심으로 VFX 위치를 갱신한다.
    /// 피벗이 발밑인 적도 이펙트가 몸 중심에 보이도록.
    /// </summary>
    private void UpdateVfxPosition(Transform vfxTransform)
    {
        if (vfxTransform == null) return;
        vfxTransform.position = GetAnchorPosition();
    }

    private Vector3 GetAnchorPosition()
    {
        if (_cachedCollider == null)
            _cachedCollider = GetComponentInChildren<Collider2D>(true);

        if (_cachedCollider != null)
        {
            Bounds b = _cachedCollider.bounds;
            return new Vector3(b.center.x, b.center.y, 0f);
        }

        return transform.position;
    }
}