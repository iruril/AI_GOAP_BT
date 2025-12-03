using BehaviorDesigner.Runtime.Tasks;
using GOAP.Assualt;
using UnityEngine;

[TaskCategory("Shooter AI/Engage")]
public class Cover : Action
{
    AssaultBrain brain;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        brain.EQS.LoadContext("Cover");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {

    }

    public override TaskStatus OnUpdate()
    {
        brain.AttackController.TryAttack();

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

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        brain.EQS.LoadContext("Engage");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {

    }

    public override TaskStatus OnUpdate()
    {
        brain.AttackController.TryAttack();

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

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override void OnStart()
    {
        brain.EQS.LoadContext("Engage");
        brain.EQS.TickEQS();
        brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
    }

    public override void OnEnd()
    {

    }

    public override TaskStatus OnUpdate()
    {
        brain.AttackController.TryAttack();

        return TaskStatus.Running;
    }
}
