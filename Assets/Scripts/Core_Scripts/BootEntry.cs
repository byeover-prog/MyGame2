using UnityEngine;

/// <summary>
/// Boot 씬 진입 시 1회 실행되는 엔트리
/// - 여기서 "스테이지 시작" 신호를 발행하고
/// - 전투 씬(Scene_Game)으로 이동
/// </summary>
public sealed class BootEntry : MonoBehaviour
{
    [Header("다음 씬")]
    [SerializeField] private string next_scene_name = SceneNames.Game;

    private void Awake()
    {
        // 플레이 모드 재시작 시 구독 꼬임 방지(선택)
        RunSignals.ClearAllSubscribers();
    }

    private void Start()
    {
        // 스테이지 시작(기준 시각 확정)
        RunSignals.RaiseStageStarted();

        // 전투 씬으로 이동
        SceneLoader.Load(next_scene_name);
    }
}
