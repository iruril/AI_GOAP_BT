using BehaviorDesigner.Runtime.Tasks;
using GOAP.Assualt;
using UnityEngine;

[TaskCategory("Shooter AI/Engage")]
public class Cover : Action
{
    AssaultBrain brain;
    float timer = 0f;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        timer = 0f;
        brain.EQS.LoadContext("Cover");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {

    }

    public override TaskStatus OnUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= brain.GunController.CurrentGun.GunInfo.ShotInterval &&
            brain.MotionController.Shootable())
        {
            brain.GunController.Fire();
            timer = 0f;
        }

        if (Vector3.Distance(transform.position, brain.Navigator.AI.endOfPath) < 0.5f)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Running;
        }
    }
}

[TaskCategory("Shooter AI/Engage")]
public class Reposition : Action
{
    AssaultBrain brain;
    float timer = 0f;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        timer = 0f;
        brain.EQS.LoadContext("Engage");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {

    }

    public override TaskStatus OnUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= brain.GunController.CurrentGun.GunInfo.ShotInterval &&
            brain.MotionController.Shootable())
        {
            brain.GunController.Fire();
            timer = 0f;
        }

        if (Vector3.Distance(transform.position, brain.Navigator.AI.endOfPath) < 0.5f 
            || !brain.Sensor.HasTarget)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Running;
        }
    }
}

[TaskCategory("Shooter AI/Engage")]
public class Shoot : Action
{
    AssaultBrain brain;
    float timer = 0f;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        timer = 0f;
        brain.EQS.LoadContext("Engage");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {
        brain.CompleteCurrentAction();
    }

    public override TaskStatus OnUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= brain.GunController.CurrentGun.GunInfo.ShotInterval &&
            brain.MotionController.Shootable())
        {
            brain.GunController.Fire();
            timer = 0f;
        }

        return TaskStatus.Running;
    }
}
