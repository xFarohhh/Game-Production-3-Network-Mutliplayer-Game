using UnityEngine;

public class HideableObject : MonoBehaviour
{
    [Tooltip("Where the player' body/mesh is placed when hiding")]
    public Transform hidePoint;

    [Tooltip("Camera position while hidden")]
    public Transform cameraPos;

    [Tooltip("How close the player must be to interact")]
    public float interactRange = 2f;

    public bool IsOccupied { get; private set; }

    public void SetOccupied(bool occupied) => IsOccupied = occupied;

    public bool CanHide(Vector3 playerPos)
    {
        if (IsOccupied) return false;
        return Vector3.Distance(playerPos, transform.position) <= interactRange;
    }
}