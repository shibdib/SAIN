using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.Preset.Personalities;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalSettingsClass : SettingsGroupBase<GlobalSettingsClass>
    {
        [Hidden]
        [JsonIgnore]
        public static GlobalSettingsClass Instance;

        public GlobalSettingsClass() {
            Instance = this;
        }

        public static GlobalSettingsClass ImportGlobalSettings(SAINPresetDefinition Preset)
        {
            string fileName = FileAndFolderNames[JsonEnum.GlobalSettings];
            string presetsFolder = FileAndFolderNames[JsonEnum.Presets];

            if (!Load.LoadObject(out GlobalSettingsClass result, fileName, presetsFolder, Preset.Name))
            {
                result = new GlobalSettingsClass();
                SaveObjectToJson(result, fileName, presetsFolder, Preset.Name);
            }
            EFTCoreSettings.UpdateCoreSettings();
            return result;
        }

        public GeneralSettings General = new GeneralSettings();
        [Name("Vanilla Bot Behavior Settings")]
        [Description("If a option here is set to ON, they will use vanilla logic, ALL Features will be disabled for these types, including personality, recoil, difficulty, and behavior.")]
        public VanillaBotSettings VanillaBots = new VanillaBotSettings();
        public PerformanceSettings Performance = new PerformanceSettings();
        public AimSettings Aiming = new AimSettings();
        public CoverSettings Cover = new CoverSettings();
        public ExtractSettings Extract = new ExtractSettings();
        public FlashlightSettings Flashlight = new FlashlightSettings();
        [Name("Force Personality")]
        public PersonalitySettings Personality = new PersonalitySettings();
        public HearingSettings Hearing = new HearingSettings();
        public LookSettings Look = new LookSettings();
        [Name("Looting Bots Integration")]
        [Description("Modify settings that relate to Looting Bots. Requires Looting Bots to be installed.")]
        public LootingBotsSettings LootingBots = new LootingBotsSettings();
        public MindSettings Mind = new MindSettings();
        public GlobalMoveSettings Move = new GlobalMoveSettings();
        [Name("No Bush ESP")]
        public NoBushESPSettings NoBushESP = new NoBushESPSettings();
        public ShootSettings Shoot = new ShootSettings();
        public TalkSettings Talk = new TalkSettings();
        [Name("Squad Talk")]
        public SquadTalkSettings SquadTalk = new SquadTalkSettings();
        public DebugSettings Debug = new DebugSettings();
        [Name("Power Level Calculation")]
        [Advanced]
        [Hidden]
        public PowerCalcSettings PowerCalc = new PowerCalcSettings();

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(General);
            SettingsList.Add(VanillaBots);
            SettingsList.Add(Performance);
            SettingsList.Add(Aiming);
            SettingsList.Add(Cover);
            SettingsList.Add(Extract);
            SettingsList.Add(Flashlight);
            SettingsList.Add(Personality);
            SettingsList.Add(Hearing);
            SettingsList.Add(Look);
            SettingsList.Add(LootingBots);
            SettingsList.Add(Mind);
            SettingsList.Add(Move);
            SettingsList.Add(NoBushESP);
            SettingsList.Add(Shoot);
            SettingsList.Add(Talk);
            SettingsList.Add(SquadTalk);
            SettingsList.Add(Debug);
            SettingsList.Add(PowerCalc);
        }
    }
}