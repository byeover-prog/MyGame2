using UnityEngine;

/// <summary>
/// 킬 카운트를 전역으로 보관하는 최소 데이터
/// - Enemy가 죽을 때 AddKill()만 호출하면 됨
/// - 다른 시스템(스폰 스케일러 등)은 여기서 읽는다
/// </summary>
public sealed class KillCountSource : MonoBehaviour
{
    [Header("현재 킬 수(디버그 확인용)")]
    [SerializeField] private int kill_count = 0;

    public int KillCount => kill_count;

    public void ResetKill()
    {
        kill_count = 0;
    }

    public void AddKill(int amount = 1)
    {
        if (amount <= 0) return;
        kill_count += amount;
        if (kill_count < 0) kill_count = 0;
    }
}