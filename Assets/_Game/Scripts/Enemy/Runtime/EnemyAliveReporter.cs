using UnityEngine;

/// <summary>
/// 이 오브젝트가 alive count 포함 대상인지 여부를
/// EnemySpawner2D에 알려주는 리포터입니다.
///
/// 왜 이렇게 바꾸는가:
/// - 기존에는 스폰되면 거의 무조건 alive count에 들어가는 구조였습니다.
/// - 새 구조에서는 MonsterDefinitionSO의 countsAsAliveEnemy 값을 기준으로
///   alive count 포함 여부를 결정해야 합니다.
/// - 풀링으로 비활성화/재활성화될 때 이전 상태가 남아
///   중복 등록되는 문제를 막기 위해 상태를 명확히 초기화합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAliveReporter : MonoBehaviour
{
    private EnemySpawner2D ownerSpawner;
    private bool shouldCountAsAlive;
    private bool isRegistered;

    /// <summary>
    /// 스포너가 생성 직후 호출합니다.
    /// 이번 스폰에서 alive count에 포함할지 함께 전달합니다.
    /// </summary>
    public void Init(EnemySpawner2D spawner, bool shouldCount)
    {
        if (isRegistered)
            TryUnregister();

        ownerSpawner = spawner;
        shouldCountAsAlive = shouldCount;

        if (shouldCountAsAlive)
            TryRegister();
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        TryUnregister();

        // 중요:
        // 풀로 돌아간 뒤 다시 활성화될 때
        // 이전 스폰의 포함 여부를 끌고 가지 않도록 초기화합니다.
        shouldCountAsAlive = false;
    }

    private void OnDestroy()
    {
        TryUnregister();
        shouldCountAsAlive = false;
    }

    private void TryRegister()
    {
        if (isRegistered)
            return;

        if (!shouldCountAsAlive)
            return;

        if (ownerSpawner == null)
            return;

        isRegistered = true;
        ownerSpawner.NotifyEnemyBecameAlive(this);
    }

    private void TryUnregister()
    {
        if (!isRegistered)
            return;

        isRegistered = false;

        if (ownerSpawner != null)
            ownerSpawner.NotifyEnemyBecameDead(this);
    }
}