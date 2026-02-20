using UnityEngine;

/// <summary>
/// 이 오브젝트가 "살아있음/죽음" 상태를 스포너에 알려주는 리포터.
/// - 풀링(Disable) / 파괴(Destroy) 경로 모두에서 안전하게 처리
/// - 스포너가 Instantiate 직후 Init()을 호출해 소유자를 주입한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAliveReporter : MonoBehaviour
{
    private EnemySpawner2D ownerSpawner;
    private bool isRegistered;

    // 스포너가 생성 직후 호출
    public void Init(EnemySpawner2D spawner)
    {
        ownerSpawner = spawner;
        // Instantiate 직후 바로 등록 시도(이미 활성 상태일 수 있음)
        TryRegister();
    }

    private void OnEnable()
    {
        // 풀에서 꺼냈을 때(활성화) 등록
        TryRegister();
    }

    private void OnDisable()
    {
        // 풀로 반납될 때(비활성화) 해제
        TryUnregister();
    }

    private void OnDestroy()
    {
        // Destroy 경로에서도 안전 해제(이미 OnDisable에서 처리됐을 수 있음)
        TryUnregister();
    }

    private void TryRegister()
    {
        if (isRegistered) return;
        if (ownerSpawner == null) return;

        isRegistered = true;
        ownerSpawner.NotifyEnemyBecameAlive(this);
    }

    private void TryUnregister()
    {
        if (!isRegistered) return;

        isRegistered = false;
        if (ownerSpawner != null)
            ownerSpawner.NotifyEnemyBecameDead(this);
    }
}