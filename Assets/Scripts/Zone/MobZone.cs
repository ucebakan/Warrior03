using UnityEngine;

public class MobZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform zoneCenter;
    [SerializeField] private SphereCollider zoneTrigger;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool validatePlayerByDistanceEveryFrame = true;
    [SerializeField] private float outsideTolerance = 0.15f;
    [SerializeField] private bool clearWhenPlayerDeadOrRespawning = true;

    [Header("Debug")]
    [SerializeField] private bool logZoneEvents = false;

    public Transform ZoneCenter => zoneCenter != null ? zoneCenter : transform;
    public SphereCollider ZoneTrigger => zoneTrigger;
    public bool HasPlayerInside { get; private set; }
    public Transform CurrentPlayer { get; private set; }

    private void Reset()
    {
        AutoSetupReferences();
    }

    private void Awake()
    {
        AutoSetupReferences();
    }

    private void Update()
    {
        if (!validatePlayerByDistanceEveryFrame)
            return;

        ValidateCurrentPlayerState();
    }

    private void AutoSetupReferences()
    {
        if (zoneTrigger == null)
        {
            zoneTrigger = GetComponent<SphereCollider>();
        }

        if (zoneCenter == null)
        {
            zoneCenter = transform;
        }

        if (zoneTrigger != null)
        {
            zoneTrigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySetPlayerFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetPlayerFromCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Transform possiblePlayer = GetPlayerRootFromCollider(other);

        if (possiblePlayer == null)
            return;

        if (CurrentPlayer == possiblePlayer)
        {
            ForceClearPlayer();

            if (logZoneEvents)
            {
                Debug.Log($"{name} | Player exited zone.", this);
            }
        }
    }

    private void TrySetPlayerFromCollider(Collider other)
    {
        Transform playerRoot = GetPlayerRootFromCollider(other);

        if (playerRoot == null)
            return;

        if (clearWhenPlayerDeadOrRespawning && IsPlayerDeadOrRespawning(playerRoot))
        {
            ForceClearPlayer();
            return;
        }

        if (!IsWorldPointInsideZone(playerRoot.position, outsideTolerance))
            return;

        HasPlayerInside = true;
        CurrentPlayer = playerRoot;

        if (logZoneEvents)
        {
            Debug.Log($"{name} | Player entered/stayed in zone.", this);
        }
    }

    private Transform GetPlayerRootFromCollider(Collider other)
    {
        if (other == null)
            return null;

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            if (other.CompareTag(playerTag))
                return other.transform;

            if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
                return other.transform.root;

            Transform parent = other.transform.parent;

            while (parent != null)
            {
                if (parent.CompareTag(playerTag))
                    return parent;

                parent = parent.parent;
            }

            return null;
        }

        return other.transform.root != null ? other.transform.root : other.transform;
    }

    private void ValidateCurrentPlayerState()
    {
        if (!HasPlayerInside && CurrentPlayer == null)
            return;

        if (CurrentPlayer == null)
        {
            ForceClearPlayer();
            return;
        }

        if (clearWhenPlayerDeadOrRespawning && IsPlayerDeadOrRespawning(CurrentPlayer))
        {
            ForceClearPlayer();

            if (logZoneEvents)
            {
                Debug.Log($"{name} | Player cleared because dead/respawning.", this);
            }

            return;
        }

        if (!IsWorldPointInsideZone(CurrentPlayer.position, outsideTolerance))
        {
            ForceClearPlayer();

            if (logZoneEvents)
            {
                Debug.Log($"{name} | Player cleared because outside zone by distance check.", this);
            }
        }
    }

    private bool IsPlayerDeadOrRespawning(Transform player)
    {
        if (player == null)
            return false;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = player.GetComponentInChildren<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = player.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsDead)
            return true;

        PlayerRespawnController respawnController = player.GetComponent<PlayerRespawnController>();

        if (respawnController == null)
            respawnController = player.GetComponentInChildren<PlayerRespawnController>();

        if (respawnController == null)
            respawnController = player.GetComponentInParent<PlayerRespawnController>();

        if (respawnController != null && respawnController.IsRespawning)
            return true;

        return false;
    }

    public void ForceClearPlayer()
    {
        HasPlayerInside = false;
        CurrentPlayer = null;
    }

    public bool IsTransformInsideZone(Transform target, float tolerance = 0f)
    {
        if (target == null)
            return false;

        return IsWorldPointInsideZone(target.position, tolerance);
    }

    public bool IsWorldPointInsideZone(Vector3 worldPosition, float tolerance = 0f)
    {
        if (ZoneCenter == null)
            return false;

        Vector3 center = ZoneCenter.position;

        Vector3 a = new Vector3(worldPosition.x, 0f, worldPosition.z);
        Vector3 b = new Vector3(center.x, 0f, center.z);

        return Vector3.Distance(a, b) <= GetZoneRadius() + Mathf.Max(0f, tolerance);
    }

    public float GetZoneRadius()
    {
        if (zoneTrigger == null) return 0f;

        float maxScale = Mathf.Max(
            zoneTrigger.transform.lossyScale.x,
            zoneTrigger.transform.lossyScale.y,
            zoneTrigger.transform.lossyScale.z
        );

        return zoneTrigger.radius * maxScale;
    }

    private void OnDrawGizmosSelected()
    {
        AutoSetupReferences();

        if (ZoneCenter == null)
            return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(ZoneCenter.position, GetZoneRadius());
    }
}