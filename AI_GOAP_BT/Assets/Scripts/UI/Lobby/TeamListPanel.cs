using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamListPanel : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] GameObject panelContentsPrefab;
    public RectTransform ContentRect;
    public Button JoinButton;

    private Dictionary<uint, PlayerListItem> itemsByNetId = new();
    private List<uint> joinOrder = new();

    public void AddUser(string nickname, uint netId)
    {
        if (itemsByNetId.ContainsKey(netId)) return;

        GameObject go = Instantiate(panelContentsPrefab, ContentRect);
        PlayerListItem item = go.GetComponent<PlayerListItem>();

        item.Init(netId);
        item.SetNickname(nickname);
        item.SetReady(false);

        itemsByNetId.Add(netId, item);
        joinOrder.Add(netId);

        RefreshOrder();
    }

    public void RemoveUser(uint netId)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;

        if(item != null) Destroy(item.gameObject);

        itemsByNetId.Remove(netId);
        joinOrder.Remove(netId);

        RefreshOrder();
    }

    private void RefreshOrder()
    {
        for (int i = 0; i < joinOrder.Count; i++)
        {
            uint netId = joinOrder[i];
            PlayerListItem item = itemsByNetId[netId];

            item.SetNumber(i + 1);
            item.transform.SetSiblingIndex(i);
        }
    }

    public void SetReady(uint netId, bool ready)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;

        item.SetReady(ready);
    }

    public void ModifyNickname(uint netId, string nickname)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;
        item.SetNickname(nickname);
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
