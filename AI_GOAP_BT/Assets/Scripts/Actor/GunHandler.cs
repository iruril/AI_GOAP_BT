using RootMotion.FinalIK;
using UnityEngine;
using System.Collections.Generic;

public class GunHandler : MonoBehaviour
{
    private BipedIK myIK;
    [Header("Bullet Prefab")]
    [SerializeField] GameObject Bullet;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform GunPos;
    [SerializeField] Transform LeftHandIKTarget;
    [SerializeField] Transform Muzzle;

    private Gun currentGun;
    private GameObject currentGunModel;

    private BulletPool bulletPool;

    private Dictionary<string, (Gun gun, GameObject instance)> gunHistory = new();

    void Awake()
    {
        myIK = GetComponent<BipedIK>();
        myIK.solvers.leftHand.target = LeftHandIKTarget;
        myIK.solvers.leftHand.IKPositionWeight = 1f;

        bulletPool = GetComponent<BulletPool>();
    }

    void Start()
    {
        LoadGun("AK-15");
    }

    void Update()
    {
        
    }

    void LoadGun(string gunName)
    {
        bool cached = false;
        (Gun gun, GameObject instance) gunData;

        if (gunHistory.ContainsKey(gunName))
        {
            cached = true;
            gunData = gunHistory[gunName];
        }
        else
        {
            gunData = GameManager.Instance.GunTable[gunName];
            gunHistory.Add(gunName, gunData);
        }

        currentGun = gunData.gun;

        if (currentGunModel != null)
            currentGunModel.SetActive(false);

        if (!cached)
        {
            currentGunModel = Instantiate(gunData.instance);
        }
        else
        {
            currentGunModel = gunData.instance;
            currentGunModel.SetActive(true);
        }

        currentGunModel.transform.SetParent(GunPos, false);
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;

        ApplyGunTransforms(currentGun);
    }

    void ApplyGunTransforms(Gun gunData)
    {
        GunPos.localPosition = gunData.GunPosition;

        Muzzle.localPosition = gunData.MuzzlePosition;

        LeftHandIKTarget.localPosition = gunData.LeftHandIKPosition;
        LeftHandIKTarget.localEulerAngles = gunData.LeftHandIKRotation;
    }

    public void Fire()
    {
        if (currentGun == null) return;
        //총알 발사
        //머즐 플래쉬
        EffectPoolManager.SpawnFromPool("MuzzleFlash", Muzzle.position, Muzzle.rotation);

        bulletPool.SpawnBullet(
            Muzzle.position,
            Muzzle.rotation,
            1 << gameObject.layer,
            Muzzle.position,                             // shotOrigin
            currentGun.GunInfo.ProjectileSpeed,          // 총알 속도
            currentGun.GunInfo.RoundDamage               // 데미지
        );
    }
}
