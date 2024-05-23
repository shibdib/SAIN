using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;

namespace SAIN.Preset.Personalities
{
    public class PersonalitySettingsClass
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        { }

        public PersonalitySettingsClass(EPersonality personality, string name, string description)
        {
            SAINPersonality = personality;
            Name = name;
            Description = description;
        }

        public EPersonality SAINPersonality;
        public string Name;
        public string Description;

        public PersonalityVariablesClass Variables = new PersonalityVariablesClass();

        public bool CanBePersonality(SAINBotInfoClass infoClass)
        {
            return CanBePersonality(infoClass.WildSpawnType, infoClass.PowerLevel, infoClass.PlayerLevel);
        }

        public bool CanBePersonality(WildSpawnType wildSpawnType, float PowerLevel, int PlayerLevel)
        {
            if (Variables.Enabled == false)
            {
                return false;
            }
            if (Variables.CanBeRandomlyAssigned && EFTMath.RandomBool(Variables.RandomlyAssignedChance))
            {
                return true;
            }
            if (!BotTypeDefinitions.BotTypes.ContainsKey(wildSpawnType))
            {
                return false;
            }
            string name = BotTypeDefinitions.BotTypes[wildSpawnType].Name;
            if (!Variables.AllowedTypes.Contains(name))
            {
                return false;
            }
            if (PowerLevel > Variables.PowerLevelMax || PowerLevel < Variables.PowerLevelMin)
            {
                return false;
            }
            if (PlayerLevel > Variables.MaxLevel)
            {
                return false;
            }
            return EFTMath.RandomBool(Variables.RandomChanceIfMeetRequirements);
        }

