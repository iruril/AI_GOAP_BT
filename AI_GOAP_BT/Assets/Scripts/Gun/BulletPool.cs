using System.Collections.Generic;
using System;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [Serializable]
    public class Pool
    {
        public GameObject Bullet;
        public int Size;
    }

    [SerializeField]
    private Pool _bulletPool;
    private Queue<GameObject> _bulletQueue;
    private GameObject _bulletContainer;

    private void Awake()
    {
        _bulletContainer = new GameObject($"{name}_Bullets");
    }

    private void Start()
    {
        _bulletQueue = new Queue<GameObject>();
        for (int i = 0; i < _bulletPool.Size; i++)
        {
            CreateNewObject(_bulletPool.Bullet);
        }
    }

    private void OnDisable()
    {

    }

    public GameObject SpawnBullet(Vector3 position, Quaternion rotation, LayerMask myTeamLayer, Vector3 origin, float projectileSpeed, float damage)
    {
        GameObject obj = _SpawnBullet(position, rotation);

        if (obj.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.Init(myTeamLayer, origin, projectileSpeed, damage);
        }

        return obj;
    }

    private GameObject _SpawnBullet(Vector3 position, Quaternion rotation)
    {
        if (_bulletQueue.Count <= 0)
        {
            CreateNewObject(_bulletPool.Bullet);
        }

        GameObject objectToSpawn = _bulletQueue.Dequeue();
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);
        return objectToSpawn;
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform);
        obj.name = "Bullet";
        obj.transform.parent = _bulletContainer.transform;

        if (obj.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.SetBulletPool(this);
        }

        obj.SetActive(false); // 비활성화시 ReturnToPool을 하므로 Enqueue가 됨
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        _bulletQueue.Enqueue(obj);
    }
}
