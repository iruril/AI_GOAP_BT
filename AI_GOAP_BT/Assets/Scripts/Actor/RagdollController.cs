using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR

[CustomEditor(typeof(RagdollController))]
public class RagdollControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RagdollController _myData = (RagdollController)target;

        if (GUILayout.Button("Ragdoll ¼¼ÆÃ"))
        {
            _myData.SetRagdoll();
            EditorUtility.SetDirty(_myData);
            AssetDatabase.SaveAssets();
        }

        base.OnInspectorGUI();
    }
}
#endif

public class RagdollController : MonoBehaviour
{
    private GOAP.Assualt.AssaultBrain myBrain;

    public GameObject root;
    [SerializeField] private List<Rigidbody> rbs = new();

    void Awake()
    {
        myBrain = GetComponent<GOAP.Assualt.AssaultBrain>();
    }

    private void Start()
    {
        myBrain.Sensor.MyStat.OnDead += OnDead;
    }

    private void OnDestroy()
    {
        myBrain.Sensor.MyStat.OnDead -= OnDead;
    }

#if UNITY_EDITOR
    public void SetRagdoll()
    {
        rbs.Clear();
        Rigidbody[] tempRigids = root.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody item in tempRigids)
        {
            if (item.TryGetComponent<HitBox>(out var hitBox)) { }
            else hitBox = item.gameObject.AddComponent<HitBox>();
            hitBox.InitHitBox(transform, gameObject.layer);
            item.gameObject.layer = LayerMask.NameToLayer("HitBox");
            rbs.Add(item);
        }
    }
#endif

    public void OnDead()
    {
        foreach (var rb in rbs)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
