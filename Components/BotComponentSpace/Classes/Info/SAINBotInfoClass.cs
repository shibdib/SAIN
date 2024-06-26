using EFT;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.Personalities;
using System.Collections.Generic;
using System.Reflection;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINBotInfoClass : SAINBase, ISAINClass
    {
        public SAINBotInfoClass(BotComponent sain) : base(sain)
        {
            Profile = new ProfileClass(sain);
            WeaponInfo = new WeaponInfoClass(sain);
            PresetHandler.OnPresetUpdated += GetFileSettings;
            GetFileSettings();
        }

        public void Init()
        {
            WeaponInfo.Init();
        }

        public void Update()
        {
            WeaponInfo.Update();
        }

        public void Dispose()
        {
            PresetHandler.OnPresetUpdated -= GetFileSettings;
            WeaponInfo.Dispose();
        }

        public ProfileClass Profile { get; private set; }

        private static FieldInfo[] EFTSettingsCategories;
        private static FieldInfo[] SAINSettingsCategories;

        private static readonly Dictionary<FieldInfo, FieldInfo[]> EFTSettingsFields = new Dictionary<FieldInfo, FieldInfo[]>();
        private static readonly Dictionary<FieldInfo, FieldInfo[]> SAINSettingsFields = new Dictionary<FieldInfo, FieldInfo[]>();

        public void GetFileSettings()
        {
            FileSettings = SAINPlugin.LoadedPreset.BotSettings.GetSAINSettings(WildSpawnType, BotDifficulty);
            CalcPersonality();
            UpdateExtractTime();
            SetConfigValues(FileSettings);
        }

        public void CalcPersonality()
        {
            Personality = GetPersonality();
            PersonalitySettingsClass = SAINPlugin.LoadedPreset.PersonalityManager.PersonalityDictionary[Personality];
            CalcTimeBeforeSearch();
            CalcHoldGroundDelay();
        }

        private void SetConfigValues(SAINSettingsClass sainFileSettings)
        {
            var eftFileSettings = BotOwner.Settings.FileSettings;
            if (EFTSettingsCategories == null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public;

                EFTSettingsCategories = eftFileSettings.GetType().GetFields(flags);
                foreach (FieldInfo field in EFTSettingsCategories)
                {
                    EFTSettingsFields.Add(field, field.FieldType.GetFields(flags));
                }

                SAINSettingsCategories = sainFileSettings.GetType().GetFields(flags);
                foreach (FieldInfo field in SAINSettingsCategories)
                {
                    SAINSettingsFields.Add(field, field.FieldType.GetFields(flags));
                }
            }

            foreach (FieldInfo sainCategoryField in SAINSettingsCategories)
            {
                FieldInfo eftCategoryField = Reflection.FindFieldByName(sainCategoryField.Name, EFTSettingsCategories);
                if (eftCategoryField != null)
                {
                    object sainCategory = sainCategoryField.GetValue(sainFileSettings);
                    object eftCategory = eftCategoryField.GetValue(eftFileSettings);

                    FieldInfo[] sainFields = SAINSettingsFields[sainCategoryField];
                    FieldInfo[] eftFields = EFTSettingsFields[eftCategoryField];

                    foreach (FieldInfo sainVarField in sainFields)
                    {
                        FieldInfo eftVarField = Reflection.FindFieldByName(sainVarField.Name, eftFields);
                        if (eftVarField != null)
                        {
                            object sainValue = sainVarField.GetValue(sainCategory);
                            if (SAINPlugin.DebugMode)
                            {
                                //string message = $"[{eftVarField.Name}] : Default Value = [{eftVarField.GetValue(eftCategory)}] New Value = [{sainValue}]";
                                //Logger.LogInfo(message);
                                //Logger.NotifyInfo(message);
                            }
                            eftVarField.SetValue(eftCategory, sainValue);
                        }
                    }
                }
            }
            UpdateSettingClass.ManualSettingsUpdate(WildSpawnType, BotDifficulty, BotOwner, FileSettings);
        }

        public SAINSettingsClass FileSettings { get; private set; }

        public float TimeBeforeSearch { get; private set; } = 0f;

        public float HoldGroundDelay { get; private set; }

        public void CalcHoldGroundDelay()
        {
            var settings = PersonalitySettings;
            float baseTime = settings.General.HoldGroundBaseTime * AggressionMultiplier;

            float min = settings.General.HoldGroundMinRandom;
            float max = settings.General.HoldGroundMaxRandom;
            HoldGroundDelay = baseTime.Randomize(min, max).Round100();
        }

        private float AggressionMultiplier => (FileSettings.Mind.Aggression * GlobalSettings.Mind.GlobalAggression * PersonalitySettings.General.AggressionMultiplier).Round100();

        public void CalcTimeBeforeSearch()
        {
            float searchTime;
            if (WildSpawnType == WildSpawnType.bossKilla || WildSpawnType == WildSpawnType.bossTagilla)
            {
                searchTime = 0.1f;
            }
            else if (Profile.IsFollower && Bot.Squad.BotInGroup)
            {
                searchTime = 10f;
            }
            else
            {
                searchTime = PersonalitySettings.Search.SearchBaseTime;
            }

            searchTime = (searchTime.Randomize(0.66f, 1.33f) / AggressionMultiplier).Round100();
            if (searchTime < 0.1f)
            {
                searchTime = 0.1f;
            }

            TimeBeforeSearch = searchTime;
            float random = 30f.Randomize(0.75f, 1.25f).Round100();
            float forgetTime = searchTime + random;
            if (forgetTime < 240f)
            {
                forgetTime = 240f.Randomize(0.9f, 1.1f).Round100();
            }

            BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = forgetTime;
            ForgetEnemyTime = forgetTime;
            ForgetEnemyTimeAI = forgetTime * 0.66f;
        }

        public float ForgetEnemyTime { get; private set; }
        public float ForgetEnemyTimeAI { get; private set; }

        private void UpdateExtractTime()
        {
            float percentage = Random.Range(FileSettings.Mind.MinExtractPercentage, FileSettings.Mind.MaxExtractPercentage);

            var squad = Bot?.Squad;
            var members = squad?.Members;
            if (squad != null && squad.BotInGroup && members != null && members.Count > 0)
            {
                if (squad.IAmLeader)
                {
                    PercentageBeforeExtract = percentage;
                    foreach (var member in members)
                    {
                        var infocClass = member.Value?.Info;
                        if (infocClass != null)
                        {
                            infocClass.PercentageBeforeExtract = percentage;
                        }
                    }
                }
                else if (PercentageBeforeExtract == -1f)
                {
                    var Leader = squad?.LeaderComponent?.Info;
                    if (Leader != null)
                    {
                        PercentageBeforeExtract = Leader.PercentageBeforeExtract;
                    }
                }
            }
            else
            {
                PercentageBeforeExtract = percentage;
            }
        }

        public EPersonality GetPersonality()
        {
            if (SAINPlugin.LoadedPreset.GlobalSettings.Personality.CheckForForceAllPers(out EPersonality result))
            {
                return result;
            }

            result = setNicknamePersonality(Player.Profile.Nickname.ToLower());
            if (result != EPersonality.Normal)
            {
                return result;
            }
            result = setBossPersonality(WildSpawnType);
            if (result != EPersonality.Normal)
            {
                return result;
            }

            foreach (var setting 
                in SAINPlugin.LoadedPreset.PersonalityManager.PersonalityDictionary)
            {
                if (setting.Value.Assignment.CanBePersonality(this))
                {
                    return setting.Key;
                }
            }
            if (Profile.IsPMC && EFTMath.RandomBool(40))
            {
                return EPersonality.Chad;
            }
            return EPersonality.Normal;
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

        public WildSpawnType WildSpawnType => Profile.WildSpawnType;
        public float PowerLevel => Profile.PowerLevel;
        public int PlayerLevel => Profile.PlayerLevel;
        public BotDifficulty BotDifficulty => Profile.BotDifficulty;

        public EPersonality Personality { get; private set; }
        public PersonalityBehaviorSettings PersonalitySettings => PersonalitySettingsClass?.Behavior;
        public PersonalitySettingsClass PersonalitySettingsClass { get; private set; }

        public float PercentageBeforeExtract { get; set; } = -1f;
        public bool ForceExtract { get; set; } = false;

        public WeaponInfoClass WeaponInfo { get; private set; }
    }
}