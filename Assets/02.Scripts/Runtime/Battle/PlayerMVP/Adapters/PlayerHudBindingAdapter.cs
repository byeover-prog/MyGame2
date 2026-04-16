using Battle.PlayerMVP.Models;
using TMPro;
using UnityEngine;

namespace Battle.PlayerMVP.Adapters
{
    [DisallowMultipleComponent]
    public sealed class PlayerHudBindingAdapter : MonoBehaviour
    {
        [Header("모델")]
        [SerializeField, Tooltip("플레이어 런타임 모델입니다.")]
        private PlayerRuntimeModel runtimeModel;

        [Header("HUD 참조")]
        [SerializeField, Tooltip("기존 HP 바 UI 컴포넌트입니다.")]
        private PlayerHPUI hpUI;

        [SerializeField, Tooltip("HP 텍스트 라벨입니다.")]
        private TMP_Text hpText;

        private void OnEnable()
        {
            if (runtimeModel == null)
                return;

            runtimeModel.Health.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(runtimeModel.Health.CurrentHp, runtimeModel.Health.MaxHp);
        }

        private void OnDisable()
        {
            if (runtimeModel == null)
                return;

            runtimeModel.Health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (hpUI != null)
            {
                hpUI.SetMaxHp(max);
                hpUI.UpdateHp(current);
            }

            if (hpText != null)
                hpText.text = $"{current}/{max}";
        }
    }
}
