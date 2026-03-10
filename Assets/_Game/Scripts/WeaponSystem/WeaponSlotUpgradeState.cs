using UnityEngine;

[System.Serializable]
public sealed class WeaponSlotUpgradeState
{
    [Header("합연산 누적")]
    public int addDamage;
    public float addRange;
    public float addProjectileSpeed;
    public float addLifetime;

    [Header("곱연산 누적(기본 1)")]
    public float mulFireRate = 1f;      // 발사 간격에 적용할지, 발사 속도(초당 발사)에 적용할지 프로젝트 기준으로 맞춤
    public float mulArea = 1f;
    public float mulKnockback = 1f;

    [Header("정수 옵션 누적")]
    public int addPierce;
    public int addSplit;
    public int addShotCount;

    [Header("토글")]
    public bool enableHoming;
    public bool enableBoomerang;
    public bool enableRotate;
}
