// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [Header("타겟")]
    [SerializeField] private Transform target;

    [Header("오프셋")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("부드러움")]
    [Tooltip("값이 작을수록 더 빠르게 따라감(민감). 보통 0.08~0.18")]
    [Min(0.01f)]
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("순간이동(텔레포트) 시 끌려오는 느낌 방지용. 보통 3~8")]
    [Min(0f)]
    [SerializeField] private float maxSpeed = 100f;

    [Header("타임스케일")]
    [Tooltip("ON이면 timeScale=0에서도 카메라가 계속 따라감(레벨업 UI에서 유용).\nOFF면 timeScale=0일 때 카메라도 멈춤.")]
    [SerializeField] private bool useUnscaledDeltaTime = true;

    [Header("픽셀 스냅(픽셀아트용)")]
    [Tooltip("ON이면 카메라 최종 위치를 픽셀 그리드에 맞춘다.")]
    [SerializeField] private bool pixelSnap = false;

    [Tooltip("스프라이트 PPU와 동일. 예: 64")]
    [Min(1f)]
    [SerializeField] private float pixelsPerUnit = 64f;

    [Tooltip("ON이면 카메라가 거의 멈췄을 때만 스냅(지터 감소).\nOFF면 항상 스냅(가장 또렷하지만 흔들릴 수 있음).")]
    [SerializeField] private bool snapOnlyWhenStill = true;

    [Tooltip("snapOnlyWhenStill=ON일 때, 이 속도(유닛/초) 이하이면 스냅 적용")]
    [Min(0f)]
    [SerializeField] private float stillSpeedThreshold = 0.05f;

    [Header("자동 타겟 찾기")]
    [SerializeField] private bool autoFindPlayerByTag = true;

    private Vector3 _velocity;
    private Vector3 _prevPos;
    private bool _prevPosInit;
    private bool _isSnapping; // 히스테리시스용 상태

    private void Awake()
    {
        if (target == null && autoFindPlayerByTag)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
    }

    private void OnEnable()
    {
        _prevPos = transform.position;
        _prevPosInit = true;
        _isSnapping = false;
    }

    public void SetTarget(Transform newTarget, bool snapNow = true)
    {
        target = newTarget;
        _velocity = Vector3.zero;
        _isSnapping = false;

        if (snapNow && target != null)
        {
            Vector3 p = target.position + offset;
            p.z = offset.z;
            transform.position = pixelSnap ? Snap(p) : p;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float dt = useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 desired = target.position + offset;
        desired.z = offset.z;

        Vector3 next = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _velocity,
            smoothTime,
            maxSpeed,
            dt
        );

        if (pixelSnap)
        {
            if (snapOnlyWhenStill)
            {
                if (!_prevPosInit)
                {
                    _prevPos = transform.position;
                    _prevPosInit = true;
                }

                float speed = (next - _prevPos).magnitude / dt;
                _prevPos = next;

                // 히스테리시스: 진입 임계값과 탈출 임계값을 다르게 설정
                // → 스냅 ON/OFF가 프레임마다 왔다갔다하는 현상(지터) 방지
                if (_isSnapping && speed > stillSpeedThreshold * 3f)
                    _isSnapping = false;
                else if (!_isSnapping && speed <= stillSpeedThreshold)
                    _isSnapping = true;

                if (_isSnapping)
                    next = Snap(next);
            }
            else
            {
                next = Snap(next);
            }
        }

        transform.position = next;
    }

    private Vector3 Snap(Vector3 p)
    {
        float unitsPerPixel = 1f / pixelsPerUnit;
        p.x = Mathf.Round(p.x / unitsPerPixel) * unitsPerPixel;
        p.y = Mathf.Round(p.y / unitsPerPixel) * unitsPerPixel;
        p.z = offset.z;
        return p;
    }
}