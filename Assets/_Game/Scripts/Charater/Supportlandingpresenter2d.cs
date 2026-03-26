using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 지원 캐릭터의 등장(낙하→착지→안정화) / 퇴장(상승→페이드) 비주얼 연출을 전담합니다.
///
/// [수정 이력]
/// - 낙하 중 착지 위치를 플레이어 기준 실시간 갱신 (메인이 움직여도 정확한 위치에 착지)
/// - 착지 시 Trigger_Land, 착지 후 Idle 전환
/// </summary>
[DisallowMultipleComponent]
public sealed class SupportLandingPresenter2D : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("기본 그림자 프리팹입니다. ConfigSO에 없으면 이걸 사용합니다.")]
    [SerializeField] private GameObject defaultShadowPrefab;

    [Header("Animator 상태 이름")]
    [Tooltip("Idle 상태 이름입니다. 착지 후 이 상태로 전환합니다.")]
    [SerializeField] private string idleStateName = "Idle";

    [Tooltip("착지 후 Idle 전환 CrossFade 시간(초)입니다.")]
    [Min(0f)] [SerializeField] private float idleCrossFadeDuration = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    // ═══════════════════════════════════════════════════
    //  등장 연출
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 지원 캐릭터 등장 시퀀스를 실행합니다.
    /// 낙하 중 착지 위치는 followTarget + offset 기준으로 매 프레임 갱신됩니다.
    /// </summary>
    /// <param name="visual">생성된 비주얼 GameObject</param>
    /// <param name="followTarget">메인 캐릭터 Transform (실시간 추적)</param>
    /// <param name="offset">메인 기준 오프셋 (좌/우)</param>
    /// <param name="config">캐릭터별 연출 설정</param>
    /// <param name="onComplete">등장 완료 콜백</param>
    public Coroutine PlayEntrance(GameObject visual, Transform followTarget, Vector3 offset,
        SupportLandingConfigSO config, Action onComplete = null)
    {
        if (visual == null || followTarget == null)
        {
            onComplete?.Invoke();
            return null;
        }

        if (config == null) config = SupportLandingConfigSO.GetDefault();
        return StartCoroutine(EntranceRoutine(visual, followTarget, offset, config, onComplete));
    }

    private IEnumerator EntranceRoutine(GameObject visual, Transform followTarget, Vector3 offset,
        SupportLandingConfigSO cfg, Action onComplete)
    {
        Transform vt = visual.transform;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        Animator anim = visual.GetComponent<Animator>();

        // ── A. 호출 예고: 그림자 선표시 ──
        Vector3 initialLandPos = followTarget.position + offset;
        GameObject shadow = SpawnShadow(initialLandPos, cfg);

        if (cfg.presignDuration > 0f)
        {
            SetAlpha(sr, 0f);

            // 그림자도 플레이어를 따라다님
            float presignElapsed = 0f;
            while (presignElapsed < cfg.presignDuration)
            {
                presignElapsed += Time.deltaTime;
                if (shadow != null)
                    shadow.transform.position = followTarget.position + offset + Vector3.down * 0.1f;
                yield return null;
            }
        }

        // ── B. 낙하: 플레이어 기준 실시간 추적하며 낙하 ──
        // LAND 모션: 낙하 시작과 동시에 발동
        if (anim != null)
        {
            anim.speed = 1f;
            anim.SetTrigger("Trigger_Land");

            if (debugLog)
                GameLogger.Log("[LandingPresenter] Trigger_Land 발동 — 낙하 시작");
        }

        SetAlpha(sr, 1f);

        float elapsed = 0f;
        Vector3 originalScale = vt.localScale;

        if (debugLog)
            GameLogger.Log($"[LandingPresenter] 낙하 시작 — height={cfg.dropHeight} duration={cfg.dropDuration}");

        while (elapsed < cfg.dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cfg.dropDuration);
            float curveT = cfg.dropCurve.Evaluate(t);

            // ★ 착지 목표를 매 프레임 갱신 — 메인이 움직여도 정확한 위치
            Vector3 currentLandPos = followTarget.position + offset;
            Vector3 currentStartPos = currentLandPos + Vector3.up * cfg.dropHeight;

            // 위치: 현재 시작점에서 현재 착지점으로 보간
            vt.position = Vector3.Lerp(currentStartPos, currentLandPos, curveT);

            // Stretch
            float stretchLerp = Mathf.Clamp01(t * 2f);
            Vector3 airScale = new Vector3(
                originalScale.x * Mathf.Lerp(1f, cfg.airStretchScale.x, stretchLerp),
                originalScale.y * Mathf.Lerp(1f, cfg.airStretchScale.y, stretchLerp),
                originalScale.z);
            vt.localScale = airScale;

            // 그림자도 실시간 추적
            if (shadow != null)
            {
                shadow.transform.position = currentLandPos + Vector3.down * 0.1f;
                UpdateShadow(shadow, curveT);
            }

            yield return null;
        }

        // 정확한 착지 위치 보정
        Vector3 finalLandPos = followTarget.position + offset;
        vt.position = finalLandPos;

        // ── C. 착지 임팩트 ──
        vt.localScale = new Vector3(
            originalScale.x * cfg.landSquashScale.x,
            originalScale.y * cfg.landSquashScale.y,
            originalScale.z);

        if (cfg.landVfxPrefab != null)
        {
            GameObject vfx = Instantiate(cfg.landVfxPrefab, finalLandPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        if (cfg.impactPauseDuration > 0f)
            yield return new WaitForSeconds(cfg.impactPauseDuration);

        if (debugLog)
            GameLogger.Log("[LandingPresenter] 착지 임팩트 완료");

        // Squash → 원래 크기 복귀
        elapsed = 0f;
        Vector3 squashedScale = vt.localScale;

        while (elapsed < cfg.squashRecoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cfg.squashRecoverDuration);
            vt.localScale = Vector3.Lerp(squashedScale, originalScale, t);
            yield return null;
        }

        vt.localScale = originalScale;

        // ★ 착지 완료 → Idle 자연 전환
        ForceTransitionToIdle(anim);

        if (shadow != null) Destroy(shadow);

        // ── D. 안정화 ──
        if (cfg.settleDuration > 0f)
            yield return new WaitForSeconds(cfg.settleDuration);

        if (debugLog)
            GameLogger.Log("[LandingPresenter] 등장 완료 — Idle 상태에서 궁극기 대기");

        onComplete?.Invoke();
    }

    // ═══════════════════════════════════════════════════
    //  퇴장 연출
    // ═══════════════════════════════════════════════════

    public Coroutine PlayExit(GameObject visual, SupportLandingConfigSO config, Action onComplete = null)
    {
        if (visual == null)
        {
            onComplete?.Invoke();
            return null;
        }

        if (config == null) config = SupportLandingConfigSO.GetDefault();
        return StartCoroutine(ExitRoutine(visual, config, onComplete));
    }

    private IEnumerator ExitRoutine(GameObject visual, SupportLandingConfigSO cfg, Action onComplete)
    {
        Transform vt = visual.transform;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();

        Vector3 startPos = vt.position;
        Vector3 endPos = startPos + Vector3.up * cfg.exitRiseHeight;
        float startAlpha = sr != null ? sr.color.a : 1f;

        if (cfg.exitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(cfg.exitVfxPrefab, startPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        if (debugLog)
            GameLogger.Log($"[LandingPresenter] 퇴장 시작 — rise={cfg.exitRiseHeight} duration={cfg.exitDuration}");

        float elapsed = 0f;
        while (elapsed < cfg.exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cfg.exitDuration);
            float curveT = cfg.exitCurve.Evaluate(t);

            vt.position = Vector3.Lerp(startPos, endPos, curveT);

            if (cfg.fadeOutOnExit && sr != null)
                SetAlpha(sr, Mathf.Lerp(startAlpha, 0f, t));

            yield return null;
        }

        if (debugLog)
            GameLogger.Log("[LandingPresenter] 퇴장 완료");

        onComplete?.Invoke();
    }

    // ═══════════════════════════════════════════════════
    //  Animator 제어
    // ═══════════════════════════════════════════════════

    public void ForceTransitionToIdle(Animator anim)
    {
        if (anim == null) return;

        anim.speed = 1f;
        anim.ResetTrigger("Trigger_Land");
        anim.ResetTrigger("Trigger_Ult");

        if (idleCrossFadeDuration > 0f)
            anim.CrossFade(idleStateName, idleCrossFadeDuration, 0);
        else
            anim.Play(idleStateName, 0, 0f);

        if (debugLog)
            GameLogger.Log($"[LandingPresenter] Idle 전환");
    }

    // ═══════════════════════════════════════════════════
    //  헬퍼
    // ═══════════════════════════════════════════════════

    private GameObject SpawnShadow(Vector3 landPos, SupportLandingConfigSO cfg)
    {
        GameObject prefab = cfg.shadowPrefab != null ? cfg.shadowPrefab : defaultShadowPrefab;
        if (prefab == null) return null;

        Vector3 shadowPos = landPos + Vector3.down * 0.1f;
        GameObject shadow = Instantiate(prefab, shadowPos, Quaternion.identity);
        shadow.name = "SupportLandShadow";
        shadow.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer sr = shadow.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.2f;
            sr.color = c;
        }

        return shadow;
    }

    private static void UpdateShadow(GameObject shadow, float dropProgress)
    {
        if (shadow == null) return;

        shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, dropProgress);

        SpriteRenderer sr = shadow.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.2f, 0.6f, dropProgress);
            sr.color = c;
        }
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}