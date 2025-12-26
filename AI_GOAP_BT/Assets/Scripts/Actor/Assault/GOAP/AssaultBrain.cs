using BehaviorDesigner.Runtime;

namespace GOAP.Assualt
{
    public enum AssualtAction
    {
        IDLE,
        MOVE_TO_CAPTURE,
        COMBAT,
        RELOAD,
        COVER
    }

    public enum AssaultGoal
    {
        SURVIVE,
        CAPTURE,
        ENGAGE_ENEMY
    }

    public class AssaultBrain : GoapBrain<AssualtAction, AssaultGoal>
    {
        public AINavigator Navigator { get; private set; }
        public Sensor.Assualt.AssaultSensor Sensor { get; private set; }
        public AnimControl.Assault.AssaultAnimFSM MotionController { get; private set; }
        public GunHandler GunController { get; private set; }
        public AttackHandler AttackController { get; private set; }
        public CorpseGenerator CorpseSpawner { get; private set; }
        public EnvQuery EQS { get; private set; }
        public BehaviorTree BT { get; private set; }

        protected override void Awake()
        {
            Navigator = GetComponent<AINavigator>();
            Sensor = GetComponent<Sensor.Assualt.AssaultSensor>();
            MotionController = GetComponent<AnimControl.Assault.AssaultAnimFSM>();
            GunController = GetComponent<GunHandler>();
            AttackController = GetComponent<AttackHandler>();
            CorpseSpawner = GetComponent<CorpseGenerator>();
            EQS = GetComponent<EnvQuery>();
            BT = GetComponent<BehaviorTree>();

            base.Awake();
        }

        public override void OnStartServer()
        {
            Sensor.MyStat.OnDead += InitGOAP;
            if (WorldManager.Instance.IsBlueTeam(this.gameObject.layer))
            {
                GunController.LoadGun("MPX");
            }
            else
            {
                GunController.LoadGun("AK-12");
            }
        }

        public override void OnStartClient()
        {
            Sensor.MyStat.OnDead += CorpseSpawner.SpawnCorpse;
            Sensor.MyStat.OnRevive += CorpseSpawner.DespawnCorpse;
        }

        public override void OnStopServer()
        {
            Sensor.MyStat.OnDead -= InitGOAP;
        }

        public override void OnStopClient()
        {
            Sensor.MyStat.OnDead -= CorpseSpawner.SpawnCorpse;
            Sensor.MyStat.OnRevive -= CorpseSpawner.DespawnCorpse;
        }

        protected override void FixedUpdate()
        {
            if (!isServer) return;

            if (Sensor.MyStat.IsDead) return;
            base.FixedUpdate();
        }

        protected override void InitGOAP()
        {
            base.InitGOAP();
            BT.enabled = false;
        }

        protected override void RegisterActions()
        {
            Actions.Add(AssualtAction.IDLE, new IdleAction(this, AssualtAction.IDLE, 50));
            Actions.Add(AssualtAction.MOVE_TO_CAPTURE, new MoveToCaptureAction(this, AssualtAction.MOVE_TO_CAPTURE, 20));
            Actions.Add(AssualtAction.COMBAT, new CombatAction(this, AssualtAction.COMBAT, 20));
            Actions.Add(AssualtAction.RELOAD, new ReloadAction(this, AssualtAction.RELOAD, 5));
            Actions.Add(AssualtAction.COVER, new CoverAction(this, AssualtAction.COVER, 10));

            DefaultActionType = AssualtAction.IDLE;
        }

        protected override void RegisterGoals()
        {
            Goals.Add(AssaultGoal.SURVIVE, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.SURVIVE,
                Priority = 100,
                IsSatisfied = () =>
                {
                    return true;
                },
                Repeatable = true
            });

            Goals.Add(AssaultGoal.CAPTURE, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.CAPTURE,
                Priority = 40,
                IsSatisfied = () =>
                {
                    return !WorldManager.Instance.IsThereUncapturedPoint(transform);
                },
                Repeatable = true
            });

            Goals.Add(AssaultGoal.ENGAGE_ENEMY, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.ENGAGE_ENEMY,
                Priority = 50,
                IsSatisfied = () =>
                {
                    return !Sensor.HasTarget;
                },
                Repeatable = true
            });

            DefaultGoalType = AssaultGoal.SURVIVE;
        }
    }
}
