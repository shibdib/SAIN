using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public enum EBotFlashedType
    {
        None,
        Partial,
        Full,
        HearingDamaged,
    }

    public enum EBotFlashedDecisions
    {
        MagDump,
        Run,
        Hide,
    }

    public class BotFlashbangedClass : BotSubClass<BotGrenadeManager>, IBotClass
    {
        public event Action<EBotFlashedType, float, float> OnBotFlashed;

        private const float BOT_FLASHED_MAX_RANGE_VISION = 40f;
        private const float BOT_FLASHED_MAX_RANGE_VISION_SQR = BOT_FLASHED_MAX_RANGE_VISION * BOT_FLASHED_MAX_RANGE_VISION;
        private const float BOT_FLASHED_FULL_ANGLE = 40f;
        private const float BOT_FLASHED_PARTIAL_ANGLE = 80f;
        private const float BOT_FLASHED_HEARING_RANGE = 30f;
        private const float BOT_FLASHED_HEARING_RANGE_SQR = BOT_FLASHED_HEARING_RANGE * BOT_FLASHED_HEARING_RANGE;
        private const float BOT_FLASH_DURATION = 8f;
        private const float BOT_FLASH_DURATION_EXPIRE_TIME = 15f;

        public BotFlashbangedClass(BotGrenadeManager grenadeManager) : base(grenadeManager)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void BotFlashed(float time, Vector3 position)
        {
            EBotFlashedType flashedType = checkIfFlashed(position);
            if (flashedType == EBotFlashedType.None) {
                return;
            }
            OnBotFlashed?.Invoke(flashedType, BOT_FLASH_DURATION, BOT_FLASH_DURATION_EXPIRE_TIME);
        }

        private EBotFlashedType checkIfFlashed(Vector3 position)
        {
            bool hearingAffected = false;
            bool partialFlashed = false;
            bool fullFlashed = false;

            Vector3 lookDirection = Bot.LookDirection;
            Vector3 flashDirection = position - Bot.Transform.EyePosition;
            float sqrMag = flashDirection.sqrMagnitude;
            if (sqrMag <= BOT_FLASHED_HEARING_RANGE_SQR) {
                hearingAffected = true;
            }
            if (sqrMag <= BOT_FLASHED_MAX_RANGE_VISION_SQR) {
                float angle = Vector3.Angle(lookDirection, flashDirection);
                if (angle <= BOT_FLASHED_FULL_ANGLE) {
                    fullFlashed = true;
                }
                else if (angle <= BOT_FLASHED_PARTIAL_ANGLE) {
                    partialFlashed = true;
                }
            }

            bool visionAffected = partialFlashed || fullFlashed;

            if (!visionAffected && !hearingAffected) {
                return EBotFlashedType.None;
            }
            if (!visionAffected) {
                return EBotFlashedType.HearingDamaged;
            }
            if (partialFlashed) {
                return EBotFlashedType.Partial;
            }
            return EBotFlashedType.Full;
        }
    }
}