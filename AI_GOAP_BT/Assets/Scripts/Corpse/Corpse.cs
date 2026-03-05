using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AYellowpaper.SerializedCollections;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(Corpse))]
public class CorpseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Corpse corpse = (Corpse)target;
        if (GUILayout.Button("Get Bones"))
        {
            Undo.RecordObject(corpse, "Get Bones");
            corpse.GetBones();
            EditorUtility.SetDirty(corpse.gameObject);
        }

        base.OnInspectorGUI();
    }
}
#endif

public class Corpse : MonoBehaviour
{
    [Header("Settings")]

#if UNITY_EDITOR
    [SerializeField] private float totalMass = 40f;
#endif
    [SerializeField] private Transform root;
    public Transform Hip => root;
    [SerializeField] private List<Transform> bones = new List<Transform>();

    [Header("Physics Data")]
    [SerializedDictionary("Bone Name", "RigidBody")]
    public SerializedDictionary<string, Rigidbody> PhysicsBones = new();

    private Vector3[] initialLocalPositions;
    private Quaternion[] initialLocalRotations;

    private void Awake()
    {
        initialLocalPositions = new Vector3[bones.Count];
        initialLocalRotations = new Quaternion[bones.Count];

        for (int i = 0; i < bones.Count; i++)
        {
            initialLocalPositions[i] = bones[i].localPosition;
            initialLocalRotations[i] = bones[i].localRotation;
        }
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

        PhysicsBones.Clear();
        PhysicsBones.Clear();
        Rigidbody[] tempRigids = root.GetComponentsInChildren<Rigidbody>();
        CharacterJoint[] tempJoints = root.GetComponentsInChildren<CharacterJoint>();

        foreach (Rigidbody rb in tempRigids)
        {
            Collider col = rb.GetComponent<Collider>();

            if (col == null)
            {
                // 콜라이더가 없더라도 최소 1.0f는 유지하여 조인트 안정성 확보
                rb.mass = 1f;
            }
            else
            {
                // 부위별 비율 계산 후, 최소값 1.0f 보장
                float calculatedMass = totalMass * GetBoneMassRatio(rb.name);
                rb.mass = Mathf.Max(calculatedMass, 1.0f);
            }

            rb.maxDepenetrationVelocity = 2.0f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.3f;
            rb.maxAngularVelocity = 15f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.solverIterations = 12;
            rb.solverVelocityIterations = 8;

            if (!PhysicsBones.ContainsKey(rb.name))
                PhysicsBones.Add(rb.name, rb);
        }

        foreach (CharacterJoint joint in tempJoints)
        {
            joint.autoConfigureConnectedAnchor = false;

            if (joint.connectedBody != null)
            {
                Vector3 worldAnchorPos = joint.transform.TransformPoint(joint.anchor);
                joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(worldAnchorPos);
            }
        }
    }

    private float GetBoneMassRatio(string boneName)
    {
        string name = boneName.ToLower();

        // 몸통 및 골반 (약 48%)
        if (name.Contains("pelvis") || name.Contains("hip")) return 0.15f; // 골반 15%
        if (name.Contains("spine")) return 0.11f; // Spine 3개 각각 약 11% (총 33%)

        // 머리 및 목 (약 8%)
        if (name.Contains("head")) return 0.05f; // 머리 5%
        if (name.Contains("neck")) return 0.03f; // 목 3%

        // 다리 (약 33%)
        if (name.Contains("upleg") || name.Contains("thigh")) return 0.10f; // 허벅지 각각 10%
        if (name.Contains("leg") || name.Contains("calf")) return 0.047f;   // 종아리 각각 4.7%

        // 팔 (약 11%)
        if (name.Contains("forearm")) return 0.03f;  // 하박(팔뚝) 각각 3%
        if (name.Contains("upperarm") || name.Contains("arm")) return 0.026f; // 상박 각각 2.6%

        // 기본값 (분류되지 않은 뼈대)
        return 0.01f;
    }
#endif

    public void ActivateWithPhysics(List<Transform> skeletons, string latestHittedPart, Vector3 shotOrigin, Vector3 velocity)
    {
        MatchBones(skeletons);
        ApplyPhysics(latestHittedPart, shotOrigin, velocity);
    }

    private void MatchBones(List<Transform> skeletons)
    {
        foreach (var pair in PhysicsBones)
        {
            pair.Value.isKinematic = true;
        }

        for (int i = 0; i < bones.Count; i++)
        {
            if (i >= skeletons.Count) break;
            bones[i].position = skeletons[i].position;
            bones[i].rotation = skeletons[i].rotation;
        }
    }

    private void ApplyPhysics(string latestHittedPart, Vector3 shotOrigin, Vector3 velocity)
    {
        foreach (var pair in PhysicsBones)
        {
            Rigidbody rb = pair.Value;
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        if (PhysicsBones.TryGetValue(latestHittedPart, out Rigidbody hitRb))
        {
            Vector3 forceDir = (hitRb.worldCenterOfMass - shotOrigin).normalized;
            hitRb.linearVelocity += forceDir * 5f;
        }
        else if (PhysicsBones.TryGetValue(root.name, out Rigidbody rootRb))
        {
            Vector3 forceDir = (rootRb.worldCenterOfMass - shotOrigin).normalized;
            rootRb.linearVelocity += forceDir * 5f;
        }
    }

    public void ResetPhysics()
    {
        foreach (var pair in PhysicsBones)
        {
            var rb = pair.Value;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ResetToBindPose();
    }

    private void ResetToBindPose()
    {
        if (initialLocalPositions == null || initialLocalRotations == null) return;

        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].localPosition = initialLocalPositions[i];
            bones[i].localRotation = initialLocalRotations[i];
        }
    }
}
