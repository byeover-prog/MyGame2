using System.Collections;
using UnityEngine;

/// <summary>
/// T키 지원 궁극기 컨트롤러.
///
/// [수정 이력]
/// - 낙하 중 착지 위치를 플레이어 기준 실시간 추적 (Presenter에 Transform+offset 전달)
/// - SupportFollower2D에 메인 SpriteRenderer 전달 → 메인의 flipX를 따라감
/// </summary>
[DisallowMultipleComponent]
public sealed class SupportUltimateController2D : MonoBehaviour
{
    [Header("쿨다운")]
    [SerializeField] private float cooldownSeconds = 240f;

    [Header("입력")]
    [SerializeField] private KeyCode supportKey = KeyCode.T;

    [Header("등장 설정")]
    [SerializeField] private float sideDistance = 4f;
    [Min(0f)] [SerializeField] private float staggerDelay = 0.08f;

    [Header("따라다니기")]
    [Min(1f)] [SerializeField] private float followSpeed = 12f;

    [Header("퇴장 설정")]
    [Min(0f)] [SerializeField] private float exitLingerDuration = 1.5f;

    [Header("참조")]
    [SerializeField] private SquadLoadout2D loadout;
    [SerializeField] private UltimateExecutor2D executor;
    [SerializeField] private Transform playerTransform;

