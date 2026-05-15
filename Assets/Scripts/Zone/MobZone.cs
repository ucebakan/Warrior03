using UnityEngine;

public class MobZone : MonoBehaviour
{
    [SerializeField] private Transform zoneCenter;
    [SerializeField] private SphereCollider zoneTrigger;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (zoneTrigger == null) return;

        if (other != null && other.CompareTag("Player"))
        {
            HasPlayerInside = true;
            CurrentPlayer = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (zoneTrigger == null) return;

        if (other != null && other.CompareTag("Player"))
        {
            HasPlayerInside = false;
            CurrentPlayer = null;
        }
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
}