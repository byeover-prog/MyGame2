using Battle.PlayerMVP.Models;
using UnityEngine;

namespace Battle.PlayerMVP.Presenters
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatPresenter : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField, Tooltip("플레이어 런타임 모델입니다.")]
        private PlayerRuntimeModel runtimeModel;

        [Header("판정")]
        [SerializeField, Tooltip("이동 중 전투 판정 속도 임계값입니다.")]
        private float moveCombatThreshold = 0.15f;

        [SerializeField, Tooltip("피격 후 전투 상태 유지 시간(초)입니다.")]
        private float combatHoldSeconds = 2f;

        private float _lastCombatTouchTime = -999f;

        public void Initialize(PlayerRuntimeModel model)
        {
            runtimeModel = model;
        }

        private void Update()
        {
            if (runtimeModel == null)
                return;

            // 이동량 기반 최소 전투 상태 골격
            if (runtimeModel.Stat.CurrentMoveSpeed >= moveCombatThreshold)
                TouchCombat();

            bool inCombat = Time.time - _lastCombatTouchTime <= combatHoldSeconds;
            runtimeModel.SetInCombat(inCombat);
        }

        // 외부(피격/공격 이벤트)에서 전투 상태 갱신 시 사용
        public void TouchCombat()
        {
            _lastCombatTouchTime = Time.time;
        }
    }
}