    [Tooltip("메인 캐릭터의 SpriteRenderer입니다. 지원 캐릭터가 같은 방향을 바라보는 데 사용합니다.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("연출 시스템")]
    [SerializeField] private SupportLandingPresenter2D landingPresenter;

    [Header("버프 시스템")]
    [SerializeField] private BattleBuffController2D buffController;

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
        if (loadout == null) loadout = GetComponent<SquadLoadout2D>();
        if (executor == null) executor = GetComponentInChildren<UltimateExecutor2D>();
        if (playerTransform == null) playerTransform = transform;
        if (playerSpriteRenderer == null) playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null && playerTransform != null)
            playerSpriteRenderer = playerTransform.GetComponent<SpriteRenderer>();
        if (landingPresenter == null) landingPresenter = GetComponent<SupportLandingPresenter2D>();
        if (landingPresenter == null) landingPresenter = FindFirstObjectByType<SupportLandingPresenter2D>();
        if (buffController == null) buffController = GetComponent<BattleBuffController2D>();
        if (buffController == null) buffController = FindFirstObjectByType<BattleBuffController2D>();
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

        Vector3 offset1 = Vector3.right * sideDistance * -1f;
        Vector3 offset2 = Vector3.right * sideDistance * 1f;

        // 초기 위치 (플레이어 현재 위치 기준)
        Vector3 landPos1 = playerTransform.position + offset1;
        Vector3 landPos2 = playerTransform.position + offset2;

        // ═══════════════════════════════════════════════════
        //  1. 비주얼 생성
        // ═══════════════════════════════════════════════════

        GameObject visual1 = SpawnVisualHidden(sup1, landPos1, -1f);
        GameObject visual2 = SpawnVisualHidden(sup2, landPos2, 1f);

        SupportLandingConfigSO cfg1 = GetLandingConfig(sup1);
        SupportLandingConfigSO cfg2 = GetLandingConfig(sup2);

        if (debugLog)
            Debug.Log($"[지원 궁극기] 등장 시작 | 지원1={GetName(sup1)} 지원2={GetName(sup2)}");

        // ═══════════════════════════════════════════════════
        //  2. 등장 (★ playerTransform + offset 전달 → 실시간 추적 낙하)
        // ═══════════════════════════════════════════════════

        bool entrance1Done = false;
        bool entrance2Done = false;

        if (landingPresenter != null)
        {
            if (visual1 != null)
                landingPresenter.PlayEntrance(visual1, playerTransform, offset1, cfg1, () => entrance1Done = true);
            else
                entrance1Done = true;

            if (staggerDelay > 0f)
                yield return new WaitForSeconds(staggerDelay);

            if (visual2 != null)
                landingPresenter.PlayEntrance(visual2, playerTransform, offset2, cfg2, () => entrance2Done = true);
            else
                entrance2Done = true;

            while (!entrance1Done || !entrance2Done)
                yield return null;
        }
        else
        {
            if (visual1 != null) { visual1.transform.position = landPos1; SetVisualVisible(visual1, true); }
            if (visual2 != null) { visual2.transform.position = landPos2; SetVisualVisible(visual2, true); }
            yield return new WaitForSeconds(0.5f);
        }

        // ═══════════════════════════════════════════════════
        //  3. 따라다니기 시작 (★ 메인 SpriteRenderer 전달)
        // ═══════════════════════════════════════════════════

        SupportFollower2D follower1 = AttachFollower(visual1, offset1);
        SupportFollower2D follower2 = AttachFollower(visual2, offset2);

        if (debugLog)
            Debug.Log("[지원 궁극기] 따라다니기 시작");

        // ═══════════════════════════════════════════════════
        //  4. 지원1 궁극기
        // ═══════════════════════════════════════════════════

        if (sup1 != null)
        {
            Animator anim1 = visual1 != null ? visual1.GetComponent<Animator>() : null;
            FireUltTriggerOnce(anim1);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원1 시전 | {sup1.DisplayName}");

            ApplySupportBuff(sup1);
            executor.SetCharacter(sup1);

            if (visual1 != null)
                executor.SetCasterOverride(visual1.transform);

            bool finished1 = false;
            executor.Execute(() => finished1 = true, isSupport: true);

            while (!finished1)
                yield return null;

            executor.SetCasterOverride(null);
            ForceIdleOnVisual(anim1);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원1 완료 → Idle | {sup1.DisplayName}");
        }

        // ═══════════════════════════════════════════════════
        //  5. 지원2 궁극기
        // ═══════════════════════════════════════════════════

        if (sup2 != null)
        {
            Animator anim2 = visual2 != null ? visual2.GetComponent<Animator>() : null;
            FireUltTriggerOnce(anim2);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원2 시전 | {sup2.DisplayName}");

            ApplySupportBuff(sup2);
            executor.SetCharacter(sup2);

            if (visual2 != null)
                executor.SetCasterOverride(visual2.transform);

            bool finished2 = false;
            executor.Execute(() => finished2 = true, isSupport: true);

            while (!finished2)
                yield return null;

            executor.SetCasterOverride(null);
            ForceIdleOnVisual(anim2);

            if (debugLog)
                Debug.Log($"[지원 궁극기] 지원2 완료 → Idle | {sup2.DisplayName}");
        }

        // ═══════════════════════════════════════════════════
        //  6. 퇴장 전 대기
        // ═══════════════════════════════════════════════════

        if (exitLingerDuration > 0f)
        {
            if (debugLog)
                Debug.Log($"[지원 궁극기] 퇴장 대기 {exitLingerDuration}초...");

            yield return new WaitForSeconds(exitLingerDuration);
        }

        // ═══════════════════════════════════════════════════
        //  7. 따라다니기 중지 + 퇴장
        // ═══════════════════════════════════════════════════

        if (follower1 != null) follower1.IsActive = false;
        if (follower2 != null) follower2.IsActive = false;

        bool exit1Done = false;
        bool exit2Done = false;

        if (landingPresenter != null)
        {
            if (visual1 != null)
                landingPresenter.PlayExit(visual1, cfg1, () => exit1Done = true);
            else
                exit1Done = true;

            if (visual2 != null)
                landingPresenter.PlayExit(visual2, cfg2, () => exit2Done = true);
            else
                exit2Done = true;

            while (!exit1Done || !exit2Done)
                yield return null;
        }
        else
        {
            exit1Done = true;
            exit2Done = true;
        }

        if (debugLog)
            Debug.Log("[지원 궁극기] 퇴장 완료");

        // ═══════════════════════════════════════════════════
        //  8. 파괴 + 정리
        // ═══════════════════════════════════════════════════

        if (visual1 != null) Destroy(visual1);
        if (visual2 != null) Destroy(visual2);

        if (loadout.Main != null)
            executor.SetCharacter(loadout.Main);

        _cooldownTimer = cooldownSeconds;
        _isExecuting = false;
        _routine = null;

        if (debugLog)
            Debug.Log($"[지원 궁극기] 종료 — 쿨다운 {cooldownSeconds}초 시작");
    }

