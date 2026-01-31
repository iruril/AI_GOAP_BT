using UnityEngine;
using Steamworks;
using System;
using System.Collections.Generic;

public class SteamAvatarManager : MonoBehaviour
{
    public static SteamAvatarManager Instance { get; private set; }

    private Dictionary<ulong, Texture2D> textureCache = new Dictionary<ulong, Texture2D>();

    public event Action<ulong, Texture2D> OnTextureLoaded;
    protected Callback<AvatarImageLoaded_t> imageLoadedCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            imageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
        }
        else { Destroy(gameObject); }
    }

    private void OnDestroy()
    {
        imageLoadedCallback?.Dispose();
        imageLoadedCallback = null;
        textureCache.Clear();
    }

    /// <summary>
    /// 캐시된 텍스처를 반환하거나 스팀에 요청합니다.
    /// </summary>
    public Texture2D GetAvatarTexture(ulong steamID)
    {
        if (steamID == 0 || !SteamManager.Initialized) return null;
        if (textureCache.TryGetValue(steamID, out Texture2D cachedTex)) return cachedTex;

        return LoadTextureFromSteam(new CSteamID(steamID));
    }

    private Texture2D LoadTextureFromSteam(CSteamID steamID)
    {
        int iImage = SteamFriends.GetLargeFriendAvatar(steamID);
        if (iImage <= 0) return null;

        return CreateTextureFromSteam(steamID.m_SteamID, iImage);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (textureCache.ContainsKey(callback.m_steamID.m_SteamID)) return;

        Texture2D newTex = CreateTextureFromSteam(callback.m_steamID.m_SteamID, callback.m_iImage);
        if (newTex != null)
        {
            OnTextureLoaded?.Invoke(callback.m_steamID.m_SteamID, newTex);
        }
    }

    private Texture2D CreateTextureFromSteam(ulong steamID, int iImage)
    {
        uint width, height;
        if (!SteamUtils.GetImageSize(iImage, out width, out height)) return null;

        byte[] avatarRaw = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(iImage, avatarRaw, (int)(width * height * 4))) return null;

        // Texture 생성
        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        texture.LoadRawTextureData(avatarRaw);
        FlipTexture(texture);
        texture.Apply();

        textureCache[steamID] = texture;
        return texture;
    }

    private void FlipTexture(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int topIdx = y * width + x;
                int bottomIdx = (height - y - 1) * width + x;
                Color32 temp = pixels[topIdx];
                pixels[topIdx] = pixels[bottomIdx];
                pixels[bottomIdx] = temp;
            }
        }
        texture.SetPixels32(pixels);
    }
}