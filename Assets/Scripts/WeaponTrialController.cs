using UnityEngine;

public class WeaponTrailController : MonoBehaviour
{
    [SerializeField] private GameObject trailRoot;
    [SerializeField] private TrailRenderer[] trails;

    private void Awake()
    {
        if (trailRoot != null)
        {
            trails = trailRoot.GetComponentsInChildren<TrailRenderer>(true);
        }
        else if (trails == null || trails.Length == 0)
        {
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        DisableAndClearTrails();
    }

    public void TrailOn()
    {
        if (trails == null) return;

        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null) continue;

            trails[i].Clear();
            trails[i].emitting = true;
        }
    }

    public void TrailOff()
    {
        if (trails == null) return;

        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null) continue;

            trails[i].emitting = false;
        }
    }

    public void DisableAndClearTrails()
    {
        if ((trails == null || trails.Length == 0) && trailRoot != null)
        {
            trails = trailRoot.GetComponentsInChildren<TrailRenderer>(true);
        }

        if (trails == null) return;

        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null) continue;

            trails[i].emitting = false;
            trails[i].Clear();
        }
    }
}