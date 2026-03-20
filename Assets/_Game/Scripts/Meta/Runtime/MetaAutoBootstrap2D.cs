using UnityEngine;

/// <summary>
/// 아웃게임에서 편성·강화가 변경될 때 인게임 BattleSnapshot을 다시 빌드하는 브릿지입니다.
/// 실제 BattleSnapshot 재빌드 로직은 프로젝트 상황에 맞게 확장하세요.
/// </summary>
public static class MetaAutoBootstrap2D
{
    /// <summary>
    /// 편성/강화 변경 후 호출됩니다.
    /// 현재 인게임 씬이 아닌 경우(아웃게임 메뉴)에서는 안전하게 무시합니다.
    /// </summary>
    public static void RebuildBattleSnapshotIfPossible()
    {
        // TODO: 인게임 씬에 PlayerStatRuntimeApplier2D가 존재하면
        //       아웃게임 보정값을 다시 적용하는 로직을 여기에 연결하세요.
        //
        // 예시:
        // var applier = Object.FindFirstObjectByType<PlayerStatRuntimeApplier2D>(FindObjectsInactive.Include);
        // if (applier != null)
        //     applier.ReapplyFromLoadout(healToFull: false);

        Debug.Log("[MetaAutoBootstrap2D] BattleSnapshot 재빌드 요청 — 아웃게임 변경 반영 대기");
    }
}