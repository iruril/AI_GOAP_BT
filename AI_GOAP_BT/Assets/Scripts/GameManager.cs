using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Player.Input;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;

    public GameObject MyPlayer { get; set; }
    public InputRecorder InputMap { get; private set; }

    private byte[] _connectionToken;

    public Dictionary<string, (Gun gun, GameObject prefab)> GunTable = new();

    public bool GunListReady = false;

    private const string _gunDataPath = "GunDatas/Guns.json";

    public float RespawnTime = 15f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }

        if (_connectionToken == null)
        {
            _connectionToken = TokenUtility.NewToken();
        }

        InputMap = GetComponent<InputRecorder>();
        BetterStreamingAssets.Initialize();
        WeaponDataLoad();
    }
    public static GameManager GetInstance()
    {
        return instance;
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
