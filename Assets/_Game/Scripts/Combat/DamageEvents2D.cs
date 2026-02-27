// UTF-8
using System;
using UnityEngine;

public static class DamageEvents2D
{
    public readonly struct DamagePopupRequest
    {
        public readonly Vector3 WorldPos;
        public readonly int Amount;
        public readonly DamageElement2D Element;

        public DamagePopupRequest(Vector3 worldPos, int amount, DamageElement2D element)
        {
            WorldPos = worldPos;
            Amount = amount;
            Element = element;
        }
    }

    public static event Action<DamagePopupRequest> OnDamagePopupRequested;

    public static void RaiseDamagePopup(Vector3 worldPos, int amount, DamageElement2D element)
    {
        OnDamagePopupRequested?.Invoke(new DamagePopupRequest(worldPos, amount, element));
    }
}