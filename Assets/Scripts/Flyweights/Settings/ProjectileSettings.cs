using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Flyweight/Settings/Projectile Settings")]
public class ProjectileSettings : FlyweightSettings
{
    public float despawnDelay = 5f;
    public float speed = 10f;
    public float damage = 10f;

    public override Flyweight Create()
    {
        var go = Instantiate(prefab);
        go.SetActive(false);
        go.name = prefab.name;

        var flyweight = go.AddComponent<Projectile>();
        flyweight.settings = this;
        return flyweight;
    }
}
