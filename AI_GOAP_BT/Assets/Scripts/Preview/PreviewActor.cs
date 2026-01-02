using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PreviewActor : MonoBehaviour
{
    public static PreviewActor Instance { get; private set; }

    private event Action OnUpdated;

    private Animator anim;
    private FullBodyBipedIK ik;
    [SerializeField] private SkinnedMeshRenderer characterMeshRenderer; 
    
    [Header("Preview Rotation")]
    [SerializeField] float rotateSensitivity = 0.2f;
    [SerializeField] float returnDelay = 3f; 
    [SerializeField] float returnDamping = 6f;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform gunPos;
    [SerializeField] Transform leftHandIKTarget;

    private Gun currentGun;
    private GameObject currentGunModel;

    private Dictionary<string, (Gun gun, GameObject instance)> gunHistory = new();
    private string currentMeshID = "";

    Quaternion originRotation;
    float lastDragTime;
    bool isDragging;

    private void Awake()
    {
        Instance = this;
        anim = GetComponent<Animator>();
        ik = GetComponent<FullBodyBipedIK>(); 
        originRotation = transform.rotation;
    }

    private void Start()
    {
        ik.solver.leftHandEffector.target = leftHandIKTarget;
        OnUpdated += () => 
        {
            transform.rotation = originRotation;
            anim.PlayInFixedTime("TakeGun", 0, 0); 
        };
        SwipePanel.Instance.OnSwipe += OnPreviewDrag;
        SwipePanel.Instance.OnSwipeEnd += OnPreviewDragEnd;
        UpdatePreview("Blue", "MPX");
    }

    private void OnDestroy()
    {
        SwipePanel.Instance.OnSwipe -= OnPreviewDrag;
        SwipePanel.Instance.OnSwipeEnd -= OnPreviewDragEnd;
    }

    void Update()
    {
        float weight = anim.GetFloat("LHandIK");
        ik.solver.leftHandEffector.positionWeight = weight;
    }

    private void LateUpdate()
    {
        if (isDragging)
            return;

        if (Time.time - lastDragTime < returnDelay)
            return;

        float t = 1f - Mathf.Exp(-returnDamping * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            originRotation,
            t
        ); 
        
        if (Quaternion.Angle(transform.rotation, originRotation) < 0.1f)
        {
            transform.rotation = originRotation;
        }
    }

    public void UpdatePreview(string characterMeshID = default, string gunName = default)
    {
        bool gunResult = false;
        bool meshResult = false;

        if (characterMeshID != default)
        {
            gunResult = LoadCharacterVisual(characterMeshID);
        }
        if (gunName != default)
        {
            meshResult = LoadGunVisual(gunName);
        }

        if (gunResult || meshResult) OnUpdated?.Invoke();
    }

    bool LoadGunVisual(string gunName)
    {
        if (gunName == currentGun?.GunName) return false;

        bool cached = gunHistory.ContainsKey(gunName);
        (Gun gun, GameObject instance) gunData;

        if (cached)
            gunData = gunHistory[gunName];
        else
            gunData = GameManager.GetInstance().GunTable[gunName];

        currentGun = gunData.gun;

        if (currentGunModel != null)
            currentGunModel.SetActive(false);

        if (!cached)
        {
            GameObject model = Instantiate(gunData.instance);
            gunHistory.Add(gunName, (gunData.gun, model));
            currentGunModel = model;
        }
        else
        {
            currentGunModel = gunHistory[gunName].instance;
        }

        currentGunModel.transform.SetParent(gunPos, false);
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;

        gunPos.localPosition = currentGun.GunPosition;

        leftHandIKTarget.localPosition = currentGun.LeftHandIKPosition;
        leftHandIKTarget.localEulerAngles = currentGun.LeftHandIKRotation;
        currentGunModel.SetActive(false);
        return true;
    }

    bool LoadCharacterVisual(string characterMeshID)
    {
        if (currentMeshID == characterMeshID) return false;
        CharacterMeshData data = Resources.Load<CharacterMeshData>($"ModelData/{characterMeshID}");
        if (data == null)
        {
            Debug.LogError($"CharacterMeshData with ID {characterMeshID} not found!");
            return false;
        }

        var anim = GetComponent<Animator>();
        bool wasEnabled = anim.enabled;

        anim.enabled = false;

        characterMeshRenderer.sharedMesh = data.CharacterMesh;
        characterMeshRenderer.materials = data.CharacterMaterials;

        anim.enabled = wasEnabled;
        currentMeshID = characterMeshID;
        return true;
    }

    public void WeaponOnHand()
    {
        currentGunModel.SetActive(true);
    }

    public void OnPreviewDrag(Vector2 delta)
    {
        float yRotate = delta.x * rotateSensitivity;

        transform.Rotate(Vector3.up, -yRotate, Space.World);

        isDragging = true;
        lastDragTime = Time.time;
    }

    public void OnPreviewDragEnd()
    {
        isDragging = false;
        lastDragTime = Time.time;
    }
}
