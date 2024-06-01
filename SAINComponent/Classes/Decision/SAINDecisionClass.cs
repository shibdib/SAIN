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
        public SAINDecisionClass(BotComponent sain) : base(sain)
        {
            SelfActionDecisions = new SelfActionDecisionClass(sain);
            EnemyDecisions = new EnemyDecisionClass(sain);
            GoalTargetDecisions = new TargetDecisionClass(sain);
            SquadDecisions = new SquadDecisionClass(sain);
        }

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
                EnemyDistance = SAINBot.Enemy != null ? SAINBot.Enemy.CheckPathDistance() : EnemyPathDistance.NoEnemy;
                getDecision();
            }
            if (mainDecisionCoroutine == null && SAINBot.BotActive)
            {
                //stopped = false;
                //mainDecisionCoroutine = SAINBot.StartCoroutine(mainDecisionLoop());
            }
            if (mainDecisionCoroutine != null && !SAINBot.BotActive)
            {
                //SAINBot.StopCoroutine(mainDecisionCoroutine);
                //mainDecisionCoroutine = null;
            }
            if (stopped && mainDecisionCoroutine != null)
            {
                //Logger.LogError("stopped && mainDecisionCoroutine != null");
            }
            if (SAINBot.Enemy != null && !HasDecision)
            {
                //Logger.LogError("Have No Decision but enemy is not null!");
            }
        }

        private Coroutine mainDecisionCoroutine;
        bool stopped = false;


        private float _nextGetDecisionTime;
        private const float getDecisionFreq = 0.1f;
        private const float getDecisionFreqAtPeace = 0.25f;

        public void Dispose()
        {
        }

        public EnemyPathDistance EnemyDistance { get; private set; }

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

            if (CheckContinueRetreat())
            {
                return;
            }

            if (SelfActionDecisions.GetDecision(out SelfDecision selfDecision))
            {
                SetDecisions(SoloDecision.Retreat, SquadDecision.None, selfDecision);
                return;
            }

            if (CurrentSoloDecision != SoloDecision.RushEnemy &&
                CurrentSoloDecision != SoloDecision.RunToCover &&
                CurrentSoloDecision != SoloDecision.Retreat &&
                shallDogfight())
            {
                SetDecisions(SoloDecision.DogFight, SquadDecision.None, SelfDecision.None);
                return;
            }
            else if (DogFightTarget != null)
            {
                DogFightTarget = null;
            }

            if (SquadDecisions.GetDecision(out SquadDecision squadDecision))
            {
                SetDecisions(SoloDecision.None, squadDecision, SelfDecision.None);
                return;
            }

            //if (CheckStuckDecision(out SoloDecision soloDecision))
            //{
            //    setDecision(soloDecision, SquadDecision.None, SelfDecision.None);
            //    return;
            //}

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
                if (_nextLogTIme < Time.time)
                {
                    //_nextLogTIme = Time.time + 3f;
                    //if (SAINBot.Enemy?.Player?.IsYourPlayer == true)
                    //    Logger.LogWarning($"{BotOwner.name} : {solo} {squad} {self}");
                }
            }
        }

        private static float _nextLogTIme;

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

        private bool shallDogfight()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            if (CurrentSoloDecision == SoloDecision.DogFight
                && DogFightTarget != null)
            {
                checkClearDogFightTarget();
                if (DogFightTarget != null)
                {
                    return true;
                }
            }
            DogFightTarget = findDogFightTarget();
            return DogFightTarget != null;
        }

        private void checkClearDogFightTarget()
        {
            if (DogFightTarget == null)
            {
                return;
            }
            if (DogFightTarget.Player?.HealthController.IsAlive == false)
            {
                DogFightTarget = null;
                return;
            }
            float pathDist = DogFightTarget.Path.PathDistance;
            if (pathDist > _dogFightEndDist)
            {
                DogFightTarget = null;
                return;
            }
            if (!DogFightTarget.IsVisible && DogFightTarget.TimeSinceSeen > 2f)
            {
                DogFightTarget = null;
                return;
            }
        }

        private SAINEnemy findDogFightTarget()
        {
            var enemies = SAINBot.EnemyController.Enemies;
            foreach (var enemy in enemies)
            {
                if (shallDogFightEnemy(enemy.Value))
                {
                    return enemy.Value;
                }
            }
            return null;
        }

        public SAINEnemy DogFightTarget { get; set; }

        private bool shallDogFightEnemy(SAINEnemy enemy)
        {
            return enemy?.IsValid == true && enemy.IsVisible && enemy.Path.PathDistance < _dogFightStartDist;
        }

        private float _dogFightStartDist = 4f;
        private float _dogFightEndDist = 10f;

        private bool shallAvoidGrenade()
        {
            Vector3? grenadePos = SAINBot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null)
            {
                float pathDist = calcPathDist(grenadePos.Value);
                return checkDistances(pathDist, (grenadePos.Value - SAINBot.Position).magnitude);
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
            Vector3 botPosition = SAINBot.Position;
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
            if (CurrentSoloDecision == SoloDecision.None)
            {
                return false;
            }
            float timeChangeDec = SAINBot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec > 10)
            {
                return false;
            }
            bool Running = CurrentSoloDecision == SoloDecision.Retreat || CurrentSoloDecision == SoloDecision.RunToCover;
            if (Running && !SAINBot.BotStuck.BotHasChangedPosition && SAINBot.BotStuck.TimeSpentNotMoving > 1f && timeChangeDec > 2f)
            {
                return false;
            }
            CoverPoint pointInUse = SAINBot.Cover.CoverInUse;
            if (pointInUse != null && pointInUse.IsBad == false)
            {
                if (pointInUse.Status == CoverStatus.InCover)
                {
                    return false;
                }
                return BotOwner.Mover.IsMoving;
            }
            return false;
        }

        private bool CheckStuckDecision(out SoloDecision Decision)
        {
            Decision = SoloDecision.None;
            bool stuck = SAINBot.BotStuck.BotIsStuck;

            if (!stuck && FinalBotUnstuckTimer != 0f)
            {
                FinalBotUnstuckTimer = 0f;
            }

            if (stuck && BotUnstuckTimerDecision < Time.time)
            {
                if (FinalBotUnstuckTimer == 0f)
                {
                    FinalBotUnstuckTimer = Time.time + 10f;
                }

                BotUnstuckTimerDecision = Time.time + 5f;

                var current = this.CurrentSoloDecision;
                if (FinalBotUnstuckTimer < Time.time && SAINBot.HasEnemy)
                {
                    Decision = SoloDecision.UnstuckDogFight;
                    return true;
                }
                if (current == SoloDecision.Search || current == SoloDecision.UnstuckSearch)
                {
                    Decision = SoloDecision.UnstuckMoveToCover;
                    return true;
                }
                if (current == SoloDecision.MoveToCover || current == SoloDecision.UnstuckMoveToCover)
                {
                    Decision = SoloDecision.UnstuckSearch;
                    return true;
                }
            }
            return false;
        }

        private float BotUnstuckTimerDecision = 0f;
        private float FinalBotUnstuckTimer = 0f;
    }
}