using Battle.PlayerMVP.Adapters;
using Battle.PlayerMVP.Models;
using Battle.PlayerMVP.Views;
using UnityEngine;

namespace Battle.PlayerMVP.Presenters
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealthPresenter : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField, Tooltip("플레이어 런타임 모델입니다.")]
        private PlayerRuntimeModel runtimeModel;

        [SerializeField, Tooltip("플레이어 뷰 컴포넌트입니다.")]
        private PlayerView view;

        [SerializeField, Tooltip("기존 체력 시스템 어댑터입니다.")]
        private LegacyPlayerHealthAdapter legacyAdapter;

        private int _lastHp = -1;
        private int _lastMaxHp = -1;
        private bool _lastDead;

        public void Initialize(PlayerRuntimeModel model, PlayerView playerView, LegacyPlayerHealthAdapter healthAdapter)
        {
            runtimeModel = model;
            view = playerView;
            legacyAdapter = healthAdapter;
        }

        private void Update()
        {
            if (runtimeModel == null || legacyAdapter == null)
                return;

            int max = legacyAdapter.MaxHp;
            int current = legacyAdapter.CurrentHp;
            bool dead = legacyAdapter.IsDead;

            if (current == _lastHp && max == _lastMaxHp && dead == _lastDead)
                return;

            _lastHp = current;
            _lastMaxHp = max;
            _lastDead = dead;

            runtimeModel.Health.SetAbsolute(max, current, dead);
            runtimeModel.SetCanTakeDamage(!dead);

            // 사망 시 뷰 속도를 안전하게 0으로 고정
            if (dead && view != null)
                view.SetVelocity(Vector2.zero);
        }
    }
}
