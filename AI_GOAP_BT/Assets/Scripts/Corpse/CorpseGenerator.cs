using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using System;


#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CorpseGenerator))]
public class CorpseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CorpseGenerator generator = (CorpseGenerator)target;
        if (GUILayout.Button("Get Bones"))
        {
            Undo.RecordObject(generator, "Get Bones");
            generator.GetBones();
            EditorUtility.SetDirty(generator.gameObject);
        }

        base.OnInspectorGUI();
    }
}
#endif

public class CorpseGenerator : NetworkBehaviour
{
    Rigidbody rb;

    [Header("본")]
    [SerializeField] private Transform root;
    [SerializeField] private List<Transform> bones = new List<Transform>();

    private Corpse corpse;
    public Corpse Corpse => corpse;

    [SyncVar] public string LatestHittedPart;
    public Vector3 ShotOrigin { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

#if UNITY_EDITOR
    public void GetBones()
    {
        if (root == null)
        {
            Debug.LogError("루트(Pelvis/Hip) 세팅이 필요합니다!!!");
            return;
        }

        bones.Clear();
        bones = root.gameObject.GetComponentsInChildren<Transform>().Where(x => !x.CompareTag("Gun")).ToList();
    }
#endif

    public void SpawnCorpse()
    {
        corpse.gameObject.SetActive(true);
        corpse.transform.parent = null;
        corpse.transform.position = this.transform.position;
        corpse.transform.rotation = this.transform.rotation;
        if (gameObject == GameManager.GetInstance().MyPlayer) Debug.Log($"{rb.linearVelocity}");
        corpse.PasteBoneTransforms(bones, LatestHittedPart, ShotOrigin, rb.linearVelocity);
    }

    public void DespawnCorpse()
    {
        corpse.transform.parent = transform;
        corpse.transform.localPosition = Vector3.zero;
        corpse.transform.localRotation = Quaternion.identity;
        corpse.gameObject.SetActive(false);
    }

    public void SetCorpseObject(GameObject corpseObj)
    {
        corpse = corpseObj.GetComponent<Corpse>();
    }
}
