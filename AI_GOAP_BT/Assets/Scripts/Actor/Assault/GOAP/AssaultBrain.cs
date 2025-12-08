using BehaviorDesigner.Runtime;
using System.Linq;
using UnityEngine;

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

        protected override void Start()
        {
            Sensor.MyStat.OnDead += InitGOAP;
            Sensor.MyStat.OnDead += CorpseSpawner.SpawnCorpse;
            Sensor.MyStat.OnRevive += CorpseSpawner.DespawnCorpse;
            Sensor.MyStat.OnUnderAttack += OnUnderAttackDuringCover;
        }

        private void OnDestroy()
        {
            Sensor.MyStat.OnDead -= InitGOAP;
            Sensor.MyStat.OnDead -= CorpseSpawner.SpawnCorpse;
            Sensor.MyStat.OnRevive -= CorpseSpawner.DespawnCorpse;
            Sensor.MyStat.OnUnderAttack -= OnUnderAttackDuringCover;
        }

        protected override void FixedUpdate()
        {
            if (Sensor.MyStat.IsDead) return;
            base.FixedUpdate();
        }

        protected override void InitGOAP()
        {
            BT.enabled = false;

            if (Goals.TryGetValue(DefaultGoalType, out var goal)) CurrentGoal = goal;
            else CurrentGoal = Goals.First().Value;

            if (Actions.TryGetValue(DefaultActionType, out var action)) CurrentAction = action;
            else CurrentAction = Actions.First().Value;

            currentGoalType = CurrentGoal.Type;
            currentActionType = CurrentAction.Type;
        }

        protected override void RegisterActions()
        {
            Actions.Add(AssualtAction.IDLE, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.IDLE,
                Cost = 50,

                Preconditions =
                {
                    () => true // 기본 Idle은 항상 실행 가능
                },

                OnStart = () => { },
                OnPhysicsUpdate = () => { },
                OnExit = () => { },

                IsUsefulForGoal = goal => true, // 어떤 Goal에도 기본 Idle은 유효
                IsFinished = false
            });

            Actions.Add(AssualtAction.MOVE_TO_CAPTURE, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.MOVE_TO_CAPTURE,
                Cost = 20,

                Preconditions =
                {
                    () => WorldManager.Instance.IsThereUncapturedPoint(transform)
                },

                OnStart = () =>
                {
                    Sensor.GetClosestCapture(out var destination);
                    Navigator.SetDestination(destination);
                },
                OnPhysicsUpdate = () =>
                {
                    if (Sensor.IsCurrentCapCapturerd())
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () =>
                {
                    Sensor.ResetCapture();
                },

                IsUsefulForGoal = goal => goal == AssaultGoal.CAPTURE,
                IsFinished = false
            });

            Actions.Add(AssualtAction.COMBAT, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.COMBAT,
                Cost = 20,

                Preconditions =
                {
                    () => Sensor.HasTarget
                },

                OnStart = () =>
                {
                    BT.enabled = true;
                },
                OnPhysicsUpdate = () =>
                {
                    if (!Sensor.HasTarget)
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () =>
                {
                    BT.enabled = false;
                },

                IsUsefulForGoal = goal =>
                {
                    return
                    goal == AssaultGoal.ENGAGE_ENEMY;
                },
                IsFinished = false
            });

            Actions.Add(AssualtAction.RELOAD, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.RELOAD,
                Cost = 10,

                Preconditions =
                {
                    () => GunController.CurrentRounds == 0
                },

                OnStart = () =>
                {
                    if (Sensor.LastSeenPosition != Vector3.negativeInfinity)
                    {
                        EQS.LoadContext("Cover");
                        EQS.TickEQS();
                        Navigator.SetDestination(EQS.BestItem.GetWorldPosition());
                    }
                    GunController.Reload(MotionController.Anim, MotionController.FBBIK.solver.leftHandEffector);
                },
                OnPhysicsUpdate = () =>
                {
                    if (GunController.CurrentRounds > 0)
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () =>
                {
                    if (Sensor.LastSeenPosition != Vector3.negativeInfinity)
                    {
                        EQS.LoadContext("Peek");
                        EQS.TickEQS();
                        Navigator.SetDestination(EQS.BestItem.GetWorldPosition());
                    }
                },

                IsUsefulForGoal = goal =>
                {
                    return true; //어느때나 탄약이 부족하면 즉시 재장전
                },
                IsFinished = false
            });

            Actions.Add(AssualtAction.COVER, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.COVER,
                Cost = 5,

                Preconditions =
                {
                    () => Sensor.MyStat.CurrentHP <= Sensor.MyStat.MaxHP * 0.25f
                },

                OnStart = () =>
                {
                    if (Sensor.LastSeenPosition != Vector3.negativeInfinity)
                    {
                        EQS.LoadContext("Cover");
                        EQS.TickEQS();
                        Navigator.SetDestination(EQS.BestItem.GetWorldPosition());
                    }
                },
                OnPhysicsUpdate = () =>
                {
                    AttackController.TryAttack();
                    if (Sensor.MyStat.CurrentHP >= Sensor.MyStat.MaxHP * 0.75f)
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () =>
                {
                    if (Sensor.LastSeenPosition != Vector3.negativeInfinity)
                    {
                        EQS.LoadContext("Peek");
                        EQS.TickEQS();
                        Navigator.SetDestination(EQS.BestItem.GetWorldPosition());
                    }
                },

                IsUsefulForGoal = goal =>
                {
                    return true; //어느때나 체력이 부족하면 즉시 엄폐한다.
                },
                IsFinished = false
            });

            DefaultActionType = AssualtAction.IDLE;
        }

        private void OnUnderAttackDuringCover(Vector3 shotOrigin)
        {
            if (CurrentAction.Type != AssualtAction.COVER)
                return;

            GunController.AimIKTarget.position = shotOrigin;

            EQS.LoadContext("Cover");
            EQS.TickEQS();

            Navigator.SetDestination(EQS.BestItem.GetWorldPosition());
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
