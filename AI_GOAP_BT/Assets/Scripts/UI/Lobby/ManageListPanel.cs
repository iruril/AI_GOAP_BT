using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManageListPanel : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] GameObject panelContentsPrefab;
    public RectTransform ContentRect;
    public Button CloseButton;

    private Dictionary<uint, ManageListItem> itemsByNetId = new();
    private List<uint> joinOrder = new();

    private void Start()
    {
        CloseButton.onClick.AddListener(DisablePanel);
        DisablePanel();
    }

    public void EnablePanel()
    {
        gameObject.SetActive(true);
    }

    public void DisablePanel()
    {
        gameObject.SetActive(false);
    }

    public void AddUser(string nickname, uint netId)
    {
        if (itemsByNetId.ContainsKey(netId)) return;

        GameObject go = Instantiate(panelContentsPrefab, ContentRect);
        ManageListItem item = go.GetComponent<ManageListItem>();

        item.SetNickname(nickname);
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            item.SetIdentity(identity);
            if (identity.GetComponent<RoomPlayer>().IsHost)
            {
                item.KickButton.interactable = false;
                item.KickButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Host";
            }
            item.KickButton.onClick.AddListener(() =>
            {
                NetworkClient.localPlayer.GetComponent<RoomPlayer>().CmdKick(identity.netId);
            });
        }

        itemsByNetId.Add(identity.netId, item);
        joinOrder.Add(identity.netId);

        RefreshOrder();
    }

    public void RemoveUser(uint netId)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;

        if (item != null) Destroy(item.gameObject);

        itemsByNetId.Remove(netId);
        joinOrder.Remove(netId);

        RefreshOrder();
    }

    public void ModifyNickname(uint netId, string nickname)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;
        item.SetNickname(nickname);
    }

    private void RefreshOrder()
    {
        for (int i = 0; i < joinOrder.Count; i++)
        {
            uint netId = joinOrder[i];
            ManageListItem item = itemsByNetId[netId];

            item.transform.SetSiblingIndex(i);
        }
    }

    public void ClearPanel()
    {
        foreach (var item in itemsByNetId.Values)
        {
            if (item != null) Destroy(item.gameObject);
        }
        itemsByNetId.Clear();
        joinOrder.Clear();
    }
}
