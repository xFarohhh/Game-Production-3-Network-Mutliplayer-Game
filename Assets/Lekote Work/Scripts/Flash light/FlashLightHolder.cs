using UnityEngine;

public class FlashlightHolder : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 offset = new Vector3(0.2f, -0.2f, 0.3f);

    void Update()
    {
        transform.position = cameraTransform.position + cameraTransform.TransformDirection(offset);
        transform.rotation = cameraTransform.rotation;
    }
}