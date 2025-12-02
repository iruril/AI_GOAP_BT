using BehaviorDesigner.Runtime.Tasks;
using GOAP.Assualt;

[TaskCategory("Shooter AI/Engage")]
public class HasSufficientAmmo : Conditional
{
    AssaultBrain brain;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override TaskStatus OnUpdate()
    {
        if (brain.GunController.CurrentRounds > brain.GunController.CurrentGun.GunInfo.MagazineCapacity * 0.2f)
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
public class TargetVisible : Conditional
{
    AssaultBrain brain;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override TaskStatus OnUpdate()
    {
        if (brain.Sensor.TargetVisible)
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
public class HasTarget : Conditional
{
    AssaultBrain brain;

    public override void OnAwake()
    {
        brain = GetComponent<AssaultBrain>();
    }

    public override TaskStatus OnUpdate()
    {
        if (brain.Sensor.HasTarget)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
