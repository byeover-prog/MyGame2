using System.Collections.Generic;
using UnityEngine;

public sealed class SkillAwakeningApplier : MonoBehaviour
{
    [Header("각성 데이터베이스")]
    [Tooltip("게임에 존재하는 모든 각성 효과 목록입니다.")]
    [SerializeField] private List<SkillAwakeningSO> awakeningDatabase = new List<SkillAwakeningSO>(16);

    [Header("참조")]
    [Tooltip("비워두면 SaveManager2D.Instance를 자동 사용합니다.")]
    [SerializeField] private SaveManager2D saveManager;

    // ─── 런타임 ───
    private readonly Dictionary<string, SkillAwakeningSO> _activeAwakenings
        = new Dictionary<string, SkillAwakeningSO>(8);

    void Start()
    {
        if (saveManager == null) saveManager = SaveManager2D.Instance;
        LoadSavedAwakenings();
    }

    /// <summary>저장된 각성 효과를 모두 로드하여 적용합니다.</summary>
    public void LoadSavedAwakenings()
    {
        _activeAwakenings.Clear();

        if (saveManager == null || saveManager.Data == null) return;
        saveManager.Data.EnsureDefaults();

        QuestProgressSaveData progress = saveManager.Data.metaProfile.questProgress;
        if (progress == null || progress.unlockedAwakeningIds == null) return;

        for (int i = 0; i < progress.unlockedAwakeningIds.Count; i++)
        {
            string awakeningId = progress.unlockedAwakeningIds[i];
            SkillAwakeningSO so = FindAwakening(awakeningId);
            if (so == null) continue;

            ApplyAwakeningInternal(so);
        }

        GameLogger.Log($"[SkillAwakeningApplier] {_activeAwakenings.Count}개 각성 효과 로드 완료");
    }

    /// <summary>새 각성 효과를 즉시 적용합니다. QuestManager에서 호출.</summary>
    public void ApplyAwakening(SkillAwakeningSO awakening)
    {
        if (awakening == null) return;
        ApplyAwakeningInternal(awakening);
        GameLogger.Log($"[SkillAwakeningApplier] 각성 적용: {awakening.DisplayName} → {awakening.TargetSkillId}");
    }

    /// <summary>해당 스킬에 활성화된 각성 효과가 있는지 확인합니다.</summary>
    public bool HasAwakening(string skillId)
    {
        return _activeAwakenings.ContainsKey(skillId);
    }

    /// <summary>해당 스킬의 각성 효과를 반환합니다.</summary>
    public SkillAwakeningSO GetAwakening(string skillId)
    {
        return _activeAwakenings.TryGetValue(skillId, out SkillAwakeningSO so) ? so : null;
    }

    /// <summary>해당 스킬의 각성 피해량 보너스(%)를 반환합니다.</summary>
    public float GetDamageBoost(string skillId)
    {
        SkillAwakeningSO so = GetAwakening(skillId);
        return so != null ? so.DamageBoostPercent : 0f;
    }

    /// <summary>해당 스킬의 각성 추가 투사체 수를 반환합니다.</summary>
    public int GetExtraProjectiles(string skillId)
    {
        SkillAwakeningSO so = GetAwakening(skillId);
        return so != null ? so.ExtraProjectiles : 0;
    }

    /// <summary>해당 스킬의 각성 범위 보너스(%)를 반환합니다.</summary>
    public float GetAreaBoost(string skillId)
    {
        SkillAwakeningSO so = GetAwakening(skillId);
        return so != null ? so.AreaBoostPercent : 0f;
    }

    /// <summary>해당 스킬의 각성 특수 효과 키를 반환합니다.</summary>
    public string GetSpecialEffectKey(string skillId)
    {
        SkillAwakeningSO so = GetAwakening(skillId);
        return so != null ? so.SpecialEffectKey : null;
    }

    // ─── 내부 ───

    private void ApplyAwakeningInternal(SkillAwakeningSO awakening)
    {
        if (awakening == null || string.IsNullOrWhiteSpace(awakening.TargetSkillId)) return;
        _activeAwakenings[awakening.TargetSkillId] = awakening;
    }

    private SkillAwakeningSO FindAwakening(string awakeningId)
    {
        if (string.IsNullOrWhiteSpace(awakeningId)) return null;

        for (int i = 0; i < awakeningDatabase.Count; i++)
        {
            if (awakeningDatabase[i] != null && awakeningDatabase[i].AwakeningId == awakeningId)
                return awakeningDatabase[i];
        }
        return null;
    }
}