// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - EnemyRootSO는 "몬스터 프리팹/기본스탯" 목록(등록소)만 담당한다.
// - EnemySpawnTimelineSO는 "시간 흐름에 따른 스폰 순서/구간"만 담당한다.
// - 스폰러는 (현재 경과시간)으로 스테이지를 고르고, 그 스테이지의 후보들에서 가중치 랜덤으로 뽑는다.
[CreateAssetMenu(menuName = "그날이후/Enemies/Enemy Spawn Timeline", fileName = "EnemySpawnTimeline")]
public sealed class EnemySpawnTimelineSO : ScriptableObject
{
    [Serializable]
    public sealed class SpawnOption
    {
        [Tooltip("EnemyRootSO.Enemies의 Id와 정확히 일치해야 함 (예: wolf, virgineghost 등)")]
        public string EnemyId;

        [Tooltip("이 구간에서 등장 가중치. 0이면 등장 안함.")]
        [Min(0f)]
        public float Weight = 1f;
    }

    [Serializable]
    public sealed class Stage
    {
        [Tooltip("디버그용 이름")]
        public string Name = "Stage";

        [Tooltip("이 스테이지가 유지되는 시간(초). 0이면 '다음 스테이지로 즉시 넘어감'")]
        [Min(0f)]
        public float Duration = 60f;

        [Tooltip("이 구간에서 등장 가능한 몬스터 후보들(가중치 랜덤)")]
        public List<SpawnOption> Options = new List<SpawnOption>(8);
    }

    [Tooltip("시간 순서대로 스테이지를 배치하세요. (0번부터 순차 진행)")]
    public List<Stage> Stages = new List<Stage>(16);
}