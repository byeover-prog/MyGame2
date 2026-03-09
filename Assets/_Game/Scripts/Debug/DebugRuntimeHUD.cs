using System.Text;
using UnityEngine;

/// <summary>
/// 먹통/시간정지/스폰 디버깅용 최소 HUD.
/// - 프로토타입에서만 사용 권장
/// </summary>
[DisallowMultipleComponent]
public sealed class DebugRuntimeHUD : MonoBehaviour
{
    [Header("참조(비우면 자동 탐색)")]
    [SerializeField] private PlayerExp playerExp;
    [SerializeField] private EnemySpawner2D enemySpawner;

    [Header("표시")]
    [SerializeField] private bool visible = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F10;

    private readonly StringBuilder _sb = new StringBuilder(256);

    private void Awake()
    {
        if (playerExp == null)
            playerExp = FindFirstObjectByType<PlayerExp>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;

        _sb.Clear();

        _sb.Append("timeScale=").Append(Time.timeScale.ToString("0.##"))
            .Append("  time=").Append(Time.time.ToString("0.0"))
            .Append("  unscaled=").Append(Time.unscaledTime.ToString("0.0"));

        if (playerExp != null)
        {
            _sb.Append("\nplayerLv=").Append(playerExp.Level)
                .Append("  exp=").Append(playerExp.CurrentExp)
                .Append("/").Append(playerExp.RequiredExp);
        }

        _sb.Append("\nenemies=").Append(EnemyRegistry2D.Count);

        if (enemySpawner != null)
        {
            _sb.Append("\nspawn: alive=").Append(enemySpawner.AliveCount)
                .Append("/").Append(enemySpawner.MaxAliveCount)
                .Append("  in=").Append(enemySpawner.TimeUntilNextSpawn.ToString("0.00"))
                .Append("s  x").Append(enemySpawner.SpawnRateMultiplier.ToString("0.##"));
        }

        GUI.Label(new Rect(10, 10, 520, 120), _sb.ToString());
    }
}