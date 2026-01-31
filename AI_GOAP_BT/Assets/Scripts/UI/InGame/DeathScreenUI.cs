using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance;

    [Header("Killer Info")]
    public RawImage killerProfile;
    public TextMeshProUGUI killerNicknameText;
    public TextMeshProUGUI killerKDAText;

    Texture2D defaultAvatarTexture;

    [Header("My Stats")]
    public TextMeshProUGUI myKillsText;
    public TextMeshProUGUI myDeathsText;
    public TextMeshProUGUI myAssistsText;

    [Header("Damage Logs (Array)")]
    [SerializeField] private TextMeshProUGUI[] existingLogTexts;

    [SerializeField] private Color damageValueColor = Color.red;
    [SerializeField] private Color bodyPartColor = Color.white;
    [SerializeField] private Color gunNameColor = Color.cyan;

    private ulong currentKillerSteamID;

    private void Awake() => Instance = this;

    private void Start()
    {
        SteamAvatarManager.Instance.OnTextureLoaded += UpdateKillerAvatar;
        defaultAvatarTexture = killerProfile.texture as Texture2D;
        Close();
    }

    private void OnDestroy()
    {
        if (SteamAvatarManager.Instance != null)
            SteamAvatarManager.Instance.OnTextureLoaded -= UpdateKillerAvatar;

        if (Instance == this) 
            Instance = null;
    }

    public void Open(DamageRecord[] records, KDA myKDA, string killerName, KDA killerKDA, ulong killerSteamID)
    {
        this.gameObject.SetActive(true); 
        currentKillerSteamID = killerSteamID;

        if (records.Length > 0)
        {
            uint lastAttackerId = records[records.Length - 1].attackerNetId;
            if (NetworkClient.spawned.TryGetValue(lastAttackerId, out var id))
            {
                var stat = id.GetComponent<Stat>();
                if (stat != null)
                {
                    killerNicknameText.color = (stat.MyTeam == Team.Blue) ? WorldManager.Instance.BlueTeamColor : WorldManager.Instance.RedTeamColor;
                }
            }
        }

        killerNicknameText.text = killerName;
        killerKDAText.text = $"KDA : {killerKDA.Kills} / {killerKDA.Deaths} / {killerKDA.Assists}";

        myKillsText.text = $"Kills : {myKDA.Kills}";
        myDeathsText.text = $"Deaths : {myKDA.Deaths}";
        myAssistsText.text = $"Assists : {myKDA.Assists}";

        killerProfile.texture = defaultAvatarTexture;
        Texture2D tex = currentKillerSteamID != 0 ? SteamAvatarManager.Instance.GetAvatarTexture(killerSteamID) : null;
        if (tex != null) killerProfile.texture = tex;

        UpdateLogs(records);
    }

    private void ClearAllLogs()
    {
        for (int i = 0; i < existingLogTexts.Length; i++)
        {
            if (existingLogTexts[i] != null)
            {
                existingLogTexts[i].text = "";
                existingLogTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateLogs(DamageRecord[] records)
    {
        ClearAllLogs();

        int recordCount = records.Length;
        int maxUI = existingLogTexts.Length;

        string c_damage = ColorUtility.ToHtmlStringRGB(damageValueColor);
        string c_part = ColorUtility.ToHtmlStringRGB(bodyPartColor);
        string c_gun = ColorUtility.ToHtmlStringRGB(gunNameColor);

        for (int i = 0; i < recordCount; i++)
        {
            if (i >= maxUI) break;

            var record = records[recordCount - 1 - i];

            string attackerName = "Unknown"; 
            Color attackerColor = Color.white;
            if (NetworkClient.spawned.TryGetValue(record.attackerNetId, out var identity))
            {
                var stat = identity.GetComponent<Stat>(); if (stat != null)
                {
                    attackerName = stat.Nickname;
                    attackerColor = (stat.MyTeam == Team.Blue) ? WorldManager.Instance.BlueTeamColor : WorldManager.Instance.RedTeamColor;
                }
            }
            string c_attacker = ColorUtility.ToHtmlStringRGB(attackerColor);
            string logText = $"<color=#{c_attacker}>{attackerName}</color>, " +
                 $"<color=#{c_damage}>{record.damage:F0}</color> damage on " +
                 $"[<color=#{c_part}>{record.hitBoxType.ToString().ToUpper()}</color>], " +
                 $"<color=#{c_gun}>{record.gunName}</color>";

            existingLogTexts[i].text = logText;
            existingLogTexts[i].transform.parent.gameObject.SetActive(true);
        }
    }

    private void UpdateKillerAvatar(ulong loadedID, Texture2D loadedTex)
    {
        if (this.gameObject.activeSelf && loadedID == currentKillerSteamID)
        {
            killerProfile.texture = loadedTex;
        }
    }

    public void Close()
    {
        ClearAllLogs();
        this.gameObject.SetActive(false);
    }
}