using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackDamage : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.6f;
    [SerializeField] private int damage = 1;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool debugLogs = true;

    public void DealDamage()
    {
        if (attackPoint == null)
        {
            if (debugLogs)
                Debug.LogWarning($"{name} EnemyAttackDamage: attackPoint is NULL");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius);
        HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();

        if (debugLogs)
            Debug.Log($"{name} DealDamage called | hits found: {hits.Length}");

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) continue;

            if (damagedPlayers.Contains(playerHealth))
                continue;

            damagedPlayers.Add(playerHealth);

            if (debugLogs)
                Debug.Log($"{name} damaged player: {playerHealth.name}");

            playerHealth.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
