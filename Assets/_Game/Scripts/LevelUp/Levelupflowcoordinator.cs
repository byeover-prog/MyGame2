// ──────────────────────────────────────────────
// LevelUpFlowCoordinator.cs
// 새 4장 레벨업 시스템의 진입 브리지 + 큐 관리자
//
// 책임:
//   - pending 레벨업 누적 관리
//   - 중복 오픈 방지 + 순차 처리
//   - 시간 정지: 큐 시작 시 1회
//   - 시간 복구: 큐 완전 종료(pending 0) 시 1회
//   - 기존 PlayerSkillUpgradeSystem._pendingLevelUpCount 역할을 대체
//
// 시간 제어 원칙:
//   연속 레벨업 중 Close()마다 복구하지 않는다.
//   큐가 전부 끝났을 때만 timeScale을 복구한다.
// ──────────────────────────────────────────────

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.LevelUp.UI;
using _Game.Player;

namespace _Game.LevelUp
{
    public sealed class LevelUpFlowCoordinator : MonoBehaviour
    {
        // ── 참조 ───────────────────────────────────

        [Header("=== 새 시스템 참조 ===")]

        [SerializeField, Tooltip("카드 생성기")]
        private LevelUpCardGenerator cardGenerator;

        [SerializeField, Tooltip("새 4장 패널 컨트롤러")]
        private LevelUpCardPanelController panelController;

        [SerializeField, Tooltip("플레이어 스킬 로드아웃")]
        private PlayerSkillLoadout loadout;

        // ── 시간 정지 설정 ─────────────────────────

        [Header("=== 시간 정지 ===")]

        [SerializeField, Tooltip("패널 열릴 때 게임을 정지할지")]
        private bool pauseTimeWhenOpen = true;

        // ── 연속 레벨업 설정 ───────────────────────

        [Header("=== 연속 레벨업 ===")]

        [SerializeField, Tooltip("연속 레벨업 패널 사이 대기 시간(초, RealTime)")]
        private float panelInterval = 0.25f;

        // ── 내부 상태 ─────────────────────────────

        /// <summary>대기 중인 레벨업 횟수 (기존 _pendingLevelUpCount 역할)</summary>
        private int pendingLevelUps;

        /// <summary>큐 처리 코루틴 실행 중 여부</summary>
        private bool isProcessing;

        /// <summary>시간 정지 전 저장된 timeScale</summary>
        private float savedTimeScale = 1f;

        /// <summary>현재 시간이 정지된 상태인지</summary>
        private bool isTimePaused;

        // ════════════════════════════════════════════
        //  외부 API
        // ════════════════════════════════════════════

        /// <summary>
        /// 레벨업 1회를 큐에 넣는다.
        /// PlayerSkillUpgradeSystem.OnLevelUp()에서 호출한다.
        /// </summary>
        public void RequestLevelUp()
        {
            pendingLevelUps++;

            if (!isProcessing)
                StartCoroutine(ProcessLevelUpQueue());
        }

        /// <summary>
        /// 패널이 닫혔음을 통보받는다.
        /// LevelUpCardPanelController.Close()에서 호출한다.
        /// 여기서 시간 복구는 하지 않는다 — 큐 종료 시에만 복구.
        /// </summary>
        public void NotifyPanelClosed()
        {
            // 큐 코루틴이 panelController.IsOpen을 체크하며 대기 중이므로
            // 별도 처리 불필요. 통보만 받는다.
        }

        // ════════════════════════════════════════════
        //  큐 처리
        // ════════════════════════════════════════════

        private IEnumerator ProcessLevelUpQueue()
        {
            isProcessing = true;

            // ── 큐 시작: 시간 정지 (1회만) ─────────
            PauseGame();

            while (pendingLevelUps > 0)
            {
                // 이전 패널이 닫힐 때까지 대기
                while (panelController != null && panelController.IsOpen)
                    yield return null;

                // 패널 사이 텀 (연속 레벨업 시 UX용)
                if (panelInterval > 0f)
                    yield return new WaitForSecondsRealtime(panelInterval);

                pendingLevelUps--;

                // 참조 검증
                if (cardGenerator == null || loadout == null || panelController == null)
                {
                    Debug.LogWarning("[FlowCoordinator] 참조 누락, 큐 중단", this);
                    break;
                }

                // 카드 생성
                List<LevelUpCardData> cards = cardGenerator.Generate(loadout);

                if (cards == null || cards.Count == 0)
                {
                    Debug.LogWarning("[FlowCoordinator] 생성된 카드 없음, 건너뜀", this);
                    continue;
                }

                // 패널 열기 (시간은 이미 정지 상태)
                panelController.Open(cards);

                Debug.Log(
                    $"[FlowCoordinator] 패널 열림 | " +
                    $"카드 {cards.Count}장 | " +
                    $"남은 큐 {pendingLevelUps}",
                    this);
            }

            // ── 큐 완전 종료: 마지막 패널 닫힘 대기 ──
            while (panelController != null && panelController.IsOpen)
                yield return null;

            // ── 큐 완전 종료: 시간 복구 (1회만) ─────
            ResumeGame();

            isProcessing = false;

            Debug.Log("[FlowCoordinator] 큐 완전 종료, 시간 복구됨", this);
        }

        // ════════════════════════════════════════════
        //  시간 정지 / 복구 (내부 전용)
        // ════════════════════════════════════════════

        /// <summary>
        /// 게임 시간을 정지한다. 큐 시작 시 1회만 호출.
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
        /// 게임 시간을 복구한다. 큐 완전 종료(pending 0) 시 1회만 호출.
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

        // ════════════════════════════════════════════
        //  안전장치
        // ════════════════════════════════════════════

        /// <summary>
        /// 예기치 않은 비활성화 시 시간이 정지된 채 방치되지 않도록 복구.
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