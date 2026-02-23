using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/업그레이드/무기 업그레이드 풀", fileName = "UpgradePool")]
public sealed class WeaponUpgradePoolSO : ScriptableObject
{
    [Tooltip("뽑기 대상 카드 리스트")]
    public List<WeaponUpgradeCardSO> cards = new List<WeaponUpgradeCardSO>();
}