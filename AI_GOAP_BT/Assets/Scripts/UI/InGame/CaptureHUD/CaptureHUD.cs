using UnityEngine;

public class CaptureHUD : MonoBehaviour
{
    public static CaptureHUD Instance;

    [Header("HUD Contents")]
    [SerializeField] CaptureHUDItem[] hudContents;

    private void Awake()
    {
        Instance = this;

        foreach (var item in hudContents)
        {
            item.ResetContent();
            item.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        var wm = WorldManager.Instance;
        if (wm == null) return;

        var captures = wm.GetCaptures();
        if (captures == null) return;

        int count = Mathf.Min(captures.Length, hudContents.Length);

        for (int i = 0; i < count && i < hudContents.Length; i++)
        {
            captures[i].OnColorChanged -= hudContents[i].SetColor;
            captures[i].OnGaugeChanged -= hudContents[i].SetFillAmout;
        }
    }

    private void Init()
    {
        var captures = WorldManager.Instance.GetCaptures();

        int count = Mathf.Min(captures.Length, hudContents.Length);

        for (int i = 0; i < hudContents.Length; i++)
        {
            if (i < count)
            {
                var item = hudContents[i];
                var cap = captures[i];

                item.gameObject.SetActive(true);
                item.ResetContent();
                item.SetText(cap.CaptureName);
                item.SetColor(WorldManager.Instance.DefColor);
                item.SetFillAmout(0f);

                cap.OnColorChanged += item.SetColor;
                cap.OnGaugeChanged += item.SetFillAmout;
            }
            else
            {
                hudContents[i].gameObject.SetActive(false);
            }
        }

        System.Array.Sort(
            hudContents,
            (a, b) =>
            {
                if (!a.gameObject.activeSelf && !b.gameObject.activeSelf) return 0;
                if (!a.gameObject.activeSelf) return 1;
                if (!b.gameObject.activeSelf) return -1;

                return string.Compare(
                    a.CurrentText,
                    b.CurrentText,
                    System.StringComparison.Ordinal
                );
            }
        );

        // Transform 순서 반영
        for (int i = 0; i < hudContents.Length; i++)
        {
            hudContents[i].transform.SetSiblingIndex(i);
        }
    }
}
