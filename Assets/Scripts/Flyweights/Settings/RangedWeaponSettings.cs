using UnityEngine;

[CreateAssetMenu(menuName = "Flyweight/Settings/Ranged Weapon Settings")]
public class RangedWeaponSettings : FlyweightSettings
{
    public int maxAmmo;
    public float fireInterval;
    public float reloadSpeed;
    public float weaponRange;
    public ProjectileSettings bulletPrefab;
    public fireType fireType;

    // Could maybe create the weapon for weapon purchasing from AI Vendors
    public override Flyweight Create()
    {
        var go = Instantiate(prefab);
        go.SetActive(false);
        go.name = prefab.name;

        var flyweight = go.AddComponent<RangedWeapons>();
        flyweight.settings = this;
        return flyweight;
    }
}

public enum fireType
{
    Single,
    Burst,
    FullAuto
}
