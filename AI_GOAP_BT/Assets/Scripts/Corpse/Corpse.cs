using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MEC;
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
    [SerializeField] private float totalMass = 40f;
    [SerializeField] private Transform root;
    public Transform Hip => root;
    [SerializeField] private List<Transform> bones = new List<Transform>();

    [Header("Physics Data")]
    [SerializedDictionary("Bone Name", "RigidBody")]
    public SerializedDictionary<string, Rigidbody> PhysicsBones = new();

    private struct JointData
    {
        public CharacterJoint joint;
        public Rigidbody connectedBody;
        public Rigidbody rb;
        public Transform transform;
    }

    private List<JointData> _jointDataList = new();

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
            rb.solverIterations = 25;
            rb.solverVelocityIterations = 15;

            if (!PhysicsBones.ContainsKey(rb.name))
                PhysicsBones.Add(rb.name, rb);
        }

        SoftJointLimitSpring swinglimit = new SoftJointLimitSpring { spring = 1000, damper = 100 };
        SoftJointLimitSpring twistlimit = new SoftJointLimitSpring { spring = 1000, damper = 100 };

        foreach (CharacterJoint joint in tempJoints)
        {
            joint.enableProjection = true;
            joint.projectionDistance = 0.01f;
            joint.projectionAngle = 2.0f;
            joint.enablePreprocessing = false; 
            joint.enableCollision = false; 
            
            joint.swingLimitSpring = swinglimit;
            joint.twistLimitSpring = twistlimit;
        }

        IgnoreInternalCollisions();
        CacheJointData();
    }

    private void IgnoreInternalCollisions()
    {
        var colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            for (int j = i + 1; j < colliders.Length; j++)
                Physics.IgnoreCollision(colliders[i], colliders[j]);
    }

    private void CacheJointData()
    {
        _jointDataList.Clear();
        foreach (var pair in PhysicsBones)
        {
            Rigidbody rb = pair.Value;
            CharacterJoint joint = rb.GetComponent<CharacterJoint>();
            if (joint != null && joint.connectedBody != null)
            {
                _jointDataList.Add(new JointData
                {
                    joint = joint,
                    connectedBody = joint.connectedBody,
                    rb = rb,
                    transform = rb.transform
                });
            }
        }
    }
#endif

    private void OnDisable()
    {
        foreach (var pair in PhysicsBones)
        {
            Rigidbody rb = pair.Value;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void PasteBoneTransforms(List<Transform> skeletons, string latestHittedPart, Vector3 shotOrigin, Vector3 velocity)
    {
        root.gameObject.SetActive(false);

        for (int i = 0; i < bones.Count; i++)
        {
            if (i >= skeletons.Count) break;
            bones[i].position = skeletons[i].position;
            bones[i].rotation = skeletons[i].rotation;
        }

        foreach (var data in _jointDataList)
        {
            data.joint.connectedAnchor = data.transform.InverseTransformPoint(data.connectedBody.position);
        }

        foreach (var pair in PhysicsBones)
        {
            Rigidbody rb = pair.Value;
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        root.gameObject.SetActive(true);

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
                hitRb.linearVelocity += addVelocity;
            }
        }
    }
}
