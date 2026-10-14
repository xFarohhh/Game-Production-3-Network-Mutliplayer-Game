using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RangedWeapons : Flyweight
{
    [Header("Weapon Settings")]
    [SerializeField] RangedWeaponSettings Settings;

    [Header("Gun variables")]
    [SerializeField] int currentAmmo;
    public int ammo { get => currentAmmo; set => currentAmmo = value; }
    public int maxAmmo => Settings.maxAmmo;
    private bool isShooting;

    [Header("Burst Settings")]
    private int burstSize = 3;
    private bool isBursting = false;
    [SerializeField] float burstCoolDown;

    [Header("Reload Settings")]
    private bool isReloading;
    private bool reloadTriggered;
    private float reloadEndTime;
    public bool reloading => isReloading;
    public float reloadTimeRemaining => isReloading ? Mathf.Max(0f, reloadEndTime - Time.time) : 0f;

    [Header("Weapon Muzzle")]
    [SerializeField] GameObject[] muzzleFlashes;
    [SerializeField] GameObject muzzleObject;


    [Header("Aim Settings")]
    [SerializeField] bool isAiming;
   // [SerializeField] GameObject lazerObject;

    [Header("Chambered Bullet")]
    public List<ProjectileSettings> projectiles;

    private PlayerLocomotion wielder;


    private void Awake()
    {
        currentAmmo = Settings.maxAmmo;
        //lazerObject.SetActive(false);
    }

    private void Update()
    {
        handleAim();
    }

    #region Reload Logic

    public void OnReload(InputAction.CallbackContext context)
    {
        reloadTriggered = context.performed;

        if (reloadTriggered)
        {
            attemptReload();
        }
    }
    private IEnumerator reloadRoutine()
    {
        Debug.Log("Reload Routine has started. Setting is Reloading true and reloading current gun");
        isReloading = true;
        reloadEndTime = Time.time + Settings.reloadSpeed;
        yield return Helpers.GetWaitForSeconds(Settings.reloadSpeed);
        currentAmmo = Settings.maxAmmo;
        Debug.Log("Ending Reload Routine.Setting isReloading back to false");
        isReloading = false;
    }
    private void attemptReload()
    {
        if (isReloading) return;
        StartCoroutine(reloadRoutine());
    }
    #endregion Reload Logic

    #region Shoot Logic

    public void OnShoot(InputAction.CallbackContext context)
    {
        isShooting = context.performed;

        if (isShooting)
        {
            StartCoroutine(shootRoutine());
        }
    }

    public void playMuzzleFlash()
    {
        GameObject muzzleFlash = Instantiate(muzzleFlashes[Random.Range(0, muzzleFlashes.Length)], muzzleObject.transform);
        Destroy(muzzleFlash, Mathf.Min(Settings.fireInterval, 0.05f));
    }

    private bool canGunShoot()
    {
        if (currentAmmo <= 0) return false;
        if (isReloading) return false;
        return true;
    }

    private void shootGun()
    {
        playMuzzleFlash();
        currentAmmo--;
        spawnBullet();
    }


    void spawnBullet()
    {

        var bulletFlyweight = FlyweightFactory.Spawn(Settings.bulletPrefab);
        bulletFlyweight.transform.position = muzzleObject.transform.position;
        bulletFlyweight.transform.rotation = muzzleObject.transform.rotation;
    }

    private IEnumerator SingleShotRoutine()
    {
        if (!canGunShoot()) yield break;

        shootGun();
        yield return Helpers.GetWaitForSeconds(Settings.fireInterval);
        yield return new WaitUntil(() => !isShooting);
    }
    private IEnumerator BurstFireRoutine()
    {
        if (isBursting) yield break;
        isBursting = true;

        for (int i = 0; i < burstSize; i++)
        {
            if (!canGunShoot()) break;

            shootGun();
            yield return Helpers.GetWaitForSeconds(Settings.fireInterval);
        }
        yield return new WaitUntil(() => !isShooting);
        yield return Helpers.GetWaitForSeconds(burstCoolDown);
        isBursting = false;
    }
    private IEnumerator autoFireRoutine()
    {
        if (!canGunShoot()) yield break;

        while (isShooting && canGunShoot())
        {
            shootGun();
            yield return Helpers.GetWaitForSeconds(Settings.fireInterval);
        }
    }

    private IEnumerator shootRoutine()
    {
        isShooting = true;

        switch (Settings.fireType)
        {
            case fireType.Single:
                yield return SingleShotRoutine();
                break;
            case fireType.Burst:
                yield return BurstFireRoutine();
                break;
            case fireType.FullAuto:
                yield return autoFireRoutine();
                break;
        }
    }
    #endregion ShootLogic

    #region Aim Logic
    public void onWeaponAim(InputAction.CallbackContext context)
    {
        isAiming = context.performed;
    }

    private void handleAim()
    {
        //lazerObject.SetActive(isAiming);
        //logic to slow down sensitivity
    }
    #endregion Aim Logic
}

