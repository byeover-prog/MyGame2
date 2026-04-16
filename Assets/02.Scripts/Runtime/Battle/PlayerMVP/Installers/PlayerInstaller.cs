using Battle.PlayerMVP.Adapters;
using Battle.PlayerMVP.Models;
using Battle.PlayerMVP.Presenters;
using Battle.PlayerMVP.Views;
using UnityEngine;

namespace Battle.PlayerMVP.Installers
{
    [DisallowMultipleComponent]
    public sealed class PlayerInstaller : MonoBehaviour
    {
        [Header("핵심")]
        [SerializeField, Tooltip("플레이어 런타임 모델입니다.")]
        private PlayerRuntimeModel runtimeModel;

        [SerializeField, Tooltip("플레이어 뷰입니다.")]
        private PlayerView playerView;

        [Header("프레젠터")]
        [SerializeField, Tooltip("이동 프레젠터입니다.")]
        private PlayerMovementPresenter movementPresenter;

        [SerializeField, Tooltip("체력 프레젠터입니다.")]
        private PlayerHealthPresenter healthPresenter;

        [SerializeField, Tooltip("전투 프레젠터(골격)입니다.")]
        private PlayerCombatPresenter combatPresenter;

        [Header("레거시 어댑터")]
        [SerializeField, Tooltip("기존 이동 시스템 어댑터입니다.")]
        private LegacyPlayerMovementAdapter legacyMovementAdapter;

        [SerializeField, Tooltip("기존 체력 시스템 어댑터입니다.")]
        private LegacyPlayerHealthAdapter legacyHealthAdapter;

        [Header("HUD 바인딩")]
        [SerializeField, Tooltip("HUD 바인딩 어댑터입니다.")]
        private PlayerHudBindingAdapter hudBindingAdapter;

        private void Awake()
        {
            if (runtimeModel == null || playerView == null)
            {
                GameLogger.LogWarning("[PlayerInstaller] 핵심 참조가 비어 있습니다.", this);
                return;
            }

            movementPresenter?.Initialize(runtimeModel, playerView, legacyMovementAdapter);
            healthPresenter?.Initialize(runtimeModel, playerView, legacyHealthAdapter);
            combatPresenter?.Initialize(runtimeModel);

            if (hudBindingAdapter == null)
                return;

            // 인스펙터 배선 누락 방지용 경고
            if (hudBindingAdapter.enabled == false)
                GameLogger.LogWarning("[PlayerInstaller] HUD 바인딩 어댑터가 비활성화되어 있습니다.", hudBindingAdapter);
        }
    }
}
