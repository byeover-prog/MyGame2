using System;
using UnityEngine;

/// <summary>
/// 몬스터의 공통 사망 후처리를 담당하는 컴포넌트입니다.
///
/// 책임:
/// - EnemyHealth2D의 죽음 이벤트를 받음
/// - 킬 카운트 증가
/// - 공통 사망 이벤트 전달
/// - 풀 반환
///
/// 왜 분리하는가:
/// - EnemyHealth2D가 체력 외 책임을 가지면
///   경험치 드랍, 킬 카운트, 풀 반환이 한 곳에 섞여 유지보수가 어려워집니다.
/// - 공통 후처리는 여기서 담당하고,
///   경험치 / 엘리트 드랍 / 퀘스트 보고 같은 보상은 별도 컴포넌트가
///   이 핸들러의 이벤트를 구독하도록 분리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterDeathHandler2D : MonoBehaviour
{
    /// <summary>
    /// 사망 시 외부 리스너에게 전달할 최소 정보입니다.
    /// </summary>
    public readonly struct MonsterDeathContext
    {
        public MonsterDeathContext(
            string monsterId,
            MonsterType monsterType,
            Vector3 worldPosition,
            bool isElite)
        {
            MonsterId = monsterId;
            MonsterType = monsterType;
            WorldPosition = worldPosition;
            IsElite = isElite;
        }

        public string MonsterId { get; }
        public MonsterType MonsterType { get; }
        public Vector3 WorldPosition { get; }
        public bool IsElite { get; }
    }

    [Header("1. 필수 연결")]
    [SerializeField, Tooltip("죽음 이벤트를 받을 EnemyHealth2D입니다.\n"
                             + "보통 같은 프리팹 루트의 EnemyHealth2D를 연결합니다.")]
    private EnemyHealth2D health;

    [SerializeField, Tooltip("마지막으로 적용된 MonsterDefinitionSO를 읽기 위한 연결입니다.\n"
                             + "monsterId, monsterType 같은 사망 정보를 구성할 때 사용합니다.")]
    private MonsterRuntimeApplier2D runtimeApplier;

    [Header("2. 공통 후처리")]
    [SerializeField, Tooltip("킬 카운트를 누적할 전역 소스입니다.\n"
                             + "비워두면 FindFirstObjectByType<KillCountSource>()로 자동 탐색합니다.")]
    private KillCountSource killCountSource;

    [SerializeField, Tooltip("사망 시 킬 카운트를 1 증가시킬지 여부입니다.")]
    private bool addKillCountOnDeath = true;

    [SerializeField, Tooltip("사망 후 이 오브젝트를 풀로 반환할지 여부입니다.\n"
                             + "일반 몬스터는 보통 켜두는 것이 맞습니다.")]
    private bool returnToPoolOnDeath = true;

    [Header("3. 디버그")]
    [SerializeField, Tooltip("사망 처리 로그를 출력할지 여부입니다.")]
    private bool debugLog = false;

    /// <summary>
    /// 공통 사망 후처리 직전에 발생하는 이벤트입니다.
    /// 보상 컴포넌트는 이 이벤트를 구독해 동작합니다.
    /// </summary>
    public event Action<MonsterDeathContext> Died;

    private bool handledDeathThisLife;

    private void Reset()
    {
        health = GetComponent<EnemyHealth2D>();
        runtimeApplier = GetComponent<MonsterRuntimeApplier2D>();
    }

    private void Awake()
    {
        if (killCountSource == null)
            killCountSource = FindFirstObjectByType<KillCountSource>();
    }

    private void OnEnable()
    {
        handledDeathThisLife = false;

        if (killCountSource == null)
            killCountSource = FindFirstObjectByType<KillCountSource>();

        if (health != null)
            health.Died += HandleHealthDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleHealthDied;

        handledDeathThisLife = false;
    }

    private void HandleHealthDied(EnemyHealth2D deadHealth)
    {
        if (handledDeathThisLife)
            return;

        handledDeathThisLife = true;

        MonsterDeathContext context = BuildDeathContext();

        if (addKillCountOnDeath && killCountSource != null)
            killCountSource.AddKill();

        if (debugLog)
        {
            Debug.Log(
                $"[MonsterDeathHandler2D] 사망 처리 | ID: {context.MonsterId} | Type: {context.MonsterType}",
                this);
        }

        Died?.Invoke(context);

        if (returnToPoolOnDeath)
            EnemyPoolTag.ReturnToPool(gameObject);
    }

    private MonsterDeathContext BuildDeathContext()
    {
        MonsterDefinitionSO definition = runtimeApplier != null
            ? runtimeApplier.AppliedDefinition
            : null;

        string monsterId = definition != null ? definition.MonsterId : string.Empty;
        MonsterType monsterType = definition != null ? definition.MonsterType : MonsterType.Normal;
        bool isElite = monsterType == MonsterType.Elite;
        Vector3 worldPosition = transform.position;

        return new MonsterDeathContext(
            monsterId,
            monsterType,
            worldPosition,
            isElite);
    }
}