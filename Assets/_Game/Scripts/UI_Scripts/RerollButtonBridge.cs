using UnityEngine;

public sealed class RerollButtonBridge : MonoBehaviour
{
    [Header("호출 대상(레벨업 패널 컨트롤러)")]
    [SerializeField] private LevelUpPanelController panel;

    // Button OnClick에 이 함수만 연결하면 됨
    public void OnClickReroll()
    {
        if (panel == null)
        {
            Debug.LogWarning("[RerollButtonBridge] panel이 비어있음", this);
            return;
        }

        panel.OnClickReroll(); // LevelUpPanelController에 public 메서드가 있어야 함
    }
}