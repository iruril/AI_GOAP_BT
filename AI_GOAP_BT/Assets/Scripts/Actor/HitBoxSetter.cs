using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR

[CustomEditor(typeof(HitBoxSetter))]
public class HitBoxSetterEditor : Editor
{
    const string INFO = "HitBox데이터 세팅 전용 컴포넌트입니다.\n" +
        "세팅 후에는 이 컴포넌트를 지워주세요.";
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(INFO, MessageType.Info);
        HitBoxSetter _myData = (HitBoxSetter)target;

        if (GUILayout.Button("HitBox 가져오기"))
        {
            _myData.SetRagdoll();
            EditorUtility.SetDirty(_myData);
            AssetDatabase.SaveAssets();
        }

        base.OnInspectorGUI();
    }
}
#endif

public class HitBoxSetter : MonoBehaviour
{
    public GameObject root;
    [SerializeField] private List<Collider> cols = new();

#if UNITY_EDITOR
    public void SetRagdoll()
    {
        cols.Clear();
        Collider[] tempCols = root.GetComponentsInChildren<Collider>();
        foreach (Collider item in tempCols)
        {
            if (item.TryGetComponent<HitBox>(out var hitBox)) { }
            else hitBox = item.gameObject.AddComponent<HitBox>();

            var corpseGenerator = GetComponent<CorpseGenerator>();
            hitBox.InitHitBox(transform, corpseGenerator, gameObject.layer);
            item.gameObject.layer = LayerMask.NameToLayer("HitBox");
            cols.Add(item);
        }
    }
#endif
}
