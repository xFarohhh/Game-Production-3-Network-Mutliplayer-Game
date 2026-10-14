using UnityEngine;

[CreateAssetMenu(menuName ="Flyweight/Flyweight Settings")]
public class FlyweightSettings : ScriptableObject
{
    public FlyWeightType type;
    public GameObject prefab;

    [Header("Inventory")]
    public Sprite icon;
    public int maxStackSize = 1;
    public float weight = 1f;
    public float itemValue;

    public bool isPickupable => type != FlyWeightType.Projectiles
                             && type != FlyWeightType.AI
                             && type != FlyWeightType.Shops;

    public bool fitsInSlot => isPickupable && type != FlyWeightType.Backpacks;


    public virtual Flyweight Create()
    {
        var go = Instantiate(prefab);
        go.SetActive(false);
        go.name = prefab.name;

        var flyweight = go.AddComponent<Flyweight>();
        flyweight.settings = this;

        return flyweight;
    }

    public virtual void OnGet(Flyweight f) => f.gameObject.SetActive(true);
    public virtual void OnRelease(Flyweight f) => f.gameObject.SetActive(false);
    public virtual void OnDestroyPoolObject(Flyweight f) => Destroy(f.gameObject);

}

public enum FlyWeightType
{
    RangedWeapons,
    MeleeWeapons,
    AI,
    Projectiles,
    Throwables,
    Backpacks,
    Shops,
    LootItems,
    Consumeables
}
