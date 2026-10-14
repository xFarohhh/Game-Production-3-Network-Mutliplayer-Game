using Unity.Services.Core;
using UnityEngine;

public class testRelay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        await UnityServices.InitializeAsync();
    }
}
