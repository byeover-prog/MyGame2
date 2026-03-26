using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Core.Session;

/// <summary>
/// 세션 종료 시 캐릭터 XP와 냥 보상을 한 번만 지급합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterRunRewardBridge2D : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("캐릭터 카탈로그입니다. 비우면 RootBootstrapper에서 찾습니다.")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Tooltip("스토리/캐주얼 XP, 냥 보상 규칙입니다. 비우면 기본값을 사용합니다.")]
    [SerializeField] private CharacterRunRewardConfigSO rewardConfig;

    [Header("현재 모드")]
    [Tooltip("이 컴포넌트가 붙어 있는 씬의 메타 보상 모드입니다.")]
    [SerializeField] private CharacterRunMode2D runMode = CharacterRunMode2D.Story;

    [Header("옵션")]
    [Tooltip("지원1/지원2도 XP를 받을지 여부입니다.")]
    [SerializeField] private bool awardSupports = true;

    [Tooltip("세션 상태 변화에 자동 반응할지 여부입니다.")]
    [SerializeField] private bool autoGrantOnSessionEnd = true;

    [Tooltip("로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;

    private SessionGameManager2D _session;
    private bool _granted;
    private CharacterRunRewardConfigSO _runtimeFallback;

    private void Awake()
    {
        if (catalog == null && RootBootstrapper.Instance != null && RootBootstrapper.Instance.CharacterRoot != null)
            catalog = RootBootstrapper.Instance.CharacterRoot.catalog;

        if (_session == null)
            _session = FindFirstObjectByType<SessionGameManager2D>();
    }

    private void OnEnable()
    {
        _granted = false;

        if (autoGrantOnSessionEnd && _session != null)
            _session.OnStateChanged += OnSessionStateChanged;
    }

    private void OnDisable()
    {
        if (_session != null)
            _session.OnStateChanged -= OnSessionStateChanged;
    }

    private void OnSessionStateChanged(SessionState previous, SessionState next)
    {
        if (_granted) return;

        if (next == SessionState.Victory)
            GrantRewards(RunResultType2D.Victory);
        else if (next == SessionState.GameOver)
            GrantRewards(RunResultType2D.Defeat);
    }

    [ContextMenu("승리 보상 지급(테스트)")]
    public void GrantVictoryRewardForDebug()
    {
        GrantRewards(RunResultType2D.Victory);
    }

    [ContextMenu("패배 보상 지급(테스트)")]
    public void GrantDefeatRewardForDebug()
    {
        GrantRewards(RunResultType2D.Defeat);
    }

    public void GrantRewards(RunResultType2D result)
    {
        if (_granted) return;
        if (catalog == null)
        {
            if (debugLog) GameLogger.LogWarning("[CharacterRunRewardBridge2D] catalog가 없어 보상을 지급하지 못했습니다.", this);
            return;
        }

        SaveManager2D saveManager = SaveManager2D.Instance;
        if (saveManager == null || saveManager.Data == null)
        {
            if (debugLog) GameLogger.LogWarning("[CharacterRunRewardBridge2D] SaveManager2D가 없어 보상을 지급하지 못했습니다.", this);
            return;
        }

        _granted = true;

        CharacterRunRewardConfigSO config = rewardConfig != null ? rewardConfig : GetRuntimeFallback();
        CharacterProgressionService2D progression = new CharacterProgressionService2D(catalog, saveManager);
        MetaWalletService2D wallet = new MetaWalletService2D(saveManager);
        CharacterMetaResolver2D resolver = new CharacterMetaResolver2D(catalog, saveManager);

        HashSet<string> awardedCharacters = new HashSet<string>();
        SquadLoadoutRuntime.Loadout loadout = SquadLoadoutRuntime.Current;

        AwardCharacter(
            progressive: progression,
            resolver: resolver,
            mode: runMode,
            characterId: loadout.mainId,
            xp: config.GetCharacterXp(runMode, result, isMain: true),
            awardedCharacters: awardedCharacters);

        if (awardSupports)
        {
            AwardCharacter(
                progressive: progression,
                resolver: resolver,
                mode: runMode,
                characterId: loadout.support1Id,
                xp: config.GetCharacterXp(runMode, result, isMain: false),
                awardedCharacters: awardedCharacters);
            AwardCharacter(
                progressive: progression,
                resolver: resolver,
                mode: runMode,
                characterId: loadout.support2Id,
                xp: config.GetCharacterXp(runMode, result, isMain: false),
                awardedCharacters: awardedCharacters);
        }

        SquadMetaBattleSnapshot2D snapshot = MetaBattleSnapshotRuntime2D.Current;

        float nyangBonusPercent = snapshot.mainBonus.nyangGainPercent;
        int nyangReward = config.GetNyang(runMode, result);
        nyangReward = Mathf.Max(0, Mathf.RoundToInt(nyangReward * (1f + (nyangBonusPercent / 100f))));
        wallet.AddNyang(nyangReward, autoSave: false);

        saveManager.Save();
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();

        if (debugLog)
            GameLogger.Log($"[CharacterRunRewardBridge2D] result={result} mode={runMode} nyang=+{nyangReward} awarded={awardedCharacters.Count}", this);
    }

    private static void AwardCharacter(
        CharacterProgressionService2D progressive,
        CharacterMetaResolver2D resolver,
        CharacterRunMode2D mode,
        string characterId,
        int xp,
        HashSet<string> awardedCharacters)
    {
        if (progressive == null) return;
        if (resolver == null) return;
        if (string.IsNullOrWhiteSpace(characterId)) return;
        if (awardedCharacters == null) return;
        if (!awardedCharacters.Add(characterId)) return;

        MetaCharacterBonusSnapshot2D bonus = resolver.BuildForCharacter(characterId);
        float expBonusPercent = mode == CharacterRunMode2D.Story
            ? bonus.storyExpGainPercent
            : bonus.casualExpGainPercent;

        int finalXp = Mathf.Max(0, Mathf.RoundToInt(xp * (1f + (expBonusPercent / 100f))));
        progressive.AddXp(characterId, finalXp, autoSave: false);
    }

    private CharacterRunRewardConfigSO GetRuntimeFallback()
    {
        if (_runtimeFallback == null)
            _runtimeFallback = CharacterRunRewardConfigSO.CreateRuntimeFallback();
        return _runtimeFallback;
    }
}
