using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneMobSpawner : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private Transform zoneCenter;
    [SerializeField] private SphereCollider zoneTrigger;
    [SerializeField] private MobZone mobZone;
    [SerializeField] private bool useTriggerRadius = true;
    [SerializeField] private float fallbackSpawnRadius = 3f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Spawn")]
    [SerializeField] private GameObject mobPrefab;
    [SerializeField] private int spawnCount = 5;
    [SerializeField] private float minDistanceBetweenMobs = 0.75f;
    [SerializeField] private int maxSpawnAttemptsPerMob = 25;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform spawnedMobsParent;

    [Header("Respawn")]
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float respawnDelay = 5f;

    [Header("Ground Align")]
    [SerializeField] private bool autoAlignToGround = true;
    [SerializeField] private float groundCheckStartHeight = 20f;
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private float visualGroundClearance = 0.02f;
    [SerializeField] private bool ignoreParticleRenderers = true;

    private readonly List<GameObject> spawnedMobs = new List<GameObject>();

    private int pendingRespawnCount = 0;
    private int spawnSequence = 0;
    private bool hasSpawnedInitialBatch = false;

    private void Reset()
    {
        AutoSetupReferences();
    }

    private void Awake()
    {
        AutoSetupReferences();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    private void Update()
    {
        CleanupNullEntries();

        if (!Application.isPlaying)
            return;

        if (!hasSpawnedInitialBatch)
            return;

        if (!enableRespawn)
            return;

        EnsureDesiredPopulation();
    }

    private void AutoSetupReferences()
    {
        if (zoneTrigger == null)
        {
            SphereCollider[] colliders = GetComponentsInChildren<SphereCollider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger)
                {
                    zoneTrigger = colliders[i];
                    break;
                }
            }
        }

        if (zoneCenter == null)
        {
            zoneCenter = zoneTrigger != null ? zoneTrigger.transform : transform;
        }

        if (mobZone == null)
        {
            mobZone = GetComponentInChildren<MobZone>(true);
        }

        if (spawnedMobsParent == null)
        {
            spawnedMobsParent = transform;
        }
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        StopAllCoroutines();
        pendingRespawnCount = 0;

        ClearSpawnedMobsInternal();

        if (mobPrefab == null)
        {
            Debug.LogWarning($"{name} | ZoneMobSpawner: Mob Prefab atanmadý.");
            return;
        }

        if (zoneCenter == null)
        {
            Debug.LogWarning($"{name} | ZoneMobSpawner: Zone Center bulunamadý.");
            return;
        }

        if (mobZone == null)
        {
            Debug.LogWarning($"{name} | ZoneMobSpawner: MobZone bulunamadý.");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOneMob();
        }

        hasSpawnedInitialBatch = true;
    }

    [ContextMenu("Clear Spawned Mobs")]
    public void ClearSpawnedMobs()
    {
        StopAllCoroutines();
        pendingRespawnCount = 0;
        hasSpawnedInitialBatch = false;
        ClearSpawnedMobsInternal();
    }

    private void ClearSpawnedMobsInternal()
    {
        for (int i = spawnedMobs.Count - 1; i >= 0; i--)
        {
            if (spawnedMobs[i] == null) continue;

            if (Application.isPlaying)
            {
                Destroy(spawnedMobs[i]);
            }
            else
            {
                DestroyImmediate(spawnedMobs[i]);
            }
        }

        spawnedMobs.Clear();
    }

    private void EnsureDesiredPopulation()
    {
        int aliveCount = spawnedMobs.Count;
        int reservedCount = aliveCount + pendingRespawnCount;
        int missingCount = spawnCount - reservedCount;

        if (missingCount <= 0)
            return;

        for (int i = 0; i < missingCount; i++)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        pendingRespawnCount++;

        yield return new WaitForSeconds(respawnDelay);

        CleanupNullEntries();

        if (hasSpawnedInitialBatch && mobPrefab != null && zoneCenter != null && mobZone != null)
        {
            if (spawnedMobs.Count < spawnCount)
            {
                SpawnOneMob();
            }
        }

        pendingRespawnCount = Mathf.Max(0, pendingRespawnCount - 1);
    }

    private bool SpawnOneMob()
    {
        float radius = GetWorldRadius();
        List<Vector3> usedPositions = GetAliveMobPositions();

        Vector3 spawnGroundPoint;
        bool found = TryGetSpawnGroundPoint(radius, usedPositions, out spawnGroundPoint);

        if (!found)
        {
            Debug.LogWarning($"{name} | ZoneMobSpawner: Respawn için uygun pozisyon bulunamadý.");
            return false;
        }

        GameObject mob = Instantiate(
            mobPrefab,
            spawnGroundPoint,
            mobPrefab.transform.rotation,
            spawnedMobsParent
        );

        spawnSequence++;
        mob.name = $"{mobPrefab.name}_{spawnSequence:00}";

        if (autoAlignToGround)
        {
            AlignMobToGround(mob, spawnGroundPoint);
        }

        ZoneMobMember zoneMember = mob.GetComponent<ZoneMobMember>();

        if (zoneMember == null)
        {
            zoneMember = mob.AddComponent<ZoneMobMember>();
        }

        zoneMember.Initialize(mobZone, mob.transform.position);

        spawnedMobs.Add(mob);
        return true;
    }

    private List<Vector3> GetAliveMobPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < spawnedMobs.Count; i++)
        {
            if (spawnedMobs[i] == null) continue;
            positions.Add(spawnedMobs[i].transform.position);
        }

        return positions;
    }

    private void CleanupNullEntries()
    {
        for (int i = spawnedMobs.Count - 1; i >= 0; i--)
        {
            if (spawnedMobs[i] == null)
            {
                spawnedMobs.RemoveAt(i);
            }
        }
    }

    private bool TryGetSpawnGroundPoint(float radius, List<Vector3> usedPositions, out Vector3 spawnGroundPoint)
    {
        for (int attempt = 0; attempt < maxSpawnAttemptsPerMob; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidateXZ = zoneCenter.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            Vector3 groundPoint;
            bool hitGround = TryGetGroundPoint(candidateXZ, out groundPoint);

            if (!hitGround)
            {
                continue;
            }

            bool tooClose = false;

            for (int i = 0; i < usedPositions.Count; i++)
            {
                Vector3 a = new Vector3(groundPoint.x, 0f, groundPoint.z);
                Vector3 b = new Vector3(usedPositions[i].x, 0f, usedPositions[i].z);

                if (Vector3.Distance(a, b) < minDistanceBetweenMobs)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            spawnGroundPoint = groundPoint;
            return true;
        }

        spawnGroundPoint = zoneCenter.position;
        return false;
    }

    private bool TryGetGroundPoint(Vector3 position, out Vector3 groundPoint)
    {
        Vector3 rayStart = new Vector3(
            position.x,
            position.y + groundCheckStartHeight,
            position.z
        );

        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = position;
        return false;
    }

    private void AlignMobToGround(GameObject mob, Vector3 groundPoint)
    {
        float lowestVisualY = GetLowestVisualY(mob);
        float deltaY = (groundPoint.y + visualGroundClearance) - lowestVisualY;
        mob.transform.position += new Vector3(0f, deltaY, 0f);
    }

    private float GetLowestVisualY(GameObject mob)
    {
        Renderer[] renderers = mob.GetComponentsInChildren<Renderer>(true);

        bool foundRenderer = false;
        float lowestY = float.MaxValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (ignoreParticleRenderers && renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            if (renderer.bounds.size == Vector3.zero)
            {
                continue;
            }

            foundRenderer = true;
            lowestY = Mathf.Min(lowestY, renderer.bounds.min.y);
        }

        if (!foundRenderer)
        {
            return mob.transform.position.y;
        }

        return lowestY;
    }

    private float GetWorldRadius()
    {
        if (useTriggerRadius && zoneTrigger != null)
        {
            float maxScale = Mathf.Max(
                zoneTrigger.transform.lossyScale.x,
                zoneTrigger.transform.lossyScale.y,
                zoneTrigger.transform.lossyScale.z
            );

            return zoneTrigger.radius * maxScale;
        }

        return fallbackSpawnRadius;
    }

    private void OnDrawGizmosSelected()
    {
        AutoSetupReferences();

        if (zoneCenter == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(zoneCenter.position, GetEditorRadius());
    }

    private float GetEditorRadius()
    {
        if (useTriggerRadius && zoneTrigger != null)
        {
            float maxScale = Mathf.Max(
                zoneTrigger.transform.lossyScale.x,
                zoneTrigger.transform.lossyScale.y,
                zoneTrigger.transform.lossyScale.z
            );

            return zoneTrigger.radius * maxScale;
        }

        return fallbackSpawnRadius;
    }
}