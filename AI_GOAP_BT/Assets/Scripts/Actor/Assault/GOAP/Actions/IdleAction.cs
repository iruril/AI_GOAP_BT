namespace GOAP.Assualt
{
    public class IdleAction : GoapAction<AssualtAction, AssaultGoal>
    {
        AssaultBrain brain;

        public IdleAction(AssaultBrain brain, AssualtAction action, int cost)
        {
            this.brain = brain;
            Type = action;
            Cost = cost;
        }

        public override bool CheckPreconditions() => true;

        public override bool IsUsefulForGoal(AssaultGoal goal) => true;

        public override void OnStart() { }

        public override void OnPhysicsUpdate() { }

        public override void OnExit() { }
    }
}