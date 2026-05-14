using System;
using UnityEngine;

[Serializable]
public sealed class PlayerSaveData2D
{
    public const int CurrentVersion = 4;

    [Header("버전")]
    [Tooltip("세이브 데이터 버전. 필드 구조가 바뀌면 올린다.")]
    public int version = CurrentVersion;

    [Header("진행도")]
    [Tooltip("누적 플레이 시간(초)")]
    public float totalPlaySeconds = 0f;

    [Tooltip("최고 레벨(예시)")]
    public int bestLevel = 1;

    [Header("밸런스/옵션")]
    [Tooltip("다크오브 투명도(0~1). 옵션 저장 예시")]
    [Range(0f, 1f)]
    public float darkOrbAlpha = 0.75f;

    [Header("메타 진행")]
    [Tooltip("편성/강화/캐릭터 레벨/재화 저장 묶음입니다.")]
    public MetaProfileSaveData2D metaProfile = MetaProfileSaveData2D.CreateDefault();

    [Header("마지막 저장 시각(디버그)")]
    public long lastSavedUnixMs = 0;

    public static PlayerSaveData2D CreateDefault()
    {
        return new PlayerSaveData2D
        {
            version = CurrentVersion,
            totalPlaySeconds = 0f,
            bestLevel = 1,
            darkOrbAlpha = 0.75f,
            metaProfile = MetaProfileSaveData2D.CreateDefault(),
            lastSavedUnixMs = 0
        };
    }

    public void EnsureDefaults()
    {
        MigrateToCurrentVersion();

        if (bestLevel < 1) bestLevel = 1;
        if (totalPlaySeconds < 0f) totalPlaySeconds = 0f;
        darkOrbAlpha = Mathf.Clamp01(darkOrbAlpha);

        if (metaProfile == null) metaProfile = MetaProfileSaveData2D.CreateDefault();
        metaProfile.EnsureDefaults();
    }

    private void MigrateToCurrentVersion()
    {
        int fromVersion = version;
        if (fromVersion <= 0)
            fromVersion = 1;

        if (fromVersion > CurrentVersion)
            return;

        if (fromVersion < 2)
            MigrateToVersion2();

        if (fromVersion < 3)
            MigrateToVersion3();

        if (fromVersion < 4)
            MigrateToVersion4();

        version = CurrentVersion;
    }

    private void MigrateToVersion2()
    {
        if (bestLevel < 1) bestLevel = 1;
        if (metaProfile == null) metaProfile = MetaProfileSaveData2D.CreateDefault();
    }

    private void MigrateToVersion3()
    {
        if (metaProfile == null) metaProfile = MetaProfileSaveData2D.CreateDefault();
        metaProfile.EnsureDefaults();
    }

    private void MigrateToVersion4()
    {
        if (metaProfile == null) metaProfile = MetaProfileSaveData2D.CreateDefault();
        metaProfile.EnsureDefaults();
    }
}
