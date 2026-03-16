using UnityEngine;

/// <summary>
/// 다크오브 투사체 1개의 상태를 나타내는 구조체.
/// GameProjectileManager가 배열로 관리한다.
/// MonoBehaviour 없이 순수 데이터만 보유.
/// </summary>
[System.Serializable]
public struct DarkOrbState
{
    /// <summary>현재 월드 위치</summary>
    public Vector2 Position;

    /// <summary>이동 방향 (정규화)</summary>
    public Vector2 Direction;

    /// <summary>이동 속도 (units/sec)</summary>
    public float Speed;

    /// <summary>남은 수명 (초). 0 이하가 되면 폭발</summary>
    public float LifetimeRemaining;

    /// <summary>폭발 데미지</summary>
    public float ExplosionDamage;

    /// <summary>폭발 반경</summary>
    public float ExplosionRadius;

    /// <summary>
    /// 분열 깊이 (depth).
    /// 0 = 이 투사체가 폭발할 때 자식을 생성하지 않음.
    /// 1 이상 = 폭발 시 자식 2개를 ±고정각으로 생성하며 depth-1을 물려줌.
    /// </summary>
    public int SplitDepthRemaining;

    /// <summary>투사체 크기 배율 (패시브 스킬 범위 반영)</summary>
    public float ScaleMultiplier;

    /// <summary>활성 상태 여부. false면 슬롯이 비어있음</summary>
    public bool IsActive;

    /// <summary>ViewPool에서 할당받은 뷰 인스턴스 인덱스 (-1 = 미할당)</summary>
    public int ViewIndex;
}