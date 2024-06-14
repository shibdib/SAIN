using EFT;
using SAIN.BotController.Classes;
using SAIN.SAINComponent.Classes.Enemy;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINDecisionClass : SAINBase, ISAINClass
    {
        public SAINDecisionClass(BotComponent bot) : base(bot)
        {
            SelfActionDecisions = new SelfActionDecisionClass(bot);
            EnemyDecisions = new EnemyDecisionClass(bot);
            GoalTargetDecisions = new TargetDecisionClass(bot);
            SquadDecisions = new SquadDecisionClass(bot);
            DogFightDecision = new DogFightDecisionClass(bot);
        }

        public DogFightDecisionClass DogFightDecision { get; private set; }

        public event Action<SoloDecision, SquadDecision, SelfDecision, float> OnDecisionMade;

        public event Action<SoloDecision, SquadDecision, SelfDecision, float> OnSAINActivated;

        public event Action<float> OnSAINDeactivated;

        public bool HasDecision => CurrentSoloDecision != SoloDecision.None
                || CurrentSelfDecision != SelfDecision.None
                || CurrentSquadDecision != SquadDecision.None;

        public void Init()
        {
        }

        public void Update()
        {
            float delay = HasDecision ? getDecisionFreq : getDecisionFreqAtPeace;

            if (_nextGetDecisionTime + delay < Time.time)
            {
                _nextGetDecisionTime = Time.time;
                getDecision();
            }
        }

        private float _nextGetDecisionTime;
        private const float getDecisionFreq = 0.05f;
        private const float getDecisionFreqAtPeace = 0.25f;

        public void Dispose()
        {
        }

        public EnemyPathDistance EnemyDistance
        {
            get
            {
                var enemy = Bot.Enemy;
                if (enemy != null)
                {
                    return enemy.CheckPathDistance();
                }
                return EnemyPathDistance.NoEnemy;
            }
        }

        public SoloDecision CurrentSoloDecision { get; private set; }
        public SoloDecision OldSoloDecision { get; private set; }

        public SquadDecision CurrentSquadDecision { get; private set; }
        public SquadDecision OldSquadDecision { get; private set; }

        public SelfDecision CurrentSelfDecision { get; private set; }
        public SelfDecision OldSelfDecision { get; private set; }

        public SelfActionDecisionClass SelfActionDecisions { get; private set; }
        public EnemyDecisionClass EnemyDecisions { get; private set; }
        public TargetDecisionClass GoalTargetDecisions { get; private set; }
        public SquadDecisionClass SquadDecisions { get; private set; }
        public List<SoloDecision> RetreatDecisions { get; private set; } = new List<SoloDecision> { SoloDecision.Retreat };
        public float ChangeDecisionTime { get; private set; }
        public float TimeSinceChangeDecision => Time.time - ChangeDecisionTime;

        private void getDecision()
        {
            if (shallAvoidGrenade())
            {
                SetDecisions(SoloDecision.AvoidGrenade, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (DogFightDecision.ShallDogFight())
            {
                SetDecisions(SoloDecision.DogFight, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (CheckContinueRetreat())
            {
                return;
            }

            if (SelfActionDecisions.GetDecision(out SelfDecision selfDecision))
            {
                SetDecisions(SoloDecision.Retreat, SquadDecision.None, selfDecision);
                return;
            }

            if (SquadDecisions.GetDecision(out SquadDecision squadDecision))
            {
                SetDecisions(SoloDecision.None, squadDecision, SelfDecision.None);
                return;
            }

            if (EnemyDecisions.GetDecision(out SoloDecision soloDecision))
            {
                SetDecisions(soloDecision, SquadDecision.None, SelfDecision.None);
                return;
            }

            SetDecisions(SoloDecision.None, SquadDecision.None, SelfDecision.None);
        }

        private void SetDecisions(SoloDecision solo, SquadDecision squad, SelfDecision self)
        {
            if (SAINPlugin.ForceSoloDecision != SoloDecision.None)
            {
                solo = SAINPlugin.ForceSoloDecision;
            }
            if (SAINPlugin.ForceSquadDecision != SquadDecision.None)
            {
                squad = SAINPlugin.ForceSquadDecision;
            }
            if (SAINPlugin.ForceSelfDecision != SelfDecision.None)
            {
                self = SAINPlugin.ForceSelfDecision;
            }

            bool newDecision = checkForNewDecision(solo, squad, self);

            CurrentSoloDecision = solo;
            CurrentSquadDecision = squad;
            CurrentSelfDecision = self;

            if (newDecision)
            {
                ChangeDecisionTime = Time.time;
                OnDecisionMade?.Invoke(solo, squad, self, Time.time);
                checkSAINStart(); 
                checkSAINEnd();
            }
        }

        private void checkSAINStart()
        {
            // If previously all decisions were none, sain has now started.
            if (OldSoloDecision == SoloDecision.None
                && OldSelfDecision == SelfDecision.None
                && OldSquadDecision == SquadDecision.None)
            {
                OnSAINActivated?.Invoke(CurrentSoloDecision, CurrentSquadDecision, CurrentSelfDecision, Time.time);
            }
        }

        private void checkSAINEnd()
        {
            // Are all decisions None? Then SAIN is no longer active.
            if (CurrentSoloDecision == SoloDecision.None
                && CurrentSelfDecision == SelfDecision.None
                && CurrentSquadDecision == SquadDecision.None)
            {
                OnSAINDeactivated?.Invoke(Time.time);
            }
        }

        private bool checkForNewDecision(SoloDecision solo, SquadDecision squad, SelfDecision self)
        {
            bool newDecision = false;
            float time = Time.time;
            if (CurrentSoloDecision != solo)
            {
                ChangeSoloDecisionTime = time;
                OldSoloDecision = CurrentSoloDecision;
                newDecision = true;
            }
            if (CurrentSquadDecision != squad)
            {
                ChangeSquadDecisionTime = time;
                OldSquadDecision = CurrentSquadDecision;
                newDecision = true;
            }
            if (CurrentSelfDecision != self)
            {
                ChangeSelfDecisionTime = time;
                OldSelfDecision = CurrentSelfDecision;
                newDecision = true;
            }
            return newDecision;
        }

        public void ResetDecisions(bool active)
        {
            if (HasDecision)
            {
                SetDecisions(SoloDecision.None, SquadDecision.None, SelfDecision.None);
                if (active)
                {
                    BotOwner.CalcGoal();
                }
            }
        }

        private bool shallAvoidGrenade()
        {
            Vector3? grenadePos = Bot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null)
            {
                float pathDist = calcPathDist(grenadePos.Value);
                return checkDistances(pathDist, (grenadePos.Value - Bot.Position).magnitude);
            }
            return false;
        }

        private bool checkDistances(float pathDist, float straightDist)
        {
            if (pathDist > straightDist * 1.5f)
            {
                return false;
            }
            if (CurrentSoloDecision == SoloDecision.AvoidGrenade)
            {
                if (straightDist < 5f)
                {
                    //return true;
                }
                if (pathDist < 15f)
                {
                    return true;
                }
            }
            if (straightDist < 3f)
            {
                //return true;
            }
            if (pathDist < 10f)
            {
                return true;
            }
            return false;
        }

        private float calcPathDist(Vector3 point)
        {
            if (_nextCalcPathTime < Time.time)
            {
                _nextCalcPathTime = Time.time + _calcPathFreq;
                _grenadePathDist = calcPathAndReturnDist(point);
            }
            return _grenadePathDist;
        }

        private float _nextCalcPathTime;
        private float _calcPathFreq = 0.5f;
        private float _grenadePathDist;

        private float calcPathAndReturnDist(Vector3 point)
        {
            Vector3 botPosition = Bot.Position;
            if (NavMesh.SamplePosition(point, out var hit, 5f, -1))
            {
                if (NavMesh.SamplePosition(botPosition, out var botHit, 0.5f, -1))
                {
                    botPosition = botHit.position;
                }
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(botPosition, hit.position, -1, path))
                {
                    return path.CalculatePathLength();
                }
            }
            return (botPosition - point).magnitude;
        }

        public float ChangeSoloDecisionTime { get; private set; }
        public float ChangeSelfDecisionTime { get; private set; }
        public float ChangeSquadDecisionTime { get; private set; }

        private bool CheckContinueRetreat()
        {
            bool runningToCover = CurrentSoloDecision == SoloDecision.Retreat || CurrentSoloDecision == SoloDecision.RunToCover;
            if (!runningToCover)
            {
                return false;
            }

            float timeChangeDec = Bot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec > 30 && 
                !Bot.BotStuck.BotHasChangedPosition)
            {
                return false;
            }

            //if (Running && !Bot.BotStuck.BotHasChangedPosition && Bot.BotStuck.TimeSpentNotMoving > 1f && timeChangeDec > 2f)
            //{
            //    return false;
            //}

            if (Bot.Cover.CoverInUse?.Status == CoverStatus.InCover)
            {
                return false;
            }

            return Bot.Mover.SprintController.Running;
        }
    }
}