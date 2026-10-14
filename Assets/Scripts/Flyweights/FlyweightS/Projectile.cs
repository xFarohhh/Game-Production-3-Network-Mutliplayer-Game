using System.Collections;
using UnityEngine;

public class Projectile : Flyweight
{
    ProjectileSettings Settings => (ProjectileSettings) base.settings;

    // Resolved on first use: layer lookups are not allowed while a MonoBehaviour is being constructed.
    static int? cachedEnvironmentMask;
    static int environmentMask => cachedEnvironmentMask ??= LayerMask.GetMask("Environment");

    private void OnEnable()
    {
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
    }

    // Returning to the pool deactivates the object; this stops the timer from releasing it a second time.
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        float distance = Settings.speed * Time.deltaTime;

        // Sweep the path about to be travelled so fast bullets cannot skip through thin geometry.
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, distance, environmentMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            returnToPool();
            return;
        }

        transform.Translate(Vector3.forward * distance);
    }

    IEnumerator DespawnAfterDelay(float delay)
    {
        yield return Helpers.GetWaitForSeconds(delay);
        returnToPool();
    }

    private void returnToPool()
    {
        FlyweightFactory.ReturnToPool(this);
    }
}
