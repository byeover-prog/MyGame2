// UTF-8
using System;
using UnityEngine;

public enum StatOp2D { Add, Mul }

public enum StatId2D
{
    DamageMul,
    DamageAdd,
    CooldownMul,
    CooldownAdd,
    ProjectileSpeedMul,
    ProjectileSpeedAdd,
    LifeSecondsMul,
    LifeSecondsAdd,
    ProjectileCountAdd,

    // 플레이어용(필요 시 확장)
    MoveSpeedMul,
    MoveSpeedAdd,
    MaxHpAdd,
    ExpGainMul,
}

[DisallowMultipleComponent]
public sealed class PassiveStatModifier2D : MonoBehaviour, ILevelableSkill
{
    [Serializable]
    public struct Entry
    {
        public StatId2D stat;
        public StatOp2D op;

        [Header("Lv1~Lv8 (Mul은 0.1 = +10% 델타 방식)")]
        public float lv1;
        public float lv2;
        public float lv3;
        public float lv4;
        public float lv5;
        public float lv6;
        public float lv7;
        public float lv8;

        public float GetValue(int level)
        {
            level = Mathf.Clamp(level, 1, 8);
            return level switch
            {
                1 => lv1,
                2 => lv2,
                3 => lv3,
                4 => lv4,
                5 => lv5,
                6 => lv6,
                7 => lv7,
                8 => lv8,
                _ => lv1
            };
        }
    }

    [Header("패시브 스탯 목록")]
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private int _level = 1;
    private PassiveStatBoard2D _board;

    // OnAttached가 호출되기 전에는 임의 등록/적용하지 않음(씬 배치 프리팹 오작동 방지)
    private bool _attached;

    public int Level => _level;
    public Entry[] Entries => entries;

    /// <summary>
    /// (SkillRunner가) 처음 장착할 때 1회 호출
    /// </summary>
    public void OnAttached(Transform newOwner)
    {
        _attached = true;

        // 보드 연결
        if (_board == null)
            _board = PassiveStatBoard2D.Instance != null
                ? PassiveStatBoard2D.Instance
                : FindFirstObjectByType<PassiveStatBoard2D>();

        if (_board != null)
            _board.Register(this);

        // 획득 시점의 레벨 반영
        ApplyLevel(_level);
    }

    /// <summary>
    /// ✅ 인터페이스 오타 대응: ILevelableSkill.OnAttaced(Transform)
    /// 프로젝트 인터페이스가 오타라면 이 메서드가 "필수"임.
    /// 내부적으로 정상 이름(OnAttached)으로 위임.
    /// </summary>
    public void OnAttaced(Transform newOwner)
    {
        OnAttached(newOwner);
    }

    /// <summary>
    /// 레벨 변화 반영(획득 Lv1 포함)
    /// </summary>
    public void ApplyLevel(int newLevel)
    {
        _level = Mathf.Clamp(newLevel, 1, 8);

        // 아직 장착 전이면 적용 보류
        if (!_attached) return;

        _board?.MarkDirty();
    }

    private void OnDisable()
    {
        // 풀/비활성화에서 빠질 때도 집계에서 제거(중복 누적 방지)
        if (_board != null)
            _board.Unregister(this);
    }

    private void OnEnable()
    {
        // 풀에서 다시 켜질 수 있으니, 장착된 상태면 재등록
        if (!_attached) return;

        if (_board == null)
            _board = PassiveStatBoard2D.Instance != null
                ? PassiveStatBoard2D.Instance
                : FindFirstObjectByType<PassiveStatBoard2D>();

        if (_board != null)
        {
            _board.Register(this);
            _board.MarkDirty();
        }
    }

    private void OnDestroy()
    {
        if (_board != null)
            _board.Unregister(this);
    }
}