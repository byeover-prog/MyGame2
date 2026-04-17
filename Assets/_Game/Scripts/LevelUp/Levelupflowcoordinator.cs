using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.LevelUp.UI;
using _Game.Player;

namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 요청 큐를 처리하고 패널 오픈/시간 정지를 관리한다.
    /// </summary>
    public sealed class LevelUpFlowCoordinator : MonoBehaviour
    {
        [Header("=== 새 시스템 참조 ===")]
        [SerializeField, Tooltip("카드 생성기입니다.")]
        private LevelUpCardGenerator cardGenerator;

        [SerializeField, Tooltip("새 4장 패널 컨트롤러입니다.")]
        private LevelUpPanelController panelController;

        [SerializeField, Tooltip("플레이어 스킬 로드아웃입니다.")]
        private PlayerSkillLoadout loadout;

        [Header("=== 시간 정지 ===")]
        [SerializeField, Tooltip("패널 열릴 때 게임을 정지할지 여부입니다.")]
        private bool pauseTimeWhenOpen = true;

        [Header("=== 연속 레벨업 ===")]
        [SerializeField, Tooltip("연속 레벨업 패널 사이 대기 시간(초, 실시간)입니다.")]
        private float panelInterval = 0.25f;

        /// <summary>대기 중인 레벨업 횟수</summary>
        private int pendingLevelUps;

        /// <summary>큐 처리 코루틴 실행 중 여부</summary>
        private bool isProcessing;

        /// <summary>현재 시간이 정지된 상태인지</summary>
        private bool isTimePaused;

        /// <summary>
        /// 레벨업 1회를 큐에 넣는다.
        /// </summary>
        public void RequestLevelUp()
        {
            pendingLevelUps++;

            if (!isProcessing)
                StartCoroutine(ProcessLevelUpQueue());
        }

        /// <summary>
        /// 패널이 닫혔음을 통보받는다.
        /// </summary>
        public void NotifyPanelClosed()
        {
            // 코루틴이 panelController.IsOpen을 감시하므로 별도 처리 없음.
        }

        /// <summary>
        /// 레벨업 큐를 순차 처리한다.
        /// </summary>
        private IEnumerator ProcessLevelUpQueue()
        {
            isProcessing = true;

            PauseGame();

            while (pendingLevelUps > 0)
            {
                while (panelController != null && panelController.IsOpen)
                    yield return null;

                if (panelInterval > 0f)
                    yield return new WaitForSecondsRealtime(panelInterval);

                pendingLevelUps--;

                if (cardGenerator == null || loadout == null || panelController == null)
                {
                    GameLogger.LogWarning("[FlowCoordinator] 참조 누락, 큐 중단", this);
                    break;
                }

                GameLogger.Log(
                    $"[FlowCoordinator] 카드 생성 시작 | loadoutInstanceId={loadout.GetInstanceID()} | loadoutName={loadout.name}",
                    loadout);

                List<LevelUpCardData> cards = cardGenerator.Generate(loadout);

                if (cards == null || cards.Count == 0)
                {
                    GameLogger.LogWarning("[FlowCoordinator] 생성된 카드 없음, 건너뜀", this);
                    continue;
                }

                panelController.Open(cards);

                GameLogger.Log(
                    $"[FlowCoordinator] 패널 열림 | 카드 {cards.Count}장 | 남은 큐 {pendingLevelUps} | loadoutInstanceId={loadout.GetInstanceID()}",
                    loadout);
            }

            while (panelController != null && panelController.IsOpen)
                yield return null;

            ResumeGame();

            isProcessing = false;

            GameLogger.Log("[FlowCoordinator] 큐 완전 종료, 시간 복구됨", this);
        }

        /// <summary>
        /// 게임 시간을 정지한다.
        /// </summary>
        private void PauseGame()
        {
            if (!pauseTimeWhenOpen)
                return;

            if (isTimePaused)
                return;

            GamePauseGate2D.Acquire(this);
            isTimePaused = true;

            GameLogger.Log("[FlowCoordinator] 시간 정지 (GamePauseGate2D)", this);
        }

        /// <summary>
        /// 게임 시간을 복구한다.
        /// </summary>
        private void ResumeGame()
        {
            if (!pauseTimeWhenOpen)
                return;

            if (!isTimePaused)
                return;

            GamePauseGate2D.Release(this);
            isTimePaused = false;

            GameLogger.Log("[FlowCoordinator] 시간 복구 (GamePauseGate2D)", this);
        }

        /// <summary>
        /// 예기치 않은 비활성화 시 시간이 정지된 채 방치되지 않도록 복구한다.
        /// </summary>
        private void OnDisable()
        {
            if (isTimePaused)
            {
                ResumeGame();
                GameLogger.LogWarning("[FlowCoordinator] OnDisable에서 강제 시간 복구", this);
            }

            pendingLevelUps = 0;
            isProcessing = false;
        }
    }
}