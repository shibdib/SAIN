using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : BotBase, IBotClass
    {
        private const float SPEED_OF_SOUND = 343;

        public BotSoundStruct? LastHeardSound { get; private set; }
        public BotSoundStruct? LastFailedSound { get; private set; }

        public HearingInputClass SoundInput { get; }
        public HearingAnalysisClass Analysis { get; }
        public HearingBulletAnalysisClass BulletAnalysis { get; }
        public HearingDispersionClass Dispersion { get; }

        public SAINHearingSensorClass(BotComponent sain) : base(sain)
        {
            SoundInput = new HearingInputClass(this);
            Analysis = new HearingAnalysisClass(this);
            BulletAnalysis = new HearingBulletAnalysisClass(this);
            Dispersion = new HearingDispersionClass(this);
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
            SoundInput.Init();
            Analysis.Init();
            BulletAnalysis.Init();
            Dispersion.Init();
        }

        public void Update()
        {
            SoundInput.Update();
            Analysis.Update();
            BulletAnalysis.Update();
            Dispersion.Update();
        }

        public void Dispose()
        {
            SoundInput.Dispose();
            Analysis.Dispose();
            BulletAnalysis.Dispose();
            Dispersion.Dispose();
        }

        public void ReactToHeardSound(BotSoundStruct sound)
        {
            sound.Results.Heard = Analysis.CheckIfSoundHeard(sound);
            sound.BulletData.BulletFelt = BulletAnalysis.DoIFeelBullet(sound);

            if (checkReact(sound))
            {
                LastHeardSound = sound;
            }
            else
            {
                LastFailedSound = sound;
            }
        }

        private bool checkReact(BotSoundStruct sound)
        {
            bool heard = sound.Results.Heard;
            bool bulletFelt = sound.BulletData.BulletFelt;
            if (!heard && !bulletFelt)
            {
                return false;
            }

            if (bulletFelt && 
                BulletAnalysis.DidShotFlyByMe(sound))
            {
                LastHeardSound = sound;
                float addDisp = heard ? 1f : 2f;
                sound.Results.EstimatedPosition = Dispersion.CalcRandomizedPosition(sound, addDisp);
                Bot.StartCoroutine(delayReact(sound));
                return true;
            }

            if (!heard)
            {
                return false;
            }
            if (sound.Info.IsGunShot && 
                !shallChaseGunshot(sound))
            {
                return false;
            }

            sound.Results.EstimatedPosition = Dispersion.CalcRandomizedPosition(sound, 1f);
            Bot.StartCoroutine(delayAddSearch(sound));
            return true;
        }

        private bool shallChaseGunshot(BotSoundStruct sound)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillChaseDistantGunshots)
            {
                return true;
            }
            if (sound.Info.EnemyDistance > searchSettings.AudioStraightDistanceToIgnore)
            {
                return false;
            }
            return true;
        }

        private IEnumerator baseHearDelay(float distance, float soundSpeed = -1f)
        {
            if (soundSpeed == -1f)
            {
                soundSpeed = SPEED_OF_SOUND;
            }
            float delay = distance / soundSpeed;
            if (Bot?.EnemyController?.AtPeace == true)
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayAtPeace;
            }
            else
            {
                delay += SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseHearingDelayWithEnemy;
            }
            yield return new WaitForSeconds(delay);
        }

        private IEnumerator delayReact(BotSoundStruct sound)
        {
            var weapon = sound.Info.EnemyPlayer.Equipment.CurrentWeapon;
            float speed = weapon != null ? weapon.BulletSpeed : -1f;

            yield return baseHearDelay(sound.Info.EnemyDistance, speed);

            Enemy enemy = sound.Info.Enemy;
            if (Bot != null && 
                sound.Info.EnemyPlayer != null &&
                enemy?.CheckValid() == true)
            {
                float projDist = sound.BulletData.ProjectionPointDistance;
                bool underFire = projDist <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
                if (!underFire && SoundInput.IgnoreHearing)
                {
                    yield break;
                }

                if (underFire)
                {
                    BotOwner?.HearingSensor?.OnEnemySounHearded?.Invoke(sound.Results.EstimatedPosition, sound.Info.EnemyDistance, sound.Info.SoundType.Convert());
                    Bot.Memory.SetUnderFire(enemy, sound.Results.EstimatedPosition);
                }
                Bot.Suppression.AddSuppression(enemy, projDist);
                enemy.SetEnemyAsSniper(sound.Info.EnemyDistance > 100f);
                enemy.Status.ShotAtMeRecently = true;
                addPointToSearch(sound);
            }
        }

        private IEnumerator delayAddSearch(BotSoundStruct sound)
        {
            yield return baseHearDelay(sound.Info.EnemyDistance);

            if (Bot != null &&
                sound.Info.EnemyPlayer != null &&
                sound.Info.Enemy?.CheckValid() == true)
            {
                addPointToSearch(sound);
            }
        }

        private void addPointToSearch(BotSoundStruct sound)
        {
            Bot.Squad.SquadInfo?.AddPointToSearch(sound, Bot);
            CheckCalcGoal();
        }

        private void CheckCalcGoal()
        {
            if (BotOwner.Memory.GoalEnemy == null)
            {
                try
                {
                    BotOwner.BotsGroup.CalcGoalForBot(BotOwner);
                }
                catch { }
            }
        }
    }
}