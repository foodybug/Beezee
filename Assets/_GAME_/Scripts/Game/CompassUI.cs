using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    public Transform cameraTransform;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            // North is typically +Z. 
            // When camera faces +Z (0 degrees), compass needle should point UP (0 degrees on Z axis).
            // When camera turns right to face +X (90 degrees), North is to the left, so compass needle should turn left (-90 or +270 on Z axis).
            Vector3 angles = transform.localEulerAngles;
            angles.z = cameraTransform.eulerAngles.y;
            transform.localEulerAngles = angles;
        }
    }
}
