using UnityEngine;

public class itemFollowCamera : MonoBehaviour
{
    public Camera cam;
    public Vector3 offsetPos;
    public Vector3 offsetRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.rotation = Quaternion.Euler(0f, 100f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        // allows for items that the player holds to follow the center of the screen in accordance to the playerrs movement and rotation;
        if(cam != null)
        {
            transform.position = cam.transform.position + cam.transform.TransformDirection(offsetPos);

            transform.rotation = cam.transform.rotation *  Quaternion.Euler(offsetRotation);
        }
    }
}
