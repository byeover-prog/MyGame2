using Battle.PlayerMVP.Adapters;
using Battle.PlayerMVP.Models;
using Battle.PlayerMVP.Views;
using UnityEngine;

namespace Battle.PlayerMVP.Presenters
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementPresenter : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField, Tooltip("플레이어 런타임 모델입니다.")]
        private PlayerRuntimeModel runtimeModel;

        [SerializeField, Tooltip("플레이어 뷰 컴포넌트입니다.")]
        private PlayerView view;

        [SerializeField, Tooltip("기존 이동 시스템 어댑터입니다.")]
        private LegacyPlayerMovementAdapter legacyAdapter;

        [Header("옵션")]
        [SerializeField, Tooltip("모델 값을 뷰 애니메이션에 반영할지 여부입니다.")]
        private bool applyVisualState = true;

        public void Initialize(PlayerRuntimeModel model, PlayerView playerView, LegacyPlayerMovementAdapter movementAdapter)
        {
            runtimeModel = model;
            view = playerView;
            legacyAdapter = movementAdapter;
        }

        private void Update()
        {
            if (runtimeModel == null || legacyAdapter == null)
                return;

            Vector2 velocity = legacyAdapter.CurrentVelocity;
            Vector2 facing = legacyAdapter.CurrentFacing;

            runtimeModel.Stat.SetCurrentMoveSpeed(velocity.magnitude);
            runtimeModel.Stat.SetFacingDirection(facing);

            if (!applyVisualState || view == null)
                return;

            view.SetWalkAnimation(legacyAdapter.IsMoving);
            view.SetFacing(facing);
        }
    }
}
