using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RootMotion.FinalIK;
using Mirror;

#if UNITY_EDITOR

[CustomEditor(typeof(HitBoxSetter))]
public class HitBoxSetterEditor : Editor
{
    const string INFO = "HitBox데이터 세팅 전용 컴포넌트입니다.\n" +
        "세팅 후에는 이 컴포넌트를 지워주세요.\n" +
        "반드시 세팅 전에 HitBox를 심을 본에 콜라이더 세팅을 해주세요";
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(INFO, MessageType.Info);
        HitBoxSetter _myData = (HitBoxSetter)target;

        if (GUILayout.Button("HitBox 세팅하기"))
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
            var hitReactIK = GetComponent<HitReaction>();

            HitBox.HitBoxType hitBoxType;

            if (item.gameObject.name.ToLower().Contains("head"))
            {
                hitBoxType = HitBox.HitBoxType.Head;
            }
            else if (item.gameObject.name.ToLower().Contains("arm") ||
                     item.gameObject.name.ToLower().Contains("leg") ||
                     item.gameObject.name.ToLower().Contains("hand") ||
                     item.gameObject.name.ToLower().Contains("foot") ||
                     item.gameObject.name.ToLower().Contains("thigh") ||
                     item.gameObject.name.ToLower().Contains("calf"))
            {
                hitBoxType = HitBox.HitBoxType.Limb;
            }
            else
            {
                hitBoxType = HitBox.HitBoxType.Body;
            }

            hitBox.InitHitBox(transform, corpseGenerator, hitReactIK, gameObject.layer, hitBoxType);
            item.gameObject.layer = LayerMask.NameToLayer("HitBox");
            cols.Add(item);
        }
    }
#endif
}
