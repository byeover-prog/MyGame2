// ──────────────────────────────────────────────
// SkillDefinitionSO.cs
// 모든 스킬(Active / Passive)의 공통 정의 SO
//
// 패시브 밸런스 수치는 PassiveBalanceTableSO에서 관리.
// 이 SO는 스킬 정체성 + UI 표시 정보를 담당한다.
// ──────────────────────────────────────────────

using UnityEngine;

namespace _Game.Skills
{
    [CreateAssetMenu(
        fileName = "NewSkillDef",
        menuName = "Game/Skill/SkillDefinition",
        order = 0)]
    public class SkillDefinitionSO : ScriptableObject
    {
        // ── 기본 정보 ──────────────────────────────

        [Header("=== 기본 정보 ===")]

        [SerializeField, Tooltip("스킬 고유 식별자 (중복 불가)")]
        private string skillId;

        [SerializeField, Tooltip("Active = 발동형 스킬 / Passive = 패시브 스킬")]
        private SkillType skillType = SkillType.Active;

        [SerializeField, Tooltip("UI에 표시할 스킬 이름")]
        private string displayName;

        [SerializeField, Tooltip("슬롯 UI에 표시할 아이콘")]
        private Sprite icon;

        // ── 태그 ───────────────────────────────────

        [Header("=== 태그 ===")]

        [SerializeField, Tooltip("카드에 표시할 태그 (예: 원거리, 근접, 공격, 방어)")]
        private string tagKr = "";

        // ── 레벨 ───────────────────────────────────

        [Header("=== 레벨 ===")]

        [SerializeField, Tooltip("최대 도달 가능 레벨")]
        [Range(1, 10)]
        private int maxLevel = 8;

        // ── 레벨별 설명 ───────────────────────────

        [Header("=== 레벨별 카드 설명 (비우면 기본 텍스트 사용) ===")]

        [SerializeField, TextArea, Tooltip("Lv.1 (신규 획득 시) 설명")]
        private string descLv1;

        [SerializeField, TextArea, Tooltip("Lv.2 설명")]
        private string descLv2;

        [SerializeField, TextArea, Tooltip("Lv.3 설명")]
        private string descLv3;

        [SerializeField, TextArea, Tooltip("Lv.4 설명")]
        private string descLv4;

        [SerializeField, TextArea, Tooltip("Lv.5 설명")]
        private string descLv5;

        [SerializeField, TextArea, Tooltip("Lv.6 설명")]
        private string descLv6;

        [SerializeField, TextArea, Tooltip("Lv.7 설명")]
        private string descLv7;

        [SerializeField, TextArea, Tooltip("Lv.8 설명")]
        private string descLv8;

        // ── 패시브 설정 ────────────────────────────

        [Header("=== 패시브 설정 (Passive 타입만 사용) ===")]

        [SerializeField, Tooltip("패시브일 때 적용할 능력치 종류 (수치는 PassiveBalanceTableSO에서 관리)")]
        private PassiveStatType passiveStatType = PassiveStatType.None;

        // ── 프로퍼티 (읽기 전용) ───────────────────

        public string SkillId              => skillId;
        public SkillType SkillType         => skillType;
        public string DisplayName          => displayName;
        public Sprite Icon                 => icon;
        public string TagKr                => tagKr;
        public int MaxLevel                => maxLevel;
        public PassiveStatType PassiveStatType => passiveStatType;

        // ── 레벨별 설명 조회 ──────────────────────

        /// <summary>
        /// 지정 레벨의 카드 설명을 반환한다.
        /// 해당 레벨 설명이 비어있으면 아래로 내려가며 마지막 설명을 사용.
        /// 전부 비어있으면 빈 문자열.
        /// </summary>
        public string GetDescriptionForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 8);

            // 해당 레벨 설명 먼저 확인
            string desc = GetDescRaw(level);
            if (!string.IsNullOrWhiteSpace(desc))
                return desc;

            // fallback: 아래로 내려가며 마지막으로 채워진 설명 사용
            for (int lv = level; lv >= 1; lv--)
            {
                string d = GetDescRaw(lv);
                if (!string.IsNullOrWhiteSpace(d))
                    return d;
            }

            return string.Empty;
        }

        private string GetDescRaw(int level)
        {
            return level switch
            {
                1 => descLv1,
                2 => descLv2,
                3 => descLv3,
                4 => descLv4,
                5 => descLv5,
                6 => descLv6,
                7 => descLv7,
                8 => descLv8,
                _ => descLv1
            };
        }
    }
}