using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalSettingsClass
    {
        public static readonly GlobalSettingsClass Defaults = new GlobalSettingsClass();

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

        [Name("Disable SAIN For Bot Types")]
        [Description("If a option here is set to ON, they will use vanilla logic.")]
        public DisableSAINSettings SAINDisabled = new DisableSAINSettings();

        public PerformanceSettings Performance = new PerformanceSettings();

        public AimSettings Aiming = new AimSettings();

        public CoverSettings Cover = new CoverSettings();

        public ExtractSettings Extract = new ExtractSettings();

        public FlashlightSettings Flashlight = new FlashlightSettings();

        [Name("Force Personality")]
        public PersonalitySettings Personality = new PersonalitySettings();

        public HearingSettings Hearing = new HearingSettings();

        public LookSettings Look = new LookSettings();

        [Name("Looting Bots")]
        public LootingBotsSettings LootingBots = new LootingBotsSettings();

        public MindSettings Mind = new MindSettings();

        public GlobalMoveSettings Move = new GlobalMoveSettings();

        [Name("No Bush ESP")]
        public NoBushESPSettings NoBushESP = new NoBushESPSettings();

        public ShootSettings Shoot = new ShootSettings();

        public TalkSettings Talk = new TalkSettings();

        [Name("Squad Talk")]
        public SquadTalkSettings SquadTalk = new SquadTalkSettings();

        [Name("Power Level Calculation")]
        [Advanced]
        [Hidden]
        public PowerCalcSettings PowerCalc = new PowerCalcSettings();
    }
}