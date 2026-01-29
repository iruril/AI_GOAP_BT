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
    [SerializeField] private float totalMass = 40f;
    [SerializeField] private Transform root;
    public Transform Hip => root;
    [SerializeField] private List<Transform> bones = new List<Transform>();

    [SerializedDictionary("Bone Name", "RigidBody")]
    public SerializedDictionary<string, Rigidbody> PhysicsBones = new();

    private bool _isOnBulletTime = false;
    private CoroutineHandle onBulletTimeHandle;

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
            float massRate = 0.05f; // 기본값 (기타 부위 5%)
            string name = rb.name.ToLower();

            if (name.Contains("pelvis") || name.Contains("hips")) massRate = 0.25f;
            else if (name.Contains("spine") || name.Contains("chest")) massRate = 0.20f;
            else if (name.Contains("head")) massRate = 0.10f;
            else if (name.Contains("thigh") || name.Contains("upperleg")) massRate = 0.12f;
            else if (name.Contains("calf") || name.Contains("leg") || name.Contains("knee")) massRate = 0.08f;
            else if (name.Contains("arm") || name.Contains("hand")) massRate = 0.025f;

            rb.mass = totalMass * massRate;

            rb.maxDepenetrationVelocity = 3.5f;
            rb.maxAngularVelocity = 90f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate; 
            rb.solverIterations = 20;
            rb.solverVelocityIterations = 10;
            rb.ResetInertiaTensor();

            if (!PhysicsBones.ContainsKey(rb.name))
                PhysicsBones.Add(rb.name, rb);
        }

        foreach (CharacterJoint joint in tempJoints)
        {
            joint.enableProjection = true;
            joint.projectionDistance = 0.05f;
            joint.projectionAngle = 2.0f;
            joint.enablePreprocessing = false; 
            joint.enableCollision = false;
            joint.twistLimitSpring = new SoftJointLimitSpring { spring = 0f, damper = 0f };
        }
    }
#endif

    public void PasteBoneTransforms(List<Transform> skeletons, string latestHittedPart, Vector3 shotOrigin, Vector3 velocity)
    {
        root.gameObject.SetActive(false);
        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].localPosition = skeletons[i].transform.localPosition;
            bones[i].localRotation = skeletons[i].transform.localRotation;
        }

        root.gameObject.SetActive(true);

        foreach (var item in PhysicsBones)
        {
            item.Value.detectCollisions = true;
            item.Value.useGravity = true;
            item.Value.isKinematic = false;
        }

        Vector3 forceDir = (this.transform.position - shotOrigin).normalized;
        foreach (var item in PhysicsBones)
        {
            item.Value.linearVelocity = velocity; 
            item.Value.angularVelocity = Vector3.zero;
        }
        PhysicsBones[latestHittedPart].AddForce(forceDir * 10f, ForceMode.Impulse);
    }

    void Update()
    {
        RigidCompensation();
    }

    private void RigidCompensation()
    {
        switch (Time.timeScale)
        {
            case < 1.0f when !onBulletTimeHandle.IsValid:
                onBulletTimeHandle = Timing.RunCoroutine(compensateRigidOnBulletTime());
                break;
            case >= 1.0f when onBulletTimeHandle.IsValid:
                Timing.KillCoroutines(onBulletTimeHandle);
                break;
        }
    }

    private IEnumerator<float> compensateRigidOnBulletTime()
    {
        while (_isOnBulletTime)
        {
            foreach (var rigid in PhysicsBones)
            {
                rigid.Value.linearVelocity = rigid.Value.linearVelocity * Time.timeScale;
                rigid.Value.linearVelocity += Physics.gravity * (1 - Time.timeScale) * Time.deltaTime;

                if (rigid.Value.linearVelocity.sqrMagnitude < 0.001f)
                {
                    rigid.Value.linearVelocity = Vector3.zero;
                }
            }
            yield return Timing.DeltaTime;
        }
    }
}
