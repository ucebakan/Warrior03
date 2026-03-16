using UnityEngine;

public class WorldSpaceBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool useLateUpdate = true;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (!useLateUpdate)
            FaceCamera();
    }

    private void LateUpdate()
    {
        if (useLateUpdate)
            FaceCamera();
    }

    private void FaceCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        Vector3 direction = transform.position - targetCamera.transform.position;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
