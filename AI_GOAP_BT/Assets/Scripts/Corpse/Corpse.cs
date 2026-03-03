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
            rb.mass = totalMass / tempRigids.Count();

            rb.maxDepenetrationVelocity = 3.5f;
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
            hitRb.linearVelocity += forceDir * 10f;
        }
        else if (PhysicsBones.TryGetValue(root.name, out Rigidbody rootRb))
        {
            Vector3 forceDir = (rootRb.worldCenterOfMass - shotOrigin).normalized;
            rootRb.linearVelocity += forceDir * 10f;
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
