// ──────────────────────────────────────────────
// LevelUpFlowCoordinator.cs
// 새 4장 레벨업 시스템의 진입 브리지 + 큐 관리자
//
// 구현 원리 요약:
// 카드 생성과 패널 오픈 시 동일한 PlayerSkillLoadout을 세션 컨텍스트로 전달한다.
// 패널은 이 컨텍스트를 리롤/보상 적용에도 그대로 재사용한다.
// 따라서 최초 생성 / 리롤 / 적용이 모두 같은 loadout 인스턴스를 바라보게 된다.
// ──────────────────────────────────────────────

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
        private LevelUpCardPanelController panelController;

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

        /// <summary>시간 정지 전 저장된 timeScale</summary>
        private float savedTimeScale = 1f;

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
                    Debug.LogWarning("[FlowCoordinator] 참조 누락, 큐 중단", this);
                    break;
                }

                Debug.Log(
                    $"[FlowCoordinator] 카드 생성 시작 | loadoutInstanceId={loadout.GetInstanceID()} | loadoutName={loadout.name}",
                    loadout);

                List<LevelUpCardData> cards = cardGenerator.Generate(loadout);

                if (cards == null || cards.Count == 0)
                {
                    Debug.LogWarning("[FlowCoordinator] 생성된 카드 없음, 건너뜀", this);
                    continue;
                }

                panelController.Open(cards, loadout);

                Debug.Log(
                    $"[FlowCoordinator] 패널 열림 | 카드 {cards.Count}장 | 남은 큐 {pendingLevelUps} | loadoutInstanceId={loadout.GetInstanceID()}",
                    loadout);
            }

            while (panelController != null && panelController.IsOpen)
                yield return null;

            ResumeGame();

            isProcessing = false;

            Debug.Log("[FlowCoordinator] 큐 완전 종료, 시간 복구됨", this);
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

            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            isTimePaused = true;

            Debug.Log($"[FlowCoordinator] 시간 정지 (saved={savedTimeScale})", this);
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

            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            isTimePaused = false;

            Debug.Log($"[FlowCoordinator] 시간 복구 (restored={Time.timeScale})", this);
        }

        /// <summary>
        /// 예기치 않은 비활성화 시 시간이 정지된 채 방치되지 않도록 복구한다.
        /// </summary>
        private void OnDisable()
        {
            if (isTimePaused)
            {
                ResumeGame();
                Debug.LogWarning("[FlowCoordinator] OnDisable에서 강제 시간 복구", this);
            }

            pendingLevelUps = 0;
            isProcessing = false;
        }
    }
}