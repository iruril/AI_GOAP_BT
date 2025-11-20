using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class ConnectionTokenUtils
{
    public static byte[] NewToken() => Guid.NewGuid().ToByteArray();

    public static int HashToken(byte[] token) => new Guid(token).GetHashCode();

    public static string TokenToString(byte[] token) => new Guid(token).ToString();
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;

    public GameObject MyPlayer { get; set; }

    private byte[] _connectionToken;

    public List<Gun.Gun> GunList = new();
    public Dictionary<string, GameObject> GunTable = new();

    public bool GunListReady = false;

    private const string _gunDataPath = "GunDatas/Guns.json";

    private void Awake()
    {
        if (_connectionToken == null)
        {
            _connectionToken = ConnectionTokenUtils.NewToken();
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }
        BetterStreamingAssets.Initialize();
    }

    private void Start()
    {
        WeaponDataLoad();
    }

    public void SetConnectionToken(byte[] token)
    {
        _connectionToken = token;
    }

    public byte[] GetConnectionToken()
    {
        return _connectionToken;
    }

    private void WeaponDataLoad()
    {
        string jsonData = FileUtility.LoadFile(_gunDataPath);
        Dictionary<string, List<Gun.Gun>> weaponList = JsonConvert.DeserializeObject<Dictionary<string, List<Gun.Gun>>>(jsonData);
        foreach (var item in weaponList["GunList"])
        {
            this.GunList.Add(item);
            GameObject weaponTemp = Resources.Load<GameObject>("Guns/" + item.WeaponName);
            this.GunTable.Add(item.WeaponName, weaponTemp);
        }
        GunListReady = true;
    }
}
