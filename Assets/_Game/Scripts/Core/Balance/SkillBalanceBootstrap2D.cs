// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core.Balance
{
    /// <summary>
    /// [구현 원리 요약]
    /// - skill_balance.json (version + skills[])을 JsonManager2D로 로드
    /// - id -> SkillRow2D를 딕셔너리로 인덱싱(O(1) 조회)
    /// - (base + AddPerLevel*(level-1))로 레벨별 최종 수치 계산
    /// - base가 -1이면 "미사용"이므로 결과도 -1 유지
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class SkillBalanceBootstrap2D : MonoBehaviour
    {
        public static SkillBalanceBootstrap2D Instance { get; private set; }

        [Header("디버그")]
        [Tooltip("로딩/인덱싱 로그 출력")]
        [SerializeField] private bool log = false;

        [Tooltip("플레이 중 F5로 리로드(개발 편의)")]
        [SerializeField] private bool allowReloadInPlay = true;

        [Tooltip("리로드 키")]
        [SerializeField] private KeyCode reloadKey = KeyCode.F5;

        public bool IsReady { get; private set; }
        public int SkillCount => _map.Count;

        private readonly Dictionary<string, SkillBalanceDB2D.SkillRow2D> _map
            = new Dictionary<string, SkillBalanceDB2D.SkillRow2D>(128, StringComparer.Ordinal);

        private void Awake()
        {
            Debug.Log($"[SkillBalanceBootstrap2D] JsonManager Instance={(JsonManager2D.Instance? JsonManager2D.Instance.name : "null")}", this);
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildIndex();
        }

        private void Update()
        {
            if (!allowReloadInPlay) return;
            if (!Application.isPlaying) return;

            if (Input.GetKeyDown(reloadKey))
                BuildIndex();
        }

        /// <summary>
        /// JSON 로드 후 id로 인덱싱한다.
        /// </summary>
        public void BuildIndex()
        {
            IsReady = false;
            _map.Clear();

            if (JsonManager2D.Instance == null)
            {
                Debug.LogError("[SkillBalanceBootstrap2D] JsonManager2D가 씬에 없습니다.");
                return;
            }

            if (!JsonManager2D.Instance.TryLoadSkillBalance(out SkillBalanceDB2D db, out string error, out var source))
            {
                Debug.LogError($"[SkillBalanceBootstrap2D] 로드 실패: {error}");
                return;
            }

            if (db == null || db.skills == null || db.skills.Length == 0)
            {
                Debug.LogError("[SkillBalanceBootstrap2D] skills가 비었습니다. (JSON에 skills 배열이 있어야 함)");
                return;
            }

            for (int i = 0; i < db.skills.Length; i++)
            {
                var row = db.skills[i];
                if (row == null) continue;
                if (!row.HasId()) continue;

                if (_map.ContainsKey(row.id))
                {
                    Debug.LogWarning($"[SkillBalanceBootstrap2D] 중복 id 감지: {row.id}");
                    continue;
                }

                _map.Add(row.id, row);
            }

            IsReady = true;

            if (log)
                Debug.Log($"[SkillBalanceBootstrap2D] 인덱싱 완료: {_map.Count} skills (source={source})");
        }

        /// <summary>
        /// 스킬 원본 row를 얻는다(계산 전).
        /// </summary>
        public bool TryGetRow(string skillId, out SkillBalanceDB2D.SkillRow2D row)
        {
            row = null;

            if (!IsReady) return false;
            if (string.IsNullOrWhiteSpace(skillId)) return false;

            return _map.TryGetValue(skillId, out row);
        }

        /// <summary>
        /// 특정 스킬의 특정 레벨 "최종 수치"를 계산해 반환.
        /// </summary>
        public bool TryResolve(string skillId, int level, out SkillBalanceResolved2D resolved)
        {
            resolved = default;

            if (!IsReady) return false;
            if (string.IsNullOrWhiteSpace(skillId)) return false;

            if (!_map.TryGetValue(skillId, out var s))
                return false;

            if (level < 1) level = 1;
            int lv = level - 1;

            int damage = CalcInt(s.damage, s.damageAddPerLevel, lv);
            float cooldown = CalcFloat(s.cooldown, s.cooldownAddPerLevel, lv);
            float speed = CalcFloat(s.speed, s.speedAddPerLevel, lv);
            float life = CalcFloat(s.life, s.lifeAddPerLevel, lv);

            int count = CalcInt(s.count, s.countAddPerLevel, lv);
            float hitInterval = CalcFloat(s.hitInterval, s.hitIntervalAddPerLevel, lv);
            float orbitRadius = CalcFloat(s.orbitRadius, s.orbitRadiusAddPerLevel, lv);
            float orbitSpeed = CalcFloat(s.orbitSpeed, s.orbitSpeedAddPerLevel, lv);

            float active = CalcFloat(s.active, s.activeAddPerLevel, lv);
            float burstInterval = CalcFloat(s.burstInterval, s.burstIntervalAddPerLevel, lv);
            float spinDps = CalcFloat(s.spinDps, s.spinDpsAddPerLevel, lv);

            int bounceCount = CalcInt(s.bounceCount, s.bounceAddPerLevel, lv);
            int chainCount = CalcInt(s.chainCount, s.chainAddPerLevel, lv);
            int splitCount = CalcInt(s.splitCount, s.splitAddPerLevel, lv);

            float explosionRadius = CalcFloat(s.explosionRadius, s.explosionRadiusAddPerLevel, lv);
            float explodeDistance = CalcFloat(s.explodeDistance, s.explodeDistanceAddPerLevel, lv);
            float childSpeed = CalcFloat(s.childSpeed, s.childSpeedAddPerLevel, lv);

            float slowRate = CalcFloat(s.slowRate, s.slowRateAddPerLevel, lv);
            float slowSeconds = CalcFloat(s.slowSeconds, s.slowSecondsAddPerLevel, lv);

            resolved = new SkillBalanceResolved2D(
                id: s.id,
                level: level,
                damage: damage,
                cooldown: cooldown,
                speed: speed,
                life: life,
                count: count,
                hitInterval: hitInterval,
                orbitRadius: orbitRadius,
                orbitSpeed: orbitSpeed,
                active: active,
                burstInterval: burstInterval,
                spinDps: spinDps,
                bounceCount: bounceCount,
                chainCount: chainCount,
                splitCount: splitCount,
                explosionRadius: explosionRadius,
                explodeDistance: explodeDistance,
                childSpeed: childSpeed,
                slowRate: slowRate,
                slowSeconds: slowSeconds
            );

            return true;
        }

        private static int CalcInt(int baseValue, int addPerLevel, int lv)
        {
            if (baseValue < 0) return -1;
            return baseValue + (addPerLevel * lv);
        }

        private static float CalcFloat(float baseValue, float addPerLevel, int lv)
        {
            if (baseValue < 0f) return -1f;
            return baseValue + (addPerLevel * lv);
        }
    }
}