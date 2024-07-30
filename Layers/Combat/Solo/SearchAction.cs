using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace SAIN.Layers.Combat.Solo
{
    internal class SearchAction : CombatAction, ISAINAction
    {
        public SearchAction(BotOwner bot) : base(bot, "Search")
        {
        }

        public override void Update()
        {
            try {
                var enemy = _searchTarget;
                if (enemy == null) {
                    enemy = Bot.Enemy;
                    if (enemy == null) {
                        return;
                    }
                    _searchTarget = enemy;
                    enemy = _searchTarget;
                    Search.ToggleSearch(true, enemy);
                }

                bool isBeingStealthy = enemy.Hearing.EnemyHeardFromPeace;
                if (isBeingStealthy) {
                    _sprintEnabled = false;
                }
                else {
                    CheckShouldSprint();
                    talk();
                }

                Steer();

                if (_nextUpdateSearchTime < Time.time) {
                    _nextUpdateSearchTime = Time.time + 0.1f;
                    Search.Search(_sprintEnabled, enemy);
                }

                if (!_sprintEnabled) {
                    Shoot.CheckAimAndFire();
                    if (!isBeingStealthy)
                        checkWeapon();
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
            }
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        private Enemy _searchTarget;

        public override void Start()
        {
            _searchTarget = Bot.Enemy;
            Search.ToggleSearch(true, _searchTarget);
            Toggle(true);
        }

        public override void Stop()
        {
            Search.ToggleSearch(false, _searchTarget);
            _searchTarget = null;
            Toggle(false);
            BotOwner.Mover?.MovementResume();
            HaveTalked = false;
        }

        private float _nextCheckTime;

        private float _nextUpdateSearchTime;

        private void talk()
        {
            if (Search.FinalDestination == null) {
                return;
            }

            // Scavs will speak out and be more vocal
            if (!HaveTalked &&
                Bot.Info.Profile.IsScav &&
                (BotOwner.Position - Search.FinalDestination.Value).sqrMagnitude < 50f * 50f) {
                HaveTalked = true;
                if (EFTMath.RandomBool(40)) {
                    Bot.Talk.Say(EPhraseTrigger.OnMutter, ETagStatus.Aware, true);
                }
            }
        }

        private void checkWeapon()
        {
            if (_nextCheckTime < Time.time) {
                _nextCheckTime = Time.time + 180f * Random.Range(0.5f, 1.5f);

                if (Bot.Enemy != null && Bot.Enemy.TimeSinceLastKnownUpdated > 20f) {
                    if (EFTMath.RandomBool())
                        Bot.Player.HandsController.FirearmsAnimator.CheckAmmo();
                    else
                        Bot.Player.HandsController.FirearmsAnimator.CheckChamber();
                }
            }
        }

        private bool HaveTalked = false;

        private SAINSearchClass Search => Bot.Search;

        private void CheckShouldSprint()
        {
            //  || Search.CurrentState == ESearchMove.MoveToDangerPoint
            if (Search.CurrentState == ESearchMove.MoveToEndPeek || Search.CurrentState == ESearchMove.Wait) {
                _sprintEnabled = false;
                return;
            }

            //  || Bot.Enemy?.InLineOfSight == true
            if (_searchTarget?.IsVisible == true) {
                _sprintEnabled = false;
                return;
            }

            if (Bot.Decision.CurrentSquadDecision == ESquadDecision.Help) {
                _sprintEnabled = true;
                return;
            }

            var persSettings = Bot.Info.PersonalitySettings;
            float chance = persSettings.Search.SprintWhileSearchChance;
            if (RandomSprintTimer < Time.time && chance > 0) {
                float myPower = Bot.Info.Profile.PowerLevel;
                if (Bot.Enemy?.EnemyPlayer != null && Bot.Enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f) {
                    chance = 100f;
                }

                _sprintEnabled = EFTMath.RandomBool(chance);
                float timeAdd;
                if (_sprintEnabled) {
                    timeAdd = 4f * Random.Range(0.5f, 2.00f);
                }
                else {
                    timeAdd = 4f * Random.Range(0.5f, 1.5f);
                }
                RandomSprintTimer = Time.time + timeAdd;
            }
        }

        private bool _sprintEnabled = false;
        private float RandomSprintTimer = 0f;

        private void Steer()
        {
            if (!Bot.Steering.SteerByPriority(null, false)
                //!Bot.Steering.LookToLastKnownEnemyPosition(_searchTarget) &&
                //!Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy)) {
                ) {
                Bot.Steering.LookToMovingDirection();
            }
        }
    }
}