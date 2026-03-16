// UTF-8
// Assets/_Game/Scripts/VFX/VFXPoolPrewarmer.cs
// ★ VFX v2 — GPT 리뷰 #1 #4 반영: 프리팹별 제한 + 폭발 강제회수 제외
using UnityEngine;

/// <summary>
/// 게임 시작 시 VFX 풀을 미리 채우고, 프리팹별 설정을 적용합니다.
/// [GameProjectileManager] 오브젝트 또는 별도 오브젝트에 붙여서 사용.
/// </summary>
public class VFXPoolPrewarmer : MonoBehaviour
{
    [Header("암흑구 본체 VFX")]
    [SerializeField] private GameObject darkOrbBodyVfx;
    [SerializeField] private int bodyPrewarmCount = 15;
    [Tooltip("동시 활성 본체 VFX 최대 수")]
    [SerializeField] private int bodyMaxActive = 10;

    [Header("암흑구 폭발 VFX")]
    [SerializeField] private GameObject darkOrbExplosionVfx;
    [SerializeField] private int explosionPrewarmCount = 12;
    [Tooltip("동시 활성 폭발 VFX 최대 수")]
    [SerializeField] private int explosionMaxActive = 15;
    [Tooltip("폭발 VFX는 강제 회수하지 않음 (재생 끊김 방지)")]
    [SerializeField] private bool explosionNoForceRecycle = true;

    [Header("기본 제한")]
    [Tooltip("프리팹별 설정이 없는 VFX의 기본 동시 활성 제한")]
    [SerializeField] private int defaultMaxActive = 12;

    private void Awake()
    {
        // 기본 제한값
        VFXPool.DefaultMaxActive = defaultMaxActive;

        // 본체 VFX
        if (darkOrbBodyVfx != null)
        {
            VFXPool.Prewarm(darkOrbBodyVfx, bodyPrewarmCount);
            VFXPool.SetMaxActive(darkOrbBodyVfx, bodyMaxActive);
        }

        // 폭발 VFX
        if (darkOrbExplosionVfx != null)
        {
            VFXPool.Prewarm(darkOrbExplosionVfx, explosionPrewarmCount);
            VFXPool.SetMaxActive(darkOrbExplosionVfx, explosionMaxActive);

            // ★ [GPT #4] 폭발 VFX는 강제 회수 제외 — 재생 중 끊김 방지
            if (explosionNoForceRecycle)
                VFXPool.SetNoForceRecycle(darkOrbExplosionVfx);
        }

        Debug.Log($"<color=lime>[VFXPoolPrewarmer] v2 완료 | body={bodyPrewarmCount}(max{bodyMaxActive}) | explosion={explosionPrewarmCount}(max{explosionMaxActive}, noForce={explosionNoForceRecycle})</color>", this);
    }
}