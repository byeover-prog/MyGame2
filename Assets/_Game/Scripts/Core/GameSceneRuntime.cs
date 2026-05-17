using UnityEngine;
using UnityEngine.SceneManagement;
using _Game.Scripts.Core.Session;

public sealed class GameSceneRuntime
{
    private readonly GameSceneContext context;
    private readonly Object logContext;
    private readonly bool log;

    private bool gameStarted;

    public GameSceneRuntime(GameSceneContext context, Object logContext, bool log)
    {
        this.context = context;
        this.logContext = logContext;
        this.log = log;
    }

    public bool IsGameRunning =>
        gameStarted
        && context != null
        && context.SessionManager != null
        && context.SessionManager.CurrentState == SessionState.Playing;

    public int KillCount =>
        context != null && context.KillCountSource != null
            ? context.KillCountSource.KillCount
            : 0;

    public bool StartGame(RunSetup runSetup)
    {
        if (gameStarted)
        {
            LogWarning("Game is already started.");
            return false;
        }

        if (context == null)
        {
            LogWarning("GameSceneContext is missing.");
            return false;
        }

        context.ResolveMissingReferences();

        if (!context.TryValidateForStart(out string contextReason))
        {
            LogWarning(contextReason);
            return false;
        }

        if (!context.TryPreparePlayerForStart(out string prepareReason))
        {
            LogWarning(prepareReason);
            return false;
        }

        string setupReason = string.Empty;
        if (runSetup == null || !runSetup.IsValid(out setupReason))
        {
            if (string.IsNullOrWhiteSpace(setupReason))
                setupReason = "RunSetup is missing.";

            LogWarning($"RunSetup invalid: {setupReason}");
            return false;
        }

        RunSetupHolder.Set(runSetup);
        SquadLoadoutRuntime.CopyFromSave(runSetup.ToFormationSaveData());

        gameStarted = true;

        context.KillCountSource.ResetKill();
        context.SessionManager.StartSession();
        context.StageManager.BeginStage(runSetup);

        RunSignals.RaiseStageStarted();

        Log("Game started.");
        return true;
    }

    public void EndDefeat()
    {
        if (!gameStarted)
            return;

        if (context.SessionManager != null)
            context.SessionManager.GameOver();

        if (context.StageManager != null)
            context.StageManager.EndStage();

        gameStarted = false;
        Log($"Game ended by defeat. KillCount: {KillCount}");
    }

    public void EndVictory()
    {
        if (!gameStarted)
            return;

        if (context.SessionManager != null)
            context.SessionManager.Victory();

        if (context.StageManager != null)
            context.StageManager.EndStage();

        gameStarted = false;
        Log($"Game ended by victory. KillCount: {KillCount}");
    }

    public void RestartCurrentScene()
    {
        gameStarted = false;
        RunSignals.ClearAllSubscribers();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Log(string message)
    {
        if (log)
            GameLogger.Log($"[GameSceneRuntime] {message}", logContext);
    }

    private void LogWarning(string message)
    {
        if (log)
            GameLogger.LogWarning($"[GameSceneRuntime] {message}", logContext);
    }
}
