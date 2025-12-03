using BehaviorDesigner.Runtime.Tasks;
using GOAP.Assualt;

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
