using UnityEngine;

// [요약] 구버전 스킬들(부메랑, 다크오브 등)을 최신 통합 풀(ProjectilePool)과 연결해주는 '브릿지(연결망)' 스크립트입니다.
// 기존 스크립트 10여 개를 수정할 필요 없이, 이 파일 하나로 완벽하게 최적화 시스템과 호환됩니다.
[DisallowMultipleComponent]
public sealed class ProjectilePool2D : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("이 스킬이 발사할 투사체 프리팹을 지정합니다. (예: 부메랑 투사체, 다크오브 투사체)")]
    [SerializeField] private PooledObject2D prefab;

    // 글로벌 최신 풀 참조
    private ProjectilePool _globalPool;

    private void Awake()
    {
        if (prefab == null) return;

        // 씬에 존재하는 최신 통합 풀 시스템을 찾아 연결합니다.
        _globalPool = FindFirstObjectByType<ProjectilePool>();
        if (_globalPool == null)
        {
            var go = new GameObject("GlobalProjectilePool");
            _globalPool = go.AddComponent<ProjectilePool>();
        }

        // 시작할 때 미리 만들어두어 게임 렉(프리징)을 방지합니다.
        _globalPool.Prewarm(prefab.gameObject, 10);
    }

    // 구버전 스킬들이 투사체를 쏠 때 이 메서드를 호출합니다.
    public T Get<T>(Vector3 pos, Quaternion Quaternion) where T : PooledObject2D
    {
        if (prefab == null) return null;

        // 실제 생성은 구버전 방식이 아닌, '최신 ProjectilePool'이 담당하여 메모리를 극대화로 절약합니다.
        GameObject obj = _globalPool.Get(prefab.gameObject, pos, Quaternion);
        if (obj == null) return null;

        T result = obj.GetComponent<T>();
        if (result != null)
        {
            // 나중에 반납할 때 이 브릿지를 통해 돌아올 수 있도록 꼬리표를 달아줍니다.
            result.BindPool(this);
        }
        return result;
    }

    // 투사체가 적을 맞추거나 수명이 다해 사라질 때 호출됩니다.
    public void Return(PooledObject2D obj)
    {
        if (_globalPool != null && prefab != null && obj != null)
        {
            // 파괴(Destroy)하지 않고 최신 풀에 안전하게 반납하여 재사용합니다.
            _globalPool.Release(prefab.gameObject, obj.gameObject);
        }
        else if (obj != null)
        {
            Destroy(obj.gameObject);
        }
    }
}