// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 보스 공격이 대상을 맞췄을 때 공통으로 피해를 전달하는 처리기다.
// 자식 콜라이더를 맞춰도 실제 피해 대상 루트를 찾아서 한 번만 적용한다.
// 우선순위는 BossDamageReceiver2D -> IDamageable2D -> PlayerHealth 순서다.

public static class BossHitResolver
{
    public static Transform GetDamageRoot(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        BossDamageReceiver2D receiver = FindBossDamageReceiver(targetCollider);
        if (receiver != null)
        {
            return receiver.transform.root;
        }

        IDamageable2D damageable = FindDamageable(targetCollider);
        if (damageable is Component damageableComponent)
        {
            return damageableComponent.transform.root;
        }

        PlayerHealth playerHealth = FindPlayerHealth(targetCollider);
        if (playerHealth != null)
        {
            return playerHealth.transform.root;
        }

        return targetCollider.transform.root;
    }

    public static bool TryApplyDamage(Collider2D targetCollider, int damage, bool debugLog = false, Object context = null)
    {
        if (!IsValidRequest(targetCollider, damage))
        {
            return false;
        }

        BossDamageReceiver2D receiver = FindBossDamageReceiver(targetCollider);
        if (receiver != null)
        {
            receiver.TakeDamage(damage);

            if (debugLog)
            {
                Debug.Log($"[BossHitResolver] BossDamageReceiver2D로 피해 적용 | target={receiver.name} damage={damage}", context);
            }

            return true;
        }

        IDamageable2D damageable = FindDamageable(targetCollider);
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            if (debugLog)
            {
                string targetName = damageable is Component component ? component.name : "IDamageable2D";
                Debug.Log($"[BossHitResolver] IDamageable2D로 피해 적용 | target={targetName} damage={damage}", context);
            }

            return true;
        }

        PlayerHealth playerHealth = FindPlayerHealth(targetCollider);
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            if (debugLog)
            {
                Debug.Log($"[BossHitResolver] PlayerHealth로 피해 적용 | target={playerHealth.name} damage={damage}", context);
            }

            return true;
        }

        if (debugLog)
        {
            Debug.LogWarning($"[BossHitResolver] 피해 수신 대상을 찾지 못했습니다. | collider={targetCollider.name}", context);
        }

        return false;
    }

    private static bool IsValidRequest(Collider2D targetCollider, int damage)
    {
        if (targetCollider == null)
        {
            return false;
        }

        if (damage <= 0)
        {
            return false;
        }

        return true;
    }

    private static BossDamageReceiver2D FindBossDamageReceiver(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        BossDamageReceiver2D receiver = targetCollider.GetComponent<BossDamageReceiver2D>();
        if (receiver != null)
        {
            return receiver;
        }

        receiver = targetCollider.GetComponentInParent<BossDamageReceiver2D>();
        if (receiver != null)
        {
            return receiver;
        }

        receiver = targetCollider.GetComponentInChildren<BossDamageReceiver2D>(true);
        return receiver;
    }

    private static IDamageable2D FindDamageable(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        IDamageable2D damageable = targetCollider.GetComponent<IDamageable2D>();
        if (damageable != null)
        {
            return damageable;
        }

        damageable = targetCollider.GetComponentInParent<IDamageable2D>();
        if (damageable != null)
        {
            return damageable;
        }

        damageable = targetCollider.GetComponentInChildren<IDamageable2D>(true);
        return damageable;
    }

    private static PlayerHealth FindPlayerHealth(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        PlayerHealth playerHealth = targetCollider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth;
        }

        playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth;
        }

        playerHealth = targetCollider.GetComponentInChildren<PlayerHealth>(true);
        return playerHealth;
    }
}