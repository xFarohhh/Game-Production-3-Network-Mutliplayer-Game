using System;
using UnityEngine;
using UnityEngine.UI;

public class MonsterLook : MonoBehaviour
{
    [SerializeField] float lookTimer;
    
    [SerializeField] float lookRange;
    [SerializeField] LayerMask playerLayer;

    [Header("CrossHair")]
    private Image crosshair;

    private void OnEnable() // Function currently is to assign the crosshair once the gameObject is enabled
    {
        if(crosshair == null)
        {
            crosshair = GameObject.FindGameObjectWithTag("Crosshair").GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("CrossHair tag has not been assigned to the crosshair. Ensure the Crosshair Gameobject has the corresponding Tag");
            return;
        }
        
    }

    private void Update()
    {
        OnMonsterLook();
    }

    private void OnMonsterLook()
    {
        RaycastHit hit;

        bool hasSeenPlayer = Physics.Raycast(transform.position, transform.forward, out hit, lookRange, playerLayer);

        if (hasSeenPlayer)
        {
            lookTimer += Time.deltaTime;
        }
        else
        {
            lookTimer = 0;
        }
        crosshair.color = hasSeenPlayer ? Color.red : Color.white; // sets the color to red if the player has been seen : Sets the color to white if the player has nmot been detected.
    }
}
