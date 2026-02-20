using UnityEngine;

/// <summary>
/// Boot 씬 진입점.
/// - 지금은 '편성 UI를 띄우는 씬'으로 쓰므로, 최소한의 안전장치만 둡니다.
/// - 나중에 진짜 부팅(세이브 로드/서비스 초기화)로 확장 가능.
/// </summary>
[DisallowMultipleComponent]
public sealed class BootEntry : MonoBehaviour
{
    [Header("디버그")]
    [SerializeField] private bool log = true;

    private void Awake()
    {
        if (log) Debug.Log("[BootEntry] Awake");

        // 프로토타입 안전장치: 시간 스케일이 다른 씬에서 0으로 남아있을 수 있음
        if (Time.timeScale <= 0f) Time.timeScale = 1f;
    }
}
