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

    [Header("자동 타겟 찾기")]
    [SerializeField] private bool autoFindPlayerByTag = true;

    private Vector3 _velocity;

    private void Awake()
    {
        if (target == null && autoFindPlayerByTag)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        // 2D에서는 z 고정(카메라가 밀리는 것 방지)
        desired.z = offset.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _velocity,
            smoothTime,
            maxSpeed,
            Time.unscaledDeltaTime // 레벨업에서 timeScale=0이어도 카메라가 멈추지 않게(원하면 deltaTime으로 변경)
        );
    }
}