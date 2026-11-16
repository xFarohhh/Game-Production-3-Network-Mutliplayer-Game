using UnityEngine;

public class HideableObject : MonoBehaviour
{
    [Tooltip("Where the player's body/mesh is placed when hiding")]
    public Transform hidePoint;

    [Tooltip("Camera position while hidden")]
    public Transform cameraPos;

    [Tooltip("How close the player must be to interact")]
    public float interactRange = 2f;

    public bool isOccupied = false;

    
    public Transform cameraAnchor => cameraPos;
}