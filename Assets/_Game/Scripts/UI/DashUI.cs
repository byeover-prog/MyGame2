using UnityEngine;
using UnityEngine.UI;

public sealed class DashUI : MonoBehaviour
{
    [Header("대쉬 아이콘")]
    [Tooltip("대쉬 아이콘 배열")]
    [SerializeField] private Image[] dashIcons;

    /// <summary>
    /// 대쉬 충전 상태 표시
    /// </summary>
    public void UpdateDash(int dashCount)
    {
        for (int i = 0; i < dashIcons.Length; i++)
        {
            dashIcons[i].enabled = i < dashCount;
        }
    }
}