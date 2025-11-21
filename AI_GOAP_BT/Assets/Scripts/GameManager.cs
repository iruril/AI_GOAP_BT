using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;

    public GameObject MyPlayer { get; set; }

    private byte[] _connectionToken;

    public Dictionary<string, (Gun gun, GameObject prefab)> GunTable = new();

    public bool GunListReady = false;

    private const string _gunDataPath = "GunDatas/Guns.json";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }

        if (_connectionToken == null)
        {
            _connectionToken = TokenUtility.NewToken();
        }

        BetterStreamingAssets.Initialize();
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
        Dictionary<string, List<Gun>> weaponList = JsonConvert.DeserializeObject<Dictionary<string, List<Gun>>>(jsonData);
        foreach (var item in weaponList["GunList"])
        {
            GameObject gunResource = Resources.Load<GameObject>("Guns/" + item.GunName);
            GunTable.Add(item.GunName, (item, gunResource));
        }
        GunListReady = true;
    }
}
