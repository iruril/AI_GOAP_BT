using UnityEngine;
using System.Collections.Generic;
using MEC;

public class InviteMessageHandler : MonoBehaviour
{
    public static InviteMessageHandler Instance;

    [SerializeField] private float popUpDuration = 5f;

    [Header("Contents")]
    [SerializeField] InviteMessageItem[] inviteMessages;

    private readonly HashSet<ulong> activeInviteIDs = new();
    private readonly List<InviteMessageItem> activeInvites = new();
    private readonly Dictionary<InviteMessageItem, CoroutineHandle> timers = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnInviteRecieced += OnInviteRecived;

        Clear();
    }

    private void OnDestroy()
    {
        if (SteamLobby.Instance != null)
            SteamLobby.Instance.OnInviteRecieced -= OnInviteRecived;
        Instance = null;
    }

    public void Clear()
    {
        foreach (var item in inviteMessages)
        {
            item.gameObject.SetActive(false);
        }
        activeInvites.Clear();
        activeInviteIDs.Clear();
        foreach (var timer in timers.Values)
        {
            Timing.KillCoroutines(timer);
        }
        timers.Clear();
    }

    private void OnInviteRecived(ulong lobbyId, ulong userId)
    {
        if (GameManager.GetInstance().IsGameplayScene) return;
        if (activeInviteIDs.Contains(userId)) return;

        InviteMessageItem item = GetAvailableItem();
        item.OnInviteRecived(lobbyId, userId);

        item.gameObject.SetActive(true);
        item.transform.SetAsLastSibling();

        activeInvites.Add(item);
        activeInviteIDs.Add(userId);
        timers[item] = Timing.RunCoroutine(AutoDisable(item));
    }

    private InviteMessageItem GetAvailableItem()
    {
        foreach (var item in inviteMessages)
        {
            if (!item.gameObject.activeSelf)
            {
                return item;
            }
        }

        InviteMessageItem oldestItem = activeInvites[0];
        activeInvites.RemoveAt(0);
        activeInviteIDs.Remove(oldestItem.InviterId);

        if (timers.ContainsKey(oldestItem))
        {
            Timing.KillCoroutines(timers[oldestItem]);
            timers.Remove(oldestItem);
        }

        oldestItem.gameObject.SetActive(false);

        return oldestItem;
    }

    public void DisableItem(InviteMessageItem item)
    {
        if (item.gameObject.activeSelf)
        {
            item.gameObject.SetActive(false);
            activeInvites.Remove(item);
            activeInviteIDs.Remove(item.InviterId);
            if (timers.ContainsKey(item))
            {
                Timing.KillCoroutines(timers[item]);
                timers.Remove(item);
            }
        }
    }

    private IEnumerator<float> AutoDisable(InviteMessageItem item)
    {
        yield return Timing.WaitForSeconds(popUpDuration);
        item.gameObject.SetActive(false);
        activeInvites.Remove(item);
        activeInviteIDs.Remove(item.InviterId);
        timers.Remove(item);
    }
}
