using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 스폰 타임라인 데이터입니다.
///
/// 왜 먼저 만드는가:
/// - 스포너는 결국 "무엇을 언제 뽑을지"를 먼저 알아야 합니다.
/// - EnemySpawner2D를 새 구조로 바꾸기 전에,
///   monsterId 기준 스폰 데이터 형식을 먼저 고정해야
///   이후 스포너 수정 범위가 흔들리지 않습니다.
///
/// 이번 단계에서는 기존 EnemySpawnTimelineSO 틀을 유지하되,
/// 새 구조 이름과 monsterId 기준으로 맞춥니다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Monster/MonsterSpawnTimeline", fileName = "MonsterSpawnTimeline")]
public sealed class MonsterSpawnTimelineSO : ScriptableObject
{
    [Serializable]
    public sealed class SpawnOption
    {
        [Header("1. 스폰 후보")]
        [SerializeField, Tooltip("스폰할 몬스터 ID입니다.\n"
                                 + "MonsterDefinitionSO의 monsterId와\n"
                                 + "정확히 일치해야 합니다.\n"
                                 + "예: normal_cat, ranged_armyghost")]
        private string monsterId;

        [SerializeField, Min(0f), Tooltip("이 몬스터가 선택될 가중치입니다.\n"
                                          + "0이면 이 구간에서는 등장하지 않습니다.\n"
                                          + "값이 클수록 같은 구간에서\n"
                                          + "선택될 확률이 높아집니다.")]
        private float weight = 1f;

        /// <summary>스폰할 몬스터 ID</summary>
        public string MonsterId => monsterId;

        /// <summary>가중치</summary>
        public float Weight => weight;
    }

    [Serializable]
    public sealed class Stage
    {
        [Header("1. 구간 정보")]
        [SerializeField, Tooltip("디버그와 Inspector에서 구분할\n"
                                 + "구간 이름입니다.\n"
                                 + "예: Stage_01_Early,\n"
                                 + "Stage_02_RangedTest")]
        private string stageName = "Stage";

        [SerializeField, Min(0f), Tooltip("이 구간이 유지되는 시간입니다.\n"
                                          + "단위는 초(second)입니다.\n"
                                          + "0이면 즉시 다음 구간으로\n"
                                          + "넘어갑니다.")]
        private float duration = 60f;

        [SerializeField, Min(0.01f), Tooltip("이 구간의 스폰 간격입니다.\n"
                                             + "단위는 초(second)입니다.\n"
                                             + "값이 작을수록 더 자주 스폰됩니다.")]
        private float spawnInterval = 1f;

        [Header("2. 스폰 후보 목록")]
        [SerializeField, Tooltip("이 구간에서 등장 가능한\n"
                                 + "몬스터 후보 목록입니다.\n"
                                 + "monsterId와 weight 조합으로 구성합니다.")]
        private List<SpawnOption> options = new List<SpawnOption>();

        /// <summary>구간 이름</summary>
        public string StageName => stageName;

        /// <summary>구간 유지 시간</summary>
        public float Duration => duration;

        /// <summary>구간 스폰 간격</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>구간 스폰 후보 목록</summary>
        public List<SpawnOption> Options => options;
    }

    [Header("1. 타임라인 구간 목록")]
    [SerializeField, Tooltip("시간 순서대로 진행할\n"
                             + "스폰 구간 목록입니다.\n"
                             + "0번부터 순서대로 검사하며,\n"
                             + "앞 구간 duration이 끝나면\n"
                             + "다음 구간으로 넘어갑니다.")]
    private List<Stage> stages = new List<Stage>();

    /// <summary>스폰 구간 목록</summary>
    public List<Stage> Stages => stages;
}