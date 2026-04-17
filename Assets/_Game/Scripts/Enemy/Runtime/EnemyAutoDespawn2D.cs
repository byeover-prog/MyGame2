using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAutoDespawn2D : MonoBehaviour
{
    private void OnEnable()
    {
        if (EnemyDespawnManager.Instance != null)
            EnemyDespawnManager.Instance.Register(transform, gameObject);
    }

    private void OnDisable()
    {
        if (EnemyDespawnManager.Instance != null)
            EnemyDespawnManager.Instance.Unregister(transform);
    }

    //  Update 없음 — 매니저가 거리 체크를 일괄 처리
}