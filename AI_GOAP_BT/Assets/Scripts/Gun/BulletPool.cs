using System.Collections.Generic;
using System;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    private static BulletPool _instance;

    [Serializable]
    public class Pool
    {
        public GameObject Bullet;
        public int Size;
    }

    [SerializeField]
    private Pool _bulletPool;
    private Queue<GameObject> _bulletQueue;

    private void Awake()
    {
        _instance = this;
        InitPool();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void InitPool()
    {
        _bulletQueue = new Queue<GameObject>();
        for (int i = 0; i < _bulletPool.Size; i++)
        {
            CreateNewObject(_bulletPool.Bullet);
        }
    }

    public static BulletPool GetInstance()
    {
        return _instance;
    }
    private GameObject _SpawnBullet(Vector3 position, Quaternion rotation)
    {
        GameObject objectToSpawn;

        if (_bulletQueue.Count > 0)
        {
            objectToSpawn = _bulletQueue.Dequeue();
        }
        else
        {
            objectToSpawn = CreateNewObject(_bulletPool.Bullet);
        }

        objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        objectToSpawn.SetActive(true);
        return objectToSpawn;
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform);
        obj.name = "Bullet";

        if (obj.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.SetBulletPool(this);
        }

        obj.SetActive(false); // 비활성화시 ReturnToPool을 하므로 Enqueue가 됨
        return obj;
    }

    public static void ReturnToPool(GameObject obj)
    {
        if (_instance == null) return;

        _instance._bulletQueue.Enqueue(obj);
    }

    public static GameObject SpawnBullet(Vector3 position, Quaternion rotation, LayerMask myTeamLayer, Vector3 origin, float projectileSpeed, float damage, float headMultiplier, float lagTime, GunHandler owner = null)
    {
        if (_instance == null) return null;

        GameObject obj = _instance._SpawnBullet(position, rotation);
        if (obj.TryGetComponent<Bullet>(out var bullet))
        {
            if (owner != null) bullet.SetOwner(owner);
            bullet.Init(myTeamLayer, origin, projectileSpeed, damage, headMultiplier, lagTime);
        }
        return obj;
    }
}
