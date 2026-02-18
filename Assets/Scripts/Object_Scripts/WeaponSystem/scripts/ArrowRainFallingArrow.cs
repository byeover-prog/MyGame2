using UnityEngine;

/// <summary>
/// 화살비 "낙하 화살" 연출 전용 오브젝트
/// - 데미지 판정 없음
/// - 지정된 속도로 이동하다가 수명 종료 시, 소유자(ArrowRainArea2D)에게 반환 요청
///
/// 주의:
/// - 충돌/트리거 필요 없음. 콜라이더를 달면 오히려 불필요한 비용/오작동 가능.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainFallingArrow : MonoBehaviour
{
    private ArrowRainArea2D owner;
    private Vector2 velocity;
    private float timeLeft;
    private bool isRunning;

    public void BindOwner(ArrowRainArea2D area)
    {
        owner = area;
    }

    public void Launch(Vector2 vel, float lifetime)
    {
        velocity = vel;
        timeLeft = Mathf.Max(0.01f, lifetime);
        isRunning = true;
    }

    private void OnEnable()
    {
        // 풀에서 재사용될 때 상태가 꼬이는 걸 방지
        if (!isRunning)
        {
            // owner가 스폰 직후 Launch를 호출하므로, 여기서는 초기화만 수행
            timeLeft = 0f;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        float dt = Time.deltaTime;
        transform.position += (Vector3)(velocity * dt);

        timeLeft -= dt;
        if (timeLeft <= 0f)
        {
            isRunning = false;

            // owner가 없으면 그냥 비활성화(안전)
            if (owner != null)
                owner.NotifyArrowFinished(this);
            else
                gameObject.SetActive(false);
        }
    }
}