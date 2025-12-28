using MEC;
using System.Collections.Generic;
using UnityEngine;

public class KillLogUI : MonoBehaviour
{
    public static KillLogUI Instance;

    [Header("Blue Color")]
    [SerializeField]
    private Color blue = Color.blue;
    [Header("Red Color")]
    [SerializeField]
    private Color red = Color.red;

    [Header("Log Contents")]
    [SerializeField] KillLogContent[] logContents;

    private readonly List<KillLogContent> activeLogs = new();
    private readonly Dictionary<KillLogContent, CoroutineHandle> timers = new();

    private const float LIFE_TIME = 5f;

    private void Awake()
    {
        Instance = this;
        foreach (var item in logContents)
        {
            item.ResetContent();
            item.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach(var item in timers)
        {
            Timing.KillCoroutines(item.Value);
        }
    }

    public void AddLog(string killer, string victim, bool isKillerBlue, bool isVictimBlue)
    {
        KillLogContent content = GetAvailableContent();

        content.SetKillerContent(killer, isKillerBlue ? blue : red);
        content.SetVictimContent(victim, isVictimBlue ? blue : red);

        content.gameObject.SetActive(true);

        content.transform.SetAsLastSibling();

        activeLogs.Add(content);
        timers[content] = Timing.RunCoroutine(AutoDisable(content));
    }

    private KillLogContent GetAvailableContent()
    {
        // 아직 안 쓰는 슬롯이 있다면
        if (activeLogs.Count < logContents.Length)
        {
            foreach (var c in logContents)
            {
                if (!activeLogs.Contains(c))
                    return c;
            }
        }

        // 전부 쓰고 있다면 가장 오래된 것 재사용
        KillLogContent oldest = activeLogs[0];
        activeLogs.RemoveAt(0);

        if (timers.TryGetValue(oldest, out var co))
        {
            Timing.KillCoroutines(co);
            timers.Remove(oldest);
        }

        oldest.ResetContent();
        oldest.gameObject.SetActive(false);

        return oldest;
    }

    private IEnumerator<float> AutoDisable(KillLogContent content)
    {
        yield return Timing.WaitForSeconds(LIFE_TIME);

        if (activeLogs.Contains(content))
            activeLogs.Remove(content);

        content.ResetContent();
        content.gameObject.SetActive(false);
        timers.Remove(content);
    }
}