// ──────────────────────────────────────────────
// LevelUpFlowTester.cs
// 새 레벨업 시스템 단독 테스트용 브리지
//
// 구 시스템과 연결 없이, 컨텍스트 메뉴로
// 카드 생성 → 패널 열기 → 선택 → 적용 전체 플로우를 테스트한다.
//
// 사용법:
//   Systems > LevelUpSystem 오브젝트에 AddComponent
//   Inspector에서 참조 할당
//   플레이 모드 → 컴포넌트 우클릭 → "새 레벨업 패널 테스트 열기"
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.LevelUp.UI;
using _Game.Player;

namespace _Game.LevelUp
{
    public sealed class LevelUpFlowTester : MonoBehaviour
    {
        // ── 참조 ───────────────────────────────────

        [Header("=== 테스트 참조 ===")]

        [SerializeField, Tooltip("카드 생성기")]
        private LevelUpCardGenerator cardGenerator;

        [SerializeField, Tooltip("새 4장용 패널 컨트롤러")]
        private LevelUpCardPanelController panelController;

        [SerializeField, Tooltip("플레이어 스킬 로드아웃")]
        private PlayerSkillLoadout loadout;

        // ════════════════════════════════════════════
        //  테스트 진입점
        // ════════════════════════════════════════════

        /// <summary>
        /// 컨텍스트 메뉴에서 실행.
        /// 카드 4장 생성 → 패널 열기 → 선택 대기
        /// </summary>
        [ContextMenu("새 레벨업 패널 테스트 열기")]
        public void TestOpenPanel()
        {
            if (cardGenerator == null)
            {
                GameLogger.LogWarning("[FlowTester] cardGenerator 미할당", this);
                return;
            }

            if (panelController == null)
            {
                GameLogger.LogWarning("[FlowTester] panelController 미할당", this);
                return;
            }

            if (loadout == null)
            {
                GameLogger.LogWarning("[FlowTester] loadout 미할당", this);
                return;
            }

            // 이미 열려있으면 무시
            if (panelController.IsOpen)
            {
                GameLogger.LogWarning("[FlowTester] 패널이 이미 열려있습니다.", this);
                return;
            }

            // 카드 4장 생성
            List<LevelUpCardData> cards = cardGenerator.Generate(loadout);

            GameLogger.Log($"[FlowTester] 카드 {cards.Count}장 생성 → 패널 열기", this);

            for (int i = 0; i < cards.Count; i++)
            {
                GameLogger.Log(
                    $"[FlowTester]   [{i}] type={cards[i].RewardType} | " +
                    $"title={cards[i].Title} | " +
                    $"desc={cards[i].Description}",
                    this);
            }

            // 패널 열기
            panelController.Open(cards);
        }
    }
}