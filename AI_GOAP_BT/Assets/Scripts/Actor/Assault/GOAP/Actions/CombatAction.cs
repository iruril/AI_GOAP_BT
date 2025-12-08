namespace GOAP.Assualt
{
    public class CombatAction : GoapAction<AssualtAction, AssaultGoal>
    {
        AssaultBrain brain;

        public CombatAction(AssaultBrain brain, AssualtAction action, int cost)
        {
            this.brain = brain;
            Type = action;
            Cost = cost;
        }

        public override bool CheckPreconditions()
        {
            return brain.Sensor.HasTarget;
        }

        public override bool IsUsefulForGoal(AssaultGoal goal)
        {
            return goal == AssaultGoal.ENGAGE_ENEMY;
        }

        public override void OnStart()
        {
            brain.BT.enabled = true;
        }

        public override void OnPhysicsUpdate()
        {
            if (!brain.Sensor.HasTarget)
                Complete();
        }

        public override void OnExit()
        {
            brain.BT.enabled = false;
        }
    }
}