        public PersonalityAssignmentSettings Assignment = new PersonalityAssignmentSettings();
        public PersonalityBehaviorSettings Behavior = new PersonalityBehaviorSettings();
        public PersonalityStatModifierSettings StatModifiers = new PersonalityStatModifierSettings();
    }

    public class PersonalityAssignmentSettings
    {
        [Name("Maximum of this Personality Per Raid")]
        [Description("How many alive bots can be assigned this personality. 0 means no limit.")]
        [Default(0f)]
        [MinMax(0f, 50f, 1f)]
        public float MaximumOfThisTypePerRaid = 0f;

        [JsonIgnore]
        [Hidden]
        private const string PowerLevelDescription = " Power level is a combined number that takes into account " +
            "armor, the class of that armor, " +
            "the attachments a bot has on their weapon, " +
            "whether they have a faceshield, " +
            "and the weapon class that is currently used by a bot." +
            " Power Level usually falls within 0 to 250 on average, and almost never goes above 500";

        [Name("Personality Enabled")]
        [Description("Enables or Disables this Personality, if a All Chads, All GigaChads, or AllRats is enabled in global settings, this value is ignored")]
        [Default(true)]
        public bool Enabled = true;

        [NameAndDescription("Can Be Randomly Assigned", "A percentage chance that this personality can be applied to any bot, regardless of bot stats, power, player level, or anything else.")]
        [Default(true)]
        public bool CanBeRandomlyAssigned = true;

        [NameAndDescription("Randomly Assigned Chance", "If personality can be randomly assigned, this is the chance that will happen")]
        [MinMax(0, 100)]
        public float RandomlyAssignedChance = 3;

        [NameAndDescription("Minimum Level", "The min level that a bot can be to be eligible for this personality.")]
        [Percentage]
        public float MinLevel = 0;

        [NameAndDescription("Max Level", "The max level that a bot can be to be eligible for this personality.")]
        [Percentage]
        public float MaxLevel = 100;


        [Name("Power Level Scale Start")]
        [Description("When a bot is at, or above this power level, they will start to have a chance to be assigned this personality.")]
        [MinMax(0, 1000, 1)]
        public float PowerLevelScaleStart = 0f;

        [Name("Power Level Scale End")]
        [Description("When a bot is at, or above this power level, they will have the full percentage chance to be assigned this personality.")]
        [MinMax(0, 1000, 1)]
        public float PowerLevelScaleEnd = 500f;

        [NameAndDescription("Power Level Minimum", "Minimum Power level for a bot to use this personality." + PowerLevelDescription)]
        [MinMax(0, 800, 1)]
        public float PowerLevelMin = 0;

        [NameAndDescription("Power Level Maximum", "Maximum Power level for a bot to use this personality." + PowerLevelDescription)]
        [MinMax(0, 800, 1)]
        public float PowerLevelMax = 800;

        [Name("Maximum Chance If Meets Requirements")]
        [Description("If the bot meets all conditions for this personality, this is the chance the personality will actually be assigned. " +
            "The percentage chance to be assigned scales if a bots power level falls between Power Level Scale Start and Power Level Scale End, " +
            "so if they fall right in the middle, and the value here is 60%, they will have a 30% chance to be assigned.")]
        [MinMax(0, 100, 1)]
        public float MaxChanceIfMeetRequirements = 50;

        [Name("Bots Who Can Use This")]
        [Description("Setting default on these always results in true")]
        [DefaultDictionary(nameof(BotTypeDefinitions.BotTypesNames))]
        [Advanced]
        [Hidden]
        public List<WildSpawnType> AllowedTypes = new List<WildSpawnType>();
    }

    public class PersonalityBehaviorSettings
    {
        public PersonalityGeneralSettings General = new PersonalityGeneralSettings();
        public PersonalitySearchSettings Search = new PersonalitySearchSettings();
        public PersonalityRushSettings Rush = new PersonalityRushSettings();
        public PersonalityCoverSettings Cover = new PersonalityCoverSettings();
        public PersonalityTalkSettings Talk = new PersonalityTalkSettings();
    }

    public class PersonalityStatModifierSettings
    {
    }

    public class PersonalityGeneralSettings
    {
        [Name("Aggression Multiplier")]
        [Description("Linearly increases or decreases search time and hold ground time.")]
        [Default(1f)]
        [MinMax(0.01f, 5f, 100)]
        public float AggressionMultiplier = 1f;

        [Name("Hold Ground Base Time")]
        [Description("The base time, before modifiers, that a personality will stand their ground and shoot or return fire on an enemy if caught out of cover.")]
        [Default(1f)]
        [Advanced]
        [MinMax(0, 3f, 10)]
        public float HoldGroundBaseTime = 1f;

        [Default(0.66f)]
        [Advanced]
        [MinMax(0.1f, 2f, 10)]
        public float HoldGroundMinRandom = 0.66f;

        [Default(1.5f)]
        [Advanced]
        [MinMax(0.1f, 2f, 10)]
        public float HoldGroundMaxRandom = 1.5f;
    }

    public class PersonalitySearchSettings
    {
        [Default(true)]
        [Advanced]
        public bool WillSearchForEnemy = true;

        [Default(true)]
        [Advanced]
        public bool WillSearchFromAudio = true;

        [Default(true)]
        public bool WillChaseDistantGunshots = true;

        [Name("Start Search Base Time")]
        [Description("The base time, before modifiers, that a personality will usually start searching for their enemy.")]
        [Default(40)]
        [MinMax(0.1f, 500f)]
        public float SearchBaseTime = 40;

        [Name("Search Wait Multiplier")]
        [Description("Linearly increases or decreases the time a bot pauses while searching.")]
        [Default(1f)]
        [MinMax(0.01f, 5f, 100)]
        public float SearchWaitMultiplier = 1f;

        [Percentage]
        [Default(0f)]
        public float SprintWhileSearchChance = 0f;

        [Default(false)]
        [Advanced]
        public bool Sneaky = false;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SneakySpeed = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SneakyPose = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SearchNoEnemySpeed = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SearchNoEnemyPose = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SearchHasEnemySpeed = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float SearchHasEnemyPose = 1f;
    }

    public class PersonalityRushSettings
    {
        [Name("Can Rush Healing/Reloading/Grenade-Pulling Enemies")]
        [Default(false)]
        public bool CanRushEnemyReloadHeal = false;

        [Name("Can Jump Push")]
        [Description("Can this personality jump when rushing an enemy?")]
        [Default(false)]
        public bool CanJumpCorners = false;

        [Name("Jump Push Chance")]
        [Description("If a bot can Jump Push, this is the chance they will actually do it.")]
        [Default(60f)]
        [Percentage()]
        public float JumpCornerChance = 60f;

        [Name("Can Bunny Hop during Jump Push")]
        [Description("Can this bot hit a clip on you?")]
        [Default(false)]
        public bool CanBunnyHop = false;

        [Name("Bunny Hop Chance")]
        [Description("If a bot can bunny hop, this is the chance they will actually do it.")]
        [Default(5f)]
        [Percentage()]
        public float BunnyHopChance = 5f;
    }

    public class PersonalityCoverSettings
    {
        [Default(true)]
        [Advanced]
        public bool CanShiftCoverPosition = true;

        [Default(1f)]
        [Advanced]
        public float ShiftCoverTimeMultiplier = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverNoEnemySpeed = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverNoEnemyPose = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverHasEnemySpeed = 1f;

        [Default(1f)]
        [Percentage0to1]
        [Advanced]
        public float MoveToCoverHasEnemyPose = 1f;
    }

    public class PersonalityTalkSettings
    {
        [Name("Can Yell Taunts")]
        [Description("Hey you...yeah YOU! FUCK YOU! You heard?")]
        [Default(false)]
        public bool CanTaunt = false;

        [Name("Can Yell Taunts Frequently")]
        [Description("HEY COCKSUCKAAAA")]
        [Default(false)]
        public bool FrequentTaunt = false;

        [Name("Can Yell Taunts Constantly")]
        [Description("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [Default(false)]
        public bool ConstantTaunt = false;

        [Description("Will this personality yell back at enemies taunting them")]
        [Default(true)]
        public bool CanRespondToEnemyVoice = true;

        [Default(20f)]
        [Advanced]
        [Percentage]
        public float TauntFrequency = 15f;

        [Default(20f)]
        [Advanced]
        [Percentage]
        public float TauntMaxDistance = 70f;

        [Default(false)]
        [Advanced]
        public bool CanFakeDeathRare = false;

        [Default(2f)]
        [Advanced]
        public float FakeDeathChance = 2f;

        [Default(false)]
        [Advanced]
        public bool CanBegForLife = false;
    }
}