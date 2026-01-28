using UnityEngine;
using System.Collections.Generic;

public class DamageStackUI : MonoBehaviour
{
    public static DamageStackUI Instance;

    [SerializeField] private DamageStackItem itemPrefab;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private float displayDuration = 2f;

    private Dictionary<uint, DamageStackItem> activeItems = new Dictionary<uint, DamageStackItem>();
    private Queue<DamageStackItem> pool = new Queue<DamageStackItem>();

    private void Awake() => Instance = this; 

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        PrewarmPool(3);
    }

    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DamageStackItem item = Instantiate(itemPrefab, contentRect);
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
    }

    public void PopDamageStack(uint targetId, float damage, bool isKilled)
    {
        if (activeItems.TryGetValue(targetId, out var existingItem))
        {
            existingItem.Refresh(damage, displayDuration, isKilled);
            existingItem.transform.SetAsLastSibling();
        }
        else
        {
            DamageStackItem newItem = GetItem();
            newItem.transform.SetParent(contentRect);
            newItem.transform.SetAsLastSibling();
            newItem.Init(targetId, damage, displayDuration, RemoveFromActive, isKilled);

            activeItems.Add(targetId, newItem);
        }
    }

    private DamageStackItem GetItem()
    {
        if (pool.Count > 0)
        {
            var item = pool.Dequeue();
            item.gameObject.SetActive(true);
            return item;
        }
        return Instantiate(itemPrefab, contentRect);
    }

    private void RemoveFromActive(uint targetId)
    {
        if (activeItems.TryGetValue(targetId, out var item))
        {
            activeItems.Remove(targetId);
            pool.Enqueue(item);
        }
    }
}
