using System.Collections.Generic;
using UnityEngine;
using System;

public class ScoreboardHUD : MonoBehaviour
{
    public static ScoreboardHUD Instance { get; private set; }

    [Header("Contents")]
    [SerializeField] GameObject scoreItemPrefab;

    [Header("Rects")]
    public RectTransform BlueContentRect;
    public RectTransform RedContentRect;

    private Dictionary<uint, ScoreboardItem> itemsByNetId = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddUser(string nickname, uint netId, bool isBlue)
    {
        if (itemsByNetId.ContainsKey(netId)) return;

        RectTransform content = isBlue ? BlueContentRect : RedContentRect;
        GameObject go = Instantiate(scoreItemPrefab, content);
        ScoreboardItem item = go.GetComponent<ScoreboardItem>();

        item.Init(netId, isBlue);
        item.SetNickname(nickname);
        item.SetKills(0);
        item.SetDeaths(0);
        item.SetAssists(0);

        itemsByNetId.Add(netId, item);
        SortTeam(content);
    }

    public void RemoveUser(uint netId)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;

        RectTransform content = null;

        if (item != null)
        {
            content = item.transform.parent as RectTransform;
            Destroy(item.gameObject);
        }

        itemsByNetId.Remove(netId);
        if (content != null) SortTeam(content);
    }

    public void UpdateKDA(uint netId, int kills, int assists, int deaths)
    {
        if (!itemsByNetId.TryGetValue(netId, out var item))
            return;

        item.SetKills(kills);
        item.SetAssists(assists);
        item.SetDeaths(deaths);

        SortTeam(item.transform.parent as RectTransform);
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
    }

    private void SortTeam(RectTransform content)
    {
        List<ScoreboardItem> list = new();

        foreach (Transform child in content)
        {
            if (child.TryGetComponent(out ScoreboardItem item))
                list.Add(item);
        }

        list.Sort((a, b) =>
        {
            int scoreCompare = b.Score.CompareTo(a.Score);
            if (scoreCompare != 0)
                return scoreCompare;

            int deathCompare = a.DeathsValue.CompareTo(b.DeathsValue);
            if (deathCompare != 0)
                return deathCompare;

            return string.Compare(a.NicknameValue, b.NicknameValue, StringComparison.Ordinal);
        });

        for (int i = 0; i < list.Count; i++)
        {
            list[i].transform.SetSiblingIndex(i);
            list[i].SetNumber(i + 1);
        }
    }
}
