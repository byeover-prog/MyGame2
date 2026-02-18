using UnityEngine;

public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("�̵�")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("�뽬")]
    [Tooltip("�����̽��ٷ� �뽬�մϴ�.")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space;

    [Tooltip("�뽬 �Ÿ�(�뷫���� ü�� �Ÿ�)")]
    [SerializeField] private float dashDistance = 2.5f;

    [Tooltip("�뽬 �ð�(ª������ '��' ����)")]
    [SerializeField] private float dashDuration = 0.12f;

    [Tooltip("�뽬 ��Ÿ��")]
    [SerializeField] private float dashCooldown = 0.9f;

    [Header("����")]
    [SerializeField] private Rigidbody2D rb;

    // ���� �Է� ����(�̵�/�뽬/��ų ���⿡ ���)
    public Vector2 MoveInput { get; private set; }

    // ���������� �ǹ� �ְ� �ٶ� ����(0�̸� ���������� ����)
    public Vector2 FacingDir { get; private set; } = Vector2.right;

    private float _dashEndTime = -999f;
    private float _nextDashReadyTime = 0f;
    private Vector2 _dashVelocity;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        
        float x = Input.GetAxisRaw("Horizontal"); //WS
        float y = Input.GetAxisRaw("Vertical"); //AD

        Vector2 input = new Vector2(x, y);
        if (input.sqrMagnitude > 1f) input.Normalize();
        MoveInput = input;

        // �ٶ󺸴� ���� ����(�Է��� ���� ����)
        if (MoveInput.sqrMagnitude > 0.0001f)
            FacingDir = MoveInput;

        // �뽬 �Է�
        if (Input.GetKeyDown(dashKey))
            TryDash();
    }

    private void FixedUpdate()
    {
        // �뽬 ���̸� �뽬 �ӵ� ����
        if (Time.time < _dashEndTime)
        {
            rb.linearVelocity = _dashVelocity;
            return;
        }

        // �Ϲ� �̵�
        rb.linearVelocity = MoveInput * moveSpeed;
    }

    private void TryDash()
    {
        if (Time.time < _nextDashReadyTime)
            return;

        // �Է��� ���ٸ� ������ �ٶ� �������� �뽬
        Vector2 dir = MoveInput.sqrMagnitude > 0.0001f ? MoveInput : FacingDir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // "�Ÿ�/�ð�"���� �ӵ� ���
        float speed = dashDistance / Mathf.Max(0.01f, dashDuration);
        _dashVelocity = dir.normalized * speed;

        _dashEndTime = Time.time + dashDuration;
        _nextDashReadyTime = Time.time + dashCooldown;
    }
}
