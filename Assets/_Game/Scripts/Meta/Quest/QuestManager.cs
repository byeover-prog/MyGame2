using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class QuestManager : MonoBehaviour
{
    [Header("퀘스트 풀")]
    [Tooltip("제안 가능한 전체 퀘스트 목록입니다.")]
    [SerializeField] private List<QuestDefinitionSO> questPool = new List<QuestDefinitionSO>(16);

    [Header("설정")]
    [Tooltip("동시 진행 가능한 최대 퀘스트 수입니다.")]
    [SerializeField] private int maxActiveQuests = 3;

    [Tooltip("새 퀘스트 제안 간격(초)입니다.")]
    [SerializeField] private float offerInterval = 90f;

    [Header("참조")]
    [Tooltip("비워두면 SaveManager2D.Instance를 자동 사용합니다.")]
    [SerializeField] private SaveManager2D saveManager;

    // ─── 런타임 상태 ───
    private readonly List<ActiveQuestState> _activeQuests = new List<ActiveQuestState>(3);
    private float _timeSinceLastOffer;
    private float _gameElapsed;

    // ─── 이벤트 ───
    public event Action<string> OnQuestStarted;
    public event Action<string, int, int> OnQuestProgressUpdated;
    public event Action<string> OnQuestCompleted;

    // ─── 싱글톤 ───
    public static QuestManager Instance { get; private set; }

    public IReadOnlyList<ActiveQuestState> ActiveQuests => _activeQuests;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (saveManager == null)
            saveManager = SaveManager2D.Instance;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _gameElapsed += dt;
        _timeSinceLastOffer += dt;

        for (int i = 0; i < _activeQuests.Count; i++)
        {
            ActiveQuestState aq = _activeQuests[i];
            if (aq.definition.QuestType == QuestType.Survive && !aq.isCompleted)
            {
                aq.currentProgress = Mathf.FloorToInt(_gameElapsed - aq.startTime);
                if (aq.currentProgress >= aq.definition.TargetCount)
                    CompleteQuest(i);
                else
                    OnQuestProgressUpdated?.Invoke(aq.definition.QuestId, aq.currentProgress, aq.definition.TargetCount);
            }
        }

        if (_timeSinceLastOffer >= offerInterval && _activeQuests.Count < maxActiveQuests)
        {
            _timeSinceLastOffer = 0f;
            TryOfferRandomQuest();
        }
    }

    public void ReportKill(EnemyGrade grade, string enemyId = null)
    {
        for (int i = _activeQuests.Count - 1; i >= 0; i--)
        {
            ActiveQuestState aq = _activeQuests[i];
            if (aq.isCompleted) continue;

            bool match = false;
            switch (aq.definition.QuestType)
            {
                case QuestType.Kill:
                    match = string.IsNullOrWhiteSpace(aq.definition.TargetEnemyId)
                         || aq.definition.TargetEnemyId == enemyId;
                    break;
                case QuestType.BossKill:
                    match = grade == EnemyGrade.Boss;
                    break;
                case QuestType.EliteKill:
                    match = grade == EnemyGrade.Elite;
                    break;
            }

            if (!match) continue;

            aq.currentProgress++;
            if (aq.currentProgress >= aq.definition.TargetCount)
                CompleteQuest(i);
            else
                OnQuestProgressUpdated?.Invoke(aq.definition.QuestId, aq.currentProgress, aq.definition.TargetCount);
        }
    }

    public void ReportCollect(int amount = 1)
    {
        for (int i = _activeQuests.Count - 1; i >= 0; i--)
        {
            ActiveQuestState aq = _activeQuests[i];
            if (aq.isCompleted || aq.definition.QuestType != QuestType.Collect) continue;

            aq.currentProgress += amount;
            if (aq.currentProgress >= aq.definition.TargetCount)
                CompleteQuest(i);
            else
                OnQuestProgressUpdated?.Invoke(aq.definition.QuestId, aq.currentProgress, aq.definition.TargetCount);
        }
    }

    public bool ForceStartQuest(string questId)
    {
        if (_activeQuests.Count >= maxActiveQuests) return false;

        for (int i = 0; i < questPool.Count; i++)
        {
            if (questPool[i] != null && questPool[i].QuestId == questId)
            {
                StartQuest(questPool[i]);
                return true;
            }
        }
        return false;
    }

    private void TryOfferRandomQuest()
    {
        QuestProgressSaveData progress = GetQuestProgress();

        List<QuestDefinitionSO> candidates = new List<QuestDefinitionSO>(questPool.Count);
        for (int i = 0; i < questPool.Count; i++)
        {
            QuestDefinitionSO def = questPool[i];
            if (def == null) continue;
            if (def.MinGameTime > _gameElapsed) continue;
            if (!def.Repeatable && progress != null && progress.IsCompleted(def.QuestId)) continue;
            if (IsActive(def.QuestId)) continue;

            candidates.Add(def);
        }

        if (candidates.Count == 0) return;

        int pick = UnityEngine.Random.Range(0, candidates.Count);
        StartQuest(candidates[pick]);
    }

    private void StartQuest(QuestDefinitionSO definition)
    {
        ActiveQuestState aq = new ActiveQuestState
        {
            definition = definition,
            currentProgress = 0,
            isCompleted = false,
            startTime = _gameElapsed
        };
        _activeQuests.Add(aq);
        OnQuestStarted?.Invoke(definition.QuestId);

        GameLogger.Log($"[QuestManager] 퀘스트 시작: {definition.DisplayName} ({definition.FormatObjective()})");
    }

    private void CompleteQuest(int index)
    {
        ActiveQuestState aq = _activeQuests[index];
        aq.isCompleted = true;
        aq.currentProgress = aq.definition.TargetCount;

        ApplyRewards(aq.definition);

        QuestProgressSaveData progress = GetQuestProgress();
        if (progress != null && !progress.IsCompleted(aq.definition.QuestId))
        {
            progress.completedQuestIds.Add(aq.definition.QuestId);
        }

        if (aq.definition.AwakeningReward != null && progress != null)
        {
            string awakeningId = aq.definition.AwakeningReward.AwakeningId;
            if (!progress.IsAwakeningUnlocked(awakeningId))
            {
                progress.unlockedAwakeningIds.Add(awakeningId);
            }
        }

        if (saveManager != null) saveManager.Save();

        OnQuestCompleted?.Invoke(aq.definition.QuestId);
        GameLogger.Log($"[QuestManager] 퀘스트 완료: {aq.definition.DisplayName}");

        _activeQuests.RemoveAt(index);
    }

    private void ApplyRewards(QuestDefinitionSO definition)
    {
        if (definition.NyangReward > 0)
        {
            MetaWalletService2D wallet = new MetaWalletService2D(saveManager);
            wallet.AddNyang(definition.NyangReward, autoSave: false);
        }

        if (definition.ExpReward > 0)
        {
            PlayerExp playerExp = FindFirstObjectByType<PlayerExp>();
            if (playerExp != null)
                playerExp.AddExp(definition.ExpReward);
        }

        if (definition.AwakeningReward != null)
        {
            SkillAwakeningApplier applier = FindFirstObjectByType<SkillAwakeningApplier>();
            if (applier != null)
                applier.ApplyAwakening(definition.AwakeningReward);
        }
    }

    private bool IsActive(string questId)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            if (_activeQuests[i].definition.QuestId == questId)
                return true;
        }
        return false;
    }

    private QuestProgressSaveData GetQuestProgress()
    {
        if (saveManager == null || saveManager.Data == null) return null;
        saveManager.Data.EnsureDefaults();
        return saveManager.Data.metaProfile.questProgress;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Instance = null;
    }

    public sealed class ActiveQuestState
    {
        public QuestDefinitionSO definition;
        public int currentProgress;
        public bool isCompleted;
        public float startTime;

        public float Progress01 => definition.TargetCount > 0
            ? Mathf.Clamp01((float)currentProgress / definition.TargetCount)
            : 0f;
    }
}