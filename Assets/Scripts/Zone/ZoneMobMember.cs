using UnityEngine;

public class ZoneMobMember : MonoBehaviour
{
    [SerializeField] private MobZone mobZone;
    [SerializeField] private Vector3 homePosition;

    public MobZone MobZone => mobZone;
    public Vector3 HomePosition => homePosition;
    public bool HasPlayerInZone => mobZone != null && mobZone.HasPlayerInside;
    public Transform CurrentPlayer => mobZone != null ? mobZone.CurrentPlayer : null;

    public void Initialize(MobZone zone, Vector3 spawnPoint)
    {
        mobZone = zone;
        homePosition = spawnPoint;
    }

    public float DistanceToHome(Vector3 worldPosition)
    {
        Vector3 a = new Vector3(worldPosition.x, 0f, worldPosition.z);
        Vector3 b = new Vector3(homePosition.x, 0f, homePosition.z);
        return Vector3.Distance(a, b);
    }

    public bool IsInsideZone(Vector3 worldPosition)
    {
        if (mobZone == null || mobZone.ZoneCenter == null)
            return false;

        Vector3 a = new Vector3(worldPosition.x, 0f, worldPosition.z);
        Vector3 b = new Vector3(
            mobZone.ZoneCenter.position.x,
            0f,
            mobZone.ZoneCenter.position.z
        );

        return Vector3.Distance(a, b) <= mobZone.GetZoneRadius();
    }
}