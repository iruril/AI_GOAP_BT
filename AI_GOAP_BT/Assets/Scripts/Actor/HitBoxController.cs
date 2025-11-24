using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR

[CustomEditor(typeof(HitBoxController))]
public class HitBoxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HitBoxController _myData = (HitBoxController)target;

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

public class HitBoxController : MonoBehaviour
{
    public GameObject root;
    [SerializeField] private List<Rigidbody> rbs = new();

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
}
