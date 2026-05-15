using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackBoxHalfExtents = new Vector3(1.2f, 1f, 1.5f);
    [SerializeField] private LayerMask enemyLayer;

    [Header("Hit VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Hit VFX Scale")]
    [SerializeField] private bool useAutoHitEffectScale = true;
    [SerializeField] private float manualHitEffectScale = 3f;
    [SerializeField] private float autoScaleReferenceSize = 1f;
    [SerializeField] private float autoScaleMultiplier = 3f;
    [SerializeField] private float autoScaleExponent = 0.8f;
    [SerializeField] private float minHitEffectScale = 2f;
    [SerializeField] private float maxHitEffectScale = 10f;

    [Header("Hit SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitImpactSFX;
    [SerializeField][Range(0f, 1f)] private float hitImpactVolume = 0.9f;

    public int CurrentAttackDamage
    {
        get
        {
            if (playerStats == null)
            {
                ResolveReferences();
            }

            return playerStats == null ? 0 : playerStats.AttackDamage;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        SetupAudioSource();
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            playerStats = GetComponentInParent<PlayerStats>();
        }

        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void DealDamage()
    {
        if (attackPoint == null)
            return;

        if (playerStats == null)
        {
            ResolveReferences();
        }

        if (playerStats == null)
        {
            Debug.LogWarning("PlayerAttackDamage could not find PlayerStats. Damage was not applied.", this);
            return;
        }

        int currentDamage = playerStats.AttackDamage;

        if (currentDamage <= 0)
        {
            Debug.LogWarning("Player attack damage is 0 or lower. Damage was not applied.", this);
            return;
        }

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
            PlayHitImpactSFX();

            EnemyHitFlash hitFlash = hit.GetComponentInParent<EnemyHitFlash>();
            if (hitFlash != null)
            {
                hitFlash.PlayFlash();
            }

            enemyHealth.TakeDamage(currentDamage);

            ZoneEnemyAI enemyAI = hit.GetComponentInParent<ZoneEnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeHitReaction();
            }
        }
    }

    private void PlayHitImpactSFX()
    {
        if (audioSource == null) return;
        if (hitImpactSFX == null) return;

        audioSource.PlayOneShot(hitImpactSFX, hitImpactVolume);
    }

    private void SpawnHitEffect(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || hitEffectPrefab == null) return;

        Vector3 spawnPosition = GetHitEffectSpawnPosition(enemyHealth);
        GameObject spawnedEffect = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);

        if (spawnedEffect == null) return;

        float finalScale = useAutoHitEffectScale
            ? CalculateAutoHitEffectScale(enemyHealth.transform)
            : manualHitEffectScale;

        spawnedEffect.transform.localScale = Vector3.one * finalScale;
    }

    private Vector3 GetHitEffectSpawnPosition(EnemyHealth enemyHealth)
    {
        Transform root = enemyHealth.transform;
        Bounds bounds = GetCombinedRendererBounds(root);

        if (bounds.size != Vector3.zero)
        {
            return bounds.center + hitEffectOffset;
        }

        return root.position + hitEffectOffset;
    }

    private float CalculateAutoHitEffectScale(Transform enemyRoot)
    {
        float enemySize = GetEnemyVisualSize(enemyRoot);

        if (enemySize <= 0.001f)
        {
            return manualHitEffectScale;
        }

        float normalizedSize = enemySize / Mathf.Max(0.01f, autoScaleReferenceSize);
        float softenedSize = Mathf.Pow(normalizedSize, autoScaleExponent);
        float scaledValue = softenedSize * autoScaleMultiplier;

        return Mathf.Clamp(scaledValue, minHitEffectScale, maxHitEffectScale);
    }

    private float GetEnemyVisualSize(Transform enemyRoot)
    {
        Bounds rendererBounds = GetCombinedRendererBounds(enemyRoot);
        if (rendererBounds.size != Vector3.zero)
        {
            Vector3 size = rendererBounds.size;
            return Mathf.Max(size.x, size.y, size.z);
        }

        Bounds colliderBounds = GetCombinedColliderBounds(enemyRoot);
        if (colliderBounds.size != Vector3.zero)
        {
            Vector3 size = colliderBounds.size;
            return Mathf.Max(size.x, size.y, size.z);
        }

        return 0f;
    }

    private Bounds GetCombinedRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        Bounds combinedBounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer is ParticleSystemRenderer)
                continue;

            if (!renderer.enabled)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? combinedBounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private Bounds GetCombinedColliderBounds(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        Bounds combinedBounds = default;
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (!col.enabled)
                continue;

            if (col.isTrigger)
                continue;

            if (!hasBounds)
            {
                combinedBounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds ? combinedBounds : new Bounds(Vector3.zero, Vector3.zero);
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