using System;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - SaveData의 생명주기(로드/기본값 생성/자동저장/종료 저장)만 담당.
/// - 실제 JSON 입출력은 JsonIO2D(static)에 위임.
/// </summary>
[DisallowMultipleComponent]
public sealed class SaveManager2D : MonoBehaviour
{
    public static SaveManager2D Instance { get; private set; }

    [Header("자동 저장")]
    [Tooltip("자동 저장 사용 여부")]
    [SerializeField] private bool autoSave = true;

    [Tooltip("자동 저장 간격(초)")]
    [Min(5f)]
    [SerializeField] private float autoSaveIntervalSeconds = 30f;

    [Header("로그")]
    [Tooltip("저장/로드 로그 출력")]
    [SerializeField] private bool log = false;

    public PlayerSaveData2D Data { get; private set; }

    private float _timer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        LoadOrCreate();
        _timer = 0f;
    }

    private void Update()
    {
        if (Data == null) return;

        // 누적 플레이 시간(예시)
        Data.totalPlaySeconds += Time.unscaledDeltaTime;

        if (!autoSave) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autoSaveIntervalSeconds)
        {
            _timer = 0f;
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void LoadOrCreate()
    {
        if (JsonIO2D.TryLoadFromPersistent(SaveKeys2D.PlayerSaveFile, out PlayerSaveData2D loaded, out string error))
        {
            Data = loaded;
            if (Data == null)
                Data = PlayerSaveData2D.CreateDefault();

            Data.EnsureDefaults();

            if (log) GameLogger.Log("[SaveManager2D] 세이브 로드 성공");
            return;
        }

        Data = PlayerSaveData2D.CreateDefault();
        Data.EnsureDefaults();
        if (log) GameLogger.LogWarning($"[SaveManager2D] 세이브 로드 실패 → 기본값 생성: {error}");

        Save(); // 기본값이라도 파일을 만들어서 안정화
    }

    public void Save()
    {
        if (Data == null) return;

        Data.EnsureDefaults();
        Data.lastSavedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (!JsonIO2D.TrySaveToPersistent(SaveKeys2D.PlayerSaveFile, Data, prettyPrint: true, out string error))
        {
            GameLogger.LogWarning($"[SaveManager2D] 저장 실패: {error}");
            return;
        }

        if (log) GameLogger.Log("[SaveManager2D] 저장 완료");
    }

    public void ResetSave()
    {
        if (!JsonIO2D.TryDeletePersistent(SaveKeys2D.PlayerSaveFile, out string error))
        {
            GameLogger.LogWarning($"[SaveManager2D] 삭제 실패: {error}");
        }

        Data = PlayerSaveData2D.CreateDefault();
        Data.EnsureDefaults();
        Save();
    }
}