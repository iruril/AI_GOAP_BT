using BehaviorDesigner.Runtime.Tasks;
using GOAP.Assualt;
using UnityEngine;

[TaskCategory("Shooter AI/Engage")]
public class TargetInvisible : Conditional
{
    AssaultBrain brain;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override TaskStatus OnUpdate()
    {
        if (!brain.Sensor.TargetVisible)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}

[TaskCategory("Shooter AI/Engage")]
public class IsFriendlyInLOF : Conditional
{
    AssaultBrain brain;
    RaycastHit[] buffer = new RaycastHit[2];
    int result;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate(); 
        CheckLOF();
    }

    public override TaskStatus OnUpdate()
    {
        if (brain.Sensor.HasTarget && result > 0)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }

    void CheckLOF()
    {
        Vector3 origin = brain.Sensor.MyEyes.position;
        Vector3 direction = brain.Sensor.LastSeenPosition - brain.Sensor.MyEyes.position;
        float distance = direction.magnitude;
        direction.Normalize();

        int mask = WorldManager.Instance.GetLevelLayers() | (1 << gameObject.layer);
        int hitCount = Physics.SphereCastNonAlloc(origin, 0.1f, direction, buffer, distance, mask);

        result = 0;
        for (int i = 0; i < hitCount; i++)
        {
            if (buffer[i].collider.gameObject != this.gameObject)
                result++;
        }
    }
}
