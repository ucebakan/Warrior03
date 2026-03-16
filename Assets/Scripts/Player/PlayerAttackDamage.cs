using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackDamage : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackBoxHalfExtents = new Vector3(1.2f, 1f, 1.5f);
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Hit VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = new Vector3(0f, 1f, 0f);

    public void DealDamage()
    {
        if (attackPoint == null) return;

        Collider[] hits = Physics.OverlapBox(
            attackPoint.position,
            attackBoxHalfExtents,
            attackPoint.rotation,
            enemyLayer
        );

        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) continue;

            if (damagedEnemies.Contains(enemyHealth))
                continue;

            damagedEnemies.Add(enemyHealth);

            SpawnHitEffect(enemyHealth);

            EnemyHitFlash hitFlash = hit.GetComponentInParent<EnemyHitFlash>();
            if (hitFlash != null)
            {
                hitFlash.PlayFlash();
            }

            enemyHealth.TakeDamage(damage);
        }
    }

    private void SpawnHitEffect(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || hitEffectPrefab == null) return;

        Vector3 spawnPosition = enemyHealth.transform.position + hitEffectOffset;
        Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            attackPoint.position,
            attackPoint.rotation,
            Vector3.one
        );

        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, attackBoxHalfExtents * 2f);
    }
}
