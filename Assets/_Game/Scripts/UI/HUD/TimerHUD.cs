using UnityEngine;
using TMPro;
using _Game.Scripts.Core.Session;

public class TimerHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtTimer;

    private void Update()
    {
        if (SessionGameManager2D.Instance == null) return;

        float t = SessionGameManager2D.Instance.SessionTime;
        int min = Mathf.FloorToInt(t / 60f);
        int sec = Mathf.FloorToInt(t % 60f);
        txtTimer.text = $"{min:00}:{sec:00}";
    }
}