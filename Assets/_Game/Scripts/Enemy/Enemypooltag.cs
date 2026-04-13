// UTF-8
using UnityEngine;

/// <summary>
/// 적 오브젝트에 자동 부착되는 풀 태그.
/// EnemyPool2D가 Get() 시 자동 추가한다. 수동 설정 불필요.
/// 
/// 다른 스크립트에서 풀 반환 시:
///   EnemyPoolTag.ReturnToPool(gameObject);
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyPoolTag : MonoBehaviour
{
    [HideInInspector] public int PoolKey;
    [HideInInspector] public EnemyPool2D Pool;

    /// <summary>
    /// 적을 풀로 반환한다. 풀 태그가 없으면 Destroy 폴백.
    /// EnemyHealth2D, EnemyAutoDespawn2D 등에서 Destroy(gameObject) 대신 호출.
    /// </summary>
    public static void ReturnToPool(GameObject go)
    {
        if (go == null) return;

        var tag = go.GetComponent<EnemyPoolTag>();
        if (tag != null && tag.Pool != null)
        {
            tag.Pool.Return(go);
        }
        else
        {
            // 풀 없으면 기존 방식 폴백
            Destroy(go);
        }
    }
}