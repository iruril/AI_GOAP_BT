using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBrowser : MonoBehaviour
{
    public static LobbyBrowser Instance;

    [Header("Contents")]
    [SerializeField] GameObject lobbyBrowserItemPrefab;

    [Header("Rect")]
    public RectTransform ContentRect;

    [Header("Button")]
    public Button RefreshButton;
    public Button QuitButton;

    private HashSet<ulong> lobbyIDs = new HashSet<ulong>();
    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnJoiningStateChanged -= OnJoiningChanged;

        Instance = null;
    }

    private void Start()
    {
        gameObject.SetActive(false);
        RefreshButton.onClick.AddListener(Refresh);
        QuitButton.onClick.AddListener(CloseBrowser);

        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnJoiningStateChanged += OnJoiningChanged;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void ClearLobbies()
    {
        foreach (Transform child in ContentRect)
        {
            Destroy(child.gameObject);
        }
        lobbyIDs.Clear();
    }

    public void AddLobby(ulong lobbyID)
    {
        if (lobbyIDs.Contains(lobbyID))
            return;
        GameObject go = Instantiate(lobbyBrowserItemPrefab, ContentRect);
        LobbyBrowserItem item = go.GetComponent<LobbyBrowserItem>();
        item.SetLobbyInfo(lobbyID);
        lobbyIDs.Add(lobbyID);
    }

    public void Refresh()
    {
        ClearLobbies();

        if (SteamLobby.Instance != null)
            SteamLobby.Instance.RequestLobbyList();
    }

    public void OpenBrowser()
    {
        gameObject.SetActive(true);
    }

    public void CloseBrowser()
    {
        gameObject.SetActive(false);
    }

    void OnJoiningChanged(bool isJoining)
    {
        foreach (Transform child in ContentRect)
        {
            LobbyBrowserItem item = child.GetComponent<LobbyBrowserItem>();
            if (item != null)
                item.SetInteractable(!isJoining);
        }
    }
}
