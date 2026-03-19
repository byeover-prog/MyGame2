// UTF-8
using System.Collections;
using UnityEngine;

/// <summary>
/// T키 지원 궁극기 컨트롤러.
///
/// [동작 흐름 — 설계 문서 기준]
/// 1. T키 입력
/// 2. 지원1(왼쪽) + 지원2(오른쪽) 동시 등장 (LAND 애니메이션)
/// 3. 지원1 궁극기 시전 → 지원2는 IDLE 대기
/// 4. 지원1 끝나면 → 지원2 궁극기 시전 → 지원1은 IDLE 대기
/// 5. 둘 다 끝나면 → 동시에 LAND 역재생으로 퇴장
/// 6. 동시에 파괴
///
/// [LAND 역재생]
/// Animator의 speed를 -1로 설정하여 LAND 애니메이션을 거꾸로 재생.
/// </summary>
[DisallowMultipleComponent]
public sealed class SupportUltimateController2D : MonoBehaviour
{
    [Header("쿨다운")]
    [Tooltip("지원 궁극기 재사용 대기시간(초). 기획서=240초(4분)")]
    [SerializeField] private float cooldownSeconds = 240f;

    [Header("입력")]
    [Tooltip("지원 궁극기 발동 키")]
    [SerializeField] private KeyCode supportKey = KeyCode.T;

    [Header("등장 설정")]
    [Tooltip("지원 캐릭터가 메인 캐릭터 좌우에 유지할 거리")]
    [SerializeField] private float sideDistance = 4f;

    [Tooltip("LAND 애니메이션 재생 대기 시간(초)")]
    [SerializeField] private float landDuration = 0.5f;

    [Tooltip("LAND 역재생(퇴장) 대기 시간(초)")]
    [SerializeField] private float exitDuration = 0.5f;

    [Header("참조")]
    [Tooltip("스쿼드 편성 데이터")]
    [SerializeField] private SquadLoadout2D loadout;

    [Tooltip("공용 궁극기 실행기")]
    [SerializeField] private UltimateExecutor2D executor;

