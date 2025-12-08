namespace GOAP.Assualt
{
    public class MoveToCaptureAction : GoapAction<AssualtAction, AssaultGoal>
    {
        AssaultBrain brain;

        public MoveToCaptureAction(AssaultBrain brain, AssualtAction action, int cost)
        {
            this.brain = brain;
            Type = action;
            Cost = cost;
        }

        public override bool CheckPreconditions()
        {
            return WorldManager.Instance.IsThereUncapturedPoint(brain.transform);
        }

        public override bool IsUsefulForGoal(AssaultGoal goal)
        {
            return goal == AssaultGoal.CAPTURE;
        }

        public override void OnStart()
        {
            brain.Sensor.GetClosestCapture(out var destination);
            brain.Navigator.SetDestination(destination);
        }

        public override void OnPhysicsUpdate()
        {
            if (brain.Sensor.IsCurrentCapCapturerd())
                Complete();
        }

        public override void OnExit()
        {
            brain.Sensor.ResetCapture();
        }
    }
}
