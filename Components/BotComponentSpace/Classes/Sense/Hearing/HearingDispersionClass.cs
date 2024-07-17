using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingDispersionClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        public HearingDispersionClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public Vector3 CalcRandomizedPosition(BotSound sound, float addDispersion)
        {
            float distance = sound.Distance;
            float baseDispersion = getBaseDispersion(distance, sound.Info.SoundType);
            float dispersionMod = getDispersionModifier(sound) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;

            finalDispersion = Mathf.Clamp(finalDispersion, 0f, 50f);
            sound.Dispersion.Dispersion = finalDispersion;
            float min = distance < 10 ? 0f : 0.5f;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.Logs.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{distance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{sound.Info.SoundType}]");

            Vector3 estimatedEnemyPos = sound.Info.Position + randomdirection;
            Vector3 dirToRandomPos = estimatedEnemyPos - Bot.Position;
            Vector3 result = Bot.Position + (dirToRandomPos.normalized * distance);
            return result;
        }

        private float getBaseDispersion(float enemyDistance, SAINSoundType soundType)
        {
            const float dispSuppGun = 13.5f;
            const float dispGun = 17.5f;
            const float dispStep = 12.5f;

            float dispersion;
            switch (soundType)
            {
                case SAINSoundType.Shot:
                    dispersion = enemyDistance / dispGun;
                    break;

                case SAINSoundType.SuppressedShot:
                    dispersion = enemyDistance / dispSuppGun;
                    break;

                default:
                    dispersion = enemyDistance / dispStep;
                    break;
            }

            return dispersion;
        }

        private void calcNewDispersion(BotSound sound)
        {
        }

        private void calcNewDispersionAngle(BotSound sound)
        {
        }

        private void calcNewDispersionDistance(BotSound sound)
        {
        }

        private float getDispersionModifier(BotSound sound)
        {
            float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, sound.Enemy.EnemyDirectionNormal);
            float scaled = (dotProduct + 1) / 2;

            float dispersionModifier = Mathf.Lerp(1.5f, 0.5f, scaled);
            //Logger.LogInfo($"Dispersion Modifier for Sound [{dispersionModifier}] Dot Product [{dotProduct}]");
            return dispersionModifier;
        }

        private float modifyDispersion(float dispersion, IPlayer person, out int soundCount)
        {
            // If a bot is hearing multiple sounds from a iPlayer, they will now be more accurate at finding the source soundPosition based on how many sounds they have heard from this particular iPlayer
            soundCount = 0;
            float finalDispersion = dispersion;
            //if (person != null && SoundsHeardFromPlayer.ContainsKey(person.ProfileId))
            //{
            //    SAINSoundCollection collection = SoundsHeardFromPlayer[person.ProfileId];
            //    if (collection != null)
            //    {
            //        soundCount = collection.Count;
            //    }
            //}
            //if (soundCount > 1)
            //{
            //    finalDispersion = (dispersion / soundCount).Round100();
            //}
            return finalDispersion;
        }

        private Vector3 getRandomizedDirection(float dispersion, float min = 0.5f)
        {
            float randomHeight = dispersion / 10f;
            float randomX = UnityEngine.Random.Range(-dispersion, dispersion);
            //float randomY = UnityEngine.Random.Range(-randomHeight, randomHeight);
            float randomZ = UnityEngine.Random.Range(-dispersion, dispersion);
            Vector3 randomdirection = new Vector3(randomX, 0, randomZ);

            if (min > 0 && randomdirection.sqrMagnitude < min * min)
            {
                randomdirection = Vector3.Normalize(randomdirection) * min;
            }
            return randomdirection;
        }

        public Vector3 GetEstimatedPoint(Vector3 source, float distance)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere;
            randomPoint.y = 0;
            randomPoint *= (distance / 10f);
            return source + randomPoint;
        }
    }
}