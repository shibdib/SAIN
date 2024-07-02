using SAIN.Preset;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SoundDispersionClass : BotBaseClass
    {
        public SoundDispersionClass(BotComponent bot) : base(bot) { }

        public void Calculate(BotSoundStruct sound)
        {
            sound.Dispersion = new SoundDispersionData
            {
                DispersionType = findType(sound),
            };

            calcDispersion(sound);
        }

        private ESoundDispersionType findType(BotSoundStruct sound)
        {
            bool heard = sound.Results.Heard;
            switch (sound.Info.SoundType)
            {
                case SAINSoundType.Shot: 
                    return heard ? ESoundDispersionType.HeardShot : ESoundDispersionType.UnheardShot;

                case SAINSoundType.SuppressedShot: 
                    return heard ? ESoundDispersionType.HeardSuppressedShot : ESoundDispersionType.UnheardSuppressedShot;

                default: 
                    return ESoundDispersionType.Footstep;
            }
        }

        private void calcDispersion(BotSoundStruct sound)
        {
            calcDispersionAngle(sound);
            calcDispersionDistance(sound);

            sound.Dispersion.EstimatedPosition = estimatePosition(sound);
        }

        private Vector3 estimatePosition(BotSoundStruct sound)
        {
            return Vector3.zero;
        }

        private void calcDispersionAngle(BotSoundStruct sound)
        {
            bool gunshot = sound.Bullet != null;
        }

        private void calcDispersionDistance(BotSoundStruct sound)
        {

        }

        private static DispersionDictionary _dispersionValues;

        protected override void UpdatePresetSettings(SAINPresetClass preset)
        {
            base.UpdatePresetSettings(preset);
            _dispersionValues = preset.GlobalSettings.Hearing.DispersionValues;
        }
    }
}