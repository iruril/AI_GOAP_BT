using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Player.Input;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;

    private GameObject myPlayer;

    public GameObject MyPlayer
    {
        get => myPlayer;
        set
        {
            myPlayer = value;
            if (value != null &&
                value.TryGetComponent(out NetworkIdentity identity))
            {
                MyNetId = identity.netId;
            }
        }
    }
    public uint MyNetId { get; private set; }
    public InputRecorder InputMap { get; private set; }
    public PlayerSettingManager Settings { get; private set; }
    public RoomManager RM { get; private set; }

    private byte[] _connectionToken;

    public Dictionary<string, (Gun gun, GameObject prefab)> GunTable = new();

    public bool GunListReady = false;

    private const string _gunDataPath = "GunDatas/Guns.json";

    public bool IsGameplayScene { get; private set; }

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
        Settings = GetComponent<PlayerSettingManager>();
        BetterStreamingAssets.Initialize();
        WeaponDataLoad();

        OnSceneChanged(SceneManager.GetActiveScene().name);
        SceneManager.activeSceneChanged += (oldScene, newScene) => OnSceneChanged(newScene.name);
    }

    private void Start()
    {
        RM = NetworkManager.singleton as RoomManager;
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

    public void OnSceneChanged(string sceneName)
    {
        InputMap.ExitChat();
        IsGameplayScene = sceneName.Contains("Gameplay");
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
