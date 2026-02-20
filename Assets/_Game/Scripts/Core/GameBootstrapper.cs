using UnityEngine;

public sealed class GameBootstrapper : MonoBehaviour
{
    [Header("여기에 씬 오브젝트를 드래그로 연결")]
    [SerializeField] private Transform player;      // YounSeol
    [SerializeField] private Transform systemsRoot; // 00_Systems

    private void Awake()
    {
        if (player == null) { Debug.LogError("[Bootstrapper] player 비어있음 → YounSeol 연결", this); return; }
        if (systemsRoot == null) { Debug.LogError("[Bootstrapper] systemsRoot 비어있음 → 00_Systems 연결", this); return; }

        var shooter = player.GetComponentInChildren<WeaponShooterSystem2D>(true);
        if (shooter == null)
        {
            Debug.LogError("[Bootstrapper] WeaponShooterSystem2D 없음 → 플레이어 프리팹에 컴포넌트 추가 필요", this);
            return;
        }

        // 프리팹에서 slots를 비워두는 운영이면, 여기서 1회 보장해도 됨(중복 안전).
        shooter.EnsureDefaultLoadoutIfEmpty();
    }
}