    [Tooltip("플레이어 Transform")]
    [SerializeField] private Transform playerTransform;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    // ── 런타임 ──
    private float _cooldownTimer;
    private bool _isExecuting;
    private Coroutine _routine;

    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
    public bool IsReady => _cooldownTimer <= 0f && !_isExecuting;

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<SquadLoadout2D>();
        if (executor == null)
            executor = GetComponentInChildren<UltimateExecutor2D>();
        if (playerTransform == null)
            playerTransform = transform;
    }

    private void Start()
    {
        _cooldownTimer = cooldownSeconds;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F10))
        {
            _cooldownTimer = 0f;
            Debug.Log("[지원 궁극기] F10 키 — 쿨다운 초기화");
        }

        if (Input.GetKeyDown(supportKey))
            TryActivate();
    }

    private void TryActivate()
    {
        if (_isExecuting)
        {
            Debug.Log("[지원 궁극기] 이미 시전 중");
            return;
        }

        if (_cooldownTimer > 0f)
        {
            Debug.Log($"[지원 궁극기] 쿨다운 중 — 남은 시간:{_cooldownTimer:F1}초");
            return;
        }

        if (loadout == null || (loadout.Support1 == null && loadout.Support2 == null))
        {
            Debug.LogWarning("[지원 궁극기] 지원 캐릭터가 편성되지 않았습니다!");
            return;
        }

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(ExecuteSupportSequence());
    }

    private IEnumerator ExecuteSupportSequence()
    {
        _isExecuting = true;

        CharacterDefinitionSO sup1 = loadout.Support1;
        CharacterDefinitionSO sup2 = loadout.Support2;

        // ═══════════════════════════════════════════════════
        //  1. 동시 등장 — 양쪽에서 LAND 애니메이션
        // ═══════════════════════════════════════════════════

        GameObject visual1 = SpawnVisual(sup1, -1f);
        GameObject visual2 = SpawnVisual(sup2,  1f);

        Animator anim1 = visual1 != null ? visual1.GetComponent<Animator>() : null;
        Animator anim2 = visual2 != null ? visual2.GetComponent<Animator>() : null;

        // LAND 정방향 동시 재생
        if (anim1 != null) { anim1.speed = 1f; anim1.SetTrigger("Trigger_Land"); }
        if (anim2 != null) { anim2.speed = 1f; anim2.SetTrigger("Trigger_Land"); }

        if (debugLog)
            Debug.Log($"[지원 궁극기] 동시 등장 | 지원1={GetName(sup1)} 지원2={GetName(sup2)}");

        yield return new WaitForSeconds(landDuration);

        // ═══════════════════════════════════════════════════
        //  2. 지원1 궁극기 시전 (지원2는 IDLE 대기)
        // ═══════════════════════════════════════════════════

        if (sup1 != null)
        {
            if (anim1 != null) anim1.SetTrigger("Trigger_Ult");

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원1 시전 | {sup1.DisplayName}");

            executor.SetCharacter(sup1);

            // VFX가 지원1 비주얼 몸에서 나가도록 설정
            if (visual1 != null)
                executor.SetCasterOverride(visual1.transform);

            bool finished1 = false;
            executor.Execute(() => finished1 = true, isSupport: true);

            while (!finished1)
                yield return null;

            // caster 복원
            executor.SetCasterOverride(null);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원1 완료 | {sup1.DisplayName}");
        }

        // ═══════════════════════════════════════════════════
        //  3. 지원2 궁극기 시전 (지원1은 IDLE 대기)
        // ═══════════════════════════════════════════════════

        if (sup2 != null)
        {
            if (anim2 != null) anim2.SetTrigger("Trigger_Ult");

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원2 시전 | {sup2.DisplayName}");

            executor.SetCharacter(sup2);

            // VFX가 지원2 비주얼 몸에서 나가도록 설정
            if (visual2 != null)
                executor.SetCasterOverride(visual2.transform);

            bool finished2 = false;
            executor.Execute(() => finished2 = true, isSupport: true);

            while (!finished2)
                yield return null;

            // caster 복원
            executor.SetCasterOverride(null);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원2 완료 | {sup2.DisplayName}");
        }

        // ═══════════════════════════════════════════════════
        //  4. 동시 퇴장 — LAND 역재생
        // ═══════════════════════════════════════════════════

        PlayReverseLand(anim1);
        PlayReverseLand(anim2);

        if (debugLog)
            Debug.Log("[지원 궁극기] 동시 퇴장 — LAND 역재생");

        yield return new WaitForSeconds(exitDuration);

        // ═══════════════════════════════════════════════════
        //  5. 동시 파괴 + 정리
        // ═══════════════════════════════════════════════════

        if (visual1 != null) Destroy(visual1);
        if (visual2 != null) Destroy(visual2);

        // 메인 캐릭터 궁극기로 복원
        if (loadout.Main != null)
            executor.SetCharacter(loadout.Main);

        _cooldownTimer = cooldownSeconds;
        _isExecuting = false;
        _routine = null;

        if (debugLog)
            Debug.Log($"[지원 궁극기] 종료 — 쿨다운 {cooldownSeconds}초 시작");
    }

    // ═══════════════════════════════════════════════════════
    //  헬퍼
    // ═══════════════════════════════════════════════════════

    private GameObject SpawnVisual(CharacterDefinitionSO charDef, float sideSign)
    {
        if (charDef == null || charDef.SupportVisualPrefab == null || playerTransform == null)
            return null;

        Vector3 spawnPos = playerTransform.position + Vector3.right * sideDistance * sideSign;
        GameObject instance = Instantiate(charDef.SupportVisualPrefab, spawnPos, Quaternion.identity);
        instance.name = $"SupportVisual_{charDef.CharacterId}";

        // 지원2(오른쪽)는 스프라이트 좌우 반전
        if (sideSign > 0f)
        {
            var sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = true;
        }

        return instance;
    }

    /// <summary>
    /// LAND 애니메이션을 역재생한다.
    /// Trigger_Land를 다시 발동하고 Animator speed를 -1로 설정.
    /// </summary>
    private void PlayReverseLand(Animator anim)
    {
        if (anim == null) return;

        anim.SetTrigger("Trigger_Land");
        anim.speed = -1f;
    }

    private static string GetName(CharacterDefinitionSO def)
    {
        return def != null ? def.DisplayName : "(없음)";
    }

    [ContextMenu("디버그: 지원궁 쿨다운 리셋")]
    public void DebugResetCooldown()
    {
        _cooldownTimer = 0f;
        Debug.Log("[지원 궁극기] 디버그 — 쿨다운 리셋");
    }
}