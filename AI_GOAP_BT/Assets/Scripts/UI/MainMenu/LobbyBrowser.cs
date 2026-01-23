using MEC;
using Steamworks;
using System;
using System.Collections.Generic;
using TMPro;
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

    [Header("Verification")]
    public TextMeshProUGUI PasswordHeader;
    public RectTransform PassworldInputPanel;
    public TMP_InputField PassworldInputField;
    public Button PasswordPanelClose;

    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private float passwordFailFadeTime = 0.5f;

    private CoroutineHandle passwordHeaderColorRoutine;
    private CoroutineHandle passwordShakeRoutine;
    private Vector2 passwordPanelOriginPos;

    private HashSet<ulong> lobbyIDs = new HashSet<ulong>();
    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Instance = this;
        passwordPanelOriginPos = PassworldInputPanel.anchoredPosition;
    }

    private void OnDestroy()
    {
        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnJoiningStateChanged -= OnJoiningChanged;

        Timing.KillCoroutines(passwordShakeRoutine);
        Timing.KillCoroutines(passwordHeaderColorRoutine);

        PassworldInputPanel.anchoredPosition = passwordPanelOriginPos;
        PasswordHeader.color = Color.white;

        Instance = null;
    }

    private void Start()
    {
        gameObject.SetActive(false);
        PassworldInputPanel.gameObject.SetActive(false);
        RefreshButton.onClick.AddListener(Refresh);
        QuitButton.onClick.AddListener(CloseBrowser);
        PasswordPanelClose.onClick.AddListener(
            () =>
            {
                if (!PassworldInputPanel.gameObject.activeSelf)
                    PassworldInputPanel.gameObject.SetActive(false);

                PassworldInputField.onEndEdit.RemoveAllListeners();
                Refresh();
            }
        );

        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnJoiningStateChanged += OnJoiningChanged;
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Timing.KillCoroutines(passwordShakeRoutine);
        Timing.KillCoroutines(passwordHeaderColorRoutine);

        PassworldInputPanel.anchoredPosition = passwordPanelOriginPos;
        PasswordHeader.color = Color.white;
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

    public void RequestValidatePassword(ulong lobbyID, Action<bool> onResult)
    {
        CSteamID lobbyCSteamID = new CSteamID(lobbyID);

        PassworldInputField.onEndEdit.RemoveAllListeners();
        PassworldInputField.text = string.Empty;

        PassworldInputField.onEndEdit.AddListener(
            (string value) =>
            {
                bool success = PasswordVerification(value, lobbyCSteamID);
                onResult?.Invoke(success);

                if (success)
                {
                    PassworldInputPanel.gameObject.SetActive(false);
                }
                else
                {
                    Timing.KillCoroutines(passwordShakeRoutine);
                    passwordShakeRoutine =
                        Timing.RunCoroutine(ShakePasswordPanel());

                    Timing.KillCoroutines(passwordHeaderColorRoutine);
                    passwordHeaderColorRoutine =
                        Timing.RunCoroutine(PasswordHeaderFailEffect());

                    PassworldInputField.text = string.Empty;
                    PassworldInputField.ActivateInputField();
                }
            }
        );

        PassworldInputPanel.gameObject.SetActive(true);
    }

    private bool PasswordVerification(string input, CSteamID lobbyCSteamID)
    {
        return input == SteamMatchmaking.GetLobbyData(lobbyCSteamID, "Password").ToString();
    }

    private IEnumerator<float> PasswordHeaderFailEffect()
    {
        Color startColor = Color.white;
        Color failColor = Color.red;

        float halfTime = passwordFailFadeTime * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            PasswordHeader.color = Color.Lerp(startColor, failColor, t);
            yield return Timing.WaitForOneFrame;
        }

        elapsed = 0f;

        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            PasswordHeader.color = Color.Lerp(failColor, startColor, t);
            yield return Timing.WaitForOneFrame;
        }

        PasswordHeader.color = startColor;
    }

    private IEnumerator<float> ShakePasswordPanel()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float normalized = elapsed / shakeDuration;
            float strength = Mathf.Sin(normalized * Mathf.PI * 4f) * shakeStrength * (1f - normalized);

            PassworldInputPanel.anchoredPosition = passwordPanelOriginPos + new Vector2(strength, 0f);
            yield return Timing.WaitForOneFrame;
        }

        PassworldInputPanel.anchoredPosition = passwordPanelOriginPos;
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
