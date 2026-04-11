using System.Collections.Generic;
using UnityEngine;


// 경험치 오브 전체를 관장하는 단일 매니저.

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class ExpOrbManager : MonoBehaviour
{
    [Header("자석")]
    [SerializeField] private float magnetRange = 3.0f;
    [SerializeField] private float minSpeed = 2.0f;
    [SerializeField] private float maxSpeed = 12.0f;
    [SerializeField] private float curvePower = 2.0f;

    [Header("제한")]
    [Tooltip("동시 존재 최대 수. 초과 시 오래된 것부터 경험치 미지급 소멸.")]
    [SerializeField] private int maxAlive = 150;

    [Tooltip("수거되지 않은 오브의 자동 소멸 시간(초). 0이면 비활성화.")]
    [SerializeField] private float despawnTime = 20f;

    [Header("성능")]
    [Tooltip("프레임당 idle 오브 거리 체크 횟수.")]
    [SerializeField] private int checkPerFrame = 80;

    [Header("참조 (비워두면 자동 탐색)")]
    [SerializeField] private Transform playerTransform;
    
    //  Static API — 오브가 매니저 참조 없이 호출

    private static ExpOrbManager s_inst;

    // 매니저 생성 전에 등록된 오브를 보관하는 대기열
    private static readonly List<ExpOrb2D> s_pending = new List<ExpOrb2D>(64);

    // 오브 OnEnable에서 호출.
    public static void Register(ExpOrb2D orb)
    {
        if (orb == null) return;
        if (s_inst != null)
            s_inst.Add(orb);
        else
            s_pending.Add(orb); // 매니저 아직 없으면 대기열
    }

    // 오브 OnDisable에서 호출.
    public static void Unregister(ExpOrb2D orb)
    {
        if (orb == null) return;
        if (s_inst != null)
        {
            s_inst._idle.Remove(orb);
            s_inst._attracting.Remove(orb);
        }
        s_pending.Remove(orb);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        s_inst = null;
        s_pending.Clear();
    }
    
    // 자석 범위 밖 대기 오브
    private readonly List<ExpOrb2D> _idle = new List<ExpOrb2D>(256);
    private int _idleCursor;

    // 자석 범위 내 이동 중 오브
    private readonly List<ExpOrb2D> _attracting = new List<ExpOrb2D>(64);

    // 전체 수 (상한 체크용) — _idle.Count + _attracting.Count
    private int AliveCount => _idle.Count + _attracting.Count;

    // 플레이어 캐시
    private PlayerExp _playerExp;
    private PlayerCombatStats2D _playerStats;
    
    //  생명주기

    private void Awake()
    {
        s_inst = this;
        ResolvePlayer();
    }

    private void OnEnable()
    {
        s_inst = this;

        // 대기열에 있는 오브들 처리
        for (int i = 0; i < s_pending.Count; i++)
        {
            var orb = s_pending[i];
            if (orb != null && orb.gameObject.activeSelf && !orb.Collected)
                Add(orb);
        }
        s_pending.Clear();
    }

    private void OnDisable()
    {
        if (s_inst == this) s_inst = null;
    }

    private void OnDestroy()
    {
        if (s_inst == this) s_inst = null;
    }
    
    //  핵심 Update — 이 1개가 모든 오브를 처리

    private void Update()
    {
        if (playerTransform == null)
        {
            ResolvePlayer();
            if (playerTransform == null) return;
        }

        Vector2 playerPos = playerTransform.position;
        float rangeMul = _playerStats != null ? _playerStats.PickupRangeMul : 1f;
        float range = magnetRange * rangeMul;
        float rangeSqr = range * range;
        float dt = Time.deltaTime;
        float now = Time.time;

        // Phase 1: idle 오브 거리 체크 (프레임 분산)
        int checks = Mathf.Min(checkPerFrame, _idle.Count);
        for (int c = 0; c < checks; c++)
        {
            if (_idle.Count == 0) break;
            if (_idleCursor >= _idle.Count) _idleCursor = 0;

            ExpOrb2D orb = _idle[_idleCursor];

            // 무효 정리
            if (orb == null || !orb.gameObject.activeSelf || orb.Collected)
            {
                _idle.RemoveAt(_idleCursor);
                continue;
            }

            // 자동 소멸
            if (despawnTime > 0f && (now - orb.SpawnTime) >= despawnTime)
            {
                _idle.RemoveAt(_idleCursor);
                orb.DoDespawn();
                continue;
            }

            // 자석 범위 진입 체크
            float sqr = ((Vector2)orb.transform.position - playerPos).sqrMagnitude;
            if (sqr <= rangeSqr)
            {
                orb.Attracting = true;
                _idle.RemoveAt(_idleCursor);
                _attracting.Add(orb);
                continue; // 커서 안 올림 — RemoveAt로 밀렸으니
            }

            _idleCursor++;
        }

        // Phase 2: attracting 오브 이동 + 수거 (매 프레임 전부)
        float expMul = _playerStats != null ? _playerStats.ExpGainMul : 1f;

        for (int i = _attracting.Count - 1; i >= 0; i--)
        {
            ExpOrb2D orb = _attracting[i];

            // 무효 정리
            if (orb == null || !orb.gameObject.activeSelf || orb.Collected)
            {
                _attracting.RemoveAt(i);
                continue;
            }

            Vector2 orbPos = orb.transform.position;
            Vector2 diff = playerPos - orbPos;
            float sqr = diff.sqrMagnitude;

            // 수거
            if (sqr <= orb.PickupDistSqr)
            {
                _attracting.RemoveAt(i);
                orb.DoCollect(_playerExp, expMul);
                continue;
            }

            // 이동 — Transform 직접 조작 (Rigidbody 없음)
            float dist = Mathf.Sqrt(sqr);
            float t = 1f - Mathf.Clamp01(dist / Mathf.Max(0.01f, range));
            float speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Pow(t, curvePower));

            Vector2 move = (diff / dist) * speed * dt; // normalized * speed * dt
            orb.transform.position = orbPos + move;
        }
    }
    
    //  내부

    private void Add(ExpOrb2D orb)
    {
        // 이미 등록되어 있으면 무시
        if (_idle.Contains(orb) || _attracting.Contains(orb)) return;

        _idle.Add(orb);

        // 상한 초과 시 가장 오래된 idle 오브 소멸
        while (maxAlive > 0 && AliveCount > maxAlive && _idle.Count > 0)
        {
            ExpOrb2D oldest = _idle[0];
            _idle.RemoveAt(0);
            if (oldest != null && oldest.gameObject.activeSelf && !oldest.Collected)
                oldest.DoDespawn();
        }
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        playerTransform = p.transform;
        _playerExp = p.GetComponentInChildren<PlayerExp>(true);
        _playerStats = p.GetComponentInChildren<PlayerCombatStats2D>(true);
    }
}