    // ═══════════════════════════════════════════════════════
    //  따라다니기
    // ═══════════════════════════════════════════════════════

    private SupportFollower2D AttachFollower(GameObject visual, Vector3 offset)
    {
        if (visual == null || playerTransform == null) return null;

        SupportFollower2D follower = visual.GetComponent<SupportFollower2D>();
        if (follower == null)
            follower = visual.AddComponent<SupportFollower2D>();

        follower.Target = playerTransform;
        follower.Offset = offset;
        follower.FollowSpeed = followSpeed;
        follower.IsActive = true;

        // ★ 메인 캐릭터의 SpriteRenderer 전달 → flipX 따라가기
        follower.MainSpriteRenderer = playerSpriteRenderer;

        return follower;
    }

    // ═══════════════════════════════════════════════════════
    //  Animator 제어
    // ═══════════════════════════════════════════════════════

    private void FireUltTriggerOnce(Animator anim)
    {
        if (anim == null) return;
        anim.ResetTrigger("Trigger_Ult");
        anim.ResetTrigger("Trigger_Land");
        anim.SetTrigger("Trigger_Ult");
    }

    private void ForceIdleOnVisual(Animator anim)
    {
        if (anim == null) return;

        if (landingPresenter != null)
            landingPresenter.ForceTransitionToIdle(anim);
        else
        {
            anim.ResetTrigger("Trigger_Ult");
            anim.ResetTrigger("Trigger_Land");
            anim.Play("Idle", 0, 0f);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  버프
    // ═══════════════════════════════════════════════════════

    private void ApplySupportBuff(CharacterDefinitionSO supportChar)
    {
        if (supportChar == null || buffController == null) return;

        SupportBuffData2D buff = supportChar.SupportBuff;
        if (!buff.IsValid)
        {
            if (debugLog)
                Debug.Log($"[지원 궁극기] {supportChar.DisplayName}에 설정된 지원 버프가 없습니다.");
            return;
        }

        buffController.ApplyBuff(buff.kind, buff.value, buff.duration);

        if (debugLog)
            Debug.Log($"[지원 궁극기] 버프 적용 — {supportChar.DisplayName} → {buff.kind} +{buff.value} ({buff.duration}초)");
    }

    // ═══════════════════════════════════════════════════════
    //  헬퍼
    // ═══════════════════════════════════════════════════════

    private GameObject SpawnVisualHidden(CharacterDefinitionSO charDef, Vector3 landPos, float sideSign)
    {
        if (charDef == null || charDef.SupportVisualPrefab == null || playerTransform == null)
            return null;

        GameObject instance = Instantiate(charDef.SupportVisualPrefab, landPos, Quaternion.identity);
        instance.name = $"SupportVisual_{charDef.CharacterId}";

        // ★ 초기 flipX는 메인 캐릭터와 동일하게
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null && playerSpriteRenderer != null)
            sr.flipX = playerSpriteRenderer.flipX;

        SetVisualVisible(instance, false);
        return instance;
    }

    private static void SetVisualVisible(GameObject visual, bool visible)
    {
        if (visual == null) return;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = visible ? 1f : 0f;
            sr.color = c;
        }
    }

    private static SupportLandingConfigSO GetLandingConfig(CharacterDefinitionSO charDef)
    {
        if (charDef == null) return SupportLandingConfigSO.GetDefault();
        return charDef.SupportLandingConfig != null
            ? charDef.SupportLandingConfig
            : SupportLandingConfigSO.GetDefault();
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