using MEC;
using System.Collections.Generic;
using UnityEngine;

public class KillLogUI : MonoBehaviour
{
    public static KillLogUI Instance;

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

        if (Instance == this)
            Instance = null;
    }

    public void AddLog(string killer, string victim, bool isKillerBlue, bool isVictimBlue, bool isHeadshot)
    {
        KillLogContent content = GetAvailableContent();

        string logText = BuildKillLogText(killer, victim, isKillerBlue, isVictimBlue, isHeadshot);

        content.SetContent(logText);

        content.gameObject.SetActive(true);
        content.transform.SetAsLastSibling();

        activeLogs.Add(content);
        timers[content] = Timing.RunCoroutine(AutoDisable(content));
    }

    private string BuildKillLogText(string killer, string victim, bool isKillerBlue, bool isVictimBlue, bool isHeadshot)
    {
        Color killerColor = isKillerBlue ? WorldManager.Instance.BlueTeamColor : WorldManager.Instance.RedTeamColor;
        Color victimColor = isVictimBlue ? WorldManager.Instance.BlueTeamColor : WorldManager.Instance.RedTeamColor;

        string killerText = Colorize(killer, killerColor);
        string victimText = Colorize(victim, victimColor);

        string headshotText = isHeadshot
            ? $" {Colorize("[Headshot]", Color.red)}"
            : string.Empty;

        return $"{killerText} Killed{headshotText} {victimText}";
    }

    private string Colorize(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
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