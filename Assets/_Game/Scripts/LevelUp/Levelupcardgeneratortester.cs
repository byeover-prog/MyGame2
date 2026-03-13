// ──────────────────────────────────────────────
// LevelUpCardGeneratorTester.cs
// 에디터에서 ContextMenu로 카드 생성 테스트
//
// 사용법:
//   Inspector에서 컴포넌트 우클릭 → "레벨업 카드 생성 테스트"
//   또는 플레이 모드에서 Inspector 점 세 개 → "레벨업 카드 생성 테스트"
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Player;

namespace _Game.LevelUp
{
    public sealed class LevelUpCardGeneratorTester : MonoBehaviour
    {
        [Header("=== 테스트 참조 ===")]

        [SerializeField, Tooltip("카드 생성기")]
        private LevelUpCardGenerator generator;

        [SerializeField, Tooltip("플레이어 스킬 로드아웃")]
        private PlayerSkillLoadout loadout;

        /// <summary>
        /// Inspector 컨텍스트 메뉴에서 실행.
        /// 카드 4장을 생성하고 Console에 로그를 출력한다.
        /// </summary>
        [ContextMenu("레벨업 카드 생성 테스트")]
        private void TestGenerate()
        {
            if (generator == null || loadout == null)
            {
                Debug.LogWarning("[CardGenTest] generator 또는 loadout 참조가 비어 있습니다.", this);
                return;
            }

            List<LevelUpCardData> cards = generator.Generate(loadout);

            Debug.Log($"[CardGenTest] ────── 카드 {cards.Count}장 생성됨 ──────", this);

            for (int i = 0; i < cards.Count; i++)
            {
                LevelUpCardData card = cards[i];
                Debug.Log(
                    $"[CardGenTest] [{i}] type={card.RewardType} | " +
                    $"title={card.Title} | desc={card.Description}",
                    this);
            }

            Debug.Log("[CardGenTest] ────── 테스트 완료 ──────", this);
        }
    }
}