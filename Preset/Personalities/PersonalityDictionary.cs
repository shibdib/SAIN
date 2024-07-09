using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Preset.Personalities
{
    public class PersonalityDictionary : Dictionary<EPersonality, PersonalitySettingsClass>
    {
        public EPersonality GetPersonality(SAINBotInfoClass infoClass, out PersonalitySettingsClass settings)
        {
            if (checkForcePersonality(out EPersonality result))
            {
                settings = this[result];
                return result;
            }

            result = setNicknamePersonality(infoClass.Profile.NickName);
            if (result != EPersonality.Normal)
            {
                settings = this[result];
                return result;
            }

            result = setBossPersonality(infoClass.Profile.WildSpawnType);
            if (result != EPersonality.Normal)
            {
                settings = this[result];
                return result;
            }

            foreach (var setting in this)
            {
                if (canBotBePersonality(infoClass, setting.Key))
                {
                    settings = setting.Value;
                    return setting.Key;
                }
            }

            if (infoClass.Profile.IsPMC && EFTMath.RandomBool(33))
                result = EPersonality.Chad;
            else
                result = EPersonality.Normal;

            settings = this[result];
            return result;
        }

        private bool checkForcePersonality(out EPersonality personality)
        {
            foreach (var item in SAINPlugin.LoadedPreset.GlobalSettings.Mind.ForcePersonality)
            {
                if (item.Value == true)
                {
                    personality = item.Key;
                    return true;
                }
            }
            personality = EPersonality.Normal;
            return false;
        }

        private EPersonality setNicknamePersonality(string nickname)
        {
            if (nickname.Contains("solarint"))
            {
                return EPersonality.GigaChad;
            }
            if (nickname.Contains("chomp") || nickname.Contains("senko"))
            {
                return EPersonality.Chad;
            }
            if (nickname.Contains("kaeno") || nickname.Contains("justnu"))
            {
                return EPersonality.Timmy;
            }
            if (nickname.Contains("ratthew") || nickname.Contains("choccy"))
            {
                return EPersonality.Rat;
            }
            return EPersonality.Normal;
        }

        private EPersonality setBossPersonality(WildSpawnType wildSpawnType)
        {
            switch (wildSpawnType)
            {
                case WildSpawnType.bossKilla:
                case WildSpawnType.bossTagilla:
                case WildSpawnType.bossKolontay:
                    return EPersonality.Wreckless;

                case WildSpawnType.bossKnight:
                case WildSpawnType.followerBigPipe:
                    return EPersonality.GigaChad;

                case WildSpawnType.followerBirdEye:
                case WildSpawnType.bossGluhar:
                    return EPersonality.SnappingTurtle;

                case WildSpawnType.bossKojaniy:
                    return EPersonality.Rat;

                case WildSpawnType.bossBully:
                case WildSpawnType.bossSanitar:
                case WildSpawnType.bossBoar:
                    return EPersonality.Coward;

                default:
                    return EPersonality.Normal;
            }
        }

        private bool canBotBePersonality(SAINBotInfoClass infoClass, EPersonality personality)
        {
            if (!this.TryGetValue(personality, out var settings))
            {
                return false;
            }
            var assignment = settings.Assignment;
            if (!assignment.Enabled)
            {
                return false;
            }
            if (checkRandomAssignment(settings))
            {
                return true;
            }
            if (meetsRequirements(infoClass, settings))
            {
                float assignmentChance = getChance(infoClass.Profile.PowerLevel, settings);
                if (EFTMath.RandomBool(assignmentChance))
                {
                    return true;
                }
            }
            return false;
        }

        private bool checkRandomAssignment(PersonalitySettingsClass settings)
        {
            return settings.Assignment.CanBeRandomlyAssigned && EFTMath.RandomBool(settings.Assignment.RandomlyAssignedChance);
        }

        private bool meetsRequirements(SAINBotInfoClass infoClass, PersonalitySettingsClass settings)
        {
            var assignment = settings.Assignment;
            return assignment.AllowedTypes.Contains(infoClass.Profile.WildSpawnType)
                && infoClass.Profile.PowerLevel <= assignment.PowerLevelMax
                && infoClass.Profile.PowerLevel > assignment.PowerLevelMin
                && infoClass.Profile.PlayerLevel <= assignment.MaxLevel
                && infoClass.Profile.PlayerLevel > assignment.MinLevel;
        }

        private float getChance(float powerLevel, PersonalitySettingsClass settings)
        {
            var assignment = settings.Assignment;
            powerLevel = Mathf.Clamp(powerLevel, 0, 1000);
            float modifier0to1 = (powerLevel - assignment.PowerLevelScaleStart) / (assignment.PowerLevelScaleEnd - assignment.PowerLevelScaleStart);
            if (assignment.InverseScale)
            {
                modifier0to1 = 1f - modifier0to1;
            }
            float result = assignment.MaxChanceIfMeetRequirements * modifier0to1;
            result = Mathf.Clamp(result, 0f, 100f);
            //Logger.LogDebug($"Result: [{result}] Power: [{powerLevel}] PowerLevelScaleStart [{PowerLevelScaleStart}] PowerLevelScaleEnd [{PowerLevelScaleEnd}] MaxChanceIfMeetRequirements [{MaxChanceIfMeetRequirements}]");
            return result;
        }
    }
}