using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AYellowpaper.SerializedCollections;

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
            float massRate = 0.05f;
            string name = rb.name.ToLower();

            if (name.Contains("pelvis") || name.Contains("hips")) massRate = 0.2f;
            else if (name.Contains("spine") || name.Contains("chest")) massRate = 0.2f;
            else if (name.Contains("head")) massRate = 0.1f;
            else if (name.Contains("thigh") || name.Contains("upperleg")) massRate = 0.11f;
            else if (name.Contains("calf") || name.Contains("leg") || name.Contains("knee")) massRate = 0.1f;
            else if (name.Contains("arm") || name.Contains("hand")) massRate = 0.065f;

            rb.mass = totalMass * massRate;

            rb.maxDepenetrationVelocity = 3.5f;
            rb.maxAngularVelocity = 15f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate; 
            rb.solverIterations = 15;
            rb.solverVelocityIterations = 8;

            if (!PhysicsBones.ContainsKey(rb.name))
                PhysicsBones.Add(rb.name, rb);
        }

        foreach (CharacterJoint joint in tempJoints)
        {
            joint.autoConfigureConnectedAnchor = true;
        }
    }
#endif

    public void PasteBoneTransforms(List<Transform> skeletons, string latestHittedPart, Vector3 shotOrigin, Vector3 velocity)
    {
        foreach (var pair in PhysicsBones)
            pair.Value.isKinematic = true;

        for (int i = 0; i < bones.Count; i++)
        {
            if (i >= skeletons.Count) break;

            if (PhysicsBones.TryGetValue(bones[i].name, out Rigidbody rb))
            {
                rb.position = skeletons[i].position;
                rb.rotation = skeletons[i].rotation;
            }

            bones[i].position = skeletons[i].position;
            bones[i].rotation = skeletons[i].rotation;
        }

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
            Vector3 addVelocity = forceDir * 10f;
            hitRb.linearVelocity += addVelocity;
        }
        else
        {
            if (PhysicsBones.TryGetValue(root.name, out Rigidbody rootRb))
            {
                Vector3 forceDir = (rootRb.worldCenterOfMass - shotOrigin).normalized;
                Vector3 addVelocity = forceDir * 10f;
                rootRb.linearVelocity += addVelocity;
            }
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
