using UnityEngine;

// [구현 원리 요약]
// - 부메랑 투사체의 "이미지"를 계속 회전시키는 전용 컴포넌트입니다.
// - 무기에서 AddComponent로 붙일 수 있게 아주 단순하게 유지합니다.
// - 로직(이동/타격)과 비주얼(회전)을 분리해서 디버그가 쉬워집니다.
[DisallowMultipleComponent]
public sealed class BoomerangSpin2D : MonoBehaviour
{
    [Tooltip("초당 회전 각도(도). 0이면 회전 없음")]
    [SerializeField] private float degreesPerSecond = 720f;

    // 무기에서 런타임으로 세팅할 때 사용
    public void SetSpin(float degPerSec)
    {
        degreesPerSecond = degPerSec;
    }

    private void Update()
    {
        if (degreesPerSecond == 0f) return;
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}